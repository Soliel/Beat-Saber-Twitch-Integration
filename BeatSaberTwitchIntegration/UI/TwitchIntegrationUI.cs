using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HMUI;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using Image = UnityEngine.UI.Image;

namespace TwitchIntegrationPlugin.UI
{
    public class TwitchIntegrationUi : MonoBehaviour
    {
        private RectTransform _mainMenuRectTransform;
        public  MainMenuViewController MainMenuViewController;

        private Button _buttonInstance;
        private Button _backButtonInstance;
        private GameObject _loadingIndicatorInstance;

        public static TwitchIntegrationUi Instance;

        public static List<Sprite> Icons = new List<Sprite>();

        public LevelRequestFlowCoordinator LevelRequestFlowCoordinator { get; set; }

        public static Dictionary<string, Sprite> CachedSprites = new Dictionary<string, Sprite>();

        private NLog.Logger _logger;


        internal static void OnLoad()
        {
            if(Instance != null)
            {
                return;
            }
            new GameObject("TwitchIntegration UI").AddComponent<TwitchIntegrationUi>();
        }

        [UsedImplicitly]
        private void Awake()
        {
            _logger = NLog.LogManager.GetCurrentClassLogger();
            Instance = this;
            
            foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                Icons.Add(sprite);
            }

            try
            {
                _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "QuitButton"));
                _backButtonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "BackArrowButton"));
                MainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _mainMenuRectTransform = _buttonInstance.transform.parent as RectTransform;
                _loadingIndicatorInstance = Resources.FindObjectsOfTypeAll<GameObject>().First(x => x.name == "LoadingIndicator");
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

            try
            {
                CreateTwitchModeButton();
                CreateDebugButton();
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }

        }

        private void CreateTwitchModeButton()
        {
            var twitchModeButton = CreateUiButton(_mainMenuRectTransform, "QuitButton");

            try
            {
                ((RectTransform) twitchModeButton.transform).anchoredPosition = new Vector2(4f, 68f);
                ((RectTransform) twitchModeButton.transform).sizeDelta = new Vector2(34f, 10f);

                SetButtonText(ref twitchModeButton, (StaticData.TwitchMode) ? "Twitch Mode: ON" : "Twitch Mode: OFF");
                //SetButtonIcon(ref _twitchModeButton, icons.First(x => (x.name == "SettingsIcon")));

                twitchModeButton.onClick.AddListener(delegate
                {
                    StaticData.TwitchMode = !StaticData.TwitchMode;
                    if (StaticData.TwitchMode)
                    {
                        SetButtonText(ref twitchModeButton, "Twitch Mode: ON");
                    } else
                    {
                        SetButtonText(ref twitchModeButton, "Twitch Mode: OFF");
                    }
                    

                });
            } catch(Exception e)
            {
                _logger.Error(e);
            }
        }

        private void CreateDebugButton()
        {
            var debugButton = CreateUiButton(_mainMenuRectTransform, "QuitButton");

            try
            {
                ((RectTransform) debugButton.transform).anchoredPosition = new Vector2(40f, 68f);
                ((RectTransform) debugButton.transform).sizeDelta = new Vector2(25f, 10f);

                SetButtonText(ref debugButton, "Request Queue");

                //SetButtonIcon(ref _debugButton, icons.First(x => (x.name == "SettingsIcon")));

                debugButton.onClick.AddListener(delegate
                {
                    try
                    {
                        if (LevelRequestFlowCoordinator == null)
                        {
                            LevelRequestFlowCoordinator = new GameObject("Twitch Integration Coordinator").AddComponent<LevelRequestFlowCoordinator>();
                        }
                        LevelRequestFlowCoordinator.Present(MainMenuViewController, true);
                    }
                    catch(Exception e)
                    {
                        _logger.Error(e);
                    }
                });
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        public Button CreateBackButton(RectTransform parent)
        {
            if (_backButtonInstance == null)
            {
                return null;
            }

            var button = Instantiate(_backButtonInstance, parent, false);
            DestroyImmediate(button.GetComponent<SignalOnUIButtonClick>());
            button.onClick = new Button.ButtonClickedEvent();

            return button;
        }

        public GameObject CreateLoadingIndicator(Transform parent)
        {
            GameObject indicator = Instantiate(_loadingIndicatorInstance, parent, false);
            return indicator;
        }

        public Button CreateUiButton(RectTransform parent, string buttonTemplate)
        {
            if (_buttonInstance == null)
            {
                return null;
            }

            var btn = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == buttonTemplate)), parent, false);
            DestroyImmediate(btn.GetComponent<SignalOnUIButtonClick>());
            btn.onClick = new Button.ButtonClickedEvent();

            return btn;
        }

        public T CreateViewController<T>(string objName) where T : VRUIViewController
        {
            var vc = new GameObject(objName).AddComponent<T>();

            vc.rectTransform.anchorMin = new Vector2(0f, 0f);
            vc.rectTransform.anchorMax = new Vector2(1f, 1f);
            vc.rectTransform.sizeDelta = new Vector2(0f, 0f);
            vc.rectTransform.anchoredPosition = new Vector2(0f, 0f);

            return vc;
        }

        public TextMeshProUGUI CreateText(RectTransform parent, string text, Vector2 position)
        {
            var textMesh = new GameObject("TextMeshProUGUI_GO").AddComponent<TextMeshProUGUI>();
            textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 4;
            textMesh.color = Color.white;
            textMesh.font = Resources.Load<TMP_FontAsset>("Teko-Medium SDF No Glow");
            textMesh.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            textMesh.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            textMesh.rectTransform.sizeDelta = new Vector2(0f, 0f);
            textMesh.rectTransform.anchoredPosition = position;
            textMesh.alignment = TextAlignmentOptions.Center;
            return textMesh;
        }

        public void SetButtonText(ref Button button, string text)
        {
            if (button.GetComponentInChildren<TextMeshProUGUI>() != null)
            {

                button.GetComponentInChildren<TextMeshProUGUI>().text = text;
            }

        }

        public void SetButtonTextSize(ref Button button, float fontSize)
        {
            if (button.GetComponentInChildren<TextMeshProUGUI>() != null)
            {
                button.GetComponentInChildren<TextMeshProUGUI>().fontSize = fontSize;
            }


        }

        public void SetButtonIcon(ref Button button, Sprite icon)
        {
            if (button.GetComponentsInChildren<Image>().Count() > 1)
            {

                button.GetComponentsInChildren<Image>()[1].sprite = icon;
            }

        }

        public void SetButtonBackground(ref Button button, Sprite background)
        {
            if (button.GetComponentsInChildren<Image>().Any())
            {

                button.GetComponentsInChildren<Image>()[0].sprite = background;
            }

        }

        public static IEnumerator LoadSprite(string spritePath, TableCell obj)
        {
            if (CachedSprites.ContainsKey(spritePath))
            {
                obj.GetComponentsInChildren<Image>()[2].sprite = CachedSprites[spritePath];
                yield break;
            }

            using (var www = new WWW(spritePath))
            {
                yield return www;
                var tex = www.texture;
                var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);
                CachedSprites.Add(spritePath, newSprite);
                obj.GetComponentsInChildren<Image>()[2].sprite = newSprite;
            }
        }

    }
}
