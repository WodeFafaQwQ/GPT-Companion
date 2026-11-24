using System;
using System.Drawing;
namespace DragonCompanion {
// A curved tail keeps its painted thickness and segment lengths. The root
// drives the swing; a delayed bend travels along the curve toward the tip.
internal static class TailSway {
 internal static float Angle(long time){if(time<0||time>=2800)return 0;double e=Math.Sin(Math.PI*time/2800.0);return (float)(16*Math.Sin(time*Math.PI*2/1900)*e*e);}
 static PointF[] Rest(string style){
  PointF[] controls=style=="pixel"?new[]{new PointF(131,158),new PointF(145,158),new PointF(160,148),new PointF(164,132),new PointF(158,117)}:new[]{new PointF(127,147),new PointF(138,154),new PointF(142,166),new PointF(133,174),new PointF(124,171),new PointF(126,161)};
  var result=new PointF[41];
  for(int n=0;n<result.Length;n++){
   float u=n*(controls.Length-1f)/(result.Length-1);int i=Math.Min(controls.Length-2,(int)u);float t=u-i,t2=t*t,t3=t2*t;
   var a=controls[Math.Max(0,i-1)];var b=controls[i];var c=controls[i+1];var d=controls[Math.Min(controls.Length-1,i+2)];
   result[n]=new PointF(.5f*(2*b.X+(-a.X+c.X)*t+(2*a.X-5*b.X+4*c.X-d.X)*t2+(-a.X+3*b.X-3*c.X+d.X)*t3),.5f*(2*b.Y+(-a.Y+c.Y)*t+(2*a.Y-5*b.Y+4*c.Y-d.Y)*t2+(-a.Y+3*b.Y-3*c.Y+d.Y)*t3));
  }
  return result;
 }
 internal static PointF[] Curve(string style,long time){
  var rest=Rest(style);var result=new PointF[rest.Length];result[0]=rest[0];float length=0;
  for(int i=1;i<rest.Length;i++)length+=Distance(rest[i-1],rest[i]);
  double envelope=time<0||time>=2800?0:Math.Pow(Math.Sin(Math.PI*time/2800.0),2);float travelled=0;
  for(int i=1;i<rest.Length;i++){
   float dx=rest[i].X-rest[i-1].X,dy=rest[i].Y-rest[i-1].Y,segment=Distance(rest[i-1],rest[i]);float progress=(travelled+segment*.5f)/length;travelled+=segment;
   float blend=Math.Max(0,Math.Min(1,(progress-.2f)/.3f));blend=blend*blend*(3-2*blend);
   double angle=blend*(Angle(time)+10*progress*Math.Sin(time*Math.PI*2/1900-progress*.8)*envelope)*Math.PI/180;
   result[i]=new PointF(result[i-1].X+(float)(dx*Math.Cos(angle)-dy*Math.Sin(angle)),result[i-1].Y+(float)(dx*Math.Sin(angle)+dy*Math.Cos(angle)));
  }
  return result;
 }
 static float Distance(PointF a,PointF b){float dx=a.X-b.X,dy=a.Y-b.Y;return (float)Math.Sqrt(dx*dx+dy*dy);}
 internal static PointF Tip(string style,long time){var curve=Curve(style,time);return curve[curve.Length-1];}
 internal static Bitmap Render(string style,Bitmap restImage,Bitmap body,Bitmap texture,bool[] mask,Rectangle area,long time){
  if(time<=0||time>=2800)return (Bitmap)restImage.Clone();var rest=Rest(style);var target=Curve(style,time);var output=(Bitmap)body.Clone();
  for(int y=area.Top;y<area.Bottom;y++)for(int x=area.Left;x<area.Right;x++){
   // Fixed foreground hair, dress and legs stay in front of the moving tail.
   if(!mask[y*192+x]&&body.GetPixel(x,y).A>0)continue;
   float nearest=float.MaxValue,fraction=0;int segment=0;
   for(int j=0;j<target.Length-1;j++){
    float dx=target[j+1].X-target[j].X,dy=target[j+1].Y-target[j].Y,l=dx*dx+dy*dy;
    float t=Math.Max(0,Math.Min(1,((x-target[j].X)*dx+(y-target[j].Y)*dy)/Math.Max(.001f,l)));
    float px=target[j].X+t*dx,py=target[j].Y+t*dy,d=(x-px)*(x-px)+(y-py)*(y-py);
    if(d<nearest){nearest=d;fraction=t;segment=j;}
   }
   if(nearest>324)continue;
   var a=target[segment];var b=target[segment+1];var c=rest[segment];var d0=rest[segment+1];
   float tl=Distance(a,b),rl=Distance(c,d0),tx=(b.X-a.X)/tl,ty=(b.Y-a.Y)/tl,rx=(d0.X-c.X)/rl,ry=(d0.Y-c.Y)/rl;
   float px0=a.X+(b.X-a.X)*fraction,py0=a.Y+(b.Y-a.Y)*fraction,offX=x-px0,offY=y-py0;
   float normal=-offX*ty+offY*tx,tangent=offX*tx+offY*ty;
   float sx=c.X+(d0.X-c.X)*fraction+rx*tangent-ry*normal,sy=c.Y+(d0.Y-c.Y)*fraction+ry*tangent+rx*normal;
   var colour=Sample(texture,sx,sy);if(colour.A>0)output.SetPixel(x,y,colour);
  }
  return output;
 }
 static Color Sample(Bitmap image,float x,float y){
  int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);float fx=x-ix,fy=y-iy,a=0,r=0,g=0,b=0;
  for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++){
   int px=ix+dx,py=iy+dy;if(px<0||py<0||px>=192||py>=192)continue;var c=image.GetPixel(px,py);float w=c.A*(dx==0?1-fx:fx)*(dy==0?1-fy:fy);a+=w;r+=c.R*w;g+=c.G*w;b+=c.B*w;
  }
  return a<.01f?Color.Transparent:Color.FromArgb(Math.Min(255,(int)Math.Round(a)),Math.Min(255,(int)Math.Round(r/a)),Math.Min(255,(int)Math.Round(g/a)),Math.Min(255,(int)Math.Round(b/a)));
 }
}
}
