using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using Image = UnityEngine.UI.Image;
using HMUI;
using System.Collections;

namespace TwitchIntegrationPlugin
{
    class TwitchIntegrationUI : MonoBehaviour
    {
        private RectTransform _mainMenuRectTransform;
        public  MainMenuViewController _mainMenuViewController;

        private Button _buttonInstance;
        private Button _backButtonInstance;
        private GameObject _loadingIndicatorInstance;

        public static TwitchIntegrationUI _instance;

        public static List<Sprite> icons = new List<Sprite>();

        public TwitchIntegrationMasterViewController _twitchIntegrationViewController;

        static public Dictionary<string, Sprite> _cachedSprites = new Dictionary<string, Sprite>();

        NLog.Logger logger;


        internal static void OnLoad()
        {
            if(_instance != null)
            {
                return;
            }
            new GameObject("TwitchIntegration UI").AddComponent<TwitchIntegrationUI>();
        }

        private void Awake()
        {
            logger = NLog.LogManager.GetCurrentClassLogger();
            _instance = this;
            
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                icons.Add(sprite);
            }

            try
            {
                _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "QuitButton"));
                _backButtonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "BackArrowButton"));
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _mainMenuRectTransform = _buttonInstance.transform.parent as RectTransform;
                _loadingIndicatorInstance = Resources.FindObjectsOfTypeAll<GameObject>().Where(x => x.name == "LoadingIndicator").First();
            }
            catch (Exception e)
            {
                logger.Error(e);
            }

            try
            {
                CreateTwitchModeButton();
                CreateDebugButton();
            }
            catch (Exception e)
            {
                logger.Error(e);
            }

        }

        private void CreateTwitchModeButton()
        {
            Button _twitchModeButton = CreateUIButton(_mainMenuRectTransform, "QuitButton");

            try
            {
                (_twitchModeButton.transform as RectTransform).anchoredPosition = new Vector2(4f, 49f);
                (_twitchModeButton.transform as RectTransform).sizeDelta = new Vector2(25f, 25f);

                SetButtonText(ref _twitchModeButton, (StaticData.TwitchMode) ? "Twitch Mode: ON" : "Twitch Mode: OFF");
                //SetButtonIcon(ref _twitchModeButton, icons.First(x => (x.name == "SettingsIcon")));

                _twitchModeButton.onClick.AddListener(delegate ()
                {
                    StaticData.TwitchMode = !StaticData.TwitchMode;
                    if (StaticData.TwitchMode)
                    {
                        SetButtonText(ref _twitchModeButton, "Twitch Mode: ON");
                    } else
                    {
                        SetButtonText(ref _twitchModeButton, "Twitch Mode: OFF");
                    }
                    

                });
            } catch(Exception e)
            {
                logger.Error(e);
            }
        }

        private void CreateDebugButton()
        {
            Button _debugButton = CreateUIButton(_mainMenuRectTransform, "QuitButton");

            try
            {
                (_debugButton.transform as RectTransform).anchoredPosition = new Vector2(4f, 38f);
                (_debugButton.transform as RectTransform).sizeDelta = new Vector2(25f, 10f);

                SetButtonText(ref _debugButton, "Twitch Debug");

                //SetButtonIcon(ref _debugButton, icons.First(x => (x.name == "SettingsIcon")));

                _debugButton.onClick.AddListener(delegate ()
                {
                    try
                    {
                        if (_twitchIntegrationViewController == null)
                        {
                            _twitchIntegrationViewController = CreateViewController<TwitchIntegrationMasterViewController>("Twitch Panel");
                        }
                        _mainMenuViewController.PresentModalViewController(_twitchIntegrationViewController, null, false);
                    }
                    catch(Exception e)
                    {
                        logger.Error(e);
                    }
                });
            }
            catch (Exception e)
            {
                logger.Error(e);
            }
        }

        public Button CreateBackButton(RectTransform parent)
        {
            if (_backButtonInstance == null)
            {
                return null;
            }

            Button _button = Instantiate(_backButtonInstance, parent, false);
            DestroyImmediate(_button.GetComponent<GameEventOnUIButtonClick>());
            _button.onClick = new Button.ButtonClickedEvent();

            return _button;
        }

        public GameObject CreateLoadingIndicator(Transform parent)
        {
            GameObject indicator = Instantiate(_loadingIndicatorInstance, parent, false);
            return indicator;
        }

        public Button CreateUIButton(RectTransform parent, string buttonTemplate)
        {
            if (_buttonInstance == null)
            {
                return null;
            }

            Button btn = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == buttonTemplate)), parent, false);
            DestroyImmediate(btn.GetComponent<GameEventOnUIButtonClick>());
            btn.onClick = new Button.ButtonClickedEvent();

            return btn;
        }

        public T CreateViewController<T>(String name) where T : VRUIViewController
        {
            T vc = new GameObject(name).AddComponent<T>();

            vc.rectTransform.anchorMin = new Vector2(0f, 0f);
            vc.rectTransform.anchorMax = new Vector2(1f, 1f);
            vc.rectTransform.sizeDelta = new Vector2(0f, 0f);
            vc.rectTransform.anchoredPosition = new Vector2(0f, 0f);

            return vc;
        }

        public TextMeshProUGUI CreateText(RectTransform parent, string text, Vector2 position)
        {
            TextMeshProUGUI textMesh = new GameObject("TextMeshProUGUI_GO").AddComponent<TextMeshProUGUI>();
            textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 4;
            textMesh.color = Color.white;
            textMesh.font = Resources.Load<TMP_FontAsset>("Teko-Medium SDF No Glow");
            textMesh.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            textMesh.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            textMesh.rectTransform.sizeDelta = new Vector2(60f, 10f);
            textMesh.rectTransform.anchoredPosition = position;

            return textMesh;
        }

        public void SetButtonText(ref Button _button, string _text)
        {
            if (_button.GetComponentInChildren<TextMeshProUGUI>() != null)
            {

                _button.GetComponentInChildren<TextMeshProUGUI>().text = _text;
            }

        }

        public void SetButtonTextSize(ref Button _button, float _fontSize)
        {
            if (_button.GetComponentInChildren<TextMeshProUGUI>() != null)
            {
                _button.GetComponentInChildren<TextMeshProUGUI>().fontSize = _fontSize;
            }


        }

        public void SetButtonIcon(ref Button _button, Sprite _icon)
        {
            if (_button.GetComponentsInChildren<UnityEngine.UI.Image>().Count() > 1)
            {

                _button.GetComponentsInChildren<UnityEngine.UI.Image>()[1].sprite = _icon;
            }

        }

        public void SetButtonBackground(ref Button _button, Sprite _background)
        {
            if (_button.GetComponentsInChildren<Image>().Any())
            {

                _button.GetComponentsInChildren<UnityEngine.UI.Image>()[0].sprite = _background;
            }

        }

        static public IEnumerator LoadSprite(string spritePath, TableCell obj)
        {
            Texture2D tex;

            if (_cachedSprites.ContainsKey(spritePath))
            {
                obj.GetComponentsInChildren<UnityEngine.UI.Image>()[2].sprite = _cachedSprites[spritePath];
                yield break;
            }

            using (WWW www = new WWW(spritePath))
            {
                yield return www;
                tex = www.texture;
                var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);
                _cachedSprites.Add(spritePath, newSprite);
                obj.GetComponentsInChildren<UnityEngine.UI.Image>()[2].sprite = newSprite;
            }
        }

    }
}
