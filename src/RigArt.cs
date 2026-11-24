using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;

namespace DragonCompanion {
// A fixed torso and continuous, opposing limb trajectories avoid pose-to-pose
// changes in proportions. Both legs use the same cycle, half a period apart.
public static class RigArt {
 sealed class Rig {public Bitmap[] Parts,Wings;}
 static readonly Dictionary<string,Lazy<Rig>> rigs=CreateRigs();
 static Dictionary<string,Lazy<Rig>> CreateRigs(){var cache=new Dictionary<string,Lazy<Rig>>();foreach(string style in new[]{"pixel","anime"}){string name=style;cache.Add(name,new Lazy<Rig>(()=>ReadRig(name),true));}return cache;}
 static Rig GetRig(string style){Lazy<Rig> entry;return rigs.TryGetValue(style,out entry)?entry.Value:null;}
 public static int LoadedAssetCount {get{int count=0;foreach(var rig in rigs.Values)if(rig.IsValueCreated)count++;return count;}}
 public static void Load(){Prepare("pixel");Prepare("anime");}
 public static void Prepare(string style){GetRig(style);}
 static Rig ReadRig(string style){var rig=new Rig();
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(style+"-rig.png")){if(stream!=null)using(var atlas=new Bitmap(stream)){var list=new Bitmap[7];for(int i=0;i<7;i++){Rectangle r=new Rectangle(i%4*(atlas.Width/4),i/4*(atlas.Height/2),atlas.Width/4,atlas.Height/2);if(style=="anime"){
   RectangleF[] regions={new RectangleF(0,0,.34f,.55f),new RectangleF(.36f,0,.15f,.5f),new RectangleF(.57f,0,.15f,.5f),new RectangleF(.8f,0,.2f,.5f),new RectangleF(.08f,.57f,.15f,.4f),new RectangleF(.34f,.57f,.16f,.4f),new RectangleF(.58f,.57f,.15f,.4f)};var q=regions[i];r=Rectangle.Round(new RectangleF(q.X*atlas.Width,q.Y*atlas.Height,q.Width*atlas.Width,q.Height*atlas.Height));}
   using(var cell=atlas.Clone(r,PixelFormat.Format32bppArgb))list[i]=Art.IsolateCharacter(cell);
  }
  // Joint circles and the extra exposed shin are construction guides in the
  // parts atlas. Keep them behind the skirt/boot instead of showing two knees.
  for(int i=3;i<=6;i++){var original=list[i];int top=i>=5?(int)(original.Height*(style=="pixel"?.30:.34)):0;int bottom=i<5?(int)(original.Height*.12):0;list[i]=original.Clone(new Rectangle(0,top,original.Width,original.Height-top-bottom),PixelFormat.Format32bppArgb);original.Dispose();}
  rig.Parts=list;}}
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(style+"-wings.png")){if(stream!=null)using(var atlas=new Bitmap(stream)){var list=new Bitmap[2];for(int i=0;i<2;i++)using(var cell=atlas.Clone(new Rectangle(i*atlas.Width/2,0,atlas.Width/2,atlas.Height),PixelFormat.Format32bppArgb))list[i]=Art.IsolateCharacter(cell);rig.Wings=list;}}
  return rig;
 }
 public static bool Available(string style){var rig=GetRig(style);return rig!=null&&rig.Parts!=null&&rig.Wings!=null;}
 public const float UpperLegLength=9,LowerLegLength=11,BootHeight=17,ShoeHeight=5.1f,GroundY=107.5f;
 public static PointF Foot(double phase){phase-=Math.Floor(phase);if(phase<.5)return new PointF((float)(4-16*phase),GroundY);double u=(phase-.5)*2,lift=Math.Sin(Math.PI*u);return new PointF((float)(-4+8*(u*u*(3-2*u))),(float)(GroundY-1.5*lift*lift));}
 public static float ShoeAngle(double phase){phase-=Math.Floor(phase);if(phase<.5)return 0;double lift=Math.Sin(Math.PI*(phase-.5)*2);return (float)(-10*lift*lift);}
 public static PointF Ankle(PointF sole,float angle){double radians=angle*Math.PI/180;return new PointF(sole.X+(float)Math.Sin(radians)*ShoeHeight,sole.Y-(float)Math.Cos(radians)*ShoeHeight);}
 static void Sprite(Graphics g,Bitmap b,RectangleF r){using(var attr=new ImageAttributes()){attr.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(b,Rectangle.Round(r),0,0,b.Width,b.Height,GraphicsUnit.Pixel,attr);}}
 static void Bone(Graphics g,Bitmap b,PointF start,PointF end,float width,float root=.5f){float dx=end.X-start.X,dy=end.Y-start.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);var saved=g.Save();g.TranslateTransform(start.X,start.Y);g.RotateTransform((float)(-Math.Atan2(dx,dy)*180/Math.PI));Sprite(g,b,new RectangleF(-width*root,-length*.09f,width,length*1.18f));g.Restore(saved);}
 public static PointF Knee(PointF hip,PointF foot,bool fly=false){double a=fly?11:UpperLegLength,b=fly?12:LowerLegLength,dx=foot.X-hip.X,dy=foot.Y-hip.Y,d=Math.Min(a+b-.001,Math.Max(Math.Abs(a-b)+.001,Math.Sqrt(dx*dx+dy*dy)));double baseAngle=Math.Atan2(dy,dx),bend=Math.Acos(Math.Max(-1,Math.Min(1,(a*a+d*d-b*b)/(2*a*d))));double angle=baseAngle-bend;return new PointF(hip.X+(float)(a*Math.Cos(angle)),hip.Y+(float)(a*Math.Sin(angle)));}
 // Thicken the thigh relative to its original proportions, keeping the
 // extra length hidden under the skirt. Overlap covers the knee seam.
 static void LegPart(Graphics g,Bitmap part,PointF start,PointF end,float root){Bone(g,part,start,end,11*1.18f*part.Width/part.Height*1.32f,root);}
 static void Boot(Graphics g,Bitmap boot,PointF knee,PointF sole,float angle){
  float width=BootHeight*boot.Width/boot.Height*1.16f;int split=(int)Math.Round(boot.Height*.7);var ankle=Ankle(sole,angle);double radians=angle*Math.PI/180;float ux=(float)Math.Cos(radians),uy=(float)Math.Sin(radians);
  // The boot shaft follows the knee; the shoe has its own ankle transform.
  // During stance its whole bottom edge remains horizontal at the ground Y.
  var topLeft=new PointF(knee.X-width*.38f*ux,knee.Y-width*.38f*uy-.6f);var topRight=new PointF(topLeft.X+width*ux,topLeft.Y+width*uy);var bottomLeft=new PointF(ankle.X-width*.38f*ux,ankle.Y-width*.38f*uy+.5f);
  using(var attr=new ImageAttributes()){attr.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(boot,new[]{topLeft,topRight,bottomLeft},new Rectangle(0,0,boot.Width,split),GraphicsUnit.Pixel,attr);}
  var saved=g.Save();g.TranslateTransform(sole.X,sole.Y);g.RotateTransform(angle);using(var attr=new ImageAttributes()){attr.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(boot,new[]{new PointF(-width*.38f,-ShoeHeight),new PointF(width*.62f,-ShoeHeight),new PointF(-width*.38f,0)},new Rectangle(0,split,boot.Width,boot.Height-split),GraphicsUnit.Pixel,attr);}g.Restore(saved);
 }
 static void Leg(Graphics g,Bitmap upper,Bitmap lower,PointF hip,PointF sole,float angle,bool fly){var ankle=Ankle(sole,angle);var knee=Knee(hip,ankle,fly);LegPart(g,upper,hip,knee,.5f);Boot(g,lower,knee,sole,angle);}
 static void Wing(Graphics g,Bitmap wing,PointF root,double phase,bool far){var saved=g.Save();g.TranslateTransform(root.X,root.Y);g.RotateTransform((float)(-10+30*Math.Sin(phase)));float height=(float)(29+12*Math.Sin(phase));Sprite(g,wing,new RectangleF(-51,-height,54,height+5));g.Restore(saved);}
 public static bool Draw(Graphics g,Rectangle rect,string style,string state,int tick){var rig=GetRig(style);if(rig==null||rig.Parts==null)return false;var p=rig.Parts;bool fly=state.StartsWith("fly"),left=state.EndsWith("left");double phase=tick/20.0,angle=phase*Math.PI*2;var saved=g.Save();g.TranslateTransform(rect.X,rect.Y);g.ScaleTransform(rect.Width/128f,rect.Height/128f);if(left){g.TranslateTransform(128,0);g.ScaleTransform(-1,1);}g.InterpolationMode=style=="pixel"?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;
  if(fly){g.TranslateTransform(64,64);g.ScaleTransform(.88f,.88f);g.TranslateTransform(-64,-64);g.TranslateTransform(70,70);g.RotateTransform(10);g.TranslateTransform(-70,-70);}
  float bob=fly?(float)Math.Sin(angle)*1.2f:(float)(.3*Math.Cos(angle*2));var hip=new PointF(style=="anime"&&!fly?71:79,(fly?85:83)+bob);var shoulder=new PointF(style=="pixel"?72:65,56+bob);float bodyHeight=94,bodyWidth=bodyHeight*p[0].Width/p[0].Height;
  var w=rig.Wings;bool haveWings=fly&&w!=null;if(haveWings)Wing(g,w[1],new PointF(69,61),angle+.3,true);
  var farFoot=Foot(phase+.5);farFoot.X+=hip.X-2;var nearFoot=Foot(phase);nearFoot.X+=hip.X;if(fly){farFoot=new PointF(70,110);nearFoot=new PointF(77,109);}
  float swing=fly?3:(float)(8*Math.Cos(angle));float armLength=28;Bone(g,p[2],new PointF(shoulder.X+3,shoulder.Y),new PointF(shoulder.X+3+swing,shoulder.Y+armLength),16,.46f);
  Leg(g,p[4],p[6],new PointF(hip.X-2,hip.Y),farFoot,fly?8:ShoeAngle(phase+.5),fly);Leg(g,p[3],p[5],hip,nearFoot,fly?8:ShoeAngle(phase),fly);
  Sprite(g,p[0],new RectangleF(18,4+bob,bodyWidth,bodyHeight));
  if(haveWings)Wing(g,w[0],new PointF(60,61),angle,false);
  Bone(g,p[1],shoulder,new PointF(shoulder.X-swing,shoulder.Y+armLength),17,.46f);g.Restore(saved);return true;
 }
}
}
