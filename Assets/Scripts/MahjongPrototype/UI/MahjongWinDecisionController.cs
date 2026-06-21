using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Win Decision Controller")]
    public sealed class MahjongWinDecisionController : MonoBehaviour
    {
        [Header("Win Decision")]
        [SerializeField] private GameObject winDecisionRoot;
        [SerializeField] private Button winButton;
        [SerializeField] private Button declineWinButton;

        private bool isSubscribed;
        private bool warnedMissingRoot;
        private bool warnedMissingWinButton;
        private bool warnedMissingDeclineButton;
        private bool didCreateRuntimeUi;

        public event Action WinRequested;
        public event Action DeclineWinRequested;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
            SetVisible(false);
        }

        private void OnEnable()
        {
            CacheReferences();
            RegisterButtonListeners();
            SetVisible(false);
        }

        private void OnDisable()
        {
            UnregisterButtonListeners();
        }

        public void SetVisible(bool visible)
        {
            CacheReferences();

            if (winDecisionRoot != null)
            {
                winDecisionRoot.SetActive(visible);
                return;
            }

            WarnMissingOnce(ref warnedMissingRoot, "WinDecisionRoot is not assigned.");
        }

        private void CacheReferences()
        {
            if (winDecisionRoot == null)
            {
                Transform root = FindChildByName("WinDecisionArea");
                if (root != null)
                    winDecisionRoot = root.gameObject;
            }

            if (winDecisionRoot == null && !didCreateRuntimeUi)
                CreateRuntimeUi();

            if (winButton == null)
                winButton = FindButtonByName("WinButton");

            if (declineWinButton == null)
                declineWinButton = FindButtonByName("DeclineWinButton");
        }

        private void RegisterButtonListeners()
        {
            if (isSubscribed)
                return;

            if (winButton != null)
            {
                winButton.onClick.AddListener(HandleWinClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingWinButton, "WinButton is not assigned.");
            }

            if (declineWinButton != null)
            {
                declineWinButton.onClick.AddListener(HandleDeclineClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingDeclineButton, "DeclineWinButton is not assigned.");
            }

            isSubscribed = true;
        }

        private void UnregisterButtonListeners()
        {
            if (!isSubscribed)
                return;

            if (winButton != null)
                winButton.onClick.RemoveListener(HandleWinClicked);

            if (declineWinButton != null)
                declineWinButton.onClick.RemoveListener(HandleDeclineClicked);

            isSubscribed = false;
        }

        private void HandleWinClicked()
        {
            WinRequested?.Invoke();
        }

        private void HandleDeclineClicked()
        {
            DeclineWinRequested?.Invoke();
        }

        private Button FindButtonByName(string objectName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button != null && button.gameObject.name == objectName)
                    return button;
            }

            return null;
        }

        private Transform FindChildByName(string objectName)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform child = transforms[i];
                if (child != null && child.gameObject.name == objectName)
                    return child;
            }

            return null;
        }

        private void CreateRuntimeUi()
        {
            didCreateRuntimeUi = true;

            Transform parent = FindChildByName("ControlArea");
            if (parent == null)
                parent = transform;

            GameObject root = new GameObject("WinDecisionArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(180f, 60f);

            HorizontalLayoutGroup layoutGroup = root.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 8f;

            winDecisionRoot = root;
            winButton = CreateButton(root.transform, "WinButton", "\u548C\u4E86");
            declineWinButton = CreateButton(root.transform, "DeclineWinButton", "\u62D2\u5426");
            winDecisionRoot.SetActive(false);
        }

        private Button CreateButton(Transform parent, string objectName, string labelText)
        {
            GameObject buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = parent.gameObject.layer;
            buttonObject.transform.SetParent(parent, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(80f, 60f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = Color.white;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject labelObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.layer = buttonObject.layer;
            labelObject.transform.SetParent(buttonObject.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color32(50, 50, 50, 255);
            ApplyExistingFont(label);

            return button;
        }

        private void ApplyExistingFont(TextMeshProUGUI label)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || text.font == null)
                    continue;

                if (text.font.name.IndexOf("NotoSansJP", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                label.font = text.font;
                label.fontSharedMaterial = text.fontSharedMaterial;
                return;
            }
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongWinDecisionController)}: {message}", this);
        }
    }
}
