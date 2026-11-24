using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
namespace DragonCompanion {
public static class ActivityModels {
 sealed class Motion {public Bitmap Source;public Bitmap[] Frames;public ActivityBell Bell;}
 static readonly Dictionary<string,Lazy<Motion>> cache=CreateCache();
 static Dictionary<string,Lazy<Motion>> CreateCache(){var result=new Dictionary<string,Lazy<Motion>>();foreach(string s in new[]{"pixel","anime"})foreach(string k in new[]{"working","tea","reminder"}){string style=s,key=k;result.Add(style+"-"+key,new Lazy<Motion>(()=>Load(style,key),true));}return result;}
 public static bool Has(string key){return PaintedActivities.Has(key)||key=="reminder";}
 public static void Prepare(string style){foreach(string key in new[]{"working","tea","reminder"}){var ready=cache[style+"-"+key].Value;}}
 public static Bitmap Source(string style,string key){return cache[style+"-"+key].Value.Source;}
 public static Rectangle BellArea(string style){return cache[style+"-reminder"].Value.Bell.MotionArea;}
 static Motion Load(string style,string key){
  var motion=new Motion();
  if(PaintedActivities.Has(key)){var painted=PaintedActivities.Get(style,key);motion.Source=painted.Source;motion.Frames=painted.Frames;return motion;}
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(style+"-tasks-8.png"))using(var atlas=new Bitmap(stream)){
   var poses=ActivitySprites.SplitRow(atlas,3);
   try{var plate=poses[0];motion.Source=new Bitmap(192,192);using(var g=Graphics.FromImage(motion.Source)){
    float scale=Math.Min(176f/plate.Width,176f/plate.Height);g.InterpolationMode=style=="pixel"?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;
    g.DrawImage(plate,Rectangle.Round(new RectangleF((192-plate.Width*scale)/2,184-plate.Height*scale,plate.Width*scale,plate.Height*scale)));
   }}finally{foreach(var pose in poses)pose.Dispose();}
  }
  motion.Bell=new ActivityBell(motion.Source,style);var clip=ActivityArt.Find(key);motion.Frames=new Bitmap[clip.Holds.Length];int elapsed=0;
  for(int i=0;i<motion.Frames.Length;i++){motion.Frames[i]=motion.Bell.Frame(elapsed);elapsed+=clip.Holds[i];}return motion;
 }
 public static Bitmap Frame(string style,string key,int index){var frames=cache[style+"-"+key].Value.Frames;return frames[Math.Max(0,index)%frames.Length];}
 public static bool Draw(Graphics graphics,Rectangle bounds,string style,string key,long milliseconds){
  if(!Has(key))return false;var clip=ActivityArt.Find(key);var motion=cache[style+"-"+key].Value;
  if(style=="anime"&&key=="reminder"){motion.Bell.DrawAnime(graphics,bounds,milliseconds);return true;}
  var frame=motion.Frames[clip.FrameAt((int)(Math.Max(0,milliseconds)%clip.Duration))];var saved=graphics.Save();float side=Math.Min(bounds.Width,bounds.Height);
  graphics.InterpolationMode=style=="pixel"?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.Half;
  graphics.DrawImage(frame,new RectangleF(bounds.X+(bounds.Width-side)/2,bounds.Bottom-side,side,side),new RectangleF(0,0,192,192),GraphicsUnit.Pixel);graphics.Restore(saved);return true;
 }
}
}
