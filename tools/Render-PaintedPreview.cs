using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using DragonCompanion;
static class PaintedPreview {
 [STAThread] static int Main(string[] args){
  string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
  string folder=Path.Combine(root,"frames");Directory.CreateDirectory(folder);
  for(int tick=0;tick<132;tick++)using(var image=new Bitmap(640,640)){
   using(var g=Graphics.FromImage(image)){
    g.Clear(Color.FromArgb(250,247,255));
    for(int row=0;row<2;row++)for(int col=0;col<2;col++){
     string style=row==0?"pixel":"anime",key=col==0?"tea":"working";
     Theme.Text(g,(row==0?"像素风 · ":"二次元风 · ")+(col==0?"喝茶":"敲键盘"),12,Theme.Ink,new RectangleF(col*320+16,row*300+8,288,28),true);
     Art.Draw(g,new Rectangle(col*320+16,row*300+36,288,264),style,key,tick);
    }
    Theme.Text(g,Theme.Credit,9,Theme.Muted,new RectangleF(16,610,608,24));
   }
   image.Save(Path.Combine(folder,tick.ToString("000")+".png"),ImageFormat.Png);
  }
  // Preserve a contact sheet of each actual held pose for visual review.
  using(var sheet=new Bitmap(15*192,4*216+24)){
   using(var g=Graphics.FromImage(sheet)){
    g.Clear(Color.FromArgb(250,247,255));int row=0;
    foreach(string style in new[]{"pixel","anime"})foreach(string key in new[]{"tea","working"}){
     var clip=ActivityArt.Find(key);int elapsed=0;
     for(int i=0;i<clip.Holds.Length;i++){
      Theme.Text(g,style+" "+key+" "+(i+1),9,Theme.Ink,new RectangleF(i*192+4,row*216+2,184,20));
      Art.Draw(g,new Rectangle(i*192,row*216+24,192,192),style,key,elapsed/50);elapsed+=clip.Holds[i];
     }
     row++;
    }
    Theme.Text(g,Theme.Credit,9,Theme.Muted,new RectangleF(12,4*216,1500,24));
   }
   sheet.Save(Path.Combine(root,"painted-poses.png"),ImageFormat.Png);
  }
  try{typeof(SelfTests).GetMethod("TestActivityMotion",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);}
  catch(TargetInvocationException e){Console.WriteLine(e.InnerException);return 1;}
  var results=(System.Collections.Generic.List<string>)typeof(SelfTests).GetField("results",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
  File.WriteAllLines(Path.Combine(root,"checks.txt"),results);
  Console.WriteLine("Rendered 132 real 50ms frames; animation checks passed; no package created.");return 0;
 }
}
