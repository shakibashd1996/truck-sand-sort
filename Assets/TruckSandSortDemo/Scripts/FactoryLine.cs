using UnityEngine;
using System.Collections.Generic;
using R=TruckSandSortDemo.FactoryGeometry.Ring;
namespace TruckSandSortDemo{
public sealed partial class FactoryPortrait {
 Transform hopperLoad;Transform[] beltPiles=new Transform[4], bins=new Transform[4],binLoads=new Transform[4],spares=new Transform[4];Transform badge,lid;
 static readonly float[] bx={176,374,570,764};
 void Machinery(){
 var baseT=Group("Equipment",scene,Vector3.zero);
 var hopper=Group("Hexagonal receiver",baseT,P(470,1090));Shadow(hopper,new Vector3(5,0,-16),306,171);
 Loft("Violet tapered casting",hopper,"A16BDF",2,Vector3.zero,new R(0,124,64,0),new R(7,132,69,0),new R(94,269,159,0),new R(143,330,194,0),new R(149,305,176,0),new R(54,122,64,0));
 Loft("Safety yellow rolled lip",hopper,Gold,2,Vector3.zero,new R(142,343,192,0),new R(147,356,202,0),new R(166,359,209,0),new R(172,351,205,0),new R(173,326,182,0),new R(169,312,165,0),new R(151,310,163,0));
 // The deep inner face and its top cap sit beneath the pearls.
 hopperLoad=Group("Receiver inventory",hopper,new Vector3(0,144,0));var pp=new List<Vector3>[3];var sz=new List<float>[3];for(int i=0;i<3;i++){pp[i]=new List<Vector3>();sz[i]=new List<float>();}
 for(int i=0;i<23;i++)for(int j=0;j<9;j++){float x=(i-11)*12+Rand(-3,3),z=(j-4)*13+Rand(-3,3);if(Mathf.Abs(x)>141-Mathf.Abs(z)*.61f)continue;int c=x+Rand(-30,30)<-44?0:x+Rand(-30,30)>34?2:1;pp[c].Add(new Vector3(x,5+14*(1-x*x/23000-z*z/6400)+Rand(-3,3),z-7));sz[c].Add(Rand(6.4f,8.4f));}
 for(int c=0;c<3;c++)Draw("Accumulated "+Pig[c],hopperLoad,Own(FactoryGeometry.Beads(pp[c],sz[c])),Pig[c],Vector3.zero);
 foreach(int s in new[]{-1,1})Ball("Rim fastener",hopper,"DBC7F4",new Vector3(s*94,150,-101),17,17,9);
 var belt=Group("Conveyor",baseT,P(470,1171));Shadow(belt,new Vector3(5,-1,-18),730,212);
 Box("Belt shell",belt,Lilac,new Vector3(0,2,0),686,47,147,33);
 Box("Recessed rubber belt",belt,"61468E",new Vector3(0,47,0),620,4,111,23);
 Box("Front belt housing",belt,"7142AF",new Vector3(0,8,-72),644,37,14,5);
 for(int i=0;i<2;i++){var t=Box("Gold drive roller",belt,Gold,new Vector3(i==0?-304:304,30,-65),80,15,80,0,1);t.localRotation=Quaternion.Euler(-90,0,0);Box("Axle cap",t,"FFAA05",new Vector3(0,14,0),43,7,43,0,1);}
 for(int i=0;i<3;i++)Chevron(332+i*145,1174,1.1f);
 var bp=new List<Vector3>();var bs=new List<float>();for(int c=0;c<4;c++){bp.Clear();bs.Clear();for(int j=0;j<42;j++){float x=Rand(-65,65),z=Rand(-34,26);bp.Add(new Vector3(x,5+Rand(0,4),z));bs.Add(Rand(4,6.3f));}
 beltPiles[c]=Draw("Conveyor pearls",belt,Own(FactoryGeometry.Beads(bp,bs)),Pig[c%3],new Vector3(0,51,0));}
 var rail=Route(25,V(178,1302),V(178,1261),V(763,1261),V(763,1302));Rail("Distributor • violet manifold",rail,45,31,Lilac);
 for(int i=0;i<4;i++){float x=bx[i];Rail("Delivery elbow",Route(12,V(x,1268),V(x,1309)),43,27,Lilac);nozzles[i]=Box("Brass nozzle",baseT,Gold,P(x,1307,35+(1558-1307)/C),57,16,53,6,1);}
 var plat=Group("Bucket stand",baseT,P(470,1581));Shadow(plat,new Vector3(5,0,-16),871,201);
 Box("Lilac plinth",plat,Lilac,Vector3.zero,827,32,166,33);Box("Recess",plat,"61409E",new Vector3(0,32,0),755,2,122,26);
 for(int i=0;i<4;i++){Bucket(baseT,i,1458,.76f,2);spares[i]=Bucket(baseT,i,1489,.84f,1);bins[i]=Bucket(baseT,i,1558,1,0);}
 }
 Transform Bucket(Transform parent,int i,float bottom,float scale,int queue){
 var t=Group(queue==0?"Active pail":"Queued pail",parent,P(bx[i],bottom,queue==0?35:14));t.localScale=Vector3.one*scale;string col=queue==0?Pig[i]:Mix(Pig[i],"AF98CA",queue==1?.78f:.91f);
 if(queue==0)Shadow(t,new Vector3(0,1,-4),169,90);
 Loft("Tapered pail",t,col,1,Vector3.zero,new R(0,115,71,0),new R(5,130,79,0),new R(16,137,84,0),new R(127,158,105,0),new R(141,155,105,0),new R(138,140,90,0),new R(45,114,67,0));
 Draw("Rolled pail rim",t,Own(FactoryGeometry.Loft(1,false,new R(119,156,101,0),new R(123,170,113,0),new R(143,174,117,0),new R(149,166,113,0),new R(150,151,97,0),new R(146,146,93,0),new R(129,143,90,0))),Mix(col,Ice,.13f),Vector3.zero);
 if(queue==0){var pts=new List<Vector3>();var sz=new List<float>();for(int j=0;j<87;j++){float a=Rand(0,6.283f),r=Mathf.Sqrt(Rand(0,1));pts.Add(new Vector3(66*r*Mathf.Cos(a),4+7*(1-r*r)+Rand(-2,2),40*r*Mathf.Sin(a)));sz.Add(Rand(5,7));}
 binLoads[i]=Draw("Pail sugar",t,Own(FactoryGeometry.Beads(pts,sz)),Pig[i],new Vector3(0,139,0));
 if(i==2){lid=Box("Snap-on cap",t,Mix(col,Ice,.24f),new Vector3(0,151,0),157,8,102,0,1);lid.gameObject.SetActive(false);}}
 return t;}
}
}



