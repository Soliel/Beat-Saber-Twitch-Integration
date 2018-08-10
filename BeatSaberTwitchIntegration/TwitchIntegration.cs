using System;
using System.Collections;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using NLog;
using TwitchIntegrationPlugin.UI;

namespace TwitchIntegrationPlugin
{
    public class TwitchIntegration : MonoBehaviour
    {
        private TwitchIntegrationUi _ui;
        public static TwitchIntegration Instance;
        //private LevelRequestMasterViewController _twitchIntegrationMasterViewController;
        private static string _loadedLevel;
        private NLog.Logger _logger;

        public static void OnLoad(string levelName)
        {
            //Console.WriteLine("Level jacker loaded.");
            _loadedLevel = levelName;

            if(Instance == null)
            {
                new GameObject("Beat Saber Twitch Integration").AddComponent<TwitchIntegration>();
            }
            else
            {
                Instance.OnLevelChange();
            }
        }

        public void OnLevelChange()
        {
            //Console.WriteLine("Level Changed was called: current level is " + _loadedLevel);
            //var menuSceneSetupData = Resources.FindObjectsOfTypeAll<MenuSceneSetupData>().First();
            if (_loadedLevel != "Menu") return;

            var flag = StaticData.DidStartFromQueue;
            StaticData.DidStartFromQueue = false;

            if (!StaticData.TwitchMode || StaticData.QueueList.Count <= 0) return;

            StartCoroutine(flag ? WaitForMenu() : WaitForResults());
        }

        [UsedImplicitly]
        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
            _ui = TwitchIntegrationUi.Instance;
            _logger = LogManager.GetCurrentClassLogger();
        }

        public IEnumerator WaitForMenu()
        {
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<MainMenuViewController>().Any());
            
            try
            {
                var queue = _ui.CreateViewController<LevelRequestMasterViewController>("Twitch Panel");
                FindObjectOfType<SoloModeSelectionViewController>().DismissModalViewController(null, true);
                FindObjectOfType<MainMenuViewController>().PresentModalViewController(queue, null, true);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to find MainMenuViewController: " + ex);
            }
        }

        
        public IEnumerator WaitForResults()
        {
            _logger.Debug("Waiting for contoller to init.");
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<ResultsViewController>().Any());
            var results = Resources.FindObjectsOfTypeAll<ResultsViewController>().First();

            results.continueButtonPressedEvent += delegate (ResultsViewController viewController) {

                try
                {
                    _logger.Debug("Results!");
                    var queue = _ui.CreateViewController<LevelRequestMasterViewController>("twitch panel");
                    viewController.DismissModalViewController(null, true);

                    FindObjectOfType<StandardLevelListViewController>().DismissModalViewController(null, true);
                    FindObjectOfType<StandardLevelDifficultyViewController>().DismissModalViewController(null, true);
                    FindObjectOfType<StandardLevelDetailViewController>().DismissModalViewController(null, true);
                    FindObjectOfType<SoloModeSelectionViewController>().DismissModalViewController(null, true);
                    FindObjectOfType<MainMenuViewController>().PresentModalViewController(queue, null, true);
                }
                catch (Exception e)
                {
                    _logger.Error($"RESULTS EXCEPTION: {e}");
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
