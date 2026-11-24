using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace DragonCompanion {
// Closed contours follow the ORIGINAL complete sleeves and fingers. In
// particular, the chest rectangle must never punch a hole through a wrist.
internal static class PaintedContours {
 internal static bool Uses(string style,string key){return key=="working"||(style=="anime"&&key=="tea");}
 static void Shape(Graphics g,float[] coordinates,bool mirror,float axis){
  var points=new PointF[coordinates.Length/2];
  for(int i=0;i<points.Length;i++)points[i]=new PointF(mirror?axis-coordinates[i*2]:coordinates[i*2],coordinates[i*2+1]);
  using(var path=new GraphicsPath()){path.AddClosedCurve(points,.12f);g.FillPath(Brushes.White,path);}
 }
 internal static Bitmap Mask(string style,string key,Bitmap pose,int index){
  var mask=new Bitmap(192,192);bool pixel=style=="pixel";
  using(var g=Graphics.FromImage(mask)){
   g.SmoothingMode=SmoothingMode.AntiAlias;
   if(key=="tea"){
    bool low=index>=4;
    if(low){
     float[] sleeve={76,103,80,103,83,106,82,111,83,118,81,124,79,129,75,133,70,134,66,132,64,128,64,123,66,118,69,114,71,110,72,106};
     Shape(g,sleeve,false,188);Shape(g,sleeve,true,188);
     // Keep the bare shoulder-to-wrist connection in the lowered pose too.
     // It sits further inward on the right and cannot use the mirrored cuff.
     Shape(g,new float[]{76,103,80,102,86,104,87,107,84,111,78,113,74,111,73,107},false,0);
     Shape(g,new float[]{100,103,106,102,111,104,114,108,113,112,109,114,105,113,100,110,98,107},false,0);
    }else{
     // The lifted forearms are asymmetric too. The left wrist extends out
     // toward x=64; the right inner forearm runs down to x=107,y=126.
     // Mirrored narrow sleeves left both strips on the resting body frame.
     Shape(g,new float[]{71,101,77,100,81,102,82,106,81,112,79,118,78,124,76,129,72,132,67,133,63,130,61,125,61,119,62,114,64,110,67,106},false,0);
     Shape(g,new float[]{105,99,110,100,114,103,118,108,122,113,125,119,125,126,122,132,118,134,113,132,109,130,106,127,105,123,105,118,105,114,103,110,101,106,102,102},false,0);
     // Overlap the wrist/hand joins with opaque native coverage. Merely
     // touching two antialiased paths blends resting-pose skin into the join.
     Shape(g,new float[]{74,100,79,99,82,101,83,105,81,109,77,111,73,108,72,104},false,0);
     Shape(g,new float[]{102,108,107,108,109,112,108,118,109,123,111,127,108,129,105,126,103,121,102,116},false,0);
    }
    // The raised cup extends BELOW the star by fifteen pixels. The old
    // 19px-high window clipped its base and pasted a second rim into the bow.
    float[] cup=low?new float[]{81,106,87,105,98,106,104,108,104,117,102,124,99,126,86,126,81,123,79,115,79,109}:
     new float[]{81,94,89,93,99,94,105,96,104,107,103,114,100,118,86,119,81,116,79,108,79,98};
    Shape(g,cup,false,0);
    // The pose is asymmetric: the cup centre is x=92, while the sleeves
    // mirror around x=94. Connect the actual right grip explicitly; mirroring
    // the left sleeve alone left a 1-4px slit through the palm next to the cup.
    Shape(g,low?new float[]{102,108,106,108,110,111,113,115,113,120,110,123,106,123,102,120,100,115}:
     new float[]{102,99,107,100,111,103,114,107,115,112,112,116,108,114,104,111,101,106},false,0);
   }else if(pixel){
    // Typing needs only the fingers/palm to change. Keep the complete upper
    // arm and cuff from the fixed illustration, so atlas shoulder drift never
    // tugs the wrist or changes the sleeve shape.
    Shape(g,new float[]{66,101,71,100,76,101,79,104,79,108,77,111,74,113,68,113,64,111,62,107,63,104},false,0);
    Shape(g,new float[]{102,102,107,101,111,103,114,106,115,110,112,113,108,115,103,114,99,112,97,108,99,104},false,0);
   }else{
    float[] hand={73,118,77,118,81,119,84,121,85,124,82,127,78,129,73,129,70,127,68,124,69,121};
    Shape(g,hand,false,188);Shape(g,hand,true,188);
   }
   if(key=="working"){
    int keyRows=pixel?115:130;
    g.CompositingMode=CompositingMode.SourceCopy;
    g.FillRectangle(Brushes.Transparent,0,keyRows,192,192-keyRows);
   }
  }
  return mask;
 }
}
}
