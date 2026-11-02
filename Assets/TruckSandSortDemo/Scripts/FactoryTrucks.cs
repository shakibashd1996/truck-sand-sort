using UnityEngine;
using System.Collections.Generic;
using R=TruckSandSortDemo.FactoryGeometry.Ring;
namespace TruckSandSortDemo {
public sealed partial class FactoryPortrait {
 sealed class Truck {public Transform root,bed,load,halo;public Vector3 start;public float yaw;public Vector3 scale;}
 Truck[] trucks=new Truck[10];
 Transform parkingDeck; void Fleet(){parkingDeck=Group("Raised loading deck",scene,new Vector3(0,240,-240*C/S));trucks[0]=TruckAt("Cobalt • dock",0,470,750,-90,1.02f,1.05f);
 trucks[1]=TruckAt("Lower amber",1,266,750,0,1,1);trucks[2]=TruckAt("Crimson",2,678,600,90,1,1);
 trucks[3]=TruckAt("Amber",1,576,307,180,1,1);trucks[4]=TruckAt("Amethyst",4,320,454,0,1,1);
 trucks[5]=TruckAt("Azure",0,586,454,0,1,1);trucks[6]=TruckAt("Lime east",3,745,440,90,.92f,1.43f);
 trucks[7]=TruckAt("Lime west",3,199,602,-90,.95f,1.2f);trucks[8]=TruckAt("Tangerine",5,415,592,180,1.04f,1);
 trucks[9]=TruckAt("Rose",6,303,307,180,1,1);}
 Truck TruckAt(string name,int id,float x,float y,float yaw,float sc,float stretch){
 string col=Pig[id],rim=Mix(col,Ice,.24f);var root=Group(name,parkingDeck,P(x,y));root.localRotation=Quaternion.Euler(0,yaw,0);root.localScale=new Vector3(sc*stretch,sc,sc);
 var u=new Truck{root=root,start=root.localPosition,yaw=yaw,scale=root.localScale};
 Shadow(root,new Vector3(2,1,-4),239,139);
 u.halo=Box("Selected • amber edge",root,"FFA52B",new Vector3(0,3,0),221,3,106,20);Box("Selected • inner light",u.halo,"FFF3CC",new Vector3(0,3,0),216,2,100,17);u.halo.gameObject.SetActive(false);
 Box("Undercarriage",root,"483047",new Vector3(0,14,0),201,20,77,12);
 for(int i=-1;i<=1;i+=2)foreach(int side in new[]{-1,1}){
 float xx=i==1?68:-66;var tire=Box("Rubber wheel",root,Tyre,new Vector3(xx,18,side*40),35,12,35,0,1);tire.localRotation=Quaternion.Euler(side*90,0,0);
 Box("Wheel hub",tire,"9572CE",new Vector3(0,11.5f,0),17,4,17,0,1);}
 var a=new Vector3(72,22,0);Loft("Rounded cab and fenders",root,col,0,a,new R(0,59,81,15),new R(7,67,90,15),new R(23,65,89,14),new R(31,56,80,12,-2));
 Loft("Panoramic windshield",root,Glass,0,a,new R(27,57,80,13,-1),new R(34,59,82,13,-2),new R(54,46,72,10,-7));
 Loft("Enamel roof",root,rim,0,a,new R(52,49,76,12,-7),new R(59,48,74,12,-8),new R(63,42,66,11,-8));
 for(int side=-1;side<=1;side+=2){Box("Cab pillar",root,col,new Vector3(48,48,side*37),7,28,7,3);
 Box("Headlamp",root,"FFF8D6",new Vector3(103,32,side*28),5,10,16,3);}
 u.bed=Group("Hydraulic body",root,new Vector3(-34,23,0));Loft("Cargo tub",u.bed,col,0,Vector3.zero,new R(0,132,77,14),new R(7,144,90,15),new R(44,148,95,14),new R(54,145,94,14),new R(56,140,89,12),new R(53,130,78,10),new R(31,122,72,10));
 u.load=Group("Loose cargo",u.bed,new Vector3(0,48,0));var points=new List<Vector3>();var sizes=new List<float>();
 for(int i=0;i<12;i++)for(int j=0;j<7;j++){float bx=(i-5.5f)*10.45f+Rand(-2,2),bz=(j-3)*10.8f+Rand(-2,2);if(Mathf.Abs(bx)>53&&Mathf.Abs(bz)>29)continue;float yy=5+10*Mathf.Max(0,1-bx*bx/4300-bz*bz/1510)+Rand(-2,2);points.Add(new Vector3(bx,yy,bz));sizes.Add(Rand(6,7.4f));}
 Draw("Sugar pearls",u.load,Own(FactoryGeometry.Beads(points,sizes)),col,Vector3.zero);
 if(Mathf.Abs(Mathf.DeltaAngle(0,yaw))==90){Arrow(u.bed,new Vector3(0,78,0),true,1);}
 else {Arrow(u.bed,new Vector3(0,29,yaw==180?48:-48),false,yaw==180?-1:1);}
 return u;}
 void Arrow(Transform p,Vector3 at,bool top,int facing){
 var pts=new[]{new Vector2(-26,-5),new Vector2(6,-5),new Vector2(6,-13),new Vector2(11,-14),new Vector2(28,-1),new Vector2(28,2),new Vector2(11,15),new Vector2(6,13),new Vector2(6,6),new Vector2(-26,6),new Vector2(-28,3),new Vector2(-28,-3)};
 var v=new List<Vector3>();foreach(var q in pts)v.Add(top?new Vector3(q.x,0,q.y):new Vector3(q.x,q.y,0));
 int[] indices={0,1,8,0,8,9,0,9,10,0,10,11,1,2,3,1,3,4,1,4,5,1,5,8,8,5,6,8,6,7};
 var m=Own(FactoryGeometry.Finish(v,new List<int>(indices)));var norm=new Vector3[v.Count];for(int i=0;i<v.Count;i++)norm[i]=top?Vector3.up:new Vector3(0,0,-facing);m.normals=norm;
 Draw("Arrow • relief shadow",p,m,"A58BB8",at+(top?new Vector3(0,-1,-3):new Vector3(0,-2,0)));Draw("Porcelain direction arrow",p,m,Ice,at);}
}
}




