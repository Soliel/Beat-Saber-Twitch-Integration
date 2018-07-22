using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using TMPro;
using System.Collections;
using System.IO;
using SongLoaderPlugin;
using UnityEngine.Networking;
using NLog;
using ICSharpCode.SharpZipLib.Zip;
using SongLoaderPlugin.OverrideClasses;

namespace TwitchIntegrationPlugin
{
    class TwitchIntegrationMasterViewController : VRUIViewController
    {
        TwitchIntegrationUI ui;

        Button _skipButton;
        Button _nextButton;
        Button _backButton;
        TextMeshProUGUI _SongName;
        MainGameSceneSetupData _mainGameSceneSetupData;
        SongLoader loader = SongLoader.Instance;
        //RequestQueueController _queueController;
        String customSongsPath;
        bool _doesSongExist;
        NLog.Logger logger;

        QueuedSong song;
        private MenuSceneSetupData _menuSceneSetupData;


        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            TwitchIntegration ti = Resources.FindObjectsOfTypeAll<TwitchIntegration>().First();

            ui = TwitchIntegrationUI._instance;
            logger = LogManager.GetCurrentClassLogger();

            try
            {
                _mainGameSceneSetupData = Resources.FindObjectsOfTypeAll<MainGameSceneSetupData>().First();
                _menuSceneSetupData = Resources.FindObjectsOfTypeAll<MenuSceneSetupData>().First();
            }
            catch (Exception e)
            {
                logger.Error("Getting Song Controller Failed. \n" + e);
            }


            if (StaticData.queueList.Count == 0)
            {
                DismissModalViewController(null, false);
            } 
            else
            {
                logger.Debug("Queue Menu Activated.");
                string docPath = "";
                song = (QueuedSong)StaticData.queueList[0];
                docPath = Application.dataPath;
                docPath = docPath.Substring(0, docPath.Length - 5);
                docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                customSongsPath = docPath + "/CustomSongs/" + song._id + "/";
            }

            if (_SongName == null)
            {
                _SongName = ui.CreateText(rectTransform, song._beatName, new Vector2(5f, -30f));
                _SongName.fontSize = 8f;
                _SongName.rectTransform.sizeDelta = new Vector2(80f, 20f);
            }

            /*if(_queueController == null)
            {
                _queueController = ui.CreateViewController<RequestQueueController>("Twitch Queue Controller");
                _queueController._parentMasterViewController = this;
            }
            screen.screenSystem.leftScreen.SetRootViewController(_queueController);
            screen.screenSystem.rightScreen.SetRootViewController(null);*/

            if (_nextButton == null)
            {
                _nextButton = ui.CreateUIButton(rectTransform, "ApplyButton");
                (_nextButton.transform as RectTransform).anchoredPosition = new Vector2(-25f, 10f);
                (_nextButton.transform as RectTransform).sizeDelta = new Vector2(25f, 10f);
                ui.SetButtonText(ref _nextButton, (doesSongExist(song)) ? "Play" : "Download");

                _nextButton.onClick.AddListener(delegate ()
                {
                    _doesSongExist = (doesSongExist(song)) ? true : false;

                    logger.Debug("CLICKED");
                    if (_doesSongExist) {
                        CustomLevel _songInfo = SongLoader.CustomLevels.Find(x => x.songName == song._songName && x.songAuthorName == song._authName);
                        SongLoader.Instance.LoadAudioClipForLevel(_songInfo, (CustomLevel level) =>
                        {
                            logger.Debug("Starting to play song");
                            try
                            {
                                StaticData.queueList.RemoveAt(0);
                                StaticData.didStartFromQueue = true;
                                _menuSceneSetupData.StartLevel(getHighestDiff(_songInfo), GameplayOptions.defaultOptions, GameplayMode.SoloStandard);
                            }
                            catch (Exception e)
                            {
                                StaticData.didStartFromQueue = false;
                                logger.Error(e);
                            }
                        });
                    }
                    else
                    {
                        logger.Debug("Starting Download");
                        DownloadSongCoroutine(song);
                    }
                });
            }

            if (_skipButton == null)
            {
                _skipButton = ui.CreateUIButton(rectTransform, "ApplyButton");
                (_skipButton.transform as RectTransform).anchoredPosition = new Vector2(5f, 10f);
                (_skipButton.transform as RectTransform).sizeDelta = new Vector2(25f, 10f);
                ui.SetButtonText(ref _skipButton, "Skip Song");

                _skipButton.onClick.AddListener(delegate ()
                {
                    StaticData.queueList.RemoveAt(0);
                    song = (QueuedSong)StaticData.queueList[0];

                    String docPath = "";
                    docPath = Application.dataPath;
                    docPath = docPath.Substring(0, docPath.Length - 5);
                    docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                    customSongsPath = docPath + "/CustomSongs/" + song._id + "/";

                    _SongName.SetText(song._beatName);
                    ui.SetButtonText(ref _nextButton, (doesSongExist(song)) ? "Play" : "Download");
                });
            }

            if (_backButton == null)
            {
                _backButton = ui.CreateBackButton(rectTransform);
                _backButton.onClick.AddListener(delegate ()
                {
                    DismissModalViewController(null, false);
                });
            }
            base.DidActivate(false, ActivationType.AddedToHierarchy);
        }

        /*private void HandleLevelDidFinish(MainGameSceneSetupData mainGameSceneSetupData, LevelCompletionResults levelCompletionResults)
        {
            this.levelCompletionResults = levelCompletionResults;
            mainGameSceneSetupData.didFinishEvent -= HandleLevelDidFinish;
            _menuSceneSetupData.TransitionToScene((levelCompletionResults == null) ? 0.35f : 1.3f);
        }*/

        public void DownloadSongCoroutine(QueuedSong songInfo)
        {

            ui.SetButtonText(ref _nextButton, "Downloading...");
            _nextButton.interactable = false;
            _skipButton.interactable = false;

            logger.Debug("Web Request sent to: " + songInfo._downloadUrl);
            UnityWebRequest www = UnityWebRequest.Get(songInfo._downloadUrl);
            www.timeout = 2;
            www.SendWebRequest().completed += (AsyncOperation asyncOp) => {

                if (www.isNetworkError || www.isHttpError || www.error != null)
                {
                    logger.Error("error connecting to download: " + www.error);
                }
                else
                {
                    string zipPath = "";
                    string docPath = "";
                    string customSongsPath = "";
                    try
                    {
                        logger.Debug("Download complete.");
                        byte[] data = www.downloadHandler.data;


                        docPath = Application.dataPath;
                        docPath = docPath.Substring(0, docPath.Length - 5);
                        docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                        customSongsPath = docPath + "/CustomSongs/" + songInfo._id + "/";
                        zipPath = customSongsPath + songInfo._beatName + ".zip";
                        if (!Directory.Exists(customSongsPath))
                        {
                            Directory.CreateDirectory(customSongsPath);
                        }
                        File.WriteAllBytes(zipPath, data);
                    }
                    catch (Exception e)
                    {
                        logger.Error(e);
                    }

                    FastZip zip = new FastZip();
                    zip.ExtractZip(zipPath, customSongsPath, null);
                    var subDir = Directory.GetDirectories(customSongsPath);
                    try
                    {
                        SongLoader.Instance.RefreshSongs();
                    }
                    catch (Exception e)
                    {
                        logger.Error(e);
                    }
                    File.Delete(zipPath);

                    _nextButton.interactable = true;
                    _skipButton.interactable = true;
                    ui.SetButtonText(ref _nextButton, "Play");
                }
            };
        }

        public IStandardLevelDifficultyBeatmap getHighestDiff(CustomLevel song)
        {
            logger.Debug("Getting diff");

            var highest = LevelDifficulty.Easy;
            IStandardLevelDifficultyBeatmap result = null;
            foreach(var difficulty in song.difficultyBeatmaps)
            {
                var diff = difficulty.difficulty;
                if(diff > highest)
                {
                    highest = diff;
                    result = difficulty;
                }
            }
            return result;
        }

        public bool doesSongExist(QueuedSong song) 
        {
            try
            {
                if (SongLoader.CustomLevels.First(x => x.songName == song._songName && x.songAuthorName == song._authName) != null)
                {
                    return true;
                }
                return false;
            } 
            catch(Exception e)
            {
                logger.Debug(e); //Not a fatal error.
                return false;
            }
        }
    }
}
