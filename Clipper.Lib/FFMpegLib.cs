using FFMpegSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipper.Lib
{
    public class FFMpegLib
    {
        public class MergeResult
        {
            public string MergedPath { get; set; }
            public VideoInfo[] VideosInfo { get; set; }
        }
        public static MergeResult Merge(string OutPath, params string[] Paths)
        {
            string ffmpegRoot = ConfigurationManager.AppSettings["ffmpegRoot"];
            //ToDo: add intro/outro.
            string Intro = ConfigurationManager.AppSettings["IntroClip"];
            string Outro = ConfigurationManager.AppSettings["OutroClip"];
            FFMpeg encoder = new FFMpeg(ffmpegRoot);

            encoder.Join(OutPath, out VideoInfo[] Info, Paths);
            return new MergeResult() { MergedPath = OutPath, VideosInfo = Info };
        }
    }
}
