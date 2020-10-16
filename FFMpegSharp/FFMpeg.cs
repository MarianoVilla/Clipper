﻿using FFMpegSharp.Enums;
using FFMpegSharp.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FFMpegSharp
{
    public delegate void ConversionHandler(int percentage);

    public class FFMpeg
    {
        private string _ffmpegPath;
        TimeSpan? _totalVideoTime = null;
        FFProbe _probe;
        Process _process;
        public bool IsConverting { get; private set; }

        
        private bool _RunProcess(string args)
        {
            bool SuccessState = true;
            _process = new Process();
            _process.StartInfo.FileName = _ffmpegPath;
            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardInput = true;
            try
            {
                _process.Start();
                _process.ErrorDataReceived += OutputData;
                _process.BeginErrorReadLine();
            }
            catch (Exception)
            {
                SuccessState = false;
            }
            finally
            {
                _process.WaitForExit(120000);
                _process.Close();
                IsConverting = false;
            }
            return SuccessState;
        }

        private void OutputData(object sender, DataReceivedEventArgs e)
        {
            try 
            { 
                if (IsConverting && e.Data != null && OnProgress != null)
                {
                    Regex r = new Regex(@"\w\w:\w\w:\w\w");
                    Match m = r.Match(e.Data);
                    if (e.Data.Contains("frame"))
                    {
                        if (m.Success)
                        {
                            TimeSpan t = TimeSpan.Parse(m.Value);
                            int percentage = (int)(t.TotalSeconds / _totalVideoTime.Value.TotalSeconds * 100);
                            OnProgress(percentage);
                        }
                    }
                }
            }
            catch(Exception)
            {
                throw new Exception("Video Information is not available. Please use FFProbe to intialize your video object.");
            }
        }

        /// <summary>
        /// Passes the conversion percentage when encoding.
        /// </summary>
        public event ConversionHandler OnProgress;

        /// <summary>
        /// Intializes the FFMPEG encoder.
        /// </summary>
        /// <param name="rootPath">Directory root where ffmpeg.exe can be found. If not specified, root will be loaded from config.</param>
        public FFMpeg(string rootPath = null)
        {
            if (rootPath == null)
                rootPath = ConfigurationManager.AppSettings["ffmpegRoot"];

            FFMpegHelper.RootExceptionCheck(rootPath);
            FFProbeHelper.RootExceptionCheck(rootPath);

            _ffmpegPath = rootPath + "\\ffmpeg.exe";
            _probe = new FFProbe(rootPath);
        }

        /// <summary>
        /// Saves a 'png' thumbnail from the input video.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output video file</param>
        /// <param name="seek">Seek position where the thumbnail should be taken.</param>
        /// <param name="thumbWidth">Thumbnail width.</param>
        /// <param name="thumbHeight">Thumbnail height.</param>
        /// <returns>Success state.</returns>
        public bool SaveThumbnail(VideoInfo source, string output, TimeSpan? seek = null, int thumbWidth = 300, int thumbHeight = 169)
        {
            _probe.SetVideoInfo(ref source);

            if (seek == null)
                seek = new TimeSpan(0, 0, 7);

            FFMpegHelper.ConversionExceptionCheck(source, output);
            FFMpegHelper.ExtensionExceptionCheck(output, ".png");

            string thumbArgs,
                   thumbSize = thumbWidth.ToString() + "x" + thumbHeight.ToString();
            thumbArgs = String.Format("-i \"{0}\" -vcodec png -vframes 1 -ss {1} -s {2} \"{3}\"", source.Path,
                                                                                                  seek.ToString(),
                                                                                                  thumbSize,
                                                                                                  output);

            return _RunProcess(thumbArgs);
        }

        /// <summary>
        /// Saves a 'png' thumbnail from the input video.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output video file</param>
        /// <param name="seek">Seek position where the thumbnail should be taken.</param>
        /// <param name="thumbWidth">Thumbnail width.</param>
        /// <param name="thumbHeight">Thumbnail height.</param>
        /// <returns>Success state.</returns>
        public bool SaveThumbnail(string source, string output, TimeSpan? seek = null, int thumbWidth = 300, int thumbHeight = 169)
        {
            return SaveThumbnail(new VideoInfo(source), output, seek, thumbWidth, thumbHeight);
        }

        /// <summary>
        /// Converts a source video to MP4 format.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output video file.</param>
        /// <param name="speed">Conversion speed preset.</param>
        /// <param name="size">Output video size.</param>
        /// <param name="multithread">Use multithreading for conversion.</param>
        /// <returns>Success state.</returns>
        public bool ToMP4(VideoInfo source, string output, Speed speed = Speed.SuperFast, VideoSize size = VideoSize.Original, bool multithread = false)
        {
            _probe.SetVideoInfo(ref source);
            _totalVideoTime = source.Duration;
            IsConverting = true;

            string threadCount = multithread ? 
                                Environment.ProcessorCount.ToString() : "1",
                                scale = FFMpegHelper.GetScale(size);

            FFMpegHelper.ConversionExceptionCheck(source, output);
            FFMpegHelper.ExtensionExceptionCheck(output, ".mp4");

            string conversionArgs = string.Format("-i \"{0}\" -threads {1} {2} -b:v 2000k -vcodec libx264 -preset {3} -g 30 \"{4}\"", source.Path,
                                                                                                                                       threadCount,
                                                                                                                                       scale,
                                                                                                                                       speed.ToString().ToLower(),
                                                                                                                                       output);

            return _RunProcess(conversionArgs);
        }

        /// <summary>
        /// Converts a source video to MP4 format.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output video file.</param>
        /// <param name="speed">Conversion speed preset.</param>
        /// <param name="size">Output video size.</param>
        /// <param name="multithread">Use multithreading for conversion.</param>
        /// <returns>Success state.</returns>
        public bool ToMP4(string source, string output, Speed speed = Speed.SuperFast, VideoSize size = VideoSize.Original, bool multithread = false)
        {
            return ToMP4(new VideoInfo(source), output, speed, size, multithread);
        }

        /// <summary>
        /// Converts a source video to WebM format.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output video file.</param>
        /// <param name="size">Output video size.</param>
        /// <param name="multithread">Use multithreading for conversion.</param>
        /// <returns>Success state.</returns>
        public bool ToWebM(VideoInfo source, string output, VideoSize size = VideoSize.Original, bool multithread = false)
        {
            _probe.SetVideoInfo(ref source);
            _totalVideoTime = source.Duration;
            IsConverting = true;

            string threadCount = multithread ? 
                                 Environment.ProcessorCount.ToString() : "1",
                   scale = FFMpegHelper.GetScale(size);

            FFMpegHelper.ConversionExceptionCheck(source, output);
            FFMpegHelper.ExtensionExceptionCheck(output, ".webm");

            string conversionArgs = string.Format("-i \"{0}\" -threads {1} {2} -vcodec libvpx -quality good -cpu-used 0 -b:v 1500k -qmin 10 -qmax 42 -maxrate 500k -bufsize 1000k -codec:a libvorbis -b:a 128k \"{3}\"", source.Path,
                                                                                                                                                                                                                         threadCount,
                                                                                                                                                                                                                         scale,
                                                                                                                                                                                                                         output);

            return _RunProcess(conversionArgs);
        }

        /// <summary>
        /// Converts a source video to WebM format.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output video file.</param>
        /// <param name="size">Output video size.</param>
        /// <param name="multithread">Use multithreading for conversion.</param>
        /// <returns>Success state.</returns>
        public bool ToWebM(string source, string output, VideoSize size = VideoSize.Original, bool multithread = false)
        {
            return ToWebM(new VideoInfo(source), output, size, multithread);
        }

        /// <summary>
        /// Converts a source video to OGV format.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output video file.</param>
        /// <param name="size">Output video size.</param>
        /// <param name="multithread">Use multithreading for conversion.</param>
        /// <returns>Success state.</returns>
        public bool ToOGV(VideoInfo source, string output, VideoSize size = VideoSize.Original, bool multithread = false)
        {
            _probe.SetVideoInfo(ref source);
            _totalVideoTime = source.Duration;
            IsConverting = true;

            string threadCount = multithread ? 
                                 Environment.ProcessorCount.ToString() : "1",
                   scale = FFMpegHelper.GetScale(size);

            FFMpegHelper.ConversionExceptionCheck(source, output);
            FFMpegHelper.ExtensionExceptionCheck(output, ".ogv");

            string conversionArgs = string.Format("-i \"{0}\" -threads {1} {2} -codec:v libtheora -qscale:v 7 -codec:a libvorbis -qscale:a 5 \"{3}\"", source.Path,
                                                                                                                                                       threadCount,
                                                                                                                                                       scale,
                                                                                                                                                       output);
            return _RunProcess(conversionArgs);
        }

        /// <summary>
        /// Converts a source video to OGV format.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output video file.</param>
        /// <param name="size">Output video size.</param>
        /// <param name="multithread">Use multithreading for conversion.</param>
        /// <returns>Success state.</returns>
        public bool ToOGV(string source, string output, VideoSize size = VideoSize.Original, bool multithread = false)
        {
            return ToOGV(new VideoInfo(source), output, size, true);
        }

        /// <summary>
        /// Converts a source video to TS format.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="IntendedOutput">Output video file.</param>
        /// <returns>Success state.</returns>
        public bool ToTS(VideoInfo source, string IntendedOutput, out string ActualOutput)
        {
            int i = 0;
            ActualOutput = IntendedOutput;
            while (File.Exists(ActualOutput) && FFMpegHelper.IsFileLocked(ActualOutput))
            {
                ActualOutput = Path.Combine(Path.GetDirectoryName(IntendedOutput), Path.GetFileNameWithoutExtension(IntendedOutput) + $"({i}){Path.GetExtension(IntendedOutput)}");
                i++;
            }
            _probe.SetVideoInfo(ref source);
            _totalVideoTime = source.Duration;
            IsConverting = true;
            if (File.Exists(IntendedOutput) && !FFMpegHelper.IsFileLocked(IntendedOutput))
                return true;

            FFMpegHelper.ConversionExceptionCheck(source, IntendedOutput);
            FFMpegHelper.ExtensionExceptionCheck(IntendedOutput, ".ts");

            string conversionArgs = string.Format("-i \"{0}\" -c copy -bsf:v h264_mp4toannexb -f mpegts \"{1}\"", source.Path, IntendedOutput);
            return _RunProcess(conversionArgs);
        }

        /// <summary>
        /// Converts a source video to TS format.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output video file.</param>
        /// <returns>Success state.</returns>
        public bool ToTS(string source, string output, out string ActualOut)
        {
            return ToTS(new VideoInfo(source), output, out ActualOut);
        }

        /// <summary>
        /// Records M3U8 streams to the specified output.
        /// </summary>
        /// <param name="httpStream">URI to pointing towards stream.</param>
        /// <param name="output">Output file</param>
        /// <returns>Success state.</returns>
        public bool SaveM3U8Stream(string httpStream, string output)
        {
            FFMpegHelper.ExtensionExceptionCheck(output, ".mp4");

            string conversionArgs = string.Format("-i \"{0}\" \"{1}\"", httpStream, output);

            return _RunProcess(conversionArgs);
        }

        /// <summary>
        /// Strips a video file of audio.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output video file.</param>
        /// <returns></returns>
        public bool Mute(string source, string output)
        {
            return Mute(new VideoInfo(source), output);
        }

        /// <summary>
        /// Strips a video file of audio.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output video file.</param>
        /// <returns></returns>
        public bool Mute(VideoInfo source, string output)
        {
            _probe.SetVideoInfo(ref source);

            FFMpegHelper.ConversionExceptionCheck(source, output);
            FFMpegHelper.ExtensionExceptionCheck(output, source.Extension);

            string args = string.Format("-i \"{0}\" -c copy -an \"{1}\"", source.Path, output);

            return _RunProcess(args);
        }

        /// <summary>
        /// Saves audio from a specific video file to disk.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output audio file.</param>
        /// <returns>Success state.</returns>
        public bool SaveAudio(string source, string output)
        {
            return SaveAudio(new VideoInfo(source), output);
        }

        /// <summary>
        /// Saves audio from a specific video file to disk.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="output">Output audio file.</param>
        /// <returns>Success state.</returns>
        public bool SaveAudio(VideoInfo source, string output)
        {
            _probe.SetVideoInfo(ref source);

            FFMpegHelper.ConversionExceptionCheck(source, output);
            FFMpegHelper.ExtensionExceptionCheck(output, ".mp3");

            string args = string.Format("-i \"{0}\" -vn -ab 256 \"{1}\"", source.Path, output);

            return _RunProcess(args);
        }

        /// <summary>
        /// Adds audio to a video file.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="audio">Source audio file.</param>
        /// <param name="output">Output video file.</param>
        /// <param name="stopAtShortest">Indicates if the encoding should stop at the shortest input file.</param>
        /// <returns>Success state</returns>
        public bool AddAudio(string source, string audio, string output, bool stopAtShortest = false)
        {
            return AddAudio(new VideoInfo(source), audio, output);
        }

        /// <summary>
        /// Adds audio to a video file.
        /// </summary>
        /// <param name="source">Source video file.</param>
        /// <param name="audio">Source audio file.</param>
        /// <param name="output">Output video file.</param>
        /// <param name="stopAtShortest">Indicates if the encoding should stop at the shortest input file.</param>
        /// <returns>Success state</returns>
        public bool AddAudio(VideoInfo source, string audio, string output, bool stopAtShortest = false)
        {
            _probe.SetVideoInfo(ref source);

            FFMpegHelper.ConversionExceptionCheck(source, output);
            FFMpegHelper.InputFilesExistExceptionCheck(audio);
            FFMpegHelper.ExtensionExceptionCheck(output, source.Extension);

            string args = string.Format("-i \"{0}\" -i \"{1}\" -c:v copy -c:a aac -strict experimental {3} \"{2}\"", source.Path, audio, output, stopAtShortest ? "-shortest" : "");

            return _RunProcess(args);
        }

        /// <summary>
        /// Adds a poster image to an audio file.
        /// </summary>
        /// <param name="image">Source image file.</param>
        /// <param name="audio">Source audio file.</param>
        /// <param name="output">Output video file.</param>
        /// <returns></returns>
        public bool AddPosterToAudio(string image, string audio, string output)
        {
            FFMpegHelper.InputFilesExistExceptionCheck(image, audio);
            FFMpegHelper.ExtensionExceptionCheck(output, ".mp4");

            string args = string.Format("-loop 1 -i \"{0}\" -i \"{1}\" -b:v 2000k -vcodec libx264 -c:a aac -strict experimental -shortest \"{2}\"", image, audio, output);

            return _RunProcess(args);
        }

        /// <summary>
        /// Joins a list of video files.
        /// </summary>
        /// <param name="output">Output video file.</param>
        /// <param name="videos">List of vides that need to be joined together.</param>
        /// <returns>Success state.</returns>
        public bool Join(string output, params VideoInfo[] videos)
        {
            string[] pathList = new string[videos.Length];
            for (int i = 0; i < pathList.Length; i++)
            {
                ToTS(videos[i].Path, videos[i].Path.Replace("mp4", "ts"), out string ActualPath);
                pathList[i] = ActualPath;
            }

            string conversionArgs = string.Format("-i \"concat:{0}\" -c copy -bsf:a aac_adtstoasc \"{1}\"", string.Join(@"|", (object[])pathList), output);
            bool result = _RunProcess(conversionArgs);

            if(result)
                foreach (string path in pathList)
                    if (File.Exists(path))
                        FFMpegHelper.TryDelete(path);

            return result;
        }
        /// <summary>
        /// Joins a list of video files.
        /// </summary>
        /// <param name="output">Output video file.</param>
        /// <param name="videos">List of vides that need to be joined together.</param>
        /// <returns>Success state.</returns>
        public bool Join(string output, out VideoInfo[] infoList, params string[] videos)
        {
            infoList = new VideoInfo[0];
            if (videos.Length > 0)
            {
                infoList = new VideoInfo[videos.Length];
                for (int i = 0; i < videos.Length; i++)
                {
                    var vidInfo = new VideoInfo(videos[i]);
                    _probe.SetVideoInfo(ref vidInfo);
                    infoList[i] = vidInfo;
                }
                return Join(output, infoList);
            }
            return false;
        }

        /// <summary>
        /// Joins a list of video files.
        /// </summary>
        /// <param name="output">Output video file.</param>
        /// <param name="videos">List of vides that need to be joined together.</param>
        /// <returns>Success state.</returns>
        public bool Join(string output, IEnumerable<VideoInfo> videos)
        {
            return Join(output, videos.ToArray());
        }

        /// <summary>
        /// Joins a list of video files.
        /// </summary>
        /// <param name="output">Output video file.</param>
        /// <param name="videos">List of vides that need to be joined together.</param>
        /// <returns>Success state.</returns>
        public bool Join(string output, IEnumerable<string> videos)
        {
            return Join(output, videos.ToArray());
        }

        /// <summary>
        /// Stops any current job that FFMpeg is running.
        /// </summary>
        public void Stop()
        {
            if(!_process.HasExited)
            {
                _process.StandardInput.Write('q');
            }
        }

        /// <summary>
        /// Kills the FFMpeg process. NOTE: killing the process will most likely end in video corruption.
        /// </summary>
        public void Kill()
        {
            if(!_process.HasExited)
            {
                _process.Kill();
            }
        }
    }
}
