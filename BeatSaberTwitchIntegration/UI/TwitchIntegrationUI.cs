using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HMUI;
using JetBrains.Annotations;
using SongLoaderPlugin;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRUI;
using Image = UnityEngine.UI.Image;

namespace TwitchIntegrationPlugin.UI
{
    public class TwitchIntegrationUi : MonoBehaviour
    {
        #region Static UI Components
        public static MenuSceneSetupDataSO MenuSceneSetupData;
        public static PlayerDataModelSO PlayerDataModel;
        public static PlatformLeaderboardsModel PlatformLeaderboardsModel;
        public static PlatformLeaderboardViewController PlatformLeaderboardViewController;
        public static DismissableNavigationController NavigationController;
        public static BeatmapDifficultyViewController DifficultyViewController;
        public static GameplaySetupViewController SetupViewController;
        public static MainMenuViewController MainMenuViewController;
        public static StandardLevelDetailViewController LevelDetailViewController;
        public static GameplaySetupViewController GameplaySetupViewController;
        public static RequestInfoViewController RequestInfoViewController;
        public static PracticeViewController PracticeViewController;
        public static ResultsViewController ResultsViewController;
        public static MainFlowCoordinator MainFlowCoordinator;
        #endregion

        public LevelRequestFlowCoordinatorNew LevelRequestFlowCoordinator { get; set; }
        
        private RectTransform _mainMenuRectTransform;
        private Button _buttonInstance;
        private Button _backButtonInstance;
        private GameObject _loadingIndicatorInstance;
        private NLog.Logger _logger;

        public static TwitchIntegrationUi Instance;
        public static List<Sprite> Icons = new List<Sprite>();
        public static Dictionary<string, Sprite> CachedSprites = new Dictionary<string, Sprite>();

        internal static void OnLoad()
        {
            if (Instance != null) return;
            new GameObject("TwitchIntegration UI").AddComponent<TwitchIntegrationUi>();
        }

        [UsedImplicitly]
        private void Awake()
        {
            _logger = NLog.LogManager.GetCurrentClassLogger();
            Instance = this;
            _logger.Trace("Waking up.");
            
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                Icons.Add(sprite);
            }

            try
            {
                FindUIComponents();
                _logger.Trace("Adding buttons.");
                _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "QuitButton"));
                _backButtonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "BackArrowButton"));
                _mainMenuRectTransform = _buttonInstance.transform.parent.parent as RectTransform;
                //_loadingIndicatorInstance = Resources.FindObjectsOfTypeAll<GameObject>().First(x => x.name == "LoadingIndicator");
                
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
            Button twitchModeButton = CreateUiButton(_mainMenuRectTransform, "QuitButton");

            try
            {
                ((RectTransform) twitchModeButton.transform).anchoredPosition = new Vector2(20f, 60f);
                ((RectTransform) twitchModeButton.transform).sizeDelta = new Vector2(34f, 10f);
                SetButtonText(ref twitchModeButton, (StaticData.TwitchMode) ? "Twitch Mode: ON" : "Twitch Mode: OFF");
                twitchModeButton.onClick = new Button.ButtonClickedEvent();

                twitchModeButton.onClick.AddListener(() => TwitchModeListener(twitchModeButton));
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        private void CreateDebugButton()
        {
            Button queueButton = CreateUiButton(_mainMenuRectTransform, "QuitButton");

            try
            {
                ((RectTransform)queueButton.transform).anchoredPosition = new Vector2(53f, 60f);
                ((RectTransform)queueButton.transform).sizeDelta = new Vector2(30f, 10f);
                SetButtonText(ref queueButton, "Request Queue");
                queueButton.onClick = new Button.ButtonClickedEvent();

                queueButton.onClick.AddListener(() => QueueButtonListener());
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        public void QueueButtonListener()
        {
            _logger.Debug("Clicked");
            /*try
            {
                if (LevelRequestFlowCoordinator == null)
                {
                    _logger.Trace("Creating LevelRequestFlowCoordinator");
                    LevelRequestFlowCoordinator = new GameObject("Twitch Integration Coordinator").AddComponent<LevelRequestFlowCoordinatorNew>();
                }
                _logger.Trace("presenting.");
                MainFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", new object[] {LevelRequestFlowCoordinator, null, false, false });
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }*/
        }

        public void TwitchModeListener(Button twitchModeButton)
        {
            StaticData.TwitchMode = !StaticData.TwitchMode;
            if (StaticData.TwitchMode)
            {
                SetButtonText(ref twitchModeButton, "Twitch Mode: ON");
            }
            else
            {
                SetButtonText(ref twitchModeButton, "Twitch Mode: OFF");
            }
        }

        public Button CreateBackButton(RectTransform parent)
        {
            if (_backButtonInstance == null)
            {
                return null;
            }

            Button button = Instantiate(_backButtonInstance, parent, false);
            DestroyImmediate(button.GetComponent<SignalOnUIButtonClick>());
            button.onClick = new Button.ButtonClickedEvent();

            return button;
        }

        public GameObject CreateLoadingIndicator(Transform parent)
        {
            return Instantiate(_loadingIndicatorInstance, parent, false);
        }

        public Button CreateUiButton(RectTransform parent, string buttonTemplate)
        {
            if (_buttonInstance == null)
            {
                return null;
            }

            Button btn = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == buttonTemplate)), parent, false);
            DestroyImmediate(btn.GetComponent<SignalOnUIButtonClick>());
            btn.onClick = new Button.ButtonClickedEvent();

            return btn;
        }

        public static T CreateViewController<T>(string objName) where T : VRUIViewController
        {
            T vc = new GameObject(objName).AddComponent<T>();

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

            using (WWW www = new WWW(spritePath))
            {
                yield return www;
                Texture2D tex = www.texture;
                Sprite newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);
                CachedSprites.Add(spritePath, newSprite);
                obj.GetComponentsInChildren<Image>()[2].sprite = newSprite;
            }
        }



        public static void FindUIComponents()
        {
            MenuSceneSetupData = null;
            PlayerDataModel = null;
            PlatformLeaderboardsModel = null;
            NavigationController = null;
            DifficultyViewController = null;
            SetupViewController = null;
            MainMenuViewController = null;
            LevelDetailViewController = null;
            PlatformLeaderboardViewController = null;
            GameplaySetupViewController = null;
            PracticeViewController = null;
            ResultsViewController = null;
            MainFlowCoordinator = null;

            if (RequestInfoViewController == null)
            {
                RequestInfoViewController = CreateViewController<RequestInfoViewController>("RequestController");
                RequestInfoViewController.rectTransform.anchorMin = new Vector2(0.3f, 0f);
                RequestInfoViewController.rectTransform.anchorMax = new Vector2(0.7f, 1f);
            }

            foreach (GameObject rootGameObject in SceneManager.GetSceneByName("menu").GetRootGameObjects())
            {
                MenuSceneSetupData = MenuSceneSetupData ?? rootGameObject.GetComponentInChildren<MenuSceneSetupDataSO>();
                PlayerDataModel = PlayerDataModel ?? rootGameObject.GetComponentInChildren<PlayerDataModelSO>();
                PlatformLeaderboardsModel = PlatformLeaderboardsModel ?? rootGameObject.GetComponentInChildren<PlatformLeaderboardsModel>();
                NavigationController = NavigationController ?? rootGameObject.GetComponentInChildren<DismissableNavigationController>();
                DifficultyViewController = DifficultyViewController ?? rootGameObject.GetComponentInChildren<BeatmapDifficultyViewController>();
                SetupViewController = SetupViewController ?? rootGameObject.GetComponentInChildren<GameplaySetupViewController>();
                MainMenuViewController = MainMenuViewController ?? rootGameObject.GetComponentInChildren<MainMenuViewController>();
                LevelDetailViewController = LevelDetailViewController ?? rootGameObject.GetComponentInChildren<StandardLevelDetailViewController>();
                PlatformLeaderboardViewController = PlatformLeaderboardViewController ?? rootGameObject.GetComponentInChildren<PlatformLeaderboardViewController>();
                GameplaySetupViewController = GameplaySetupViewController ?? rootGameObject.GetComponentInChildren<GameplaySetupViewController>();
                PracticeViewController = PracticeViewController ?? rootGameObject.GetComponentInChildren<PracticeViewController>();
                ResultsViewController = ResultsViewController ?? rootGameObject.GetComponentInChildren<ResultsViewController>();
                MainFlowCoordinator = MainFlowCoordinator ?? rootGameObject.GetComponentInChildren<MainFlowCoordinator>();
            }
        }
    }
}
