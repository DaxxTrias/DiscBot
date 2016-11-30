using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscBot.JSON
{
    public class Credentials
    {
        public string Token { get; set; } = "";
        public string ClientId { get; set; }
        public ulong BotId { get; set; } = 1212;
        public ulong[] Owners { get; set; } = {12, 12};
    }
    class JSON
    {
    }
}
