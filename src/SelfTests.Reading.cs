using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace DragonCompanion {
public static partial class SelfTests {
 static double PoseDistance(Bitmap a,Bitmap b){double distance=0;for(int y=0;y<a.Height;y+=2)for(int x=0;x<a.Width;x+=2){var p=a.GetPixel(x,y);var q=b.GetPixel(x,y);distance+=Math.Abs(p.A-q.A)+Math.Abs(p.R*p.A-q.R*q.A)/255.0+Math.Abs(p.G*p.A-q.G*q.A)/255.0+Math.Abs(p.B*p.A-q.B*q.A)/255.0;}return distance;}
 static bool PageConnected(Bitmap image,Point root){var visited=new HashSet<int>();var queue=new Queue<Point>();queue.Enqueue(root);visited.Add(root.Y*image.Width+root.X);while(queue.Count>0){var p=queue.Dequeue();for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int x=p.X+dx,y=p.Y+dy,index=y*image.Width+x;if(x<0||y<0||x>=image.Width||y>=image.Height||image.GetPixel(x,y).A<64||!visited.Add(index))continue;queue.Enqueue(new Point(x,y));}}for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).A>=64&&!visited.Contains(y*image.Width+x))return false;return image.GetPixel(root.X,root.Y).A>=64;}
 static void TestReadingRefinements(string folder){
  foreach(string style in new[]{"pixel","anime"}){
   var root=style=="pixel"?new Point(82,108):new Point(91,111);bool connected=true;for(int i=0;i<8;i++)using(var leaf=new Bitmap(192,192)){using(var graphics=Graphics.FromImage(leaf)){graphics.TranslateTransform(8,8);ReadingBook.DrawPage(graphics,new Size(176,176),style,i==0?.01f:i/7f);}connected&=PageConnected(leaf,root);if(i==0){Check(ActivityArt.OpaqueBounds(leaf).Top<=(style=="pixel"?97:101),style+" first lifted leaf starts above the real uppermost page layer");leaf.Save(Path.Combine(folder,"reading-leaf-root-"+style+".png"),ImageFormat.Png);}}
   Check(connected,style+" every lifted leaf remains one connected surface attached to the actual book gutter");
   foreach(bool enter in new[]{true,false}){long start=enter?ReadingBook.TurningMilliseconds:ReadingBook.ThoughtStartsAt+ReadingBook.ThoughtMilliseconds;var from=enter?ActivityArt.Frame(style,"read",7):ReadingBook.Thought(style);var to=enter?ReadingBook.Thought(style):ActivityArt.Frame(style,"read",0);Check(SpriteHash(ReadingBook.ImageAt(style,start))==SpriteHash(from)&&SpriteHash(ReadingBook.ImageAt(style,start+ReadingBook.TransitionMilliseconds))==SpriteHash(to),style+(enter?" settles into":" resumes from")+" thinking without an endpoint pop");var hashes=new HashSet<string>();double largest=0,total=PoseDistance(from,to);Bitmap previous=from;bool bounded=true;for(int i=0;i<=14;i++){var sample=ReadingBook.ImageAt(style,start+i*50);hashes.Add(SpriteHash(sample));largest=Math.Max(largest,PoseDistance(previous,sample));previous=sample;var bounds=ActivityArt.OpaqueBounds(sample);bounded&=bounds.Left>=0&&bounds.Top>=0&&bounds.Right<=sample.Width&&bounds.Bottom<=sample.Height;}Check(hashes.Count==15&&largest<total*.4,style+(enter?" incoming":" outgoing")+" pose transition has fifteen gradual geometry-aligned frames rather than a two-pose jump");Check(bounded,style+" transition keeps the full character inside its fixed canvas");}
  }
 }
}
}
