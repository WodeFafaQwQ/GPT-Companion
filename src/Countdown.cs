using System;
using System.Drawing;
using System.Windows.Forms;

namespace DragonCompanion {
public sealed class CountdownClock {
 readonly Func<DateTimeOffset> now;DateTimeOffset? deadline;TimeSpan? pausedRemaining;
 public CountdownClock():this(()=>DateTimeOffset.UtcNow){}
 public CountdownClock(Func<DateTimeOffset> clock){now=clock;}
 public bool Active{get{return deadline.HasValue||pausedRemaining.HasValue;}}
 public bool Paused{get{return pausedRemaining.HasValue;}}
 public TimeSpan Remaining{get{var t=pausedRemaining??(deadline.HasValue?deadline.Value-now():TimeSpan.Zero);return t<TimeSpan.Zero?TimeSpan.Zero:t;}}
 public string Display{get{long seconds=(long)Math.Ceiling(Remaining.TotalSeconds);return seconds>=3600?(seconds/3600).ToString("00")+":"+(seconds/60%60).ToString("00")+":"+(seconds%60).ToString("00"):(seconds/60).ToString("00")+":"+(seconds%60).ToString("00");}}
 public void Start(TimeSpan duration){if(duration<=TimeSpan.Zero||duration>TimeSpan.FromHours(24))throw new ArgumentOutOfRangeException("duration");pausedRemaining=null;deadline=now()+duration;}
 public void TogglePause(){if(Paused){deadline=now()+pausedRemaining.Value;pausedRemaining=null;}else if(deadline.HasValue&&Remaining>TimeSpan.Zero){pausedRemaining=Remaining;deadline=null;}}
 public void Cancel(){deadline=null;pausedRemaining=null;}
 public bool ConsumeExpiration(){if(deadline.HasValue&&now()>=deadline.Value){Cancel();return true;}return false;}
}
public sealed class CountdownDialog:Form {
 readonly Controller app;readonly SoftNumber minutes,seconds;readonly SurfaceLabel remaining;readonly SoftButton pause;readonly Timer timer=new Timer{Interval=250};
 public CountdownDialog(Controller controller){app=controller;Text="GPT · 番茄钟";ClientSize=new Size(470,370);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(251,250,254);Font=Theme.Font(10);ForeColor=Theme.Ink;AutoScaleMode=AutoScaleMode.None;
  AddLabel("给自己一点专注的时间",24,18,420,42,18,true);AddLabel("选择常用时长，或自行输入分钟和秒数。",24,63,420,30,10,false);
  int[] presets={5,15,25,45,60};for(int i=0;i<presets.Length;i++){int value=presets[i];var button=new SoftButton(value+" 分钟"){Location=new Point(24+i*86,105),Size=new Size(78,36)};button.Click+=(s,e)=>{minutes.Value=value;seconds.Value=0;};Controls.Add(button);}
  minutes=new SoftNumber{Location=new Point(24,166),Width=132,Minimum=0,Maximum=999,Value=Math.Max(0,Math.Min(999,app.Settings.CountdownMinutes)),AccessibleName="番茄钟分钟"};seconds=new SoftNumber{Location=new Point(230,166),Width=132,Minimum=0,Maximum=59,Value=Math.Max(0,Math.Min(59,app.Settings.CountdownSeconds)),AccessibleName="番茄钟秒"};Controls.Add(minutes);Controls.Add(seconds);AddLabel("分钟",168,173,50,28,11,false);AddLabel("秒",374,173,70,28,11,false);
  remaining=AddLabel("",24,219,420,30,11,false);
  var start=new SoftButton("开始番茄钟",true){Location=new Point(24,271),Width=150};start.Click+=(s,e)=>{int total=(int)minutes.Value*60+(int)seconds.Value;if(total<=0){remaining.Text="请设置大于 0 秒的时间。";return;}app.Settings.CountdownMinutes=(int)minutes.Value;app.Settings.CountdownSeconds=(int)seconds.Value;app.Save();app.Countdown.Start(TimeSpan.FromSeconds(total));app.Say("番茄钟开始",app.Countdown.Display+" 后提醒你。","working",5);Close();};Controls.Add(start);
  pause=new SoftButton("暂停 / 继续"){Location=new Point(187,271),Width=124};pause.Click+=(s,e)=>{app.Countdown.TogglePause();UpdateStatus();};Controls.Add(pause);
  var cancel=new SoftButton("取消计时"){Location=new Point(324,271),Width=122};cancel.Click+=(s,e)=>{app.Countdown.Cancel();app.Say("番茄钟","计时已取消。","wave",4);UpdateStatus();};Controls.Add(cancel);
  AddLabel(Theme.Credit,24,335,420,26,9,false);timer.Tick+=(s,e)=>UpdateStatus();timer.Start();UpdateStatus();FormClosed+=(s,e)=>timer.Dispose();
 }
 SurfaceLabel AddLabel(string text,int x,int y,int w,int h,float size,bool bold){var l=new SurfaceLabel{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=Theme.Font(size,bold),ForeColor=bold?Theme.Ink:Theme.Muted};Controls.Add(l);return l;}
 void UpdateStatus(){remaining.Text=app.Countdown.Active?(app.Countdown.Paused?"已暂停 · ":"剩余 · ")+app.Countdown.Display:"当前没有正在运行的番茄钟";pause.Enabled=app.Countdown.Active;pause.Text=app.Countdown.Paused?"继续":"暂停";}
}
}
