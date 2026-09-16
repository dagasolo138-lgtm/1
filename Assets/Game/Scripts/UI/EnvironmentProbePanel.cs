using ShanMen.Cultivation;
using ShanMen.FiveElements;
using ShanMen.Grid;
using UnityEngine;
using UnityEngine.UI;

namespace ShanMen.UI
{
    public sealed class EnvironmentProbePanel:MonoBehaviour
    {
        public Camera worldCamera;public GridMap grid;public QiField qiField;public FiveElementField fiveElementField;Text _text;Font _font;
        void Start(){if(worldCamera==null)worldCamera=Camera.main;if(grid==null)grid=FindFirstObjectByType<GridMap>();if(qiField==null)qiField=FindFirstObjectByType<QiField>();if(fiveElementField==null)fiveElementField=FindFirstObjectByType<FiveElementField>();_font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(_font==null)_font=Font.CreateDynamicFontFromOSFont("Arial",14);BuildUI();}
        void Update(){if(_text==null||worldCamera==null||grid==null)return;Ray ray=worldCamera.ScreenPointToRay(Input.mousePosition);Plane ground=new(Vector3.up,Vector3.zero);if(!ground.Raycast(ray,out float enter)){_text.text=string.Empty;return;}Vector3 world=ray.GetPoint(enter);GridPosition cell=grid.WorldToGrid(world);if(!grid.InBounds(cell)){_text.text=string.Empty;return;}float qi=qiField!=null?qiField.Sample(world):0f;ElementValues e=fiveElementField!=null?fiveElementField.Sample(world):default;string road=grid.HasRoad(cell)?"  道路":string.Empty;_text.text=$"格 {cell}  灵气 {qi:0.0}  木 {e.wood:0} 火 {e.fire:0} 土 {e.earth:0} 金 {e.metal:0} 水 {e.water:0}{road}";}
        void BuildUI(){var canvasGo=new GameObject("EnvironmentProbeCanvas");canvasGo.transform.SetParent(transform,false);Canvas canvas=canvasGo.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;CanvasScaler scaler=canvasGo.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600f,900f);scaler.matchWidthOrHeight=0.5f;canvasGo.AddComponent<GraphicRaycaster>();var panel=new GameObject("EnvironmentProbe",typeof(RectTransform));panel.transform.SetParent(canvasGo.transform,false);RectTransform rect=panel.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0.5f,1f);rect.anchorMax=new Vector2(0.5f,1f);rect.pivot=new Vector2(0.5f,1f);rect.anchoredPosition=new Vector2(0f,-18f);rect.sizeDelta=new Vector2(590f,34f);Image image=panel.AddComponent<Image>();image.color=new Color(0.08f,0.09f,0.11f,0.88f);image.raycastTarget=false;var textGo=new GameObject("Text",typeof(RectTransform));textGo.transform.SetParent(panel.transform,false);_text=textGo.AddComponent<Text>();_text.font=_font;_text.fontSize=14;_text.alignment=TextAnchor.MiddleCenter;_text.color=Color.white;_text.raycastTarget=false;RectTransform textRect=_text.rectTransform;textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=new Vector2(8f,0f);textRect.offsetMax=new Vector2(-8f,0f);}
    }
}
