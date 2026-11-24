using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using DragonCompanion;
static class BellPreview {
 [STAThread] static int Main(string[] args) {
  string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
  string frames=Path.Combine(root,"frames");Directory.CreateDirectory(frames);
  for(int tick=0;tick<64;tick++)using(var image=new Bitmap(640,376)) {
   using(var graphics=Graphics.FromImage(image)) {
    graphics.Clear(Color.FromArgb(250,247,255));
    for(int col=0;col<2;col++) {
     Theme.Text(graphics,col==0?"像素风 · 铃铛":"二次元风 · 铃铛",12,Theme.Ink,new RectangleF(col*320+16,12,288,28),true);
     Art.Draw(graphics,new Rectangle(col*320+16,44,288,288),col==0?"pixel":"anime","reminder",tick);
    }
    Theme.Text(graphics,Theme.Credit,9,Theme.Muted,new RectangleF(16,346,608,24));
   }
   image.Save(Path.Combine(frames,tick.ToString("000")+".png"),ImageFormat.Png);
  }
  string detail=Path.Combine(root,"grip-frames");Directory.CreateDirectory(detail);
  for(int tick=0;tick<64;tick++)using(var image=new Bitmap(896,500)){
   using(var graphics=Graphics.FromImage(image)){
    graphics.Clear(Color.FromArgb(250,247,255));
    Theme.Text(graphics,"二次元 · 日程提醒",12,Theme.Ink,new RectangleF(16,12,390,28),true);
    Theme.Text(graphics,"握持与左右摆动 · 放大",12,Theme.Ink,new RectangleF(432,12,448,28),true);
    Art.Draw(graphics,new Rectangle(0,44,400,400),"anime","reminder",tick);
    var saved=graphics.Save();graphics.SetClip(new Rectangle(432,64,440,384));
    Art.Draw(graphics,new Rectangle(432-106*8,64-102*8,192*8,192*8),"anime","reminder",tick);
    graphics.Restore(saved);
    Theme.Text(graphics,Theme.Credit,9,Theme.Muted,new RectangleF(16,466,864,24));
   }
   image.Save(Path.Combine(detail,tick.ToString("000")+".png"),ImageFormat.Png);
  }
  try {typeof(SelfTests).GetMethod("TestActivityMotion",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);}
  catch(TargetInvocationException error){Console.WriteLine(error.InnerException);return 1;}
  var results=typeof(SelfTests).GetField("results",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
  File.WriteAllLines(Path.Combine(root,"checks.txt"),(System.Collections.Generic.List<string>)results);
  Console.WriteLine("Activity checks passed; rendered 64 actual bell frames at 50ms.");return 0;
 }
}
