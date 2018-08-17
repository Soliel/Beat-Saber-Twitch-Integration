using IllusionPlugin;
using NLog;
using TwitchIntegrationPlugin.UI;
using UnityEngine.SceneManagement;

namespace TwitchIntegrationPlugin
{
    public class TwitchIntegrationPlugin : IPlugin
    {

        public string Name => "Beat Saber Twitch Integration";
        public string Version => "2.0_bs-0.11.1";
        private readonly BeatBot _bot = new BeatBot();
        //private NLog.Logger _logger; 

        public void OnApplicationStart()
        { 
            _bot.Start();
            StaticData.TwitchMode = false;

            var nlogconfig = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "TILog.txt" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            nlogconfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
            nlogconfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            LogManager.Configuration = nlogconfig;
            //logger = LogManager.GetCurrentClassLogger();
        }

        public void OnApplicationQuit()
        {
            _bot.Exit();
        }

        public void OnLevelWasLoaded(int level)
        {
            if (SceneManager.GetActiveScene().name != "Menu") return;

            TwitchIntegrationUi.OnLoad();
            //TwitchIntegration.OnLoad(SceneManager.GetActiveScene().name);
            LevelRequestFlowCoordinator.OnLoad(SceneManager.GetActiveScene().name);
        }

        public void OnLevelWasInitialized(int level)
        {
        
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }
    }
}