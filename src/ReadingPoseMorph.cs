using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DragonCompanion {
// Corresponding face, book, hands and feet rows prevent a double face when the
// original thinking drawing has different proportions from the reading plate.
static class ReadingPoseMorph {
 static PointF[] Landmarks(string style,bool thought,Size size){
  float[] ys;float[][] rows;
  if(style=="pixel"){
   ys=thought?new float[]{0,18,43,71,85,93,113,142,160,184,192}:new float[]{0,18,45,76,90,100,120,142,160,184,192};
   rows=thought?new float[][]{new float[]{42,65,83,102,130},new float[]{45,65,83,102,129},new float[]{46,66,83,101,122},new float[]{48,69,83,96,120},new float[]{49,68,83,98,119},new float[]{32,48,61,76,113},new float[]{32,47,61,78,126},new float[]{33,65,84,105,137},new float[]{32,70,82,94,151},new float[]{32,70,82,94,151},new float[]{42,65,83,102,130}}:new float[][]{new float[]{36,65,85,105,136},new float[]{36,65,85,105,136},new float[]{38,66,85,105,136},new float[]{40,71,86,101,133},new float[]{43,69,85,105,132},new float[]{53,68,82,99,112},new float[]{48,67,82,100,118},new float[]{31,64,85,106,143},new float[]{31,74,86,100,164},new float[]{31,74,86,100,164},new float[]{36,65,85,105,136}};
  }else{
   ys=thought?new float[]{0,19,43,69,81,89,108,140,160,184,192}:new float[]{0,19,49,87,99,105,124,145,162,184,192};
   rows=thought?new float[][]{new float[]{58,79,92,105,131},new float[]{60,79,92,105,129},new float[]{61,79,92,105,127},new float[]{64,80,92,103,125},new float[]{66,80,92,103,124},new float[]{54,64,71,80,119},new float[]{53,64,73,85,125},new float[]{52,76,92,108,138},new float[]{53,86,95,105,140},new float[]{53,86,95,105,140},new float[]{58,79,92,105,131}}:new float[][]{new float[]{37,76,94,113,143},new float[]{37,76,94,113,143},new float[]{40,76,94,113,142},new float[]{43,80,96,111,140},new float[]{49,78,94,113,137},new float[]{65,78,91,105,119},new float[]{61,77,91,106,125},new float[]{40,75,94,113,143},new float[]{43,84,94,105,147},new float[]{43,84,94,105,147},new float[]{37,76,94,113,143}};
  }
  float scale=Math.Min(176f/size.Width,176f/size.Height),left=(192-size.Width*scale)/2,top=184-size.Height*scale;var points=new PointF[ys.Length*7];for(int y=0;y<ys.Length;y++)for(int x=0;x<7;x++)points[y*7+x]=new PointF(((x==0?0:x==6?192:rows[y][x-1])-left)/scale,(ys[y]-top)/scale);return points;
 }
 static byte[] Read(Bitmap image){var area=new Rectangle(Point.Empty,image.Size);var bits=image.LockBits(area,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{var data=new byte[image.Width*image.Height*4];for(int y=0;y<image.Height;y++)Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),data,y*image.Width*4,image.Width*4);return data;}finally{image.UnlockBits(bits);}}
 static float Cross(PointF a,PointF b,PointF c){return (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);}
 static void Sample(byte[] data,int width,int height,float x,float y,float[] output){Array.Clear(output,0,4);int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);float fx=x-ix,fy=y-iy;for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++){int px=ix+dx,py=iy+dy;if(px<0||py<0||px>=width||py>=height)continue;float w=(dx==0?1-fx:fx)*(dy==0?1-fy:fy);int p=(py*width+px)*4;float a=data[p+3];output[3]+=a*w;for(int c=0;c<3;c++)output[c]+=data[p+c]*a/255f*w;}}
 public static Bitmap[] Frames(Bitmap from,Bitmap to,string style,bool entering){int width=from.Width,height=from.Height;var a=Read(from);var b=Read(to);var first=Landmarks(style,!entering,from.Size);var last=Landmarks(style,entering,from.Size);var result=new Bitmap[15];result[0]=(Bitmap)from.Clone();result[14]=(Bitmap)to.Clone();var ca=new float[4];var cb=new float[4];
  for(int frame=1;frame<14;frame++){float t=frame/14f;t=t*t*(3-2*t);var points=new PointF[first.Length];for(int i=0;i<points.Length;i++)points[i]=new PointF(first[i].X*(1-t)+last[i].X*t,first[i].Y*(1-t)+last[i].Y*t);var output=new byte[a.Length];
   for(int row=0;row<10;row++)for(int col=0;col<6;col++)for(int half=0;half<2;half++){int i=row*7+col,j=half==0?i+1:i+8,k=half==0?i+8:i+7;var p=points[i];var q=points[j];var r=points[k];float area=Cross(p,q,r);int left=Math.Max(0,(int)Math.Floor(Math.Min(p.X,Math.Min(q.X,r.X)))),right=Math.Min(width-1,(int)Math.Ceiling(Math.Max(p.X,Math.Max(q.X,r.X)))),top=Math.Max(0,(int)Math.Floor(Math.Min(p.Y,Math.Min(q.Y,r.Y)))),bottom=Math.Min(height-1,(int)Math.Ceiling(Math.Max(p.Y,Math.Max(q.Y,r.Y))));
    for(int y=top;y<=bottom;y++)for(int x=left;x<=right;x++){var point=new PointF(x,y);float u=Cross(point,q,r)/area,v=Cross(p,point,r)/area,w=1-u-v;if(u<-.0001f||v<-.0001f||w<-.0001f)continue;Sample(a,width,height,first[i].X*u+first[j].X*v+first[k].X*w,first[i].Y*u+first[j].Y*v+first[k].Y*w,ca);Sample(b,width,height,last[i].X*u+last[j].X*v+last[k].X*w,last[i].Y*u+last[j].Y*v+last[k].Y*w,cb);int index=(y*width+x)*4;float alpha=ca[3]*(1-t)+cb[3]*t;for(int c=0;c<3;c++)output[index+c]=alpha<=0?(byte)0:(byte)Math.Max(0,Math.Min(255,Math.Round((ca[c]*(1-t)+cb[c]*t)*255/alpha)));output[index+3]=(byte)Math.Round(alpha);}
   }
   var image=new Bitmap(width,height,PixelFormat.Format32bppArgb);var bits=image.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{for(int y=0;y<height;y++)Marshal.Copy(output,y*width*4,IntPtr.Add(bits.Scan0,y*bits.Stride),width*4);}finally{image.UnlockBits(bits);}result[frame]=image;
  }return result;
 }
}
}
