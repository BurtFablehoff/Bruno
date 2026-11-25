using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Script.Serialization;

namespace Bruno
{
    internal static class ClockLookReview
    {
        static readonly int[] Hours={12,1,2,3,4,5,6,7,8,9,10,11};
        static readonly string[] Names={"Up","Up/right","Right/up","Right","Right/down","Down/right","Down","Down/left","Left/down","Left","Left/up","Up/left"};
        static string Key(string pose,int hour){return hour==0?pose:"clock-"+pose+"-h"+hour.ToString("00");}
        static void Pet(Graphics g,Placement placement,string key,float x,float floor,double size)
        {Point p;using(var scene=placement.Paint(key,new PointF(x,floor),size,out p))g.DrawImageUnscaled(scene,p);}
        static void Sheet(Assets art,string directory,string pose,bool dark)
        {
            var placement=new Placement(art);
            using(var b=new Bitmap(1920,1620))using(var g=Graphics.FromImage(b))
            using(var ink=new SolidBrush(dark?Color.WhiteSmoke:Color.FromArgb(30,35,40)))
            using(var band=new SolidBrush(dark?Color.FromArgb(57,65,77):Color.FromArgb(160,174,190)))
            using(var title=new Font("Segoe UI",18))
            {
                g.Clear(dark?Color.FromArgb(30,34,40):Color.FromArgb(242,244,246));
                for(int i=0;i<12;i++)
                {
                    int x=i%4*480,y=i/4*540;g.FillRectangle(band,x,y+455,480,70);
                    g.DrawString(Hours[i]+" o'clock  "+Names[i],title,ink,x+18,y+15);
                    Pet(g,placement,Key(pose,Hours[i]),x+160,y+455,.27);
                }
                b.Save(Path.Combine(directory,pose+"-"+(dark?"dark":"light")+".png"),ImageFormat.Png);
            }
        }
        static void Cycle(Assets art,string directory,bool dark)
        {
            var hours=new List<int>();var delays=new List<int>();
            hours.Add(0);delays.Add(100);
            foreach(int h in Hours){hours.Add(h);delays.Add(65);}
            hours.Add(0);delays.Add(100);
            for(int i=11;i>=0;i--){hours.Add(Hours[i]);delays.Add(40);}
            hours.Add(0);delays.Add(100);
            string dest=Path.Combine(directory,dark?"dark-frames":"light-frames");Directory.CreateDirectory(dest);
            var placement=new Placement(art);
            for(int i=0;i<hours.Count;i++)using(var b=new Bitmap(1400,640))using(var g=Graphics.FromImage(b))
            using(var ink=new SolidBrush(dark?Color.WhiteSmoke:Color.FromArgb(30,35,40)))
            using(var band=new SolidBrush(dark?Color.FromArgb(57,65,77):Color.FromArgb(160,174,190)))
            using(var title=new Font("Segoe UI",21))using(var label=new Font("Segoe UI",16))
            {
                g.Clear(dark?Color.FromArgb(30,34,40):Color.FromArgb(242,244,246));g.FillRectangle(band,0,540,1400,100);
                string direction=hours[i]==0?"Looking forward":hours[i]+" o'clock";
                g.DrawString("Bruno looks around — "+direction,title,ink,24,18);
                g.DrawString("Sitting",label,ink,195,65);g.DrawString("Lying down",label,ink,845,65);
                Pet(g,placement,Key("sit",hours[i]),290,540,.30);
                Pet(g,placement,Key("lay",hours[i]),935,540,.30);
                b.Save(Path.Combine(dest,i.ToString("00")+".png"),ImageFormat.Png);
            }
            File.WriteAllText(Path.Combine(directory,"delays.json"),new JavaScriptSerializer().Serialize(delays));
        }
        [STAThread]static int Main(string[] args)
        {
            try
            {
                Directory.CreateDirectory(args[0]);using(var art=new Assets())
                {foreach(string pose in new[]{"sit","lay"})foreach(bool dark in new[]{false,true})Sheet(art,args[0],pose,dark);Cycle(art,args[0],false);Cycle(art,args[0],true);}
                return 0;
            }
            catch(Exception e){File.WriteAllText(args[0]+".error.txt",e.ToString());return 1;}
        }
    }
}
