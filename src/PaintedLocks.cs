using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace DragonCompanion {
internal static class PaintedLocks {
 internal static Rectangle Chest(string style){return style=="pixel"?new Rectangle(77,88,26,47):new Rectangle(83,96,27,47);}
 internal static Bitmap CupMask(string style,Bitmap pose,int index){
  bool pixel=style=="pixel",low=pixel?(index==0||index>=6):index>=4;
  int top=pixel?(low?101:90):(low?110:99),bottom=pixel?(low?117:104):(low?122:108);
  double sx=0,sy=0,count=0;int left=pixel?83:86,right=pixel?95:98;
  for(int y=top;y<bottom;y++)for(int x=left;x<right;x++){
   var c=pose.GetPixel(x,y);bool star=pixel?c.R>205&&c.G>200&&c.B>220:c.R>160&&c.G>120&&c.B<195&&c.R-c.B>30&&c.G-c.B>5;
   if(star){sx+=x;sy+=y;count++;}
  }
  float cx=count>2?(float)(sx/count):(pixel?89:92),cy=count>2?(float)(sy/count):(pixel?(low?109:96):(low?115:103));
  var mask=new Bitmap(192,192);
  using(var g=Graphics.FromImage(mask)){
   g.SmoothingMode=SmoothingMode.AntiAlias;
   var cup=new RectangleF(cx-10.5f,cy-(pixel?11.5f:7.5f),21,pixel?22:19);
   using(var p=Theme.Round(cup,3))g.FillPath(Brushes.White,p);
   g.FillEllipse(Brushes.White,cx-19,cy-9,11,18);g.FillEllipse(Brushes.White,cx+9,cy-9,12,18);
  }
  return mask;
 }
 internal static Bitmap Mask(string style,string key,Bitmap cup){
  bool pixel=style=="pixel";var mask=new Bitmap(192,192);
  using(var g=Graphics.FromImage(mask)){
   g.SmoothingMode=SmoothingMode.AntiAlias;
   if(key=="tea"){
    g.FillEllipse(Brushes.White,pixel?new RectangleF(57,96,23,32):new RectangleF(64,105,24,31));
    g.FillEllipse(Brushes.White,pixel?new RectangleF(95,96,23,32):new RectangleF(104,105,23,31));
   }else{
    g.FillEllipse(Brushes.White,pixel?new RectangleF(60,96,20,15):new RectangleF(66,111,22,14));
    g.FillEllipse(Brushes.White,pixel?new RectangleF(90,96,21,15):new RectangleF(98,111,23,14));
   }
   g.CompositingMode=CompositingMode.SourceCopy;g.FillRectangle(Brushes.Transparent,Chest(style));g.CompositingMode=CompositingMode.SourceOver;
   if(cup!=null)g.DrawImageUnscaled(cup,0,0);
  }
  return mask;
 }
 internal static Bitmap Body(string style,Bitmap[] poses,Bitmap[] masks,int reference,int preferred){
  var source=poses[reference];var body=(Bitmap)source.Clone();
  for(int y=0;y<192;y++)for(int x=0;x<192;x++){
   if(Chest(style).Contains(x,y)&&masks[preferred].GetPixel(x,y).A==0){var clear=poses[preferred].GetPixel(x,y);var original=source.GetPixel(x,y);if(clear.A>=245&&original.A>=245)body.SetPixel(x,y,Color.FromArgb(original.A,clear.R,clear.G,clear.B));}
   if(masks[reference].GetPixel(x,y).A==0)continue;
   for(int k=0;k<9;k++){
    int donor=k==0?preferred:k-1;
    if(donor==reference||masks[donor].GetPixel(x,y).A>0)continue;
    var c=poses[donor].GetPixel(x,y);var a=source.GetPixel(x,y);if(c.A<245||a.A<245)continue;
    body.SetPixel(x,y,Color.FromArgb(a.A,c.R,c.G,c.B));break;
   }
  }
  return body;
 }
 internal static Bitmap Frame(Bitmap body,Bitmap pose,Bitmap mask){
  var result=(Bitmap)body.Clone();var area=ActivityArt.OpaqueBounds(mask);
  for(int y=area.Top;y<area.Bottom;y++)for(int x=area.Left;x<area.Right;x++){
   int alpha=mask.GetPixel(x,y).A;if(alpha==0)continue;var a=body.GetPixel(x,y);var b=pose.GetPixel(x,y);if(a.A<245||b.A<245)continue;float t=alpha/255f;
   result.SetPixel(x,y,Color.FromArgb(a.A,(int)Math.Round(a.R+(b.R-a.R)*t),(int)Math.Round(a.G+(b.G-a.G)*t),(int)Math.Round(a.B+(b.B-a.B)*t)));
  }
  return result;
 }
}
}