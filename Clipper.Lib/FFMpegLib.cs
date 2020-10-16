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
        public static string Merge(string MergedName, params string[] Paths)
        {
            string ffmpegRoot = ConfigurationManager.AppSettings["ffmpegRoot"];
            //ToDo: add intro/outro.
            string Intro = ConfigurationManager.AppSettings["IntroClip"];
            string Outro = ConfigurationManager.AppSettings["OutroClip"];
            FFMpeg encoder = new FFMpeg(ffmpegRoot);

            encoder.Join(MergedName, Paths);
            return null;
        }
    }
}
