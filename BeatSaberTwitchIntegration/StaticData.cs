using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TwitchIntegrationPlugin
{
    public static class StaticData
    {
        public static bool TwitchMode { get; set; }
        public static ArrayList QueueList = new ArrayList();
        public static bool DidStartFromQueue = false;
        public static LevelCompletionResults LastLevelCompletionResults;
        public static IStandardLevelDifficultyBeatmap LastLevelPlayed;
    }
}
