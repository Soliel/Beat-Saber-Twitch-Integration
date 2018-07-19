using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TwitchIntegrationPlugin
{
    class Config
    {
        public string Channel { get; set; }
        public string Token { get; set; }
        public string Username { get; set; }
        public bool ModOnly { get; set; }
        public bool SubOnly { get; set; }
        public int ViewerLimit { get; set; }
        public int SubLimit { get; set; }
        public bool ContinueQueue { get; set; }
        public bool Randomize { get; set; }
        public int RandomizeLimit { get; set; }

        public Config(string channel, string token, string username, bool modonly, bool subonly, int viewerlimit, int sublimit, bool continuequeue, bool randomize, int randomizelimit)
        {
            Channel = channel;
            Token = token;
            Username = username;
            ModOnly = modonly;
            SubOnly = subonly;
            ViewerLimit = viewerlimit;
            SubLimit = sublimit;
            ContinueQueue = continuequeue;
            Randomize = randomize;
            RandomizeLimit = randomizelimit;
        }
    }
}
