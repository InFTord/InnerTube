﻿using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;

namespace InnerTube.Renderers;

public class VideoRenderer : IRenderer
{
	public string Type { get; }

	public string Id { get; }
	public string Title { get; }
	public string Description { get; }
	public TimeSpan Duration { get; }
	public string Published { get; }
	public string ViewCount { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public Channel Channel { get; }
	public IEnumerable<Badge> Badges { get; }

	public VideoRenderer(JToken renderer)
	{
		Type = renderer.Path.Split(".").Last();
		Id = renderer["videoId"]!.ToString();
		Title = Utils.ReadRuns(renderer.GetFromJsonPath<JArray>("title.runs") ?? new JArray());
		Description = Utils.ReadRuns(renderer.GetFromJsonPath<JArray>("detailedMetadataSnippets[0].snippetText.runs") ?? new JArray());
		Published = renderer["publishedTimeText"]!["simpleText"]!.ToString();
		ViewCount = renderer["viewCountText"]!["simpleText"]!.ToString();
		Thumbnails = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>("thumbnail.thumbnails") ?? new JArray());
		Channel = new Channel
		{
			Id = renderer.GetFromJsonPath<string>("longBylineText.runs[0].navigationEndpoint.browseEndpoint.browseId")!,
			Title = renderer.GetFromJsonPath<string>("longBylineText.runs[0].text")!,
			Avatar = Utils.GetThumbnails(renderer.GetFromJsonPath<JArray>(
				"channelThumbnailSupportedRenderers.channelThumbnailWithLinkRenderer.thumbnail.thumbnails") ?? new JArray()).LastOrDefault()?.Url,
			Subscribers = null
		};
		Badges = renderer["badges"]?.ToObject<JArray>()?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ?? Array.Empty<Badge>();

		if (!TimeSpan.TryParseExact(renderer["lengthText"]?["simpleText"]?.ToString(), "%m\\:%s", CultureInfo.InvariantCulture, out TimeSpan timeSpan))
			if (!TimeSpan.TryParseExact(renderer["lengthText"]?["simpleText"]?.ToString(), "%h\\:%m\\:%s",
				    CultureInfo.InvariantCulture, out timeSpan))
				timeSpan = TimeSpan.Zero;
		Duration = timeSpan;
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder()
			.AppendLine($"[{Type}] {Title}")
			.AppendLine($"- Id: {Id}")
			.AppendLine($"- Duration: {Duration}")
			.AppendLine($"- Published: {Published}")
			.AppendLine($"- ViewCount: {ViewCount}")
			.AppendLine($"- Thumbnail count: {Thumbnails.Count()}")
			.AppendLine($"- Channel: {Channel}")
			.AppendLine($"- Badges: {string.Join(" | ", Badges.Select(x => x.ToString()))}")
			.AppendLine(Description);

		return sb.ToString();
	}
}