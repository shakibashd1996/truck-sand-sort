using UnityEngine;
using UnityEngine.UI;
namespace TruckSandSortDemo{
public sealed partial class FactoryPortrait {
 RectTransform hud,barFill;RectTransform[] tags=new RectTransform[4];Text[] counts=new Text[4];Text amount;Font font;
 RectTransform Rect(string n,Transform par,float x,float y,float w,float h){var rt=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(par,false);rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.sizeDelta=new Vector2(w,h);rt.anchoredPosition=new Vector2(x,-y);return rt;}
 static float SD(float x,float y,float w,float h,float r){float a=Mathf.Abs(x)-w/2+r,b=Mathf.Abs(y)-h/2+r;return Mathf.Sqrt(Mathf.Max(a,0)*Mathf.Max(a,0)+Mathf.Max(b,0)*Mathf.Max(b,0))+Mathf.Min(Mathf.Max(a,b),0)-r;}
 RectTransform Plaque(string n,Transform p,float x,float y,int w,int h,float r,string top,string bottom,bool outline=true){
 var rt=Rect(n,p,x,y,w+14,h+14);var tex=Own(new Texture2D(w+14,h+14,TextureFormat.RGBA32,false));tex.wrapMode=TextureWrapMode.Clamp;tex.filterMode=FilterMode.Bilinear;
 Color a=Hex(top),b=Hex(bottom);var pix=new Color32[tex.width*tex.height];for(int j=0;j<tex.height;j++)for(int i=0;i<tex.width;i++){float xx=i+.5f-tex.width*.5f,yy=j+.5f-tex.height*.5f,d=SD(xx,yy,w,h,r);
 float t=Mathf.Clamp01(yy/h+.5f);Color c=Color.Lerp(b,a,t);if(outline){float edge=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((-d-1)/4));c=Color.Lerp(c,yy>0?Color.Lerp(a,Color.white,.57f):Color.Lerp(b,Color.black,.23f),edge*.86f);float shine=Mathf.Exp(-Mathf.Pow((t-.11f)*14,2));c=Color.Lerp(c,a,shine*.20f);}
 float coverage=Mathf.Clamp01(.5f-d);float shadow=(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((SD(xx-1,yy+3,w,h,r)+2)/7)))*.5f;
 Color s=Hex("251342");s.a=shadow;float alpha=coverage+shadow*(1-coverage);if(alpha>.0001f){c=(c*coverage+s*shadow*(1-coverage))/alpha;c.a=alpha;}else c=Color.clear;pix[j*tex.width+i]=c;}
 tex.SetPixels32(pix);tex.Apply(false,true);var img=rt.gameObject.AddComponent<RawImage>();img.texture=tex;img.raycastTarget=false;return rt;}
 Text Txt(string n,Transform p,string s,float x,float y,int size,Color col){var rt=Rect(n,p,x,y,350,90);var t=rt.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.fontStyle=FontStyle.Bold;t.alignment=TextAnchor.MiddleCenter;t.verticalOverflow=VerticalWrapMode.Overflow;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.color=col;t.text=s;t.raycastTarget=false;var o=t.gameObject.AddComponent<Outline>();o.effectColor=new Color(.15f,.065f,.24f,.88f);o.effectDistance=new Vector2(1.7f,-2);return t;}
 void Overlay(){var canvas=new GameObject("Factory HUD",typeof(RectTransform),typeof(Canvas)).GetComponent<Canvas>();canvas.transform.SetParent(scene,false);canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=cam;canvas.sortingOrder=100;
 hud=(RectTransform)canvas.transform;hud.sizeDelta=new Vector2(941,1671);hud.rotation=cam.transform.rotation;hud.position=cam.transform.position+cam.transform.forward*100;
 font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
 var gear=Plaque("Settings",hud,68-470.5f,65-835.5f,88,81,23,"C38AFE","783CD3");
 var cog=Rect("Gear silhouette",gear,0,-1,54,54);cog.gameObject.AddComponent<FactorySymbol>().kind=0;
 var level=Plaque("Level",hud,0,65-835.5f,305,81,41,"BB72FF","8031EB");Txt("Level number",level,"Level 20",0,-2,46,Color.white);
 var coin=Plaque("Currency",hud,793-470.5f,65-835.5f,250,76,36,"A267EB","6234AC");Txt("Balance",coin,"1,050",0,-2,35,Color.white);
 var token=Plaque("Token",coin,-87,-1,54,54,27,"FFF45A","FFAB00");var star=Rect("Embossed star",token,0,0,33,33);star.gameObject.AddComponent<FactorySymbol>().kind=1;
 var plus=Plaque("Add",coin,93,0,68,75,20,"FFE42C","FF9700");Txt("Plus",plus,"+",0,-3,59,Color.white);
 var meter=Plaque("Belt inventory",hud,719-470.5f,1025-835.5f,180,86,23,"B37CF7","673DA5");Plaque("Inset",meter,0,-1,163,69,15,"5B358D","482674",false);amount=Txt("Stock",meter,"15 / 30",28,-15,25,Color.white);
 for(int i=0;i<6;i++){int[] xx={-62,-52,-42,-57,-47,-52};int[] yy={-10,-10,-10,-17,-17,-24};Plaque("Grain",meter,xx[i],yy[i],11,11,5,"FFE93F","FFAD04",false);}
 Plaque("Gauge trough",meter,0,17,141,20,10,"452266","5F3F87");barFill=Plaque("Gauge fill",meter,-32,17,76,16,8,"FFF347","FFAC03");
 for(int i=0;i<4;i++){tags[i]=Plaque("Pail count "+i,hud,bx[i]-470.5f,1516-835.5f,99,51,15,Mix(Pig[i],"26162C",.46f),Mix(Pig[i],"231420",.54f),false);counts[i]=Txt("Capacity",tags[i],"0/3",0,-1,33,Color.white);}
 badge=Plaque("Completion seal",hud,bx[2]-470.5f,1555-835.5f,43,43,22,"92FF2E","28AF00");Rect("Tick",badge,0,0,32,29).gameObject.AddComponent<FactorySymbol>().kind=2;}
}
public sealed class FactorySymbol:MaskableGraphic{
 public int kind;
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();float w=rectTransform.rect.width*.5f;
 if(kind==2){var p=new[]{new Vector2(-.92f,0),new Vector2(-.64f,.26f),new Vector2(-.23f,-.13f),new Vector2(.68f,.81f),new Vector2(.96f,.56f),new Vector2(-.22f,-.70f)};
 foreach(var pt in p)vh.AddVert(pt*w,Color.white,Vector2.zero);vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,5);vh.AddTriangle(2,3,4);vh.AddTriangle(2,4,5);return;}
 int n=kind==0?48:10;for(int i=0;i<n;i++){float a=Mathf.PI/2+i*2*Mathf.PI/n;var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));float r=kind==0?(i%6==1||i%6==2||i%6==3?.99f:.81f):(i%2==0?1:.49f);var c=kind==0?Color.white:new Color(1,.72f,.02f);
 vh.AddVert(u*(kind==0?.34f*w:0),c,Vector2.zero);vh.AddVert(u*r*w,c,Vector2.zero);}
 for(int i=0;i<n;i++){int a=i*2,b=(i+1)%n*2;vh.AddTriangle(a,a+1,b+1);vh.AddTriangle(a,b+1,b);}}
}
}

