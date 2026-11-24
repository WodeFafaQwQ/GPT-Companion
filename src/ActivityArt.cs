using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;

namespace DragonCompanion {
public sealed class ActivityClip {
 public readonly string Key,Name,Group;public readonly int Row;public readonly int[] Holds;
 public ActivityClip(string key,string name,string group,int row,params int[] holds){Key=key;Name=name;Group=group;Row=row;Holds=holds;}
 public int Duration {get{return Holds.Sum();}}
 public int FrameAt(int milliseconds){int time=Math.Max(0,milliseconds)%Duration;for(int i=0;i<Holds.Length;i++){if(time<Holds[i])return i;time-=Holds[i];}return 0;}
}
public static class ActivityArt {
 static int[] Holds(int count,int milliseconds){return Enumerable.Repeat(milliseconds,count).ToArray();}
 public static readonly ActivityClip[] Clips={
  new ActivityClip("read","看书翻页","daily",0,2800,120,120,150,150,120,120,500),
  new ActivityClip("tea","喝茶","daily",2,PaintedActivities.Holds("tea")),
  new ActivityClip("rest","抱枕休息","daily",3,Holds(80,100)),
  new ActivityClip("working","专注工作","tasks",0,PaintedActivities.Holds("working")),
  new ActivityClip("waiting","等待回复","tasks",1,Holds(60,100)),
  new ActivityClip("complete","完成庆祝","tasks",2,Holds(64,50)),
  new ActivityClip("reminder","日程提醒","tasks",3,Holds(64,50))
 };
 static readonly Dictionary<string,Lazy<Bitmap[][]>> atlases=CreateCache();
 static Dictionary<string,Lazy<Bitmap[][]>> CreateCache(){var result=new Dictionary<string,Lazy<Bitmap[][]>>();foreach(string style in new[]{"pixel","anime"})foreach(string group in new[]{"daily","tasks"}){string resource=style+"-"+group+"-8";result.Add(resource,new Lazy<Bitmap[][]>(()=>Read(resource),true));}return result;}
 public static ActivityClip Find(string key){if(key=="look")key="read";return Clips.FirstOrDefault(c=>c.Key==key);}
 public static bool Has(string key){return Find(key)!=null;}
 public static int LoadedAssetCount {get{return atlases.Values.Count(a=>a.IsValueCreated);}}
 public static void Prepare(string style){ActivityModels.Prepare(style);foreach(string group in new[]{"daily","tasks"})Get(style,group);}
 public static void Load(){Prepare("pixel");Prepare("anime");}
 static Bitmap[][] Get(string style,string group){Lazy<Bitmap[][]> atlas;return atlases.TryGetValue(style+"-"+group+"-8",out atlas)?atlas.Value:null;}
 static Bitmap[][] Read(string resource){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(resource+".png")){if(stream==null)return null;using(var atlas=new Bitmap(stream)){var cuts=RowCuts(atlas);var rows=new Bitmap[4][];string group=resource.Contains("-daily-")?"daily":"tasks",style=resource.StartsWith("pixel-")?"pixel":"anime";for(int row=0;row<4;row++){var clip=Clips.FirstOrDefault(c=>c.Group==group&&c.Row==row);if(clip==null||ActivityModels.Has(clip.Key)){rows[row]=new Bitmap[0];continue;}if(clip.Key=="read"){rows[row]=new Bitmap[8];for(int column=0;column<8;column++){var cell=Rectangle.FromLTRB(column*atlas.Width/8,cuts[row],(column+1)*atlas.Width/8,cuts[row+1]);using(var part=atlas.Clone(cell,PixelFormat.Format32bppArgb))rows[row][column]=Art.IsolateCharacter(part,true);}Normalize(rows[row]);}else rows[row]=ActivitySprites.SplitRow(atlas,row);rows[row]=ActivityLayers.Stabilize(rows[row],clip.Key,style,clip.Holds.Length);}return rows;}}}
 // Generated sheets can reserve a shorter row for the seated pillow pose.
 // Use the three actual transparent gutters, rather than cutting off feet at
 // mathematical quarters. Ignore isolated fringe pixels when finding gutters.
 public static int[] RowCuts(Bitmap atlas){var cuts=new List<int>{0};var data=atlas.LockBits(new Rectangle(0,0,atlas.Width,atlas.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{var bytes=new byte[atlas.Width*4];int start=-1;for(int y=0;y<=atlas.Height;y++){int count=0;if(y<atlas.Height){System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),bytes,0,bytes.Length);for(int x=0;x<atlas.Width;x++)if(bytes[x*4+3]>=32)count++;}bool empty=y<atlas.Height&&count<Math.Max(8,atlas.Width/50);if(empty&&start<0)start=y;if(!empty&&start>=0){if(y-start>=3&&start>atlas.Height*.1&&y<atlas.Height*.95)cuts.Add((start+y)/2);start=-1;}}}finally{atlas.UnlockBits(data);}if(cuts.Count!=4)throw new InvalidOperationException("动作图集缺少三个清晰的行间隔");cuts.Add(atlas.Height);return cuts.ToArray();}
 // All eight frames share a crop, scale and baseline. Cropping each frame to
 // its own silhouette would make the character change size while turning pages.
 static void Normalize(Bitmap[] frames){int width=frames.Min(b=>b.Width),height=frames.Min(b=>b.Height);int left=width,top=height,right=0,bottom=0;foreach(var b in frames){var rect=OpaqueBounds(b);if(rect.IsEmpty)continue;left=Math.Min(left,rect.Left);top=Math.Min(top,rect.Top);right=Math.Max(right,rect.Right);bottom=Math.Max(bottom,rect.Bottom);}if(right<=left)return;var crop=Rectangle.FromLTRB(Math.Max(0,left-4),Math.Max(0,top-4),Math.Min(width,right+4),Math.Min(height,bottom+4));for(int i=0;i<frames.Length;i++){var old=frames[i];frames[i]=old.Clone(crop,PixelFormat.Format32bppArgb);old.Dispose();}}
 public static Rectangle OpaqueBounds(Bitmap b){var data=b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);int left=b.Width,top=b.Height,right=-1,bottom=-1;try{var bytes=new byte[b.Width*4];for(int y=0;y<b.Height;y++){System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),bytes,0,bytes.Length);for(int x=0;x<b.Width;x++)if(bytes[x*4+3]>=32){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}}}finally{b.UnlockBits(data);}return right<left?Rectangle.Empty:Rectangle.FromLTRB(left,top,right+1,bottom+1);}
 public static Bitmap Frame(string style,string key,int index){if(ActivityModels.Has(key))return ActivityModels.Frame(style,key,index);var clip=Find(key);if(clip==null)return null;var atlas=Get(style,clip.Group);return atlas==null?null:atlas[clip.Row][Math.Max(0,index)%atlas[clip.Row].Length];}
 public static bool Draw(Graphics graphics,Rectangle bounds,string style,string key,int tick){var clip=Find(key);if(clip==null)return false;long milliseconds=Math.Max(0,(long)tick)*50;if(ActivityModels.Draw(graphics,bounds,style,key,milliseconds))return true;var image=clip.Key=="read"?ReadingBook.ImageAt(style,milliseconds):Frame(style,key,clip.FrameAt((int)(milliseconds%clip.Duration)));if(image==null)return false;if(clip.Key=="complete")CelebrationFx.Draw(graphics,bounds,milliseconds);float scale=Math.Min((bounds.Width-16f)/image.Width,(bounds.Height-16f)/image.Height);var destination=new RectangleF(bounds.X+(bounds.Width-image.Width*scale)/2,bounds.Bottom-8-image.Height*scale,image.Width*scale,image.Height*scale);if(clip.Key=="complete")destination.Y-=CelebrationFx.Jump(milliseconds)*Math.Min(bounds.Width,bounds.Height)/192f;var saved=graphics.Save();graphics.InterpolationMode=style=="pixel"?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.Half;using(var attributes=new ImageAttributes()){attributes.SetWrapMode(WrapMode.TileFlipXY);if(clip.Key=="complete")graphics.DrawImage(image,destination,new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel);else graphics.DrawImage(image,Rectangle.Round(destination),0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);}graphics.Restore(saved);return true;}
 public static string ForEvent(string kind,string source){if(source=="番茄钟"&&kind=="complete")return "rest";return kind;}
}
public sealed class IdleActivities {
 readonly Random random;readonly List<string> bag=new List<string>();string previous="";int nextTick,endTick;bool eligible=true;
 public string Pose {get;private set;}public int StartedTick {get;private set;}
 public IdleActivities(int seed):this(new Random(seed)){}
 public IdleActivities():this(new Random()){}
 IdleActivities(Random source){random=source;Pose="idle";nextTick=random.Next(400,801);}
 int Gap(){return random.Next(600,1001);}
 public void Advance(int tick,bool allowed){if(!allowed){if(eligible||Pose!="idle")nextTick=tick+Gap();eligible=false;Pose="idle";return;}if(!eligible){eligible=true;nextTick=Math.Max(nextTick,tick+200);}if(Pose!="idle"&&tick>=endTick){Pose="idle";nextTick=tick+Gap();}if(Pose!="idle"||tick<nextTick)return;if(bag.Count==0)bag.AddRange(new[]{"read","tea","rest"});var choices=bag.Where(p=>p!=previous).ToArray();string chosen=choices[random.Next(choices.Length)];bag.Remove(chosen);Pose=previous=chosen;StartedTick=tick;int seconds=chosen=="read"?random.Next(22,33):chosen=="tea"?random.Next(7,11):random.Next(10,16);endTick=tick+seconds*20;}
}
public static class WanderMotion {
 public const int MinimumPauseSeconds=35,MaximumPauseExclusive=66,InitialPauseSeconds=40;
 public static Point Offset(Random random,bool flying){int x=random.Next(flying?260:220,flying?421:361)*(random.Next(2)==0?-1:1);int y=flying?random.Next(120,201)*(random.Next(2)==0?-1:1):0;return new Point(x,y);}
}
}
