using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using PetFramework;

namespace Bruno
{
    internal static class Checks
    {
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static byte[] Pixels(Bitmap bitmap) {
            byte[] result=new byte[bitmap.Width*bitmap.Height*4];var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try{for(int y=0;y<bitmap.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),result,y*bitmap.Width*4,bitmap.Width*4);}finally{bitmap.UnlockBits(data);}return result;
        }
        static Rectangle Bounds(byte[] data,int width,int height) {
            int left=width,top=height,right=0,bottom=0;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(data[(y*width+x)*4+3]>16){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x+1);bottom=Math.Max(bottom,y+1);}
            return Rectangle.FromLTRB(left,top,right,bottom);
        }
        static void Body(byte[] basis,byte[] frame,int width,int row,bool alpha) {
            Require(basis.Length==frame.Length,"Expression canvas drift");
            for(int i=0;i<basis.Length;i++)if((alpha&&i%4==3)||i>=row*width*4)Require(basis[i]==frame[i],"Approved expression body/alpha changed at "+i);
        }
        static void Order() {
            IntPtr self=new IntPtr(1);Rectangle body=new Rectangle(100,600,300,400);uint process=42;
            TopWindow pet=new TopWindow(self,"pet",body,true,process),tray=new TopWindow(new IntPtr(2),"Shell_TrayWnd",new Rectangle(0,1000,1920,80),true,50),popup=new TopWindow(new IntPtr(3),"popup",body,true,60),own=new TopWindow(new IntPtr(4),"menu",body,true,process),away=new TopWindow(new IntPtr(5),"popup",new Rectangle(10,10,40,40),true,70),plain=new TopWindow(new IntPtr(6),"plain",body,false,80),tiny=new TopWindow(new IntPtr(7),"tiny",new Rectangle(100,600,1,1),true,90);
            TopWindow[][] cases={new[]{tiny,tray,pet},new[]{pet,tray},new[]{popup,tray,pet},new[]{away,tray,pet},new[]{own,tray,pet},new[]{plain,tray,pet},new[]{pet},new[]{tray}};
            bool[] expected={true,false,false,true,true,true,false,false};
            for(int i=0;i<cases.Length;i++)Require(TaskbarOrder.ShouldRaise(cases[i],self,process,body)==expected[i],"Taskbar order case "+i);
        }
        static void Finish(Routine routine){for(int i=0;i<100&&routine.Action.Length>0;i++)routine.Step(.01,false);}
        public static void Run(Assets art,string path) {
            int frames=0,placements=0,routes=0,actions=0;var placement=new Placement(art);
            foreach(var pair in art.Manifest.frames) {
                using(Stream s=Assets.Open("Bruno.Art."+pair.Key))using(var sha=SHA256.Create())Require(BitConverter.ToString(sha.ComputeHash(s)).Replace("-","").ToLowerInvariant()==pair.Value.sha256,"Embedded digest: "+pair.Key);
                Bitmap b=art.Get(pair.Key);byte[] data=Pixels(b);Require(Bounds(data,b.Width,b.Height)==pair.Value.Bounds,"Visible bounds: "+pair.Key);frames++;
                foreach(string pose in new[]{"sit","lay"})if(pair.Key.StartsWith(pose+"-")) {
                    bool eye=pair.Key.Contains("blink"),mouth=pair.Key.Contains("tongue"),head=pair.Key.Contains("tilt");
                    if(eye||mouth||head)Body(Pixels(art.Get(pose)),data,b.Width,head?(pose=="sit"?660:610):(eye?(pose=="sit"?352:315):470),eye||mouth);
                }
            }
            Require(frames==50,"Unexpected runtime frame count");
            foreach(string pose in new[]{"sit","lay","sleep"}) {
                FrameInfo f=art.Manifest.frames[pose];double row=pose=="sit"?1265:pose=="lay"?905:900;
                Require(f.supportY==row&&f.Bounds.Bottom>row,"Body contact/paw overhang lost: "+pose);
                Point contact=pose=="sit"?new Point(550,1265):pose=="lay"?new Point(300,905):new Point(210,900);
                Require(art.Get(pose).GetPixel(contact.X,contact.Y).A>16,"Body contact landmark is transparent: "+pose);
                foreach(var pair in art.Manifest.frames)if(pair.Key.StartsWith(pose+"-blink")||pair.Key.StartsWith(pose+"-tilt")||pair.Key.StartsWith(pose+"-tongue"))Require(pair.Value.supportY==row,"Expression changed body contact");
            }
            foreach(var pair in art.Manifest.clips) {
                ClipInfo clip=pair.Value;Require(clip.frames.Length==clip.seconds.Length&&clip.Duration>0&&!clip.draft,"Invalid approved clip");
                foreach(string f in clip.frames)Require(art.Manifest.frames.ContainsKey(f),"Missing clip frame: "+f);
                foreach(double seconds in clip.seconds)Require(seconds>0,"Nonpositive frame duration");
            }
            foreach(string pose in new[]{"sit","lay"}) {
                Require(Math.Abs(art.Manifest.clips[pose+"-blink"].Duration-.20)<.000001,"Approved blink timing changed");
                Require(Math.Abs(art.Manifest.clips[pose+"-tilt"].Duration-2.82)<.000001,"Approved tilt timing changed");
                Require(Math.Abs(art.Manifest.clips[pose+"-tongue"].Duration-1.32)<.000001,"Approved tongue timing changed");
            }
            Require(Math.Abs(art.Manifest.clips["sit-to-lay"].Duration-.38)<.000001&&Math.Abs(art.Manifest.clips["lay-to-sleep"].Duration-.48)<.000001,"Approved posture timing changed");
            Rectangle[] monitors={new Rectangle(0,0,1920,1080),new Rectangle(-1920,-180,1920,1080),new Rectangle(1920,400,1280,1024),new Rectangle(0,0,420,600),new Rectangle(-300,0,300,500)};
            foreach(Rectangle area in monitors)foreach(double preferred in new[]{.16,.24,.30,.36})foreach(var pair in art.Manifest.frames)foreach(double requested in new[]{area.Left-10000.0,area.Right+10000.0}) {
                double scale=placement.Scale(area,preferred),x=placement.Clamp(area,requested,preferred);RectangleF sprite=placement.Sprite(pair.Key,new PointF((float)x,area.Bottom),scale);FrameInfo f=pair.Value;double k=f.scale*scale;
                Require(sprite.Left+f.Bounds.Left*k>=area.Left+3.8&&sprite.Left+f.Bounds.Right*k<=area.Right-3.8,"Side clipping: "+pair.Key);
                Require(sprite.Top+f.Bounds.Top*k>=area.Top+3.8,"Top clipping: "+pair.Key);
                Require(Math.Abs(sprite.Top+f.supportY*k-area.Bottom)<.01,"Floor support drift");placements++;
            }
            foreach(string start in new[]{"sit","lay","sleep"})foreach(string end in new[]{"sit","lay","sleep"}) {
                var r=new Routine(art,23){Automatic=false,DraftAnimations=false,SleepTransitions=false};r.SetPose(start,true);r.DraftAnimations=true;r.SleepTransitions=true;r.SetPose(end,true);
                var visited=new List<string>();
                for(int i=0;i<100&&r.InTransition;i++) {
                    if(!visited.Contains(r.Action))visited.Add(r.Action);
                    Require(!r.TryAction("tilt",true)&&!r.TryAction("blink",true)&&!r.TryAction("tongue",true),"Expression interrupted posture motion");
                    double age=r.Age;string frame=r.Frame;r.Step(.05,true);Require(r.Age==age&&r.Frame==frame,"Menu/drag advanced transition");
                    r.Paused=true;r.Step(.05,false);Require(r.Age==age,"Pause advanced transition");r.Paused=false;r.Step(.02,false);
                }
                Require(r.Pose==end&&r.Frame==end&&!r.InTransition,"Posture route failed: "+start+" -> "+end);
                if(start=="sit"&&end=="sleep")Require(visited.Contains("sit-to-lay")&&visited.Contains("lay-to-sleep"),"Sleep skipped lying");
                if(start=="sleep"&&end=="sit")Require(visited.Contains("sleep-to-lay")&&visited.Contains("lay-to-sit"),"Wake skipped lying");routes++;
            }
            foreach(string pose in new[]{"sit","lay"})foreach(string action in new[]{"blink","tilt","tongue"})for(int seed=0;seed<8;seed++) {
                var r=new Routine(art,seed){Automatic=false,DraftAnimations=false};r.SetPose(pose,true);Require(r.TryAction(action),"Awake action unavailable");
                double duration=r.ActionDuration;Require(r.Pose==pose,"Expression changed pose");double elapsed=0;
                while(r.Action.Length>0&&elapsed<5){r.Step(.01,false);elapsed+=.01;Require(r.Pose==pose,"Expression left awake pose");}
                Require(r.Action==""&&r.Frame==pose&&Math.Abs(elapsed-duration)<.011,"Expression timing/return differs");actions++;
            }
            var sleep=new Routine(art,4){DraftAnimations=false,SleepTransitions=false};sleep.SetPose("sleep",true);
            foreach(string a in new[]{"blink","tilt","tongue","smile","lick","missing"})Require(!sleep.TryAction(a),"Sleep accepted action");sleep.ObserveHover();Require(sleep.Action=="","Sleep reacted to hover");
            double stopped=sleep.Clock;sleep.Paused=true;sleep.Step(.1,false);Require(sleep.Clock==stopped,"Paused clock moved");sleep.Paused=false;sleep.Step(.1,true);Require(sleep.Clock==stopped,"Suppressed clock moved");
            double rest=0,total=0;int stateChanges=0,automaticActions=0,autonomousSleeps=0;
            for(int seed=0;seed<4;seed++) {
                var r=new Routine(art,seed);r.SelectAwake(seed%2==0?"sit":"lay");
                for(int tick=0;tick<864000;tick++) {
                    string previous=r.Pose;r.Step(.1,false);Require(Routine.ValidPose(r.Pose)&&art.Manifest.frames.ContainsKey(r.Frame),"Unavailable automatic pose/frame");
                    Require(r.Pose!="sleep"||r.Action==""||r.Action=="sleep-to-lay","Sleeping expression");
                    if(r.Action=="")rest+=.1;if(previous!=r.Pose)stateChanges++;total+=.1;
                }
                Require(r.Counts.ContainsKey("lay-to-sleep"),"Autonomous sleep missing");autonomousSleeps+=r.Counts["lay-to-sleep"];foreach(int count in r.Counts.Values)automaticActions+=count;
            }
            Require(rest/total>.90&&stateChanges>30&&automaticActions>100,"Quiet routine coverage insufficient");
            using(var hit=new Bitmap(3,3)){hit.SetPixel(1,1,Color.White);Require(LayeredSpriteWindow.IsOpaque(hit,new Point(1,1))&&!LayeredSpriteWindow.IsOpaque(hit,new Point(0,0)),"Alpha hit test");}
            object mouseLook=MouseLookChecks.Run(art);
            Order();Require(Program.Singleton=="Local\\"+Program.SettingsName&&Program.SettingsName.StartsWith("BrunoCompanion"),"Identity isolation failed");
            File.WriteAllText(path,new JavaScriptSerializer().Serialize(new{passed=true,mouseLook=mouseLook,embeddedFrames=frames,geometryPlacements=placements,postureRoutes=routes,awakeActionCases=actions,simulatedHours=96,restFraction=rest/total,stateChanges=stateChanges,automaticActions=automaticActions,autonomousSleeps=autonomousSleeps,approvedActionAndPostureTimings=true,approvedBodiesAndAlpha=true,sleepBlocksExpressions=true,pauseMenuDragGates=true,taskbarOrderCases=8,cacheLimit=Assets.CacheLimit,uiOpened=false,registryWritten=false,networkUsed=false,manualReviewRequired="Visible companion, clicks, drag, monitor changes and mixed-DPI feel"}));
        }
        public static void RenderPoseCycle(Assets art,string directory) {
            Directory.CreateDirectory(directory);var placement=new Placement(art);var keys=new List<string>();var delays=new List<int>();
            Action<string,int> hold=delegate(string key,int delay){keys.Add(key);delays.Add(delay);};
            Action<string> clip=delegate(string name){ClipInfo c=art.Manifest.clips[name];for(int i=0;i<c.frames.Length;i++)hold(c.frames[i],(int)Math.Round(c.seconds[i]*100));};
            hold("sit",120);clip("sit-to-lay");hold("lay",100);clip("lay-to-sleep");hold("sleep",140);clip("sleep-to-lay");hold("lay",100);clip("lay-to-sit");
            for(int i=0;i<keys.Count;i++)using(var b=new Bitmap(1000,650))using(var g=Graphics.FromImage(b))using(var font=new Font("Segoe UI",18))using(var ink=new SolidBrush(Color.FromArgb(30,35,40)))using(var line=new Pen(Color.Gray)) {
                g.Clear(Color.FromArgb(242,244,246));g.DrawString("Bruno - approved poses through the companion renderer",font,ink,24,14);Point at;using(Bitmap scene=placement.Paint(keys[i],new PointF(400,570),.34,out at))g.DrawImageUnscaled(scene,at);g.DrawLine(line,24,570,976,570);g.DrawString(keys[i],font,ink,24,602);b.Save(Path.Combine(directory,i.ToString("00")+".png"),ImageFormat.Png);
            }
            File.WriteAllText(Path.Combine(directory,"delays.json"),new JavaScriptSerializer().Serialize(delays));
        }
        public static void Render(Assets art,string directory){RenderPoseCycle(art,directory);}
        public static void RenderContact(Assets art,string path) {
            var placement=new Placement(art);
            using(var b=new Bitmap(1500,650))using(var g=Graphics.FromImage(b))using(var font=new Font("Segoe UI",18))using(var ink=new SolidBrush(Color.FromArgb(30,35,40)))using(var band=new SolidBrush(Color.FromArgb(160,174,190)))using(var line=new Pen(Color.FromArgb(60,75,95),2)) {
                g.Clear(Color.FromArgb(242,244,246));g.DrawString("Bruno - body contact at the taskbar edge; paws hang in front",font,ink,20,16);
                g.FillRectangle(band,0,550,1500,100);g.DrawLine(line,0,550,1500,550);
                string[] poses={"sit","lay","sleep"};for(int i=0;i<3;i++) {
                    Point location;using(Bitmap scene=placement.Paint(poses[i],new PointF(i*500+200,550),.30,out location))g.DrawImageUnscaled(scene,location);
                    g.DrawString(poses[i]=="sit"?"Seated butt":poses[i]=="lay"?"Chest / elbows":"Sleeping chest / elbows",font,ink,i*500+20,90);
                }
                b.Save(path,ImageFormat.Png);
            }
        }
    }
}
