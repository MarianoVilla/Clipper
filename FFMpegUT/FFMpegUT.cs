using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FFMpegSharp;
using System.IO;

namespace FFMpegUT
{
    [TestClass]
    public class FFMpegUT
    {
        FFMpeg encoder;
        FileInfo input;

        public FFMpegUT()
        {
            encoder = new FFMpeg();

            input = new FileInfo("input.mp4");
            if (!input.Exists)
            {
                Webber.Core.WebR.Download(input.FullName, "http://r2---sn-aiglln7k.googlevideo.com/videoplayback?initcwndbps=1062500&ipbits=0&mime=video%2Fmp4&sparams=dur%2Cid%2Cinitcwndbps%2Cip%2Cipbits%2Citag%2Clmt%2Cmime%2Cmm%2Cmn%2Cms%2Cmv%2Cnh%2Cpl%2Cratebypass%2Csource%2Cupn%2Cexpire&id=o-ABO6Qt7Jt0HntmDbidoR_KhxDHGooya-uTulDyjzHfsu&mn=sn-aiglln7k&mm=31&ms=au&mv=m&mt=1437308919&pl=33&ip=2a02%3A2498%3Ae003%3A46%3A3%3A90%3A0%3A2&ratebypass=yes&itag=22&sver=3&expire=1437330590&upn=qKtbfS2tEjw&key=yt5&signature=0A09E081A8C6C23BD2BE8F662EA633666E4993C4.A7D1F425A7F94FF92BEA4F206DB8213F30416735&fexp=901816%2C9406192%2C9407888%2C9407943%2C9408142%2C9408420%2C9408710%2C9412471%2C9415417%2C9415657%2C9415955%2C9416126%2C9416899%2C9417203&nh=IgpwcjAzLmxocjE0KgkxMjcuMC4wLjE&lmt=1431977342207817&dur=59.930&source=youtube&title=A+Flat+Animation+-+%28Motion+Graphics%29");
                input = new FileInfo("input.mp4");
            }
        }

        [TestMethod]
        public void InitializeFFMpeg()
        {
            Assert.IsNotNull(encoder);
        }

        [TestMethod]
        public void ConvertToMP4()
        {
            string output = input.Directory.FullName + "\\" + input.Name.Replace(input.Extension, "_converted.mp4");
            if (File.Exists(output))
                File.Delete(output);

            encoder.ToMP4(input.FullName, output);

            Assert.IsTrue(File.Exists(output));
        }

        [TestMethod]
        public void ConvertToTS()
        {
            string output = input.Directory.FullName + "\\" + input.Name.Replace(input.Extension, "_converted.ts");
            if (File.Exists(output))
                File.Delete(output);

            encoder.ToTS(input.FullName, output);

            Assert.IsTrue(File.Exists(output));
        }

        [TestMethod]
        public void ConvertToWEBM()
        {
            string output = input.Directory.FullName + "\\" + input.Name.Replace(input.Extension, "_converted.webm");
            if (File.Exists(output))
                File.Delete(output);

            encoder.ToWebM(input.FullName, output);

            Assert.IsTrue(File.Exists(output));
        }

        [TestMethod]
        public void ConvertToOGV()
        {
            string output = input.Directory.FullName + "\\" + input.Name.Replace(input.Extension, "_converted.ogv");
            if (File.Exists(output))
                File.Delete(output);

            encoder.ToOGV(input.FullName, output);

            Assert.IsTrue(File.Exists(output));
        }

        [TestMethod]
        public void SaveThumbnail()
        {
            string output = input.Directory.FullName + "\\" + input.Name.Replace(input.Extension, "_converted.png");
            if (File.Exists(output))
                File.Delete(output);

            encoder.SaveThumbnail(input.FullName, output);

            Assert.IsTrue(File.Exists(output));
        }

        [TestMethod]
        public void SaveMute()
        {
            string output = input.Directory.FullName + "\\" + input.Name.Replace(input.Extension, "_mute_converted" + input.Extension);
            if (File.Exists(output))
                File.Delete(output);

            encoder.Mute(input.FullName, output);

            Assert.IsTrue(File.Exists(output));
        }

        [TestMethod]
        public void SaveAudio()
        {
            string output = input.Directory.FullName + "\\" + input.Name.Replace(input.Extension, "_audio.mp3");
            if (File.Exists(output))
                File.Delete(output);

            encoder.SaveAudio(input.FullName, output);

            Assert.IsTrue(File.Exists(output));
        }

        [TestMethod]
        public void AddAudio()
        {
            SaveMute();
            SaveAudio();
            string noaudio = input.Directory.FullName + "\\" + input.Name.Replace(input.Extension, "_mute_converted" + input.Extension);
            string audio = input.Directory.FullName + "\\" + input.Name.Replace(input.Extension, "_audio.mp3");
            string output = input.Directory.FullName + "\\" + input.Name.Replace(input.Extension, "_with_audio" + input.Extension);

            if (File.Exists(output))
                File.Delete(output);

            encoder.AddAudio(noaudio, audio, output);

            Assert.IsTrue(File.Exists(output));
        }

        [TestMethod]
        public void AddPoster()
        {
            SaveAudio();
            //SaveThumbnail();
            string poster = input.Directory.FullName + "\\" + input.Name.Replace(input.Extension, "_converted.png");
            string audio = input.Directory.FullName + "\\" + input.Name.Replace(input.Extension, "_audio.mp3");
            string output = input.Directory.FullName + "\\" + input.Name.Replace(input.Extension, "_with_poster.mp4");

            if (File.Exists(output))
                File.Delete(output);

            encoder.AddPosterToAudio(poster, audio, output);

            Assert.IsTrue(File.Exists(output));
        }
    }
}
