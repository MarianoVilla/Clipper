using Alpha.UtilidadesMariano.GeneralLIb.Util;
using Clipper.Lib.TwitchModel;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
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
        public async Task UploadClipAsync(string FilePath, BroadcasterInfo Info = null, string Title = null, string PrivacyStatus = "private", IEnumerable<string> Tags = null)
        {
            try
            {
                Title ??= Path.GetFileNameWithoutExtension(FilePath);
                LogUtil.Log($"Uploading {FilePath}\n{Title}");

                var youtubeService = BuildService(await BuildCredential());
                var video = new Video();
                video.Snippet = new VideoSnippet();
                video.Snippet.Title = Title;
                video.Snippet.Description = $"Subscribe to {Info.broadcaster_name ?? "the streamer"}! Twitch.tv/{Info.broadcaster_name}";
                video.Snippet.Tags = Tags?.ToArray() ?? new[]{ "clip", Info.broadcaster_name, Info.broadcaster_language, Info.game_name };
                video.Snippet.CategoryId = "22"; // See https://developers.google.com/youtube/v3/docs/videoCategories/list
                video.Status = new VideoStatus();
                video.Status.PrivacyStatus = PrivacyStatus; // or "private" or "public"
                var filePath = FilePath;

                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                    var Res = videosInsertRequest.Upload();
                }
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex);
            }

        }
        async Task<UserCredential> BuildCredential()
        {
            using (var stream = new FileStream(@"C:\Users\dager\source\repos\Clipper\Clipper.ConsoleApp\client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { YouTubeService.Scope.YoutubeUpload },
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
