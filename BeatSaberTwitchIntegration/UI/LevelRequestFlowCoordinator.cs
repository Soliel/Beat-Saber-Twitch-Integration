using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using VRUI;
using SongLoaderPlugin;
using NLog;
using SongLoaderPlugin.OverrideClasses;

namespace TwitchIntegrationPlugin.UI
{
    public class LevelRequestFlowCoordinator : FlowCoordinator
    {
        private MenuSceneSetupData _menuSceneSetupData;
        private MainGameSceneSetupData _mainGameSceneSetupData;
        private ResultsFlowCoordinator _resultsFlowCoordinator;
        private LevelRequestNavigationController _levelRequestNavigationController;
        private RequestInfoViewController _requestInfoViewController;
        private StandardLevelDifficultyViewController _levelDifficultyViewController;
        private StandardLevelDetailViewController _levelDetailViewController;
        private TwitchIntegrationUi _ui;
        private QueuedSong _song;
        private CustomLevel _customLevel;
        private NLog.Logger _logger;

        private bool _initialized;

        public event Action DidFinishEvent;

        public void Present(VRUIViewController parentViewController, bool fromDebug)
        {
            _ui = TwitchIntegrationUi.Instance;
            _logger = LogManager.GetCurrentClassLogger();

            try
            {
                _menuSceneSetupData = Resources.FindObjectsOfTypeAll<MenuSceneSetupData>().First();
                _mainGameSceneSetupData = Resources.FindObjectsOfTypeAll<MainGameSceneSetupData>().First();
                _resultsFlowCoordinator = Resources.FindObjectsOfTypeAll<ResultsFlowCoordinator>().First();

                if (_levelRequestNavigationController == null)
                {
                    _levelRequestNavigationController =
                        _ui.CreateViewController<LevelRequestNavigationController>("LevelRequestNavController");
                }

                if (_requestInfoViewController == null)
                {
                    _requestInfoViewController = _ui.CreateViewController<RequestInfoViewController>("RequestInfo");
                    _requestInfoViewController.rectTransform.anchorMin = new Vector2(0.3f, 0f);
                    _requestInfoViewController.rectTransform.anchorMax = new Vector2(0.7f, 1f);
                }

                _levelDifficultyViewController =
                    Resources.FindObjectsOfTypeAll<StandardLevelDifficultyViewController>().First();
                _levelDetailViewController =
                    Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First();
            }
            catch (Exception e)
            {
                _logger.Error("Unable to load UI components: " + e);
                return;
            }

            if (!_initialized)
            {
                DidFinishEvent += Finish;
                _levelRequestNavigationController.DidFinishEvent += HandleLevelRequestNavigationControllerDidfinish;
                _levelDifficultyViewController.didSelectDifficultyEvent +=
                    HandleDifficultyViewControllerDidSelectDifficulty;
                _levelDetailViewController.didPressPlayButtonEvent += HandleDetailViewControllerDidPressPlayButton;



                _requestInfoViewController.DownloadButtonpressed += HandleDidPressDownloadButton;
                _requestInfoViewController.SkipButtonPressed += HandleDidPressSkipButton;

                _initialized = true;
            }

            //_levelRequestNavigationController.Init();
            parentViewController.PresentModalViewController(_levelRequestNavigationController, null,
                StaticData.DidStartFromQueue);
            _requestInfoViewController.Init("Default Song Name", "Default User");

            _levelRequestNavigationController.PushViewController(_requestInfoViewController, true);

            if (!fromDebug) return; //Loading song preview arrests control from the results controller, causing it to display improperly. 
            CheckQueueAndUpdate();
        }

        private void Finish()
        {
            if (!_initialized) return;

            DidFinishEvent -= Finish;
            _levelRequestNavigationController.DidFinishEvent -= HandleLevelRequestNavigationControllerDidfinish;
            _levelDifficultyViewController.didSelectDifficultyEvent -=
                HandleDifficultyViewControllerDidSelectDifficulty;
            _levelDetailViewController.didPressPlayButtonEvent -= HandleDetailViewControllerDidPressPlayButton;
            _requestInfoViewController.DownloadButtonpressed -= HandleDidPressDownloadButton;
            _requestInfoViewController.SkipButtonPressed -= HandleDidPressSkipButton;

            _initialized = false;
        }

        private void HandleLevelRequestNavigationControllerDidfinish(LevelRequestNavigationController viewController)
        {
            viewController.DismissModalViewController(null);
            DidFinishEvent?.Invoke();
        }

        private void HandleDifficultyViewControllerDidSelectDifficulty(
            StandardLevelDifficultyViewController viewController,
            IStandardLevelDifficultyBeatmap difficultyLevel)
        {
            if (!_levelDetailViewController.isInViewControllerHierarchy)
            {
                _levelDetailViewController.Init(
                    _customLevel.GetDifficultyLevel(_levelDifficultyViewController.selectedDifficultyLevel.difficulty),
                    GameplayMode.SoloStandard, StandardLevelDetailViewController.LeftPanelViewControllerType.HowToPlay);
                _levelRequestNavigationController.PushViewController(_levelDetailViewController,
                    viewController.isRebuildingHierarchy);
            }
            else
                _levelDetailViewController.SetContent(
                    _customLevel.GetDifficultyLevel(_levelDifficultyViewController.selectedDifficultyLevel.difficulty),
                    GameplayMode.SoloStandard);
        }

        private void HandleDetailViewControllerDidPressPlayButton(StandardLevelDetailViewController viewController)
        {
            StaticData.LastLevelCompletionResults = null;
            StaticData.DidStartFromQueue = true;
            StaticData.LastLevelPlayed = viewController.difficultyLevel;
            StaticData.QueueList.RemoveAt(0);
            Finish();

            _mainGameSceneSetupData.Init(_levelDetailViewController.difficultyLevel, GameplayOptions.defaultOptions, GameplayMode.SoloStandard, 0f);
            _mainGameSceneSetupData.didFinishEvent += HandleMainGameSceneDidFinish;
            _mainGameSceneSetupData.TransitionToScene(0.7f);
        }

        private void HandleResultsFlowCoordinatorDidFinish()
        {
            _resultsFlowCoordinator.didFinishEvent -= HandleResultsFlowCoordinatorDidFinish;
            CheckQueueAndUpdate();
            _levelDetailViewController.RefreshContent();
        }

        private void HandleMainGameSceneDidFinish(MainGameSceneSetupData mainGameSceneSetupData, LevelCompletionResults levelCompletionResults)
        {
            StaticData.LastLevelCompletionResults = levelCompletionResults;
            mainGameSceneSetupData.didFinishEvent -= HandleMainGameSceneDidFinish;
            _menuSceneSetupData.TransitionToScene(0.7f);
        }

        private void HandleDidPressSkipButton()
        {
            _levelRequestNavigationController.ClearChildControllers();
            _requestInfoViewController.Init("Default song", "Default Requestor");
            _levelRequestNavigationController.PushViewController(_requestInfoViewController);
            StaticData.QueueList.RemoveAt(0);
            _song = (QueuedSong) StaticData.QueueList[0];
            CheckQueueAndUpdate();
        }

        private void HandleDidPressDownloadButton()
        {
            StartCoroutine(_requestInfoViewController.DownloadSongCoroutine(_song));
        }

        public void CheckQueueAndUpdate()
        {
            if (StaticData.QueueList.Count <= 0) return;
            _song = (QueuedSong) StaticData.QueueList[0];
            _requestInfoViewController.SetQueuedSong(_song);

            if (!_requestInfoViewController.DoesSongExist(_song))
            {
                _requestInfoViewController.SetDownloadButtonText("Download");
                _requestInfoViewController.SetDownloadState(true);
                return;
            }

            _requestInfoViewController.SetDownloadButtonText("Downloaded");
            _requestInfoViewController.SetDownloadState(false);

            _customLevel = SongLoader.CustomLevels.Find(x => x.levelID.Contains(_song.SongHash));

            SongLoader.Instance.LoadAudioClipForLevel(_customLevel, (level) =>
            {
                try
                {
                    var songPreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().First();
                    songPreviewPlayer.CrossfadeTo(_customLevel.audioClip, _customLevel.previewStartTime,
                        _customLevel.previewDuration);
                }
                catch (Exception e)
                {
                    _logger.Error("Unable to start song preview: " + e); // non critical
                }
            });

            if (!_levelDifficultyViewController.isInViewControllerHierarchy)
            {
                _levelDifficultyViewController.Init(_customLevel.difficultyBeatmaps, false);
                _levelRequestNavigationController.PushViewController(_levelDifficultyViewController);
            }
            else
                _levelDifficultyViewController.SetDifficultyLevels(_customLevel.difficultyBeatmaps,
                    _levelDifficultyViewController.selectedDifficultyLevel);
        }

        public static void OnLoad(string levelName)
        {
            if (levelName != "Menu") return;
            if (TwitchIntegrationUi.Instance.LevelRequestFlowCoordinator == null)
            {
                TwitchIntegrationUi.Instance.LevelRequestFlowCoordinator = new GameObject("Twitch Integration Coordinator").AddComponent<LevelRequestFlowCoordinator>();
            } 
                
            FindObjectOfType<LevelRequestFlowCoordinator>().OnMenuLoaded();
        }

        private void OnMenuLoaded()
        {
            //_logger.Debug("Called");
            if(_logger == null) _logger = LogManager.GetCurrentClassLogger();
            StartCoroutine(StaticData.DidStartFromQueue ? WaitForMenu() : WaitForResults());
        }

        //This essentially rebuilds the UI Hiearchy without using a unity scene. 
        public IEnumerator WaitForMenu()
        {
            _logger.Debug("Starting wait");
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<MainMenuViewController>().Any());
            _logger.Debug("Wait over~");

            
            VRUIViewController parent = FindObjectOfType<MainMenuViewController>();
            try
            {
                if (FindObjectOfType<SoloModeSelectionViewController>() != null)
                {
                    parent = FindObjectOfType<SoloModeSelectionViewController>();
                    _logger.Debug("Parent set");
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

            if (StaticData.QueueList.Count > 0 && StaticData.TwitchMode)
            {
                try
                {
                    Present(parent, false);
                    // ReSharper disable once InvertIf
                    if (StaticData.LastLevelCompletionResults != null)
                    {
                        ShowResults(_levelRequestNavigationController);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to find MainMenuViewController: " + ex);
                }
            }
            else
            {
                FindObjectOfType<StandardLevelSelectionFlowCoordinator>().Present(parent, FindObjectOfType<LevelCollectionsForGameplayModes>().GetLevels(GameplayMode.SoloStandard), GameplayMode.SoloStandard);
                // ReSharper disable once InvertIf
                if (StaticData.LastLevelCompletionResults != null)
                {
                    _logger.Debug("Presenting Results");
                    ShowResults(FindObjectOfType<StandardLevelSelectionNavigationController>());
                }
            }
            
        }

        private void ShowResults(VRUIViewController parentViewController)
        {
            if (_resultsFlowCoordinator == null)
            {
                _resultsFlowCoordinator = Resources.FindObjectsOfTypeAll<ResultsFlowCoordinator>().First();
            }
            _resultsFlowCoordinator.didFinishEvent += HandleResultsFlowCoordinatorDidFinish;
            _resultsFlowCoordinator.Present(parentViewController, StaticData.LastLevelCompletionResults, StaticData.LastLevelPlayed,
                GameplayOptions.defaultOptions, GameplayMode.SoloStandard);
            StaticData.LastLevelCompletionResults = null;
            StartCoroutine(WaitForResults());
        }

        public IEnumerator WaitForResults()
        {
            if (!StaticData.TwitchMode || StaticData.QueueList.Count <= 0) yield break;
            _logger.Debug("Waiting for contoller to init.");
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<ResultsViewController>().Any());
            var results = Resources.FindObjectsOfTypeAll<ResultsViewController>().First();

            results.continueButtonPressedEvent += delegate(ResultsViewController viewController)
            {
                try
                {
                    _logger.Debug("Results!");
                    viewController.DismissModalViewController(null, true);
                    StaticData.DidStartFromQueue = false;

                    FindObjectOfType<StandardLevelDetailViewController>().DismissModalViewController(null, true);
                    FindObjectOfType<StandardLevelDifficultyViewController>().DismissModalViewController(null, true);
                    FindObjectOfType<StandardLevelListViewController>().DismissModalViewController(null, true);
                    FindObjectOfType<StandardLevelSelectionNavigationController>().DismissModalViewController(null, true);
                    FindObjectOfType<SoloModeSelectionViewController>().DismissModalViewController(null, true);
                    FindObjectOfType<StandardLevelSelectionFlowCoordinator>().Finish();

                    Present(FindObjectOfType<MainMenuViewController>(), true);
                    StaticData.LastLevelCompletionResults = null;
                    StaticData.DidStartFromQueue = false;
                }
                catch (Exception e)
                {
                    _logger.Error($"RESULTS EXCEPTION: {e}");
                }
            };
        }
    }
}
