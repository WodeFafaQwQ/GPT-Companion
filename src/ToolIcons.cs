using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DragonCompanion {
public sealed class ToolDefinition {public string Id,Name,Summary,Hint,Icon;}
public static class UtilityCatalog {
 public static readonly ToolDefinition[] Items={new ToolDefinition{Id="pomodoro",Name="番茄钟",Summary="选择专注时长，到时提醒休息",Hint="支持自定义时间、暂停与继续",Icon="pomodoro"},new ToolDefinition{Id="schedules",Name="日程提醒",Summary="安排常规日程与特殊事件",Hint="单次 / 每天 / 每周 / 每月 / 每年",Icon="calendar"},new ToolDefinition{Id="plans",Name="制定计划",Summary="输入目标与期限，Codex 制定每日安排",Hint="晚间询问完成情况，未完成自动重排",Icon="planner"},new ToolDefinition{Id="vocabulary",Name="单词卡片",Summary="翻面查看解释、例句和词形变化",Hint="高考 / 四级 / 六级 / 雅思 / 自定义",Icon="vocabulary"}};
}
public sealed class CatalogIcon:SurfaceControl {
 public string Symbol="pomodoro",GamePath="",GameId="";
 public CatalogIcon(){Size=new Size(64,64);TabStop=false;}
 protected override void OnPaint(PaintEventArgs e){if(GameId!="")using(var icon=LineIcons.Game(GamePath,GameId))e.Graphics.DrawImage(icon,ClientRectangle);else LineIcons.DrawTool(e.Graphics,ClientRectangle,Symbol);}
}
public static partial class LineIcons {
 static readonly Dictionary<string,Bitmap> gameCache=new Dictionary<string,Bitmap>();
 public static bool HasToolSymbol(string symbol){return symbol=="pomodoro"||symbol=="calendar"||symbol=="planner"||symbol=="vocabulary";}
 [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="PrivateExtractIconsW")]static extern uint ExtractIcons(string file,int index,int width,int height,IntPtr[] handles,uint[] ids,uint count,uint flags);
 [DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr handle);
 public static Bitmap OriginalGameIcon(string path){var handles=new IntPtr[1];try{if(ExtractIcons(path,0,256,256,handles,new uint[1],1,0)>0&&handles[0]!=IntPtr.Zero)using(var icon=Icon.FromHandle(handles[0]))return icon.ToBitmap();}finally{if(handles[0]!=IntPtr.Zero)DestroyIcon(handles[0]);}using(var icon=Icon.ExtractAssociatedIcon(path))return icon==null?null:icon.ToBitmap();}
 public static void DrawTool(Graphics g,Rectangle area,string symbol){var saved=g.Save();g.TranslateTransform(area.X,area.Y);g.ScaleTransform(area.Width/64f,area.Height/64f);g.SmoothingMode=SmoothingMode.AntiAlias;Theme.Box(g,new RectangleF(2,2,60,60),Color.White,15);using(var outline=new Pen(Color.FromArgb(184,158,225),1.6f))using(var path=Theme.Round(new RectangleF(2,2,60,60),15))g.DrawPath(outline,path);using(var pen=new Pen(Theme.Purple,2.2f)){pen.StartCap=pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;
  if(symbol=="pomodoro"){g.DrawEllipse(pen,17,20,30,29);g.DrawArc(pen,22,13,20,14,195,150);g.DrawLine(pen,32,22,32,32);g.DrawLine(pen,32,32,39,36);g.DrawLine(pen,28,15,25,11);g.DrawLine(pen,32,15,36,10);g.DrawLine(pen,22,48,20,51);g.DrawLine(pen,42,48,44,51);}
  else if(symbol=="calendar"){using(var path=Theme.Round(new RectangleF(15,17,34,34),7))g.DrawPath(pen,path);g.DrawLine(pen,15,28,49,28);g.DrawLine(pen,24,13,24,22);g.DrawLine(pen,40,13,40,22);g.DrawLines(pen,new[]{new Point(24,39),new Point(30,44),new Point(41,34)});}
  else if(symbol=="planner"){using(var path=Theme.Round(new RectangleF(17,16,31,36),6))g.DrawPath(pen,path);Theme.Box(g,new RectangleF(24,12,17,8),Color.White,3);using(var path=Theme.Round(new RectangleF(24,12,17,8),3))g.DrawPath(pen,path);for(int y=28;y<=46;y+=9){g.DrawLines(pen,new[]{new Point(22,y),new Point(24,y+2),new Point(27,y-2)});g.DrawLine(pen,32,y,42,y);}}
  else if(symbol=="vocabulary"){
   using(var rear=Theme.Round(new RectangleF(12,15,31,35),5))g.DrawPath(pen,rear);
   Theme.Box(g,new RectangleF(20,20,31,35),Color.White,5);
   using(var front=Theme.Round(new RectangleF(20,20,31,35),5))g.DrawPath(pen,front);
   g.DrawLines(pen,new[]{new Point(28,42),new Point(35,28),new Point(42,42)});
   g.DrawLine(pen,31,37,39,37);
   g.DrawLine(pen,28,48,43,48);
  }
  else{using(var path=Theme.Round(new RectangleF(17,17,30,30),7))g.DrawPath(pen,path);g.DrawLine(pen,24,32,40,32);g.DrawLine(pen,32,24,32,40);}
 }g.Restore(saved);}
 public static Bitmap Tool(string symbol,int size=64){var bitmap=new Bitmap(size,size,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(bitmap))DrawTool(g,new Rectangle(0,0,size,size),symbol);return bitmap;}
 public static bool IsCircular(Bitmap source){int left=source.Width,top=source.Height,right=-1,bottom=-1;for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++)if(source.GetPixel(x,y).A>64){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}if(right<=left||bottom<=top)return false;int span=Math.Min(right-left,bottom-top),inset=Math.Max(1,span/10);return source.GetPixel(left+inset,top+inset).A<32&&source.GetPixel(right-inset,top+inset).A<32&&source.GetPixel(left+inset,bottom-inset).A<32&&source.GetPixel(right-inset,bottom-inset).A<32;}
 public static Bitmap OutlineGame(Bitmap source,int size=64){return SmoothOutlineGame(source,size);}
 public static Bitmap Game(string path,string id){string key=id+"|"+path;Bitmap cached;if(gameCache.TryGetValue(key,out cached))return (Bitmap)cached.Clone();Bitmap output=null;if(File.Exists(path))try{using(var bitmap=OriginalGameIcon(path))if(bitmap!=null)output=OutlineGame(bitmap);}catch{}if(output==null)using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(id+"-icon.png")){if(stream!=null)using(var original=new Bitmap(stream))output=OutlineGame(original);}if(output==null){output=new Bitmap(64,64,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(output)){g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.White);using(var pen=new Pen(Theme.Purple,2))g.DrawEllipse(pen,6,6,52,52);Theme.Text(g,id=="osu-mania"?"osu!":"游戏",11,Theme.Purple,new RectangleF(16,23,44,25),true);}}gameCache[key]=output;return (Bitmap)output.Clone();}
}
}
