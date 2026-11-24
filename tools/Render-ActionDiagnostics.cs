using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using DragonCompanion;
static class ActionDiagnostics {
 [STAThread]static void Main(string[] args){Directory.CreateDirectory(args[0]);foreach(string style in new[]{"pixel","anime"})foreach(string key in new[]{"tea","working","waiting","rest","reminder","complete"}){var clip=ActivityArt.Find(key);for(int i=0;i<4;i++){int time=i*clip.Duration/4;using(var image=new Bitmap(192,192)){using(var graphics=Graphics.FromImage(image))Art.Draw(graphics,new Rectangle(0,0,192,192),style,key,time/50);image.Save(Path.Combine(args[0],style+"-"+key+"-"+i+".png"),ImageFormat.Png);}}}
  foreach(string style in new[]{"pixel","anime"})using(var atlas=new Bitmap(Path.Combine(args[1],style+"-daily-8.png"))){var poses=ActivitySprites.SplitRow(atlas,2);for(int i=0;i<8;i++){poses[i].Save(Path.Combine(args[0],style+"-tea-source-"+i+".png"),ImageFormat.Png);poses[i].Dispose();}}
 }
}
