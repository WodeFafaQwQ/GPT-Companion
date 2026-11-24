using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;


namespace DragonCompanion {
public static class ReadingBook {
 public const int PagesBeforeThought=3,ThoughtMilliseconds=6000,TransitionMilliseconds=700;
 public static long TurningMilliseconds {get{return PagesBeforeThought*(long)ActivityArt.Find("read").Duration;}}
 public static long ThoughtStartsAt {get{return TurningMilliseconds+TransitionMilliseconds;}}
 public static long CycleMilliseconds {get{return ThoughtStartsAt+ThoughtMilliseconds+TransitionMilliseconds;}}
 static long Phase(long milliseconds){return Math.Max(0,milliseconds)%CycleMilliseconds;}
 public static bool IsThinking(long milliseconds){long phase=Phase(milliseconds);return phase>=ThoughtStartsAt&&phase<ThoughtStartsAt+ThoughtMilliseconds;}
 public static string PreviewKey(long milliseconds){long phase=Phase(milliseconds);if(phase<TurningMilliseconds)return FrameAt(milliseconds).ToString();if(IsThinking(milliseconds))return "thinking";return (phase<ThoughtStartsAt?"settle-":"resume-")+TransitionStep(milliseconds);}
 static int TransitionStep(long milliseconds){long phase=Phase(milliseconds),start=phase<ThoughtStartsAt?TurningMilliseconds:ThoughtStartsAt+ThoughtMilliseconds;return Math.Max(0,Math.Min(14,(int)((phase-start)/50)));}
 public static Bitmap ImageAt(string style,long milliseconds){long phase=Phase(milliseconds);if(phase<TurningMilliseconds)return ActivityArt.Frame(style,"read",FrameAt(milliseconds));if(IsThinking(milliseconds))return Thought(style);return transitions[style+(phase<ThoughtStartsAt?"-in":"-out")].Value[TransitionStep(milliseconds)];}
 public static void Prepare(string style){var incoming=transitions[style+"-in"].Value;var outgoing=transitions[style+"-out"].Value;}
 public static int FrameAt(long milliseconds){return ActivityArt.Find("read").FrameAt((int)(Math.Max(0,milliseconds)%CycleMilliseconds));}
 sealed class Plane {public PointF SpineTop,SpineFront,LeftTop,LeftFront,RightTop,RightFront;public RectangleF Visible;}
 // Artist-calibrated positions on the actual held books in a 192px render.
 // The tail makes the pixel character/book noticeably left of the canvas center.
 static Plane Layout(string style){return style=="pixel"?new Plane{SpineTop=new PointF(80.5f,105),SpineFront=new PointF(82,108),LeftTop=new PointF(55,97.5f),LeftFront=new PointF(54,99.5f),RightTop=new PointF(107,97),RightFront=new PointF(111,99),Visible=new RectangleF(50,94,65,17)}:new Plane{SpineTop=new PointF(89,108),SpineFront=new PointF(91,111.5f),LeftTop=new PointF(68,102),LeftFront=new PointF(66,104),RightTop=new PointF(115,101.5f),RightFront=new PointF(118,104),Visible=new RectangleF(61,99,62,16)};}
 static PointF Mix(PointF a,PointF b,float t){return new PointF(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t);}
 public static void DrawPage(Graphics g,Size texture,string style,float progress){if(progress<=0)return;var plane=Layout(style);float t=Math.Max(0,Math.Min(1,progress)),side=(float)Math.Cos(Math.PI*t),reach=Math.Abs(side),lift=(float)Math.Sin(Math.PI*t)*6;
  var upper=Mix(plane.SpineTop,side>=0?plane.RightTop:plane.LeftTop,reach);var lower=Mix(plane.SpineFront,side>=0?plane.RightFront:plane.LeftFront,reach);upper.Y-=lift;lower.Y-=lift;float curl=.7f*(float)Math.Sin(Math.PI*t);
  // Convert the book's own calibrated plane back to the cached texture space.
  float scale=Math.Min(176f/texture.Width,176f/texture.Height),left=(192-texture.Width*scale)/2,top=184-texture.Height*scale;var saved=g.Save();using(var transform=new Matrix(1/scale,0,0,1/scale,-left/scale,-top/scale))g.MultiplyTransform(transform,MatrixOrder.Prepend);g.SetClip(plane.Visible,CombineMode.Intersect);g.SmoothingMode=SmoothingMode.AntiAlias;
  using(var page=new GraphicsPath()){page.AddBezier(plane.SpineTop,Mix(plane.SpineTop,upper,.4f),new PointF(upper.X-curl,upper.Y-.4f),upper);page.AddLine(upper,lower);page.AddBezier(lower,new PointF(lower.X+curl,lower.Y+.4f),Mix(plane.SpineFront,lower,.4f),plane.SpineFront);page.CloseFigure();using(var fill=new SolidBrush(t<.5f?Color.FromArgb(255,253,240):Color.FromArgb(249,244,229)))g.FillPath(fill,page);using(var pen=new Pen(Color.FromArgb(153,125,174),.6f)){pen.LineJoin=LineJoin.Round;g.DrawPath(pen,page);}}
  // The leaf is bound along this fixed gutter, down to the visible cover seam.
  // Both ends belong to the same page path; never lift the inner edge with the tip.
  using(var join=new Pen(Color.FromArgb(175,144,121),.85f)){join.StartCap=join.EndCap=LineCap.Round;g.DrawLine(join,plane.SpineTop,new PointF(plane.SpineFront.X,plane.SpineFront.Y+.7f));}g.Restore(saved);
 }
 static readonly Dictionary<string,Lazy<Bitmap[]>> transitions=CreateTransitions();
 static Dictionary<string,Lazy<Bitmap[]>> CreateTransitions(){var result=new Dictionary<string,Lazy<Bitmap[]>>();foreach(string name in new[]{"pixel","anime"})foreach(bool incoming in new[]{true,false}){string style=name;bool enter=incoming;result.Add(style+(enter?"-in":"-out"),new Lazy<Bitmap[]>(()=>ReadingPoseMorph.Frames(enter?ActivityArt.Frame(style,"read",7):Thought(style),enter?Thought(style):ActivityArt.Frame(style,"read",0),style,enter),true));}return result;}
 static readonly Dictionary<string,Lazy<Bitmap>> thoughts=CreateThoughts();
 static Dictionary<string,Lazy<Bitmap>> CreateThoughts(){var result=new Dictionary<string,Lazy<Bitmap>>();foreach(string name in new[]{"pixel","anime"}){string style=name;result.Add(style,new Lazy<Bitmap>(()=>MakeThought(Art.OriginalReading(style),ActivityArt.Frame(style,"read",0)),true));}return result;}
 public static Bitmap Thought(string style){Lazy<Bitmap> image;return thoughts.TryGetValue(style,out image)?image.Value:null;}
 public static float HeadCenter(Bitmap image){var bounds=ActivityArt.OpaqueBounds(image);double sum=0,mass=0;for(int y=bounds.Top;y<bounds.Top+bounds.Height*.32;y++)for(int x=bounds.Left;x<bounds.Right;x++){int alpha=image.GetPixel(x,y).A;if(alpha<32)continue;sum+=x*alpha;mass+=alpha;}return mass==0?image.Width/2f:(float)(sum/mass);}
 static Bitmap MakeThought(Bitmap original,Bitmap reference){if(original==null)return (Bitmap)reference.Clone();var source=ActivityArt.OpaqueBounds(original);var target=ActivityArt.OpaqueBounds(reference);float anchor=HeadCenter(reference),sourceAnchor=HeadCenter(original),scale=target.Height/(float)source.Height;scale=Math.Min(scale,(anchor-4)/Math.Max(1,sourceAnchor-source.Left));scale=Math.Min(scale,(reference.Width-4-anchor)/Math.Max(1,source.Right-sourceAnchor));var result=new Bitmap(reference.Width,reference.Height,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(result)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;using(var attributes=new ImageAttributes()){attributes.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(original,Rectangle.Round(new RectangleF(anchor-sourceAnchor*scale,target.Bottom-source.Bottom*scale,original.Width*scale,original.Height*scale)),0,0,original.Width,original.Height,GraphicsUnit.Pixel,attributes);}}return result;}
}
}
