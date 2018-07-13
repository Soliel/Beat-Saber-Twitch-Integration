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

        public Config(string channel, string token, string username)
        {
            Channel = channel;
            Token = token;
            Username = username;
        }
    }
}
