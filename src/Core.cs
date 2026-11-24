using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Microsoft.Win32;
using System.Windows.Forms;

namespace DragonCompanion {
public static class Json {
 public static JavaScriptSerializer Serializer(){var s=new JavaScriptSerializer {MaxJsonLength=8*1024*1024};s.RegisterConverters(new[]{new OffsetConverter()});return s;}
 public static string Encode(object o) { return Serializer().Serialize(o); }
 public static Dictionary<string,object> Read(string s) { return new JavaScriptSerializer { MaxJsonLength=8*1024*1024 }.DeserializeObject(s) as Dictionary<string,object>; }
 public static Dictionary<string,object> Obj(Dictionary<string,object> d,string k) { object v; return d!=null && d.TryGetValue(k,out v) ? v as Dictionary<string,object> : null; }
 public static object Get(Dictionary<string,object> d,string k) { object v; return d!=null && d.TryGetValue(k,out v)?v:null; }
 public static string Str(Dictionary<string,object> d,string k) { var v=Get(d,k); return v==null?"":Convert.ToString(v); }
 public static double? Num(Dictionary<string,object> d,string k) { double v; return Get(d,k)!=null && double.TryParse(Str(d,k),System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture,out v)?(double?)v:null; }
}
public sealed class OffsetConverter:JavaScriptConverter {
 public override IEnumerable<Type> SupportedTypes {get{return new[]{typeof(DateTimeOffset)};}}
 public override IDictionary<string,object> Serialize(object obj,JavaScriptSerializer serializer){return new Dictionary<string,object>{{"iso",((DateTimeOffset)obj).ToString("o")}};}
 public override object Deserialize(IDictionary<string,object> d,Type t,JavaScriptSerializer serializer){return DateTimeOffset.Parse(Convert.ToString(d["iso"]));}
}
public sealed class Settings {
 public int Scale=2;
 public int AnimationMode=2;
 public int PanelWidth=0,PanelHeight=0;
 public bool PanelMaximized;
 public string DialogueStyle="tsundere";
 public int CountdownMinutes=25, CountdownSeconds=0;
 public int X=int.MinValue, Y=int.MinValue;
 public bool CustomInitialPosition;
 public int InitialX=int.MinValue,InitialY=int.MinValue;
 public bool Topmost=true, Sound=false, Wander=false, Quiet=false, Monitor=true, QuotaEnabled=true, HideNativeOnStart=false;
 public bool OsuManiaEnabled=true;
 public bool Cs2Enabled=true,Cs2FixedPosition;
 public string Cs2ExePath="",Cs2Corner="right";
 public int Cs2FixedX=int.MinValue,Cs2FixedY=int.MinValue;
 public string GameModeSelection="auto";
 public string OsuManiaExePath="";
 public string OsuManiaCorner="right";
 public bool OsuManiaFixedPosition;
 public int OsuManiaFixedX=int.MinValue,OsuManiaFixedY=int.MinValue;
 public int OsuManiaKeys=4;
 public int[] OsuMania4={ (int)Keys.D,(int)Keys.F,(int)Keys.J,(int)Keys.K };
 public int[] OsuMania5={ (int)Keys.D,(int)Keys.F,(int)Keys.Space,(int)Keys.J,(int)Keys.K };
 public int[] OsuMania6={ (int)Keys.S,(int)Keys.D,(int)Keys.F,(int)Keys.J,(int)Keys.K,(int)Keys.L };
 public int[] OsuMania7={ (int)Keys.S,(int)Keys.D,(int)Keys.F,(int)Keys.Space,(int)Keys.J,(int)Keys.K,(int)Keys.L };
 public int[] OsuMania8={ (int)Keys.A,(int)Keys.S,(int)Keys.D,(int)Keys.F,(int)Keys.J,(int)Keys.K,(int)Keys.L,(int)Keys.OemSemicolon };
 public int RefreshMinutes=5;
 public string CodexPath="", SkinPath="", Style="pixel";
}
public static class Store {
 public static string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DragonCompanion");
 public static string CodexHome { get { return Environment.GetEnvironmentVariable("CODEX_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex"); } }
 public static void Init() { Directory.CreateDirectory(Root); Directory.CreateDirectory(Path.Combine(Root,"inbox")); }
 public static T Load<T>(string name) where T:new() { try { return Json.Serializer().Deserialize<T>(File.ReadAllText(Path.Combine(Root,name),Encoding.UTF8)); } catch { return new T(); } }
 const string ScheduleName="schedules.json", ScheduleRecoveryName="schedules.recovery.json";
 static ScheduleBook ReadSchedule(string path,out string invalid){
  invalid=null;string raw;
  try{raw=File.ReadAllText(path,Encoding.UTF8);}catch(FileNotFoundException){return null;}
  try{
   var serializer=Json.Serializer();
   var doc=serializer.DeserializeObject(raw) as Dictionary<string,object>;
   if(doc==null||!(Json.Get(doc,"Items") is System.Collections.IList)||(doc.ContainsKey("Plans")&&!(doc["Plans"] is System.Collections.IList)))throw new FormatException();
   var book=serializer.ConvertToType<ScheduleBook>(doc);
   if(book.Items==null||book.Plans==null)throw new FormatException();
   return book;
  }catch(Exception e){invalid=e.GetType().Name;return null;}
 }
 internal static ScheduleBook LoadSchedules(){
  string primary=Path.Combine(Root,ScheduleName), recovery=Path.Combine(Root,ScheduleRecoveryName), invalid, invalidRecovery;
  var book=ReadSchedule(primary,out invalid);
  if(book!=null){
   if(!File.Exists(recovery))try{using(var seed=new StagedSave(recovery,book))seed.Commit();}catch(IOException e){Log("日程备份初始化失败："+e.GetType().Name);}catch(UnauthorizedAccessException e){Log("日程备份初始化失败："+e.GetType().Name);}
   return book;
  }
  book=ReadSchedule(recovery,out invalidRecovery);
  if(book==null){
   if(invalid==null&&invalidRecovery==null)return new ScheduleBook();
   throw new IOException("日程文件或恢复副本无法读取，原文件已保留，避免覆盖计划。");
  }
  string quarantined=invalid==null?null:Path.Combine(Root,"schedules.unreadable-"+Guid.NewGuid().ToString("N")+".json");
  using(var restored=new StagedSave(primary,book,false))restored.Commit(quarantined);
  Log(invalid==null?"已恢复缺失的日程与计划文件。":"已从恢复副本修复日程，原文件已单独保留："+invalid);
  return book;
 }
 internal sealed class StagedSave:IDisposable {
  readonly string file,temp,recovery,recoveryTemp;
  internal StagedSave(string target,object value,bool mirror=true){
   file=target;temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
   if(mirror&&(Path.GetFileName(file)==ScheduleName||Path.GetFileName(file)=="vocabulary.json")){recovery=Path.Combine(Path.GetDirectoryName(file),Path.GetFileNameWithoutExtension(file)+".recovery.json");recoveryTemp=temp+".recovery";}
   try{
    File.WriteAllText(temp,Json.Encode(value),new UTF8Encoding(false));
    if(recoveryTemp!=null)try{RetryRecovery(()=>File.Copy(temp,recoveryTemp,true));}catch(IOException e){Log("恢复副本准备失败："+e.GetType().Name);}catch(UnauthorizedAccessException e){Log("恢复副本准备失败："+e.GetType().Name);}
   }catch{Dispose();throw;}
  }
  static void Publish(string prepared,string target,string previous){if(File.Exists(target))File.Replace(prepared,target,previous,true);else File.Move(prepared,target);}
  static void RetryRecovery(Action publish){
   for(int attempt=0;;attempt++){
    try{publish();return;}
    catch(IOException){if(attempt>=4)throw;System.Threading.Thread.Sleep(10<<attempt);}
   }
  }
  internal void Commit(string previous=null){
   Publish(temp,file,previous);
   if(recoveryTemp!=null&&File.Exists(recoveryTemp)){
    try{RetryRecovery(()=>Publish(recoveryTemp,recovery,null));}catch(IOException e){Log("恢复副本更新失败："+e.GetType().Name);}catch(UnauthorizedAccessException e){Log("恢复副本更新失败："+e.GetType().Name);}
   }
  }
  public void Dispose(){try{File.Delete(temp);}catch{}if(recoveryTemp!=null)try{File.Delete(recoveryTemp);}catch{}}
 }
 internal static StagedSave PrepareSave(string name,object value){return new StagedSave(Path.Combine(Root,name),value);}
 public static void Save(string name,object value) {
  using(var save=PrepareSave(name,value)){
   for(int attempt=0;;attempt++){
    try{save.Commit();return;}catch(IOException){if(attempt>=4)throw;System.Threading.Thread.Sleep(30*(1<<attempt));}
   }
  }
 }
 public static void Log(string message) { try { var p=Path.Combine(Root,"diagnostics.log"); if(File.Exists(p)&&new FileInfo(p).Length>256000) File.WriteAllText(p,""); File.AppendAllText(p,DateTimeOffset.Now.ToString("u")+" "+message+Environment.NewLine); } catch {} }
 public static string FindCodex(string custom) {
  if(!string.IsNullOrWhiteSpace(custom)&&File.Exists(custom)&&Path.GetExtension(custom).Equals(".exe",StringComparison.OrdinalIgnoreCase)) return custom;
  foreach(var dir in (Environment.GetEnvironmentVariable("PATH")??"").Split(';')) { try { var f=Path.Combine(dir.Trim('"'),"codex.exe"); if(File.Exists(f)) return f; } catch {} }
  var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OpenAI","Codex","bin");
  if(Directory.Exists(root)) { var f=new DirectoryInfo(root).GetDirectories().OrderByDescending(x=>x.LastWriteTimeUtc).Select(x=>Path.Combine(x.FullName,"codex.exe")).FirstOrDefault(File.Exists); if(f!=null)return f; }
  return "";
 }
}
public static class Startup {
 const string Key="Software\\Microsoft\\Windows\\CurrentVersion\\Run", Name="DragonCompanion";
 public static bool Enabled { get { try {using(var k=Registry.CurrentUser.OpenSubKey(Key)) return k!=null && k.GetValue(Name)!=null;} catch(System.Security.SecurityException){return false;} catch(UnauthorizedAccessException){return false;} } }
 public static string CommandFor(string executable){return "\""+Path.GetFullPath(executable)+"\" --background";}
 public static void Set(bool on) { using(var k=Registry.CurrentUser.CreateSubKey(Key)) { if(on)k.SetValue(Name,CommandFor(Application.ExecutablePath)); else k.DeleteValue(Name,false); } }
 public static void RefreshEnabledPath(){using(var key=Registry.CurrentUser.OpenSubKey(Key,true)){if(key==null||key.GetValue(Name)==null)return;string command=CommandFor(Application.ExecutablePath);if(!string.Equals(Convert.ToString(key.GetValue(Name)),command,StringComparison.OrdinalIgnoreCase))key.SetValue(Name,command);}}
}
public sealed class QuotaWindow {
 public double UsedPercent; public int Minutes; public long ResetsAt;
 public double Remaining { get { return Math.Max(0,Math.Min(100,100-UsedPercent)); } }
 public string Label { get { return Minutes==10080?"每周额度":Minutes==300?"5 小时额度":Minutes>=1440?(Minutes/1440)+" 天额度":Minutes>=60?(Minutes/60)+" 小时额度":Minutes+" 分钟额度"; } }
 public string ResetText { get { try { return ResetsAt>0?DateTimeOffset.FromUnixTimeSeconds(ResetsAt).ToLocalTime().ToString("MM-dd HH:mm")+" 重置":"重置时间未提供"; } catch {return "重置时间未知";} } }
}
public sealed class QuotaBucket { public string Id, Name; public QuotaWindow Primary,Secondary; }
public sealed class QuotaSnapshot {
 public List<QuotaBucket> Buckets=new List<QuotaBucket>();
 public DateTimeOffset UpdatedAt; public string Source="", Error="";
 public bool Stale { get { return UpdatedAt==default(DateTimeOffset)||DateTimeOffset.UtcNow-UpdatedAt>TimeSpan.FromMinutes(12)||!string.IsNullOrEmpty(Error); } }
 public QuotaBucket Main { get { return Buckets.FirstOrDefault(x=>x.Id=="codex")??Buckets.FirstOrDefault(); } }
 public static QuotaWindow Window(Dictionary<string,object> d) {
  var used=Json.Num(d,"usedPercent")??Json.Num(d,"used_percent"); if(!used.HasValue||double.IsNaN(used.Value)||double.IsInfinity(used.Value))return null;
  return new QuotaWindow { UsedPercent=used.Value, Minutes=(int)(Json.Num(d,"windowDurationMins")??Json.Num(d,"window_minutes")??0), ResetsAt=(long)(Json.Num(d,"resetsAt")??Json.Num(d,"resets_at")??0) };
 }
 public static QuotaSnapshot Parse(Dictionary<string,object> result,string source) {
  var s=new QuotaSnapshot { UpdatedAt=DateTimeOffset.UtcNow,Source=source };
  var all=Json.Obj(result,"rateLimitsByLimitId");
  if(all!=null&&all.Count>0) foreach(var pair in all) { var d=pair.Value as Dictionary<string,object>; s.Add(d,pair.Key); }
  else { var d=Json.Obj(result,"rateLimits")??Json.Obj(result,"rate_limits")??result; s.Add(d,Json.Str(d,"limitId")); }
  if(s.Buckets.Count==0)throw new InvalidDataException("接口未返回可用额度；请检查 Codex 的登录方式。");
  return s;
 }
 void Add(Dictionary<string,object>d,string id) { if(d==null)return; var b=new QuotaBucket { Id=string.IsNullOrEmpty(id)?"codex":id,Name=Json.Str(d,"limitName"),Primary=Window(Json.Obj(d,"primary")),Secondary=Window(Json.Obj(d,"secondary")) }; if(b.Primary!=null||b.Secondary!=null)Buckets.Add(b); }
}
public sealed class CodexQuota {
 public static async Task<QuotaSnapshot> Read(string exe) {
  if(string.IsNullOrEmpty(exe))throw new FileNotFoundException("未找到 Codex CLI，请在设置中选择 codex.exe。");
  using(var p=new Process()) {
   p.StartInfo=new ProcessStartInfo(exe,"app-server --listen stdio://") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8 };
   var response=new TaskCompletionSource<QuotaSnapshot>();
   p.OutputDataReceived+=(sender,e)=> {
    if(e.Data==null)return;
    try {
     var m=Json.Read(e.Data); var id=Json.Num(m,"id");
     if(id==1) { if(Json.Obj(m,"error")!=null)throw new InvalidOperationException("Codex 握手失败。"); p.StandardInput.WriteLine("{\"method\":\"initialized\",\"params\":{}}"); p.StandardInput.WriteLine("{\"id\":2,\"method\":\"account/rateLimits/read\"}"); p.StandardInput.Flush(); }
     if(id==2) { if(Json.Obj(m,"error")!=null)throw new InvalidOperationException("额度读取失败，请先在 Codex 中登录 ChatGPT 账号。"); response.TrySetResult(QuotaSnapshot.Parse(Json.Obj(m,"result"),"Codex 官方本地接口")); }
    } catch(Exception error) { response.TrySetException(error); }
   };
   p.ErrorDataReceived+=(sender,e)=>{};
   p.EnableRaisingEvents=true; p.Exited+=(sender,e)=>response.TrySetException(new IOException("Codex 接口进程已退出。"));
   try {
    p.Start(); p.BeginOutputReadLine();p.BeginErrorReadLine();
    p.StandardInput.WriteLine("{\"id\":1,\"method\":\"initialize\",\"params\":{\"clientInfo\":{\"name\":\"dragon_companion\",\"title\":\"Dragon Companion\",\"version\":\"1.0.0\"}}}");p.StandardInput.Flush();
    if(await Task.WhenAny(response.Task,Task.Delay(25000))!=response.Task)throw new TimeoutException("额度查询超时；将保留上次结果并标为过期。");
    return await response.Task;
   } finally { try { if(!p.HasExited) { p.StandardInput.Close(); if(!p.WaitForExit(800)) p.Kill(); } } catch {} }
  }
 }
}
public sealed class PetEvent {
 public string Id=Guid.NewGuid().ToString("N"), Kind="", Message="", ThreadId="", Source="";
 public DateTimeOffset Time=DateTimeOffset.UtcNow;
}
public static class EventParser {
 public static bool IsChildSession(Dictionary<string,object> meta){object source;return !string.IsNullOrEmpty(Json.Str(meta,"parent_thread_id"))||!string.IsNullOrEmpty(Json.Str(meta,"parent_session_id"))||(meta.TryGetValue("source",out source)&&(source is Dictionary<string,object>?((Dictionary<string,object>)source).ContainsKey("subagent"):Convert.ToString(source).StartsWith("subagent",StringComparison.OrdinalIgnoreCase)));}
 public static PetEvent Parse(Dictionary<string,object> record,string threadId) {
  var p=Json.Obj(record,"payload"); var outer=Json.Str(record,"type"); var type=Json.Str(p,"type");
  string kind="",message="";
  if(outer=="event_msg") {
   switch(type) {
    case "task_started": kind="working";message="任务开始了，我帮你守着进度。";break;
    case "task_complete": case "task_completed": object error;bool failed=p.TryGetValue("error",out error)&&error!=null;kind=failed?"error":"complete";message=failed?"这轮生成遇到问题了，需要你看一下。":"这轮生成已结束，去看看结果吧。";break;
    case "turn_aborted": kind="interrupted";message="任务已中断，等你决定下一步。";break;
    case "task_failed": kind="error";message="任务遇到问题了，需要你看一下。";break;
    case "exec_approval_request":case "apply_patch_approval_request":case "request_user_input":kind="waiting";message="需要你的确认，回到 Codex 看一下吧。";break;
    case "user_message":kind="resumed";message="收到回复，继续关注任务。";break;
   }
  }
  if(outer=="response_item"&&type=="function_call") {
   var name=Json.Str(p,"name"); if(name.EndsWith("request_user_input")||name.EndsWith("request_user_input_async")) { kind="waiting";message="GPT 举手：这边有个问题等你回答。"; }
  }
  if(kind=="")return null;
  DateTimeOffset stamp; if(!DateTimeOffset.TryParse(Json.Str(record,"timestamp"),out stamp))stamp=DateTimeOffset.UtcNow;
  return new PetEvent { Kind=kind,Message=message,ThreadId=threadId,Source="Codex 本地会话",Time=stamp };
 }
}
public sealed class SessionWatcher {
 sealed class Cursor { public long Offset; public string ThreadId="",LastCompletion="";public bool Ignore,HasMetadata;public void ReadMeta(Dictionary<string,object> meta){ThreadId=Json.Str(meta,"id");if(ThreadId=="")ThreadId=Json.Str(meta,"session_id");HasMetadata=ThreadId!="";Ignore=EventParser.IsChildSession(meta);} }
 readonly Dictionary<string,Cursor> cursors=new Dictionary<string,Cursor>(StringComparer.OrdinalIgnoreCase);
 readonly string root; readonly DateTimeOffset started=DateTimeOffset.UtcNow; bool initialized;
 public string Status="正在连接本地会话…";
 public SessionWatcher(string home) { root=Path.Combine(home,"sessions"); }
 public List<PetEvent> Poll() {
  var events=new List<PetEvent>();
  if(!Directory.Exists(root)) {Status="未找到本地 Codex 会话";return events;}
  var files=new List<FileInfo>();
  // Date directory naming follows the CLI's local timezone. Scan both adjacent days.
  for(int n=0;n<4;n++) {var d=DateTime.Now.AddDays(-n);var dir=Path.Combine(root,d.ToString("yyyy"),d.ToString("MM"),d.ToString("dd"));if(Directory.Exists(dir))files.AddRange(new DirectoryInfo(dir).GetFiles("*.jsonl"));}
  foreach(var info in files.OrderByDescending(x=>x.LastWriteTimeUtc).Take(150)) {
   try {
    Cursor c; if(!cursors.TryGetValue(info.FullName,out c)) {
     c=new Cursor();cursors[info.FullName]=c;
     using(var fs=new FileStream(info.FullName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)) using(var reader=new StreamReader(fs,Encoding.UTF8)) {var first=reader.ReadLine();try {c.ReadMeta(Json.Obj(Json.Read(first),"payload"));}catch{}}
     if(!initialized){c.Offset=info.Length;continue;}
    }
    if(c.Ignore){c.Offset=info.Length;continue;}
    if(info.Length<c.Offset){c.Offset=0;c.LastCompletion="";}
    if(info.Length==c.Offset)continue;
    using(var fs=new FileStream(info.FullName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)) {
     fs.Position=c.Offset;int count=(int)Math.Min(fs.Length-c.Offset,8*1024*1024);var data=new byte[count];int read=0,k;while(read<count&&(k=fs.Read(data,read,count-read))>0)read+=k;
     int last=Array.LastIndexOf(data,(byte)10,read-1,read);if(last<0){if(count>=8*1024*1024)c.Offset+=read;continue;}
     string text=Encoding.UTF8.GetString(data,0,last+1);c.Offset+=last+1;
     foreach(var line in text.Split('\n')) {if(line.Length<2)continue;try {var record=Json.Read(line);if(Json.Str(record,"type")=="session_meta"){c.ReadMeta(Json.Obj(record,"payload"));continue;}if(c.Ignore||!c.HasMetadata)continue;var ev=EventParser.Parse(record,c.ThreadId);if(ev!=null&&ev.Time>=started){if(ev.Kind=="complete"){string turn=Json.Str(Json.Obj(record,"payload"),"turn_id");if(turn!=""&&turn==c.LastCompletion)continue;c.LastCompletion=turn;}events.Add(ev);}}catch{}}
    }
   }catch(IOException){}catch(UnauthorizedAccessException){}
  }
  initialized=true;Status="已连接 · 本地会话事件";return events.OrderBy(x=>x.Time).ToList();
 }
}
public static class Inbox {
 public static readonly string[] Kinds={"working","complete","waiting","error","interrupted"};
 public static void Send(string kind,string text,string threadId) {
  if(!Kinds.Contains(kind))throw new ArgumentException("状态必须是 working、complete、waiting、error 或 interrupted。");
  Guid id;if(!string.IsNullOrEmpty(threadId)&&!Guid.TryParse(threadId,out id))throw new ArgumentException("thread 必须是 UUID。");
  var e=new PetEvent {Kind=kind,Message=text.Length>180?text.Substring(0,180):text,ThreadId=threadId,Source="本地通知接口"};
  Store.Save(Path.Combine("inbox",e.Id+".json"),e);
 }
 public static List<PetEvent> Drain() {
  var list=new List<PetEvent>();
  foreach(var file in Directory.GetFiles(Path.Combine(Store.Root,"inbox"),"*.json").Take(50)) {
   try {if(new FileInfo(file).Length<16000) {var e=Json.Serializer().Deserialize<PetEvent>(File.ReadAllText(file,Encoding.UTF8));if(e!=null&&Kinds.Contains(e.Kind)&&DateTimeOffset.UtcNow-e.Time<TimeSpan.FromHours(12))list.Add(e);}File.Delete(file);}catch(IOException){}catch(Exception e){Store.Log("通知文件读取失败："+e.GetType().Name);try{File.Delete(file);}catch{}}
  }return list;
 }
}
}
