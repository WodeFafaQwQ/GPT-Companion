using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace DragonCompanion {
// Tea retains a full raise/sip/lower. The shoulder-to-elbow connection is drawn
// ON TOP of the body, rather than disappearing behind the hair/body plate.
public static class TeaActivityModel {
 public const float UpperLength=13,LowerLength=17;
 static readonly Dictionary<string,Lazy<ActivityModelParts>> models=new Dictionary<string,Lazy<ActivityModelParts>>{{"pixel",new Lazy<ActivityModelParts>(()=>new ActivityModelParts("pixel"),true)},{"anime",new Lazy<ActivityModelParts>(()=>new ActivityModelParts("anime"),true)}};
 public sealed class Pose {public RectangleF Body;public PointF LeftRoot,RightRoot,LeftElbow,RightElbow,LeftWrist,RightWrist,Cup;public float Angle;}
 static float Ease(float value){float t=Math.Max(0,Math.Min(1,value));return t*t*t*(10+t*(-15+6*t));}
 public static float Lift(long milliseconds){float phase=(Math.Max(0,milliseconds)%7200)/7200f;return Ease((phase-.08f)/.25f)*(1-Ease((phase-.62f)/.28f));}
 static PointF Rotate(PointF p,float degrees){double angle=degrees*Math.PI/180;return new PointF((float)(p.X*Math.Cos(angle)-p.Y*Math.Sin(angle)),(float)(p.X*Math.Sin(angle)+p.Y*Math.Cos(angle)));}
 static PointF Elbow(PointF root,PointF wrist,bool left){double dx=wrist.X-root.X,dy=wrist.Y-root.Y,d=Math.Sqrt(dx*dx+dy*dy),angle=Math.Atan2(dy,dx),bend=Math.Acos(Math.Max(-1,Math.Min(1,(UpperLength*UpperLength+d*d-LowerLength*LowerLength)/(2*UpperLength*d))));angle+=(left?1:-1)*bend;return new PointF(root.X+(float)(UpperLength*Math.Cos(angle)),root.Y+(float)(UpperLength*Math.Sin(angle)));}
 public static Pose State(string style,long milliseconds){var model=models[style].Value;float center=model.CenterX,lift=Lift(milliseconds);var pose=new Pose{Body=model.BodyDestination,LeftRoot=new PointF(center-16,110),RightRoot=new PointF(center+16,110),Cup=new PointF(center,125-16*lift),Angle=-4*lift};var left=Rotate(new PointF(-10,-4),pose.Angle);var right=Rotate(new PointF(11,-4),pose.Angle);pose.LeftWrist=new PointF(pose.Cup.X+left.X,pose.Cup.Y+left.Y);pose.RightWrist=new PointF(pose.Cup.X+right.X,pose.Cup.Y+right.Y);pose.LeftElbow=Elbow(pose.LeftRoot,pose.LeftWrist,true);pose.RightElbow=Elbow(pose.RightRoot,pose.RightWrist,false);return pose;}
 static void Image(Graphics graphics,Bitmap image,RectangleF destination){graphics.DrawImage(image,destination,new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel);}
 static void UpperArm(Graphics graphics,PointF root,PointF elbow){using(var outline=new Pen(Color.FromArgb(151,111,137),7))using(var skin=new Pen(Color.FromArgb(255,226,210),5.9f)){outline.StartCap=outline.EndCap=skin.StartCap=skin.EndCap=LineCap.Round;graphics.DrawLine(outline,root,elbow);graphics.DrawLine(skin,root,elbow);}}
 public static PointF SleeveSocket(string style,bool left,bool cuff){var image=models[style].Value.Images[left?1:2];bool pixel=style=="pixel";return cuff?new PointF(image.Width*.5f,image.Height*(pixel?.82f:.88f)):new PointF(image.Width*(pixel?(left?.73f:.27f):(left?.56f:.44f)),image.Height*(pixel?.07f:.06f));}
 public static byte SleeveSocketAlpha(string style,bool left,bool cuff){var point=SleeveSocket(style,left,cuff);return models[style].Value.Images[left?1:2].GetPixel((int)point.X,(int)point.Y).A;}
 static void Sleeve(Graphics graphics,Bitmap image,string style,bool left,PointF elbow,PointF wrist){var root=SleeveSocket(style,left,false);var cuff=SleeveSocket(style,left,true);float sx=cuff.X-root.X,sy=cuff.Y-root.Y,scale=LowerLength/(float)Math.Sqrt(sx*sx+sy*sy),angle=(float)((Math.Atan2(wrist.Y-elbow.Y,wrist.X-elbow.X)-Math.Atan2(sy,sx))*180/Math.PI);var saved=graphics.Save();graphics.TranslateTransform(elbow.X,elbow.Y);graphics.RotateTransform(angle);graphics.ScaleTransform(scale,scale);graphics.TranslateTransform(-root.X,-root.Y);Image(graphics,image,new RectangleF(0,0,image.Width,image.Height));graphics.Restore(saved);}
 static void Wrist(Graphics graphics,PointF wrist){using(var skin=new SolidBrush(Color.FromArgb(255,226,210)))graphics.FillEllipse(skin,wrist.X-2.1f,wrist.Y-2.1f,4.2f,4.2f);}
 static void Object(Graphics graphics,Bitmap image,PointF point,float height,float anchorX,float anchorY,float angle){float width=height*image.Width/image.Height;var saved=graphics.Save();graphics.TranslateTransform(point.X,point.Y);graphics.RotateTransform(angle);Image(graphics,image,new RectangleF(-width*anchorX,-height*anchorY,width,height));graphics.Restore(saved);}
 public static Bitmap Frame(string style,long milliseconds){var parts=models[style].Value.Images;var pose=State(style,milliseconds);var result=new Bitmap(192,192,PixelFormat.Format32bppArgb);using(var graphics=Graphics.FromImage(result)){graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.Half;graphics.SmoothingMode=SmoothingMode.AntiAlias;
   Image(graphics,parts[0],pose.Body);
   UpperArm(graphics,pose.LeftRoot,pose.LeftElbow);UpperArm(graphics,pose.RightRoot,pose.RightElbow);
   Sleeve(graphics,parts[1],style,true,pose.LeftElbow,pose.LeftWrist);Sleeve(graphics,parts[2],style,false,pose.RightElbow,pose.RightWrist);
   Wrist(graphics,pose.LeftWrist);Wrist(graphics,pose.RightWrist);
   Object(graphics,parts[6],pose.Cup,21,.40f,.54f,pose.Angle);
   Object(graphics,parts[4],pose.LeftWrist,11,.5f,.08f,pose.Angle);Object(graphics,parts[5],pose.RightWrist,11,.5f,.08f,pose.Angle);
  }return result;}
}
}
