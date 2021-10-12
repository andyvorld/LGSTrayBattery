using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayGHUB
{
    struct GHUBMsg
    {
#pragma warning disable 0649
        public string msgId;
        public string verb;
        public string path;
        public string origin;
        public JObject result;
        public JObject payload;
#pragma warning restore 0649
    }
}
