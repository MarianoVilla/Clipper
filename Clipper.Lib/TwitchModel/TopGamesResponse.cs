using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipper.Lib.TwitchModel
{
    public class TopGamesResponse
    {
        public List<GameInfo> data { get; set; }
        public Pagination pagination { get; set; }
    }
    public class GameInfo
    {
        public string id { get; set; }
        public string name { get; set; }
        public string box_art_url { get; set; }
    }
}
