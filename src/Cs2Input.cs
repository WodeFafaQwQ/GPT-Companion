using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DragonCompanion {
public sealed class Cs2Profile:IGameProfile {
 readonly Settings settings;readonly HashSet<int> ids=new HashSet<int>();IntPtr window;
 public Cs2Profile(Settings value){settings=value;}
 public string Id {get{return "cs2";}} public string Name {get{return "Counter-Strike 2";}}
 public bool Enabled {get{return settings.Cs2Enabled;}set{settings.Cs2Enabled=value;}}
 public bool Running {get{return ids.Count!=0;}}
 public bool Foreground {get{uint pid;GetWindowThreadProcessId(GetForegroundWindow(),out pid);return ids.Contains((int)pid);}}
 public Rectangle ScreenBounds {get{return window==IntPtr.Zero?Screen.PrimaryScreen.WorkingArea:Screen.FromHandle(window).WorkingArea;}}
 public string ExePath {get{return settings.Cs2ExePath??"";}set{settings.Cs2ExePath=value??"";}}
 string installed;
 public string EffectiveExePath {get{if(!string.IsNullOrWhiteSpace(ExePath))return ExePath;return installed??(installed=FindInstalled());}}
 public string IconPath {get{return EffectiveExePath;}}
 public string ExecutableLabel {get{return string.IsNullOrEmpty(EffectiveExePath)?"自动识别 cs2.exe；也可手动定位游戏程序。":EffectiveExePath;}}
 public string Corner {get{return settings.Cs2Corner=="left"?"left":"right";}set{settings.Cs2Corner=value=="left"?"left":"right";}}
 public bool FixedPosition {get{return settings.Cs2FixedPosition&&settings.Cs2FixedX!=int.MinValue&&settings.Cs2FixedY!=int.MinValue;}set{settings.Cs2FixedPosition=value;}}
 public Point SavedPosition {get{return new Point(settings.Cs2FixedX,settings.Cs2FixedY);}set{settings.Cs2FixedX=value.X;settings.Cs2FixedY=value.Y;settings.Cs2FixedPosition=true;}}
 public static bool IsSelectedProcess(string selected,string name,string actual){if(string.IsNullOrWhiteSpace(selected))return name.Equals("cs2",StringComparison.OrdinalIgnoreCase);try{return Path.GetFullPath(selected).Equals(Path.GetFullPath(actual),StringComparison.OrdinalIgnoreCase);}catch{return false;}}
 public void Poll(){ids.Clear();window=IntPtr.Zero;if(!Enabled)return;string selected=ExePath;foreach(var p in Process.GetProcessesByName(string.IsNullOrWhiteSpace(selected)?"cs2":Path.GetFileNameWithoutExtension(selected)))try{if(!IsSelectedProcess(selected,p.ProcessName,string.IsNullOrWhiteSpace(selected)?"":p.MainModule.FileName))continue;ids.Add(p.Id);if(window==IntPtr.Zero)window=p.MainWindowHandle;}catch{}finally{p.Dispose();}}
 public static string FindInstalled(){var roots=new List<string>();try{using(var key=Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam")){var path=key==null?null:key.GetValue("SteamPath") as string;if(!string.IsNullOrWhiteSpace(path))roots.Add(path);}}catch{}
  roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam"));foreach(var root in roots.ToArray())try{string vdf=Path.Combine(root,"steamapps","libraryfolders.vdf");if(File.Exists(vdf))foreach(Match m in Regex.Matches(File.ReadAllText(vdf),"\"path\"\\s+\"([^\"]+)\""))roots.Add(m.Groups[1].Value.Replace("\\\\","\\"));}catch{}
  foreach(var root in roots.Distinct(StringComparer.OrdinalIgnoreCase)){var candidate=Path.Combine(root,"steamapps","common","Counter-Strike Global Offensive","game","bin","win64","cs2.exe");if(File.Exists(candidate))return candidate;}return "";
 }
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
}

// Only transient state is kept. No keystroke history, files, or outgoing input.
public sealed class Cs2InputState {
 readonly List<int> held=new List<int>();
 public float Yaw,TargetYaw,MouseX,MouseY,TargetMouseX,TargetMouseY;
 public bool MouseLeft,MouseRight,MouseMiddle;
 public int LatestKey {get{return held.Count==0?0:held[held.Count-1];}}
 public string Bubble {get{return LatestKey==0?"":KeyLabel(LatestKey);}}
 public bool Down(int key){return held.Contains(key);}
 public static int Normalize(int vk,int scan,int flags){if(vk==(int)Keys.ShiftKey)return scan==0x36?(int)Keys.RShiftKey:(int)Keys.LShiftKey;if(vk==(int)Keys.ControlKey)return (flags&2)!=0?(int)Keys.RControlKey:(int)Keys.LControlKey;if(vk==(int)Keys.Menu)return (flags&2)!=0?(int)Keys.RMenu:(int)Keys.LMenu;return vk;}
 public static string KeyLabel(int key){switch((Keys)key){case Keys.LControlKey:case Keys.RControlKey:return "Ctrl";case Keys.LShiftKey:case Keys.RShiftKey:return "Shift";case Keys.LMenu:case Keys.RMenu:return "Alt";case Keys.LWin:case Keys.RWin:return "Win";case Keys.Capital:return "Caps";case Keys.Escape:return "Esc";case Keys.Oemtilde:return "~";default:return ManiaKeys.Label(key);}}
 public void Key(int key,bool down){if(key<=0||key>=255||key==(int)Keys.ProcessKey||key==(int)Keys.Packet)return;if(down){if(!held.Contains(key))held.Add(key);}else held.Remove(key);}
 public void Mouse(int dx,int dy,int buttons,bool relative){if(relative){TargetYaw=Limit(TargetYaw+dx*.0035f,1);TargetMouseX=Limit(TargetMouseX+dx*.035f,7);TargetMouseY=Limit(TargetMouseY+dy*.035f,4);}if((buttons&1)!=0)MouseLeft=true;if((buttons&2)!=0)MouseLeft=false;if((buttons&4)!=0)MouseRight=true;if((buttons&8)!=0)MouseRight=false;if((buttons&16)!=0)MouseMiddle=true;if((buttons&32)!=0)MouseMiddle=false;}
 static float Limit(float value,float limit){return Math.Max(-limit,Math.Min(limit,value));}
 public void Step(double dt){float blend=(float)(1-Math.Exp(-14*Math.Max(0,dt)));Yaw+=(TargetYaw-Yaw)*blend;MouseX+=(TargetMouseX-MouseX)*blend;MouseY+=(TargetMouseY-MouseY)*blend;float decay=(float)Math.Exp(-3*Math.Max(0,dt));TargetMouseX*=decay;TargetMouseY*=decay;}
 public void Clear(){held.Clear();MouseLeft=MouseRight=MouseMiddle=false;Yaw=TargetYaw=MouseX=MouseY=TargetMouseX=TargetMouseY=0;}
 // Byte parsing is shared by live WM_INPUT and synthetic packet verification.
 public bool Packet(byte[] packet,int header){if(packet==null||header<16||packet.Length<header)return false;int type=BitConverter.ToInt32(packet,0);if(type==0&&packet.Length>=header+24){Mouse(BitConverter.ToInt32(packet,header+12),BitConverter.ToInt32(packet,header+16),BitConverter.ToUInt16(packet,header+4),(BitConverter.ToUInt16(packet,header)&1)==0);return true;}if(type==1&&packet.Length>=header+16){int flags=BitConverter.ToUInt16(packet,header+2);Key(Normalize(BitConverter.ToUInt16(packet,header+6),BitConverter.ToUInt16(packet,header),flags),(flags&1)==0);return true;}return false;}
}

public sealed class Cs2RawInput:IDisposable {
 readonly Cs2InputState state;readonly Func<bool> permitted;readonly IntPtr buffer=Marshal.AllocHGlobal(256);bool registered,disposed;
 public string Error="";
 public bool Registered {get{return registered;}}
 public Cs2RawInput(Cs2InputState input,Func<bool> allow){state=input;permitted=allow;}
 [StructLayout(LayoutKind.Sequential)]struct Device {public ushort Page,Usage;public uint Flags;public IntPtr Target;}
 public void Enable(IntPtr handle,bool enabled){if(disposed||registered==enabled)return;var devices=new[]{new Device{Page=1,Usage=2,Flags=enabled?0x100u:1u,Target=enabled?handle:IntPtr.Zero},new Device{Page=1,Usage=6,Flags=enabled?0x100u:1u,Target=enabled?handle:IntPtr.Zero}};if(RegisterRawInputDevices(devices,2,(uint)Marshal.SizeOf(typeof(Device)))){registered=enabled;Error="";}else Error="无法读取键鼠输入（Windows 错误 "+Marshal.GetLastWin32Error()+"）";if(!enabled)state.Clear();}
 public void Read(IntPtr raw){if(!registered||!permitted()){state.Clear();return;}uint size=256;uint header=(uint)(IntPtr.Size==8?24:16);uint count=GetRawInputData(raw,0x10000003,buffer,ref size,header);if(count==uint.MaxValue||count>256||count<header)return;var bytes=new byte[count];Marshal.Copy(buffer,bytes,0,(int)count);state.Packet(bytes,(int)header);}
 public void Dispose(){if(disposed)return;Enable(IntPtr.Zero,false);disposed=true;Marshal.FreeHGlobal(buffer);}
 [DllImport("user32.dll",SetLastError=true)]static extern bool RegisterRawInputDevices(Device[] devices,uint count,uint size);
 [DllImport("user32.dll",SetLastError=true)]static extern uint GetRawInputData(IntPtr raw,uint command,IntPtr data,ref uint size,uint headerSize);
}
}
