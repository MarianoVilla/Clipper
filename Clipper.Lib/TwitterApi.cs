using Alpha.UtilidadesMariano.GeneralLIb.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Core.Models;
using Tweetinvi.Models;

namespace Clipper.Lib
{
    public class TwitterApi
    {
        public static string ApiKey { get; private set; } = "KuL4KWtzBlUN3gDFrXeggJi0R";
        public static string ApiSecret { get; private set; } = "9ih42bWMY14lyhIg2gZOukbdPkyty5ujEaTmbhqmlhCOLKh8Ii";
        public static string BearerToken { get; private set; } = "AAAAAAAAAAAAAAAAAAAAANCIIwEAAAAAVJHsD3UXWdT18ISVuVTvljncxqk%3D0Cv34Qz1zx1puSEJmQZrbGLH2pYWPfXjKBk9c0BlkfpiFDVsSj";
        public static string AccessToken { get; private set; } = "1317188480708104194-CexTREZLYKrYCiiP9bHmBSbnsArBnX";
        public static string AccessTokenSecret { get; private set; } = "C9L8qwjwaMnPkPzJlthEQhHrckgnIQT7dHAT0hjniSeU6";
        TwitterClient Client;

        public TwitterApi()
        {
            try
            {
                LogUtil.Log("Building Twitter client.");
                var credentials = new TwitterCredentials(ApiKey, ApiSecret, AccessToken, AccessTokenSecret);
                Client = new TwitterClient(credentials);
                var authenticatedUser = Client.Users.GetAuthenticatedUserAsync().GetAwaiter().GetResult();
                LogUtil.Log($"Authenticated user: {authenticatedUser.Name}");
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex);
            }

            
        }
        public async Task Tweet(string Text)
        {
            try
            {
                _ = await Client.Tweets.PublishTweetAsync(Text); 
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex);
            }

        }

    }
}
