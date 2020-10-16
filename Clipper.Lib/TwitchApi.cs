using Alpha.UtilidadesMariano.GeneralLIb.Util;
using Clipper.Lib.TwitchModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Clipper.Lib
{
    public class TwitchApi
    {
        private OauthResponse Auth = new OauthResponse() { access_token = "cm7z9mgik2ppfrkbbw2ljud4tx37e4", expires_in = 5290152, token_type = "bearer" };
        private string ClientId = "7naigan80t3pco9z7rzfemx7n30r9e";
        public TwitchApi()
        {
            RefreshToken();
        }
        public void RefreshToken()
        {
            using(var Client = new WebClient())
            {
                Auth = Client.UploadString($"https://id.twitch.tv/oauth2/token?client_id={ClientId}&client_secret=r70is99w7xa5d4m3qnvrd5eoekhk1d&grant_type=client_credentials", "").FromJson<OauthResponse>();
            }
        }
        public TopGamesResponse GetTopGames()
        {
            using (var Client = CreateAuthenticatedClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                return Client.DownloadString(new Uri("https://api.twitch.tv/helix/games/top"))?.FromJson<TopGamesResponse>();
            }
        }
        //ToDo: filter by language and date.
        public ClipsResponse GetTopClips(int GameId, int First = 5)
        {
            using (var Client = CreateAuthenticatedClient())
            {
                return Client.DownloadString($"https://api.twitch.tv/helix/clips?game_id={GameId}&first={First}")?.FromJson<ClipsResponse>();
            }
        }
        public async Task<string> DownloadClipAsync(ClipInfo Clip, string OutputFolder = null)
        {
            string ClipDirect = Clip.thumbnail_url.Replace("-preview-480x272.jpg", ".mp4");
            OutputFolder ??= Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string OutputPath = Path.Combine(OutputFolder, $"{Clip.broadcaster_name}_{Clip.title}.mp4");
            using (var Client = new WebClient())
            {
                var Data = await Client.DownloadDataTaskAsync(ClipDirect);
                File.WriteAllBytes(OutputPath, Data);
            }
            return OutputPath;
        }
        public BroadcasterInfo GetBroadcasterInfo(string BroadcasterId)
        {
            try
            {
                using (var Client = CreateAuthenticatedClient())
                {
                    return Client.DownloadString($"https://api.twitch.tv/helix/channels?broadcaster_id={BroadcasterId}")?.FromJson<BroadcasterInfoResponse>()?.data?.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex);
                return null;
            }
        }
        WebClient CreateAuthenticatedClient()
        {
            var Client = new WebClient();
            Client.Headers.Add("Authorization", $"Bearer {Auth.access_token}");
            Client.Headers.Add("Client-ID", ClientId);
            Client.Headers.Add("Accept", "*/*");
            return Client;
        }
    }
}
;