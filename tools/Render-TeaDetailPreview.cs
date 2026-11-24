using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using DragonCompanion;
static class TeaDetailPreview {
 [STAThread] static void Main(string[] args){
  string folder=Path.Combine(Path.GetFullPath(args[0]),"detail-frames");Directory.CreateDirectory(folder);
  for(int tick=0;tick<132;tick++)using(var image=new Bitmap(640,400))using(var native=new Bitmap(192,192)){
   using(var n=Graphics.FromImage(native))Art.Draw(n,new Rectangle(0,0,192,192),"anime","tea",tick);
   using(var g=Graphics.FromImage(image)){
    g.Clear(Color.FromArgb(250,247,255));
    Theme.Text(g,"二次元 · 喝茶",12,Theme.Ink,new RectangleF(16,8,288,28),true);
    Theme.Text(g,"手腕与袖口放大",12,Theme.Ink,new RectangleF(336,8,288,28),true);
    Art.Draw(g,new Rectangle(16,38,288,300),"anime","tea",tick);
    g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
    g.DrawImage(native,new Rectangle(336,90,288,192),new Rectangle(58,92,72,48),GraphicsUnit.Pixel);
    Theme.Text(g,Theme.Credit,9,Theme.Muted,new RectangleF(16,366,608,24));
   }
   image.Save(Path.Combine(folder,tick.ToString("000")+".png"),ImageFormat.Png);
  }
  Console.WriteLine("Rendered 132 real anime tea samples with an unmodified native-frame close-up.");
 }
}
