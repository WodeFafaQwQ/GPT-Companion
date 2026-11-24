using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
namespace DragonCompanion {
// A single high-resolution static illustration keeps the cuff, fingers and
// surrounding hair consistent. Only the upright bell and its loop animate.
internal sealed class AnimeBellArt {
 const int Resolution=768;
 readonly Bitmap background,hand,bell;
 readonly Rectangle bellBounds;
 internal static readonly PointF Grip=new PointF(121.5f,117);
 internal AnimeBellArt(){
  var parts=new ActivityModelParts("anime");bell=parts.Images[7];
  for(int i=0;i<7;i++)parts.Images[i].Dispose();
  bellBounds=ActivityArt.OpaqueBounds(bell);
  background=new Bitmap(Resolution,Resolution);hand=new Bitmap(Resolution,Resolution);
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("anime-reminder-clean.png"))
  using(var clean=new Bitmap(stream)){
   using(var g=Graphics.FromImage(background)){
    Quality(g);g.DrawImage(clean,new RectangleF(-2/1.04f*4,0,192/1.04f*4,192/1.04f*4));
   }
   using(var g=Graphics.FromImage(hand))using(var grip=new GraphicsPath()){
    Quality(g);g.ScaleTransform(4,4);
    grip.AddPolygon(new[]{new PointF(119.8f,113.2f),new PointF(121.8f,113.1f),new PointF(122.5f,115.2f),new PointF(124.3f,116.6f),new PointF(126.4f,120.5f),new PointF(125.4f,124.4f),new PointF(121.5f,123),new PointF(118,120.5f),new PointF(117,118.4f),new PointF(118.1f,116)});
    g.SetClip(grip);g.DrawImage(background,new RectangleF(0,0,192,192));
   }
  }
 }
 static void Quality(Graphics g){g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;}
 internal void Draw(Graphics graphics,Rectangle bounds,long milliseconds){
  var saved=graphics.Save();Quality(graphics);float side=Math.Min(bounds.Width,bounds.Height);
  graphics.TranslateTransform(bounds.X+(bounds.Width-side)/2,bounds.Bottom-side);graphics.ScaleTransform(side/192,side/192);
  graphics.DrawImage(background,new RectangleF(0,0,192,192));DrawLayers(graphics,milliseconds);graphics.Restore(saved);
 }
 void DrawLayers(Graphics graphics,long milliseconds){
  var moving=graphics.Save();graphics.TranslateTransform(Grip.X,Grip.Y);graphics.RotateTransform(ActivityBell.Angle(milliseconds));
  using(var edge=new Pen(Color.FromArgb(106,68,56),1.25f))using(var gold=new Pen(Color.FromArgb(255,205,86),.65f)){
   graphics.DrawEllipse(edge,-2.2f,-.5f,4.4f,6);graphics.DrawEllipse(gold,-2.2f,-.5f,4.4f,6);
  }
  float height=24,width=height*bellBounds.Width/bellBounds.Height;
  graphics.DrawImage(bell,new RectangleF(-width/2,4,width,height),bellBounds,GraphicsUnit.Pixel);
  graphics.Restore(moving);graphics.DrawImage(hand,new RectangleF(0,0,192,192));
 }
 internal Bitmap Frame(long milliseconds){var frame=new Bitmap(192,192);using(var g=Graphics.FromImage(frame))Draw(g,new Rectangle(0,0,192,192),milliseconds);return frame;}
 internal bool FingerCovers(int x,int y){
  for(int dy=-4;dy<=4;dy++)for(int dx=-4;dx<=4;dx++)if(hand.GetPixel(x*4+2+dx,y*4+2+dy).A<250)return false;
  return true;
 }
}
}
