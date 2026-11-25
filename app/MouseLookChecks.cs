using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Script.Serialization;

namespace Bruno
{
    internal static class MouseLookChecks
    {
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static PointF Target(PointF head,double degrees)
        {double a=degrees*Math.PI/180;return new PointF(head.X+(float)(300*Math.Sin(a)),head.Y-(float)(300*Math.Cos(a)));}
        static void Settle(MouseLook look,Routine routine,bool enabled,PointF head,PointF target,double size)
        {for(int i=0;i<80;i++)look.Update(.016,routine,enabled,false,head,target,MouseLook.DeadZone(size));}
        internal static object Run(Assets art)
        {
            int directions=0,actions=0;var placement=new Placement(art);
            foreach(string pose in new[]{"sit","lay"})foreach(double size in new[]{.16,.24,.30,.36})
            foreach(PointF support in new[]{new PointF(400,1000),new PointF(-1500,700),new PointF(2300,1400),new PointF(300,-50),new PointF(-300,-200)})
            {
                var routine=new Routine(art,7){Automatic=false,DraftAnimations=false,SleepTransitions=false};routine.SetPose(pose,true);
                var look=new MouseLook();PointF head=MouseLook.Head(art,placement,pose,support,size);
                for(int index=0;index<12;index++)
                {
                    PointF target=Target(head,index*30);Settle(look,routine,true,head,target,size);
                    string expected="clock-"+pose+"-h"+(index==0?12:index).ToString("00");
                    Require(look.Index==index&&look.DisplayFrame(routine,true)==expected,"Mouse direction/frame mismatch: "+pose+" "+index);
                    Require(art.Manifest.frames.ContainsKey(expected)&&!art.Manifest.frames[expected].draft,"Unapproved gaze frame");
                    string frozen=look.DisplayFrame(routine,true);
                    for(int i=0;i<10;i++)look.Update(.1,routine,true,true,head,Target(head,(index+6)*30),15);
                    Require(look.DisplayFrame(routine,true)==frozen,"Menu/drag advanced gaze");
                    routine.Paused=true;look.Update(.1,routine,true,false,head,Target(head,(index+6)*30),15);
                    Require(look.DisplayFrame(routine,true)==frozen,"Pause advanced gaze");routine.Paused=false;
                    directions++;
                }
                Settle(look,routine,true,head,head,size);Require(look.DisplayFrame(routine,true)==pose,"Dead zone did not return to exact neutral");
                Settle(look,routine,true,head,Target(head,90),size);look.Update(.1,routine,false,false,head,Target(head,90),15);
                Require(look.DisplayFrame(routine,false)==pose&&look.Index==-1,"Mouse-look toggle did not reset");
            }
            foreach(string pose in new[]{"sit","lay"})
            {
                var routine=new Routine(art,2){Automatic=false,DraftAnimations=false,SleepTransitions=false};routine.SetPose(pose,true);
                var look=new MouseLook();PointF head=new PointF(-500,250);
                Settle(look,routine,true,head,Target(head,0),.3);
                look.Update(.1,routine,true,false,head,Target(head,16),15);Require(look.Desired==0,"Boundary jitter");
                look.Update(.1,routine,true,false,head,Target(head,23),15);Require(look.Desired==1,"Hysteresis did not release");
                Settle(look,routine,true,head,Target(head,0),.3);
                look.Update(.1,routine,true,false,head,Target(head,330),15);Require(look.Index==11,"Clock wrap took long way");
                foreach(string action in new[]{"blink","tilt","tongue"})
                {
                    routine.SetPose(pose,true);Settle(look,routine,true,head,Target(head,90),.3);
                    Require(routine.TryAction(action,true),"Expression setup failed");
                    look.Update(.1,routine,true,false,head,Target(head,90),15);
                    Require(look.DisplayFrame(routine,true)==routine.Frame&&!look.DisplayFrame(routine,true).StartsWith("clock-"),"Expression mixed with gaze");
                    for(int i=0;i<60&&routine.Action.Length!=0;i++)routine.Step(.1,false);
                    Settle(look,routine,true,head,Target(head,270),.3);
                    Require(look.Index==9,"Gaze failed to resume after expression");actions++;
                }
                routine.SetPose("sleep",true);look.Update(.1,routine,true,false,head,Target(head,90),15);
                Require(look.DisplayFrame(routine,true)=="sleep"&&look.Index==-1,"Sleep accepted gaze");
                routine.DraftAnimations=true;routine.SetPose("sit",true);
                look.Update(.1,routine,true,false,head,Target(head,90),15);
                Require(look.DisplayFrame(routine,true)==routine.Frame,"Transition mixed with gaze");
            }
            return new{passed=true,directionCases=directions,expressionResumeCases=actions,screenClockMapping=true,headAnchorUsesNeutralPose=true,negativeCoordinates=true,shortestWrap=true,hysteresis=true,deadZone=true,menuDragPauseFreeze=true,sleepAndTransitionsExcluded=true,wholeSpriteSelection=true};
        }
        internal static void Render(Assets art,string directory)
        {
            Directory.CreateDirectory(directory);var hours=new List<int>();var delays=new List<int>();
            int[] order={12,1,2,3,4,5,6,7,8,9,10,11};
            hours.Add(0);delays.Add(100);foreach(int h in order){hours.Add(h);delays.Add(65);}
            hours.Add(0);delays.Add(100);for(int i=11;i>=0;i--){hours.Add(order[i]);delays.Add(40);}hours.Add(0);delays.Add(100);
            var placement=new Placement(art);var sit=new Routine(art,1){Automatic=false};var lay=new Routine(art,1){Automatic=false,DraftAnimations=false};lay.SetPose("lay",true);
            var sitLook=new MouseLook();var layLook=new MouseLook();
            foreach(bool dark in new[]{false,true})
            {
                string dest=Path.Combine(directory,dark?"dark-frames":"light-frames");Directory.CreateDirectory(dest);
                for(int i=0;i<hours.Count;i++)
                {
                    PointF sitHead=MouseLook.Head(art,placement,"sit",new PointF(290,540),.30),layHead=MouseLook.Head(art,placement,"lay",new PointF(935,540),.30);
                    double degrees=hours[i]==12?0:hours[i]*30;
                    Settle(sitLook,sit,true,sitHead,hours[i]==0?sitHead:Target(sitHead,degrees),.30);
                    Settle(layLook,lay,true,layHead,hours[i]==0?layHead:Target(layHead,degrees),.30);
                    using(var b=new Bitmap(1400,640))using(var g=Graphics.FromImage(b))
                    using(var ink=new SolidBrush(dark?Color.WhiteSmoke:Color.FromArgb(30,35,40)))
                    using(var band=new SolidBrush(dark?Color.FromArgb(57,65,77):Color.FromArgb(160,174,190)))
                    using(var title=new Font("Segoe UI",21))using(var label=new Font("Segoe UI",16))
                    {
                        g.Clear(dark?Color.FromArgb(30,34,40):Color.FromArgb(242,244,246));g.FillRectangle(band,0,540,1400,100);
                        string direction=hours[i]==0?"Looking forward":hours[i]+" o'clock";
                        g.DrawString("Bruno looks around — "+direction,title,ink,24,18);
                        g.DrawString("Sitting",label,ink,195,65);g.DrawString("Lying down",label,ink,845,65);
                        Point p;using(var scene=placement.Paint(sitLook.DisplayFrame(sit,true),new PointF(290,540),.30,out p))g.DrawImageUnscaled(scene,p);
                        using(var scene=placement.Paint(layLook.DisplayFrame(lay,true),new PointF(935,540),.30,out p))g.DrawImageUnscaled(scene,p);
                        b.Save(Path.Combine(dest,i.ToString("00")+".png"),ImageFormat.Png);
                    }
                }
            }
            File.WriteAllText(Path.Combine(directory,"delays.json"),new JavaScriptSerializer().Serialize(delays));
        }
    }
}
