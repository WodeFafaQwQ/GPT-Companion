using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DragonCompanion {
// Add future games by registering another IGameProfile; the companion and
// dashboard depend on this interface rather than osu-specific process code.
public interface IGameProfile {
 string Id {get;}
 string Name {get;}
 string IconPath {get;}
 bool Enabled {get;set;}
 bool Running {get;}
 bool Foreground {get;}
 System.Drawing.Rectangle ScreenBounds {get;}
 string Corner {get;set;}
 bool FixedPosition {get;set;}
 Point SavedPosition {get;set;}
 void Poll();
}
public sealed class GameCenter {
 readonly List<IGameProfile> profiles=new List<IGameProfile>();
 readonly Settings settings;
 public readonly OsuManiaProfile OsuMania;
 public readonly Cs2Profile Cs2;
 public GameCenter(Settings settings){this.settings=settings;OsuMania=new OsuManiaProfile(settings);Cs2=new Cs2Profile(settings);profiles.Add(OsuMania);profiles.Add(Cs2);}
 public IEnumerable<IGameProfile> Profiles {get{return profiles;}}
 public IGameProfile Active {get{return profiles.FirstOrDefault(p=>p.Enabled&&p.Running&&p.Foreground)??profiles.FirstOrDefault(p=>p.Enabled&&p.Running);}}
 public IGameProfile Find(string id){return profiles.FirstOrDefault(p=>p.Id==id);}
 public string Selection {get{return settings.GameModeSelection=="off"||profiles.Any(p=>p.Id==settings.GameModeSelection)?settings.GameModeSelection:"auto";}set{settings.GameModeSelection=value=="off"||profiles.Any(p=>p.Id==value)?value:"auto";var chosen=profiles.FirstOrDefault(p=>p.Id==settings.GameModeSelection);if(chosen!=null)chosen.Enabled=true;}}
 // A remembered game choice is a preference, not a lock that suppresses every
 // other enabled game. Foreground games win; a stopped preference falls back
 // to another running game. Only the explicit off choice disables all modes.
 public static IGameProfile Resolve(IEnumerable<IGameProfile> profiles,string selection){if(selection=="off")return null;var running=profiles.Where(p=>p.Enabled&&p.Running).ToArray();return running.FirstOrDefault(p=>p.Foreground)??running.FirstOrDefault(p=>p.Id==selection)??running.FirstOrDefault();}
 public IGameProfile Selected {get{return Resolve(profiles,Selection);}}
 public void Poll(){foreach(var profile in profiles)profile.Poll();}
}
public static class ManiaKeys {
 public static int NormalizeMode(int mode){return mode>=4&&mode<=8?mode:4;}
 public static int[] Defaults(int mode){switch(NormalizeMode(mode)){
  case 5:return new[]{(int)Keys.D,(int)Keys.F,(int)Keys.Space,(int)Keys.J,(int)Keys.K};
  case 6:return new[]{(int)Keys.S,(int)Keys.D,(int)Keys.F,(int)Keys.J,(int)Keys.K,(int)Keys.L};
  case 7:return new[]{(int)Keys.S,(int)Keys.D,(int)Keys.F,(int)Keys.Space,(int)Keys.J,(int)Keys.K,(int)Keys.L};
  case 8:return new[]{(int)Keys.A,(int)Keys.S,(int)Keys.D,(int)Keys.F,(int)Keys.J,(int)Keys.K,(int)Keys.L,(int)Keys.OemSemicolon};
  default:return new[]{(int)Keys.D,(int)Keys.F,(int)Keys.J,(int)Keys.K};
 }}
 public static bool Valid(int[] keys,int mode){return keys!=null&&keys.Length==NormalizeMode(mode)&&keys.All(k=>Bindable((Keys)k))&&keys.Distinct().Count()==keys.Length;}
 public static bool Bindable(Keys raw){var key=raw&Keys.KeyCode;return raw==key&&key!=Keys.None&&key!=Keys.ProcessKey&&key!=Keys.Packet&&key!=Keys.ShiftKey&&key!=Keys.ControlKey&&key!=Keys.Menu&&key!=Keys.LWin&&key!=Keys.RWin&&key!=Keys.LButton&&key!=Keys.RButton&&key!=Keys.MButton&&key!=Keys.Escape;}
 public static string Label(int value){var key=(Keys)value;if(key>=Keys.D0&&key<=Keys.D9)return ((int)key-(int)Keys.D0).ToString();if(key>=Keys.NumPad0&&key<=Keys.NumPad9)return "Num"+((int)key-(int)Keys.NumPad0);switch(key){case Keys.Space:return "Space";case Keys.Left:return "←";case Keys.Right:return "→";case Keys.Up:return "↑";case Keys.Down:return "↓";case Keys.Oemcomma:return ",";case Keys.OemPeriod:return ".";case Keys.OemMinus:return "-";case Keys.Oemplus:return "=";case Keys.OemSemicolon:return ";";case Keys.Tab:return "Tab";case Keys.Enter:return "Enter";default:return key.ToString();}}
 }
// The lane monitor and key display are separate surfaces. Keep original key
// indices so moving Space to its own row never changes its screen lane.
public sealed class ManiaSceneLayout {
 public Rectangle Character;public RectangleF Monitor,FurnitureBounds,Cushion;public PointF[] ScreenPlane,DeskPlane,DeskFeet,KeyboardPlane;public RectangleF[] Keycaps,PhysicalKeycaps;public int Bottom;public bool FacesRight;
 public static int ScreenSlot(int lane,int count,bool facesRight){return facesRight?lane:count-1-lane;}
 public static RectangleF[] PhysicalKeys(int[] keys,bool facesRight){var result=new RectangleF[keys.Length];var upper=Enumerable.Range(0,keys.Length).Where(i=>keys[i]!=(int)Keys.Space).ToArray();bool hasSpace=upper.Length<keys.Length;float tile=(62-Math.Max(0,upper.Length-2)*2-5)/(float)upper.Length,x=5;
  for(int n=0;n<upper.Length;n++){if(n>0)x+=n==upper.Length/2?5:2;result[upper[n]]=new RectangleF(x,3,tile,hasSpace?12:18);x+=tile;}
  for(int i=0;i<keys.Length;i++){if(keys[i]==(int)Keys.Space)result[i]=new RectangleF(19,18,34,4);if(!facesRight){var r=result[i];r.X=72-r.Right;result[i]=r;}}return result;
 }
 static RectangleF Bounds(PointF[] points){float x=points.Min(p=>p.X),y=points.Min(p=>p.Y);return new RectangleF(x,y,points.Max(p=>p.X)-x,points.Max(p=>p.Y)-y);}
 public static ManiaSceneLayout Create(int width,int petTop,int size,string corner,int[] keys){
  bool left=corner=="left";int poseSize=Math.Min(180,Math.Max(156,size+28)),poseX=left?17:width-poseSize-17;
  int poseY=petTop+size-poseSize+8;float factor=poseSize/156f;
  Func<float,float,PointF> point=(sx,sy)=>new PointF(left?17+sx*factor:width-17-sx*factor,poseY+sy*factor);
  // The desk's long axis points from GPT toward the monitor. The monitor and
  // keyboard span its cross axis, so their fronts point back toward GPT.
  var result=new ManiaSceneLayout{FacesRight=left,Character=new Rectangle(poseX,poseY,poseSize,poseSize),Keycaps=new RectangleF[keys.Length],PhysicalKeycaps=PhysicalKeys(keys,left),ScreenPlane=new[]{point(163,-10),point(215,20),point(215,103),point(163,73)},DeskPlane=new[]{point(111,103),point(179,81),point(235,113),point(167,135)},KeyboardPlane=new[]{point(120,99),point(158,121),point(170,117),point(132,95)}};
  // Equal projected leg lengths keep the near edge from looking stunted.
  // Their feet follow the same depth slope as the tabletop, rather than a flat Y.
  float legLength=result.Character.Bottom-3-result.DeskPlane[2].Y;
  result.DeskFeet=new[]{new PointF(result.DeskPlane[2].X,result.DeskPlane[2].Y+legLength),new PointF(result.DeskPlane[3].X,result.DeskPlane[3].Y+legLength)};
  result.Monitor=Bounds(result.ScreenPlane);result.FurnitureBounds=RectangleF.Union(RectangleF.Union(result.Monitor,Bounds(result.DeskPlane)),Bounds(result.DeskFeet));
  result.FurnitureBounds.Inflate(3,3);
  result.Character.Offset((int)Math.Round((left?12:-12)*factor),(int)Math.Round(8*factor));
  result.Cushion=new RectangleF(result.Character.Left+poseSize*(left?.16f:.12f),result.Character.Bottom-poseSize*.16f,poseSize*.72f,poseSize*.21f);
  var upper=Enumerable.Range(0,keys.Length).Where(i=>keys[i]!=(int)Keys.Space).ToArray();int tile=upper.Length<=4?28:upper.Length<=6?24:21,gap=4,centerGap=16,split=upper.Length/2;
  float total=upper.Length*tile+Math.Max(0,upper.Length-2)*gap+(upper.Length>1?centerGap:0),center=Math.Max(total/2+6,Math.Min(width-total/2-6,poseX+poseSize/2f)),x=center-total/2,y=Math.Max(petTop+size+14,result.DeskFeet.Max(p=>p.Y)+10);
  for(int n=0;n<upper.Length;n++){if(n>0)x+=n==split?centerGap:gap;result.Keycaps[upper[n]]=new RectangleF(x,y,tile,tile);x+=tile;}
  for(int i=0;i<keys.Length;i++)if(keys[i]==(int)Keys.Space)result.Keycaps[i]=new RectangleF(center-tile*1.5f,y+tile+6,tile*3,tile);
  result.Bottom=(int)Math.Ceiling(result.Keycaps.Max(r=>r.Bottom));return result;
 }

 public bool ClickThrough(Point p){return FurnitureBounds.Contains(p)||Keycaps.Any(r=>r.Contains(p));}
}
public sealed class OsuManiaProfile:IGameProfile {
 readonly Settings settings;readonly HashSet<int> processIds=new HashSet<int>();IntPtr window;
 public OsuManiaProfile(Settings settings){this.settings=settings;}
 public string Id {get{return "osu-mania";}}
 public string Name {get{return "osu!mania";}}
 public bool Enabled {get{return settings.OsuManiaEnabled;}set{settings.OsuManiaEnabled=value;}}
 public bool Running {get{return processIds.Count>0;}}
 public bool Foreground {get{if(!Running)return false;uint pid;GetWindowThreadProcessId(GetForegroundWindow(),out pid);return processIds.Contains((int)pid);}}
 public System.Drawing.Rectangle ScreenBounds {get{return window!=IntPtr.Zero?Screen.FromHandle(window).WorkingArea:Screen.PrimaryScreen.WorkingArea;}}
 public int Mode {get{return ManiaKeys.NormalizeMode(settings.OsuManiaKeys);}set{settings.OsuManiaKeys=ManiaKeys.NormalizeMode(value);}}
 public string ExePath {get{return settings.OsuManiaExePath??"";}set{settings.OsuManiaExePath=value??"";}}
 public static string FindInstalledLazer(string localAppData){foreach(string relative in new[]{"osulazer\\current\\osu!.exe","osulazer\\osu!.exe"}){var path=Path.Combine(localAppData,relative);if(File.Exists(path))return path;}return "";}
 public string EffectiveExePath {get{return string.IsNullOrWhiteSpace(ExePath)?FindInstalledLazer(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)):ExePath;}}
 public string IconPath {get{return EffectiveExePath;}}
 public string ExecutableLabel {get{var path=EffectiveExePath;return string.IsNullOrEmpty(path)?"游戏程序：自动识别常见 osu! 进程":string.IsNullOrWhiteSpace(ExePath)?"自动定位 lazer："+path:"游戏程序："+path;}}
 public string Corner {get{return settings.OsuManiaCorner=="left"?"left":"right";}set{settings.OsuManiaCorner=value=="left"?"left":"right";}}
 public bool FixedPosition {get{return settings.OsuManiaFixedPosition&&settings.OsuManiaFixedX!=int.MinValue&&settings.OsuManiaFixedY!=int.MinValue;}set{settings.OsuManiaFixedPosition=value;}}
 public Point SavedPosition {get{return new Point(settings.OsuManiaFixedX,settings.OsuManiaFixedY);}set{settings.OsuManiaFixedX=value.X;settings.OsuManiaFixedY=value.Y;settings.OsuManiaFixedPosition=true;}}
 public int[] KeysFor(int mode){mode=ManiaKeys.NormalizeMode(mode);var keys=mode==4?settings.OsuMania4:mode==5?settings.OsuMania5:mode==6?settings.OsuMania6:mode==7?settings.OsuMania7:settings.OsuMania8;return ManiaKeys.Valid(keys,mode)?(int[])keys.Clone():ManiaKeys.Defaults(mode);}
 public int[] CurrentKeys {get{return KeysFor(Mode);}}
 public void SetKeys(int mode,int[] keys){if(!ManiaKeys.Valid(keys,mode))throw new ArgumentException("按键必须各不相同，且数量与定轨模式一致。");var copy=(int[])keys.Clone();switch(ManiaKeys.NormalizeMode(mode)){case 5:settings.OsuMania5=copy;break;case 6:settings.OsuMania6=copy;break;case 7:settings.OsuMania7=copy;break;case 8:settings.OsuMania8=copy;break;default:settings.OsuMania4=copy;break;}}
 public bool[] Pressed(){var keys=CurrentKeys;var pressed=new bool[keys.Length];if(!Enabled||!Foreground)return pressed;for(int i=0;i<keys.Length;i++)pressed[i]=(GetAsyncKeyState(keys[i])&0x8000)!=0;return pressed;}
 public static bool IsDefaultProcess(string processName){return new[]{"osu!","osu","osu!lazer","osu!.lazer"}.Any(n=>n.Equals(processName,StringComparison.OrdinalIgnoreCase));}
 public static bool IsSelectedProcess(string selectedPath,string processName,string actualPath){if(string.IsNullOrWhiteSpace(selectedPath))return IsDefaultProcess(processName);try{return Path.GetFullPath(selectedPath).Equals(Path.GetFullPath(actualPath),StringComparison.OrdinalIgnoreCase);}catch{return false;}}
 public void Poll(){processIds.Clear();window=IntPtr.Zero;if(!Enabled)return;string selected=EffectiveExePath;var names=string.IsNullOrWhiteSpace(selected)?new[]{"osu!","osu","osu!lazer","osu!.lazer"}:new[]{Path.GetFileNameWithoutExtension(selected)};
  foreach(string name in names)foreach(var process in Process.GetProcessesByName(name))try{
   string actual="";if(!string.IsNullOrWhiteSpace(selected))actual=process.MainModule.FileName;
   if(!IsSelectedProcess(selected,process.ProcessName,actual))continue;
   processIds.Add(process.Id);
   if(window==IntPtr.Zero&&process.MainWindowHandle!=IntPtr.Zero)window=process.MainWindowHandle;
  }catch{}finally{process.Dispose();}
 }
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint processId);
 [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
}
}
