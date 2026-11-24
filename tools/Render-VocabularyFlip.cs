using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using DragonCompanion;

static class RenderVocabularyFlip {
 [STAThread] static void Main(string[] args){
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  string folder=Path.GetFullPath(args[0]);Directory.CreateDirectory(folder);
  using(var form=new Form{ClientSize=new Size(640,410),StartPosition=FormStartPosition.Manual,Location=new Point(-3000,-3000),BackColor=Color.FromArgb(251,250,254)})
  using(var card=new VocabularyFlashcard{Bounds=new Rectangle(20,20,600,340)}){
   var word=new VocabularyWord{Word="grow",Explanation="v. 生长；成长；逐渐变得",Example="Small steps help us grow every day.\n每天的小进步帮助我们成长。",Forms="过去式 grew；过去分词 grown；现在分词 growing"};
   form.Controls.Add(card);card.SetWord(word);form.Show();Application.DoEvents();
   var watch=Stopwatch.StartNew();var timings=new List<string>();bool flipped=false;int index=0;
   while(watch.ElapsedMilliseconds<1900){
    if(!flipped&&watch.ElapsedMilliseconds>=600){card.Reveal();flipped=true;}
    Application.DoEvents();
    if(watch.ElapsedMilliseconds>=index*25){
     string file=index.ToString("000")+".png";
     using(var image=new Bitmap(640,410)){
      using(var graphics=Graphics.FromImage(image)){
       graphics.Clear(form.BackColor);
       Theme.Text(graphics,Theme.Credit,9,Theme.Muted,new RectangleF(20,380,600,25));
      }
      card.DrawToBitmap(image,new Rectangle(20,20,600,340));
      image.Save(Path.Combine(folder,file),ImageFormat.Png);
     }
     timings.Add(file+","+watch.ElapsedMilliseconds);index++;
    }
    Thread.Sleep(1);
   }
   File.WriteAllLines(Path.Combine(folder,"timings.csv"),timings);
  }
 }
}
