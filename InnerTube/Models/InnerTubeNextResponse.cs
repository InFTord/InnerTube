﻿using InnerTube.Exceptions;
using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class InnerTubeNextResponse
{
	public string Id { get; }
	public string Title { get; }
	public string Description { get; }
	public string DateText { get; }
	public string ViewCount { get; }
	public string LikeCount { get; }
	public Channel Channel { get; }
	public CommentThreadRenderer? TeaserComment { get; }
	public string? CommentsContinuation { get; }
	public string? CommentCount { get; }
	public IEnumerable<IRenderer> Recommended { get; }

	public InnerTubeNextResponse(JObject playerResponse)
	{
		JToken resultsArray =
			playerResponse.GetFromJsonPath<JToken>("contents.twoColumnWatchNextResults.results.results");
		if (resultsArray is null || !resultsArray.Any(x => x.Path.EndsWith("contents")))
			throw new InnerTubeException("Cannot get information about this video");

		JToken? errorObject = resultsArray.GetFromJsonPath<JToken>(
			"contents[0].itemSectionRenderer.contents[0].backgroundPromoRenderer");
		if (errorObject is not null)
			throw new NotFoundException(Utils.ReadRuns(errorObject["title"]!["runs"]!.ToObject<JArray>()!));

		Id = playerResponse.GetFromJsonPath<string>("currentVideoEndpoint.watchEndpoint.videoId")!;
		Title = Utils.ReadRuns(resultsArray.GetFromJsonPath<JArray>(
			"contents[0].videoPrimaryInfoRenderer.title.runs")!);
		JArray? descriptionArray = resultsArray.GetFromJsonPath<JArray>(
			"contents[1].videoSecondaryInfoRenderer.description.runs");
		Description = descriptionArray != null ? Utils.ReadRuns(descriptionArray) : "";
		DateText = resultsArray.GetFromJsonPath<string>(
				"contents[0].videoPrimaryInfoRenderer.dateText.simpleText")
			!;
		ViewCount = resultsArray.GetFromJsonPath<string>(
				"contents[0].videoPrimaryInfoRenderer.viewCount.videoViewCountRenderer.viewCount.simpleText")
			!;
		LikeCount = resultsArray.GetFromJsonPath<string>(
				"contents[0].videoPrimaryInfoRenderer.videoActions.menuRenderer.topLevelButtons[0].toggleButtonRenderer.defaultText.simpleText")
			!;
		JObject channelObject = resultsArray.GetFromJsonPath<JObject>(
				"contents[1].videoSecondaryInfoRenderer.owner.videoOwnerRenderer")
			!;
		Channel = new Channel
		{
			Id = channelObject.GetFromJsonPath<string>("navigationEndpoint.browseEndpoint.browseId")!,
			Title = channelObject.GetFromJsonPath<string>("title.runs[0].text")!,
			Avatar = Utils.GetThumbnails(channelObject.GetFromJsonPath<JArray>("thumbnail.thumbnails") ?? new JArray())
				.LastOrDefault()?.Url,
			Subscribers = channelObject.GetFromJsonPath<string>("subscriberCountText.simpleText")!,
			Badges = channelObject.GetFromJsonPath<JArray>("badges")
				?.Select(x => new Badge(x["metadataBadgeRenderer"]!)) ?? Array.Empty<Badge>()
		};

		JObject? commentObject = resultsArray.GetFromJsonPath<JObject>(
			"contents[2].itemSectionRenderer.contents[0].commentsEntryPointHeaderRenderer");
		TeaserComment = commentObject != null
			? new CommentThreadRenderer(
				"",
				commentObject.GetFromJsonPath<string>("teaserContent.simpleText")!,
				new Channel
				{
					Id = null,
					Title =
						commentObject.GetFromJsonPath<string>("teaserAvatar.accessibility.accessibilityData.label")!,
					Avatar = Utils.GetThumbnails(commentObject.GetFromJsonPath<JArray>("teaserAvatar.thumbnails")!)
						.Last()
						.Url,
					Subscribers = null,
					Badges = Array.Empty<Badge>()
				},
				null,
				false,
				null,
				null)
			: null;
		CommentCount = commentObject != null
			? commentObject["commentCount"]!["simpleText"]!.ToString()
			: null;

		CommentsContinuation = resultsArray.GetFromJsonPath<string>(
			"contents[3].itemSectionRenderer.contents[0].continuationItemRenderer.continuationEndpoint.continuationCommand.token");

		JArray? recommendedList =
			playerResponse.GetFromJsonPath<JArray>(
				"contents.twoColumnWatchNextResults.secondaryResults.secondaryResults.results");
		Recommended = recommendedList != null
			? Utils.ParseRenderers(recommendedList)
			: Array.Empty<IRenderer>();
	}
}