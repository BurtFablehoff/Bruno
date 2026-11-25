using System;
using System.Drawing;

namespace Bruno
{
    // Selects complete approved sprite PNGs. There are no head layers or composites.
    internal sealed class MouseLook
    {
        int current=-1,desired=-1;double wait;string pose="";
        internal int Index{get{return current;}}
        internal int Desired{get{return desired;}}
        internal void Reset(){current=desired=-1;wait=0;pose="";}
        internal static double Distance(double a,double b)
        {double d=Math.Abs(a-b)%360;return Math.Min(d,360-d);}
        internal static PointF Head(Assets art,Placement placement,string pose,PointF support,double size)
        {
            FrameInfo f=art.Manifest.frames[pose];RectangleF sprite=placement.Sprite(pose,support,size);
            double k=f.scale*size;
            return new PointF(sprite.X+(float)((pose=="sit"?554:610)*k),sprite.Y+(float)((pose=="sit"?300:275)*k));
        }
        internal static double DeadZone(double size){return Math.Max(12,85*size);}
        internal string DisplayFrame(Routine routine,bool enabled)
        {
            if(!enabled||pose!=routine.Pose||routine.Pose=="sleep"||routine.Action.Length!=0||routine.InTransition||current<0)return routine.Frame;
            return "clock-"+routine.Pose+"-h"+(current==0?12:current).ToString("00");
        }
        internal void Update(double dt,Routine routine,bool enabled,bool suppressed,PointF head,PointF target,double deadZone)
        {
            if(!enabled||routine.Pose=="sleep"||routine.Action.Length!=0||routine.InTransition){Reset();return;}
            if(pose!=routine.Pose){Reset();pose=routine.Pose;}
            if(suppressed||routine.Paused)return;
            double dx=target.X-head.X,dy=target.Y-head.Y;
            if(dx*dx+dy*dy<=deadZone*deadZone){current=desired=-1;wait=0;return;}
            double angle=(Math.Atan2(dx,-dy)*180/Math.PI+360)%360;
            int next=((int)Math.Floor((angle+15)/30))%12;
            // Keep the existing target through small movements across an hour boundary.
            if(desired<0||Distance(angle,desired*30)>21)desired=next;
            if(current<0){current=desired;wait=.065;return;}
            wait-=Math.Max(0,Math.Min(.1,dt));
            if(current==desired){wait=Math.Min(wait,0);return;}
            if(wait>0)return;
            int clockwise=(desired-current+12)%12;
            current=(current+(clockwise<6?1:11))%12;
            wait=.065;
        }
    }
}
