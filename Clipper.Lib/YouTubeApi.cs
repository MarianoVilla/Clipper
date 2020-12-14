using Alpha.UtilidadesMariano.GeneralLIb.Util;
using Clipper.Lib.TwitchModel;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clipper.Lib
{
    public class YouTubeApi
    {
        static string GetClippedApiKey = AppSettings.Instance["YouTubeApiKey"];
        static string YouTubeApiUrl = @"https://www.googleapis.com/youtube/v3";
        public List<Video> Responses { get; } = new List<Video>();

        public async Task UploadClipAsync(string FilePath, BroadcasterInfo Info = null, string Title = null, string Privacy = null, IEnumerable<string> Tags = null, string Description = null, DateTimeOffset? PublishAtUtc = null)
        {
            try
            {
                Title ??= Path.GetFileNameWithoutExtension(FilePath);
                Privacy ??= PrivacyStatus.Private;

                LogUtil.Log($"Uploading {FilePath}\n{Title}");

                var youtubeService = BuildService(await BuildCredential(YouTubeService.Scope.YoutubeUpload));
                var video = new Video();
                video.Snippet = new VideoSnippet();
                video.Snippet.Title = Title;
                video.Snippet.Description = "\n" + Description ?? $"\nFollow {Info.broadcaster_name ?? "the streamer"}! Twitch.tv/{Info.broadcaster_name}";
                video.Snippet.Tags = Tags?.ToArray() ?? new[]{ "clip", Info.broadcaster_name, Info.broadcaster_language, Info.game_name };
                video.Snippet.CategoryId = "22";
                video.Status = new VideoStatus();
                video.Status.PrivacyStatus = Privacy;
                video.Status.PublishAt = PublishAtUtc.HasValue ? PublishAtUtc.Value.ToString("yyyy-MM-ddTHH:mm:ss.000zzz") : JsonConvert.SerializeObject(DateTime.UtcNow.AddMinutes(5));
                var filePath = FilePath;

                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                    videosInsertRequest.ResponseReceived += VideosInsertRequest_ResponseReceived;
                    var Res = await videosInsertRequest.UploadAsync();

                }
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex);
            }

        }
        private void VideosInsertRequest_ResponseReceived(Video Vid)
        {
            LogUtil.Log($"Received response for video {Vid.Snippet.Title}, with ID {Vid.Id}.");
            Responses.Add(Vid);
        }

        public async Task CommentOnVideo(string Id, string Comment)
        {
            try
            {
                LogUtil.Log($"Commenting on video with ID {Id}.");
                var youtubeService = BuildService(await BuildCredential(YouTubeService.Scope.YoutubeUpload));

                // Define the CommentThread object, which will be uploaded as the request body.
                CommentThread commentThread = new CommentThread();

                // Add the snippet object property to the CommentThread object.
                CommentThreadSnippet snippet = new CommentThreadSnippet();
                Comment topLevelComment = new Comment();
                CommentSnippet commentSnippet = new CommentSnippet() { TextOriginal = Comment };
                topLevelComment.Snippet = commentSnippet;
                snippet.TopLevelComment = topLevelComment;
                snippet.VideoId = Id;
                commentThread.Snippet = snippet;

                // Define and execute the API request
                var request = youtubeService.CommentThreads.Insert(commentThread, "snippet");
                CommentThread response = await request.ExecuteAsync();
                LogUtil.Log($"CommentThread responseId: {response.Id}");
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex, "There was an error while commenting.");
            }

        }

        public async Task ChannelInfo()
        {
            try
            {
                var youtubeService = BuildService(await BuildCredential(YouTubeService.Scope.YoutubeReadonly));
                var channelsListRequest = youtubeService.Channels.List("contentDetails");
                channelsListRequest.Id = "UCQ-GfbkzKiQ0Xy-glx7DCyQ";

                var channelsListResponse = await channelsListRequest.ExecuteAsync();
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex);
            }

        }
        static string GetClippedPlaylistId = "UUQ-GfbkzKiQ0Xy-glx7DCyQ";
        public async Task<PlaylistItemListResponse> GetVideos(string PlaylistId = null, int MaxResults = 5)
        {
            PlaylistId ??= GetClippedPlaylistId;
            using(var Client = new WebClient())
            {
                return (await Client.DownloadStringTaskAsync($"{YouTubeApiUrl}/playlistItems?playlistId={PlaylistId}&key={GetClippedApiKey}&part=snippet&maxResults={MaxResults}")).FromJson<PlaylistItemListResponse>();
            }
        }
        async Task<UserCredential> BuildCredential(params string[] Scopes)
        {
            using (var stream = new FileStream(@"C:\Users\dager\source\repos\Clipper\Clipper.ConsoleApp\client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None
                );
            }
        }
        YouTubeService BuildService(UserCredential Credential) => 
            new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credential,
                ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
            });
    }
}
