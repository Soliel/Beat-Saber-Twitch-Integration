using IllusionPlugin;
using System;
using NLog;
using UnityEngine.SceneManagement;

namespace TwitchIntegrationPlugin
{
    public class TwitchIntegrationPlugin : IPlugin
    {

        public string Name => "Beat Saber Twitch Integration";
        public string Version => "a0.1";
        BeatBot bot = new BeatBot();

        public void OnApplicationStart()
        { 
            bot.Start();
            StaticData.TwitchMode = false;

            var nlogconfig = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "TILog.txt" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            nlogconfig.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            nlogconfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            LogManager.Configuration = nlogconfig;
        }

        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene arg1)
        {
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
        }

        public void OnApplicationQuit()
        {
            bot.Exit();
        }

        public void OnLevelWasLoaded(int level)
        {
            Console.WriteLine("Loading scene " + level);
            if (level == 1)
            {
                TwitchIntegrationUI.OnLoad();
                TwitchIntegration.OnLoad(level);
            }
            else if (level > 1)
            {
                TwitchIntegration.OnLoad(level);
            }
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