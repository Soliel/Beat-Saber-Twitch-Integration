using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace TwitchIntegrationPlugin.UI
{
    public class LevelRequestNavigationController : VRUINavigationController
    {
        private Button _backButtonObject;
        private TwitchIntegrationUi _ui;

        public event Action<LevelRequestNavigationController> DidFinishEvent;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (activationType != ActivationType.AddedToHierarchy) return;
            _ui = TwitchIntegrationUi.Instance;

            if (_backButtonObject == null)
            {
                _backButtonObject = _ui.CreateBackButton(rectTransform);
            }

            _backButtonObject.gameObject.SetActive(true);
            _backButtonObject.onClick.AddListener(DismissButtonWasPressed);
        }

        public void DismissButtonWasPressed()
        {
            DidFinishEvent?.Invoke(this);
        }
    }
}
