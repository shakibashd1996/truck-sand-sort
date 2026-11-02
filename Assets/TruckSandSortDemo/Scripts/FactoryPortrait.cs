using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R=TruckSandSortDemo.FactoryGeometry.Ring;
namespace TruckSandSortDemo {
public sealed partial class FactoryPortrait:MonoBehaviour{
 const float S=.5591929f,C=.8290376f;
 const string Lilac="AD7DF4",Gold="FFC000",Plum="422575",Ice="FFF9FF",Tyre="322040",Glass="302453";
 static readonly string[] Pig={"00ABFF","FFD016","FF354E","7BE411","A34CFA","FF8C10","FF65BA"};
 readonly Dictionary<string,Material> paint=new Dictionary<string,Material>();
 readonly Dictionary<string,Mesh> cache=new Dictionary<string,Mesh>();readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
 readonly System.Random rng=new System.Random(2037);
 Camera cam; Shader toy,shade;Transform scene; DemoSequenceController settings;
 static Vector3 P(float x,float y,float h=0)=>new Vector3(x-470.5f,h,(835.5f-y-h*C)/S);
 static Color Hex(string s){ColorUtility.TryParseHtmlString("#"+s,out var c);return c;}
 static string Mix(string a,string b,float t)=>ColorUtility.ToHtmlStringRGB(Color.Lerp(Hex(a),Hex(b),t));
 T Own<T>(T obj) where T:UnityEngine.Object{owned.Add(obj);return obj;}
 Transform Group(string n,Transform parent,Vector3 pos){var t=new GameObject(n).transform;t.SetParent(parent,false);t.localPosition=pos;return t;}
 Material Mat(string col,bool shadow=false){string k=(shadow?"s":"p")+col;if(!paint.TryGetValue(k,out var m)){m=Own(new Material(shadow?shade:toy));m.name=k;m.color=Hex(col);paint[k]=m;}return m;}
 Transform Draw(string n,Transform par,Mesh mesh,string col,Vector3 pos){var t=Group(n,par,pos);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(col);return t;}
 Transform Loft(string n,Transform p,string c,int k,Vector3 at,params R[] rings)=>Draw(n,p,Own(FactoryGeometry.Loft(k,rings)),c,at);
 Transform Box(string n,Transform p,string c,Vector3 at,float w,float h,float d,float r=6,int k=0){
 string key=$"{w}:{h}:{d}:{r}:{k}";if(!cache.TryGetValue(key,out var m))cache[key]=m=Own(FactoryGeometry.Bar(w,h,d,r,k));return Draw(n,p,m,c,at);}
 float Rand(float a,float b)=>Mathf.Lerp(a,b,(float)rng.NextDouble());
 Mesh glob;Transform Ball(string n,Transform p,string col,Vector3 at,float w,float h,float d){var t=Draw(n,p,glob,col,at);t.localScale=new Vector3(w,h,d)*.5f;return t;}
 Mesh smear;void Shadow(Transform p,Vector3 at,float w,float d,string color="16092E82"){
 var t=Draw("Soft contact",p,smear,"302050",at);t.localScale=new Vector3(w,1,d);t.GetComponent<Renderer>().sharedMaterial=Mat(color,true);}
 void Start(){settings=GetComponent<DemoSequenceController>();scene=Group("Portrait factory • physical set",transform,Vector3.zero);toy=Resources.Load<Shader>("FactoryToy");shade=Resources.Load<Shader>("FactoryShadow");
 glob=Own(FactoryGeometry.Beads(new List<Vector3>{Vector3.zero},new List<float>{1}));
 smear=Own(new Mesh());smear.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};smear.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};smear.triangles=new[]{0,2,1,0,3,2};smear.RecalculateNormals();
 cam=Group("Locked portrait camera",scene,Vector3.zero).gameObject.AddComponent<Camera>();cam.tag="MainCamera";cam.transform.rotation=Quaternion.Euler(34,0,0);cam.transform.position=-cam.transform.forward*3600;cam.orthographic=true;cam.nearClipPlane=5;cam.farClipPlane=8000;cam.backgroundColor=Hex("472B80");cam.clearFlags=CameraClearFlags.SolidColor;cam.allowHDR=false;Fit();
 Board();Machinery();Fleet();Overlay();AnimateAt(0);}
 void Fit(){cam.orthographicSize=Mathf.Max(835.5f,470.5f/Mathf.Max(cam.aspect,.05f));}
 void OnDestroy(){foreach(var x in owned)if(x)Destroy(x);}
 // A rounded polyline in reference-frame coordinates; the section has physical height and a bevel.
 List<Vector2> Route(float corner,params Vector2[] input){var a=new List<Vector2>{input[0]};for(int i=1;i<input.Length-1;i++){var v=input[i];var u=input[i-1]-v;var w=input[i+1]-v;float r=Mathf.Min(corner,Mathf.Min(u.magnitude,w.magnitude)*.49f);var p=v+u.normalized*r;var q=v+w.normalized*r;for(int j=0;j<=12;j++){float t=j/12f;a.Add(Vector2.Lerp(Vector2.Lerp(p,v,t),Vector2.Lerp(v,q,t),t));}}a.Add(input[input.Length-1]);return a;}
 static Vector2 V(float a,float b)=>new Vector2(a,b);
 void Rail(string name,List<Vector2> path,float width,float h,string color,float baseH=0){
 var v=new List<Vector3>();var tri=new List<int>();float[] ex={-.42f,-.5f,-.5f,-.47f,-.40f,.40f,.47f,.5f,.5f,.42f};float[] ey={0,.13f,.78f,.94f,1,1,.94f,.78f,.13f,0};int count=ex.Length;
 for(int i=0;i<path.Count;i++){Vector3 p=P(path[i].x,path[i].y,baseH);var prev=path[Mathf.Max(0,i-1)];var next=path[Mathf.Min(path.Count-1,i+1)];var tangent=(P(next.x,next.y)-P(prev.x,prev.y)).normalized;var side=new Vector3(-tangent.z,0,tangent.x);
 for(int k=0;k<count;k++)v.Add(p+side*(width*ex[k])+Vector3.up*h*ey[k]);
 if(i>0)for(int k=0;k<count-1;k++){int a=(i-1)*count+k,b=a+count;tri.AddRange(new[]{a,b,a+1,a+1,b,b+1});}}
 for(int k=1;k<count-1;k++){int e=v.Count-count;tri.AddRange(new[]{0,k+1,k,e,e+k,e+k+1});}
 var mesh=Own(FactoryGeometry.Finish(v,tri)); // normals must face up across the crown, regardless of path direction
 var normals=mesh.normals;if(normals[count+4].y<0){for(int i=0;i<normals.Length;i++)normals[i]=-normals[i];mesh.normals=normals;}
 Draw(name,scene,mesh,color,Vector3.zero);}
 void Board(){
 Box("Violet studio floor",scene,"50318A",P(470,835,-70),3300,5,5800,60);
 var arena=Route(112,V(389,888),V(389,832),V(115,832),V(115,181),V(824,181),V(824,690));var lip=Route(29,V(552,888),V(552,832),V(714,832));
 Rail("Arena • long return",arena,32,22,Lilac);Rail("Arena • short return",lip,32,22,Lilac);
 foreach(int x in new[]{199,306,413,528,636,742})Box("Mould joint",scene,"9973CE",P(x,163,22),1.6f,.6f,30,0);
 foreach(int y in new[]{243,346,447,548,644,736})foreach(int x in new[]{115,824})if(x==115||y<680)Box("Mould joint",scene,"9670C8",P(x,y,22),30,.7f,2,0);
 foreach(int x in new[]{389,552})Box("Outlet guard",scene,Gold,P(x,869),36,17,43,6);
 var routeA=Route(31,V(714,811),V(714,736),V(844,736),V(844,681));var routeB=Route(28,V(713,811),V(739,794),V(928,794),V(928,681));
 foreach(var path in new[]{routeA,routeB}){Rail("Amber route surround",path,13,3,"FF9B56");Rail("Exit luminous core",path,6,4,"FFF6F0",4);}
 for(int i=0;i<2;i++)Chevron(790+24*i,765,1);
 gate=Group("Sliding exit arm",scene,Vector3.zero); Box("Exit gate",gate,Ice,P(887,691),98,20,27,9);for(int i=0;i<3;i++){var t=Box("Barrier vermilion",gate,"FF5154",P(858+30*i,673,20),14,2,26,2);t.localRotation=Quaternion.Euler(0,-31,0);}
 Box("Exit junction",scene,Gold,P(713,810),30,16,38,6);
 Prop("Plant",settings.plantPrefab,47,396,88,92);Prop("Traffic cone",settings.conePrefab,869,893,72,81);Prop("Work light",settings.lampPrefab,53,1237,98,234);}
 void Prop(string n,GameObject prefab,float x,float bottom,float width,float height){if(!prefab)return;var t=Group(n,scene,P(x,bottom));var obj=Instantiate(prefab,t,false);obj.transform.localPosition=Vector3.zero;
 var rr=obj.GetComponentsInChildren<Renderer>();if(rr.Length==0)return;
 // Fit the imported mesh using its projected bounds, keeping the artist's FBX basis.
 float left=1e9f,right=-1e9f,lo=1e9f,hi=-1e9f;foreach(var r in rr){var b=r.localBounds;for(int i=0;i<8;i++){var l=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var p=t.InverseTransformPoint(r.transform.TransformPoint(l));float y=p.y*C+p.z*S;left=Mathf.Min(left,p.x);right=Mathf.Max(right,p.x);lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);}}
 float scale=Mathf.Min(width/Mathf.Max(.001f,right-left),height/Mathf.Max(.001f,hi-lo));obj.transform.localScale*=scale;obj.transform.localPosition=new Vector3(-(left+right)*.5f*scale,-lo*scale/C,0);Shadow(t,new Vector3(3,1,-7),width*1.25f,width);}
 void Chevron(float x,float y,float scale){foreach(int s in new[]{-1,1}){var t=Box("Direction pip",scene,Ice,P(x,y+s*5*scale,8),22*scale,2,8*scale,2);t.localRotation=Quaternion.Euler(0,-s*48,0);}}
}
}


