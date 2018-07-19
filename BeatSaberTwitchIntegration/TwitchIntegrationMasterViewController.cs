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

namespace TwitchIntegrationPlugin
{
    class TwitchIntegrationMasterViewController : VRUINavigationController
    {
        TwitchIntegrationUI ui;

        Button _skipButton;
        Button _nextButton;
        Button _backButton;
        TextMeshProUGUI _SongName;
        MainGameSceneSetupData _mainGameSceneSetupData;
        SongLoader loader = SongLoader.Instance;
        RequestQueueController _queueController;
        String customSongsPath;
        bool _doesSongExist;
        NLog.Logger logger;

        QueuedSong song;

        protected override void DidActivate()
        {
            ui = TwitchIntegrationUI._instance;
            logger = LogManager.GetCurrentClassLogger();

            try
            {
                _mainGameSceneSetupData = Resources.FindObjectsOfTypeAll<MainGameSceneSetupData>().First();
            }
            catch (Exception e)
            {
                logger.Error("Getting Song Controller Failed. \n" + e);
            }


            if (StaticData.songQueue.Count == 0)
            {
                DismissModalViewController(null, false);
            } 
            else
            {
                logger.Debug("Queue Menu Activated.");
                string docPath = "";
                song = StaticData.songQueue.Peek();
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

            if(_queueController == null)
            {
                _queueController = ui.CreateViewController<RequestQueueController>("Twitch Queue Controller");
                _queueController._parentMasterViewController = this;
            }
            screen.screenSystem.leftScreen.SetRootViewController(_queueController);
            screen.screenSystem.rightScreen.SetRootViewController(null);

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
                        logger.Debug("Starting to play song");
                        try
                        {
                            StaticData.songQueue.Dequeue();
                            CustomSongInfo _songInfo = SongLoader.CustomSongInfos.Find(x => x.songName == song._songName && x.authorName == song._authName);
                            _mainGameSceneSetupData.SetData(_songInfo.levelId, getHighestDiff(_songInfo), null, null, 0f, GameplayOptions.defaultOptions, GameplayMode.SoloStandard, null);
                            _mainGameSceneSetupData.TransitionToScene(0.7f);
                        }
                        catch(Exception e)
                        {
                            logger.Error(e);
                        }
                    }
                    else
                    {
                        StartCoroutine(DownloadSongCoroutine(song));
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
                    StaticData.songQueue.Dequeue();
                    song = StaticData.songQueue.Peek();

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
            base.DidActivate();
        }

        public IEnumerator DownloadSongCoroutine(QueuedSong songInfo)
        {

            ui.SetButtonText(ref _nextButton, "Downloading...");
            _nextButton.interactable = false;
            _skipButton.interactable = false;

            UnityWebRequest www = UnityWebRequest.Get(songInfo._downloadUrl);
            www.timeout = 10;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                logger.Error("error connecting to download.");
            }
            else
            {
                string zipPath = "";
                string docPath = "";
                string customSongsPath = "";
                try
                {
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
                    yield break;
                }

                FastZip zip = new FastZip();
                zip.ExtractZip(zipPath, customSongsPath, null);
                var subDir = Directory.GetDirectories(customSongsPath);
                try
                {
                    SongLoader.Instance.RefreshSongs();
                }
                catch(Exception e)
                {
                    logger.Error(e);
                }
                File.Delete(zipPath);

                _nextButton.interactable = true;
                _skipButton.interactable = true;
                ui.SetButtonText(ref _nextButton, "Play");
            }
        }

        public CustomLevelStaticData.Difficulty getHighestDiff(CustomSongInfo song)
        {
            logger.Debug("Getting diff");
            LevelStaticData.Difficulty highest = LevelStaticData.Difficulty.Normal;
            foreach(var difficulty in song.difficultyLevels)
            {
                LevelStaticData.Difficulty diff = (LevelStaticData.Difficulty)Enum.Parse(typeof(LevelStaticData.Difficulty), difficulty.difficulty);
                if(diff > highest)
                {
                    highest = diff;
                }
            }
            return highest;
        }

        public bool doesSongExist(QueuedSong song) 
        {
            try
            {
                if (SongLoader.CustomLevelStaticDatas.First(x => song._songName == x.songName && song._authName == x.authorName) != null)
                {
                    return true;
                }
                return false;
            } 
            catch(Exception e)
            {
                logger.Error(e);
                return false;
            }
        }
    }
}
