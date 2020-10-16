using Alpha.UtilidadesMariano.GeneralLIb.Util;
using Clipper.Lib;
using Clipper.Lib.TwitchModel;
using FFMpegSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Clipper.ConsoleApp
{
    class Program
    {
        static TwitchApi Twitch = new TwitchApi();
        static YouTubeApi YouTube = new YouTubeApi();
        static async Task Main()
        {
            LogUtil.ConfigurarLog("Clipper.ConsoleApp");
            try
            {
                var TopGames = Twitch.GetTopGames();
                var GameClips = new Dictionary<GameInfo, List<(ClipInfo, string)>>();
                foreach (var game in TopGames.data.Take(1))
                {
                    var LocalClipPaths = new List<(ClipInfo, string)>();
                    var Id = int.TryParse(game.id, out int Result) ? Result : 0;
                    if (Id == 0)
                        continue;
                    var Clips = Twitch.GetTopClips(Id, 1);
                    foreach (var c in Clips.data)
                    {
                        var ClipPath = await Twitch.DownloadClipAsync(c);
                        if (ClipPath != null)
                            LocalClipPaths.Add((c, ClipPath));
                    }
                    GameClips.Add(game, LocalClipPaths);
                }

                foreach (var gameclips in GameClips)
                {
                    //var MergedOutputPath = FFMpegLib.Merge($@"C:\Users\dager\source\repos\Clipper\FFMpegSharp\Resources\Video\{gameclips.Key}_Merged.mp4", gameclips.Value.Select(x => x.Item2).ToArray());
                    //await YouTube.UploadClipAsync(MergedOutputPath);
                    gameclips.Value.ForEach(
                        async x => await YouTube.UploadClipAsync(x.Item2, new BroadcasterInfo() { broadcaster_name = x.Item1.broadcaster_name, game_name = gameclips.Key.name }, x.Item1.title, "public"));
                }
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex);
            }

        }

    }
}
