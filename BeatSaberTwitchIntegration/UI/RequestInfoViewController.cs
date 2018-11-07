using System;
using System.Collections;
using System.Collections.Generic;
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
using Logger = NLog.Logger;

namespace TwitchIntegrationPlugin.UI
{
    public class RequestInfoViewController : VRUIViewController
    {

        private bool _initialized;

        private TwitchIntegrationUi _ui;
        private TextMeshProUGUI _songName;
        private TextMeshProUGUI _requestorName;
        private Button _downloadButton;
        private Button _skipButton;
        private Logger _logger;
        private string _songTitle;
        private string _requestorNameString;
        private QueuedSong _queuedSong;

        public event Action SkipButtonPressed;
        public event Action DownloadButtonpressed;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (_songName == null)
            {
                _songName = _ui.CreateText(rectTransform, _songTitle, new Vector2(0f, 0f));
                _songName.fontSize = 6f;
                _songName.rectTransform.anchorMin = new Vector2(0.0f, 0.7f);
                _songName.rectTransform.anchorMax = new Vector2(1f, 0.6f);
            }
            else
                _songName.SetText(_songTitle);

            if (_requestorName == null)
            {
                _requestorName = _ui.CreateText(rectTransform, "Requested By:\n" + _requestorNameString, new Vector2(0f, 0f));
                _requestorName.fontSize = 4f;
                _requestorName.rectTransform.anchorMin = new Vector2(0.2f, 0.4f);
                _requestorName.rectTransform.anchorMax = new Vector2(0.8f, 0.5f);

            }
            else
                _requestorName.SetText("Requested By:\n" + _requestorNameString);

            if (_skipButton == null)
            {
                _skipButton = _ui.CreateUiButton(rectTransform, "ApplyButton");
                ((RectTransform)_skipButton.transform).anchorMin = new Vector2(0.6f, 0.1f);
                ((RectTransform)_skipButton.transform).anchorMax = new Vector2(1f, 0.2f);
                ((RectTransform)_skipButton.transform).sizeDelta = new Vector2(0f, 0f);
                ((RectTransform)_skipButton.transform).anchoredPosition = new Vector2(-5f, 0f);
                _ui.SetButtonText(ref _skipButton, "Skip");
                _skipButton.onClick.AddListener(OnSkipButtonPressed);
            }

            if (_downloadButton == null)
            {
                _downloadButton = _ui.CreateUiButton(rectTransform, "ApplyButton");
                ((RectTransform)_downloadButton.transform).anchorMin = new Vector2(0f, 0.1f);
                ((RectTransform)_downloadButton.transform).anchorMax = new Vector2(0.4f, 0.2f);
                ((RectTransform)_downloadButton.transform).sizeDelta = new Vector2(0f, 0f);
                ((RectTransform)_downloadButton.transform).anchoredPosition = new Vector2(5f, 0f);
                _ui.SetButtonText(ref _downloadButton, "Download");
                _downloadButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = _downloadButton.GetComponentInChildren<TextMeshProUGUI>().fontSize - 0.3f;
                _downloadButton.onClick.AddListener(OnDownloadButtonPressed);
            }

        }

        public void Init(string songName, string requestorName)
        {
            _ui = TwitchIntegrationUi.Instance;
            _logger = LogManager.GetCurrentClassLogger();
            _songTitle = songName;
            _requestorNameString = requestorName;
            

            _initialized = true;
        }

        public void SetQueuedSong(QueuedSong song)
        {
            if (!_initialized) return;
            _songName.SetText(song.BeatName);
            _requestorName.SetText("Requested By:\n" + song.RequestedBy);
        }

        private void OnSkipButtonPressed()
        {
            SkipButtonPressed?.Invoke();
        }

        private void OnDownloadButtonPressed()
        {
            DownloadButtonpressed?.Invoke();
        }

        public IEnumerator DownloadSongCoroutine(QueuedSong song)
        {
            _ui.SetButtonText(ref _downloadButton, "Downloading...");
            _downloadButton.interactable = false;
            _skipButton.interactable = false;

            _logger.Debug("Web Request sent to: " + song.DownloadUrl);
            var www = UnityWebRequest.Get(song.DownloadUrl);

            var timeout = false;
            var time = 0f;

            var asyncRequest = www.SendWebRequest();

            while (!asyncRequest.isDone || asyncRequest.progress < 1f)
            {
                yield return null;

                time += Time.deltaTime;

                if (!(time >= 15f) || asyncRequest.progress != 0f) continue;
                www.Abort();
                timeout = true;
            }

            if (www.isNetworkError || www.isHttpError || timeout)
            {
                www.Abort();
                _skipButton.interactable = true;
                _downloadButton.interactable = true;
                _ui.SetButtonText(ref _downloadButton, "Download");
            }
            else
            {
                var zipPath = "";
                var customSongsPath = "";
                try
                {
                    _logger.Debug("Download complete in: " + time);
                    var data = www.downloadHandler.data;

                    var docPath = Application.dataPath;
                    docPath = docPath.Substring(0, docPath.Length - 5);
                    docPath = docPath.Substring(0, docPath.LastIndexOf("/", StringComparison.Ordinal));
                    customSongsPath = docPath + "/CustomSongs/" + song.Id + "/";
                    zipPath = customSongsPath + song.Id + ".zip";
                    if (!Directory.Exists(customSongsPath))
                    {
                        Directory.CreateDirectory(customSongsPath);
                    }
                    File.WriteAllBytes(zipPath, data);
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                    _skipButton.interactable = true;
                    _downloadButton.interactable = true;
                    _ui.SetButtonText(ref _downloadButton, "Download");
                }

                var zip = new FastZip();
                zip.ExtractZip(zipPath, customSongsPath, null);
                try
                {
                    SongLoader.Instance.RefreshSongs();
                    _queuedSong = song;
                    SongLoader.SongsLoadedEvent += OnSongsLoaded;
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
                File.Delete(zipPath);
                _skipButton.interactable = true;
            }
        }

        private void OnSongsLoaded(SongLoader songLoader, List<CustomLevel> list)
        {
            SongLoader.SongsLoadedEvent -= OnSongsLoaded;
            _downloadButton.interactable = true;
            if (DoesSongExist(_queuedSong))
            {
                _ui.SetButtonText(ref _downloadButton, "Downloaded");
            }
            else
                _ui.SetButtonText(ref _downloadButton, "Download");

            _queuedSong = null;
            FindObjectOfType<LevelRequestFlowCoordinator>().CheckQueueAndUpdate(); //This kinda goes against the purpose of a flow controller, but I just want it to work.
        }

        public void SetDownloadButtonText(string text)
        {
            _ui.SetButtonText(ref _downloadButton, text);
        }

        public void SetDownloadState(bool state)
        {
            _downloadButton.interactable = state;
        }

        public void SetSkipState(bool state)
        {
            _skipButton.interactable = state;
        }

        public bool DoesSongExist(QueuedSong song)
        {
            try
            {
                return SongLoader.CustomLevels.FirstOrDefault(x => x.levelID.Contains(song.SongHash)) != null;
            }
            catch (Exception e)
            {
                _logger.Debug(e); //Not a fatal error.
                return false;
            }
        }
    }
}
