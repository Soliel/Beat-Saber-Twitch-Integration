using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRUI;

namespace TwitchIntegrationPlugin
{
    class TwitchIntegration : MonoBehaviour
    {
        TwitchIntegrationUI ui;
        public static TwitchIntegration _instance;
        static int _loadedLevel;

        public static void OnLoad(int level)
        {
            Console.WriteLine("Level jacker loaded.");
            _loadedLevel = level;

            if(_instance == null)
            {
                new GameObject("Beat Saber Twitch Integration").AddComponent<TwitchIntegration>();
                return;
            }
            else
            {
                _instance.OnLevelChange();
            }
        }

        public void OnLevelChange()
        {
            Console.WriteLine("Level Changed was called: current level is " + _loadedLevel);

            if (_loadedLevel <= 1 && StaticData.TwitchMode && StaticData.songQueue.Count > 0)
            {
                StartCoroutine(WaitForResults());
            }
        }

        private void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(this);
            ui = TwitchIntegrationUI._instance;
        }

        IEnumerator WaitForResults()
        {
            yield return new WaitUntil(() => { return Resources.FindObjectsOfTypeAll<ResultsViewController>().Count() > 0; });
            ResultsViewController results = Resources.FindObjectsOfTypeAll<ResultsViewController>().First();

            results.resultsViewControllerDidPressContinueButtonEvent += delegate (ResultsViewController viewController) {

                try
                {
                    TwitchIntegrationMasterViewController queue = ui.CreateViewController<TwitchIntegrationMasterViewController>("twitch panel");

                    viewController.DismissModalViewController(null, true);
                    FindObjectOfType<SongSelectionMasterViewController>().DismissModalViewController(null, true);
                    FindObjectOfType<SoloModeSelectionViewController>().DismissModalViewController(null, true);

                    FindObjectOfType<MainMenuViewController>().PresentModalViewController(queue, null, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"RESULTS EXCEPTION: {e}");
                }
            };
        }
    }
}
