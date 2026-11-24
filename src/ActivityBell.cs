using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
namespace DragonCompanion {
// Original fingers cover the pull-ring; the rigid bell hangs below that grip.
public sealed class ActivityBell {
 readonly Bitmap background,bell,hand;
 readonly PointF pivot,bodyTop;
 readonly AnimeBellArt animeArt;
 public readonly Rectangle MotionArea;
 static bool Skin(Color p){return p.A>0&&p.R>170&&p.G>115&&p.B>130&&p.R-p.B<95;}
 public ActivityBell(Bitmap source,string style){
  bool pixel=style=="pixel";
  if(!pixel){animeArt=new AnimeBellArt();pivot=AnimeBellArt.Grip;MotionArea=new Rectangle(101,110,45,44);return;}
  var area=pixel?new Rectangle(97,87,24,28):new Rectangle(122,95,27,30);
  var skinArea=HandArea(style);
  var mask=new bool[192*192];
  for(int y=area.Top;y<area.Bottom;y++)for(int x=area.Left;x<area.Right;x++){
   var p=source.GetPixel(x,y);
   mask[y*192+x]=p.A>=32&&!(skinArea.Contains(x,y)&&Skin(p))&&p.R>125&&p.G>65&&p.B<170&&p.R-p.B>35&&p.G>p.B;
  }
  // Retain the original bell highlights and outline.
  for(int y=area.Top;y<area.Bottom;y++){
   int left=area.Right,right=area.Left-1;
   for(int x=area.Left;x<area.Right;x++)if(mask[y*192+x]){left=Math.Min(left,x);right=Math.Max(right,x);}
   for(int x=left;x<=right;x++)if(!(skinArea.Contains(x,y)&&Skin(source.GetPixel(x,y))))mask[y*192+x]=true;
  }
  var expanded=(bool[])mask.Clone();
  for(int y=area.Top+1;y<area.Bottom-1;y++)for(int x=area.Left+1;x<area.Right-1;x++){
   if(!mask[y*192+x])continue;
   for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
    var p=source.GetPixel(x+dx,y+dy);
    if(p.A>=32&&!(skinArea.Contains(x+dx,y+dy)&&Skin(p)))expanded[(y+dy)*192+x+dx]=true;
   }
  }
  // Gold hair ornaments and skin outlines must stay on the original figure.
  mask=LargestPart(expanded,area);
  background=(Bitmap)source.Clone();bell=new Bitmap(192,192,PixelFormat.Format32bppArgb);hand=new Bitmap(192,192,PixelFormat.Format32bppArgb);
  int top=192,leftBound=192,rightBound=0,bottom=0,count=0;
  for(int y=area.Top;y<area.Bottom;y++)for(int x=area.Left;x<area.Right;x++){
   if(!mask[y*192+x])continue;
   var p=source.GetPixel(x,y);bell.SetPixel(x,y,p);count++;
   top=Math.Min(top,y);leftBound=Math.Min(leftBound,x);rightBound=Math.Max(rightBound,x);bottom=Math.Max(bottom,y);
  }
  RestoreCoveredBackground(style,background,mask,area);
  for(int y=skinArea.Top;y<skinArea.Bottom;y++)for(int x=skinArea.Left;x<skinArea.Right;x++){
   var p=source.GetPixel(x,y);if(Skin(p))hand.SetPixel(x,y,p);
  }
  if(count<10)throw new InvalidOperationException("Bell extraction failed: "+style);
  double sum=0;int roots=0;
  for(int y=top;y<=Math.Min(bottom,top+3);y++)for(int x=leftBound;x<=rightBound;x++)if(mask[y*192+x]){sum+=x;roots++;}
  bodyTop=new PointF(roots>0?(float)(sum/roots):(leftBound+rightBound)/2f,top);
  // The loop meets the existing curled fingers, not the old bell position.
  pivot=pixel?new PointF(109,107):AnimeBellArt.Grip;
  var oldArea=Rectangle.FromLTRB(leftBound,top,rightBound+1,bottom+1);
  var hangingArea=Rectangle.FromLTRB((int)pivot.X-18,(int)pivot.Y-4,(int)pivot.X+21,(int)pivot.Y+bottom-top+12);
  MotionArea=Rectangle.Intersect(new Rectangle(0,0,192,192),Rectangle.Union(oldArea,hangingArea));
 }
 static bool[] LargestPart(bool[] mask,Rectangle area){
  var seen=new bool[mask.Length];var largest=new List<int>();
  for(int y=area.Top;y<area.Bottom;y++)for(int x=area.Left;x<area.Right;x++){
   int start=y*192+x;if(!mask[start]||seen[start])continue;
   var part=new List<int>();var queue=new Queue<int>();queue.Enqueue(start);seen[start]=true;
   while(queue.Count>0){int p=queue.Dequeue();part.Add(p);int px=p%192,py=p/192;
    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
     int nx=px+dx,ny=py+dy;if(!area.Contains(nx,ny))continue;int next=ny*192+nx;
     if(mask[next]&&!seen[next]){seen[next]=true;queue.Enqueue(next);}
    }
   }
   if(part.Count>largest.Count)largest=part;
  }
  var result=new bool[mask.Length];foreach(int p in largest)result[p]=true;return result;
 }
 // Use the edited clean backplate only beneath the old bell. Every original
 // pixel outside that mask, including the original hand, remains unchanged.
 static void RestoreCoveredBackground(string style,Bitmap output,bool[] mask,Rectangle area){
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(style+"-reminder-clean.png"))
  using(var clean=new Bitmap(stream))using(var plate=new Bitmap(192,192)){
   using(var graphics=Graphics.FromImage(plate)){
    graphics.InterpolationMode=style=="pixel"?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;
    // Register the generated clean plate against the unchanged original face,
    // hair and clothing before using its tiny repair patch.
    float scale=style=="pixel"?1.06f:1.04f,offset=style=="pixel"?1:2;
    graphics.PixelOffsetMode=PixelOffsetMode.Half;
    graphics.DrawImage(clean,new RectangleF(-offset/scale,0,192/scale,192/scale));
   }
   for(int y=area.Top;y<area.Bottom;y++)for(int x=area.Left;x<area.Right;x++)if(mask[y*192+x]){
    // The generated backplate can include a slightly shifted copy of a finger.
    // Keep only its background texture; the original hand is the sole hand layer.
    bool finger=false;
    for(int dy=-2;style=="pixel"&&dy<=2;dy++)for(int dx=-2;dx<=2;dx++){
     var c=plate.GetPixel(x+dx,y+dy);
     finger|=c.R>170&&c.G>115&&c.B>115&&c.R-c.G>=8&&c.R-c.B>=14&&c.G>=c.B-6;
    }
    output.SetPixel(x,y,plate.GetPixel(finger?Math.Min(191,x+14):x,y));
   }
  }
 }
 internal PointF SuspensionPoint {get{return pivot;}}
 internal static Rectangle HandArea(string style){return style=="pixel"?new Rectangle(103,104,12,13):new Rectangle(118,114,8,9);}
 internal void DrawAnime(Graphics graphics,Rectangle bounds,long milliseconds){animeArt.Draw(graphics,bounds,milliseconds);}
 internal bool FingerCovers(int x,int y){return animeArt!=null?animeArt.FingerCovers(x,y):hand.GetPixel(x,y).A>=240;}
 public static float Angle(long milliseconds){return 16*(float)Math.Sin(Math.Max(0,milliseconds)*Math.PI*2/3200);}
 public Bitmap Frame(long milliseconds){
  if(animeArt!=null)return animeArt.Frame(milliseconds);
  var result=(Bitmap)background.Clone();
  using(var graphics=Graphics.FromImage(result)){
   graphics.SetClip(MotionArea,CombineMode.Intersect);graphics.SmoothingMode=SmoothingMode.AntiAlias;
   graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.Half;
   graphics.TranslateTransform(pivot.X,pivot.Y);graphics.RotateTransform(Angle(milliseconds));
   // The loop is behind the fixed fingers; the bell joins its lower end.
   using(var outline=new Pen(Color.FromArgb(93,61,75),1.6f))using(var gold=new Pen(Color.FromArgb(247,195,73),.9f)){
    graphics.DrawEllipse(outline,-2.4f,-.5f,4.8f,6.4f);graphics.DrawEllipse(gold,-2.4f,-.5f,4.8f,6.4f);
   }
   graphics.TranslateTransform(-bodyTop.X,5-bodyTop.Y);graphics.DrawImage(bell,new RectangleF(0,0,192,192));
  }
  // Copy original hand pixels last so the fingertips visibly grasp the loop.
  for(int y=MotionArea.Top;y<MotionArea.Bottom;y++)for(int x=MotionArea.Left;x<MotionArea.Right;x++){
   var p=hand.GetPixel(x,y);if(p.A>0)result.SetPixel(x,y,p);
  }
  return result;
 }
}
}
