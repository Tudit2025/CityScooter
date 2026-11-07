using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace CityScooter.Audio
{
    /// <summary>Small in-game controls; all settings route through the same audio director.</summary>
    public sealed class CityAudioControls : MonoBehaviour
    {
        public CityAudioDirector audioDirector;
        GameObject panel;
        Text muteLabel;
        Font font;
        void Start()
        {
            if(audioDirector==null)audioDirector=GetComponent<CityAudioDirector>();
            if(audioDirector==null||audioDirector.bank==null)return;
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(FindAnyObjectByType<EventSystem>()==null)
            {
                var events=new GameObject("Audio UI EventSystem");events.transform.SetParent(transform,false);
                events.AddComponent<EventSystem>();events.AddComponent<InputSystemUIInputModule>();
            }
            var canvasGo=new GameObject("Audio Controls",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasGo.transform.SetParent(transform,false);
            var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30;
            var scaler=canvasGo.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var root=canvasGo.GetComponent<RectTransform>();
            Button(root,"Sound",new Vector2(-30,30),new Vector2(150,48),()=>
            {
                audioDirector.Engage();panel.SetActive(!panel.activeSelf);audioDirector.PlayCue("UI_Click");
                if(!panel.activeSelf)audioDirector.SavePreferences();
            });
            var rt=Rect(root,"Sound Settings",new Vector2(-30,88),new Vector2(340,300));panel=rt.gameObject;
            panel.AddComponent<Image>().color=new Color(.055f,.08f,.12f,.97f);
            Label(rt,"Sound",new Vector2(22,-12),new Vector2(296,40),26);
            Slider(rt,"Master",-70,audioDirector.Master,v=>audioDirector.SetVolumes(v,audioDirector.Music,audioDirector.Ambience,audioDirector.Effects));
            Slider(rt,"Music",-117,audioDirector.Music,v=>audioDirector.SetVolumes(audioDirector.Master,v,audioDirector.Ambience,audioDirector.Effects));
            Slider(rt,"City",-164,audioDirector.Ambience,v=>audioDirector.SetVolumes(audioDirector.Master,audioDirector.Music,v,audioDirector.Effects));
            Slider(rt,"Effects",-211,audioDirector.Effects,v=>audioDirector.SetVolumes(audioDirector.Master,audioDirector.Music,audioDirector.Ambience,v));
            var mute=Button(rt,"Mute [M]",new Vector2(-18,13),new Vector2(145,36),()=>{audioDirector.SetMuted(!audioDirector.Muted);audioDirector.PlayCue("UI_Click");});
            muteLabel=mute.GetComponentInChildren<Text>();panel.SetActive(false);
        }
        void Update(){if(muteLabel!=null)muteLabel.text=audioDirector.Muted?"Unmute [M]":"Mute [M]";}
        RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);
            rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(1,0);rt.anchoredPosition=position;rt.sizeDelta=size;return rt;
        }
        Text Label(Transform parent,string label,Vector2 position,Vector2 size,int fontSize=22)
        {
            var rt=Rect(parent,label,position,size);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);
            var text=rt.gameObject.AddComponent<Text>();text.font=font;text.fontSize=fontSize;text.text=label;text.color=new Color(.98f,.9f,.7f);text.raycastTarget=false;
            text.alignment=TextAnchor.MiddleLeft;return text;
        }
        Button Button(Transform parent,string label,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var rt=Rect(parent,label,position,size);rt.gameObject.AddComponent<Image>().color=new Color(.09f,.14f,.19f,.97f);
            var button=rt.gameObject.AddComponent<Button>();button.onClick.AddListener(action);button.navigation=new Navigation{mode=Navigation.Mode.None};
            var text=Label(rt,label,new Vector2(0,0),size);text.alignment=TextAnchor.MiddleCenter;return button;
        }
        void Slider(RectTransform parent,string label,float y,float value,UnityEngine.Events.UnityAction<float> changed)
        {
            Label(parent,label,new Vector2(20,y-4),new Vector2(95,30),20);
            var rt=Rect(parent,label+" Volume",new Vector2(128,y-8),new Vector2(190,26));rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);
            rt.gameObject.AddComponent<Image>().color=new Color(.2f,.26f,.3f);
            var fill=Rect(rt,"Fill",Vector2.zero,Vector2.zero);fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;fill.gameObject.AddComponent<Image>().color=new Color(.96f,.65f,.18f);
            var handle=Rect(rt,"Handle",Vector2.zero,new Vector2(12,4));handle.pivot=new Vector2(.5f,.5f);handle.gameObject.AddComponent<Image>().color=new Color(1,.94f,.78f);
            var slider=rt.gameObject.AddComponent<Slider>();slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.minValue=0;slider.maxValue=1;slider.SetValueWithoutNotify(value);slider.onValueChanged.AddListener(changed);
            slider.navigation=new Navigation{mode=Navigation.Mode.None};
        }
    }
}
