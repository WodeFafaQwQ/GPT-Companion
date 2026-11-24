using System;
using System.Drawing;
using System.Drawing.Imaging;
namespace DragonCompanion {
public static partial class SelfTests {
 static Bitmap RenderActivity(string style,string key,int tick){var image=new Bitmap(192,192);using(var graphics=Graphics.FromImage(image))Art.Draw(graphics,new Rectangle(0,0,192,192),style,key,tick);return image;}
 static double MotionLength(PointF a,PointF b){return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}
 static Bitmap RenderLargeReminder(int tick){var image=new Bitmap(384,384);using(var g=Graphics.FromImage(image))Art.Draw(g,new Rectangle(0,0,384,384),"anime","reminder",tick);return image;}
 static double ReminderBellCenter(Bitmap image){double sum=0,count=0;for(int y=246;y<302;y++)for(int x=208;x<280;x++){var p=image.GetPixel(x,y);if(p.A>240&&p.R>140&&p.G>80&&p.B<160&&p.R-p.B>40&&p.G-p.B>10){sum+=x/2.0;count++;}}if(count<20)throw new Exception("Rendered bell cannot be located");return sum/count;}
 static void TestActivityMotion(){
  double acceleration=(CelebrationFx.Jump(340)-2*CelebrationFx.Jump(300)+CelebrationFx.Jump(260))/(.04*.04);Check(Math.Abs(acceleration+CelebrationFx.Gravity)<.05&&CelebrationFx.JumpVelocity(220)>0&&CelebrationFx.JumpVelocity(540)<0,"Celebration retains accelerated ascent/descent and grounded pauses");
  bool quiet=true;float minTail=0,maxTail=0;
  for(long ms=0;ms<18000;ms+=50){float angle=IdleArt.TailAngleAt(ms);if(ms<5000||ms>=7800)quiet&=angle==0;minTail=Math.Min(minTail,angle);maxTail=Math.Max(maxTail,angle);}
  Check(quiet&&minTail<-1&&maxTail>1,"Idle tail has a long quiet interval and a gentle two-sided sway, rather than constant fidgeting");
  foreach(string style in new[]{"pixel","anime"}){
   foreach(string key in new[]{"working"})Check(SpriteHash(ActivityModels.Source(style,key))==SpriteHash(ActivityModels.Frame(style,key,0)),style+" "+key+" starts with the original complete illustration instead of rebuilt hands or body");
   var first=ActivityModels.Frame(style,"working",0);var typing=ActivityModels.Frame(style,"working",3);double unused;var changed=ChangedArea(first,typing,out unused);var hands=PaintedActivities.Get(style,"working").MotionArea;Check(!changed.IsEmpty&&hands.Contains(changed),style+" painted typing displays a distinct complete pose");Check(PaintedActivities.Get(style,"working").Originals.Length==8&&ActivityArt.Find("working").Holds.Length==8,style+" typing plays eight painted poses rather than a deformation field");
   var typingFrames=PaintedActivities.Get(style,"working").Frames;int minFloor=192,maxFloor=0;float minHead=192,maxHead=0;
   foreach(var frame in typingFrames){var box=ActivityArt.OpaqueBounds(frame);minFloor=Math.Min(minFloor,box.Bottom);maxFloor=Math.Max(maxFloor,box.Bottom);float head=ReadingBook.HeadCenter(frame);minHead=Math.Min(minHead,head);maxHead=Math.Max(maxHead,head);}
   Check(maxFloor-minFloor<=1&&maxHead-minHead<=1.5f,style+" whole painted typing poses keep a common floor and head anchor");
   var plain=ActivityModels.Frame(style,"reminder",0);var swung=ActivityModels.Frame(style,"reminder",16);var bellChange=ChangedArea(plain,swung,out unused);Check(!bellChange.IsEmpty&&ActivityModels.BellArea(style).Contains(bellChange),style+" reminder changes only the original bell area");var skinArea=ActivityBell.HandArea(style);var gripModel=new ActivityBell(ActivityModels.Source(style,"reminder"),style);bool handFixed=true;int skin=0;for(int y=skinArea.Top;y<skinArea.Bottom;y++)for(int x=skinArea.Left;x<skinArea.Right;x++){var p=(style=="pixel"?ActivityModels.Source(style,"reminder"):plain).GetPixel(x,y);if(!gripModel.FingerCovers(x,y))continue;skin++;handFixed&=p==plain.GetPixel(x,y)&&Math.Abs(p.R-swung.GetPixel(x,y).R)<=3&&Math.Abs(p.G-swung.GetPixel(x,y).G)<=3&&Math.Abs(p.B-swung.GetPixel(x,y).B)<=3;}Check(skin>5&&handFixed,style+" fixed grip stays still while the bell swings");
   var original=ActivityModels.Source(style,"reminder");var correction=ChangedArea(original,plain,out unused);if(style=="pixel")Check(!correction.IsEmpty&&ActivityModels.BellArea(style).Contains(correction),style+" corrected hanging bell preserves the original figure outside the bell layers");else {using(var a=plain.Clone(new Rectangle(15,15,155,80),PixelFormat.Format32bppArgb))using(var b=swung.Clone(new Rectangle(15,15,155,80),PixelFormat.Format32bppArgb))Check(SpriteHash(a)==SpriteHash(b),"Anime high-resolution reminder keeps the complete head and horns static");}
   var bell=gripModel;var ring=bell.SuspensionPoint;double nearest=100;int fixedGripPixels=0;for(int y=skinArea.Top;y<skinArea.Bottom;y++)for(int x=skinArea.Left;x<skinArea.Right;x++){var p=(style=="pixel"?original:plain).GetPixel(x,y);if(!bell.FingerCovers(x,y))continue;nearest=Math.Min(nearest,MotionLength(ring,new PointF(x,y)));bool fixedPixel=true;for(int frame=0;frame<64;frame++){var q=ActivityModels.Frame(style,"reminder",frame).GetPixel(x,y);fixedPixel&=Math.Abs(p.R-q.R)<=3&&Math.Abs(p.G-q.G)<=3&&Math.Abs(p.B-q.B)<=3;}if(fixedPixel)fixedGripPixels++;else Check(false,style+" fixed fingers must cover the ring at every swing angle");}Check(nearest<=2&&fixedGripPixels>5,style+" upper pull-ring is attached at the original fingers, which stay in front through the full swing");
   if(style=="anime"){
    int oldGlints=0,remainingGlints=0;
    for(int y=105;y<115;y++)for(int x=130;x<142;x++){
     var a=original.GetPixel(x,y);var b=plain.GetPixel(x,y);
     if(a.R>200&&a.G>170&&a.B>130&&a.R-a.B>20)oldGlints++;
     if(b.R>200&&b.G>170&&b.B>130&&b.R-b.B>20)remainingGlints++;
    }
    Check(oldGlints>=3&&remainingGlints<oldGlints/3,"Anime bell highlights do not remain as stationary fragments on the original wing");
    using(var neutral=RenderLargeReminder(0))using(var left=RenderLargeReminder(16))using(var right=RenderLargeReminder(48)){
     double mid=ReminderBellCenter(neutral),lx=ReminderBellCenter(left),rx=ReminderBellCenter(right);
     Check(lx<ring.X-1&&rx>ring.X+1&&Math.Abs((lx+rx)/2-mid)<.8,"Rendered anime bell swings across BOTH sides of the grip with a balanced upright centre: "+lx.ToString("F2")+" / "+mid.ToString("F2")+" / "+rx.ToString("F2"));
     using(var enlarged=new Bitmap(384,384)){
      using(var g=Graphics.FromImage(enlarged)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;g.DrawImage(plain,new Rectangle(0,0,384,384));}
      int detail=0;for(int y=226;y<250;y++)for(int x=234;x<254;x++){var a=neutral.GetPixel(x,y);var b=enlarged.GetPixel(x,y);if(Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B)>9)detail++;}
      Check(detail>20,"Anime grip draws native high-resolution finger and cuff detail instead of enlarging the cached 192px frame");
     }
    }
   }
   foreach(string action in new[]{"tea","working"}){
    var painted=PaintedActivities.Get(style,action);bool bodyFixed=true,silhouetteFixed=true;
    var mask=painted.ActionMask;foreach(var frame in painted.Frames)for(int y=0;y<192;y++)for(int x=0;x<192;x++){
     var source=painted.Source.GetPixel(x,y);var pixel=frame.GetPixel(x,y);silhouetteFixed&=source.A==pixel.A;
     if(mask.GetPixel(x,y).A==0||source.A<245)bodyFixed&=source==pixel;
    }
    Check(bodyFixed&&silhouetteFixed,style+" "+action+" keeps every non-action body pixel and the entire silhouette fixed");
    bool chestFixed=true;var chest=PaintedLocks.Chest(style);var order=PaintedActivities.Order(style,action);
    for(int i=0;i<painted.Frames.Length;i++)for(int y=chest.Top;y<chest.Bottom;y++)for(int x=chest.Left;x<chest.Right;x++)if(painted.PropMasks[order[i]].GetPixel(x,y).A==0)chestFixed&=painted.Frames[i].GetPixel(x,y)==painted.StaticBody.GetPixel(x,y);
    Check(chestFixed,style+" "+action+" exposed chest and collar always use ONE fixed background instead of changing with the arms");
    if(PaintedContours.Uses(style,action)){
     bool completeConnections=true;int checkedPixels=0;
     for(int i=0;i<painted.Frames.Length;i++){
      bool low=order[i]>=4;
      Point[] stations=action=="tea"?
       (low?new[]{new Point(77,107),new Point(76,119),new Point(72,129),new Point(111,107),new Point(112,119),new Point(116,129),new Point(92,123)}:
        new[]{new Point(77,104),new Point(74,114),new Point(72,126),new Point(111,104),new Point(114,114),new Point(116,126),new Point(92,116)}):
       (style=="pixel"?new[]{new Point(70,104),new Point(72,106),new Point(72,109),new Point(103,105),new Point(110,107),new Point(105,110)}:
        new[]{new Point(74,121),new Point(76,122),new Point(77,124),new Point(114,121),new Point(112,122),new Point(111,124)});
      foreach(var p in stations){
       completeConnections&=painted.PropMasks[order[i]].GetPixel(p.X,p.Y).A==255;
       var native=painted.Originals[order[i]].GetPixel(p.X,p.Y);var actual=painted.Frames[i].GetPixel(p.X,p.Y);
       completeConnections&=native.A>=245&&actual.R==native.R&&actual.G==native.G&&actual.B==native.B;checkedPixels++;
      }
     }
     Check(completeConnections&&checkedPixels>40,style+" "+action+" preserves the original painted moving parts without cutting a palm, finger or cup base");
     if(action=="tea"){
      bool palmJoined=true;for(int i=0;i<painted.Frames.Length;i++){
       bool low=order[i]>=4;var bridge=low?new Rectangle(103,113,5,7):new Rectangle(104,104,5,6);
       for(int y=bridge.Top;y<bridge.Bottom;y++)for(int x=bridge.Left;x<bridge.Right;x++){
        palmJoined&=painted.PropMasks[order[i]].GetPixel(x,y).A==255;
        var native=painted.Originals[order[i]].GetPixel(x,y);var actual=painted.Frames[i].GetPixel(x,y);
        palmJoined&=native.R==actual.R&&native.G==actual.G&&native.B==actual.B;
       }
      }
      Check(palmJoined,"Anime tea right palm remains continuous between the asymmetric cup handle and sleeve in all poses");
      bool forearmsComplete=true;int forearmPixels=0;
      for(int i=0;i<painted.Frames.Length;i++){
       var corridors=order[i]<4?new[]{new Rectangle(64,107,17,21),new Rectangle(103,107,12,22)}:
        new[]{new Rectangle(72,104,13,20),new Rectangle(100,104,15,21)};
       foreach(var corridor in corridors)
       for(int y=corridor.Top;y<corridor.Bottom;y++)for(int x=corridor.Left;x<corridor.Right;x++){
        var native=painted.Originals[order[i]].GetPixel(x,y);
        bool nativeSkin=native.A>=245&&native.R>185&&native.G>125&&native.G<235&&native.R-native.B>20&&native.R-native.G>7&&native.G-native.B>5;
        if(!nativeSkin)continue;forearmPixels++;var actual=painted.Frames[i].GetPixel(x,y);
        forearmsComplete&=actual.R==native.R&&actual.G==native.G&&actual.B==native.B;
       }
      }
      Check(forearmsComplete&&forearmPixels>500,"Anime tea preserves the entire painted skin corridor on BOTH forearms in raised AND lowered poses, including shoulder and wrist joins");
     }
     if(action=="working"){
      bool upperArmFixed=true;var roots=style=="pixel"?new[]{new Rectangle(70,92,9,8),new Rectangle(101,92,10,8)}:new[]{new Rectangle(73,106,10,11),new Rectangle(105,106,10,11)};
      foreach(var f in painted.Frames)foreach(var root in roots)for(int y=root.Top;y<root.Bottom;y++)for(int x=root.Left;x<root.Right;x++)upperArmFixed&=f.GetPixel(x,y)==painted.StaticBody.GetPixel(x,y);
      Check(upperArmFixed,style+" typing fixes every shoulder, upper-arm and cuff pixel while the original fingers tap");
      var keys=style=="pixel"?new Rectangle(59,115,48,7):new Rectangle(66,131,51,4);bool keyboardFixed=true;
      foreach(var f in painted.Frames)for(int y=keys.Top;y<keys.Bottom;y++)for(int x=keys.Left;x<keys.Right;x++)keyboardFixed&=f.GetPixel(x,y)==painted.StaticBody.GetPixel(x,y);
      Check(keyboardFixed,style+" typing keeps the keyboard and its key rows still below the moving fingers");
     }
    }
    var face=new Rectangle(0,0,192,style=="pixel"?82:91);using(var fixedFace=painted.Source.Clone(face,PixelFormat.Format32bppArgb))foreach(var frame in painted.Frames)using(var f=frame.Clone(face,PixelFormat.Format32bppArgb))Check(SpriteHash(f)==SpriteHash(fixedFace),style+" "+action+" leaves the face, hair and horns still");
   }
   var tailRest=TailSway.Curve(style,0);double farthest=0;bool tailLengths=true;
   for(int ms=0;ms<2800;ms+=50){var curve=TailSway.Curve(style,ms);tailLengths&=curve[0]==tailRest[0];for(int i=1;i<curve.Length;i++)tailLengths&=Math.Abs(MotionLength(curve[i-1],curve[i])-MotionLength(tailRest[i-1],tailRest[i]))<.01;farthest=Math.Max(farthest,MotionLength(curve[curve.Length-1],tailRest[tailRest.Length-1]));}
   Check(tailLengths&&farthest>(style=="pixel"?10:4),style+" tail root stays anchored, segment lengths are preserved, and the tip has a clearly larger travel: "+farthest.ToString("F1")+"px");
   using(var still=IdleArt.Frame(style,0))using(var tail=IdleArt.Frame(style,6375))using(var blink=IdleArt.Frame(style,2400)){
    double ignored;var tailChanges=ChangedArea(still,tail,out ignored);var blinkChanges=ChangedArea(still,blink,out ignored);
    Check(!tailChanges.IsEmpty&&IdleArt.TailArea(style).Contains(tailChanges),style+" idle sway moves only the tail tip");
    Check(!blinkChanges.IsEmpty&&IdleArt.EyeArea(style).Contains(blinkChanges),style+" idle blink never swaps or moves the whole character");
   }
   var tea=PaintedActivities.Get(style,"tea");Check(SpriteHash(tea.Frames[0])==SpriteHash(tea.Frames[tea.Frames.Length-1])&&PaintedActivities.Order(style,"tea")[7]==(style=="pixel"?5:3)&&ActivityArt.Find("tea").Holds[7]>=1500,style+" tea follows the original raise/sip/lower poses, holds the sip, and returns to the exact same resting image");
   using(var ground=RenderActivity(style,"complete",0))using(var apex=RenderActivity(style,"complete",8))using(var landed=RenderActivity(style,"complete",14)){int floor=ActivityArt.OpaqueBounds(ground).Bottom;Check(floor-ActivityArt.OpaqueBounds(apex).Bottom>=4&&floor==ActivityArt.OpaqueBounds(landed).Bottom,style+" accelerated hop returns to the same ground line");}
  }
  TestIdleMotion();
 }
}
}
