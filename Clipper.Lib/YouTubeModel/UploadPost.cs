using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipper.Lib.YouTubeModel
{
    public class UploadPost
    {
        public Snippet snippet { get; set; }
        public Status status { get; set; }
    }
    public class Snippet
    {
        public string categoryId { get; set; }
        public string description { get; set; }
        public string title { get; set; }
    }

    public class Status
    {
        public string privacyStatus { get; set; }
    }
}
