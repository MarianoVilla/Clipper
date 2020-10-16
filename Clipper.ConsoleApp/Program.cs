using Alpha.UtilidadesMariano.GeneralLIb.Models;
using Alpha.UtilidadesMariano.GeneralLIb.Util;
using Clipper.Lib;
using Clipper.Lib.TwitchModel;
using FFMpegSharp;
using FFMpegSharp.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
                int Top = 5;
                foreach (var game in TopGames.data.Where(x => x.id != "509663").Take(5))
                {
                    var LocalClipPaths = new List<(ClipInfo, string)>();
                    var Id = int.TryParse(game.id, out int Result) ? Result : 0;
                    if (Id == 0)
                        continue;
                    var Clips = Twitch.GetTopClips(Id, Top, new DateRange(DateTime.Now.AddDays(-1), DateTime.Now));
                    foreach (var c in Clips.data)
                    {
                        var ClipPath = await Twitch.DownloadClipAsync(c);
                        if (ClipPath != null)
                            LocalClipPaths.Add((c, ClipPath));
                    }
                    GameClips.Add(game, LocalClipPaths.OrderBy(x => x.Item2).Select(x => (x.Item1,x.Item2)).ToList());
                }

                foreach (var gameclips in GameClips)
                {
                    try
                    {
                        string FileName = $"{(gameclips.Key.name == "Just Chatting" ? "IRL" : gameclips.Key.name)} TOP {gameclips.Value.Count} CLIPS {DateTime.Now.ToString("MMM-dd", new CultureInfo("en-US"))} {string.Join(", ", gameclips.Value.Select(x => x.Item1.broadcaster_name))}".RemoveInvalidChars().Truncate(100);
                        var OutputDir = Path.Combine(ExeFolder, DateTime.Now.ToString("yyyy_MMM_dd", new CultureInfo("en-US")));
                        Directory.CreateDirectory(OutputDir);
                        var OutputPath = Path.Combine(OutputDir, FileName);
                        var Result = FFMpegLib.Merge(OutputPath + ".mp4", gameclips.Value.OrderBy(x => x.Item2).Select(x => x.Item2).ToArray());
                        BuildMetadata(gameclips, FileName, OutputDir, Result);
                        Directory.GetFiles(ExeFolder, "*.ts").Union(Directory.GetFiles(ExeFolder, "*.mp4")).ToList().ForEach(x => FFMpegHelper.TryDelete(x));
                        //await YouTube.UploadClipAsync(MergedOutputPath);
                        //gameclips.Value.ForEach(
                        //    async x => await YouTube.UploadClipAsync(x.Item2, new BroadcasterInfo() { broadcaster_name = x.Item1.broadcaster_name, game_name = gameclips.Key.name }, x.Item1.title, "public"));
                    }
                    catch (Exception ex)
                    {
                        LogUtil.Log(ex);
                    }

                }
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex);
            }

        }

        private static void BuildMetadata(KeyValuePair<GameInfo, List<(ClipInfo, string)>> gameclips, string FileName, string OutputDir, FFMpegLib.MergeResult Result)
        {
            string Tags = BuildTags(gameclips);
            string Description = BuildDescription(gameclips, Result);
            File.WriteAllText(Path.Combine(OutputDir, FileName.RemoveInvalidChars() + ".txt"), $"{Tags} \n{Description}");
        }

        private static string BuildTags(KeyValuePair<GameInfo, List<(ClipInfo, string)>> gameclips) => $"twitch, clips, top clips, twitch top clips, {gameclips.Key.name}, {gameclips.Key.name} clips, {string.Join(", ", gameclips.Value.Select(x => x.Item1.broadcaster_name))}";

        private static string BuildDescription(KeyValuePair<GameInfo, List<(ClipInfo, string)>> gameclips, FFMpegLib.MergeResult Result)
        {
            var Broadcasters = gameclips.Value.OrderBy(x => x.Item2).Select(x => x.Item1.broadcaster_name);
            var Span = new TimeSpan(00, 00, 00);
            string Timestamps = $"{new DateTime(Span.Ticks):mm:ss} {Broadcasters.FirstOrDefault()} https://twitch.tv/{Broadcasters.FirstOrDefault()}";
            int i = 0;
            foreach (var b in Broadcasters.Skip(1))
            {
                Span = Span.Add(Result.VideosInfo[i].Duration);
                Timestamps += $"\n{new DateTime(Span.Ticks).AddSeconds(-1):mm:ss} {b} {(Timestamps.Contains($"{b}") ? "" : $"https://twitch.tv/{b}")}";
                i++;
            }
            var Description = Timestamps;
            return Description;
        }

        static string ExeFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }
}
