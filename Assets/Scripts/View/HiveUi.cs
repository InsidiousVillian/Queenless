using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Queenless.Rules;

namespace Queenless
{
    public sealed class HiveUi
    {
        Canvas canvas;
        GameObject titleRoot;
        GameObject playRoot;
        GameObject modalRoot;
        TextMeshProUGUI titleWord;
        TextMeshProUGUI hint;
        TextMeshProUGUI controls;
        TextMeshProUGUI chamberLabel;
        TextMeshProUGUI muteLabel;
        RectTransform bottomBar;
        Button mind;
        Button stay;
        Button follow;
        Button wait;
        Image mindPulse;
        Image followPulse;
        Image waitPulse;
        GridLayoutGroup buttonGrid;
        string steadyHint = "";
        float flashUntil;
        TMP_FontAsset font;

        public void Build(GameFlow flow)
        {
            SpriteFactory.Ensure();
            font = Font();
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            var canvasGo = new GameObject("UI");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var vignetteGo = new GameObject("Vignette", typeof(RectTransform));
            vignetteGo.transform.SetParent(canvas.transform, false);
            Stretch(vignetteGo.GetComponent<RectTransform>());
            var raw = vignetteGo.AddComponent<RawImage>();
            raw.texture = SpriteFactory.Vignette;
            raw.color = Color.white;
            raw.raycastTarget = false;

            titleRoot = new GameObject("Title", typeof(RectTransform));
            titleRoot.transform.SetParent(canvas.transform, false);
            Stretch(titleRoot.GetComponent<RectTransform>());
            BuildTitle(flow);

            playRoot = new GameObject("Play", typeof(RectTransform));
            playRoot.transform.SetParent(canvas.transform, false);
            Stretch(playRoot.GetComponent<RectTransform>());
            BuildPlay(flow);

            modalRoot = new GameObject("Modal", typeof(RectTransform));
            modalRoot.transform.SetParent(canvas.transform, false);
            Stretch(modalRoot.GetComponent<RectTransform>());
            modalRoot.SetActive(false);
            playRoot.SetActive(false);
        }

        public void Tick(int coach)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                EventSystem.current.SetSelectedGameObject(null);
            Pulse(mindPulse, coach == 1);
            Pulse(followPulse, coach == 2);
            Pulse(waitPulse, coach == 3);
            if (flashUntil > 0f && Time.unscaledTime >= flashUntil)
                RestoreHint();
        }

        public void Layout()
        {
            if (buttonGrid == null)
                return;
            float width = Screen.width;
            int cols = width < 760 ? 3 : 6;
            float cell = Mathf.Clamp((width - 80f) / cols, 120f, 240f);
            buttonGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            buttonGrid.constraintCount = cols;
            buttonGrid.cellSize = new Vector2(cell, 64f);
            if (bottomBar != null)
                bottomBar.sizeDelta = new Vector2(0f, width < 760 ? 340f : 250f);
            if (titleWord != null)
                titleWord.fontSize = width < 700 ? 64 : 92;
        }

        public void ShowTitle(bool muted)
        {
            titleRoot.SetActive(true);
            playRoot.SetActive(false);
            modalRoot.SetActive(false);
            SetMute(muted);
        }

        public void ShowPlay()
        {
            titleRoot.SetActive(false);
            playRoot.SetActive(true);
            modalRoot.SetActive(false);
        }

        public void SetChamber(int number, string name, string hintText, bool waitKnown)
        {
            chamberLabel.text = "Chamber " + number + "\n" + name;
            steadyHint = hintText;
            hint.text = hintText;
            hint.color = Palette.Core;
            controls.text = waitKnown ? HiveCopy.ControlsWait : HiveCopy.Controls;
            flashUntil = 0f;
        }

        public void SetVerbs(bool canSwitch, bool canStay, bool canFollow, bool canWait)
        {
            Dim(mind, canSwitch);
            Dim(stay, canStay);
            Dim(follow, canFollow);
            Dim(wait, canWait);
        }

        public void Flash(string reason, bool silence)
        {
            hint.text = reason;
            hint.color = silence ? Palette.Pad : Palette.Honey;
            flashUntil = Time.unscaledTime + 2f;
        }

        public void RestoreHint()
        {
            flashUntil = 0f;
            hint.text = steadyHint;
            hint.color = Palette.Core;
        }

        public void SetMute(bool muted)
        {
            if (muteLabel != null)
                muteLabel.text = muted ? "Sound off" : "Sound on";
        }

        public void ShowIntro(string name, string quote, Action dismiss)
        {
            OpenModal(name, quote, dismiss, "Continue");
        }

        public void ShowClear(Action next)
        {
            OpenModal(HiveCopy.Clear, "", next, "Next");
        }

        public void ShowCaught(Action undo, Action restart)
        {
            var panel = OpenModal(HiveCopy.Caught, "", null, null);
            var row = Row(panel);
            Button(row, "Undo", delegate { undo(); });
            Button(row, "Restart", delegate { restart(); });
        }

        public void ShowEnding(Action back)
        {
            OpenModal("The pads answer.", "For a moment the four of you are one thought again. Not a queen. A chorus. The hive is still queenless. It is no longer mindless.", back, "Back to the chambers");
        }

        public void ShowPause(bool muted, Action resume, Action restart, Action howTo, Action mute, Action title)
        {
            var panel = OpenModal("Paused", "", null, null);
            var column = Column(panel);
            Button(column, "Resume", delegate { resume(); });
            Button(column, "Restart", delegate { restart(); });
            Button(column, "How to play", delegate { howTo(); });
            Button(column, muted ? "Unmute" : "Mute", delegate { mute(); });
            Button(column, "Quit to title", delegate { title(); });
        }

        public void ShowHowTo(Action back)
        {
            OpenModal("How to play", HiveCopy.HowTo, back, "Back");
        }

        public void ShowCredits(Action back)
        {
            OpenModal("Credits", HiveCopy.Credits, back, "Back");
        }

        public void ShowMissing(Action back)
        {
            OpenModal(HiveCopy.Missing, "Back to the title.", back, "Back to title");
        }

        public void ShowChambers(int cleared, Action<int> pick, Action back)
        {
            modalRoot.SetActive(true);
            Clear(modalRoot.transform);
            var dim = Dimmer(modalRoot.transform);
            var panel = Panel(dim.transform);
            Title(panel, "Chambers", 42);
            var scroll = Scroll(panel, out var content);
            for (int n = 1; n <= LevelLibrary.Count; n++)
            {
                var def = LevelLibrary.Get(n);
                bool done = (cleared & (1 << n)) != 0;
                string label = n + "   " + def.Name + (done ? "    ●" : "");
                int pickNumber = n;
                var button = Button(content, label, delegate { pick(pickNumber); });
                var text = button.GetComponentInChildren<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.Left;
                text.margin = new Vector4(18f, 0f, 0f, 0f);
                if (done)
                    text.color = Palette.Honey;
            }
            Button(panel, "Back", delegate { back(); });
        }

        void BuildTitle(GameFlow flow)
        {
            var column = new GameObject("Column", typeof(RectTransform));
            column.transform.SetParent(titleRoot.transform, false);
            var rect = column.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 760f);
            var layout = column.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            titleWord = Label(column.transform, "QUEENLESS", 92, Palette.Honey, FontStyles.Bold, TextAlignmentOptions.Center);
            titleWord.characterSpacing = 12f;
            titleWord.GetComponent<LayoutElement>().minHeight = 110f;
            var blurb = Label(column.transform, HiveCopy.Title, 26, Palette.Core, FontStyles.Normal, TextAlignmentOptions.Center);
            blurb.GetComponent<LayoutElement>().minHeight = 90f;
            var line = Label(column.transform, HiveCopy.TitleLine, 24, Palette.Body, FontStyles.Italic, TextAlignmentOptions.Center);
            line.GetComponent<LayoutElement>().minHeight = 48f;
            Space(column.transform, 12f);
            MenuButton(column.transform, "Enter the hive", flow.EnterHive);
            MenuButton(column.transform, "How to play", flow.ShowHowTo);
            MenuButton(column.transform, "Chambers", flow.ShowChambers);
            MenuButton(column.transform, "Credits", flow.ShowCredits);

            var mute = MenuButton(titleRoot.transform, "Sound on", flow.ToggleMute);
            var muteRect = mute.GetComponent<RectTransform>();
            muteRect.anchorMin = new Vector2(1f, 0f);
            muteRect.anchorMax = new Vector2(1f, 0f);
            muteRect.pivot = new Vector2(1f, 0f);
            muteRect.anchoredPosition = new Vector2(-28f, 28f);
            muteRect.sizeDelta = new Vector2(180f, 56f);
            muteLabel = mute.GetComponentInChildren<TextMeshProUGUI>();
        }

        void BuildPlay(GameFlow flow)
        {
            var top = new GameObject("Top", typeof(RectTransform));
            top.transform.SetParent(playRoot.transform, false);
            var topRect = top.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.sizeDelta = new Vector2(0f, 92f);
            var word = Label(top.transform, "QUEENLESS", 28, Palette.Honey, FontStyles.Bold, TextAlignmentOptions.Left);
            var wordRect = word.rectTransform;
            wordRect.anchorMin = new Vector2(0f, 0f);
            wordRect.anchorMax = new Vector2(0.5f, 1f);
            wordRect.offsetMin = new Vector2(28f, 0f);
            wordRect.offsetMax = new Vector2(0f, -8f);
            word.characterSpacing = 6f;
            chamberLabel = Label(top.transform, "", 24, Palette.Core, FontStyles.Normal, TextAlignmentOptions.Right);
            var chamberRect = chamberLabel.rectTransform;
            chamberRect.anchorMin = new Vector2(0.4f, 0f);
            chamberRect.anchorMax = new Vector2(1f, 1f);
            chamberRect.offsetMin = Vector2.zero;
            chamberRect.offsetMax = new Vector2(-28f, -8f);

            var bottom = new GameObject("Bottom", typeof(RectTransform));
            bottom.transform.SetParent(playRoot.transform, false);
            var bottomRect = bottom.GetComponent<RectTransform>();
            bottomBar = bottomRect;
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0f);
            bottomRect.pivot = new Vector2(0.5f, 0f);
            bottomRect.sizeDelta = new Vector2(0f, 250f);
            var column = bottom.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(24, 24, 8, 20);
            column.spacing = 8f;
            column.childAlignment = TextAnchor.LowerCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            hint = Label(bottom.transform, "", 24, Palette.Core, FontStyles.Italic, TextAlignmentOptions.Center);
            hint.GetComponent<LayoutElement>().minHeight = 72f;
            controls = Label(bottom.transform, HiveCopy.Controls, 18, Palette.Body, FontStyles.Normal, TextAlignmentOptions.Center);
            controls.GetComponent<LayoutElement>().minHeight = 36f;

            var buttons = new GameObject("Buttons", typeof(RectTransform));
            buttons.transform.SetParent(bottom.transform, false);
            buttonGrid = buttons.AddComponent<GridLayoutGroup>();
            buttonGrid.spacing = new Vector2(10f, 10f);
            buttonGrid.childAlignment = TextAnchor.MiddleCenter;
            buttonGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            buttonGrid.constraintCount = 6;
            buttonGrid.cellSize = new Vector2(180f, 64f);
            var fit = buttons.AddComponent<LayoutElement>();
            fit.minHeight = 140f;
            fit.preferredHeight = 150f;

            mind = Verb(buttons.transform, "Mind", flow.OnMind, out mindPulse);
            stay = Verb(buttons.transform, "Stay", flow.OnStay, out var unusedStay);
            UnityEngine.Object.Destroy(unusedStay.gameObject);
            follow = Verb(buttons.transform, "Follow", flow.OnFollow, out followPulse);
            wait = Verb(buttons.transform, "Wait", flow.OnWait, out waitPulse);
            Verb(buttons.transform, "Undo", flow.OnUndo, out var unusedUndo);
            UnityEngine.Object.Destroy(unusedUndo.gameObject);
            Verb(buttons.transform, "Restart", flow.OnRestart, out var unusedRestart);
            UnityEngine.Object.Destroy(unusedRestart.gameObject);
        }

        RectTransform OpenModal(string title, string body, Action primary, string primaryLabel)
        {
            modalRoot.SetActive(true);
            Clear(modalRoot.transform);
            var dim = Dimmer(modalRoot.transform);
            if (primary != null)
            {
                var hit = dim.GetComponent<Button>();
                hit.onClick.AddListener(delegate { primary(); });
            }
            var panel = Panel(dim.transform);
            panel.GetComponent<Image>().raycastTarget = false;
            Title(panel, title, 40);
            Scroll(panel, out var content);
            if (!string.IsNullOrEmpty(body))
            {
                var text = Label(content, body, 22, Palette.Core, FontStyles.Normal, TextAlignmentOptions.TopLeft);
                text.alignment = TextAlignmentOptions.TopLeft;
                float width = Mathf.Min(820f, Screen.width - 48f) - 80f;
                Vector2 preferred = text.GetPreferredValues(body, Mathf.Max(240f, width), 0f);
                int lines = 1;
                for (int i = 0; i < body.Length; i++)
                {
                    if (body[i] == '\n')
                        lines++;
                }
                float fallback = lines * 32f;
                text.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(preferred.y + 16f, fallback);
            }
            if (primary != null && primaryLabel != null)
                Button(panel, primaryLabel, delegate { primary(); });
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            return panel;
        }

        Image Dimmer(Transform parent)
        {
            var image = Image("Dim", parent, SpriteFactory.White);
            Stretch(image.rectTransform);
            image.color = new Color(0f, 0f, 0f, 0.62f);
            var button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            return image;
        }

        RectTransform Panel(Transform parent)
        {
            var image = Image("Panel", parent, SpriteFactory.White);
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            float w = Mathf.Min(820f, Screen.width - 48f);
            float h = Mathf.Min(680f, Screen.height - 48f);
            rect.sizeDelta = new Vector2(w, h);
            image.color = new Color(0.12f, 0.08f, 0.05f, 0.96f);
            var layout = image.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 24);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        RectTransform Scroll(RectTransform panel, out RectTransform content)
        {
            var root = new GameObject("Scroll", typeof(RectTransform));
            root.transform.SetParent(panel, false);
            var rootRect = root.GetComponent<RectTransform>();
            var flex = root.AddComponent<LayoutElement>();
            flex.flexibleHeight = 1f;
            flex.minHeight = 160f;
            var scroll = root.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(root.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<RectMask2D>();
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewportImage.raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewport.transform, false);
            content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fit = contentGo.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = content;
            return rootRect;
        }

        void Title(RectTransform panel, string text, float size)
        {
            var label = Label(panel, text, size, Palette.Honey, FontStyles.Bold, TextAlignmentOptions.Left);
            label.characterSpacing = 2f;
        }

        RectTransform Column(Transform panel)
        {
            var go = new GameObject("Actions", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var fit = go.AddComponent<LayoutElement>();
            fit.minHeight = 280f;
            return go.GetComponent<RectTransform>();
        }

        RectTransform Row(Transform panel)
        {
            var go = new GameObject("Actions", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fit = go.AddComponent<LayoutElement>();
            fit.minHeight = 64f;
            return go.GetComponent<RectTransform>();
        }

        Button MenuButton(Transform parent, string text, Action click)
        {
            var button = Button(parent, text, click);
            var fit = button.GetComponent<LayoutElement>();
            fit.minHeight = 58f;
            fit.preferredHeight = 62f;
            return button;
        }

        Button Verb(Transform parent, string text, Action click, out Image pulse)
        {
            var button = Button(parent, text, click);
            var glow = Image("Pulse", button.transform, SpriteFactory.Rounded);
            Stretch(glow.rectTransform);
            glow.rectTransform.offsetMin = new Vector2(-6f, -6f);
            glow.rectTransform.offsetMax = new Vector2(6f, 6f);
            glow.color = Palette.WithAlpha(Palette.Honey, 0f);
            glow.raycastTarget = false;
            glow.transform.SetAsFirstSibling();
            glow.enabled = false;
            pulse = glow;
            return button;
        }

        Button Button(Transform parent, string text, Action click)
        {
            var image = Image(text, parent, SpriteFactory.Rounded);
            image.color = Palette.Wax;
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Palette.Wax;
            colors.highlightedColor = Palette.WallHighlight;
            colors.pressedColor = new Color(0.55f, 0.36f, 0.16f, 1f);
            colors.selectedColor = Palette.Wax;
            colors.disabledColor = Palette.WithAlpha(Palette.Wax, 0.4f);
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            if (click != null)
                button.onClick.AddListener(delegate { click(); });
            var label = Label(image.transform, text, 22, Palette.Honey, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.raycastTarget = false;
            var fit = image.gameObject.AddComponent<LayoutElement>();
            fit.minHeight = 56f;
            return button;
        }

        void Dim(Button button, bool on)
        {
            var colors = button.colors;
            var wax = Palette.Wax;
            var dim = Palette.WithAlpha(Palette.Wax, 0.38f);
            colors.normalColor = on ? wax : dim;
            colors.highlightedColor = on ? Palette.WallHighlight : dim;
            colors.pressedColor = on ? new Color(0.55f, 0.36f, 0.16f, 1f) : dim;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.color = on ? Palette.Honey : new Color(0.55f, 0.4f, 0.25f, 0.65f);
        }

        void Pulse(Image image, bool on)
        {
            if (image == null)
                return;
            image.enabled = on;
            if (!on)
                return;
            float a = 0.2f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f));
            image.color = Palette.WithAlpha(Palette.Honey, a);
        }

        TextMeshProUGUI Label(Transform parent, string text, float size, Color color, FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.text = text;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            var fit = go.AddComponent<LayoutElement>();
            fit.minHeight = size + 12f;
            return tmp;
        }

        Image Image(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            return image;
        }

        void Space(Transform parent, float height)
        {
            var go = new GameObject("Space", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var fit = go.AddComponent<LayoutElement>();
            fit.minHeight = height;
            fit.preferredHeight = height;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Clear(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
        }

        TMP_FontAsset Font()
        {
            if (font != null)
                return font;
            var os = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            font = TMP_FontAsset.CreateFontAsset(os);
            return font;
        }
    }
}
