using UnityEngine;
using System.Collections.Generic;
namespace TruckSandSortDemo {
internal static class FactoryGeometry {
 public struct Ring {public float x,y,z,w,d,r;public Ring(float h,float width,float depth,float radius,float dx=0,float dz=0){y=h;w=width;d=depth;r=radius;x=dx;z=dz;}}
 public static Vector2[] Shape(float w,float d,float r,int kind){
 var p=new List<Vector2>(); r=Mathf.Min(r,Mathf.Min(w,d)*.495f);
 if(kind==1){for(int i=0;i<56;i++){float a=i*Mathf.PI/28;p.Add(new Vector2(Mathf.Cos(a)*w/2,Mathf.Sin(a)*d/2));}}
 else if(kind==2){var poly=new[]{new Vector2(.50f,0),new Vector2(.35f,.50f),new Vector2(-.35f,.50f),new Vector2(-.50f,0),new Vector2(-.32f,-.50f),new Vector2(.32f,-.50f)};
 for(int i=0;i<6;i++){Vector2 a=poly[(i+5)%6],b=poly[i],c=poly[(i+1)%6];for(int j=0;j<=6;j++){float t=j/6f;var v=Vector2.Lerp(Vector2.Lerp(b,a,.13f),b,t)*(1-t)+Vector2.Lerp(b,Vector2.Lerp(b,c,.13f),t)*t;p.Add(new Vector2(v.x*w,v.y*d));}}}
 else {for(int i=0;i<4;i++){float cx=(i==0||i==3?1:-1)*(w/2-r),cz=(i<2?1:-1)*(d/2-r);for(int j=0;j<=8;j++){float a=(i+j/8f)*Mathf.PI/2;p.Add(new Vector2(cx+r*Mathf.Cos(a),cz+r*Mathf.Sin(a)));}}}
 return p.ToArray();}
 public static Mesh Loft(int kind,params Ring[] rings)=>Loft(kind,true,rings);
 public static Mesh Loft(int kind,bool capped,params Ring[] rings){var verts=new List<Vector3>();var tris=new List<int>();int n=0;
 foreach(var s in rings){var p=Shape(s.w,s.d,s.r,kind);n=p.Length;int q=verts.Count;
 for(int j=0;j<n;j++)verts.Add(new Vector3(p[j].x+s.x,s.y,p[j].y+s.z));
 if(q>0)for(int j=0;j<n;j++){int a=q-n+j,b=q-n+(j+1)%n,c=a+n,d=b+n;tris.AddRange(new[]{a,c,d,a,d,b});}}
 if(!capped)return Finish(verts,tris);
 int cap=verts.Count;var top=rings[rings.Length-1];verts.Add(new Vector3(top.x,top.y,top.z));for(int j=0;j<n;j++)tris.AddRange(new[]{cap,cap-n+(j+1)%n,cap-n+j});
 return Finish(verts,tris);}
 public static Mesh Finish(List<Vector3> v,List<int> t){var m=new Mesh();if(v.Count>65535)m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
 public static Mesh Bar(float w,float h,float d,float r,int shape=0){float b=Mathf.Min(h*.32f,Mathf.Min(5,Mathf.Min(w,d)*.12f));return Loft(shape,new Ring(0,w-b*1.7f,d-b*1.7f,r),new Ring(b,w,d,r),new Ring(h-b,w,d,r),new Ring(h-.22f*b,w-.7f*b,d-.7f*b,r),new Ring(h,w-2*b,d-2*b,r));}
 public static Mesh Beads(List<Vector3> pts,List<float> sizes){
 var verts=new List<Vector3>();var norm=new List<Vector3>();var tri=new List<int>();
 foreach(var center in pts){int o=verts.Count;float radius=sizes[o/77];for(int j=0;j<=6;j++){float a=j*Mathf.PI/6;
 for(int k=0;k<=10;k++){float u=k*Mathf.PI/5;var n=new Vector3(Mathf.Sin(a)*Mathf.Cos(u),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(u));verts.Add(center+radius*n);norm.Add(n);}}
 for(int j=0;j<6;j++)for(int k=0;k<10;k++){int a=o+j*11+k;tri.AddRange(new[]{a,a+1,a+11,a+1,a+12,a+11});}}
 var m=new Mesh();if(verts.Count>65535)m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;m.SetVertices(verts);m.SetNormals(norm);m.SetTriangles(tri,0);m.RecalculateBounds();return m;}
}
}


