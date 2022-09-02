﻿namespace InnerTube;

public class InnerTubeConfiguration
{
	/// <summary>
	/// Used to get the playback info of age-restricted videos.
	/// </summary>
	public InnerTubeAuthorization? Authorization { get; set; } = null;
	
	/// <summary>
	/// Used to determine how many InnerTubePlayerResponse objects should be cached before they expire 
	/// </summary>
	public long CacheSize { get; set; } = 10;
	
	/// <summary>
	/// TimeSpan interval for removing cached InnerTubePlayerResponse objects.
	/// </summary>
	public TimeSpan CacheExpirationPollingInterval { get; set; } = TimeSpan.FromMinutes(10);

	/// <summary>
	/// InnerTube API key. Do not change unless you know what you're doing
	/// </summary>
	public string ApiKey { get; set; } = @"AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";
}