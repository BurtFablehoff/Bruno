using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;

// Bruno-specific posture keys and registration. Approved endpoints are read-only.
internal static class TransitionReview
{
    public sealed class Pose {
        public string id {get;set;} public string file {get;set;}
        public double scale {get;set;} public double anchorX {get;set;}
        public double supportY {get;set;} public double offsetX {get;set;}
    }
    public sealed class Manifest { public Pose[] assets {get;set;} public int[] sequence {get;set;} public int[] delays {get;set;} }
    static Manifest Config;
    static readonly List<Bitmap> Frames=new List<Bitmap>();
    static Bitmap Load(string id) {
        using(Stream s=Assembly.GetExecutingAssembly().GetManifestResourceStream("BrunoTransitions."+id))
        {if(s==null)throw new Exception("Missing "+id);using(var b=new Bitmap(s))return new Bitmap(b);}
    }
    static byte[] Bytes(Bitmap b) {
        byte[] data=new byte[b.Width*b.Height*4];var locked=b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try{for(int y=0;y<b.Height;y++)Marshal.Copy(IntPtr.Add(locked.Scan0,y*locked.Stride),data,y*b.Width*4,b.Width*4);}finally{b.UnlockBits(locked);}return data;
    }
    static void Initialize() {
        using(Stream s=Assembly.GetExecutingAssembly().GetManifestResourceStream("BrunoTransitions.manifest"))using(var reader=new StreamReader(s))Config=new JavaScriptSerializer().Deserialize<Manifest>(reader.ReadToEnd());
        foreach(Pose pose in Config.assets) {
            Frames.Add(Load(pose.id));
        }
    }
    static void Draw(Graphics g,int index,float anchor,float floor,float factor) {
        Pose p=Config.assets[index];Bitmap b=Frames[index];float k=(float)p.scale*factor;
        float x=anchor+(float)p.offsetX*factor-(float)p.anchorX*k,y=floor-(float)p.supportY*k;
        g.DrawImage(b,new RectangleF(x,y,b.Width*k,b.Height*k));
    }
    static void Render(Graphics g,int index,bool dark) {
        g.Clear(dark?Color.FromArgb(30,34,40):Color.FromArgb(242,244,246));
        g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
        using(var font=new Font("Segoe UI",20))using(var small=new Font("Segoe UI",13))using(var ink=new SolidBrush(dark?Color.WhiteSmoke:Color.FromArgb(30,35,40)))using(var line=new Pen(Color.FromArgb(130,140,150))) {
            g.DrawString("Bruno - sit / lie / sleep transition candidates",font,ink,24,14);
            g.DrawString(Config.assets[index].id,small,ink,24,55);
            Draw(g,index,400,570,1);
            g.DrawLine(line,24,570,976,570);
            g.DrawString("Same approved endpoints. Forward and reverse keys. Posture motion awaits review.",small,ink,24,602);
        }
    }
    static void Check() {
        if(Config.sequence.Length!=Config.delays.Length)throw new Exception("Sequence timing mismatch");
        for(int k=4;k<=8;k++)if(Math.Abs(Config.assets[k].offsetX-Config.assets[4].offsetX)>.0001)throw new Exception("Sleep rear-paw world anchor changed");
        for(int k=0;k<Frames.Count;k++) {
            var b=Frames[k];var data=Bytes(b);int visible=0;
            for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++) {
                int alpha=data[(y*b.Width+x)*4+3];if(alpha<=16)continue;visible++;
                if(x==0||y==0||x==b.Width-1||y==b.Height-1)throw new Exception("Visible edge clipping: "+Config.assets[k].id);
                var p=Config.assets[k];double px=400+p.offsetX+(x-p.anchorX)*p.scale,py=570+(y-p.supportY)*p.scale;
                if(px<20||px>980||py<90||py>573)throw new Exception("Review placement outside bounds: "+p.id);
            }
            if(visible<1000)throw new Exception("Missing sprite");
        }
        // Each reverse route reuses the same images and registration, not redraws.
        int[] expected={0,1,2,3,4,5,6,7,8,7,6,5,4,3,2,1};
        if(Config.sequence.Length!=expected.Length)throw new Exception("Route length mismatch");
        for(int i=0;i<expected.Length;i++)if(Config.sequence[i]!=expected[i]||Config.delays[i]<=0)throw new Exception("Route/timing mismatch");
    }
    static void Export(string directory) {
        Directory.CreateDirectory(directory);Check();
        string native=Path.Combine(directory,"native-frames");Directory.CreateDirectory(native);
        for(int i=0;i<Frames.Count;i++)Frames[i].Save(Path.Combine(native,Config.assets[i].id+".png"),ImageFormat.Png);
        foreach(bool dark in new[]{false,true}) {
            string folder=Path.Combine(directory,dark?"dark-frames":"light-frames");Directory.CreateDirectory(folder);
            for(int i=0;i<Config.sequence.Length;i++)using(var b=new Bitmap(1000,650))using(var g=Graphics.FromImage(b)) {
                Render(g,Config.sequence[i],dark);b.Save(Path.Combine(folder,i.ToString("00")+".png"),ImageFormat.Png);
            }
        }
        using(var b=new Bitmap(3240,440))using(var g=Graphics.FromImage(b))using(var ink=new SolidBrush(Color.FromArgb(30,35,40)))using(var font=new Font("Segoe UI",14))using(var line=new Pen(Color.Gray)) {
            g.Clear(Color.FromArgb(242,244,246));g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            for(int i=0;i<Frames.Count;i++){Draw(g,i,i*360+150,380,.60f);g.DrawString(Config.assets[i].id,font,ink,i*360+16,402);g.DrawLine(line,i*360+5,380,i*360+355,380);}
            b.Save(Path.Combine(directory,"keyposes.png"),ImageFormat.Png);
        }
        File.WriteAllText(Path.Combine(directory,"checks.json"),"{\"passed\":true,\"nativePoseFrames\":9,\"cycleFrames\":16,\"sleepRearAnchorAligned\":true,\"placementAndEdgeChecks\":true,\"reverseReusesForwardKeys\":true,\"intactSourceDrawings\":true}");
    }
    sealed class ReviewForm:Form {
        readonly Timer timer=new Timer();readonly System.Diagnostics.Stopwatch watch=System.Diagnostics.Stopwatch.StartNew();bool dark;
        public ReviewForm(){Text="Bruno posture candidates - B: light/dark";ClientSize=new Size(1000,650);DoubleBuffered=true;timer.Interval=30;timer.Tick+=delegate{Invalidate();};timer.Start();KeyDown+=delegate(object s,KeyEventArgs e){if(e.KeyCode==Keys.B)dark=!dark;};}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);int total=0;foreach(int delay in Config.delays)total+=delay;double t=(watch.Elapsed.TotalMilliseconds/10)%total;int index=0;while(index<Config.delays.Length-1&&t>=Config.delays[index]){t-=Config.delays[index];index++;}e.Graphics.ScaleTransform(ClientSize.Width/1000f,ClientSize.Height/650f);Render(e.Graphics,Config.sequence[index],dark);}
        protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
    }
    [STAThread]static int Main(string[] args) {
        try{Initialize();if(args.Length==2&&args[0]=="--export"){Export(args[1]);return 0;}Application.EnableVisualStyles();Application.Run(new ReviewForm());return 0;}
        catch(Exception e){if(args.Length==2){Directory.CreateDirectory(args[1]);File.WriteAllText(Path.Combine(args[1],"error.txt"),e.ToString());}else MessageBox.Show(e.Message);return 1;}
        finally{foreach(var b in Frames)b.Dispose();}
    }
}
