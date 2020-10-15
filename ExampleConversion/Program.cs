using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using FFMpegSharp;
using System.Configuration;

namespace ExampleConversion
{
    class Program
    {
        static void Main(string[] args)
        {
            string ffmpegRoot = ConfigurationManager.AppSettings["ffmpegRoot"],
                    inputFile = "G:\\input.mov",
                    outputFile = "G:\\output.mp4";

            FFMpeg encoder = new FFMpeg();
            
            encoder.OnProgress += encoder_OnProgress;

            // encoder.ToWebM(inputFile, outputFile.Replace("mp4", "WEBM"));
            // encoder.ToOGV(inputFile, outputFile.Replace("mp4", "ogv"));
            // encoder.SaveThumbnail(outputFile, "G:\\thumb.jpg", TimeSpan.FromMinutes(1));
            // encoder.ToMP4(inputFile, outputFile);
        }

        static void encoder_OnProgress(int percentage)
        {
            Console.WriteLine("Progress {0}%", percentage);
        }
    }
}
