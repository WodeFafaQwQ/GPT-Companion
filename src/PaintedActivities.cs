using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
namespace DragonCompanion {
// Play the original painted poses. Arms, hands and props are never stretched,
// rotated separately, or reconstructed with joints.
public sealed class PaintedActivity {
 public readonly Bitmap Source,StaticBody,ActionMask;
 public readonly Bitmap[] Frames,Originals,PropMasks;
 public readonly Rectangle MotionArea;
 internal PaintedActivity(string style,string key){
  bool tea=key=="tea",pixel=style=="pixel";int reference=tea?7:0;

  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(style+(tea?"-daily-8.png":"-tasks-8.png")))using(var atlas=new Bitmap(stream)){
   var poses=ActivitySprites.SplitRow(atlas,tea?2:0);Originals=new Bitmap[8];
   try{
    var boxes=new Rectangle[8];var heads=new float[8];
    for(int i=0;i<8;i++){boxes[i]=ActivityArt.OpaqueBounds(poses[i]);heads[i]=ReadingBook.HeadCenter(poses[i]);}
    float originalScale=Math.Min(176f/poses[reference].Width,176f/poses[reference].Height);
    float height=boxes[reference].Height*originalScale,floor=184-(poses[reference].Height-boxes[reference].Bottom)*originalScale;
    for(int i=0;i<8;i++)height=Math.Min(height,176f*boxes[i].Height/boxes[i].Width);
    // One uniform whole-image transform per drawing. Fix head centre and the
    // foot baseline without stretching, masking or splicing any body parts.
    float anchor=96;
    for(int pass=0;pass<15;pass++){
     float refScale=height/boxes[reference].Height;
     anchor=(192-poses[reference].Width*refScale)/2+heads[reference]*refScale;
     bool fits=true;for(int i=0;i<8;i++){float scale=height/boxes[i].Height;fits&=anchor-(heads[i]-boxes[i].Left)*scale>=8&&anchor+(boxes[i].Right-heads[i])*scale<=184;}
     if(fits)break;height*=.98f;
    }
    for(int i=0;i<8;i++){
     float scale=height/boxes[i].Height;
     Originals[i]=new Bitmap(192,192);
     using(var g=Graphics.FromImage(Originals[i])){
      g.InterpolationMode=pixel?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;
      g.DrawImage(poses[i],new RectangleF(anchor-heads[i]*scale,floor-boxes[i].Bottom*scale,poses[i].Width*scale,poses[i].Height*scale));
     }
    }
   }finally{foreach(var pose in poses)pose.Dispose();}
  }
  // Raster sampling can shift the alpha-weighted head centre by a pixel.
  // Correct that with an integer translation of the ENTIRE finished drawing.
  float finalAnchor=ReadingBook.HeadCenter(Originals[reference]);
  for(int i=0;i<8;i++)if(i!=reference){
   int dx=(int)Math.Round(finalAnchor-ReadingBook.HeadCenter(Originals[i]));
   if(dx==0)continue;var old=Originals[i];var translated=new Bitmap(192,192);
   using(var g=Graphics.FromImage(translated)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(old,dx,0);}
   Originals[i]=translated;old.Dispose();
  }
  Source=Originals[reference];var order=PaintedActivities.Order(style,key);Frames=new Bitmap[order.Length];
  var masks=new Bitmap[8];PropMasks=new Bitmap[8];ActionMask=new Bitmap(192,192);
  for(int i=0;i<8;i++){
   if(PaintedContours.Uses(style,key)){
    masks[i]=PaintedContours.Mask(style,key,Originals[i],i);
    PropMasks[i]=(Bitmap)masks[i].Clone();
   }else{
    PropMasks[i]=tea?PaintedLocks.CupMask(style,Originals[i],i):new Bitmap(192,192);
    masks[i]=PaintedLocks.Mask(style,key,tea?PropMasks[i]:null);
   }
   for(int y=0;y<192;y++)for(int x=0;x<192;x++)if(masks[i].GetPixel(x,y).A>0)ActionMask.SetPixel(x,y,Color.White);
  }
  StaticBody=tea?PaintedLocks.Body(style,Originals,masks,reference,pixel?3:0):(Bitmap)Source.Clone();
  MotionArea=ActivityArt.OpaqueBounds(ActionMask);
  for(int i=0;i<Frames.Length;i++)Frames[i]=PaintedLocks.Frame(StaticBody,Originals[order[i]],masks[order[i]]);
  foreach(var mask in masks)mask.Dispose();
  Source=Frames[0];


 }

}
public static class PaintedActivities {
 static readonly Dictionary<string,Lazy<PaintedActivity>> cache=Create();
 static Dictionary<string,Lazy<PaintedActivity>> Create(){var map=new Dictionary<string,Lazy<PaintedActivity>>();foreach(string s in new[]{"pixel","anime"})foreach(string k in new[]{"tea","working"}){string style=s,key=k;map.Add(style+"-"+key,new Lazy<PaintedActivity>(()=>new PaintedActivity(style,key),true));}return map;}
 public static bool Has(string key){return key=="tea"||key=="working";}
 public static PaintedActivity Get(string style,string key){return cache[style+"-"+key].Value;}
 public static int[] Holds(string key){return key=="tea"?new[]{1000,200,200,200,200,200,250,1600,250,200,200,200,200,200,800}:new[]{250,150,150,150,150,150,150,500};}
 public static int[] Order(string style,string key){if(key!="tea")return new[]{0,1,2,3,4,5,6,7};return style=="pixel"?new[]{7,0,1,2,3,4,5,5,4,3,2,1,0,7,7}:new[]{7,6,5,4,0,1,2,3,2,1,0,4,5,6,7};}
}
}
