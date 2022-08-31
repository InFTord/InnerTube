﻿using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public static class Utils
{
	public static T? GetFromJsonPath<T>(this JToken json, string jsonPath)
	{
		try
		{
			string[] properties = jsonPath.Split(".");
			JToken? current = json;
			foreach (string key in properties)
			{
				Match match = Regex.Match(key, @"\[([0-9]*)\]");
				current = match.Success ? current[key.Split("[")[0]]?[int.Parse(match.Groups[1].Value)] : current[key];
				if (current is null) break;
			}

			return current is null ? default : current.ToObject<T>();
		}
		catch
		{
			return default;
		}
	}

	public static string ReadRuns(JArray runs, bool includeFormatting = true)
	{
		string str = "";
		foreach (JToken runToken in runs)
		{
			JObject run = runToken as JObject;
			if (run is null) continue;

			if (!includeFormatting)
			{
				str += run["text"];
			}
			else if (run.ContainsKey("bold"))
			{
				str += "<b>" + run["text"] + "</b>";
			}
			else if (run.ContainsKey("navigationEndpoint"))
			{
				if (run?["navigationEndpoint"]?["urlEndpoint"] is not null)
				{
					string url = run["navigationEndpoint"]?["urlEndpoint"]?["url"]?.ToString() ?? "";
					if (url.StartsWith("https://www.youtube.com/redirect"))
					{
						NameValueCollection qsl = HttpUtility.ParseQueryString(url.Split("?")[1]);
						url = qsl["url"] ?? qsl["q"];
					}

					str += $"<a href=\"{url}\">{run["text"]}</a>";
				}
				else if (run?["navigationEndpoint"]?["commandMetadata"] is not null)
				{
					string url = run["navigationEndpoint"]?["commandMetadata"]?["webCommandMetadata"]?["url"]
						?.ToString() ?? "";
					if (url.StartsWith("/"))
						url = "https://youtube.com" + url;
					str += $"<a href=\"{url}\">{run["text"]}</a>";
				}
			}
			else
			{
				str += run["text"];
			}
		}

		return str;
	}

	public static Thumbnail[] GetThumbnails(JArray thumbnails)
	{
		return thumbnails.Select(x =>
		{
			string url = x["url"]!.ToObject<string>()!;
			Thumbnail a = new()
			{
				Width = x["width"]?.ToObject<int>(),
				Height = x["height"]?.ToObject<int>(),
				Url = url.StartsWith("http")
					? new Uri(url)
					: new Uri("https:" + url)
			};
			return a;
		}).ToArray();
	}

	public static Dictionary<int, Uri> GetLevelsFromStoryboardSpec(string? specStr, long duration)
	{
		Dictionary<int, Uri> urls = new();
		if (specStr is null) return new Dictionary<int, Uri>();
		List<string> spec = new(specStr.Split("|"));
		string baseUrl = spec[0];
		spec.RemoveAt(0);
		spec.Reverse();
		int L = spec.Count - 1;
		for (int i = 0; i < spec.Count; i++)
		{
			string[] args = spec[i].Split("#");
			int width = int.Parse(args[0]);
			int height = int.Parse(args[1]);
			int frameCount = int.Parse(args[2]);
			int cols = int.Parse(args[3]);
			int rows = int.Parse(args[4]);
			string N = args[6];
			string sigh = args[7];
			string url = baseUrl
				.Replace("$L", (spec.Count - 1 - i).ToString())
				.Replace("$N", N) + "&sigh=" + sigh;
			float fragmentCount = frameCount / (cols * rows);
			float fragmentDuration = duration / fragmentCount;

			for (int j = 0; j < Math.Ceiling(fragmentCount); j++)
				urls.TryAdd(spec.Count - 1 - i, new Uri(url.Replace("$M", j.ToString())));
		}

		return urls;
	}

	public static TimeSpan ParseDuration(string duration)
	{
		if (!TimeSpan.TryParseExact(duration, "%m\\:%s", CultureInfo.InvariantCulture, out TimeSpan timeSpan))
			if (!TimeSpan.TryParseExact(duration, "%h\\:%m\\:%s",
				    CultureInfo.InvariantCulture, out timeSpan))
				timeSpan = TimeSpan.Zero;
		return timeSpan;
	}

	public static string GetParams(this ChannelTabs tab) =>
		tab switch
		{
			ChannelTabs.Home => "EghmZWF0dXJlZA%3D%3D",
			ChannelTabs.Videos => "EgZ2aWRlb3M%3D",
			ChannelTabs.Playlists => "EglwbGF5bGlzdHM%3D",
			ChannelTabs.Community => "Egljb21tdW5pdHk%3D",
			ChannelTabs.Channels => "EghjaGFubmVscw%3D%3D",
			ChannelTabs.About => "EgVhYm91dA%3D%3D",
			ChannelTabs.Search => "EgZzZWFyY2g%3D",
			var _ => ""
		};
}