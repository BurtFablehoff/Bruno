using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Bruno-specific masks, registration and review timing. Raw generated keys are
// candidate inputs; only bounded composites are exported for animation review.
internal static class ActionReview
{
    static readonly string[] Poses={"sit","lay"};
    static readonly string[] Keys={"blink-half","blink-closed","tilt-mid","tilt-peak","tongue-retracted","tongue-peak"};
    static readonly Dictionary<string,Bitmap> Images=new Dictionary<string,Bitmap>();
    static readonly double[] Times={0,1.8,1.86,1.92,1.94,1.96,2.0,2.06,2.12,2.86,2.98,3.12,4.46,4.62,6};
    static Bitmap Load(string id)
    {
        using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("BrunoReview."+id))
        { if(stream==null)throw new Exception("Missing resource: "+id); using(var b=new Bitmap(stream))return new Bitmap(b); }
    }
    sealed class Pixels
    {
        public readonly int Width,Height; public readonly byte[] Data;
        public Pixels(Bitmap b)
        {
            Width=b.Width;Height=b.Height;Data=new byte[Width*Height*4];
            var locked=b.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try {for(int y=0;y<Height;y++)Marshal.Copy(IntPtr.Add(locked.Scan0,y*locked.Stride),Data,y*Width*4,Width*4);}
            finally{b.UnlockBits(locked);}
        }
        public Bitmap Bitmap()
        {
            var b=new Bitmap(Width,Height,PixelFormat.Format32bppArgb);
            var locked=b.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try{for(int y=0;y<Height;y++)Marshal.Copy(Data,y*Width*4,IntPtr.Add(locked.Scan0,y*locked.Stride),Width*4);}
            finally{b.UnlockBits(locked);}return b;
        }
    }
    static double Ellipse(int x,int y,double cx,double cy,double rx,double ry)
    { double d=Math.Sqrt(Math.Pow((x-cx)/rx,2)+Math.Pow((y-cy)/ry,2));return Math.Max(0,Math.Min(1,(1-d)/.18)); }
    static double Weight(string pose,string key,int x,int y)
    {
        bool sit=pose=="sit";
        if(key.StartsWith("blink"))
            return sit?Math.Max(Ellipse(x,y,400,281,78,70),Ellipse(x,y,721,278,78,70)):
                Math.Max(Ellipse(x,y,465,247,74,67),Ellipse(x,y,760,244,74,67));
        if(key.StartsWith("tongue"))return sit?Ellipse(x,y,562,425,79,44):Ellipse(x,y,619,384,77,44);
        // Preserve the approved lower body exactly, with a short neck blend.
        double start=sit?540:490,end=sit?660:610;
        return Math.Max(0,Math.Min(1,(end-y)/(end-start)));
    }
    static Bitmap Composite(string pose,string key,Bitmap source)
    {
        var basis=Images[pose];if(source.Size!=basis.Size)throw new Exception("Canvas mismatch: "+pose+"-"+key);
        var result=new Pixels(basis);var candidate=new Pixels(source);
        bool keepAlpha=!key.StartsWith("tilt");
        for(int y=0;y<result.Height;y++)for(int x=0;x<result.Width;x++)
        {
            double t=Weight(pose,key,x,y);if(t<=0)continue;
            int i=(y*result.Width+x)*4;
            if(keepAlpha) {
                for(int c=0;c<3;c++)result.Data[i+c]=(byte)Math.Round(result.Data[i+c]*(1-t)+candidate.Data[i+c]*t);
            } else {
                double a=result.Data[i+3]*(1-t),b=candidate.Data[i+3]*t,total=a+b;
                for(int c=0;c<3;c++)result.Data[i+c]=total<.5?(byte)0:(byte)Math.Round((result.Data[i+c]*a+candidate.Data[i+c]*b)/total);
                result.Data[i+3]=(byte)Math.Round(total);
            }
        }
        return result.Bitmap();
    }
    static void Initialize()
    {
        foreach(string pose in Poses) {
            Images.Add(pose,Load(pose));
            foreach(string key in Keys)using(Bitmap raw=Load(pose+"-"+key))Images.Add(pose+"-"+key,Composite(pose,key,raw));
        }
    }
    static string KeyAt(int action,double t)
    {
        t=t%6;
        if(action==0){if(t<1.8||t>=2.0)return "";if(t<1.86||t>=1.94)return "blink-half";return "blink-closed";}
        if(action==1){if(t<1.8||t>=4.62)return "";return t<1.96||t>=4.46?"tilt-mid":"tilt-peak";}
        if(t<1.8||t>=3.12)return "";
        if(t<1.94||t>=2.98)return "tongue-retracted";
        if(t<2.06||t>=2.86)return "";
        return "tongue-peak";
    }
    static Bitmap At(string pose,int action,double t)
    {string key=KeyAt(action,t);return Images[key==""?pose:pose+"-"+key];}
    static void Render(Graphics g,double t,bool detail,bool dark)
    {
        g.Clear(dark?Color.FromArgb(30,34,40):Color.FromArgb(242,244,246));
        g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
        using(var ink=new SolidBrush(dark?Color.WhiteSmoke:Color.FromArgb(30,35,40)))
        using(var font=new Font("Segoe UI",18))using(var small=new Font("Segoe UI",12))
        using(var line=new Pen(Color.FromArgb(130,140,150)))
        {
            g.DrawString("Bruno - blink / head tilt / tongue peek (candidates)",font,ink,20,12);
            string[] names={"Blink","Head tilt","Tongue peek / retract"};
            for(int row=0;row<2;row++)for(int action=0;action<3;action++)
            {
                string pose=Poses[row];var im=At(pose,action,t);
                int left=500*action,top=55+row*(detail?360:440);
                g.DrawString((row==0?"Sit - ":"Lie - ")+names[action],small,ink,left+20,top);
                if(detail) {
                    Rectangle src=row==0?new Rectangle(160,20,800,580):new Rectangle(230,0,800,580);
                    g.DrawImage(im,new Rectangle(left+20,top+26,460,334),src,GraphicsUnit.Pixel);
                } else {
                    float scale=row==0?.27f:(float)(.27*318/295);
                    float anchor=row==0?554:816,support=row==0?1382:1006,floor=top+405;
                    g.DrawImage(im,new RectangleF(left+250-anchor*scale,floor-support*scale,im.Width*scale,im.Height*scale));
                    g.DrawLine(line,left+10,floor,left+490,floor);
                }
            }
        }
    }
    static int Check()
    {
        int changed=0;
        foreach(string pose in Poses)foreach(string key in Keys)
        {
            var basis=new Pixels(Images[pose]);var frame=new Pixels(Images[pose+"-"+key]);int local=0;
            for(int y=0;y<basis.Height;y++)for(int x=0;x<basis.Width;x++)
            {
                int i=(y*basis.Width+x)*4;bool same=true;
                for(int c=0;c<4;c++)if(basis.Data[i+c]!=frame.Data[i+c])same=false;
                if(!same){local++;if(Weight(pose,key,x,y)<=0)throw new Exception("Pixels changed outside mask");}
                if(!key.StartsWith("tilt")&&basis.Data[i+3]!=frame.Data[i+3])throw new Exception("Alpha drift");
                if((x==0||x==basis.Width-1||y==0||y==basis.Height-1)&&frame.Data[i+3]>16)throw new Exception("Visible edge/background artifact: "+pose+"-"+key);
            }
            if(local==0)throw new Exception("Empty expression: "+pose+key);changed+=local;
        }
        foreach(string pose in Poses)for(int a=0;a<3;a++)
            if(!Object.ReferenceEquals(At(pose,a,0),Images[pose])||!Object.ReferenceEquals(At(pose,a,5.9),Images[pose]))throw new Exception("Return to neutral failed");
        return changed;
    }
    static void Export(string directory)
    {
        Directory.CreateDirectory(directory);int changes=Check();
        string native=Path.Combine(directory,"native-frames");Directory.CreateDirectory(native);
        foreach(var pair in Images)pair.Value.Save(Path.Combine(native,pair.Key+".png"),ImageFormat.Png);
        foreach(bool detail in new[]{false,true}) {
            string folder=Path.Combine(directory,detail?"detail-frames":"body-frames");Directory.CreateDirectory(folder);
            for(int i=0;i<Times.Length-1;i++)using(var b=new Bitmap(1500,detail?780:950))using(var g=Graphics.FromImage(b)) {
                Render(g,Times[i]+.001,detail,false);b.Save(Path.Combine(folder,i.ToString("00")+".png"),ImageFormat.Png);
            }
        }
        foreach(bool dark in new[]{false,true})using(var b=new Bitmap(1500,950))using(var g=Graphics.FromImage(b)) {
            Render(g,2.3,false,dark);b.Save(Path.Combine(directory,"peak-"+(dark?"dark":"light")+".png"),ImageFormat.Png);
        }
        File.WriteAllText(Path.Combine(directory,"checks.json"),"{\"passed\":true,\"compositeFrames\":12,\"outsideMaskChanges\":0,\"blinkTongueAlphaChanges\":0,\"neutralReturnCases\":6,\"changedPixels\":"+changes+",\"reviewFrameCount\":"+(Times.Length-1)+"}");
        var delays=new List<string>();for(int i=0;i<Times.Length-1;i++)delays.Add(((int)Math.Round((Times[i+1]-Times[i])*100)).ToString());
        File.WriteAllText(Path.Combine(directory,"delays.txt"),String.Join(",",delays.ToArray()));
    }
    sealed class ReviewForm:Form
    {
        readonly Timer timer=new Timer();readonly System.Diagnostics.Stopwatch watch=System.Diagnostics.Stopwatch.StartNew();bool detail=true,dark=false;
        public ReviewForm() {
            Text="Bruno action candidates - D: detail/body, B: background";ClientSize=new Size(1200,640);DoubleBuffered=true;
            timer.Interval=30;timer.Tick+=delegate{Invalidate();};timer.Start();
            KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==KeysCode.D)detail=!detail;if(e.KeyCode==KeysCode.B)dark=!dark;};
        }
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.ScaleTransform(ClientSize.Width/1500f,ClientSize.Height/(detail?780f:950f));Render(e.Graphics,watch.Elapsed.TotalSeconds,detail,dark);}
        protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
    }
    // Alias avoids the expression-key array shadowing WinForms Keys.
    static class KeysCode {public const System.Windows.Forms.Keys D=System.Windows.Forms.Keys.D,B=System.Windows.Forms.Keys.B;}
    [STAThread]static int Main(string[] args)
    {
        try{Initialize();if(args.Length==2&&args[0]=="--export"){Export(args[1]);return 0;}Application.EnableVisualStyles();Application.Run(new ReviewForm());return 0;}
        catch(Exception e){if(args.Length==2)File.WriteAllText(Path.Combine(args[1],"error.txt"),e.ToString());else MessageBox.Show(e.Message);return 1;}
        finally{foreach(var b in Images.Values)b.Dispose();}
    }
}
