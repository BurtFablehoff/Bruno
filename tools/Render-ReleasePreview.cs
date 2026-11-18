using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Script.Serialization;

namespace Bruno
{
    internal static class ReleasePreview
    {
        [STAThread] static void Main(string[] args)
        {
            string directory=args[0];Directory.CreateDirectory(directory);
            using(var art=new Assets())
            {
                var keys=new List<string>();var delays=new List<int>();
                Action<string,int> hold=delegate(string key,int delay){keys.Add(key);delays.Add(delay);};
                Action<string> clip=delegate(string key){var c=art.Manifest.clips[key];for(int i=0;i<c.frames.Length;i++)hold(c.frames[i],(int)Math.Round(c.seconds[i]*100));};
                hold("sit",120);clip("sit-to-lay");hold("lay",100);clip("lay-to-sleep");
                hold("sleep",140);clip("sleep-to-lay");hold("lay",100);clip("lay-to-sit");
                var placement=new Placement(art);
                for(int i=0;i<keys.Count;i++)using(var b=new Bitmap(800,560))using(var g=Graphics.FromImage(b))
                using(var band=new SolidBrush(Color.FromArgb(160,174,190)))
                using(var ink=new SolidBrush(Color.FromArgb(30,35,40)))using(var font=new Font("Segoe UI",18))
                {
                    g.Clear(Color.FromArgb(242,244,246));g.FillRectangle(band,0,470,800,90);
                    g.DrawString("Bruno",font,ink,24,18);Point location;
                    using(var scene=placement.Paint(keys[i],new PointF(325,470),.30,out location))g.DrawImageUnscaled(scene,location);
                    b.Save(Path.Combine(directory,i.ToString("00")+".png"),ImageFormat.Png);
                }
                File.WriteAllText(Path.Combine(directory,"delays.json"),new JavaScriptSerializer().Serialize(delays));
            }
        }
    }
}
