using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
namespace DragonCompanion {
// Generated characters can cross their nominal equal-width cells. Segment the
// whole transparent row before extracting, so wings/tails/hands stay complete.
public static class ActivitySprites {
 sealed class Component {public Rectangle Bounds;public int Count;}
 public static Bitmap[] SplitRow(Bitmap atlas,int row){var cuts=ActivityArt.RowCuts(atlas);int width=atlas.Width,top=Math.Max(0,cuts[row]-12),end=Math.Min(atlas.Height,cuts[row+1]+12),height=end-top;using(var strip=atlas.Clone(new Rectangle(0,top,width,height),PixelFormat.Format32bppArgb)){
  var bits=strip.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);var pixels=new byte[width*height*4];try{for(int y=0;y<height;y++)Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),pixels,y*width*4,width*4);}finally{strip.UnlockBits(bits);}
  var visited=new bool[width*height];var queue=new int[visited.Length];var components=new List<Component>();for(int seed=0;seed<visited.Length;seed++){if(visited[seed]||pixels[seed*4+3]<32)continue;int head=0,tail=1,left=width,componentTop=height,right=0,bottom=0;visited[seed]=true;queue[0]=seed;while(head<tail){int p=queue[head++],x=p%width,y=p/width;left=Math.Min(left,x);right=Math.Max(right,x);componentTop=Math.Min(componentTop,y);bottom=Math.Max(bottom,y);for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=width||ny>=height)continue;int q=ny*width+nx;if(visited[q]||pixels[q*4+3]<32)continue;visited[q]=true;queue[tail++]=q;}}if(tail>=500&&bottom-componentTop>=height*.4)components.Add(new Component{Count=tail,Bounds=Rectangle.FromLTRB(left,componentTop,right+1,bottom+1)});}
  var characters=components.OrderByDescending(c=>c.Count).Take(8).OrderBy(c=>c.Bounds.Left).ToArray();if(characters.Length!=8)throw new InvalidOperationException("动作行无法识别八个完整人物："+row+" / "+characters.Length);
  int canvasWidth=characters.Max(c=>c.Bounds.Width)+16,canvasHeight=characters.Max(c=>c.Bounds.Height)+16;var result=new Bitmap[8];for(int i=0;i<8;i++){var box=characters[i].Bounds;box.Inflate(2,2);box.Intersect(new Rectangle(0,0,width,height));using(var part=strip.Clone(box,PixelFormat.Format32bppArgb))using(var isolated=Art.IsolateCharacter(part)){var image=new Bitmap(canvasWidth+8,canvasHeight+8,PixelFormat.Format32bppArgb);using(var graphics=Graphics.FromImage(image))graphics.DrawImageUnscaled(isolated,(image.Width-isolated.Width)/2,image.Height-isolated.Height-6);result[i]=image;}}return result;
 }}
}
}
