using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipper.Lib.TwitchModel
{
    public class BroadcasterInfoResponse
    {
        public List<BroadcasterInfo> data { get; set; }
    }
    public class BroadcasterInfo
    {
        public string broadcaster_id { get; set; }
        public string broadcaster_name { get; set; }
        public string broadcaster_language { get; set; }
        public string game_id { get; set; }
        public string game_name { get; set; }
        public string title { get; set; }
    }
}
