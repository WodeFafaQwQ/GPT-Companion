using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using DragonCompanion;
static class ActivityPreview {
 [STAThread]static void Main(string[] args){string root=Path.GetFullPath(args[0]);foreach(string style in new[]{"pixel","anime"}){string folder=Path.Combine(root,"activity-frames",style);Directory.CreateDirectory(folder);for(int tick=0;tick<200;tick++)using(var image=new Bitmap(736,440)){using(var graphics=Graphics.FromImage(image)){graphics.Clear(Color.FromArgb(250,247,255));int daily=0,tasks=0;foreach(var clip in ActivityArt.Clips){int x,y;if(clip.Group=="daily"){x=104+daily++*184;y=8;}else{x=12+tasks++*184;y=214;}Theme.Text(graphics,clip.Name,10,Theme.Ink,new RectangleF(x,y,176,26),true);Art.Draw(graphics,new Rectangle(x,y+24,176,176),style,clip.Key,tick);}Theme.Text(graphics,Theme.Credit,8,Theme.Muted,new RectangleF(12,421,710,18));}image.Save(Path.Combine(folder,tick.ToString("000")+".png"),ImageFormat.Png);}}
  string detail=Path.Combine(root,"preferences-frames");Directory.CreateDirectory(detail);for(int tick=0;tick<200;tick++)using(var image=new Bitmap(896,540)){using(var graphics=Graphics.FromImage(image)){graphics.Clear(Color.FromArgb(250,247,255));for(int row=0;row<2;row++){string style=row==0?"pixel":"anime";for(int col=0;col<4;col++){string key=new[]{"working","tea","complete","reminder"}[col];Theme.Text(graphics,(row==0?"像素风 · ":"二次元风 · ")+ActivityArt.Find(key).Name,10,Theme.Ink,new RectangleF(col*224+10,row*254+10,214,25),true);Art.Draw(graphics,new Rectangle(col*224,row*254+32,224,220),style,key,tick);}}Theme.Text(graphics,Theme.Credit,8,Theme.Muted,new RectangleF(10,516,876,20));}image.Save(Path.Combine(detail,tick.ToString("000")+".png"),ImageFormat.Png);}
  Console.WriteLine("Rendered 200 real 50ms frames per overview and close-up; no release packaged.");
 }
}
