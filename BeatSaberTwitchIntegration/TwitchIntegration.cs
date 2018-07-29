using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NLog;
using VRUI;

namespace TwitchIntegrationPlugin
{
    class TwitchIntegration : MonoBehaviour
    {
        TwitchIntegrationUI ui;
        public static TwitchIntegration _instance;
        LevelRequestMasterViewController _twitchIntegrationMasterViewController;
        static String _loadedLevel;
        NLog.Logger logger;

        public static void OnLoad(String levelName)
        {
            //Console.WriteLine("Level jacker loaded.");
            _loadedLevel = levelName;

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
            //Console.WriteLine("Level Changed was called: current level is " + _loadedLevel);
            var _menuSceneSetupData = Resources.FindObjectsOfTypeAll<MenuSceneSetupData>().First();
            if (_loadedLevel == "Menu")
            {
                var flag = StaticData.didStartFromQueue;
                StaticData.didStartFromQueue = false;

                if (StaticData.TwitchMode && StaticData.queueList.Count > 0)
                {
                    if(flag)
                    {
                        StartCoroutine(WaitForMenu());
                    }
                    else
                    {
                        StartCoroutine(WaitForResults());
                    }
                }
            }
        }

        private void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(this);
            ui = TwitchIntegrationUI._instance;
            logger = LogManager.GetCurrentClassLogger();
        }

        IEnumerator WaitForMenu()
        {
            yield return new WaitUntil(() => { return Resources.FindObjectsOfTypeAll<SoloModeSelectionViewController>().Count() > 0; });
            
            try
            {
                LevelRequestMasterViewController queue = ui.CreateViewController<LevelRequestMasterViewController>("Twitch Panel");
                FindObjectOfType<SoloModeSelectionViewController>().DismissModalViewController(null, true);
                FindObjectOfType<MainMenuViewController>().PresentModalViewController(queue, null, true);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to find MainMenuViewController: " + ex);
            }
        }

        
        IEnumerator WaitForResults()
        {
            logger.Debug("Waiting for contoller to init.");
            yield return new WaitUntil(() => { return Resources.FindObjectsOfTypeAll<ResultsViewController>().Count() > 0; });
            ResultsViewController results = Resources.FindObjectsOfTypeAll<ResultsViewController>().First();

            results.continueButtonPressedEvent += delegate (ResultsViewController viewController) {

                try
                {
                    logger.Debug("Results!");
                    LevelRequestMasterViewController queue = ui.CreateViewController<LevelRequestMasterViewController>("twitch panel");
                    viewController.DismissModalViewController(null, true);

                    FindObjectOfType<StandardLevelListViewController>().DismissModalViewController(null, true);
                    FindObjectOfType<StandardLevelDifficultyViewController>().DismissModalViewController(null, true);
                    FindObjectOfType<StandardLevelDetailViewController>().DismissModalViewController(null, true);
                    FindObjectOfType<SoloModeSelectionViewController>().DismissModalViewController(null, true);
                    FindObjectOfType<MainMenuViewController>().PresentModalViewController(queue, null, true);
                }
                catch (Exception e)
                {
                    logger.Error($"RESULTS EXCEPTION: {e}");
                }
            };
        }

        //Recursive function to get all children of a view controller. I don't use it now, but it may be useful so leaving it in as a comment.
        /*public List<VRUIViewController> getAllChildren(VRUIViewController viewController)
        {
            Console.WriteLine("GetAllChildren Iteration.");
            if(viewController.childViewController == null)
            {
                return new List<VRUIViewController>();
            }
            var child = viewController.childViewController;
            return (List<VRUIViewController>)new List<VRUIViewController>() { viewController }.Union(getAllChildren(viewController.childViewController));
        }*/


    }
}
