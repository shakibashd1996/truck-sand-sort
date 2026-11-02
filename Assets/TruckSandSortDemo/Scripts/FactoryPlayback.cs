using UnityEngine;
using System.Collections.Generic;
namespace TruckSandSortDemo{
public sealed partial class FactoryPortrait{
 float clock;bool hold; readonly List<Jet> jets=new List<Jet>();readonly Transform[] nozzles=new Transform[4];
 sealed class Jet{public Transform root;public Transform[] beads;}
 void JetSet(int id,int count){var j=new Jet{root=Group("Moving sand • "+id,scene,Vector3.zero),beads=new Transform[count]};for(int i=0;i<count;i++){float d=Rand(6.5f,11);j.beads[i]=Ball("Grain",j.root,Pig[id],Vector3.zero,d,d,d);}jets.Add(j);}
 void Flow(int id,float t,Vector2 a,Vector2 b,float elevation,float spread,bool visible){var j=jets[id];j.root.gameObject.SetActive(visible);if(!visible)return;
 for(int i=0;i<j.beads.Length;i++){float f=Mathf.Repeat(i/(float)j.beads.Length+t*1.15f,1);float g=.27f*f+.73f*f*f;var pt=Vector2.Lerp(a,b,g);pt.x+=Mathf.Sin(i*14.73f+t*2)*spread*(.64f+.36f*f);pt.y+=Mathf.Cos(i*1.7f)*2;float h=elevation*(1-g)+185;j.beads[i].position=P(pt.x,pt.y,h);}}
 static float F(float a,float b,float t)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
 static float Pulse(float t)=>Mathf.Sin(Mathf.PI*Mathf.Clamp01(t));
 void Move(Truck u,float t,float a,float b,Vector2 from,Vector2 to,float ya,float yb){
 if(t<a)return;float f=F(a,b,t);u.root.localPosition=P(Mathf.Lerp(from.x,to.x,f),Mathf.Lerp(from.y,to.y,f));u.root.localRotation=Quaternion.Euler(0,Mathf.LerpAngle(ya,yb,f),0);}
 void Delivery(int i,float t,float arrive,float end,int stream=-1){var u=trucks[i];float unload=F(arrive,end,t);
 u.bed.localRotation=Quaternion.Euler(0,0,15*Pulse(unload));
 u.load.localScale=new Vector3(1,1-unload*.96f,1);u.load.localPosition=new Vector3(0,48-unload*26,0);
 u.halo.gameObject.SetActive(t>=arrive&&t<end);
 WorldStream(stream<0?i:stream,t,u.bed.TransformPoint(new Vector3(-72,42,0)),hopperLoad.TransformPoint(new Vector3(0,18,30)),t>=arrive&&t<end,5);
 }
 void LaterDelivery(int truckIndex,int stream,float time,float start,Vector2 bay,float yaw,float approach,float duration){
   var truck=trucks[truckIndex];
   float progress=(time-start)/approach;
   // Descend the cleared side aisle, then turn into the dock. West backs out first.
   Move(truck,progress,0,.4f,bay,V(bay.x,735),yaw,yaw);
   float inward=bay.x>470?180:0;
   Move(truck,progress,.4f,.76f,V(bay.x,735),V(470,735),yaw,inward);
   Move(truck,progress,.76f,.88f,V(470,735),V(470,735),inward,-90);
   Move(truck,progress,.88f,1,V(470,735),V(470,808),-90,-90);
   float arrive=start+approach,end=arrive+duration;
   Delivery(truckIndex,time,arrive,end,stream);
   ExitDock(truck,(time-end)*(truckIndex==5?1.65f:1));
 }
 // Each grain leaves at a known time, accelerates, skips on the mound, and settles.
 // Sampling by time keeps scrubbing and replay identical and lets the last grains land.
 void PourIntoPail(int lane,float time,float start,float end,Vector3 lip,Vector3 impact){
   Jet jet=jets[4+lane];const float flight=.39f,settle=.20f,period=.64f;
   bool visible=time>=start&&time<end;
   jet.root.gameObject.SetActive(visible);
   if(!visible)return;
   for(int k=0;k<jet.beads.Length;k++){
     float offset=period*Mathf.Repeat(k*.618034f+lane*.19f,1);
     float age=Mathf.Repeat(time-start-offset,period);
     float born=time-age;
     var bead=jet.beads[k];
     bool emitted=born>=start&&born<=end-flight-settle&&age<flight+settle;
     bead.gameObject.SetActive(emitted);if(!emitted)continue;
     float cycle=Mathf.Floor((born-start+.001f)/period);
     float angle=k*2.39996f+cycle*.83f;
     var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
     float scatter=3+9*Mathf.Repeat(k*.413f,1);
     Vector3 contact=impact+radial*scatter;
     if(age<flight){
       float u=age/flight;
       bead.position=Vector3.Lerp(lip+radial*2.5f,contact,.12f*u+.88f*u*u);
     } else {
       float u=(age-flight)/settle;
       bead.position=contact+radial*(u*(7+scatter))+Vector3.up*(4*u*(1-u)*(3+scatter*.45f)-u*4);
     }
   }
 }
 void WorldStream(int id,float t,Vector3 source,Vector3 target,bool on,float width){
 var jet=jets[id];jet.root.gameObject.SetActive(on);if(!on)return;
 for(int k=0;k<jet.beads.Length;k++){float phase=Mathf.Repeat(t*1.35f+k/(float)jet.beads.Length,1);
 var p=Vector3.Lerp(source,target,phase);p.y=Mathf.Lerp(source.y,target.y,phase*phase)+12*phase*(1-phase);
 jet.beads[k].position=p+new Vector3(Mathf.Sin(k*13.7f)*width,0,Mathf.Cos(k*6.9f)*width);}
 }
 Transform gate;RectTransform tapCue,stopCue;float lastTap=-100;
 Renderer[] spareShell;MaterialPropertyBlock coat;
 static float GateOpen(float elapsed)=>F(.8f,1.8f,elapsed)-F(4.9f,5.6f,elapsed);
 void ExitDock(Truck truck,float elapsed){
   truck.root.gameObject.SetActive(elapsed<4.9f);if(elapsed<0)return;
   // Tangents align the cab with the road throughout both bends.
   if(elapsed<1.2f)DriveCurve(truck,F(0,1.2f,elapsed),V(470,808),V(470,770),V(501,765),V(568,765));
   else if(elapsed<3.6f)DriveCurve(truck,F(1.2f,3.6f,elapsed),V(568,765),V(727,765),V(882,792),V(882,707));
   else DriveCurve(truck,F(3.6f,4.9f,elapsed),V(882,707),V(882,643),V(999,630),V(1080,630));
 }
 void DriveCurve(Truck truck,float f,Vector2 a,Vector2 b,Vector2 c,Vector2 d){
   Vector2 ab=Vector2.Lerp(a,b,f),bc=Vector2.Lerp(b,c,f),cd=Vector2.Lerp(c,d,f);
   Vector2 left=Vector2.Lerp(ab,bc,f),right=Vector2.Lerp(bc,cd,f),pos=Vector2.Lerp(left,right,f),dir=right-left;
   truck.root.localPosition=P(pos.x,pos.y);truck.root.localRotation=Quaternion.Euler(0,Mathf.Atan2(dir.y/S,dir.x)*Mathf.Rad2Deg,0);
 }
 void BlockedFeedback(float t){
   if(!tapCue){
     coat=new MaterialPropertyBlock();spareShell=spares[2].GetComponentsInChildren<Renderer>();
     tapCue=Plaque("Tap feedback",hud,320-470.5f,385-835.5f,66,32,16,"AF81FF","6943AE");Txt("Tap",tapCue,"TAP",0,0,18,Color.white);
     stopCue=Plaque("Lane occupied",hud,449-470.5f,422-835.5f,105,34,12,"FC5359","BE273C");Txt("Occupied",stopCue,"BLOCKED",0,0,18,Color.white);
   }
#if ENABLE_INPUT_SYSTEM
   var pointer=UnityEngine.InputSystem.Pointer.current;
   if(t<52.6f&&pointer!=null&&pointer.press.wasPressedThisFrame){
     var pixel=cam.WorldToScreenPoint(trucks[4].root.position+Vector3.up*42);
     Vector2 delta=(pointer.position.ReadValue()-(Vector2)pixel)*(2*cam.orthographicSize/cam.pixelHeight);
     if(Mathf.Abs(delta.x)<108&&Mathf.Abs(delta.y)<62)lastTap=t;
   }
#endif
   float age=t-lastTap;if(t>=50&&t<52.5f)age=Mathf.Min(age<0?100:age,t-50);
   bool tapped=age>=0&&age<2.5f;tapCue.gameObject.SetActive(tapped&&age<.8f);stopCue.gameObject.SetActive(tapped&&age>.18f);
   tapCue.localScale=Vector3.one*(1+.22f*Pulse(age/.5f));
   trucks[4].halo.gameObject.SetActive(tapped);if(t<52.6f)trucks[5].halo.gameObject.SetActive(tapped&&age>.18f);
 }
 void Update(){if(!cam||hold)return;float speed=settings?settings.playbackSpeed:1;clock+=Time.unscaledDeltaTime*speed;
 if(clock>60+(settings?settings.finalHoldSeconds:3)&&settings&&settings.autoReplay)clock=0;
 AnimateAt(Mathf.Min(clock,60));}
 public void PreviewTime(float seconds){hold=true;AnimateAt(Mathf.Clamp(seconds,0,60));}
 void AnimateAt(float t){Fit();if(jets.Count==0){JetSet(0,32);JetSet(1,32);JetSet(2,32);JetSet(0,15);for(int i=0;i<4;i++)JetSet(i,48);JetSet(2,19);JetSet(3,32);JetSet(3,32);JetSet(0,32);}
 for(int i=0;i<trucks.Length;i++){var u=trucks[i];u.root.localPosition=u.start;u.root.localScale=u.scale;u.root.localRotation=Quaternion.Euler(0,u.yaw,0);}
 // All three reverse into the central dock. Loaded and empty journeys stay inside the curb.
 Move(trucks[0],t,5,9,V(470,750),V(470,808),-90,-90);Delivery(0,t,9,12);
 ExitDock(trucks[0],t-12);
 for(int i=1;i<=2;i++){
   var u=trucks[i];float start=i==1?16:24;
   if(i==1) Move(u,t,start,start+1.4f,V(266,750),V(470,750),0,0);
   else {
     Move(u,t,start,start+.65f,V(678,600),V(678,750),90,90);
     Move(u,t,start+.65f,start+1.65f,V(678,750),V(470,750),90,180);
   }
   Move(u,t,start+(i==1?1.4f:1.65f),start+2.4f,V(470,750),V(470,750),i==1?0:180,-90);
   Move(u,t,start+2.4f,start+3,V(470,750),V(470,808),-90,-90);
   Delivery(i,t,start+3,start+5);ExitDock(u,t-start-5);
 }
 // Dispatch trucks whose lane has cleared. The azure truck stays put for the blocked tap.
 LaterDelivery(6,9,t,34,V(745,440),90,3.4f,2.4f);
 LaterDelivery(7,10,t,43,V(199,602),-90,3.2f,2.3f);
 LaterDelivery(5,11,t,52.6f,V(586,454),0,2.3f,2.0f);
 float opening=Mathf.Max(GateOpen(t-12),Mathf.Max(GateOpen(t-21),GateOpen(t-29)));
 opening=Mathf.Max(opening,Mathf.Max(GateOpen(t-39.8f),Mathf.Max(GateOpen(t-48.5f),GateOpen((t-56.9f)*1.65f))));
 gate.localPosition=Vector3.right*140*opening;
 BlockedFeedback(t);
 float total=F(9,12,t)+F(19,21,t)+F(27,29,t)+F(37.4f,39.8f,t)+F(46.2f,48.5f,t)+F(54.9f,56.9f,t);
 hopperLoad.localPosition=new Vector3(0,144+total*2-4*F(30,44,t),0);
 Flow(3,t,V(469,1092),V(464,1130),45,9,true);
 for(int i=0;i<4;i++){var p=beltPiles[i].localPosition;p.x=-207+Mathf.Repeat(i*139+t*43,556);if(p.x>250)p.x-=556;p.x=Mathf.Clamp(p.x,-227,227);beltPiles[i].localPosition=p;}
 float departure=F(44.8f,47,t),advance=F(46.3f,48.8f,t);
 bool next=t>=47;
 for(int i=0;i<4;i++){
   float start,end,baseUnits,dose;
   if(i==0){bool late=t>=56;start=late?56:12;end=late?59.9f:17;baseUnits=late?2.4f:2;dose=late?.48f:.4f;}
   else if(i==1){start=21;end=27;baseUnits=1;dose=1;}
   else if(i==2){start=next?49:30;end=next?57:42;baseUnits=next?0:2;dose=1;}
   else {bool second=t>=49;start=second?49:39.8f;end=second?54:45;baseUnits=second?1:0;dose=1;}
   float units=baseUnits+dose*F(start,end,t);
   int cap=Mathf.Min(3,Mathf.FloorToInt(units+.00001f));
   var receiver=bins[i];float bottom=1558;
   if(i==2){
     bottom+=322*departure-6*Pulse((t-44.5f)/.3f);
     bins[i].localPosition=P(bx[i],bottom,35);
     bins[i].gameObject.SetActive(t<47);
     float spareY=Mathf.Lerp(1489,1558,advance);
     spares[i].localPosition=P(bx[i],spareY,Mathf.Lerp(14,35,advance));
     spares[i].localScale=Vector3.one*Mathf.Lerp(.84f,1,advance);
     var tint=Color.Lerp(Hex(Mix(Pig[i],"AF98CA",.78f)),Hex(Pig[i]),advance);
     foreach(var renderer in spareShell){coat.SetColor("_Color",renderer==spareShell[0]?tint:Color.Lerp(tint,Color.white,.13f));renderer.SetPropertyBlock(coat);}
     if(next){receiver=spares[i];bottom=spareY;}
     binLoads[i].SetParent(receiver,false);
   }
   tags[i].anchoredPosition=new Vector2(bx[i]-470.5f,835.5f-bottom+42);counts[i].text=cap+"/3";
   float fraction=units/3;
   Transform mound=binLoads[i];
   mound.gameObject.SetActive(units>.002f);
   mound.localPosition=new Vector3(0,Mathf.Lerp(65,141,Mathf.Sqrt(fraction)),0);
   float radius=Mathf.Lerp(.18f,1,Mathf.Sqrt(fraction));
   mound.localScale=new Vector3(radius,Mathf.Lerp(.4f,1,fraction),radius);
   PourIntoPail(i,t,start,end,nozzles[i].position,mound.TransformPoint(Vector3.up*12));
 }
 badge.gameObject.SetActive(t>=42&&t<47);badge.localPosition=tags[2].localPosition+Vector3.down*40;
 lid.gameObject.SetActive(t>=44.5f&&t<47);
 int stock=Mathf.Clamp(Mathf.RoundToInt(15+4*total-9*F(30,45,t)),0,30);amount.text=stock+" / 30";float f=stock/30f;barFill.localScale=new Vector3(f*1.88f,1,1);barFill.anchoredPosition=new Vector2(-69+71*f,-17);
 var burst=jets[8];float phase=t-44;burst.root.gameObject.SetActive(phase>=0&&phase<1.2f);if(phase>=0&&phase<1.2f)for(int k=0;k<burst.beads.Length;k++){float a=k*2.4f;burst.beads[k].position=P(bx[2]+Mathf.Cos(a)*phase*88,1430-phase*(110+37*Mathf.Sin(a))+66*phase*phase,200);}
 }
}
}





