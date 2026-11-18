using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Script.Serialization;

namespace Bruno
{
    // Candidate review through the actual companion Placement/Assets code.
    internal static class SmoothTransitionReview
    {
        sealed class Shot {public string Key;public int Delay;public Shot(string k,int d){Key=k;Delay=d;}}
        static List<Shot> Cycle(ArtManifest manifest,bool actions)
        {
            var result=new List<Shot>();
            Action<string,int> hold=delegate(string key,int delay){result.Add(new Shot(key,delay));};
            Action<string> clip=delegate(string key){var c=manifest.clips[key];for(int i=0;i<c.frames.Length;i++)hold(c.frames[i],(int)Math.Round(c.seconds[i]*100));};
            Action<string> awake=delegate(string pose){if(actions){foreach(string a in new[]{"blink","tilt","tongue"}){clip(pose+"-"+a);hold(pose,35);}}};
            hold("sit",120);awake("sit");clip("sit-to-lay");hold("lay",100);awake("lay");clip("lay-to-sleep");hold("sleep",140);clip("sleep-to-lay");hold("lay",100);clip("lay-to-sit");
            return result;
        }
        static int Total(List<Shot> shots){int t=0;foreach(var s in shots)t+=s.Delay;return t;}
        static string At(List<Shot> shots,int time){foreach(var s in shots){if(time<s.Delay)return s.Key;time-=s.Delay;}return shots[shots.Count-1].Key;}
        static void Starts(List<Shot> shots,SortedSet<int> times){int t=0;times.Add(t);foreach(var s in shots){t+=s.Delay;times.Add(t);}}
        static void Draw(Graphics g,Assets art,Placement placement,string key,int panel,float size)
        {
            Point location;using(Bitmap scene=placement.Paint(key,new PointF(panel*900+320,530),size,out location))g.DrawImageUnscaled(scene,location);
        }
        static void Export(Assets art,ArtManifest baseline,string directory,bool context)
        {
            string folder=Path.Combine(directory,context?"context":"cycle");Directory.CreateDirectory(folder);
            var before=Cycle(baseline,context);var after=Cycle(art.Manifest,context);
            if(Total(before)!=Total(after))throw new Exception("Transition duration changed");
            var starts=new SortedSet<int>();Starts(before,starts);Starts(after,starts);int[] times=new int[starts.Count];starts.CopyTo(times);
            var placement=new Placement(art);var delays=new List<int>();
            foreach(bool dark in new[]{false,true})
            {
                string dest=Path.Combine(folder,dark?"dark-frames":"light-frames");Directory.CreateDirectory(dest);
                for(int i=0;i<times.Length-1;i++)using(var b=new Bitmap(1800,630))using(var g=Graphics.FromImage(b))
                using(var ink=new SolidBrush(dark?Color.WhiteSmoke:Color.FromArgb(30,35,40)))using(var title=new Font("Segoe UI",20))using(var label=new Font("Segoe UI",13))
                using(var band=new SolidBrush(dark?Color.FromArgb(57,65,77):Color.FromArgb(160,174,190)))
                {
                    g.Clear(dark?Color.FromArgb(30,34,40):Color.FromArgb(242,244,246));g.FillRectangle(band,0,530,1800,100);
                    g.DrawString(baseline.frames.Count>21?"Previous review":"Current transitions",title,ink,24,18);g.DrawString(baseline.frames.Count>21?"Corrected transition":"With extra frames",title,ink,924,18);
                    g.DrawString("Same size and overall speed",label,ink,24,59);
                    int t=times[i];Draw(g,art,placement,At(before,t),0,.30f);Draw(g,art,placement,At(after,t),1,.30f);
                    b.Save(Path.Combine(dest,i.ToString("00")+".png"),ImageFormat.Png);
                    if(!dark)delays.Add(times[i+1]-times[i]);
                }
            }
            File.WriteAllText(Path.Combine(folder,"delays.json"),new JavaScriptSerializer().Serialize(delays));
        }
        public static void Run(Assets art,string directory)
        {
            Directory.CreateDirectory(directory);ArtManifest baseline;
            using(var s=Assets.Open("Bruno.BaselineManifest"))using(var reader=new StreamReader(s))baseline=new JavaScriptSerializer().Deserialize<ArtManifest>(reader.ReadToEnd());
            foreach(var old in baseline.frames){FrameInfo now=art.Manifest.frames[old.Key];if(now.sha256!=old.Value.sha256||now.scale!=old.Value.scale||now.supportX!=old.Value.supportX||now.supportY!=old.Value.supportY)throw new Exception("Approved frame/registration changed: "+old.Key);}
            foreach(string name in new[]{"sit-to-lay","lay-to-sit","lay-to-sleep","sleep-to-lay"})if(art.Manifest.clips[name].frames.Length!=5||Math.Abs(art.Manifest.clips[name].Duration-baseline.clips[name].Duration)>.000001)throw new Exception("Expected two extra keys with original total duration: "+name);
            ArtManifest comparison=baseline;
            if(System.Array.IndexOf(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames(),"Bruno.PreviousReviewManifest")>=0)
                using(var s=Assets.Open("Bruno.PreviousReviewManifest"))using(var reader=new StreamReader(s))comparison=new JavaScriptSerializer().Deserialize<ArtManifest>(reader.ReadToEnd());
            Export(art,comparison,directory,false);Export(art,comparison,directory,true);
            var keys=new List<string>();keys.Add("sit");keys.AddRange(art.Manifest.clips["sit-to-lay"].frames);keys.Add("lay");
            using(var b=new Bitmap(1920,1080))using(var g=Graphics.FromImage(b))
            using(var ink=new SolidBrush(Color.FromArgb(30,35,40)))using(var font=new Font("Segoe UI",17))
            using(var band=new SolidBrush(Color.FromArgb(160,174,190)))
            {
                g.Clear(Color.FromArgb(242,244,246));var placement=new Placement(art);
                for(int i=0;i<keys.Count;i++)
                {
                    int col=i%4,row=i/4;g.FillRectangle(band,col*480,row*540+450,480,90);
                    g.DrawString((i+1)+". "+keys[i],font,ink,col*480+15,row*540+12);
                    Point p;using(Bitmap scene=placement.Paint(keys[i],new PointF(col*480+160,row*540+450),.30,out p))g.DrawImageUnscaled(scene,p);
                }
                b.Save(Path.Combine(directory,"sit-sequence.png"),ImageFormat.Png);
            }
            File.WriteAllText(Path.Combine(directory,"preservation.json"),new JavaScriptSerializer().Serialize(new{passed=true,existingFramesUnchanged=baseline.frames.Count,newFrames=art.Manifest.frames.Count-baseline.frames.Count,intermediateFramesPerRoute=5,totalDurationsUnchanged=true,renderPath="Companion Assets and Placement",bodyContactUnchanged=true}));
        }
    }
}
