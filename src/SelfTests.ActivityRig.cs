using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
namespace DragonCompanion {
public static partial class SelfTests {
 static void TestActivityRig(string folder){
  TestActivityMotion();
  Check(!ActivityArt.Has("stretch")&&ActivityArt.Clips.Count(c=>c.Group=="daily")==3,"Stretch is removed from the action registry and the idle menu contains reading, tea and pillow rest");
  foreach(string style in new[]{"pixel","anime"}){
   var rest=ActivityArt.Find("rest");using(var first=new Bitmap(192,192)){using(var graphics=Graphics.FromImage(first))Art.Draw(graphics,new Rectangle(0,0,192,192),style,"rest",0);bool eyesOnly=true;for(int i=0;i<rest.Holds.Length;i++)using(var sample=new Bitmap(192,192)){using(var graphics=Graphics.FromImage(sample))Art.Draw(graphics,new Rectangle(0,0,192,192),style,"rest",i*2);for(int y=0;y<192;y++)for(int x=0;x<192;x++){bool eye=style=="pixel"?(y>=65&&y<=85&&(x>=58&&x<=82||x>=88&&x<=112)):(y>=104&&y<=124&&(x>=60&&x<=84||x>=100&&x<=124));if(!eye&&first.GetPixel(x,y)!=sample.GetPixel(x,y))eyesOnly=false;}}
    Check(eyesOnly,style+" pillow rest changes only the eyelids; pillow, arms, body and silhouette remain pixel-identical");}
   bool sideways=true,stationaryBody=true;for(int frame=0;frame<64;frame++)for(int y=0;y<192;y+=3)for(int x=0;x<192;x+=3){var d=ActivityRig.Displacement("reminder",style,x,y,frame/64f);sideways&=d.Y==0&&Math.Abs(d.X)<=4.51f;if(y>=132||y<=81)stationaryBody&=d==PointF.Empty;}Check(sideways&&stationaryBody,style+" bell no longer distorts any body bitmap");
   string original=SpriteHash(Art.OriginalCelebration(style));var cheer=ActivityArt.Frame(style,"complete",32);Check(!ActivityArt.OpaqueBounds(cheer).IsEmpty&&SpriteHash(Art.OriginalCelebration(style))==original,style+" celebration retains its original raised-arms source without changing the drawing");
  }
  // The formerly spliced chest receives contradictory colours in all later
  // source frames. The resulting animation must still depend on frame zero only.
  Bitmap[][] sources=new Bitmap[2][];for(int set=0;set<2;set++){sources[set]=new Bitmap[8];for(int frame=0;frame<8;frame++){var image=new Bitmap(192,192);using(var graphics=Graphics.FromImage(image)){graphics.FillEllipse(Brushes.Lavender,new Rectangle(45,20,110,162));graphics.FillRectangle(frame==0?Brushes.Purple:set==0?Brushes.Red:Brushes.Blue,65,87,70,55);}sources[set][frame]=image;}}
  var a=ActivityLayers.Stabilize(sources[0],"working","pixel",36);var b=ActivityLayers.Stabilize(sources[1],"working","pixel",36);try{Check(a.Zip(b,(first,second)=>SpriteHash(first)==SpriteHash(second)).All(s=>s),"Task animation reads its complete atlas drawings rather than arbitrary supplied fragments");}finally{foreach(var image in a)image.Dispose();foreach(var image in b)image.Dispose();}
  using(var first=new Bitmap(192,192))using(var next=new Bitmap(192,192)){using(var graphics=Graphics.FromImage(first))CelebrationFx.Draw(graphics,new Rectangle(0,0,192,192),1000);using(var graphics=Graphics.FromImage(next))CelebrationFx.Draw(graphics,new Rectangle(0,0,192,192),1016);Check(SpriteHash(first)!=SpriteHash(next),"Flowers and ribbons use continuous time trajectories rather than sprite frame stepping");first.Save(Path.Combine(folder,"celebration-particles.png"),ImageFormat.Png);}
 }
}
}
