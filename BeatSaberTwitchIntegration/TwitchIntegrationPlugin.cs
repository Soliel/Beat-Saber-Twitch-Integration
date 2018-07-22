using IllusionPlugin;
using System;
using NLog;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchIntegrationPlugin
{
    public class TwitchIntegrationPlugin : IPlugin
    {

        public string Name => "Beat Saber Twitch Integration";
        public string Version => "v1.1_bs-0.11.1";
        BeatBot bot = new BeatBot();
        private NLog.Logger logger; 

        public void OnApplicationStart()
        { 
            bot.Start();
            StaticData.TwitchMode = false;

            var nlogconfig = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "TILog.txt" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            nlogconfig.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            nlogconfig.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            LogManager.Configuration = nlogconfig;
            logger = LogManager.GetCurrentClassLogger();
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
            if (level == 1)
            {
                TwitchIntegrationUI.OnLoad();
                TwitchIntegration.OnLoad(level);
            }
            else if (level > 1)
            {
                TwitchIntegration.OnLoad(level);
            }

            logger.Debug(SceneManager.GetActiveScene().name);
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