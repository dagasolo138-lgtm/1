using System.Collections.Generic;
using ShanMen.Economy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShanMen.UI
{
    public sealed class StockpilePanel : MonoBehaviour
    {
        public ResourceLedger ledger;

        readonly List<PriorityBinding> _priorityBindings = new();
        readonly List<FilterBinding> _filterBindings = new();
        RectTransform _panelRect;
        Transform _content;
        Font _font;
        int _knownStockpileCount = -1;
        float _nextRefresh;

        static readonly ResourceType[] ResourceColumns =
        {
            ResourceType.Wood,
            ResourceType.Stone,
            ResourceType.Food,
            ResourceType.Herb,
            ResourceType.SpiritStone
        };

        sealed class PriorityBinding
        {
            public ResourceContainer container;
            public Text text;
        }

        sealed class FilterBinding
        {
            public ResourceContainer container;
            public ResourceType type;
            public Image image;
            public Text text;
        }

        void Start()
        {
            if (ledger == null) ledger = FindFirstObjectByType<ResourceLedger>();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            BuildUI();
            RebuildRows();
        }

        void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.35f;

            int count = CountStockpiles();
            if (count != _knownStockpileCount) RebuildRows();
            RefreshBindings();
        }

        void BuildUI()
        {
            EnsureEventSystem();
            var canvasGo = new GameObject("StockpileCanvas");
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject panel = CreateUIObject("StockpilePanel", canvasGo.transform);
            _panelRect = panel.GetComponent<RectTransform>();
            _panelRect.anchorMin = new Vector2(0f, 1f);
            _panelRect.anchorMax = new Vector2(0f, 1f);
            _panelRect.pivot = new Vector2(0f, 1f);
            _panelRect.anchoredPosition = new Vector2(18f, -18f);
            _panelRect.sizeDelta = new Vector2(610f, 140f);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.08f, 0.1f, 0.92f);
            VerticalLayoutGroup vertical = panel.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(10, 10, 9, 9);
            vertical.spacing = 5f;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandHeight = false;

            Text title = CreateText("Title", panel.transform, 15, TextAnchor.MiddleLeft);
            title.text = "仓库物流：优先级越高越先接货；点击资源按钮切换是否接收";
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            GameObject header = CreateRow("Header", panel.transform, 30f);
            AddCell(header.transform, "仓库", 150f, TextAnchor.MiddleLeft);
            AddCell(header.transform, "优先", 64f, TextAnchor.MiddleCenter);
            for (int i = 0; i < ResourceColumns.Length; i++)
                AddCell(header.transform, ResourceLabel(ResourceColumns[i]), 66f, TextAnchor.MiddleCenter);

            GameObject contentGo = CreateUIObject("Rows", panel.transform);
            _content = contentGo.transform;
            VerticalLayoutGroup rows = contentGo.AddComponent<VerticalLayoutGroup>();
            rows.spacing = 4f;
            rows.childControlWidth = true;
            rows.childControlHeight = true;
            rows.childForceExpandHeight = false;
        }

        void RebuildRows()
        {
            if (_content == null || ledger == null) return;
            for (int i = _content.childCount - 1; i >= 0; i--) Destroy(_content.GetChild(i).gameObject);
            _priorityBindings.Clear();
            _filterBindings.Clear();

            int count = 0;
            for (int i = 0; i < ledger.Containers.Count; i++)
            {
                ResourceContainer container = ledger.Containers[i];
                if (container == null || container.role != ResourceContainerRole.Stockpile) continue;
                CreateStockpileRow(container, count++);
            }

            _knownStockpileCount = count;
            if (_panelRect != null) _panelRect.sizeDelta = new Vector2(610f, 92f + Mathf.Max(1, count) * 40f);
            RefreshBindings();
        }

        void CreateStockpileRow(ResourceContainer container, int index)
        {
            GameObject row = CreateRow($"Stockpile_{index}", _content, 36f);
            string label = container.transform.parent != null ? container.transform.parent.name : container.name;
            AddCell(row.transform, label, 150f, TextAnchor.MiddleLeft);

            Text priorityText;
            CreateButton(row.transform, 64f, out Image priorityImage, out priorityText, () =>
            {
                container.SetLogisticsPriority(container.logisticsPriority >= 5 ? 0 : container.logisticsPriority + 1);
                RefreshBindings();
            });
            priorityImage.color = new Color(0.22f, 0.25f, 0.3f, 1f);
            _priorityBindings.Add(new PriorityBinding { container = container, text = priorityText });

            for (int i = 0; i < ResourceColumns.Length; i++)
            {
                ResourceType type = ResourceColumns[i];
                CreateButton(row.transform, 66f, out Image image, out Text text, () =>
                {
                    container.SetAccepted(type, !container.Accepts(type));
                    RefreshBindings();
                });
                _filterBindings.Add(new FilterBinding { container = container, type = type, image = image, text = text });
            }
        }

        void RefreshBindings()
        {
            for (int i = 0; i < _priorityBindings.Count; i++)
            {
                PriorityBinding binding = _priorityBindings[i];
                if (binding.container == null || binding.text == null) continue;
                binding.text.text = binding.container.logisticsPriority.ToString();
            }

            for (int i = 0; i < _filterBindings.Count; i++)
            {
                FilterBinding binding = _filterBindings[i];
                if (binding.container == null || binding.image == null || binding.text == null) continue;
                bool accepted = binding.container.Accepts(binding.type);
                binding.text.text = accepted ? ResourceLabel(binding.type) : "×";
                binding.image.color = accepted
                    ? new Color(0.28f, 0.42f, 0.28f, 1f)
                    : new Color(0.28f, 0.18f, 0.18f, 1f);
            }
        }

        int CountStockpiles()
        {
            if (ledger == null) return 0;
            int count = 0;
            for (int i = 0; i < ledger.Containers.Count; i++)
            {
                ResourceContainer container = ledger.Containers[i];
                if (container != null && container.role == ResourceContainerRole.Stockpile) count++;
            }
            return count;
        }

        GameObject CreateRow(string name, Transform parent, float height)
        {
            GameObject row = CreateUIObject(name, parent);
            row.AddComponent<LayoutElement>().preferredHeight = height;
            HorizontalLayoutGroup horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 5f;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = true;
            return row;
        }

        Text AddCell(Transform parent, string value, float width, TextAnchor anchor)
        {
            Text text = CreateText("Cell", parent, 13, anchor);
            text.text = value;
            text.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
            return text;
        }

        void CreateButton(Transform parent, float width, out Image image, out Text text, UnityEngine.Events.UnityAction action)
        {
            GameObject go = CreateUIObject("Button", parent);
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = 30f;
            image = go.AddComponent<Image>();
            Button button = go.AddComponent<Button>();
            text = CreateText("Label", go.transform, 13, TextAnchor.MiddleCenter);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            button.onClick.AddListener(action);
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

        static string ResourceLabel(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood: return "木";
                case ResourceType.Stone: return "石";
                case ResourceType.Food: return "食";
                case ResourceType.Herb: return "药";
                case ResourceType.SpiritStone: return "灵石";
                default: return type.ToString();
            }
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
