namespace youtubeVideoFetcher;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Text.Json;

class VideoFetch
{
    private string _jsonFileName = "videos.json";

    YouTubeService youtubeService;

    public VideoFetch(string apiKey)
    {
        youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = apiKey,
        });
        LogManager.LogMan.Info("Client Initialized. You may now use commands to fetch videos.");
    }


    public void FetchVideosByChannel(string channelUsername)
    {
        LogManager.LogMan.Info("Fetching videos from channel " + channelUsername);

        ChannelsResource.ListRequest channelListRequest = youtubeService.Channels.List("contentDetails");
        channelListRequest.ForUsername = channelUsername;
        ChannelListResponse channelsListResponse = channelListRequest.Execute();

        // read previously saved video data from file
        string jsonString = File.ReadAllText(_jsonFileName);
        VideosData data = JsonSerializer.Deserialize<VideosData>(jsonString);

        // handle initialization if file is empty
        if (data == null || jsonString == "{}")
        {
            data = new VideosData();
            data.Videos = new List<VideoData>();
        }

        int prevCount = data.Videos.Count;
        int duplicates = 0;

        if (channelsListResponse.PageInfo.TotalResults == 0)
        {
            LogManager.LogMan.Info("No videos found for that channel, Cancelling Task.");
            return;
        }

        foreach (Channel channel in channelsListResponse.Items)
        {
            string uploadsListId = channel.ContentDetails.RelatedPlaylists.Uploads;
            string nextPageToken = "";

            while (nextPageToken != null)
            {
                PlaylistItemsResource.ListRequest playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
                playlistItemsListRequest.PlaylistId = uploadsListId;
                playlistItemsListRequest.PageToken = nextPageToken;
                PlaylistItemListResponse playlistItemsListResponse = playlistItemsListRequest.Execute();

                foreach (PlaylistItem playlistItem in playlistItemsListResponse.Items)
                {
                    // check for duplicate videos
                    int index = data.Videos.FindIndex(item => item.VideoID == playlistItem.Snippet.ResourceId.VideoId);
                    if (index >= 0)
                    {
                        duplicates += 1;
                        continue;
                    }

                    VideoData videoData = new VideoData
                    {
                        VideoID = playlistItem.Snippet.ResourceId.VideoId,
                        CreatorID = playlistItem.Snippet.VideoOwnerChannelId,
                    };

                    data.Videos.Add(videoData);
                }

                nextPageToken = playlistItemsListResponse.NextPageToken;
            }
        }

        LogManager.LogMan.Info("-----------------------------------------");
        LogManager.LogMan.Info(duplicates + " duplicate videos ignored");
        LogManager.LogMan.Info((data.Videos.Count - prevCount) + " videos fetched");
        LogManager.LogMan.Info("");
        LogManager.LogMan.Info("Would you like to add all videos to the saved list? (y/n)");
        LogManager.LogMan.Info("-----------------------------------------");

        string confirmation = LogManager.LogMan.ReadLine();

        if (confirmation == "y")
        {
            string videosJsonString = JsonSerializer.Serialize<VideosData>(data);
            File.WriteAllText(_jsonFileName, videosJsonString);

            LogManager.LogMan.Info((data.Videos.Count - prevCount) + " videos added");
        }
        else
        {
            LogManager.LogMan.Info("Task cancelled");
        }
    }

    public void FetchVideoByURL(string videoURL)
    {
        LogManager.LogMan.Info("Fetching video from  " + videoURL);

        // get youtube video ID from url
        string videoURLParams = videoURL.Split("/")[3];
        string videoID = videoURLParams.Substring(8, 11);

        // read previously saved video data from file
        string jsonString = File.ReadAllText(_jsonFileName);
        VideosData data = JsonSerializer.Deserialize<VideosData>(jsonString);

        // handle initialization if file is empty
        if (data == null || jsonString == "{}")
        {
            data = new VideosData();
            data.Videos = new List<VideoData>();
        }

        int prevCount = data.Videos.Count;

        // check for duplicate videos
        int index = data.Videos.FindIndex(item => item.VideoID == videoID);
        if (index >= 0)
        {
            LogManager.LogMan.Info("Video has already been added, Cancelling Task.");
            return;
        }

        VideosResource.ListRequest listRequest = youtubeService.Videos.List("contentDetails,snippet");
        listRequest.Id = videoID;
        VideoListResponse response = listRequest.Execute();

        VideoData videoData = new VideoData
        {
            VideoID = videoID,
            CreatorID = response.Items[0].Snippet.ChannelId,
        };

        data.Videos.Add(videoData);

        LogManager.LogMan.Info("-----------------------------------------");
        LogManager.LogMan.Info(response.Items[0].Snippet.Title + " fetched");
        LogManager.LogMan.Info("");
        LogManager.LogMan.Info("Would you like to add this video to the saved list? (y/n)");
        LogManager.LogMan.Info("-----------------------------------------");

        string confirmation = LogManager.LogMan.ReadLine();

        if (confirmation == "y")
        {
            string videosJsonString = JsonSerializer.Serialize<VideosData>(data);
            File.WriteAllText(_jsonFileName, videosJsonString);

            LogManager.LogMan.Info("Video added");
        }
        else
        {
            LogManager.LogMan.Info("Task cancelled");
        }
    }
}