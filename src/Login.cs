using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DragonCompanion {
public static class Login {
 public static async Task Run(string exe,CancellationToken cancel,Action<string> status) {
  if(string.IsNullOrEmpty(exe))throw new FileNotFoundException("未找到 Codex CLI，请先在设置中选择 codex.exe。");
  using(var p=new Process()) {
   var done=new TaskCompletionSource<bool>();string loginId="";
   p.StartInfo=new ProcessStartInfo(exe,"app-server --listen stdio://") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8 };
   Action<object> send=o=>{p.StandardInput.WriteLine(Json.Encode(o));p.StandardInput.Flush();};
   p.OutputDataReceived+=(s,e)=>{
    if(e.Data==null)return;
    try {
     var m=Json.Read(e.Data);var id=Json.Num(m,"id");
     if(id==1){if(Json.Obj(m,"error")!=null)throw new Exception("登录接口初始化失败。");send(new{method="initialized",@params=new{}});send(new{id=3,method="account/login/start",@params=new{type="chatgpt"}});}
     if(id==3){var r=Json.Obj(m,"result");if(r==null)throw new Exception("无法启动登录，请检查 Codex 状态。");loginId=Json.Str(r,"loginId");Uri url;if(!Uri.TryCreate(Json.Str(r,"authUrl"),UriKind.Absolute,out url)||url.Scheme!="https"||!(url.Host=="auth.openai.com"||url.Host=="auth0.openai.com"||url.Host=="chatgpt.com"||url.Host.EndsWith(".chatgpt.com")))throw new Exception("登录接口返回了无法验证的授权地址。");Process.Start(new ProcessStartInfo(url.AbsoluteUri){UseShellExecute=true});status("请在浏览器完成官方登录；本窗口会自动同步结果。");}
     if(Json.Str(m,"method")=="account/login/completed"){var r=Json.Obj(m,"params");if(Json.Get(r,"success") is bool&&(bool)Json.Get(r,"success"))done.TrySetResult(true);else done.TrySetException(new Exception("登录未完成，可以重新尝试。"));}
    }catch(Exception ex){done.TrySetException(ex);}
   };
   p.ErrorDataReceived+=(s,e)=>{};p.EnableRaisingEvents=true;p.Exited+=(s,e)=>done.TrySetException(new Exception("登录进程已关闭。"));
   try {p.Start();p.BeginOutputReadLine();p.BeginErrorReadLine();send(new{id=1,method="initialize",@params=new{clientInfo=new{name="dragon_companion",title="Dragon Companion",version="1.0.0"}}});
    using(cancel.Register(()=>done.TrySetCanceled())) {if(await Task.WhenAny(done.Task,Task.Delay(TimeSpan.FromMinutes(5)))!=done.Task)throw new TimeoutException("登录等待已超时，请重试。");await done.Task;}
   }finally{try{if(!p.HasExited){if(loginId!="")send(new{id=4,method="account/login/cancel",@params=new{loginId=loginId}});p.StandardInput.Close();if(!p.WaitForExit(600))p.Kill();}}catch{}}
  }
 }
}
}
