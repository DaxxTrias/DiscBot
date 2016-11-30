using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace DiscBot
{
    public class configuration
    {
        public char Prefix { get; set; }
        public ulong Owner { get; set; }
        public string Token { get; set; }
        public ulong[] BindToChannels { get; set; }
        public ulong defaultRoomID { get; set; }
        public ulong idDefaultGroup { get; set; }
        public ulong idModsGroup { get; set; }
        public ulong idAdminGroup { get; set; }
        public int volume { get; set; }

        public configuration()
        {
            Prefix = '$';
            Owner = new ulong { };
            Token = "";
            BindToChannels = new ulong[] { 0 };
            defaultRoomID = 0;
            volume = 0;
            idDefaultGroup = new ulong { };
            idModsGroup = new ulong { };
            idAdminGroup = new ulong { };

        }

        public void SaveFile(string loc)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);

            if (!File.Exists(loc))
                File.Create(loc).Close();

            File.WriteAllText(loc, json);
        }

        public static configuration LoadFile(string loc)
        {
            string json = File.ReadAllText(loc);
            return JsonConvert.DeserializeObject<configuration>(json);
        }
    }
}
