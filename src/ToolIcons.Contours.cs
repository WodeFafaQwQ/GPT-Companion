using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace DragonCompanion {
public static partial class LineIcons {
 // Trace the original white symbol, not every colour transition in its tile.
 // This keeps CS2's player silhouette and discards the blue/orange panel seam
 // and the old duplicate rectangular border.
 static Bitmap SmoothOutlineGame(Bitmap source,int size){bool circle=IsCircular(source);int width=source.Width+2,height=source.Height+2;var symbol=new float[width*height];var alpha=new float[width*height];int whites=0,opaque=0;
  for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++){var color=source.GetPixel(x,y);int i=(y+1)*width+x+1;float coverage=color.A/255f;alpha[i]=coverage;if(color.A>128)opaque++;float minimum=Math.Min(color.R,Math.Min(color.G,color.B)),maximum=Math.Max(color.R,Math.Max(color.G,color.B));symbol[i]=coverage*Math.Max(0,Math.Min(1,(minimum-150)/65f))*Math.Max(0,1-(maximum-minimum)/100f);if(symbol[i]>.5f)whites++;}
  // Coloured symbols without a white foreground use contrast to their border
  // colour. The white-symbol branch is used by the installed osu! and CS2.
  if(whites<3||whites>opaque*.80f){var bg=source.GetPixel(source.Width/2,0);for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++){var c=source.GetPixel(x,y);symbol[(y+1)*width+x+1]=c.A/255f*Math.Min(1,(Math.Abs(c.R-bg.R)+Math.Abs(c.G-bg.G)+Math.Abs(c.B-bg.B))/160f);}}
  const int supersample=4;var output=new Bitmap(size,size,PixelFormat.Format32bppArgb);using(var high=new Bitmap(size*supersample,size*supersample,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(high)){g.ScaleTransform(supersample,supersample);g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
   using(var outer=circle?new GraphicsPath():Theme.Round(new RectangleF(0,0,size,size),size*.19f)){if(circle)outer.AddEllipse(0,0,size,size);g.SetClip(outer);g.FillPath(Brushes.White,outer);using(var pen=new Pen(Theme.Purple,Math.Max(1.1f,size/48f))){pen.StartCap=pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;DrawContours(g,symbol,width,height,source.Width,source.Height,size,pen,circle);if(circle)DrawContours(g,alpha,width,height,source.Width,source.Height,size,pen);else{float inset=pen.Width/2;using(var frame=Theme.Round(new RectangleF(inset,inset,size-pen.Width,size-pen.Width),Math.Max(1,size*.19f-inset)))g.DrawPath(pen,frame);}}}
   using(var target=Graphics.FromImage(output)){target.CompositingMode=CompositingMode.SourceCopy;target.InterpolationMode=InterpolationMode.HighQualityBicubic;target.DrawImage(high,new Rectangle(0,0,size,size));}}
  return output;
 }
 static void DrawContours(Graphics g,float[] values,int width,int height,int sourceWidth,int sourceHeight,int size,Pen pen,bool ignoreFrame=false){var points=new Dictionary<int,PointF>();var links=new Dictionary<int,List<int>>();int vertical=width*height;
  Action<int,PointF> point=(id,p)=>{if(!points.ContainsKey(id))points[id]=p;};Action<int,int> connect=(a,b)=>{if(!links.ContainsKey(a))links[a]=new List<int>();if(!links.ContainsKey(b))links[b]=new List<int>();links[a].Add(b);links[b].Add(a);};
  for(int y=0;y<height-1;y++)for(int x=0;x<width-1;x++){float a=values[y*width+x],b=values[y*width+x+1],c=values[(y+1)*width+x+1],d=values[(y+1)*width+x];bool aa=a>=.5f,bb=b>=.5f,cc=c>=.5f,dd=d>=.5f;if(aa==bb&&bb==cc&&cc==dd)continue;var edge=new List<int>();
   if(aa!=bb){int id=y*width+x;point(id,new PointF(x+(.5f-a)/(b-a),y));edge.Add(id);}if(bb!=cc){int id=vertical+y*width+x+1;point(id,new PointF(x+1,y+(.5f-b)/(c-b)));edge.Add(id);}if(cc!=dd){int id=(y+1)*width+x;point(id,new PointF(x+(.5f-d)/(c-d),y+1));edge.Add(id);}if(dd!=aa){int id=vertical+y*width+x;point(id,new PointF(x,y+(.5f-a)/(d-a)));edge.Add(id);}
   if(edge.Count==2)connect(edge[0],edge[1]);else if(edge.Count==4){bool center=(a+b+c+d)*.25f>=.5f;if(aa==center){connect(edge[0],edge[1]);connect(edge[2],edge[3]);}else{connect(edge[0],edge[3]);connect(edge[1],edge[2]);}}}
  var visited=new HashSet<int>();foreach(int start in links.Keys){if(visited.Contains(start))continue;var contour=new List<PointF>();int current=start,previous=-1;for(int guard=0;guard<points.Count+1;guard++){if(visited.Contains(current))break;visited.Add(current);contour.Add(points[current]);var next=links[current].FirstOrDefault(n=>n!=previous);previous=current;current=next;if(current==start)break;}if(contour.Count<4)continue;
   // Resampling and a gentle periodic spline turn pixel steps into smooth
   // vector strokes while retaining the full original-size silhouette.
   if(ignoreFrame&&contour.Max(p=>p.X)-contour.Min(p=>p.X)>sourceWidth*.82f&&contour.Max(p=>p.Y)-contour.Min(p=>p.Y)>sourceHeight*.82f)continue;var simplified=SimplifyClosed(contour,.55f);if(simplified.Length<3)continue;for(int i=0;i<simplified.Length;i++)simplified[i]=new PointF((simplified[i].X-.5f)*size/sourceWidth,(simplified[i].Y-.5f)*size/sourceHeight);g.DrawClosedCurve(pen,simplified,.12f,FillMode.Alternate);
  }
 }
 static PointF[] SimplifyClosed(List<PointF> points,float tolerance){int farthest=1;double longest=0;for(int i=1;i<points.Count;i++){double dx=points[i].X-points[0].X,dy=points[i].Y-points[0].Y,d=dx*dx+dy*dy;if(d>longest){longest=d;farthest=i;}}var path=new List<PointF>();path.AddRange(points);path.Add(points[0]);var keep=new bool[path.Count];keep[0]=keep[farthest]=keep[path.Count-1]=true;Simplify(path,keep,0,farthest,tolerance*tolerance);Simplify(path,keep,farthest,path.Count-1,tolerance*tolerance);return path.Where((p,i)=>i<path.Count-1&&keep[i]).ToArray();}
 static void Simplify(List<PointF> points,bool[] keep,int first,int last,float squaredTolerance){if(last-first<2)return;var a=points[first];var b=points[last];float dx=b.X-a.X,dy=b.Y-a.Y,length=dx*dx+dy*dy,best=squaredTolerance;int chosen=-1;for(int i=first+1;i<last;i++){var p=points[i];float t=length==0?0:Math.Max(0,Math.Min(1,((p.X-a.X)*dx+(p.Y-a.Y)*dy)/length)),px=p.X-a.X-t*dx,py=p.Y-a.Y-t*dy,distance=px*px+py*py;if(distance>best){best=distance;chosen=i;}}if(chosen<0)return;keep[chosen]=true;Simplify(points,keep,first,chosen,squaredTolerance);Simplify(points,keep,chosen,last,squaredTolerance);}
}
}
