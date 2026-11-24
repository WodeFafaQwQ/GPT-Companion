using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace DragonCompanion {
public static class Theme {
 public const string Credit="developed by WodeFafa";
 public static readonly Color Ink=Color.FromArgb(43,35,65), Muted=Color.FromArgb(127,118,145), Purple=Color.FromArgb(119,86,216), Light=Color.FromArgb(246,243,252), Line=Color.FromArgb(232,226,243), Green=Color.FromArgb(47,148,118);
 public static Font Font(float size, bool bold=false) { return new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Point); }
 public static GraphicsPath Round(RectangleF r,float radius) { var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p; }
 public static void Box(Graphics g,RectangleF r,Color color,float radius=16) {using(var p=Round(r,radius))using(var b=new SolidBrush(color))g.FillPath(b,p);}
 public static void Text(Graphics g,string text,float size,Color c,RectangleF rect,bool bold=false) {using(var f=Font(size,bold))using(var b=new SolidBrush(c))using(var sf=new StringFormat {Trimming=StringTrimming.EllipsisCharacter})g.DrawString(text,f,b,rect,sf);}
 // Paint each control against its actual surface. Replaying transparent parent
 // paint can leak GDI text into siblings during partial invalidation.
 public static Color Surface(Control control){for(var p=control.Parent;p!=null;p=p.Parent){var card=p as RoundedPanel;if(card!=null)return card.FillColor;if(p.BackColor.A==255)return p.BackColor;}return Color.FromArgb(251,250,254);}
 public static void Background(Control control,Graphics g){using(var b=new SolidBrush(Surface(control)))g.FillRectangle(b,control.ClientRectangle);}
 public static void ControlText(Graphics g,string text,Font font,Rectangle rect,Color color,bool center){using(var b=new SolidBrush(color))using(var sf=new StringFormat{Alignment=center?StringAlignment.Center:StringAlignment.Near,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap})g.DrawString(text,font,b,rect,sf);}
 public static void Focus(Graphics g,Rectangle rect){using(var pen=new Pen(Theme.Purple)){pen.DashStyle=DashStyle.Dot;g.DrawRectangle(pen,rect);}}
}
public static class Art {
 // The cache is immutable after construction. Each resource is decoded once;
 // optional poses may be prepared off-thread without racing a Dictionary write
 // against painting on the UI thread.
 static readonly Dictionary<string,Lazy<Bitmap[]>> frames=CreateFrames();
 static Dictionary<string,Lazy<Bitmap[]>> CreateFrames(){var cache=new Dictionary<string,Lazy<Bitmap[]>>();foreach(string name in new[]{"anime","pixel","anime-walk","pixel-walk","anime-lift","pixel-lift","anime-walk-left","pixel-walk-left","anime-pickup","pixel-pickup","anime-mania","pixel-mania"}){string resource=name;cache.Add(resource,new Lazy<Bitmap[]>(()=>ReadFrames(resource),true));}return cache;}
 static Bitmap[] GetFrames(string name){Lazy<Bitmap[]> entry;return frames.TryGetValue(name,out entry)?entry.Value:null;}
 public static int LoadedAssetCount {get{return frames.Values.Count(x=>x.IsValueCreated);}}
 public static void Load() {foreach(string name in new[]{"anime","pixel","anime-walk","pixel-walk","anime-lift","pixel-lift","anime-walk-left","pixel-walk-left"})GetFrames(name);}
 public static void LoadStyle(string style){GetFrames(style);}
 static Bitmap[] ReadFrames(string name){
  if(name.EndsWith("-walk-left")){var original=GetFrames(name.Substring(0,name.Length-5));if(original==null)return null;var mirrored=new Bitmap[original.Length];for(int n=0;n<original.Length;n++){mirrored[n]=(Bitmap)original[n].Clone();mirrored[n].RotateFlip(RotateFlipType.RotateNoneFlipX);}return mirrored;}
  using(var atlas=Read(name+".png")){if(atlas==null)return null;if(name.EndsWith("-pickup")||name.EndsWith("-mania"))return new[]{IsolateCharacter(atlas)};bool animated=name.EndsWith("-walk")||name.EndsWith("-lift");var list=new Bitmap[8];for(int n=0;n<8;n++){var cell=new Rectangle(n%4*(atlas.Width/4),n/4*(atlas.Height/2),atlas.Width/4,atlas.Height/2);using(var isolated=atlas.Clone(cell,PixelFormat.Format32bppArgb))list[n]=IsolateCharacter(isolated,animated);}if(animated)NormalizeWalkFrames(list);return list;}
 }
 // All walking frames share one crop and scale, preserving the original foot
 // baseline and body offsets instead of stretching each pose independently.
 static void NormalizeWalkFrames(Bitmap[] list){int left=list[0].Width,top=list[0].Height,right=0,bottom=0;foreach(var b in list){var data=b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{var row=new byte[b.Width*4];for(int y=0;y<b.Height;y++){Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length);for(int x=0;x<b.Width;x++)if(row[x*4+3]>=32){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}}}finally{b.UnlockBits(data);}}if(right<left)return;left=Math.Max(0,left-4);top=Math.Max(0,top-4);right=Math.Min(list[0].Width-1,right+4);bottom=Math.Min(list[0].Height-1,bottom+4);var crop=new Rectangle(left,top,right-left+1,bottom-top+1);for(int n=0;n<list.Length;n++){var old=list[n];list[n]=old.Clone(crop,PixelFormat.Format32bppArgb);old.Dispose();}}
 // Each sprite owns a separate texture. Retain the connected character and a
 // two-pixel alpha fringe; discard detached fragments from neighbouring cells.
 // This prevents both cross-cell interpolation and stray horns at atlas seams.
 public static Bitmap IsolateCharacter(Bitmap cell,bool preserveCanvas=false){
  int w=cell.Width,h=cell.Height,count=w*h;var data=cell.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);var bytes=new byte[data.Stride*h];Marshal.Copy(data.Scan0,bytes,0,bytes.Length);int stride=data.Stride;cell.UnlockBits(data);
  var visited=new bool[count];var queue=new int[count];int[] largest=new int[0];
  for(int seed=0;seed<count;seed++){if(visited[seed]||bytes[(seed/w)*stride+(seed%w)*4+3]<32)continue;int head=0,tail=1;queue[0]=seed;visited[seed]=true;while(head<tail){int p=queue[head++],x=p%w,y=p/w;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=w||ny>=h)continue;int q=ny*w+nx;if(!visited[q]&&bytes[ny*stride+nx*4+3]>=32){visited[q]=true;queue[tail++]=q;}}}if(tail>largest.Length){largest=new int[tail];Array.Copy(queue,largest,tail);}}
  var keep=new bool[count];int left=w,top=h,right=0,bottom=0;foreach(int p in largest){int x=p%w,y=p/w;left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++){int nx=x+dx,ny=y+dy;if(nx>=0&&ny>=0&&nx<w&&ny<h)keep[ny*w+nx]=true;}}
  for(int p=0;p<count;p++)if(!keep[p])bytes[(p/w)*stride+(p%w)*4+3]=0;
  using(var cleaned=new Bitmap(w,h,PixelFormat.Format32bppArgb)){var output=cleaned.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);Marshal.Copy(bytes,0,output.Scan0,bytes.Length);cleaned.UnlockBits(output);if(preserveCanvas)return (Bitmap)cleaned.Clone();if(largest.Length==0)return new Bitmap(8,8,PixelFormat.Format32bppArgb);left=Math.Max(0,left-3);top=Math.Max(0,top-3);right=Math.Min(w-1,right+3);bottom=Math.Min(h-1,bottom+3);var crop=cleaned.Clone(new Rectangle(left,top,right-left+1,bottom-top+1),PixelFormat.Format32bppArgb);var padded=new Bitmap(crop.Width+8,crop.Height+8,PixelFormat.Format32bppArgb);using(crop)using(var g=Graphics.FromImage(padded))g.DrawImageUnscaled(crop,4,4);return padded;}
 }
 static Bitmap Read(string name) { var s=Assembly.GetExecutingAssembly().GetManifestResourceStream(name);if(s==null)return null;using(s)using(var b=new Bitmap(s))return new Bitmap(b); }
 public static Bitmap OriginalIdle(string style,bool closed){var list=GetFrames(style);return list==null?null:list[closed?1:0];}
 public static Bitmap OriginalReading(string style){var list=GetFrames(style);return list==null?null:list[2];}
 static readonly Dictionary<string,Lazy<Bitmap>> celebrations=new Dictionary<string,Lazy<Bitmap>>{{"pixel",new Lazy<Bitmap>(()=>ReadCelebration("pixel"),true)},{"anime",new Lazy<Bitmap>(()=>ReadCelebration("anime"),true)}};
 static Bitmap ReadCelebration(string style){using(var atlas=Read(style+"-daily-8.png")){var poses=ActivitySprites.SplitRow(atlas,1);var result=poses[3];for(int i=0;i<poses.Length;i++)if(i!=3)poses[i].Dispose();return result;}}
 public static Bitmap OriginalCelebration(string style){return celebrations[style].Value;}
 public static bool Available(string style) {return GetFrames(style)!=null;}
 public static void LoadPickup(){foreach(string style in new[]{"pixel","anime"})GetFrames(style+"-pickup");}
 public static void LoadMania(){foreach(string style in new[]{"pixel","anime"})GetFrames(style+"-mania");}
 public static void PrepareMotion(string style){IdleArt.Prepare(style);ActivityArt.Prepare(style);ReadingBook.Prepare(style);GetFrames(style+"-pickup");GetFrames(style+"-lift");RigArt.Prepare(style);}
 static bool DrawPickup(Graphics g,Rectangle rect,string style,string state,int tick){var list=GetFrames(style+"-pickup");if(list==null)return false;var b=list[0];double angle=state=="grab"?Math.Sin(tick*.14)*3:Math.Min(1,tick/4.0)*35,rad=Math.Abs(angle)*Math.PI/180;float scale=(float)Math.Min((rect.Width-12)/(b.Width*Math.Cos(rad)+b.Height*Math.Sin(rad)),(rect.Height-12)/(b.Width*Math.Sin(rad)+b.Height*Math.Cos(rad)));var saved=g.Save();g.TranslateTransform(rect.X+rect.Width/2f,rect.Y+rect.Height/2f);g.RotateTransform((float)angle);g.InterpolationMode=style=="pixel"?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(b,new RectangleF(-b.Width*scale/2,-b.Height*scale/2,b.Width*scale,b.Height*scale));g.Restore(saved);return true;}
 public static string AmbientPose(string state,int tick,int mode){if(state!="idle")return state;int cycle=mode==0?3600:mode==1?1800:1200;int duration=mode==0?240:mode==1?200:160;int phase=tick%cycle;if(phase>=cycle-duration)return "look";return "idle";}
 public static int Frame(string state,int tick,int mode=1) {switch(state){case "working":case "look":return 2;case "waiting":return 3;case "complete":return 4;case "error":case "interrupted":return 5;case "wave":return 6;case "sleep":return 7;default:int interval=mode==0?250:mode==1?125:65;return tick%interval>=interval-6?1:0;}}
 public static int WalkFrame(int tick){return tick/2%8;}
 static float Smooth(float a,float b,float x){float t=Math.Max(0,Math.Min(1,(x-a)/(b-a)));return t*t*(3-2*t);}
 static float Bell(float value,float center,float radius){float t=Math.Abs(value-center)/radius;return t>=1?0:(float)(.5+.5*Math.Cos(t*Math.PI));}
 public static float ManiaHandDisplacement(string style,float x,float y,int tick,int handMask){if(x<.75f||y<.47f||y>.76f)return 0;bool pixel=style=="pixel";float far=Smooth(.77f,.89f,x)*(1-Smooth(.89f,.99f,x))*Bell(y,pixel?.565f:.55f,.06f),near=Smooth(.80f,.97f,x)*Bell(y,pixel?.61f:.585f,.075f);return (float)(-2.4*(((handMask&1)!=0?far*Math.Sin(tick*Math.PI/3):0)+((handMask&2)!=0?near*Math.Sin((tick+1)*Math.PI/3):0)));}
 static void DrawManiaSprite(Graphics g,Bitmap sprite,Rectangle destination,string style,int tick,int hands){
  if(hands==0){g.DrawImage(sprite,destination);return;}
  // Deform only a small arm region. The displacement fades to zero at wrists
  // and region edges, retaining the chosen pose and continuous sleeve joints.
  var band=new RectangleF(destination.X+destination.Width*.75f,destination.Y+destination.Height*.47f,destination.Width*.25f,destination.Height*.29f);var saved=g.Save();g.SetClip(band,CombineMode.Exclude);g.DrawImage(sprite,destination);g.Restore(saved);
  int columns=8,rows=10;using(var attributes=new ImageAttributes()){attributes.SetWrapMode(WrapMode.TileFlipXY);for(int row=0;row<rows;row++)for(int column=0;column<columns;column++){
   float u0=.75f+.25f*column/columns,u1=.75f+.25f*(column+1)/columns,v0=.47f+.29f*row/rows,v1=.47f+.29f*(row+1)/rows,amplitude=destination.Width/156f;
   var a=new PointF(destination.X+u0*destination.Width,destination.Y+v0*destination.Height+ManiaHandDisplacement(style,u0,v0,tick,hands)*amplitude);var b=new PointF(destination.X+u1*destination.Width,destination.Y+v0*destination.Height+ManiaHandDisplacement(style,u1,v0,tick,hands)*amplitude);var c=new PointF(destination.X+u0*destination.Width,destination.Y+v1*destination.Height+ManiaHandDisplacement(style,u0,v1,tick,hands)*amplitude);
   g.DrawImage(sprite,new[]{a,b,c},new RectangleF(u0*sprite.Width,v0*sprite.Height,(u1-u0)*sprite.Width,(v1-v0)*sprite.Height),GraphicsUnit.Pixel,attributes);
  }}
 }
 public static void Draw(Graphics g,Rectangle rect,string style,string state,int tick,int mode=1,bool centered=false) {
  if(ActivityArt.Draw(g,rect,style,state,tick))return;
  if(state=="idle"&&IdleArt.Draw(g,rect,style,tick,centered))return;
  if((state=="grab"||(state=="drop"&&tick<4))&&DrawPickup(g,rect,style,state,tick))return;
  Bitmap[] gamePose;if(state=="osu"&&(gamePose=GetFrames(style+"-mania"))!=null){var b=gamePose[0];float factor=Math.Min((float)(rect.Width-4)/b.Width,(float)(rect.Height-4)/b.Height);int dw=(int)(b.Width*factor),dh=(int)(b.Height*factor);g.InterpolationMode=style=="pixel"?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;DrawManiaSprite(g,b,new Rectangle(rect.X+(rect.Width-dw)/2,rect.Bottom-dh-2,dw,dh),style,tick,mode);return;}
  if((state.StartsWith("walk-")||state.StartsWith("fly-"))&&RigArt.Draw(g,rect,style,state,tick))return;
  bool walking=state=="walk-left"||state=="walk-right",lifting=state=="grab"||state=="drop";Bitmap[] list;
  if((list=GetFrames(style+(lifting?"-lift":walking?(state=="walk-left"?"-walk-left":"-walk"):"")))!=null) {int f=lifting?(state=="grab"?tick/4%4:4+Math.Min(3,tick/4)):walking?WalkFrame(tick):Frame(state,tick,mode);var sprite=list[f];int w=sprite.Width,h=sprite.Height;float factor=Math.Min((float)(rect.Width-8)/w,(float)(rect.Height-8)/h);int dw=Math.Max(1,(int)(w*factor)),dh=Math.Max(1,(int)(h*factor));var dest=new Rectangle(rect.X+(rect.Width-dw)/2,centered?rect.Y+(rect.Height-dh)/2:rect.Bottom-dh-4,dw,dh);g.InterpolationMode=style=="pixel"?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;using(var attributes=new ImageAttributes()){attributes.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(sprite,dest,0,0,w,h,GraphicsUnit.Pixel,attributes);}return;}
  // A small deterministic fallback keeps the app usable if a custom build omits art.
  using(var b=new Bitmap(64,64))using(var pg=Graphics.FromImage(b)) {
   Action<string,int,int,int,int> r=(hex,x,y,w,h)=>{using(var brush=new SolidBrush(ColorTranslator.FromHtml(hex)))pg.FillRectangle(brush,x,y,w,h);};
   r("#ada0cc",39,42,18,9);r("#ece9fb",42,42,16,6);r("#ece9fb",54,35,5,10);r("#9884bc",14,11,35,34);r("#f4f0ff",16,10,31,35);r("#c3b8d4",17,4,5,12);r("#c3b8d4",42,4,5,12);r("#fff4e9",23,19,20,18);r("#fefcff",19,13,25,8);r("#fefcff",19,16,6,15);r("#fefcff",38,16,6,9);r("#7953ad",26,26,4,6);r("#7953ad",35,26,4,6);r("#ffffff",26,26,2,2);r("#ffffff",35,26,2,2);r("#e5b8ca",25,32,4,2);r("#e5b8ca",37,32,4,2);r("#a78bcd",23,38,20,15);r("#e8dff8",21,44,24,10);r("#ffffff",21,51,24,3);r("#827499",25,54,6,6);r("#827499",36,54,6,6);r("#fff4e9",17,38,6,9);r("#fff4e9",43,38,6,9);
   g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(b,rect);
  }
 }
 public static Icon MakeIcon() {using(var source=Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico")){if(source!=null)using(var icon=new Icon(source,new Size(64,64)))return (Icon)icon.Clone();}using(var b=new Bitmap(64,64)) {using(var g=Graphics.FromImage(b))Draw(g,new Rectangle(0,0,64,64),"pixel","idle",0);var h=b.GetHicon();try{return (Icon)Icon.FromHandle(h).Clone();}finally{DestroyIcon(h);}}}
 [DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr h);
}
public sealed class LayeredPet:Form {
 string gameId="osu-mania";bool initialPositioning;Point? initialOrigin;PointF keyboardHand;
 public readonly Cs2InputState Cs2Input=new Cs2InputState();readonly Cs2RawInput rawInput;
 public string GameId {get{return gameId;}}
 public bool Positioning {get{return gamePositioning||initialPositioning;}}
 IGameProfile GameProfile {get{return app.Games.Find(gameId)??app.Games.OsuMania;}}
 bool Cs2Feedback {get{return gameMode&&gameId=="cs2"&&!Positioning&&!gameDestination.HasValue&&app.Games.Cs2.Enabled&&app.Games.Cs2.Foreground;}}
 public string InputError {get{return rawInput==null?"":rawInput.Error;}}
 readonly IdleActivities idleActivities=new IdleActivities();string lastVisualState="idle";int visualStartedTick;
 readonly Controller app; readonly Timer animation=new Timer {Interval=50}; readonly Random movement=new Random();Bitmap surface;int tick,walkTick,dragTick,landingTick=-1,route;Point down,start;bool lifted,chordUsed,flying,gameMode,gamePositioning;MouseButtons pressed;Point? target,gameDestination,gameOrigin;PointF precise;RectangleF bubbleBounds;Rectangle gameArea;const int PetTop=246;DateTimeOffset nextMove=DateTimeOffset.UtcNow.AddSeconds(WanderMotion.InitialPauseSeconds),lastMotion=DateTimeOffset.UtcNow;double motionSeconds;
 public LayeredPet(Controller controller) {
  app=controller;rawInput=new Cs2RawInput(Cs2Input,()=>Cs2Feedback);Text="GPT · 桌面伙伴";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;TopMost=app.Settings.Topmost;
  ApplySize();Location=HasInitialPosition?new Point(app.Settings.InitialX,app.Settings.InitialY):app.Settings.X==int.MinValue?DefaultNormalPosition():new Point(app.Settings.X,app.Settings.Y);Clamp();
  animation.Tick+=(s,e)=>Animate();
  MouseDown+=(s,e)=>{if(pressed==MouseButtons.None&&!lifted)chordUsed=false;pressed|=e.Button;StopWalking();if(Positioning)Capture=true;if(GrabChord(pressed)&&!lifted){down=Cursor.Position;start=Location;lifted=true;chordUsed=true;dragTick=0;landingTick=-1;Capture=true;Render();}};
  MouseMove+=(s,e)=>{if(lifted){UpdateDrag();Render();}};
  MouseUp+=(s,e)=>{pressed&=~e.Button;if(lifted&&!GrabChord(PhysicalButtons()))Drop();if(chordUsed){if(PhysicalButtons()==MouseButtons.None)Capture=false;return;}Capture=false;if(Positioning)return;if(e.Button==MouseButtons.Right){app.Menu.Show(Cursor.Position);return;}if(e.Button!=MouseButtons.Left)return;if(DateTimeOffset.UtcNow<app.BubbleUntil&&bubbleBounds.Contains(e.Location)&&app.LastThread!="")app.OpenThread();else app.Pat();};
  MouseCaptureChanged+=(s,e)=>{if(!Capture){if(lifted&&GrabChord(PhysicalButtons())){BeginInvoke((Action)(()=>{if(lifted&&!IsDisposed)Capture=true;}));}else{if(lifted)Drop();pressed=MouseButtons.None;}}};
  MouseDoubleClick+=(s,e)=>{if(e.Button==MouseButtons.Left&&!chordUsed&&!lifted&&!Positioning)app.ShowPanel();};
  Shown+=(s,e)=>{Render();animation.Start();};FormClosed+=(s,e)=>{animation.Dispose();rawInput.Dispose();if(surface!=null)surface.Dispose();};
 }
 protected override bool ShowWithoutActivation {get{return true;}}
 void Animate(){tick++;var now=DateTimeOffset.UtcNow;double dt=Math.Max(0,Math.Min(.1,(now-lastMotion).TotalSeconds));lastMotion=now;if(lifted){dragTick++;UpdateDrag();}if(landingTick>=0&&++landingTick>=16)landingTick=-1;
  if(gameDestination.HasValue&&!lifted){var destination=gameDestination.Value;double dx=destination.X-Left,dy=destination.Y-Top,distance=Math.Sqrt(dx*dx+dy*dy),step=430*dt;walkTick++;if(distance<=step){Location=destination;gameDestination=null;if(!gameMode)gameOrigin=null;}else Location=new Point(Left+(int)Math.Round(dx/distance*step),Top+(int)Math.Round(dy/distance*step));}
  else if(!gameMode&&!initialPositioning){
   bool canMove=!app.Menu.Visible&&app.Settings.Wander&&app.State=="idle"&&pressed==MouseButtons.None&&!lifted&&landingTick<0&&now>app.BubbleUntil;
   if(!canMove&&target.HasValue)StopWalking();
   if(canMove){if(!target.HasValue&&now>=nextMove){flying=++route%2==0;var area=CharacterScreen();var offset=WanderMotion.Offset(movement,flying);int dx=offset.X,dy=offset.Y;var point=ClampCharacter(new Point(Left+dx,Top+dy),area,Width,64*app.Settings.Scale);if(Math.Abs(point.X-Left)<40)point=ClampCharacter(new Point(Left-dx,Top-dy),area,Width,64*app.Settings.Scale);target=point;precise=Location;motionSeconds=0;walkTick=0;}
    if(target.HasValue){float dx=target.Value.X-precise.X,dy=target.Value.Y-precise.Y;double distance=Math.Sqrt(dx*dx+dy*dy),speed=(flying?48:24)*app.Settings.Scale/2.0,step=speed*dt;motionSeconds+=dt;walkTick=(int)(motionSeconds*20);if(distance<=step){Location=target.Value;if(flying)landingTick=8;StopWalking();}else{precise=new PointF(precise.X+(float)(dx/distance*step),precise.Y+(float)(dy/distance*step));Location=Point.Round(precise);}}
   }
  }idleActivities.Advance(tick,app.PoseState=="idle"&&!gameMode&&!Positioning&&!lifted&&!target.HasValue&&!gameDestination.HasValue&&landingTick<0&&pressed==MouseButtons.None&&!app.Menu.Visible);if(gameMode&&gameId=="cs2"){if(!Cs2Feedback)Cs2Input.Clear();Cs2Input.Step(dt);var goal=Cs2Art.KeyboardHand(Cs2Input.LatestKey);float ease=(float)(1-Math.Exp(-16*dt));keyboardHand=new PointF(keyboardHand.X+(goal.X-keyboardHand.X)*ease,keyboardHand.Y+(goal.Y-keyboardHand.Y)*ease);}if(!app.Diagnostics&&IsHandleCreated)rawInput.Enable(Handle,gameMode&&gameId=="cs2"&&!Positioning);Render();
 }
 public bool GameMode {get{return gameMode;}}
 public bool PositioningGame {get{return gamePositioning;}}
 public RectangleF GamePlacementBounds(){if(initialPositioning)return new RectangleF((Width-64*app.Settings.Scale)/2-8,PetTop-8,64*app.Settings.Scale+16,64*app.Settings.Scale+16);if(gameId=="cs2")return Cs2Art.Bounds(Width,PetTop,64*app.Settings.Scale);var layout=ManiaSceneLayout.Create(Width,PetTop,64*app.Settings.Scale,app.Games.OsuMania.Corner,app.Games.OsuMania.CurrentKeys);var bounds=RectangleF.Union(layout.Character,RectangleF.Union(layout.FurnitureBounds,layout.Cushion));foreach(var key in layout.Keycaps)bounds=RectangleF.Union(bounds,key);bounds.Inflate(7,7);return RectangleF.Intersect(bounds,new RectangleF(1,1,Width-3,Height-3));}
 public Point NormalLocation {get{return initialOrigin??gameOrigin??Location;}}
 public bool HasInitialPosition {get{return app.Settings.CustomInitialPosition&&app.Settings.InitialX!=int.MinValue&&app.Settings.InitialY!=int.MinValue;}}
 public Point DefaultNormalPosition(){var work=Screen.PrimaryScreen.WorkingArea;return new Point(work.Right-Width-35,work.Bottom-PetTop-64*app.Settings.Scale-48);}
 public void BeginInitialPlacement(){EndGamePlacement();if(gameMode){gameMode=false;Location=gameOrigin??Location;gameOrigin=null;}gameDestination=null;initialOrigin=Location;initialPositioning=true;Cursor=Cursors.SizeAll;TopMost=true;StopWalking();Render();}
 public void ResetNormalPosition(){EndGamePlacement();app.Settings.CustomInitialPosition=false;var point=DefaultNormalPosition();if(gameMode)gameOrigin=point;else{gameDestination=null;Location=point;Clamp();}app.Settings.X=point.X;app.Settings.Y=point.Y;app.Save();Render();}
 public void CancelPlacement(){EndGamePlacement();app.RefreshGameMode();Render();}
 public static Point GameCorner(Rectangle area,int width,int scale,string corner,int[] keys=null){var layout=ManiaSceneLayout.Create(width,PetTop,64*scale,corner,keys??ManiaKeys.Defaults(4));return new Point(corner=="left"?area.Left+12:area.Right-width-12,area.Bottom-layout.Bottom-12);}
 public static Point ClampGamePosition(Point location,Rectangle area,int width,int scale,string corner,int[] keys){var layout=ManiaSceneLayout.Create(width,PetTop,64*scale,corner,keys);float left=Math.Min(layout.Character.Left,Math.Min(layout.FurnitureBounds.Left,layout.Keycaps.Min(r=>r.Left))),right=Math.Max(layout.Character.Right,Math.Max(layout.FurnitureBounds.Right,layout.Keycaps.Max(r=>r.Right))),top=Math.Min(layout.Character.Top,layout.Monitor.Top-3);return new Point((int)Math.Max(area.Left+6-left,Math.Min(area.Right-6-right,location.X)),(int)Math.Max(area.Top+6-top,Math.Min(area.Bottom-6-layout.Bottom,location.Y)));}
 public static Point ClampScene(Point position,Rectangle area,RectangleF scene){return new Point((int)Math.Max(area.Left+6-scene.Left,Math.Min(area.Right-6-scene.Right,position.X)),(int)Math.Max(area.Top+6-scene.Top,Math.Min(area.Bottom-6-scene.Bottom,position.Y)));}
 Point ClampCurrentGame(Point position,Rectangle area){return gameId=="cs2"?ClampScene(position,area,Cs2Art.Bounds(Width,PetTop,64*app.Settings.Scale)):ClampGamePosition(position,area,Width,app.Settings.Scale,app.Games.OsuMania.Corner,app.Games.OsuMania.CurrentKeys);}
 public Point GameDestination(){var profile=GameProfile;if(!profile.FixedPosition){if(gameId=="cs2"){var scene=Cs2Art.Bounds(Width,PetTop,64*app.Settings.Scale);return new Point((int)(profile.Corner=="left"?gameArea.Left+12-scene.Left:gameArea.Right-12-scene.Right),(int)(gameArea.Bottom-scene.Bottom-12));}return GameCorner(gameArea,Width,app.Settings.Scale,profile.Corner,app.Games.OsuMania.CurrentKeys);}var position=profile.SavedPosition;var area=Screen.AllScreens.OrderByDistance(new Point(position.X+Width/2,position.Y+PetTop+64*app.Settings.Scale));return ClampCurrentGame(position,area);}
 public void SetGameCorner(){if(gameMode&&!gamePositioning){var destination=GameDestination();gameDestination=destination!=Location?destination:(Point?)null;}}
 public void BeginGamePlacement(string id="osu-mania"){EndGamePlacement();EnterGame(CharacterScreen(),id);gamePositioning=true;Cs2Input.Clear();Cursor=Cursors.SizeAll;TopMost=true;StopWalking();Location=GameDestination();gameDestination=null;Render();}
 public void EndGamePlacement(){gamePositioning=false;if(initialPositioning){initialPositioning=false;if(initialOrigin.HasValue)Location=initialOrigin.Value;initialOrigin=null;}Cursor=Cursors.Default;TopMost=app.Settings.Topmost;}
 public void EnterGame(Rectangle area,string id="osu-mania"){if(!gameMode)gameOrigin=Location;gameId=id;gameMode=true;gameArea=area;Cs2Input.Clear();keyboardHand=PointF.Empty;StopWalking();SetGameCorner();walkTick=0;Render();}
 public void UpdateGameScreen(Rectangle area){if(!gameMode)return;if(area!=gameArea){gameArea=area;SetGameCorner();}}
 public void ExitGame(){if(!gameMode)return;gameMode=false;EndGamePlacement();StopWalking();gameDestination=gameOrigin.HasValue?ClampCharacter(gameOrigin.Value,CharacterScreen(),Width,64*app.Settings.Scale):(Point?)null;Render();}
 void UpdateDrag(){if(!GrabChord(PhysicalButtons())){Drop();return;}var p=Cursor.Position;var location=new Point(start.X+p.X-down.X,start.Y+p.Y-down.Y);var area=Screen.AllScreens.OrderByDistance(p);Location=gamePositioning?ClampCurrentGame(location,area):ClampCharacter(location,area,Width,64*app.Settings.Scale);}
 [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
 static MouseButtons PhysicalButtons(){MouseButtons b=MouseButtons.None;if(GetAsyncKeyState(1)<0)b|=MouseButtons.Left;if(GetAsyncKeyState(2)<0)b|=MouseButtons.Right;return b;}
 public void StopWalking(){target=null;nextMove=DateTimeOffset.UtcNow.AddSeconds(movement.Next(WanderMotion.MinimumPauseSeconds,WanderMotion.MaximumPauseExclusive));}
 public static bool GrabChord(MouseButtons buttons){return (buttons&(MouseButtons.Left|MouseButtons.Right))==(MouseButtons.Left|MouseButtons.Right);}
 void Drop(){lifted=false;landingTick=0;StopWalking();if(initialPositioning){Clamp();app.Settings.CustomInitialPosition=true;app.Settings.InitialX=app.Settings.X=Left;app.Settings.InitialY=app.Settings.Y=Top;initialOrigin=null;EndGamePlacement();app.Save();app.RefreshGameMode();}else if(gamePositioning){Location=ClampCurrentGame(Location,CharacterScreen());GameProfile.SavedPosition=Location;EndGamePlacement();app.Save();app.RefreshGameMode();SetGameCorner();}else{Clamp();if(gameMode){SetGameCorner();}else{app.Settings.X=Left;app.Settings.Y=Top;app.Save();}}Render();}
 protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x80000|0x80;return p;}}
 public void ApplySize(){int size=64*app.Settings.Scale;Size=new Size(Math.Max(360,size+60),size+PetTop*2);TopMost=Positioning||app.Settings.Topmost;SetGameCorner();}
 public RectangleF MeasureBubble(Graphics g){using(var body=Theme.Font(10))using(var title=Theme.Font(10,true)){float maxWidth=Width-20;float natural=Math.Max(g.MeasureString(app.Bubble??"",body).Width,g.MeasureString(app.BubbleTitle??"",title).Width)+30;float w=Math.Max(158,Math.Min(maxWidth,natural));float h=Math.Max(70,Math.Min(PetTop-25,g.MeasureString(app.Bubble??"",body,(int)w-30).Height+49));var area=CharacterScreen();float x=Math.Max(area.Left-Left+6,Math.Min(area.Right-Left-w-6,(Width-w)/2));float y=PetTop-12-h;if(Top+y<area.Top+6)y=PetTop+64*app.Settings.Scale+12;return new RectangleF(x,y,w,h);}}
 Rectangle CharacterScreen(){return Screen.AllScreens.OrderByDistance(new Point(Left+Width/2,Top+PetTop+32*app.Settings.Scale));}
 public static Point ClampCharacter(Point location,Rectangle area,int width,int size){int side=(width-size)/2;return new Point(Math.Max(area.Left-side,Math.Min(area.Right-side-size,location.X)),Math.Max(area.Top-PetTop,Math.Min(area.Bottom-PetTop-size,location.Y)));}
 public void Clamp(){Location=ClampCharacter(Location,CharacterScreen(),Width,64*app.Settings.Scale);}
 public RectangleF MeasureBadge(Graphics g,string text){float w;using(var font=Theme.Font(9))w=Math.Max(80,Math.Min(Width-20,g.MeasureString(text,font).Width+28));var area=CharacterScreen();float x=Math.Max(area.Left-Left+6,Math.Min(area.Right-Left-w-6,(Width-w)/2));float y=PetTop+64*app.Settings.Scale+10;if(Top+y+28>area.Bottom-4)y=PetTop-36;return new RectangleF(x,y,w,28);}
 public Bitmap Compose() {
  var bitmap=new Bitmap(Width,Height,PixelFormat.Format32bppArgb);
  using(var g=Graphics.FromImage(bitmap)) {
   g.SmoothingMode=SmoothingMode.AntiAlias;
   // Layered windows discard fully transparent pixels before WM_NCHITTEST.
   // Keep the explicit placement surface barely nonzero so its empty areas
   // actually receive the mouse, then restore transparency when placement ends.
   if(Positioning)using(var placementFill=new SolidBrush(Color.FromArgb(2,Theme.Purple)))g.FillRectangle(placementFill,GamePlacementBounds());
   bool bubble=!Positioning&&!(gameMode&&gameId=="cs2")&&DateTimeOffset.UtcNow<app.BubbleUntil;
   if(bubble){bubbleBounds=MeasureBubble(g);var r=bubbleBounds;Theme.Box(g,r,Color.FromArgb(250,255,253,255),18);using(var pen=new Pen(Theme.Line))using(var path=Theme.Round(r,18))g.DrawPath(pen,path);Theme.Text(g,app.BubbleTitle,10,Theme.Purple,new RectangleF(r.X+15,r.Y+10,r.Width-30,24),true);Theme.Text(g,app.Bubble,10,Theme.Ink,new RectangleF(r.X+15,r.Y+37,r.Width-30,r.Height-45));using(var b=new SolidBrush(Color.FromArgb(250,255,253,255)))g.FillPolygon(b,new[]{new Point(Width/2-8,(int)r.Bottom-1),new Point(Width/2+8,(int)r.Bottom-1),new Point(Width/2,(int)r.Bottom+9)});}
   string visualState=app.PoseState;if(visualState!=lastVisualState){lastVisualState=visualState;visualStartedTick=tick;}int size=64*app.Settings.Scale;int bob=0;if(app.State=="complete"&&!ActivityArt.Has(visualState)&&!gameMode&&!lifted&&landingTick<0)bob-=(int)(Math.Abs(Math.Sin(tick*.20))*9);
   string pose=gamePositioning?"osu":initialPositioning?"idle":lifted?"grab":gameDestination.HasValue?(gameDestination.Value.X<Left?"fly-left":"fly-right"):gameMode?"osu":landingTick>=0?"drop":target.HasValue?(flying?(target.Value.X<Left?"fly-left":"fly-right"):(target.Value.X<Left?"walk-left":"walk-right")):(visualState=="idle"?idleActivities.Pose:visualState);
   bool[] gameKeys=gameMode?app.Games.OsuMania.Pressed():null;
   if(gameMode&&pose=="osu"&&gameId=="cs2")Cs2Art.Draw(g,Width,PetTop,size,app.Settings.Style,Cs2Input,keyboardHand);
   else if(gameMode&&pose=="osu")DrawGame(g,Width,PetTop,size,app.Settings.Style,app.Games.OsuMania.Corner,app.Games.OsuMania.CurrentKeys,gameKeys,tick);
   else Art.Draw(g,new Rectangle((Width-size)/2,PetTop+bob,size,size),app.Settings.Style,pose,lifted?dragTick:gameDestination.HasValue?walkTick:landingTick>=0?landingTick:target.HasValue?walkTick:visualState=="idle"&&pose!="idle"?tick-idleActivities.StartedTick:ActivityArt.Has(pose)?tick-visualStartedTick:tick,app.Settings.AnimationMode);
   if(Positioning){var r=GamePlacementBounds();using(var pen=new Pen(Color.FromArgb(190,Theme.Purple),1.4f)){pen.DashStyle=DashStyle.Dash;using(var path=Theme.Round(r,10))g.DrawPath(pen,path);}}
   string quota=app.Quota.Main!=null&&app.Quota.Main.Primary!=null?app.Quota.Main.Primary.Remaining.ToString("0")+"%":"—";
   string pill=app.Countdown.Active?(app.Countdown.Paused?"已暂停 ":"番茄钟 ")+app.Countdown.Display:app.StatusLabel+"  ·  额度 "+quota+(app.Quota.Main!=null&&app.Quota.Stale?" · 旧":"");
   float badgeFade=(float)Math.Max(0,Math.Min(1,(app.BadgeUntil-DateTimeOffset.UtcNow).TotalSeconds));
   if(badgeFade>0&&!gameMode){var badge=MeasureBadge(g,pill);Theme.Box(g,badge,Color.FromArgb((int)(240*badgeFade),48,39,67),14);Theme.Text(g,pill,9,Color.FromArgb((int)(255*badgeFade),Color.White),new RectangleF(badge.X+14,badge.Y+5,badge.Width-28,22));}
  }return bitmap;
 }
 public void Render(){if(!IsHandleCreated||IsDisposed||!Visible)return;var b=Compose();var old=surface;surface=b;SetBitmap(b);if(old!=null)old.Dispose();}
 static Matrix PlaneTransform(PointF[] plane,float width,float height){var a=plane[0];return new Matrix((plane[1].X-a.X)/width,(plane[1].Y-a.Y)/width,(plane[3].X-a.X)/height,(plane[3].Y-a.Y)/height,a.X,a.Y);}
 static void FillPlane(Graphics g,PointF[] points,Color fill){using(var brush=new SolidBrush(fill))g.FillPolygon(brush,points);}
 static void DrawGameCushion(Graphics g,RectangleF bounds,string style,bool facesRight){
  var saved=g.Save();g.SmoothingMode=style=="pixel"?SmoothingMode.None:SmoothingMode.AntiAlias;
  if(!facesRight){g.TranslateTransform(bounds.Left+bounds.Right,0);g.ScaleTransform(-1,1);}
  Func<float,float,PointF> p=(x,y)=>new PointF(bounds.Left+x*bounds.Width,bounds.Top+y*bounds.Height);
  using(var shadow=new SolidBrush(Color.FromArgb(25,76,53,108)))g.FillEllipse(shadow,new RectangleF(bounds.X+3,bounds.Bottom-5,bounds.Width-6,9));
  using(var pillow=new GraphicsPath()){
   pillow.AddBezier(p(.04f,.32f),p(.02f,.12f),p(.13f,.11f),p(.25f,.15f));
   pillow.AddBezier(p(.25f,.15f),p(.43f,.10f),p(.69f,.02f),p(.83f,.10f));
   pillow.AddBezier(p(.83f,.10f),p(.98f,.07f),p(1,.26f),p(.95f,.44f));
   pillow.AddBezier(p(.95f,.44f),p(.99f,.67f),p(.93f,.82f),p(.78f,.81f));
   pillow.AddBezier(p(.78f,.81f),p(.59f,.88f),p(.34f,.95f),p(.19f,.86f));
   pillow.AddBezier(p(.19f,.86f),p(.04f,.90f),p(.01f,.66f),p(.04f,.32f));pillow.CloseFigure();
   using(var side=(GraphicsPath)pillow.Clone())using(var offset=new Matrix()){offset.Translate(0,2.5f);side.Transform(offset);using(var edge=new SolidBrush(Color.FromArgb(190,161,216)))g.FillPath(edge,side);}
   using(var fill=new LinearGradientBrush(bounds,Color.FromArgb(243,232,252),Color.FromArgb(216,192,235),LinearGradientMode.Vertical))g.FillPath(fill,pillow);
   using(var outline=new Pen(Color.FromArgb(178,148,203),1))g.DrawPath(outline,pillow);
  }
  using(var seam=new Pen(Color.FromArgb(234,216,245),1.1f))g.DrawBezier(seam,p(.15f,.73f),p(.35f,.83f),p(.62f,.77f),p(.86f,.66f));
  using(var crease=new Pen(Color.FromArgb(189,158,211),.8f)){g.DrawBezier(crease,p(.10f,.61f),p(.11f,.67f),p(.17f,.68f),p(.21f,.71f));g.DrawBezier(crease,p(.91f,.49f),p(.89f,.55f),p(.87f,.58f),p(.82f,.61f));}
  g.Restore(saved);
 }
 public static void DrawGame(Graphics g,int width,int petTop,int size,string style,string corner,int[] keys,bool[] pressed,int tick){
  if(keys==null||pressed==null||keys.Length!=pressed.Length)return;
  var layout=ManiaSceneLayout.Create(width,petTop,size,corner,keys);bool left=layout.FacesRight;var screen=layout.ScreenPlane;var desk=layout.DeskPlane;
  var cushion=layout.Cushion;if(style=="anime")cushion.Offset(0,layout.Character.Height*4/156f);DrawGameCushion(g,cushion,style,left);
  // One shared side-view scene: the desk, physical keyboard and monitor all
  // follow the seated character's perspective. Existing hands cover the keys.
  using(var leg=new Pen(Color.FromArgb(124,96,168),4)){leg.StartCap=LineCap.Round;leg.EndCap=LineCap.Round;for(int n=0;n<layout.DeskFeet.Length;n++)g.DrawLine(leg,desk[n+2],layout.DeskFeet[n]);}
  FillPlane(g,desk.Select(p=>new PointF(p.X,p.Y+4)).ToArray(),Color.FromArgb(132,103,172));FillPlane(g,desk,Color.FromArgb(214,196,237));using(var edge=new Pen(Color.FromArgb(157,122,194),1.2f))g.DrawPolygon(edge,desk);
  var stand=new PointF((screen[2].X+screen[3].X)/2,(screen[2].Y+screen[3].Y)/2);var foot=new PointF(stand.X,stand.Y+15);using(var pen=new Pen(Color.FromArgb(146,120,185),4))g.DrawLine(pen,stand,foot);
  var cross=new PointF((screen[1].X-screen[0].X)*.22f,(screen[1].Y-screen[0].Y)*.22f);var forward=new PointF((desk[1].X-desk[0].X)*.07f,(desk[1].Y-desk[0].Y)*.07f);FillPlane(g,new[]{new PointF(foot.X-cross.X-forward.X,foot.Y-cross.Y-forward.Y),new PointF(foot.X+cross.X-forward.X,foot.Y+cross.Y-forward.Y),new PointF(foot.X+cross.X+forward.X,foot.Y+cross.Y+forward.Y),new PointF(foot.X-cross.X+forward.X,foot.Y-cross.Y+forward.Y)},Color.FromArgb(168,143,205));
  float depth=left?4:-4;FillPlane(g,new[]{screen[1],new PointF(screen[1].X+depth,screen[1].Y-1.3f),new PointF(screen[2].X+depth,screen[2].Y-1.3f),screen[2]},Color.FromArgb(102,77,139));
  var savedScreen=g.Save();using(var matrix=PlaneTransform(screen,170,102))g.MultiplyTransform(matrix,MatrixOrder.Prepend);
  Theme.Box(g,new RectangleF(0,0,170,102),Color.FromArgb(250,38,30,63),9);using(var outline=new Pen(Color.FromArgb(171,143,228),2))using(var path=Theme.Round(new RectangleF(0,0,170,102),9))g.DrawPath(outline,path);
  int gap=3;float laneWidth=(150-gap*(keys.Length-1))/(float)keys.Length;
  for(int i=0;i<keys.Length;i++){
   int slot=ManiaSceneLayout.ScreenSlot(i,keys.Length,left);float x=10+slot*(laneWidth+gap);var lane=new RectangleF(x,10,laneWidth,82);
   if(pressed[i]){Theme.Box(g,new RectangleF(x-2,8,laneWidth+4,88),Color.FromArgb(95,190,155,255),7);Theme.Box(g,lane,Color.FromArgb(174,119,86,216),6);using(var shine=new SolidBrush(Color.FromArgb(204,250,242,255)))g.FillRectangle(shine,x+3,14,Math.Max(2,laneWidth-6),58);}
   else Theme.Box(g,lane,Color.FromArgb(i%2==0?86:102,189,169,230),6);
   using(var line=new Pen(Color.FromArgb(pressed[i]?245:105,231,215,255),pressed[i]?2f:1f))g.DrawLine(line,x+3,78,x+laneWidth-3,78);
  }
  g.Restore(savedScreen);
  var savedKeyboard=g.Save();using(var matrix=PlaneTransform(layout.KeyboardPlane,72,24))g.MultiplyTransform(matrix,MatrixOrder.Prepend);Theme.Box(g,new RectangleF(0,0,72,24),Color.FromArgb(112,84,151),3);for(int i=0;i<keys.Length;i++)Theme.Box(g,layout.PhysicalKeycaps[i],pressed[i]?Color.FromArgb(237,208,255):Color.FromArgb(239,227,252),1.5f);g.Restore(savedKeyboard);
  var character=layout.Character;if(style=="anime")character.Offset(0,(int)Math.Round(character.Height*12/156f));int poseSize=character.Width,poseX=character.X,poseY=character.Y;
  int handMask=0;for(int i=0;i<keys.Length;i++)if(pressed[i])handMask|=keys[i]==(int)Keys.Space?3:i<keys.Length/2?1:2;
  if(left)Art.Draw(g,new Rectangle(poseX,poseY,poseSize,poseSize),style,"osu",tick,handMask);
  else{var saved=g.Save();g.TranslateTransform(poseX+poseSize,0);g.ScaleTransform(-1,1);Art.Draw(g,new Rectangle(0,poseY,poseSize,poseSize),style,"osu",tick,handMask);g.Restore(saved);}
  for(int i=0;i<keys.Length;i++){
   var box=layout.Keycaps[i];Theme.Box(g,new RectangleF(box.X,box.Y+3,box.Width,box.Height),Color.FromArgb(180,70,48,111),6);if(pressed[i])box.Y+=2;
   Theme.Box(g,box,pressed[i]?Theme.Purple:Color.FromArgb(251,244,237,255),6);
   using(var border=new Pen(pressed[i]?Color.FromArgb(225,199,255):Color.FromArgb(173,144,217),1.2f))using(var path=Theme.Round(box,6))g.DrawPath(border,path);
   string label=ManiaKeys.Label(keys[i]);using(var font=Theme.Font(keys[i]==(int)Keys.Space?8:label.Length>3?6:7,true))Theme.ControlText(g,label,font,Rectangle.Round(box),pressed[i]?Color.White:Theme.Ink,true);
  }
 }
 protected override void WndProc(ref Message m){if(m.Msg==0xff&&rawInput!=null)rawInput.Read(m.LParam);if(m.Msg==0x84&&lifted){m.Result=new IntPtr(1);return;}if(m.Msg==0x84&&Positioning&&GamePlacementBounds().Contains(new Point((short)((long)m.LParam&0xffff)-Left,(short)(((long)m.LParam>>16)&0xffff)-Top))){m.Result=new IntPtr(1);return;}if(m.Msg==0x84&&surface!=null){int x=(short)((long)m.LParam&0xffff)-Left,y=(short)(((long)m.LParam>>16)&0xffff)-Top;if(gameMode&&!gameDestination.HasValue&&(gameId=="cs2"?Cs2Art.Bounds(Width,PetTop,64*app.Settings.Scale).Contains(x,y)&&y>PetTop+64*app.Settings.Scale-40:ManiaSceneLayout.Create(Width,PetTop,64*app.Settings.Scale,app.Games.OsuMania.Corner,app.Games.OsuMania.CurrentKeys).ClickThrough(new Point(x,y)))){m.Result=new IntPtr(-1);return;}if(x>=0&&y>=0&&x<surface.Width&&y<surface.Height&&surface.GetPixel(x,y).A<16){m.Result=new IntPtr(-1);return;}}base.WndProc(ref m);}
 [StructLayout(LayoutKind.Sequential)]struct POINT{public int x,y;public POINT(int a,int b){x=a;y=b;}}
 [StructLayout(LayoutKind.Sequential)]struct SIZE{public int cx,cy;public SIZE(int a,int b){cx=a;cy=b;}}
 [StructLayout(LayoutKind.Sequential,Pack=1)]struct BLEND{public byte op,flags,alpha,format;}
 [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr h);[DllImport("user32.dll")]static extern int ReleaseDC(IntPtr h,IntPtr dc);
 [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);[DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);[DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr o);[DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr o);
 [DllImport("user32.dll")]static extern bool UpdateLayeredWindow(IntPtr h,IntPtr dest,ref POINT point,ref SIZE size,IntPtr src,ref POINT source,int key,ref BLEND blend,int flags);
 void SetBitmap(Bitmap bitmap){IntPtr dc=GetDC(IntPtr.Zero),mem=CreateCompatibleDC(dc),h=bitmap.GetHbitmap(Color.FromArgb(0)),old=SelectObject(mem,h);try{var pos=new POINT(Left,Top);var size=new SIZE(Width,Height);var source=new POINT(0,0);var blend=new BLEND {op=0,flags=0,alpha=255,format=1};UpdateLayeredWindow(Handle,dc,ref pos,ref size,mem,ref source,0,ref blend,2);}finally{SelectObject(mem,old);DeleteObject(h);DeleteDC(mem);ReleaseDC(IntPtr.Zero,dc);}}
}
public static class Screens {public static Rectangle OrderByDistance(this Screen[] screens,Point p){foreach(var s in screens)if(s.WorkingArea.Contains(p))return s.WorkingArea;var best=Screen.PrimaryScreen.WorkingArea;long distance=long.MaxValue;foreach(var s in screens){long dx=(long)s.WorkingArea.Left+s.WorkingArea.Width/2-p.X,dy=(long)s.WorkingArea.Top+s.WorkingArea.Height/2-p.Y;long d=dx*dx+dy*dy;if(d<distance){distance=d;best=s.WorkingArea;}}return best;}}
}
