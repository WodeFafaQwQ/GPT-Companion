using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace DragonCompanion {
// Uses Codex's own profile-menu command. No process is stopped and no Codex configuration file is written.
public sealed class NativeCompanion {
 readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);
 public static bool IsTrustedCodexPath(string exe){
  if(string.IsNullOrEmpty(exe))return false;
  string path=exe.Replace('/','\\'),name=Path.GetFileName(path);
  bool package=path.IndexOf("\\WindowsApps\\OpenAI.Codex_",StringComparison.OrdinalIgnoreCase)>=0||path.IndexOf("\\WindowsApps\\OpenAI.ChatGPT-Desktop_",StringComparison.OrdinalIgnoreCase)>=0||path.IndexOf("\\OpenAI\\Codex\\",StringComparison.OrdinalIgnoreCase)>=0;
  return package&&(name.Equals("ChatGPT.exe",StringComparison.OrdinalIgnoreCase)||name.Equals("Codex.exe",StringComparison.OrdinalIgnoreCase));
 }
 // A shortcut suffix may follow the command, e.g. "显示虚拟宠物 Alt+Win+P".
 public static bool? MenuOffersShow(string name){
  if(string.IsNullOrEmpty(name))return null;
  string label=name.Trim();
  if(label.StartsWith("显示虚拟宠物",StringComparison.Ordinal)||label.StartsWith("Show virtual pet",StringComparison.OrdinalIgnoreCase)||label.StartsWith("Show pet",StringComparison.OrdinalIgnoreCase))return true;
  if(label.StartsWith("隐藏虚拟宠物",StringComparison.Ordinal)||label.StartsWith("Hide virtual pet",StringComparison.OrdinalIgnoreCase)||label.StartsWith("Hide pet",StringComparison.OrdinalIgnoreCase))return false;
  return null;
 }
 public bool? SavedState(){
  try{
   var file=Path.Combine(Store.CodexHome,".codex-global-state.json");
   var value=Json.Get(Json.Read(File.ReadAllText(file)),"electron-avatar-overlay-open");
   return value is bool?(bool?)((bool)value):null;
  }catch{return null;}
 }
 public string SavedStateLabel(){var state=SavedState();return state.HasValue?"Codex 原生虚拟宠物："+(state.Value?"已显示":"已关闭"):"Codex 原生虚拟宠物：状态未知";}
 public Task<string> Hide(){return SetVisible(false);}
 public Task<string> Restore(){return SetVisible(true);}
 async Task<string> SetVisible(bool desired){
  await gate.WaitAsync();
  try{return await Task.Run(()=>ChangeOnWorker(desired));}
  finally{gate.Release();}
 }
 string ChangeOnWorker(bool desired){
  IntPtr window=FindMainWindow();
  if(window==IntPtr.Zero)return "未找到正在运行的 Codex 主窗口，无法切换原生宠物。";
  bool wasMinimized=IsIconic(window);
  try{
   if(wasMinimized){ShowWindowAsync(window,9);Thread.Sleep(550);}
   var root=AutomationElement.FromHandle(window);
   if(root==null)return "无法读取 Codex 主窗口的头像菜单。";
   bool? offered=ReadMenuRetry(root,false);
   if(!offered.HasValue)return SavedState()==desired?(desired?"Codex 原生虚拟宠物已经显示。":"Codex 原生虚拟宠物已经关闭。") : "Codex 头像菜单中没有可识别的虚拟宠物开关；未执行切换。";
   if(offered.Value!=desired)return desired?"Codex 原生虚拟宠物已经显示。":"Codex 原生虚拟宠物已经关闭。";
   Thread.Sleep(150);
   offered=ReadMenuRetry(root,true);
   if(!offered.HasValue)return "宠物菜单在切换前已变化，请重试。";
   if(offered.Value!=desired)return "宠物状态已被其他操作改变，请刷新后重试。";
   for(int i=0;i<8;i++){
    Thread.Sleep(250);
    bool? now=null;
    try{now=ReadMenu(root,false);}catch(InvalidOperationException){}
    if((now.HasValue&&now.Value!=desired)||SavedState()==desired){Thread.Sleep(350);return desired?"Codex 原生虚拟宠物已显示，开关状态已核对。":"Codex 原生虚拟宠物已关闭，开关状态已核对。";}
   }
   return "已调用 Codex 宠物菜单，但未能确认状态改变；请在 Codex 中检查。";
  }catch(Exception e){Store.Log("原生桌宠切换："+e.GetType().Name);return "无法操作 Codex 头像菜单（"+e.GetType().Name+"）。请确认 Codex 已启动且可交互。";}
  finally{if(wasMinimized&&IsWindow(window))ShowWindowAsync(window,6);}
 }
 static bool? ReadMenuRetry(AutomationElement root,bool invoke){for(int i=0;i<4;i++){try{var value=ReadMenu(root,invoke);if(value.HasValue)return value;}catch(InvalidOperationException){}Thread.Sleep(260);}return null;}
 static bool? ReadMenu(AutomationElement root,bool invoke){
  var avatar=FindProfile(root);
  if(avatar==null)return null;
  object expansion;
  if(!avatar.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,out expansion))return null;
  var pattern=(ExpandCollapsePattern)expansion;
  bool opened=pattern.Current.ExpandCollapseState==ExpandCollapseState.Collapsed;
  if(opened){pattern.Expand();Thread.Sleep(130);}
  bool clicked=false;
  try{
   var menu=root.FindFirst(TreeScope.Descendants,new AndCondition(
    new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Menu),
    new PropertyCondition(AutomationElement.NameProperty,avatar.Current.Name)));
   if(menu==null)return null;
   var items=menu.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.MenuItem));
   AutomationElement target=null;bool? offersShow=null;
   foreach(AutomationElement item in items){
    var match=MenuOffersShow(item.Current.Name);
    if(!match.HasValue)continue;
    if(target!=null)return null;
    target=item;offersShow=match;
   }
   if(target==null)return null;
   if(invoke){object action;if(!target.TryGetCurrentPattern(InvokePattern.Pattern,out action))return null;((InvokePattern)action).Invoke();clicked=true;}
   return offersShow;
  }finally{
   if(opened&&!clicked)try{var fresh=FindProfile(root);object p;if(fresh!=null&&fresh.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,out p))((ExpandCollapsePattern)p).Collapse();}catch{}
  }
 }
 static AutomationElement FindProfile(AutomationElement root){
  var elements=root.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Button));
  AutomationElement found=null;
  foreach(AutomationElement item in elements){
   string name=item.Current.Name;
   if(name!="打开个人资料菜单"&&!name.Equals("Open profile menu",StringComparison.OrdinalIgnoreCase))continue;
   if(found!=null)return null;
   found=item;
  }
  return found;
 }
 static IntPtr FindMainWindow(){
  IntPtr found=IntPtr.Zero;int count=0;
  EnumWindows((h,param)=>{try{
   if(!IsWindowVisible(h)||(WindowStyle(h)&0x00C00000)==0)return true;
   uint pid;GetWindowThreadProcessId(h,out pid);
   using(var p=Process.GetProcessById((int)pid)){
    if(!p.ProcessName.Equals("ChatGPT",StringComparison.OrdinalIgnoreCase)||!IsTrustedCodexPath(p.MainModule.FileName))return true;
    var name=new StringBuilder(64);GetClassName(h,name,name.Capacity);
    if(name.ToString()!="Chrome_WidgetWin_1")return true;
    count++;found=h;
   }
  }catch{}return true;},IntPtr.Zero);
  return count==1?found:IntPtr.Zero;
 }
 static long WindowStyle(IntPtr h){return IntPtr.Size==8?GetLong64(h,-16).ToInt64():GetLong32(h,-16);}
 delegate bool EnumCallback(IntPtr h,IntPtr param);
 [DllImport("user32.dll")]static extern bool EnumWindows(EnumCallback callback,IntPtr param);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetLong64(IntPtr h,int index);
 [DllImport("user32.dll",EntryPoint="GetWindowLongW")]static extern int GetLong32(IntPtr h,int index);
 [DllImport("user32.dll")]static extern bool IsIconic(IntPtr h);
 [DllImport("user32.dll")]static extern bool IsWindow(IntPtr h);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")]static extern bool ShowWindowAsync(IntPtr h,int command);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,StringBuilder name,int max);
}
}
