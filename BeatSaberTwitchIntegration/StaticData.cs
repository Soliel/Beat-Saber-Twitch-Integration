using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TwitchIntegrationPlugin
{
    public static class StaticData
    {
        public static bool TwitchMode { get; set; }
        public static Queue<QueuedSong> songQueue = new Queue<QueuedSong>();
    }
}
