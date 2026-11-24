using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using DragonCompanion;
static class IdlePreview {
 [STAThread]static void Main(string[] args){
  IdleArt.Prepare("pixel");IdleArt.Prepare("anime");string root=Path.GetFullPath(args[0]);string folder=Path.Combine(root,"frames");Directory.CreateDirectory(folder);
  int start=args.Length>1?int.Parse(args[1]):0;
  int count=args.Length>2?int.Parse(args[2]):240;
  for(int tick=0;tick<count;tick++)using(var image=new Bitmap(640,400)){
   using(var g=Graphics.FromImage(image)){
    g.Clear(Color.FromArgb(250,247,255));
    for(int col=0;col<2;col++){
     string style=col==0?"pixel":"anime";
     Theme.Text(g,(col==0?"像素风":"二次元风")+" · 待机小动作",12,Theme.Ink,new RectangleF(col*320+16,8,288,28),true);
     Art.Draw(g,new Rectangle(col*320+8,40,304,304),style,"idle",start+tick);
    }
    Theme.Text(g,Theme.Credit,9,Theme.Muted,new RectangleF(16,368,608,24));
   }
   image.Save(Path.Combine(folder,tick.ToString("000")+".png"),ImageFormat.Png);
  }
  Console.WriteLine("Rendered "+count+" real idle frames at 50 ms: quiet pauses, native blink, tail sway and left/right glances.");
 }
}
