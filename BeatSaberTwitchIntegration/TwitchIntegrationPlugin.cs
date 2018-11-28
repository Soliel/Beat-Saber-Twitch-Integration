using IllusionPlugin;
using NLog;
using NLog.Config;
using NLog.Targets;
using TwitchIntegrationPlugin.UI;
using UnityEngine.SceneManagement;

namespace TwitchIntegrationPlugin
{
    public class TwitchIntegrationPlugin : IPlugin
    {

        public string Name => "Beat Saber Twitch Integration";
        public string Version => "2.0.2_bs-0.11.2";
        private static BeatBotNew _bot;
        
        public void OnApplicationStart()
        {
            StaticData.TwitchMode = false;

            LoggingConfiguration nLogConfig = new LoggingConfiguration();

            FileTarget logFile = new FileTarget("logfile") { FileName = "TILog.txt" };
            ConsoleTarget logConsole = new ConsoleTarget("logconsole");

            nLogConfig.AddRule(LogLevel.Trace, LogLevel.Fatal, logConsole);
            nLogConfig.AddRule(LogLevel.Trace, LogLevel.Fatal, logFile);
            LogManager.Configuration = nLogConfig;
            
            _bot = new BeatBotNew();
            SceneManager.sceneLoaded += HandleSceneManagerOnSceneLoaded;
        }

        private void HandleSceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Menu") return;
            TwitchIntegrationUi.OnLoad();
        }

        public void OnApplicationQuit()
        {
            if(StaticData.Config.ContinueQueue)
                StaticData.SongQueue.SaveSongQueue();

            StaticData.BanList.SaveBanList();
        }

        public void OnLevelWasLoaded(int level)
        {
            //TwitchIntegrationUi.OnLoad();
            //LevelRequestFlowCoordinator.OnLoad(SceneManager.GetActiveScene().name);
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