using System;
using System.Collections.Generic;
using System.Drawing;
namespace DragonCompanion {
public static partial class SelfTests {
 static double IdleIrisCenter(Bitmap image,string style){
  Rectangle[] windows=style=="pixel"?new[]{new Rectangle(64,66,14,10),new Rectangle(92,66,14,10)}:
   new[]{new Rectangle(76,63,15,9),new Rectangle(100,61,15,9)};
  double total=0;
  foreach(var window in windows){double sum=0,weight=0;for(int y=window.Top;y<window.Bottom;y++)for(int x=window.Left;x<window.Right;x++){
   var p=image.GetPixel(x,y);double w=Math.Max(0,p.B-p.G-15)*Math.Max(0,p.R-p.G-5);if(p.B<80)w=0;sum+=x*w;weight+=w;
  }if(weight<=0)throw new Exception("Cannot locate the painted iris");total+=sum/weight;}
  return total/2;
 }
 static void TestIdleMotion(){
  var starts=new List<long>();int active=0;
  for(long ms=0;ms<85000;ms+=50){long phase=IdleTiming.TailPhase(ms);if(phase==0)starts.Add(ms);if(phase>=0)active++;}
  var gaps=new HashSet<long>();bool pauses=starts.Count==4;for(int i=1;i<starts.Count;i++){long gap=starts[i]-starts[i-1];gaps.Add(gap);pauses&=gap-2800>=14000;}
  Check(pauses&&gaps.Count==3&&active*50==11200,"Idle tail uses distinct long quiet intervals instead of continuously repeating a short motion");
  foreach(string style in new[]{"pixel","anime"}){
   var source=IdleArt.Source(style);string sourceHash=SpriteHash(source);var eyeArea=IdleArt.EyeArea(style);var tailArea=IdleArt.TailArea(style);
   var blinkImages=new HashSet<string>();for(int ms=2250;ms<=2550;ms+=25)using(var frame=IdleArt.Frame(style,ms))blinkImages.Add(SpriteHash(frame));
   Check(blinkImages.Count==2,style+" blink uses the native open and opaque closed cels without half-transparent double eyelids");
   using(var neutral=IdleArt.Frame(style,0))using(var right=IdleArt.Frame(style,10400))using(var left=IdleArt.Frame(style,16000)){
    double mid=IdleIrisCenter(neutral,style),rx=IdleIrisCenter(right,style),lx=IdleIrisCenter(left,style);
    Check(rx>mid+.12&&lx<mid-.12,style+" rendered pupils look to BOTH sides while the face stays still: "+lx.ToString("F2")+" / "+mid.ToString("F2")+" / "+rx.ToString("F2"));
    bool lashesFixed=true;var lashes=style=="pixel"?new[]{new Rectangle(62,62,16,3),new Rectangle(90,62,16,3)}:
     new[]{new Rectangle(76,60,16,2),new Rectangle(100,58,16,2)};
    foreach(var window in lashes)for(int y=window.Top;y<window.Bottom;y++)for(int x=window.Left;x<window.Right;x++)lashesFixed&=neutral.GetPixel(x,y)==right.GetPixel(x,y)&&neutral.GetPixel(x,y)==left.GetPixel(x,y);
    double ignored;var rightChange=ChangedArea(neutral,right,out ignored);var leftChange=ChangedArea(neutral,left,out ignored);
    Check(lashesFixed&&IdleArt.IrisArea(style).Contains(rightChange)&&IdleArt.IrisArea(style).Contains(leftChange),style+" gaze fixes upper eyelashes and moves only inside the iris openings");
   }
   bool bodyFixed=true,alphaFixed=true,feetFixed=true;
   foreach(long ms in new long[]{2250,2300,2350,2499,2500,5000,5050,6375,7750,7800,9600,10400,10850,11400,15500,16000,16750,20650,21050,25400,26750,29200,29600,31600,46600,48000})using(var frame=IdleArt.Frame(style,ms)){
    feetFixed&=ActivityArt.OpaqueBounds(frame).Bottom==ActivityArt.OpaqueBounds(source).Bottom;
    for(int y=0;y<192;y++)for(int x=0;x<192;x++){
     var a=source.GetPixel(x,y);var b=frame.GetPixel(x,y);
     if(!eyeArea.Contains(x,y)&&!tailArea.Contains(x,y))bodyFixed&=a==b;
     if(!tailArea.Contains(x,y))alphaFixed&=a.A==b.A;
    }
   }
   Check(bodyFixed&&alphaFixed&&feetFixed,style+" idle keeps every non-eye/non-tail pixel, body alpha and foot baseline fixed across all event boundaries ("+bodyFixed+", "+alphaFixed+", "+feetFixed+")");
   using(var before=IdleArt.Frame(style,0))using(var after=IdleArt.Frame(style,17950))using(var negative=IdleArt.Frame(style,-1)){
    Check(SpriteHash(before)==SpriteHash(after)&&SpriteHash(before)==SpriteHash(negative),style+" idle preview begins and ends in the same quiet pose with no loop snap");
   }
   Check(SpriteHash(source)==sourceHash,style+" idle animation never modifies its original fixed illustration");
  }
 }
}
}
