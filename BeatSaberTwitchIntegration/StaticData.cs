using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TwitchIntegrationPlugin.Serializables;

namespace TwitchIntegrationPlugin
{
    public static class StaticData
    {
        public static bool TwitchMode = false;
        public static SongQueue SongQueue = new SongQueue();
        public static BanList BanList = new BanList();
        public static Dictionary<string, int> UserRequestCount = new Dictionary<string, int>();
        public static Config Config = new Config(false, false, 0, 0, false, false, 0);
        public static bool DidStartFromQueue = false;
        public static LevelCompletionResults LastLevelCompletionResults;
        public static IStandardLevelDifficultyBeatmap LastLevelPlayed;
    }
}
