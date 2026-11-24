using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
namespace DragonCompanion {
// Assign complete connected pieces to atlas slots by centroid. Bounding boxes
// may cross a nominal grid boundary; never cut the body/tail at that boundary.
public sealed class ActivityModelParts {
 public readonly Bitmap[] Images;
 public readonly RectangleF BodyDestination;public readonly float CenterX;
 public ActivityModelParts(string style){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(style+"-activity-model.png")){if(stream==null)throw new InvalidOperationException("缺少独立动作部件："+style);using(var atlas=new Bitmap(stream))Images=Extract(atlas);}var body=Images[0];float scale=Math.Min(168f/body.Height,176f/body.Width);BodyDestination=new RectangleF((192-body.Width*scale)/2,184-body.Height*scale,body.Width*scale,body.Height*scale);CenterX=BodyDestination.X+ReadingBook.HeadCenter(body)*scale;}
 public static Bitmap[] Extract(Bitmap atlas){int width=atlas.Width,height=atlas.Height;var bits=atlas.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);var data=new byte[width*height*4];try{for(int y=0;y<height;y++)Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),data,y*width*4,width*4);}finally{atlas.UnlockBits(bits);}
  var seen=new bool[width*height];var queue=new int[seen.Length];var sizes=new int[8];var boxes=new Rectangle[8];
  for(int seed=0;seed<seen.Length;seed++){if(seen[seed]||data[seed*4+3]<32)continue;int head=0,tail=1,left=width,top=height,right=0,bottom=0;long sumX=0,sumY=0;seen[seed]=true;queue[0]=seed;while(head<tail){int p=queue[head++],x=p%width,y=p/width;sumX+=x;sumY+=y;left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=width||ny>=height)continue;int q=ny*width+nx;if(seen[q]||data[q*4+3]<32)continue;seen[q]=true;queue[tail++]=q;}}
   if(tail<100)continue;int column=Math.Min(3,(int)(sumX/(double)tail*4/width)),row=Math.Min(1,(int)(sumY/(double)tail*2/height)),slot=row*4+column;if(tail<=sizes[slot])continue;sizes[slot]=tail;boxes[slot]=Rectangle.FromLTRB(left,top,right+1,bottom+1);
  }
  var result=new Bitmap[8];for(int i=0;i<8;i++){if(sizes[i]<500)throw new InvalidOperationException("独立动作部件缺失："+i);var crop=boxes[i];crop.Inflate(4,4);crop.Intersect(new Rectangle(0,0,width,height));result[i]=atlas.Clone(crop,PixelFormat.Format32bppArgb);CleanFringe(result[i]);}return result;
 }
 // Generated transparent edges sometimes contain saturated key-colour crumbs.
 // Remove only such pixels beside transparency; opaque material stays intact.
 static void CleanFringe(Bitmap image){var box=new Rectangle(Point.Empty,image.Size);var bits=image.LockBits(box,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);var data=new byte[image.Width*image.Height*4];try{for(int y=0;y<image.Height;y++)Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),data,y*image.Width*4,image.Width*4);var alpha=(byte[])data.Clone();for(int y=1;y<image.Height-1;y++)for(int x=1;x<image.Width-1;x++){int p=(y*image.Width+x)*4;int b=data[p],g=data[p+1],r=data[p+2];bool neon=g<85&&(b>185&&r>185||b>200&&r<90||r>200&&b<80);if(!neon)continue;bool edge=false;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)edge|=alpha[((y+dy)*image.Width+x+dx)*4+3]<32;if(edge)data[p+3]=0;}for(int y=0;y<image.Height;y++)Marshal.Copy(data,y*image.Width*4,IntPtr.Add(bits.Scan0,y*bits.Stride),image.Width*4);}finally{image.UnlockBits(bits);}}
}
}
