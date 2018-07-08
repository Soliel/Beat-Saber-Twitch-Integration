using IllusionPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.SceneManagement;

namespace TwitchIntegrationPlugin
{
    /* TODO section
     * TODO: Change BeatBot.cs to take advantage of Coroutines from MonoBehavior 
     */ 
    public class TwitchIntegrationPlugin : IPlugin
    {

        public string Name => "Beat Saber Twitch Integration";
        public string Version => "a1.0";
        BeatBot bot = new BeatBot();

        public void OnApplicationStart()
        { 
            bot.Start();
            StaticData.TwitchMode = false;
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