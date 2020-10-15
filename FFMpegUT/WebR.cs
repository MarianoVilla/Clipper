using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Webber.Core
{
    public delegate void DownloadProgress(double progress, string file);

    public static class WebR
    {
        private static int BufferSize { get; set; }
        public static DownloadProgress OnProgress;

        static WebR()
        {
            BufferSize = 4096;
        }

        public static void Download(string path, string url, bool retryOnTimeout = false)
        {
            WebRequest request = WebRequest.Create(url);

            try
            {
                using (var response = request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        FileInfo file = new FileInfo(path);

                        Stream fileStream = file.Open(FileMode.OpenOrCreate);
                        byte[] buffer = new byte[BufferSize];

                        long downloadedBytes = 0,
                             totalBytes = response.ContentLength;

                        int length;

                        double initialProgress = 0;

                        while ((length = stream.Read(buffer, 0, BufferSize)) != 0)
                        {
                            fileStream.Write(buffer, 0, length);

                            if (OnProgress != null)
                            {
                                downloadedBytes += length;
                                double progress = Math.Round(((double)downloadedBytes / totalBytes) * 100, 0);

                                if (progress > initialProgress)
                                    OnProgress(progress, file.Name);

                                initialProgress = progress;
                            }
                        }

                        fileStream.Close();
                        fileStream.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                if (retryOnTimeout)
                {
                    Thread.Sleep(4000);
                    Download(url, path);
                }
            }
        }

        public static async Task DownloadAsync(string path, params string[] links)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            List<Task> downloadFiles = new List<Task>();

            foreach (var link in links)
            {
                downloadFiles.Add(new Task(() => {

                    string fileName = link.Remove(0, link.LastIndexOf('/') + 1),
                           filePath = string.Format(@"{0}\{1}", path, fileName);
                    Download(filePath, link);
                }));

                downloadFiles.Last().Start();
            }

            await Task.WhenAll(downloadFiles);
        }

        public static async Task DownloadAsync(string path, IEnumerable<string> links)
        {
            await DownloadAsync(path, links.ToArray());
        }

        //public static string Post()
        //{

        //}

        public static string QueryString(Dictionary<string, string> collection)
        {
            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);

            foreach (var entry in collection)
                queryString[entry.Key] = entry.Value;

            return queryString.ToString();
        }

        public static string Get(string url)
        {
            WebRequest request = WebRequest.Create(url);

            string responseString = string.Empty;

            using (var response = request.GetResponse())
            {
                using (var stream = new StreamReader(response.GetResponseStream()))
                {
                    responseString = stream.ReadToEnd();
                }
            }

            return responseString;
        }

        //public static async Task<string> GetAsync(string url)
        //{
        //    Task.Run(() => {

        //    });
        //}

        /*
        TO-DO CRUD
        */
    }
}
