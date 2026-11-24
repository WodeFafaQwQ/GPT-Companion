using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Text;
using DragonCompanion;

// Render a close-up with the same Art.Draw path used by the desktop pet.
static class ReadingPreview {
 [STAThread]static void Main(string[] args){string folder=Path.GetFullPath(args[0]);Directory.CreateDirectory(folder);var timings=new List<string>();int index=-1,hold=0;string previous="",filename="";long duration=ReadingBook.CycleMilliseconds+ActivityArt.Find("read").Duration-2500;
  for(int elapsed=0;elapsed<duration;elapsed+=50){int tick=(elapsed+2500)/50;bool thinking=ReadingBook.IsThinking(tick*50L);string key=ReadingBook.PreviewKey(tick*50L);if(key!=previous){if(index>=0)timings.Add(filename+","+hold+","+previous);index++;filename=index.ToString("000")+".png";hold=0;previous=key;using(var image=new Bitmap(640,382)){using(var graphics=Graphics.FromImage(image)){graphics.Clear(Color.FromArgb(250,247,255));Theme.Text(graphics,"像素风",11,Theme.Ink,new RectangleF(18,12,300,30),true);Theme.Text(graphics,"二次元风",11,Theme.Ink,new RectangleF(338,12,300,30),true);Art.Draw(graphics,new Rectangle(24,45,272,272),"pixel","read",tick);Art.Draw(graphics,new Rectangle(344,45,272,272),"anime","read",tick);Theme.Text(graphics,thinking?"保留原动作：持书思考片刻":"连续翻页，随后思考，再继续阅读",10,Theme.Purple,new RectangleF(18,328,600,27),true);Theme.Text(graphics,Theme.Credit,8,Theme.Muted,new RectangleF(18,361,600,20));}image.Save(Path.Combine(folder,filename),ImageFormat.Png);}}hold+=50;}
  timings.Add(filename+","+hold+","+previous);File.WriteAllLines(Path.Combine(folder,"timings.csv"),timings,Encoding.UTF8);Console.WriteLine("Rendered "+(index+1)+" reading keyframes with actual hold times: "+folder);
 }
}
