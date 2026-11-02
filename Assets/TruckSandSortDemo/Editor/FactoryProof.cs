using UnityEditor;
using UnityEngine;
using System.IO;
namespace TruckSandSortDemo.Editor {
public static class FactoryProof {
 [MenuItem("Tools/Truck Sand Sort/Save portrait frame %#g")]
 public static void Save(){
 var cam=Camera.main;if(!EditorApplication.isPlaying||!cam)return;
 var old=cam.targetTexture;var active=RenderTexture.active;
 var rt=RenderTexture.GetTemporary(1080,1920,24,RenderTextureFormat.ARGB32);
 var tex=new Texture2D(1080,1920,TextureFormat.RGB24,false);
 try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1080,1920),0,0);tex.Apply();
 var dir=Path.GetFullPath("Captures");Directory.CreateDirectory(dir);File.WriteAllBytes(Path.Combine(dir,"FactoryPortrait.png"),tex.EncodeToPNG());}
 finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
 }
}}

