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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Clipper.ConsoleApp
{
    class Program
    {
        static TwitchApi Twitch = new TwitchApi();
        static YouTubeApi YouTube = new YouTubeApi();
        static TwitterApi Twitter = new TwitterApi();
        static async Task Main()
        {
            LogUtil.ConfigurarLog("Clipper.ConsoleApp");
            try
            {
                var TopGames = Twitch.GetTopGames();
                var GameClips = new Dictionary<GameInfo, List<(ClipInfo, string)>>();
                int Top = 5;
                foreach (var game in TopGames.data.Where(x => x.id != "509663").Take(6))
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
                    GameClips.Add(game, LocalClipPaths.OrderBy(x => x.Item2).Select(x => (x.Item1, x.Item2)).ToList());
                }
                var StartDate = DateTimeOffset.Now.AddHours(2);
                foreach (var gameclips in GameClips)
                {
                    try
                    {
                        string FileName = $"TOP {gameclips.Value.Count} {(gameclips.Key.name == "Just Chatting" ? "IRL" : gameclips.Key.name)} TWITCH CLIPS {DateTime.Now.ToString("MMM-dd", new CultureInfo("en-US"))} {string.Join(", ", gameclips.Value.Select(x => x.Item1.broadcaster_name).Distinct())}".RemoveInvalidChars().Truncate(100);
                        var OutputDir = Path.Combine(ExeFolder, DateTime.Now.ToString("yyyy_MMM_dd", new CultureInfo("en-US")));
                        Directory.CreateDirectory(OutputDir);
                        var OutputPath = Path.Combine(OutputDir, FileName);
                        var Result = FFMpegLib.Merge(OutputPath + ".mp4", gameclips.Value.OrderBy(x => x.Item2).Select(x => x.Item2).ToArray());
                        var Tags = BuildTags(gameclips);
                        string Description = BuildDescription(gameclips, Result);
                        BuildMetadata(gameclips, FileName, OutputDir, Result);
                        await YouTube.UploadClipAsync(Result.MergedPath, Title: FileName, Privacy: PrivacyStatus.Private, Tags: Tags, Description: Description, PublishAtUtc: StartDate);
                        StartDate = StartDate.AddHours(3);
                    }
                    catch (Exception ex)
                    {
                        LogUtil.Log(ex);
                    }

                }
                HandleComments();
                Directory.GetFiles(ExeFolder, "*.ts").Union(Directory.GetFiles(ExeFolder, "*.mp4")).ToList().ForEach(x => FFMpegHelper.TryDelete(x));
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex);
            }

        }

        static void HandleComments()
        {
            LogUtil.Log($"Handling comments at path {ExeFolder}");
            var LatestIdsFilePath = Path.Combine(ExeFolder, "LatestIds.txt");
            CommentOnPreviousVideos(LatestIdsFilePath);
            CreateNewLatestIdsFile(LatestIdsFilePath);
        }
        static void CommentOnPreviousVideos(string LatestIdsFilePath)
        {
            try
            {
                if (!File.Exists(LatestIdsFilePath))
                {
                    LogUtil.Log("Found no previous LatestIds file.");
                    return;
                }
                LogUtil.Log("Reading previous LatestIds file.");
                using (var F = new StreamReader(LatestIdsFilePath))
                {
                    var LatestIds = F.ReadLine()?.Split(',');
                    if (LatestIds is null || LatestIds.None())
                    {
                        LogUtil.Log("Found no latest IDs.");
                        return;
                    }
                    LogUtil.Log($"Found {LatestIds.Length} IDs to comment on.");
                    LatestIds.ToList().ForEach(x => YouTube.CommentOnVideo(x, "Like, subscribe and drink water!"));
                }
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex, "Something went wrong while commenting on previous videos.");
            }

        }
        static void CreateNewLatestIdsFile(string LatestIdsFilePath)
        {
            try
            {
                LogUtil.Log("Creating new LatestIds file.");
                FileUtil.DeleteIfExists(LatestIdsFilePath);
                if (YouTube.Responses.None())
                {
                    LogUtil.Log("Found no responses on YouTube API.");
                    return;
                }
                LogUtil.Log($"Found {YouTube.Responses.Count} YouTube responses.");
                File.Create(LatestIdsFilePath);
                using (var F = new StreamWriter(LatestIdsFilePath))
                {
                    F.WriteLine(string.Join(",", YouTube.Responses.Select(x => x.Id)));
                }
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex, "Something went wrong while creating a new LatestIds file.");
            }

        }
        private static void BuildMetadata(KeyValuePair<GameInfo, List<(ClipInfo, string)>> gameclips, string FileName, string OutputDir, FFMpegLib.MergeResult Result)
        {
            string Tags = string.Join(",",BuildTags(gameclips));
            string Description = BuildDescription(gameclips, Result);
            File.WriteAllText(Path.Combine(OutputDir, FileName.RemoveInvalidChars() + ".txt"), $"{Tags} \n{Description}");
        }

        private static string[] BuildTags(KeyValuePair<GameInfo, List<(ClipInfo, string)>> gameclips) => $"twitch, clips, top clips, twitch top clips, {gameclips.Key.name}, {gameclips.Key.name} clips, {string.Join(", ", gameclips.Value.Select(x => x.Item1.broadcaster_name))}".Split(',');

        private static string BuildDescription(KeyValuePair<GameInfo, List<(ClipInfo, string)>> gameclips, FFMpegLib.MergeResult Result)
        {
            var Broadcasters = gameclips.Value.OrderBy(x => x.Item2).Select(x => x.Item1.broadcaster_name);
            var Span = new TimeSpan(00, 00, 00);
            string Timestamps = $"{new DateTime(Span.Ticks):mm:ss} {Broadcasters.FirstOrDefault()} https://twitch.tv/{Broadcasters.FirstOrDefault()}";
            int i = 0;
            foreach (var b in Broadcasters.Skip(1))
            {
                Span = Span.Add(Result.VideosInfo[i].Duration);
                Timestamps += $"\n{new DateTime(Span.Ticks).AddMilliseconds(-500):mm:ss} {b} {(Timestamps.Contains($"{b}") ? "" : $"https://twitch.tv/{Regex.Replace(b, @"\s+", "")}")}";
                i++;
            }
            var Description = @"Subscribe for more! https://www.youtube.com/channel/UCQ-GfbkzKiQ0Xy-glx7DCyQ?sub_confirmation=1
Top Twitch clips daily!" + "\n" + Timestamps;
            return Description;
        }

        static string ExeFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }
}
