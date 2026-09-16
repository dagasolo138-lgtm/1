using System.Collections.Generic;
using ShanMen.Buildings;
using ShanMen.Economy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShanMen.UI
{
    public sealed class BuildToolbar : MonoBehaviour
    {
        public BuildController buildController;

        readonly List<ButtonBinding> _buttons = new();
        Font _font;
        Text _hint;

        sealed class ButtonBinding
        {
            public int index;
            public Image image;
        }

        void Start()
        {
            if (buildController == null) buildController = FindFirstObjectByType<BuildController>();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            BuildUI();
            if (buildController != null) buildController.SelectionChanged += RefreshSelection;
            RefreshSelection();
        }

        void OnDisable()
        {
            if (buildController != null) buildController.SelectionChanged -= RefreshSelection;
        }

        void BuildUI()
        {
            EnsureEventSystem();
            var canvasGo = new GameObject("BuildToolbarCanvas");
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject panel = CreateUIObject("BuildToolbar", canvasGo.transform);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 18f);
            rect.sizeDelta = new Vector2(930f, 102f);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.08f, 0.1f, 0.92f);
            VerticalLayoutGroup vertical = panel.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(10, 10, 8, 8);
            vertical.spacing = 5f;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandHeight = false;

            GameObject row = CreateUIObject("Buttons", panel.transform);
            row.AddComponent<LayoutElement>().preferredHeight = 58f;
            HorizontalLayoutGroup horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 6f;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;

            if (buildController != null && buildController.definitions != null)
            {
                for (int i = 0; i < buildController.definitions.Length && i < 9; i++)
                {
                    BuildingDefinition definition = buildController.definitions[i];
                    if (definition == null) continue;
                    CreateBuildingButton(row.transform, i, definition);
                }
            }

            _hint = CreateText("Hint", panel.transform, 13, TextAnchor.MiddleCenter);
            _hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        }

        void CreateBuildingButton(Transform parent, int index, BuildingDefinition definition)
        {
            GameObject go = CreateUIObject($"Build_{definition.id}", parent);
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = 142f;
            layout.preferredHeight = 56f;
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.24f, 1f);
            Button button = go.AddComponent<Button>();

            Text text = CreateText("Label", go.transform, 13, TextAnchor.MiddleCenter);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);
            text.text = $"{index + 1}  {definition.displayName}\n{CostText(definition)}";

            int captured = index;
            button.onClick.AddListener(() => buildController?.SelectIndex(captured));
            _buttons.Add(new ButtonBinding { index = index, image = image });
        }

        void RefreshSelection()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                bool selected = buildController != null && buildController.SelectedIndex == _buttons[i].index;
                _buttons[i].image.color = selected
                    ? new Color(0.34f, 0.5f, 0.3f, 1f)
                    : new Color(0.18f, 0.2f, 0.24f, 1f);
            }

            if (_hint == null) return;
            if (buildController != null && buildController.Selected != null)
                _hint.text = $"已选择：{buildController.Selected.displayName}　左键放置 / 右键取消选择";
            else
                _hint.text = "右键点击工地可取消，并把已送达材料变成返料堆等待搬回仓库";
        }

        static string CostText(BuildingDefinition definition)
        {
            if (definition.buildCosts == null || definition.buildCosts.Length == 0) return "免费";
            string text = string.Empty;
            for (int i = 0; i < definition.buildCosts.Length; i++)
            {
                if (i > 0) text += "  ";
                text += $"{ResourceLabel(definition.buildCosts[i].type)}{definition.buildCosts[i].amount}";
            }
            return text;
        }

        static string ResourceLabel(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood: return "木";
                case ResourceType.Stone: return "石";
                case ResourceType.Food: return "食";
                case ResourceType.Herb: return "药";
                case ResourceType.SpiritStone: return "灵";
                default: return type.ToString();
            }
        }

        Text CreateText(string name, Transform parent, int size, TextAnchor anchor)
        {
            GameObject go = CreateUIObject(name, parent);
            Text text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
