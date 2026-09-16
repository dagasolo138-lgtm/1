using System.Collections.Generic;
using ShanMen.Characters;
using ShanMen.Economy;
using ShanMen.Jobs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShanMen.UI
{
    public sealed class WorkPriorityPanel : MonoBehaviour
    {
        public CultivatorAgent[] agents;
        public ResourceLedger ledger;

        readonly List<PriorityBinding> _priorityBindings = new();
        readonly List<AgentStatusBinding> _statusBindings = new();
        Text _resourceText;
        Font _font;
        float _nextRefresh;

        static readonly JobType[] JobColumns =
        {
            JobType.Haul,
            JobType.Build,
            JobType.Farm,
            JobType.Craft,
            JobType.Cultivate
        };

        sealed class PriorityBinding
        {
            public CultivatorAgent agent;
            public JobType type;
            public Text text;
        }

        sealed class AgentStatusBinding
        {
            public CultivatorAgent agent;
            public Text text;
        }

        void Start()
        {
            if (ledger == null) ledger = FindFirstObjectByType<ResourceLedger>();
            if (agents == null || agents.Length == 0) agents = FindObjectsByType<CultivatorAgent>(FindObjectsSortMode.None);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            BuildUI();
            RefreshAll();
        }

        void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.25f;
            RefreshAll();
        }

        void BuildUI()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("WorkPriorityCanvas");
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject panelGo = CreateUIObject("WorkPriorityPanel", canvasGo.transform);
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-18f, -18f);
            panelRect.sizeDelta = new Vector2(715f, 92f + Mathf.Max(1, agents.Length) * 46f);
            Image panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.09f, 0.11f, 0.92f);
            VerticalLayoutGroup vertical = panelGo.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(12, 12, 10, 10);
            vertical.spacing = 5f;
            vertical.childControlHeight = true;
            vertical.childControlWidth = true;
            vertical.childForceExpandHeight = false;

            _resourceText = CreateText("Resources", panelGo.transform, 15, TextAnchor.MiddleLeft);
            _resourceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            GameObject header = CreateRow("Header", panelGo.transform, 34f);
            AddCell(header.transform, "弟子", 86f, 14, TextAnchor.MiddleLeft);
            AddCell(header.transform, "当前 / 需求", 210f, 14, TextAnchor.MiddleLeft);
            for (int i = 0; i < JobColumns.Length; i++)
                AddCell(header.transform, CultivatorAgent.JobLabel(JobColumns[i]), 72f, 14, TextAnchor.MiddleCenter);

            for (int i = 0; i < agents.Length; i++)
            {
                CultivatorAgent agent = agents[i];
                if (agent == null) continue;
                GameObject row = CreateRow($"Agent_{i}", panelGo.transform, 40f);
                AddCell(row.transform, agent.displayName, 86f, 14, TextAnchor.MiddleLeft);
                Text status = AddCell(row.transform, string.Empty, 210f, 13, TextAnchor.MiddleLeft);
                _statusBindings.Add(new AgentStatusBinding { agent = agent, text = status });

                for (int j = 0; j < JobColumns.Length; j++)
                    CreatePriorityButton(row.transform, agent, JobColumns[j]);
            }
        }

        void CreatePriorityButton(Transform parent, CultivatorAgent agent, JobType type)
        {
            GameObject buttonGo = CreateUIObject($"{type}Button", parent);
            LayoutElement layout = buttonGo.AddComponent<LayoutElement>();
            layout.preferredWidth = 72f;
            layout.preferredHeight = 32f;
            Image image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.24f, 1f);
            Button button = buttonGo.AddComponent<Button>();

            Text text = CreateText("Value", buttonGo.transform, 15, TextAnchor.MiddleCenter);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            CultivatorAgent capturedAgent = agent;
            JobType capturedType = type;
            button.onClick.AddListener(() =>
            {
                int current = capturedAgent.GetPriority(capturedType);
                capturedAgent.SetPriority(capturedType, current >= 5 ? 0 : current + 1);
                RefreshAll();
            });

            _priorityBindings.Add(new PriorityBinding { agent = agent, type = type, text = text });
        }

        GameObject CreateRow(string name, Transform parent, float height)
        {
            GameObject row = CreateUIObject(name, parent);
            HorizontalLayoutGroup horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 5f;
            horizontal.childControlHeight = true;
            horizontal.childControlWidth = true;
            horizontal.childForceExpandHeight = true;
            horizontal.childForceExpandWidth = false;
            row.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        Text AddCell(Transform parent, string value, float width, int size, TextAnchor anchor)
        {
            Text text = CreateText(value, parent, size, anchor);
            text.text = value;
            text.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
            return text;
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

        void RefreshAll()
        {
            if (_resourceText != null && ledger != null)
            {
                _resourceText.text = $"库存  木 {ledger.Get(ResourceType.Wood)}   石 {ledger.Get(ResourceType.Stone)}   食 {ledger.Get(ResourceType.Food)}   药 {ledger.Get(ResourceType.Herb)}   灵石 {ledger.Get(ResourceType.SpiritStone)}";
            }

            for (int i = 0; i < _priorityBindings.Count; i++)
            {
                PriorityBinding binding = _priorityBindings[i];
                if (binding.agent == null || binding.text == null) continue;
                int value = binding.agent.GetPriority(binding.type);
                binding.text.text = value == 0 ? "×" : value.ToString();
            }

            for (int i = 0; i < _statusBindings.Count; i++)
            {
                AgentStatusBinding binding = _statusBindings[i];
                if (binding.agent == null || binding.text == null) continue;
                string carry = binding.agent.CarryingText;
                string state = string.IsNullOrEmpty(carry) ? binding.agent.CurrentJobName : $"{binding.agent.CurrentJobName} / {carry}";
                string needs = binding.agent.NeedText;
                binding.text.text = string.IsNullOrEmpty(needs) ? state : $"{state} / {needs}";
            }
        }
    }
}
