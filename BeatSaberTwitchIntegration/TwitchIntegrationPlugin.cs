using IllusionPlugin;
using NLog;
using TwitchIntegrationPlugin.UI;
using UnityEngine.SceneManagement;

namespace TwitchIntegrationPlugin
{
    public class TwitchIntegrationPlugin : IPlugin
    {

        public string Name => "Beat Saber Twitch Integration";
        public string Version => "2.0.2_bs-0.11.2";
        private readonly BeatBot _bot = new BeatBot();

        public void OnApplicationStart()
        { 
            _bot.Start();
            StaticData.TwitchMode = false;

            var nlogconfig = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "TILog.txt" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            nlogconfig.AddRule(LogLevel.Error, LogLevel.Fatal, logconsole);
            nlogconfig.AddRule(LogLevel.Error, LogLevel.Fatal, logfile);
            LogManager.Configuration = nlogconfig;
        }

        public void OnApplicationQuit()
        {
            _bot.Exit();
        }

        public void OnLevelWasLoaded(int level)
        {
            if (SceneManager.GetActiveScene().name != "Menu") return;

            TwitchIntegrationUi.OnLoad();
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