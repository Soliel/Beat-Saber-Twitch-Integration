
using System;
using System.Linq;
using NLog;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using TwitchIntegrationPlugin.Serializables;
using UnityEngine;
using VRUI;

namespace TwitchIntegrationPlugin.UI
{
    public class LevelRequestFlowCoordinatorNew : FlowCoordinator
    {
        //private TwitchIntegrationUi _ui;
        private QueuedSong _song;
        private LevelSO _customLevel;
        private NLog.Logger _logger;
        public event Action<LevelRequestFlowCoordinatorNew> DidFinishEvent;


        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
                title = "Twitch Integration";
            if (activationType != ActivationType.AddedToHierarchy)
                return;
            if (TwitchIntegrationUi.RequestInfoViewController == null)
                TwitchIntegrationUi.FindUIComponents();

            _logger = LogManager.GetCurrentClassLogger();

            TwitchIntegrationUi.NavigationController.didFinishEvent += HandleNavigationControllerDidFinish;
            TwitchIntegrationUi.DifficultyViewController.didSelectDifficultyEvent +=
                HandleDifficultyViewControllerDidSelectDifficulty;
            TwitchIntegrationUi.LevelDetailViewController.didPressPlayButtonEvent +=
                HandleLevelDetailViewControllerDidPressPlay;
            TwitchIntegrationUi.LevelDetailViewController.didPressPracticeButtonEvent +=
                HandleLevelDetailViewControllerDidPressPracticeButton;
            TwitchIntegrationUi.PracticeViewController.didFinishEvent += HandlePracticeViewControllerDidFinish;
            TwitchIntegrationUi.PracticeViewController.didPressPlayButtonEvent +=
                HandlePracticeViewControllerDidPressPlayButton;
            TwitchIntegrationUi.ResultsViewController.continueButtonPressedEvent +=
                HandleResultsViewControllerContinueButtonPressed;
            TwitchIntegrationUi.ResultsViewController.restartButtonPressedEvent +=
                HandleResultsViewControllerRestartButtonPressed;

            PlayerDataModelSO.LocalPlayer currentPlayer = TwitchIntegrationUi.PlayerDataModel.currentLocalPlayer;
            TwitchIntegrationUi.GameplaySetupViewController.Init(currentPlayer.playerSpecificSettings, currentPlayer.gameplayModifiers);
            SetViewControllerToNavigationConctroller(TwitchIntegrationUi.NavigationController, TwitchIntegrationUi.RequestInfoViewController);
            ProvideInitialViewControllers(TwitchIntegrationUi.NavigationController, TwitchIntegrationUi.GameplaySetupViewController, null);
        }

        public virtual void HandleNavigationControllerDidFinish(DismissableNavigationController viewController)
        {
            DidFinishEvent?.Invoke(this);
        }

        private  void StartLevel(System.Action beforeSceneSwitchCallback, bool practice)
        {
            IDifficultyBeatmap difficultyBeatmap = TwitchIntegrationUi.LevelDetailViewController.difficultyBeatmap;
            PlayerSpecificSettings playerSettings = TwitchIntegrationUi.GameplaySetupViewController.playerSettings;
            GameplayModifiers gameplayModifiers = new GameplayModifiers(TwitchIntegrationUi.GameplaySetupViewController.gameplayModifiers);
            PracticeSettings practiceSettings = !practice
                ? null
                : TwitchIntegrationUi.PlayerDataModel.sharedPracticeSettings;
            TwitchIntegrationUi.MenuSceneSetupData.StartStandardLevel(difficultyBeatmap, gameplayModifiers, playerSettings, practiceSettings, beforeSceneSwitchCallback, HandleRequestLevelDidFinish);
        }

        private void HandleRequestLevelDidFinish(StandardLevelSceneSetupDataSO standardLevelSceneSetupData, LevelCompletionResults levelCompletionResults)
        {
            bool prevGameWasPractice = TwitchIntegrationUi.PracticeViewController.isInViewControllerHierarchy;
            if (!prevGameWasPractice)
                TwitchIntegrationUi.PlayerDataModel.currentLocalPlayer.playerAllOverallStatsData.soloFreePlayOverallStatsData.UpdateWithLevelCompletionResults(levelCompletionResults);
            TwitchIntegrationUi.PlayerDataModel.Save();
            if (levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.Restart)
            {
                StartLevel(null, prevGameWasPractice);
            }
            else
            {
                if (levelCompletionResults.levelEndStateType != LevelCompletionResults.LevelEndStateType.Failed && levelCompletionResults.levelEndStateType != LevelCompletionResults.LevelEndStateType.Cleared)
                    return;
                ProcessScore(levelCompletionResults, prevGameWasPractice);
                IDifficultyBeatmap difficultyBeatmap = TwitchIntegrationUi.LevelDetailViewController.difficultyBeatmap;
                TwitchIntegrationUi.ResultsViewController.Init(levelCompletionResults, difficultyBeatmap);
                PresentViewController(TwitchIntegrationUi.ResultsViewController, null, true);
            }
        }

        public virtual void ProcessScore(LevelCompletionResults levelCompletionResults, bool practice)
        {
            PlayerDataModelSO.LocalPlayer currentLocalPlayer = TwitchIntegrationUi.PlayerDataModel.currentLocalPlayer;
            IDifficultyBeatmap difficultyBeatmap = TwitchIntegrationUi.LevelDetailViewController.difficultyBeatmap;
            GameplayModifiers gameplayModifiers = TwitchIntegrationUi.GameplaySetupViewController.gameplayModifiers;
            bool flag = levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared;
            string levelId = difficultyBeatmap.level.levelID;
            BeatmapDifficulty difficulty = difficultyBeatmap.difficulty;
            PlayerLevelStatsData playerLevelStatsData = currentLocalPlayer.GetPlayerLevelStatsData(levelId, difficulty);
            playerLevelStatsData.IncreaseNumberOfGameplays();
            if (flag)
                playerLevelStatsData.UpdateScoreData(levelCompletionResults.score, levelCompletionResults.maxCombo, levelCompletionResults.fullCombo, levelCompletionResults.rank);
            if (!flag || practice)
                return;
            TwitchIntegrationUi.PlatformLeaderboardsModel.AddScore(difficultyBeatmap, levelCompletionResults.unmodifiedScore, gameplayModifiers);
        }

        public void CheckQueueAndUpdate()
        {
            if (!StaticData.SongQueue.DoesQueueHaveSongs()) return;
            _song = StaticData.SongQueue.PeekQueuedSong();
            TwitchIntegrationUi.RequestInfoViewController.SetQueuedSong(_song);

            if (!TwitchIntegrationUi.RequestInfoViewController.DoesSongExist(_song))
            {
                TwitchIntegrationUi.RequestInfoViewController.SetDownloadButtonText("Download");
                TwitchIntegrationUi.RequestInfoViewController.SetDownloadState(true);
                return;
            }

            TwitchIntegrationUi.RequestInfoViewController.SetDownloadButtonText("Downloaded");
            TwitchIntegrationUi.RequestInfoViewController.SetDownloadState(false);
            _customLevel = SongLoader.CustomLevels.Find(x => x.levelID.Contains(_song.SongHash));
            CustomLevel customLevelCl = (CustomLevel)_customLevel;

            SongLoader.Instance.LoadAudioClipForLevel(customLevelCl, (level) =>
            {
                try
                {
                    SongPreviewPlayer songPreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().First();
                    songPreviewPlayer.CrossfadeTo(customLevelCl.audioClip, _customLevel.previewStartTime,
                        _customLevel.previewDuration);
                }
                catch (Exception e)
                {
                    _logger.Error("Unable to start song preview: " + e); // non critical
                }
            });

            if (!TwitchIntegrationUi.DifficultyViewController.isInViewControllerHierarchy)
            {
                TwitchIntegrationUi.DifficultyViewController.Init(_customLevel.difficultyBeatmaps);
                PushViewControllerToNavigationController(TwitchIntegrationUi.NavigationController, TwitchIntegrationUi.DifficultyViewController);
            }
            else
                TwitchIntegrationUi.DifficultyViewController.SetDifficultyBeatmaps(_customLevel.difficultyBeatmaps,
                    TwitchIntegrationUi.DifficultyViewController.selectedDifficultyBeatmap);
        }

        private void HandleDifficultyViewControllerDidSelectDifficulty(BeatmapDifficultyViewController viewController,
            IDifficultyBeatmap difficultyBeatmap)
        {
            TwitchIntegrationUi.PlatformLeaderboardViewController.SetData(difficultyBeatmap);
            SetRightScreenViewController(TwitchIntegrationUi.PlatformLeaderboardViewController);
            if (!TwitchIntegrationUi.LevelDetailViewController.isInViewControllerHierarchy)
            {
                TwitchIntegrationUi.LevelDetailViewController.Init(difficultyBeatmap, TwitchIntegrationUi.PlayerDataModel.currentLocalPlayer, true);
                PushViewControllerToNavigationController(TwitchIntegrationUi.NavigationController, TwitchIntegrationUi.LevelDetailViewController);
            }
            else
            {
                TwitchIntegrationUi.LevelDetailViewController.SetContent(difficultyBeatmap, TwitchIntegrationUi.PlayerDataModel.currentLocalPlayer, true);
            }
        }

        private void HandleLevelDetailViewControllerDidPressPlay(StandardLevelDetailViewController viewController)
        {
            StartLevel(null, true);
        }

        private void HandleLevelDetailViewControllerDidPressPracticeButton(StandardLevelDetailViewController viewController)
        {
            TwitchIntegrationUi.PracticeViewController.Init(_customLevel, TwitchIntegrationUi.PlayerDataModel.sharedPracticeSettings);
            PresentViewController(TwitchIntegrationUi.PracticeViewController);
        }

        private void HandlePracticeViewControllerDidPressPlayButton()
        {
            StartLevel(null, true);
        }

        private void HandlePracticeViewControllerDidFinish(PracticeViewController viewController)
        {
            DismissViewController(viewController);
        }

        private void HandleResultsViewControllerContinueButtonPressed(VRUIViewController viewController)
        {
            SetLeftScreenViewController(TwitchIntegrationUi.GameplaySetupViewController);
            CheckQueueAndUpdate();
            DismissViewController(viewController);
        }

        private void HandleResultsViewControllerRestartButtonPressed(VRUIViewController viewController)
        {
            StartLevel(() =>
            {
                SetLeftScreenViewController(TwitchIntegrationUi.GameplaySetupViewController, true);
                DismissViewController(viewController, null, true);
            }, TwitchIntegrationUi.PracticeViewController.isInViewControllerHierarchy);
        }

        private void HandleDidPressDownloadButton()
        {
            StartCoroutine(TwitchIntegrationUi.RequestInfoViewController.DownloadSongCoroutine(_song));
        }

        private void HandleDidPressSkipButton()
        {
            TwitchIntegrationUi.RequestInfoViewController.Init("Default song", "Default Requestor");
            PushViewControllerToNavigationController(TwitchIntegrationUi.NavigationController, TwitchIntegrationUi.RequestInfoViewController);
            StaticData.SongQueue.PopQueuedSong();
            CheckQueueAndUpdate();
        }
    }
}
