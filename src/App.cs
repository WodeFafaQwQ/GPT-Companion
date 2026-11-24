using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DragonCompanion {
public sealed partial class Controller:ApplicationContext {
 public NativeCompanion Native=new NativeCompanion(); public GameCenter Games; public string NativeStatus="通过 Codex 左下角头像菜单切换原生虚拟宠物。";
 public Settings Settings; public QuotaSnapshot Quota; public LayeredPet Pet; public Dashboard Panel;
 public readonly bool Diagnostics;
 public ContextMenuStrip Menu=new CompanionMenu();public List<PetEvent> History;
 readonly Dictionary<string,string> tasks=new Dictionary<string,string>();readonly SessionWatcher watcher;readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer {Interval=2000};readonly NotifyIcon tray;readonly Random random=new Random();
 bool polling,refreshing,closing;DateTimeOffset nextQuota=DateTimeOffset.MinValue,temporaryUntil; string temporary="idle",temporaryPose="idle";
 public string Bubble="同时按住左右键揪起我，双击打开伙伴中心。",BubbleTitle="你好，我是GPT",LastThread="";
 public DateTimeOffset BubbleUntil=DateTimeOffset.UtcNow.AddSeconds(12), BadgeUntil=DateTimeOffset.MinValue;public CountdownClock Countdown=new CountdownClock();
 public string Connection {get{return Settings.Monitor?watcher.Status:"任务监听已暂停";}}
 public bool Busy {get{return tasks.Values.Any(x=>x=="working"||x=="waiting");}}
 public string State {get{if(DateTimeOffset.UtcNow<temporaryUntil)return temporary;if(tasks.Values.Any(x=>x=="waiting"))return "waiting";if(tasks.Values.Any(x=>x=="working"))return "working";return "idle";}}
 public string PoseState {get{return DateTimeOffset.UtcNow<temporaryUntil?temporaryPose:State;}}
 public string StatusLabel {get{switch(State){case "waiting":return "等你回复";case "working":return "正在工作";case "complete":return "任务完成";case "error":return "需要关注";case "sleep":return "休息一下";default:return "陪在你身边";}}}
 public Controller(bool background,bool diagnostics=false,string initialPage=null) {
  Diagnostics=diagnostics;
  Store.Init();Settings=Store.Load<Settings>("settings.json")??new Settings();Settings.Scale=Math.Max(2,Math.Min(5,Settings.Scale));Settings.RefreshMinutes=Math.Max(1,Math.Min(60,Settings.RefreshMinutes));if(Settings.Style!="anime")Settings.Style="pixel";Games=new GameCenter(Settings);Schedules=Store.LoadSchedules();Schedules.Normalize();RestorePendingPlanReviews(DateTime.Now);
  Quota=Store.Load<QuotaSnapshot>("quota.json")??new QuotaSnapshot();History=Store.Load<List<PetEvent>>("history.json")??new List<PetEvent>();watcher=new SessionWatcher(Store.CodexHome);
  BuildMenu();Pet=new LayeredPet(this);MainForm=Pet;
  Menu.Items.Add(new ToolStripSeparator());Menu.Items.Add("关闭 Codex 原生宠物",null,async(s,e)=>await HideNative());Menu.Items.Add("显示 Codex 原生宠物",null,async(s,e)=>await RestoreNative());
  tray=new NotifyIcon {Icon=Art.MakeIcon(),Visible=!diagnostics,Text="GPT · 桌面伙伴",ContextMenuStrip=Menu};tray.DoubleClick+=(s,e)=>ShowPanel();tray.BalloonTipClicked+=(s,e)=>OpenThread();
  timer.Tick+=async(s,e)=>await Tick();
  if(!diagnostics){Pet.Show();try{Startup.RefreshEnabledPath();}catch(Exception e){Store.Log("自启路径更新："+e.GetType().Name);}if(!background||initialPage!=null)ShowPanel(initialPage);timer.Start();var motionPreparation=StartupAssets.PrepareMotionLater(Settings.Style);if(Settings.HideNativeOnStart){var delayed=new System.Windows.Forms.Timer{Interval=6000};delayed.Tick+=async(s,e)=>{delayed.Stop();delayed.Dispose();await HideNative();};delayed.Start();}}
 }
 void BuildMenu(){Menu.Items.Add("打开伙伴中心",null,(s,e)=>ShowPanel());BuildGameMenu();BuildToolsMenu();Menu.Items.Add("显示 / 隐藏 GPT",null,(s,e)=>{if(Pet.Visible)Pet.Hide();else{Pet.Show();Pet.Clamp();}});Menu.Items.Add("刷新额度",null,async(s,e)=>await RefreshQuota());Menu.Items.Add(new ToolStripSeparator());Menu.Items.Add("像素风",null,(s,e)=>SetStyle("pixel"));Menu.Items.Add("二次元风",null,(s,e)=>SetStyle("anime"));Menu.Items.Add(new ToolStripSeparator());Menu.Items.Add("暂停 / 恢复提醒",null,(s,e)=>{Settings.Quiet=!Settings.Quiet;Save();Say("提醒设置",Settings.Quiet?"已进入免打扰，任务仍记录在伙伴中心。":"提醒已恢复。","wave",6);});var positionMenu=new ToolStripMenuItem("桌宠位置");positionMenu.DropDown=new CompanionMenu(true);positionMenu.DropDownItems.Add("自定义初始位置…",null,(s,e)=>{ShowPanel();Panel.Navigate("positions");Pet.BeginInitialPlacement();});positionMenu.DropDownItems.Add("恢复默认初始位置",null,(s,e)=>Pet.ResetNormalPosition());Menu.Items.Add(positionMenu);Menu.Items.Add("退出",null,(s,e)=>Quit());}
 void BuildGameMenu(){var parent=new ToolStripMenuItem("游戏模式");var submenu=new CompanionMenu(true);parent.DropDown=submenu;
  foreach(var profile in Games.Profiles){string id=profile.Id;submenu.Items.Add(new ToolStripMenuItem(profile.Name,null,(s,e)=>SelectGameMode(id)){Tag=id});}
  submenu.Items.Add(new ToolStripSeparator());submenu.Items.Add("进入游戏中心",null,(s,e)=>{ShowPanel();Panel.Navigate("games");});
  Action update=()=>{var selected=Games.Selected;foreach(ToolStripItem item in submenu.Items){var option=item as ToolStripMenuItem;if(option!=null&&option.Tag is string)option.Checked=selected!=null&&(string)option.Tag==selected.Id;}submenu.PrepareLayout();};parent.DropDownOpening+=(s,e)=>update();update();Menu.Items.Add(parent);
 }
 public void SelectGameMode(string selection){Pet.EndGamePlacement();Games.Selection=selection;Save();RefreshGameMode();}
 public void Save(){try{Store.Save("settings.json",Settings);}catch(Exception e){Store.Log("设置保存失败："+e.GetType().Name);}}
 public void SetStyle(string style){Settings.Style=style;Save();Pet.Render();if(Panel!=null&&!Panel.IsDisposed)Panel.Rebuild();Say("换装完成",style=="pixel"?"我是 GPT，像素风上线。今天也陪你一起做事！":"我是 GPT，换好衣服，继续陪你做事。","wave",6);}
 public void Say(string title,string text,string state,int seconds,string pose=null){BubbleTitle=title;Bubble=text;BubbleUntil=DateTimeOffset.UtcNow.AddSeconds(seconds);temporary=state;temporaryPose=pose??state;temporaryUntil=DateTimeOffset.UtcNow.AddSeconds(seconds);Pet.StopWalking();Pet.Render();}
 public void Pat(){BadgeUntil=DateTimeOffset.UtcNow.AddSeconds(5);Say("GPT 回复",Dialogue.Reply(Settings.DialogueStyle,"pat",random.Next(2)),"wave",6);}
 public async Task HideNative(){NativeStatus=await Native.Hide();if(closing)return;Say("原生桌宠",NativeStatus,"wave",14);if(Panel!=null&&!Panel.IsDisposed)Panel.UpdateLive();}
 public async Task RestoreNative(){NativeStatus=await Native.Restore();if(closing)return;Say("原生桌宠",NativeStatus,"wave",14);if(Panel!=null&&!Panel.IsDisposed)Panel.UpdateLive();}
 public void ShowCountdown(){using(var dialog=new CountdownDialog(this))dialog.ShowDialog(Panel!=null&&!Panel.IsDisposed&&Panel.Visible?(IWin32Window)Panel:Pet);}
 public void Chat(string text){if(string.IsNullOrWhiteSpace(text))return;if(text.Contains("额度")){var b=Quota.Main;Say("额度小助手",b!=null&&b.Primary!=null?b.Primary.Label+"剩余 "+b.Primary.Remaining.ToString("0")+"%。"+(Quota.Stale?"这是上次记录，请刷新确认。":"") : "还没有额度数据，到任务与额度页登录或刷新吧。","wave",12);}else if(text.Contains("你好")||text.Contains("在吗"))Say("GPT 回复",Dialogue.Reply(Settings.DialogueStyle,"hello"),"wave",8);else if(text.Contains("谢谢"))Say("GPT 回复",Dialogue.Reply(Settings.DialogueStyle,"thanks"),"complete",8);else if(text.Contains("休息")||text.Contains("晚安"))Say("GPT 回复",Dialogue.Reply(Settings.DialogueStyle,"rest"),"sleep",40);else if(text.Contains("专注")||text.Contains("倒计时")||text.Contains("番茄"))ShowCountdown();else if(text.Contains("计划")){ShowPanel();Panel.Navigate("plans");}else if(text.Contains("日程")){ShowPanel("schedules");}else Say("GPT 回复",Dialogue.Reply(Settings.DialogueStyle,"other"),"wave",12);}
 public void ShowPanel(string targetPage=null){
  if(Panel==null||Panel.IsDisposed)Panel=new Dashboard(this,targetPage??"home");
  else if(targetPage!=null)Panel.Navigate(targetPage);
  Panel.ShowAndRefresh();
 }
 public void OpenThread(){if(PendingPlanReviews.Count>0&&History.FirstOrDefault()!=null&&History[0].Source=="计划复盘"){ShowPlanReview();return;}Guid id;if(Guid.TryParse(LastThread,out id)) {try{Process.Start(new ProcessStartInfo("codex://threads/"+id.ToString()){UseShellExecute=true});}catch{ShowPanel();}}else ShowPanel();}
 public async Task RefreshQuota(){if(refreshing||closing)return;refreshing=true;try {Quota=await CodexQuota.Read(Store.FindCodex(Settings.CodexPath));if(closing)return;Store.Save("quota.json",Quota);}catch(Exception e){Quota.Error=e.Message;Store.Log("额度查询："+e.GetType().Name);}finally{refreshing=false;nextQuota=DateTimeOffset.UtcNow.AddMinutes(Settings.RefreshMinutes);if(!closing&&Panel!=null&&!Panel.IsDisposed)Panel.UpdateLive();}}
 public bool Refreshing {get{return refreshing;}}
 public void BeginGamePlacement(string id="osu-mania"){Pet.BeginGamePlacement(id);if(Panel!=null&&!Panel.IsDisposed)Panel.UpdateLive();}
 public void ResetGamePosition(string corner=null,string id="osu-mania"){var profile=Games.Find(id);if(profile==null)return;if(corner!=null)profile.Corner=corner;profile.FixedPosition=false;Pet.EndGamePlacement();Save();RefreshGameMode();Pet.SetGameCorner();if(Panel!=null&&!Panel.IsDisposed)Panel.UpdateLive();}
 public void RefreshGameMode(){if(Pet.Positioning){if(Panel!=null&&!Panel.IsDisposed)Panel.UpdateLive();return;}bool was=Pet.GameMode;Games.Poll();var active=Games.Selected;if(active!=null&&(!was||Pet.GameId!=active.Id))Pet.EnterGame(active.ScreenBounds,active.Id);else if(active==null&&was)Pet.ExitGame();else if(active!=null)Pet.UpdateGameScreen(active.ScreenBounds);if(Panel!=null&&!Panel.IsDisposed)Panel.UpdateLive();}
 async Task Tick(){if(polling||closing)return;polling=true;try{var showSignal=Path.Combine(Store.Root,"show.signal");if(File.Exists(showSignal)){var requested=File.ReadAllText(showSignal);File.Delete(showSignal);ShowPanel(requested=="plans"?"plans":null);}RefreshGameMode();var events=await Task.Run(()=>{var list=watcher.Poll();if(!Settings.Monitor)list.Clear();list.AddRange(Inbox.Drain());return list;});if(closing)return;foreach(var ev in events)Receive(ev);PollSchedules(DateTime.Now);PollPlanReviews(DateTime.Now);if(Countdown.ConsumeExpiration()){Receive(new PetEvent {Kind="complete",Message="番茄钟结束！休息一下，再继续下一件事吧。",Source="番茄钟"});}if(Settings.QuotaEnabled&&DateTimeOffset.UtcNow>=nextQuota&&!refreshing){var unused=RefreshQuota();}if(Panel!=null&&!Panel.IsDisposed)Panel.UpdateLive();}catch(Exception e){Store.Log("事件监听："+e.GetType().Name);}finally{polling=false;}}
 public void Receive(PetEvent e){if(closing)return;if(e.Kind=="resumed"){if(tasks.ContainsKey(e.ThreadId))tasks[e.ThreadId]="working";return;}if(!string.IsNullOrEmpty(e.ThreadId)){if(e.Kind=="complete"||e.Kind=="error"||e.Kind=="interrupted")tasks.Remove(e.ThreadId);else tasks[e.ThreadId]=e.Kind;}History.Insert(0,e);if(History.Count>100)History.RemoveRange(100,History.Count-100);Store.Save("history.json",History);LastThread=e.ThreadId??"";if(Settings.Quiet)return;string title=e.Kind=="reminder"?"日程提醒":e.Source=="番茄钟"?"番茄钟结束":e.Kind=="complete"?"任务完成":e.Kind=="waiting"?"需要你的回复":e.Kind=="error"?"任务出错":e.Kind=="interrupted"?"任务中断":"开始工作";Say(title,e.Message,e.Kind,e.Kind=="waiting"?60:14,ActivityArt.ForEvent(e.Kind,e.Source));if(e.Kind!="working"){tray.BalloonTipTitle="GPT · "+title;tray.BalloonTipText=e.Message.Length>240?e.Message.Substring(0,237)+"…":e.Message;tray.ShowBalloonTip(8000);if(Settings.Sound)SystemSounds.Asterisk.Play();}}
 public void Quit(){if(closing)return;if(pendingEarlyCompletions>0){quitAfterEarlyCompletions=true;return;}closing=true;StopPlanJobs();timer.Stop();timer.Dispose();var pos=Pet.NormalLocation;Settings.X=pos.X;Settings.Y=pos.Y;Save();if(Panel!=null)Panel.Dispose();tray.Visible=false;tray.Dispose();Menu.Dispose();Pet.Close();ExitThread();}
}
static class Program {
 [DllImport("user32.dll")]static extern bool SetProcessDPIAware();
 static void InitializeUi(){SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);}
 [STAThread]static int Main(string[] args){
  var startupClock=Stopwatch.StartNew();
  if(args.Length>=2&&args[0]=="--self-test"){InitializeUi();StartupAssets.LoadAll();return SelfTests.Run(args[1]);}
  if(args.Length>=2&&args[0]=="--startup-probe"){InitializeUi();return StartupDiagnostics.Run(args[1],args.Contains("--eager-assets"),args.Contains("--anime")?"anime":"pixel",startupClock);}
  if(args.Length>=2&&args[0]=="--quota-probe"){Store.Root=args[1];Store.Init();try{var q=CodexQuota.Read(Store.FindCodex("")).GetAwaiter().GetResult();Store.Save("quota-probe.json",q);return 0;}catch(Exception e){File.WriteAllText(Path.Combine(Store.Root,"quota-error.txt"),e.Message);return 1;}}
  if(args.Length>=3&&args[0]=="--notify"){Store.Init();try{Inbox.Send(args[1],args[2],args.Length>=5&&args[3]=="--thread"?args[4]:"");return 0;}catch(Exception e){MessageBox.Show(e.Message+"\n\n"+Theme.Credit,"通知失败");return 2;}}
  Store.Init();if(args.Contains("--compact")){var config=Store.Load<Settings>("settings.json")??new Settings();config.Scale=2;Store.Save("settings.json",config);}bool first;using(var mutex=new Mutex(true,"Local\\DragonCompanion."+Environment.UserName,out first)){
   if(!first){File.WriteAllText(Path.Combine(Store.Root,"show.signal"),args.Contains("--plans")?"plans":"show");return 0;}
   InitializeUi();
   Application.ThreadException+=(s,e)=>{Store.Log(e.Exception.ToString());MessageBox.Show("GPT 桌宠遇到一个问题，详情已写入本地日志。\n"+e.Exception.Message+"\n\n"+Theme.Credit,"GPT");};
   try{Application.Run(new Controller(args.Contains("--background"),initialPage:args.Contains("--plans")?"plans":null));return 0;}catch(Exception e){Store.Log(e.ToString());MessageBox.Show("启动失败："+e.Message+"\n\n"+Theme.Credit,"GPT");return 1;}
  }
 }
}
}
