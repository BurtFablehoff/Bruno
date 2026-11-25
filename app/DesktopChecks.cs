using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using PetFramework;

namespace Bruno
{
    internal static class DesktopChecks
    {
        [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr window,int index);
        [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr w,IntPtr l);
        [DllImport("user32.dll")]static extern uint GetGuiResources(IntPtr process,uint flags);
        sealed class Probe:LayeredSpriteWindow
        {
            public void UploadScene(Bitmap image,Point location){Present(image,location);}
            public void Upload(){Upload(new Point(100,100));}
            public void Upload(Point location)
            {
                Bitmap scene=new Bitmap(8,8,PixelFormat.Format32bppPArgb);
                using(Graphics g=Graphics.FromImage(scene)){g.Clear(Color.Transparent);g.FillRectangle(Brushes.Tan,2,2,4,4);}
                Present(scene,location);
            }
        }
        static void Require(bool condition,string text){if(!condition)throw new Exception(text);}
        static IntPtr XY(int x,int y){return new IntPtr((y<<16)|(x&65535));}
        static int MenuAndGestureChecks()
        {
            Rectangle[] menus={new Rectangle(100,100,200,200),new Rectangle(300,120,180,300)};int cases=0;
            foreach(int button in new[]{1,2,4,8,16})
            {
                Require(MenuInput.Dismiss(new Point(800,800),button,0,false,menus),"Outside mouse button failed to dismiss menu.");
                Require(!MenuInput.Dismiss(new Point(150,150),button,0,false,menus),"Root menu click dismissed menu.");
                Require(!MenuInput.Dismiss(new Point(350,200),button,0,false,menus),"Submenu click dismissed menu.");cases+=3;
            }
            Require(!MenuInput.Dismiss(new Point(800,800),1,1,false,menus),"Opening held button immediately closed menu.");
            Require(MenuInput.Dismiss(new Point(800,800),3,1,false,menus),"New second button did not dismiss menu.");
            Require(MenuInput.Dismiss(new Point(150,150),0,0,true,menus),"Escape did not dismiss menu.");cases+=3;
            PointerGesture gesture=new PointerGesture();gesture.Begin(System.Windows.Forms.MouseButtons.Left,new Point(10,10));gesture.Move(new Point(12,12));Require(gesture.End(System.Windows.Forms.MouseButtons.Left)==PointerResult.Click,"Simple companion click classified as drag.");cases++;
            gesture.Begin(System.Windows.Forms.MouseButtons.Left,new Point(10,10));gesture.Move(new Point(20,15));Require(gesture.End(System.Windows.Forms.MouseButtons.Left)==PointerResult.Drag,"Real left drag opened menu.");cases++;
            gesture.Begin(System.Windows.Forms.MouseButtons.Right,new Point(10,10));Require(gesture.End(System.Windows.Forms.MouseButtons.Right)==PointerResult.Click,"Right click did not open menu.");cases++;
            return cases;
        }
        public static void Run(Assets art,string path)
        {
            int menuChecks;
            using(CompanionWindow menu=new CompanionWindow(art,true))menuChecks=menu.CheckMenuWiring();
            int inputChecks=MenuAndGestureChecks(),bodyHits=0,overhangHits=0,clockUploads=0;uint gdiBefore,gdiAfter;bool foregroundUnchanged;
            IntPtr foreground=GetForegroundWindow();
            using(Probe probe=new Probe())
            {
                IntPtr handle=probe.Handle;Require(!IsWindowVisible(handle),"Probe unexpectedly visible.");
                int styles=GetWindowLong(handle,-20);
                Require((styles&0x80000)!=0&&(styles&0x08000000)!=0&&(styles&0x80)!=0,"Layered/nonactivating/tool-window flags missing.");
                probe.Upload();gdiBefore=GetGuiResources(Process.GetCurrentProcess().Handle,0);
                for(int i=0;i<200;i++)probe.Upload();
                gdiAfter=GetGuiResources(Process.GetCurrentProcess().Handle,0);
                Require(gdiAfter<=gdiBefore+3,"GDI objects leaked during repeated sprite uploads.");
                Require(!IsWindowVisible(handle),"Native upload made the hidden window visible.");
                Require(probe.ClientSize==new Size(8,8),"Managed client bounds differ from uploaded scene.");
                int opaque=SendMessage(handle,0x84,IntPtr.Zero,XY(103,103)).ToInt32();
                Require(opaque==1,"Opaque sprite region did not accept hit testing: result="+opaque+", managed bounds="+probe.Bounds);
                Require(SendMessage(handle,0x84,IntPtr.Zero,XY(100,100)).ToInt32()==-1,"Transparent sprite region did not pass clicks through.");
                Point[] locations={new Point(-1900,-100),new Point(2200,800),new Point(100,100)};
                foreach(Point location in locations)
                {
                    probe.Upload(location);
                    Require(probe.PresentedBounds==new Rectangle(location,new Size(8,8)),"Uploaded scene bounds did not follow position.");
                    Require(SendMessage(handle,0x84,IntPtr.Zero,XY(location.X+3,location.Y+3)).ToInt32()==1,"Opaque hit target lost after monitor-coordinate move.");
                    Require(SendMessage(handle,0x84,IntPtr.Zero,XY(location.X,location.Y)).ToInt32()==-1,"Transparent hit target changed after move.");
                }
                Require(SendMessage(handle,0x21,IntPtr.Zero,IntPtr.Zero).ToInt32()==3,"Mouse activation is not suppressed.");
                Require(GetForegroundWindow()!=handle,"Probe took foreground focus.");foregroundUnchanged=GetForegroundWindow()==foreground;
                Placement placement=new Placement(art);
                foreach(string key in new[]{"sit","lay"})
                {
                    FrameInfo info=art.Manifest.frames[key];PointF support=new PointF(-1200,700);Point location;
                    Bitmap scene=placement.Paint(key,support,.36,out location);Size size=scene.Size;probe.UploadScene(scene,location);
                    Require(probe.ClientSize==size,"Full sprite client area was limited to the old form size.");
                    Point[] pixels=key=="sit"?new[]{new Point(560,340),new Point(560,800),new Point(370,1330),new Point(180,1180)}:new[]{new Point(600,340),new Point(1000,600),new Point(400,970),new Point(1210,830)};
                    RectangleF sprite=placement.Sprite(key,support,.36);
                    foreach(Point pixel in pixels)
                    {
                        Require(art.Get(key).GetPixel(pixel.X,pixel.Y).A>16,"Invalid body hit fixture: "+key+" "+pixel);
                        int xx=(int)Math.Round(sprite.X+pixel.X*info.scale*.36),yy=(int)Math.Round(sprite.Y+pixel.Y*info.scale*.36);
                        Require(SendMessage(handle,0x84,IntPtr.Zero,XY(xx,yy)).ToInt32()==1,"Visible companion body region did not accept click: "+key+" "+pixel);bodyHits++;
                        if((key=="sit"&&pixel==new Point(370,1330))||(key=="lay"&&pixel==new Point(400,970))) {Require(yy>support.Y,"Front paw no longer hangs below the floor");overhangHits++;}
                    }
                }
                foreach(var entry in art.Manifest.frames)if(entry.Key.StartsWith("clock-"))
                {
                    Point origin;PointF support=new PointF(-1200,700);
                    Bitmap whole=placement.Paint(entry.Key,support,.24,out origin);Size size=whole.Size;
                    Point paw=Point.Empty;bool found=false;
                    for(int yy=whole.Height-1;yy>=0&&!found;yy--)for(int xx=0;xx<whole.Width;xx++)
                        if(origin.Y+yy>support.Y&&whole.GetPixel(xx,yy).A>32){paw=new Point(origin.X+xx,origin.Y+yy);found=true;break;}
                    Require(found,"Clock sprite lost overhanging paws: "+entry.Key);
                    probe.UploadScene(whole,origin);Require(probe.ClientSize==size,"Clock upload clipped whole body");
                    Require(SendMessage(handle,0x84,IntPtr.Zero,XY(paw.X,paw.Y)).ToInt32()==1,"Clock paw not clickable: "+entry.Key);
                    clockUploads++;
                }
            }
            Require(clockUploads==24,"Clock native coverage missing");
            Require(overhangHits==2,"Dangling paw input coverage missing");
            var report=new{passed=true,menuChecks=menuChecks,menuAndGestureCases=inputChecks,fullBodyHitRegions=bodyHits,pawHitTargetsBelowFloor=overhangHits,managedClientMatchesWholeSprite=true,hiddenNativeUploads=206+clockUploads,clockWholeSpriteUploads=clockUploads,movedHitTestPositions=3,negativeScreenCoordinates=true,windowStyles=true,transparentHitTest=true,opaqueHitTest=true,mouseActivationSuppressed=true,
                gdiObjectsBefore=gdiBefore,gdiObjectsAfter=gdiAfter,foregroundUnchanged=foregroundUnchanged,visibleWindowsOpened=false,trayIconShown=false,userSettingsWritten=false,
                manualStillRequired="Owner feel/appearance of visible companion, actual drag/drop input and mixed-DPI monitor changes"};
            File.WriteAllText(path,new JavaScriptSerializer().Serialize(report));
        }
    }
}
