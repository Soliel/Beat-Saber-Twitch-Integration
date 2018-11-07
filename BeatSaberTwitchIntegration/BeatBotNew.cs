using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace TwitchIntegrationPlugin
{
    public class BeatBotNew
    {
        private Logger _logger;
        public BeatBotNew()
        {
            _logger = LogManager.GetCurrentClassLogger();
            StaticData.TiConfig = StaticData.TiConfig.LoadFromJson();



        }
    }
}
