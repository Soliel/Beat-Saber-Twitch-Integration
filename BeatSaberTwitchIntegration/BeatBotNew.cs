using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using UnityEngine;
using Logger = NLog.Logger;

namespace TwitchIntegrationPlugin
{
    public class BeatBotNew : MonoBehaviour
    {
        private Logger _logger;
        public BeatBotNew()
        {
            _logger = LogManager.GetCurrentClassLogger();

            StaticData.Config = StaticData.Config.LoadFromJson();
            StaticData.SongQueue.LoadSongQueue();
            StaticData.BanList.LoadBanList();


        }
    }
}
