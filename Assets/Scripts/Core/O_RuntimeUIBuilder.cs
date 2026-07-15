using Universes.Prestige;
using Universes.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Universes.Core
{
    public class O_RuntimeUIBuilder
    {
        public O_HUDController Hud { get; private set; }
        public O_FloatingTextSpawner FloatingText { get; private set; }

        public static O_RuntimeUIBuilder Build(O_UniverseRunController run, O_UpgradeCatalog catalog, O_VariantCatalog variants)
        {
            var builder = new O_RuntimeUIBuilder();
            builder.CreateUi(run, catalog, variants);
            return builder;
        }

        private void CreateUi(O_UniverseRunController run, O_UpgradeCatalog catalog, O_VariantCatalog variants)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            FloatingText = canvasGo.AddComponent<O_FloatingTextSpawner>();
            SetField(FloatingText, "canvas", canvas);

            Hud = canvasGo.AddComponent<O_HUDController>();
            var upgradePanel = canvasGo.AddComponent<O_UpgradePanelController>();
            var collapsePanel = canvasGo.AddComponent<O_CollapsePanelController>();
            var variantPicker = canvasGo.AddComponent<O_VariantPickerController>();

            SetField(Hud, "runController", run);
            SetField(upgradePanel, "runController", run);
            SetField(upgradePanel, "catalog", catalog);
            SetField(collapsePanel, "runController", run);
            SetField(variantPicker, "runController", run);
            SetField(variantPicker, "catalog", variants);

            BuildTopBar(canvasGo.transform, font, Hud);
            BuildLeftPanel(canvasGo.transform, font, Hud);
            BuildStarInfo(canvasGo.transform, font, Hud);
            BuildBottomPanel(canvasGo.transform, font, Hud, upgradePanel);
            BuildCollapseModal(canvasGo.transform, font, collapsePanel, variantPicker);
            BuildVariantModal(canvasGo.transform, font, variantPicker);
        }

        private void BuildTopBar(Transform canvas, Font font, O_HUDController hud)
        {
            var bar = CreatePanel(canvas, "TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -45), new Vector2(0, 0));
            bar.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.1f, 0.9f);
            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 8, 8);
            layout.spacing = 40;

            SetField(hud, "stardustText", CreateText(bar.transform, "Stardust: 0", font, 20));
            SetField(hud, "dnaText", CreateText(bar.transform, "DNA: 0", font, 20));
            SetField(hud, "timerText", CreateText(bar.transform, "Time: 00:00", font, 20));

            var sliderGo = new GameObject("EntropySlider");
            sliderGo.transform.SetParent(bar.transform, false);
            var sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(280, 22);
            sliderGo.AddComponent<LayoutElement>().preferredWidth = 280;

            var slider = sliderGo.AddComponent<Slider>();
            var bg = CreateImage(sliderGo.transform, new Color(0.15f, 0.15f, 0.2f));
            Stretch(bg.rectTransform);
            var fillArea = new GameObject("Fill");
            fillArea.transform.SetParent(sliderGo.transform, false);
            Stretch(fillArea.AddComponent<RectTransform>());
            var fill = CreateImage(fillArea.transform, new Color(0.4f, 0.7f, 0.9f));
            Stretch(fill.rectTransform);
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            slider.interactable = false;

            SetField(hud, "entropySlider", slider);
            SetField(hud, "entropyFill", fill);
        }

        private void BuildLeftPanel(Transform canvas, Font font, O_HUDController hud)
        {
            var panel = CreatePanel(canvas, "LeftPanel", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(8, 8), new Vector2(8, -55));
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(175, 0);
            panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f, 0.9f);
            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.padding = new RectOffset(8, 8, 8, 8);

            SetField(hud, "bigBangButton", CreateButton(panel.transform, "Big Bang", font));
            SetField(hud, "createStarButton", CreateButton(panel.transform, "Create Star", font));
            SetField(hud, "createStarCostText", CreateText(panel.transform, "25", font, 11));
            SetField(hud, "createPlanetButton", CreateButton(panel.transform, "Create Planet", font));
            SetField(hud, "createPlanetCostText", CreateText(panel.transform, "100", font, 11));
            SetField(hud, "slowEntropyButton", CreateButton(panel.transform, "Slow Entropy", font));
            SetField(hud, "collapseButton", CreateButton(panel.transform, "Collapse Universe", font));
        }

        private void BuildStarInfo(Transform canvas, Font font, O_HUDController hud)
        {
            var panel = CreatePanel(canvas, "StarInfo", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(-8, 0));
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(190, 130);
            panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f, 0.9f);
            var text = CreateText(panel.transform, "Select a star", font, 13);
            Stretch(text.rectTransform, 8);
            text.alignment = TextAnchor.UpperLeft;
            panel.SetActive(false);
            SetField(hud, "starInfoPanel", panel);
            SetField(hud, "starInfoText", text);
        }

        private void BuildBottomPanel(Transform canvas, Font font, O_HUDController hud, O_UpgradePanelController upgrades)
        {
            var panel = CreatePanel(canvas, "BottomPanel", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(190, 8), new Vector2(-210, 8));
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(-400, 195);
            panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f, 0.9f);

            var stats = CreateText(panel.transform, "", font, 12);
            var statsRect = stats.rectTransform;
            statsRect.anchorMin = new Vector2(0, 0);
            statsRect.anchorMax = new Vector2(0.32f, 1);
            statsRect.offsetMin = new Vector2(10, 8);
            statsRect.offsetMax = new Vector2(-4, -8);
            stats.alignment = TextAnchor.UpperLeft;
            SetField(hud, "statsText", stats);

            var scrollGo = new GameObject("UpgradeScroll");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.32f, 0);
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(4, 8);
            scrollRect.offsetMax = new Vector2(-8, -8);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            Stretch(viewport.AddComponent<RectTransform>());
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;

            SetField(upgrades, "contentRoot", content.transform);
        }

        private void BuildCollapseModal(Transform canvas, Font font, O_CollapsePanelController collapse, O_VariantPickerController variants)
        {
            var panel = CreatePanel(canvas, "CollapsePanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            panel.AddComponent<Image>().color = new Color(0, 0, 0, 0.75f);

            var box = CreatePanel(panel.transform, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            box.GetComponent<RectTransform>().sizeDelta = new Vector2(440, 380);
            box.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.2f);

            var text = CreateText(box.transform, "", font, 14);
            Stretch(text.rectTransform, 12);
            text.alignment = TextAnchor.UpperLeft;

            var btn = CreateButton(box.transform, "Continue", font);
            var btnRect = btn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0);
            btnRect.anchorMax = new Vector2(0.5f, 0);
            btnRect.anchoredPosition = new Vector2(0, 28);
            btnRect.sizeDelta = new Vector2(150, 34);

            panel.SetActive(false);
            SetField(collapse, "panel", panel);
            SetField(collapse, "breakdownText", text);
            SetField(collapse, "continueButton", btn);
        }

        private void BuildVariantModal(Transform canvas, Font font, O_VariantPickerController picker)
        {
            var panel = CreatePanel(canvas, "VariantPanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            panel.AddComponent<Image>().color = new Color(0, 0, 0, 0.82f);

            var title = CreateText(panel.transform, "Choose Your Next Parallel Universe", font, 20);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -45);
            titleRect.sizeDelta = new Vector2(700, 40);
            title.alignment = TextAnchor.MiddleCenter;

            var cards = new GameObject("Cards");
            cards.transform.SetParent(panel.transform, false);
            var cardsRect = cards.AddComponent<RectTransform>();
            cardsRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardsRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardsRect.sizeDelta = new Vector2(720, 200);
            var hlg = cards.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            panel.SetActive(false);
            SetField(picker, "panel", panel);
            SetField(picker, "cardsRoot", cards.transform);
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return go;
        }

        private static Text CreateText(Transform parent, string content, Font font, int size)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            go.AddComponent<LayoutElement>().minHeight = size + 6;
            return text;
        }

        private static Button CreateButton(Transform parent, string label, Font font)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.2f, 0.35f, 0.55f);
            var btn = go.AddComponent<Button>();
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(155, 30);
            go.AddComponent<LayoutElement>().minHeight = 34;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.font = font;
            text.fontSize = 13;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            Stretch(text.rectTransform);
            return btn;
        }

        private static Image CreateImage(Transform parent, Color color)
        {
            var go = new GameObject("Image");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static void Stretch(RectTransform rect, float pad = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(pad, pad);
            rect.offsetMax = new Vector2(-pad, -pad);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
