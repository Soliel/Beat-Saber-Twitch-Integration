using System;
using System.Collections;
using System.IO;
using System.Linq;
using NLog;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using TMPro;
using TwitchIntegrationPlugin.ICSharpCode.SharpZipLib.Zip;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using VRUI;

namespace TwitchIntegrationPlugin.UI
{
    public class LevelRequestMasterViewController : VRUINavigationController
    {
        private TwitchIntegrationUi _ui;

        private Button _skipButton;
        private Button _nextButton;
        private Button _backButton;
        private TextMeshProUGUI _songName;
        private bool _doesSongExist;
        private NLog.Logger _logger;

        private QueuedSong _song;
        private MenuSceneSetupData _menuSceneSetupData;



        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            //var ti = Resources.FindObjectsOfTypeAll<TwitchIntegration>().First();

            _ui = TwitchIntegrationUi.Instance;
            _logger = LogManager.GetCurrentClassLogger();

            try
            {
                //_mainGameSceneSetupData = Resources.FindObjectsOfTypeAll<MainGameSceneSetupData>().First();
                _menuSceneSetupData = Resources.FindObjectsOfTypeAll<MenuSceneSetupData>().First();
            }
            catch (Exception e)
            {
                _logger.Error("Getting Song Controller Failed. \n" + e);
            }


            if (StaticData.QueueList.Count == 0)
            {
                DismissModalViewController(null);
            } 
            else
            {
                _logger.Debug("Queue Menu Activated.");
                //string docPath = "";
                _song = (QueuedSong)StaticData.QueueList[0];
                /*docPath = Application.dataPath;
                docPath = docPath.Substring(0, docPath.Length - 5);
                docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                customSongsPath = docPath + "/CustomSongs/" + song._id + "/";*/
            }

            if (_songName == null)
            {
                _songName = _ui.CreateText(rectTransform, _song.BeatName, new Vector2(5f, -30f));
                _songName.fontSize = 8f;
                _songName.rectTransform.sizeDelta = new Vector2(80f, 20f);
            }

            if (_nextButton == null)
            {
                _nextButton = _ui.CreateUiButton(rectTransform, "ApplyButton");
                ((RectTransform) _nextButton.transform).anchoredPosition = new Vector2(-25f, 10f);
                ((RectTransform) _nextButton.transform).sizeDelta = new Vector2(25f, 10f);
                _ui.SetButtonText(ref _nextButton, DoesSongExist(_song) ? "Play" : "Download");

                _nextButton.onClick.AddListener(delegate
                {
                    _doesSongExist = DoesSongExist(_song);

                    _logger.Debug("CLICKED");
                    if (_doesSongExist) {
                        try
                        {
                            var songInfo = SongLoader.CustomLevels.Find(x => x.songName == _song.SongName &&
                                x.songAuthorName == _song.AuthName &&
                                x.songSubName == _song.SongSubName);

                            SongLoader.Instance.LoadAudioClipForLevel(songInfo, (level) =>
                            {
                                _logger.Debug("Starting to play song");
                                try
                                {
                                    StaticData.QueueList.RemoveAt(0);
                                    StaticData.DidStartFromQueue = true;

                                    _menuSceneSetupData.StartLevel(GetHighestDiff(songInfo), GameplayOptions.defaultOptions, GameplayMode.SoloStandard);
                                }
                                catch (Exception e)
                                {
                                    StaticData.DidStartFromQueue = false;
                                    _logger.Error(e);
                                }
                            });
                        }
                        catch(Exception ex)
                        {
                            _logger.Error(ex);
                        }
                    }
                    else
                    {
                        _logger.Debug("Starting Download");
                        StartCoroutine(DownloadSongCoroutine(_song));
                    }
                });
            }

            if (_skipButton == null)
            {
                _skipButton = _ui.CreateUiButton(rectTransform, "ApplyButton");
                ((RectTransform) _skipButton.transform).anchoredPosition = new Vector2(5f, 10f);
                ((RectTransform) _skipButton.transform).sizeDelta = new Vector2(25f, 10f);
                _ui.SetButtonText(ref _skipButton, "Skip Song");

                _skipButton.onClick.AddListener(delegate 
                {
                    StaticData.QueueList.RemoveAt(0);
                    _song = (QueuedSong)StaticData.QueueList[0];

                    _songName.SetText(_song.BeatName);
                    _ui.SetButtonText(ref _nextButton, (DoesSongExist(_song)) ? "Play" : "Download");
                });
            }

            if (_backButton == null)
            {
                _backButton = _ui.CreateBackButton(rectTransform);
                _backButton.onClick.AddListener(delegate 
                {
                    DismissModalViewController(null);
                });
            }
            base.DidActivate(false, ActivationType.AddedToHierarchy);
        }

        public IEnumerator DownloadSongCoroutine(QueuedSong songInfo)
        {

            _ui.SetButtonText(ref _nextButton, "Downloading...");
            _nextButton.interactable = false;
            _skipButton.interactable = false;

            _logger.Debug("Web Request sent to: " + songInfo.DownloadUrl);
            UnityWebRequest www = UnityWebRequest.Get(songInfo.DownloadUrl);

            bool timeout = false;
            float time = 0f;

            UnityWebRequestAsyncOperation asyncRequest = www.SendWebRequest();

            while (!asyncRequest.isDone || asyncRequest.progress < 1f)
            {
                yield return null;

                time += Time.deltaTime;

                if (time >= 15f && asyncRequest.progress == 0f)
                {
                    www.Abort();
                    timeout = true;
                }

                if (www.isNetworkError || www.isHttpError || timeout)
                {
    
                    _skipButton.interactable = true;
                    _nextButton.interactable = true;
                }
                else
                {
                    var zipPath = "";
                    var customSongsPath = "";
                    try
                    {
                        _logger.Debug("Download complete.");
                        var data = www.downloadHandler.data;


                        var docPath = Application.dataPath;
                        docPath = docPath.Substring(0, docPath.Length - 5);
                        docPath = docPath.Substring(0, docPath.LastIndexOf("/", StringComparison.Ordinal));
                        customSongsPath = docPath + "/CustomSongs/" + songInfo.Id + "/";
                        zipPath = customSongsPath + songInfo.Id + ".zip";
                        if (!Directory.Exists(customSongsPath))
                        {
                            Directory.CreateDirectory(customSongsPath);
                        }
                        File.WriteAllBytes(zipPath, data);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e);
                        _nextButton.interactable = true;
                        _skipButton.interactable = true;
                    }

                    var zip = new FastZip();
                    zip.ExtractZip(zipPath, customSongsPath, null);
                    //var subDir = Directory.GetDirectories(customSongsPath);
                    try
                    {
                        SongLoader.Instance.RefreshSongs();
                        SongLoader.SongsLoadedEvent += (songLoader, list) => { _nextButton.interactable = true; };
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e);
                    }
                    File.Delete(zipPath);

                    _skipButton.interactable = true;
                    _ui.SetButtonText(ref _nextButton, "Play");
                }
            }
        }

        public IStandardLevelDifficultyBeatmap GetHighestDiff(CustomLevel song)
        {
            _logger.Debug("Getting diff");

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

        public bool DoesSongExist(QueuedSong song)
        {
            try
            {
                return SongLoader.CustomLevels.FirstOrDefault(x => x.songName == song.SongName &&
                                                                   x.songAuthorName == song.AuthName &&
                                                                   x.beatsPerMinute == song.Bpm &&
                                                                   x.songSubName == song.SongSubName) != null;
            }
            catch (Exception e)
            {
                _logger.Debug(e); //Not a fatal error.
                return false;
            }
        }
    }
}
