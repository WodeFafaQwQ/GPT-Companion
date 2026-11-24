using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace DragonCompanion {
// One fixed idle drawing. Only the tail tip and eye windows change; the pet
// never swaps its whole body for a separately painted blinking pose.
public static class IdleArt {
 sealed class Idle {
  public Bitmap Source,Closed,TailBody,TailTexture,TailRest;public bool[] TailMask;public Rectangle TailArea;public PointF Root;public RectangleF[] Eyes,Irises;public Lazy<Bitmap[]> Sways;
 }
 static readonly Dictionary<string,Lazy<Idle>> cache=Create();
 static Dictionary<string,Lazy<Idle>> Create(){var map=new Dictionary<string,Lazy<Idle>>();foreach(string s in new[]{"pixel","anime"}){string style=s;map.Add(style,new Lazy<Idle>(()=>Load(style),true));}return map;}
 static Bitmap Fit(Bitmap art,bool pixel){var image=new Bitmap(192,192);float scale=Math.Min(184f/art.Width,184f/art.Height);int w=(int)(art.Width*scale),h=(int)(art.Height*scale);using(var g=Graphics.FromImage(image)){g.InterpolationMode=pixel?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(art,new Rectangle((192-w)/2,188-h,w,h));}return image;}
 static Idle Load(string style){
  bool pixel=style=="pixel";var idle=new Idle();idle.Source=Fit(Art.OriginalIdle(style,false),pixel);idle.Closed=Fit(Art.OriginalIdle(style,true),pixel);
  var a=ActivityArt.OpaqueBounds(idle.Source);var b=ActivityArt.OpaqueBounds(idle.Closed);int dx=(int)Math.Round(ReadingBook.HeadCenter(idle.Source)-ReadingBook.HeadCenter(idle.Closed)),dy=a.Top-b.Top;
  var closed=idle.Closed;idle.Closed=new Bitmap(192,192);using(var g=Graphics.FromImage(idle.Closed))g.DrawImageUnscaled(closed,dx,dy);closed.Dispose();
  idle.Eyes=pixel?new[]{new RectangleF(62,62,16,14),new RectangleF(90,62,16,14)}:new[]{new RectangleF(76,60,16,15),new RectangleF(100,58,16,15)};
  // Gaze moves the texture INSIDE the eye opening. Upper lashes, eyebrows,
  // cheeks and hair never slide along with the pupils.
  idle.Irises=pixel?new[]{new RectangleF(64,66,14,10),new RectangleF(92,66,14,10)}:
   new[]{new RectangleF(76.5f,63,14,9),new RectangleF(100.5f,61,14,9)};
  idle.Root=pixel?new PointF(131,158):new PointF(127,147);
  PointF[] points=pixel?new[]{new PointF(131,150),new PointF(144,149),new PointF(154,143),new PointF(157,135),new PointF(155,124),new PointF(156,115),new PointF(161,115),new PointF(169,124),new PointF(173,136),new PointF(171,147),new PointF(164,159),new PointF(153,167),new PointF(139,173),new PointF(128,169)}:new[]{new PointF(129,145),new PointF(136,145),new PointF(143,151),new PointF(147,160),new PointF(145,171),new PointF(137,179),new PointF(126,180),new PointF(119,173),new PointF(121,165),new PointF(128,166),new PointF(133,167),new PointF(135,162),new PointF(133,156),new PointF(129,154)};
  idle.TailMask=new bool[192*192];
  using(var path=new GraphicsPath()){
   path.AddPolygon(points);idle.TailArea=Rectangle.Ceiling(path.GetBounds());idle.TailArea.Inflate(22,22);idle.TailArea.Intersect(new Rectangle(0,0,192,192));
   using(var coverage=new Region(path))using(var border=(GraphicsPath)path.Clone())using(var pen=new Pen(Color.White,9)){
    border.Widen(pen);coverage.Union(border);
    for(int y=0;y<192;y++)for(int x=0;x<192;x++){
     float rx=x-idle.Root.X,ry=y-idle.Root.Y;
     bool foreground=pixel?(x<136&&y<152||x<129&&y<174):(x<127&&y<162||x<134&&y<148);
     if(!coverage.IsVisible(x+.5f,y+.5f)||rx*rx+ry*ry<20||foreground)continue;
     idle.TailMask[y*192+x]=true;
    }
   }
  }
  idle.TailBody=(Bitmap)idle.Source.Clone();idle.TailRest=(Bitmap)idle.Source.Clone();idle.TailTexture=new Bitmap(192,192);
  for(int y=0;y<192;y++)for(int x=0;x<192;x++)if(idle.TailMask[y*192+x]){
   idle.TailTexture.SetPixel(x,y,idle.Source.GetPixel(x,y));idle.TailBody.SetPixel(x,y,Color.Transparent);
  }
  // These independent immutable textures are prepared off the first-paint path.
  idle.Sways=new Lazy<Bitmap[]>(()=>{var list=new Bitmap[56];for(int i=0;i<list.Length;i++)list[i]=TailSway.Render(style,idle.TailRest,idle.TailBody,idle.TailTexture,idle.TailMask,idle.TailArea,i*50);return list;},true);
  return idle;
 }
 public static float TailAngleAt(long milliseconds){return TailSway.Angle(IdleTiming.TailPhase(milliseconds));}
 public static PointF TailTipAt(string style,long milliseconds){return TailSway.Tip(style,IdleTiming.TailPhase(milliseconds));}
 public static void Prepare(string style){var ready=cache[style].Value.Sways.Value;}
 public static Rectangle TailArea(string style){return cache[style].Value.TailArea;}
 public static Rectangle EyeArea(string style){var e=cache[style].Value.Eyes;return Rectangle.Ceiling(RectangleF.Union(e[0],e[1]));}
 internal static Rectangle IrisArea(string style){var e=cache[style].Value.Irises;return Rectangle.Ceiling(RectangleF.Union(e[0],e[1]));}
 public static Bitmap Source(string style){return cache[style].Value.Source;}
 public static Bitmap Frame(string style,long milliseconds){
  var idle=cache[style].Value;long phase=IdleTiming.TailPhase(milliseconds);bool pixel=style=="pixel",blink=IdleTiming.Blink(milliseconds);float gaze=IdleTiming.Gaze(milliseconds);
  if(pixel)gaze=(float)Math.Round(gaze);
  var image=(Bitmap)(phase>=0?idle.Sways.Value[phase/50]:idle.Source).Clone();
  // Draw eyes on an independent surface. A GDI+ paint on the full pet can
  // round translucent outline RGB even outside the clip, causing idle flicker.
  if(blink||Math.Abs(gaze)>.001)using(var eyeLayer=(Bitmap)idle.Source.Clone()){
   using(var g=Graphics.FromImage(eyeLayer)){
   g.InterpolationMode=pixel?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;
   foreach(var eye in (blink?idle.Eyes:idle.Irises)){var saved=g.Save();using(var path=new GraphicsPath()){path.AddEllipse(eye);g.SetClip(path);}
    // Use the original opaque closed-eye cel. Crossfading open and closed
    // eyes made two superimposed eyelids during a blink.
    if(blink)g.DrawImageUnscaled(idle.Closed,0,0);
    else g.DrawImage(idle.Source,new RectangleF(gaze,0,192,192));g.Restore(saved);
   }
   }
   foreach(var window in (blink?idle.Eyes:idle.Irises)){
    var area=Rectangle.Ceiling(window);for(int y=area.Top;y<area.Bottom;y++)for(int x=area.Left;x<area.Right;x++){
     var original=idle.Source.GetPixel(x,y);if(original.A<245)continue;var colour=eyeLayer.GetPixel(x,y);
     image.SetPixel(x,y,Color.FromArgb(original.A,colour.R,colour.G,colour.B));
    }
   }
  }
  return image;
 }
 public static bool Draw(Graphics graphics,Rectangle bounds,string style,int tick,bool centered){
  if(!cache.ContainsKey(style)||!Art.Available(style))return false;
  using(var frame=Frame(style,Math.Max(0,(long)tick)*50)){
   var saved=graphics.Save();graphics.InterpolationMode=style=="pixel"?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.Half;
   float side=Math.Min(bounds.Width,bounds.Height);graphics.DrawImage(frame,new RectangleF(bounds.X+(bounds.Width-side)/2,centered?bounds.Y+(bounds.Height-side)/2:bounds.Bottom-side,side,side));graphics.Restore(saved);
  }return true;
 }
}
}
