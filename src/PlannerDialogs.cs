using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace DragonCompanion {
public class PlannerDialog:Form {
 protected readonly Panel Body;readonly Panel viewport;readonly SurfaceLabel credit;
 protected PlannerDialog(string title,int height){Text="GPT · "+title;Font=Theme.Font(10);ForeColor=Theme.Ink;BackColor=Color.FromArgb(251,250,254);AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.CenterParent;Icon=Art.MakeIcon();MinimizeBox=false;MaximizeBox=false;var work=Screen.PrimaryScreen.WorkingArea;ClientSize=new Size(Math.Min(620,work.Width-48),Math.Min(height,work.Height-70));MinimumSize=new Size(400,300);viewport=new Panel{Location=Point.Empty,AutoScroll=true,BackColor=BackColor};Body=new Panel{Location=Point.Empty,Size=new Size(600,height-38),BackColor=BackColor};viewport.Controls.Add(Body);Controls.Add(viewport);credit=Label(this,Theme.Credit,24,ClientSize.Height-27,570,24,9);Resize+=(s,e)=>Arrange();Arrange();}
 void Arrange(){viewport.Bounds=new Rectangle(0,0,ClientSize.Width,Math.Max(100,ClientSize.Height-34));viewport.AutoScrollMinSize=Body.Size;credit.SetBounds(24,ClientSize.Height-27,Math.Max(200,ClientSize.Width-48),24);}
 protected SurfaceLabel Label(Control parent,string text,int x,int y,int w,int h,float size=10,bool bold=false){var l=new SurfaceLabel{Text=text,Bounds=new Rectangle(x,y,w,h),ForeColor=bold?Theme.Ink:Theme.Muted,Font=Theme.Font(size,bold)};parent.Controls.Add(l);return l;}
 protected SurfaceLabel TextLabel(string text,int x,int y,int w,int h,float size=10,bool bold=false){return Label(Body,text,x,y,w,h,size,bold);}
 protected SoftButton Action(string text,int x,int y,int width,System.Action action,bool primary=false){var b=new SoftButton(text,primary){Bounds=new Rectangle(x,y,width,38)};b.Click+=(s,e)=>action();Body.Controls.Add(b);return b;}
 protected SoftNumber Number(string name,int x,int y,int width,int min,int max,int value){var n=new SoftNumber{Compact=true,Location=new Point(x,y),Width=width,Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),AccessibleName=name};Body.Controls.Add(n);return n;}
}
public sealed class GoalPlanDialog:PlannerDialog {
 readonly GoalPlan original;readonly ToolTextInput goal;readonly SoftNumber year,month,day,remindHour,remindMinute,reviewHour,reviewMinute,budget;readonly SurfaceLabel error;
 public GoalPlan Result;
 public GoalPlanDialog(GoalPlan existing=null):base(existing==null?"制定计划":"修改期限与提醒",580){
  original=existing==null?new GoalPlan{Deadline=GoalPlan.DateKey(DateTime.Now.AddDays(14))}:existing.Copy();
  TextLabel(existing==null?"制定每日计划":"修改期限与提醒",24,16,560,40,21,true);
  TextLabel(existing==null?"填写目标与结束日期，由 Codex 拆成每日安排并加入日程。":"已完成的打勾记录会保留；更改期限后自动重排剩余任务。",24,61,560,38,10);
  TextLabel("目标",24,101,560,24,10,true);goal=new ToolTextInput(true){Bounds=new Rectangle(24,130,560,72)};goal.Editor.MaxLength=1000;goal.Editor.AccessibleName="计划目标";goal.Editor.Text=original.Goal;goal.Editor.ReadOnly=existing!=null;Body.Controls.Add(goal);
  TextLabel("结束日期",24,215,250,25,10,true);var end=original.End;
  year=Number("计划结束年份",24,245,96,2000,2099,end.Year);month=Number("计划结束月份",155,245,66,1,12,end.Month);day=Number("计划结束日期",255,245,66,1,31,end.Day);
  TextLabel("年",125,252,26,25);TextLabel("月",225,252,26,25);TextLabel("日",325,252,26,25);
  year.ValueChanged+=(s,e)=>ClampDay();month.ValueChanged+=(s,e)=>ClampDay();ClampDay();TextLabel("支持未来一年内的每日计划",365,252,218,28,9);
  TextLabel("每日提醒",24,305,100,26,10,true);remindHour=Number("每日提醒小时",128,299,65,0,23,original.ReminderMinutes/60);TextLabel(":",200,306,16,25,12,true);remindMinute=Number("每日提醒分钟",221,299,65,0,59,original.ReminderMinutes%60);
  TextLabel("每天可投入",326,305,116,26,10,true);budget=Number("每天可投入分钟",449,299,88,5,720,original.DailyMinutes);TextLabel("分钟",545,306,43,25,9);
  TextLabel("晚间询问",24,360,100,26,10,true);reviewHour=Number("晚间询问小时",128,354,65,0,23,original.ReviewMinutes/60);TextLabel(":",200,361,16,25,12,true);reviewMinute=Number("晚间询问分钟",221,354,65,0,59,original.ReviewMinutes%60);TextLabel("每天询问是否完成；未完成会重新安排。",326,358,260,43,9);
  Action(existing==null?"由 Codex 生成并加入日程":"保存修改",24,425,300,Save,true);Action("返回",340,425,120,()=>{DialogResult=DialogResult.Cancel;Close();});error=TextLabel("",24,478,560,45,9);error.ForeColor=Color.FromArgb(176,72,104);
 }
 void ClampDay(){if(day==null)return;day.Maximum=DateTime.DaysInMonth((int)year.Value,(int)month.Value);}
 void Save(){try{var plan=original.Copy();plan.Goal=goal.Editor.Text.Trim();plan.Deadline=GoalPlan.DateKey(new DateTime((int)year.Value,(int)month.Value,(int)day.Value));plan.ReminderMinutes=(int)remindHour.Value*60+(int)remindMinute.Value;plan.ReviewMinutes=(int)reviewHour.Value*60+(int)reviewMinute.Value;plan.DailyMinutes=(int)budget.Value;plan.Validate(DateTime.Now);if(original.Days.Count==0)PlanEngine.Request(plan,DateTime.Now,"首次制定计划");Result=plan;DialogResult=DialogResult.OK;Close();}catch(ArgumentException ex){error.Text=ex.Message;}}
}
public sealed class PlanFeedbackDialog:PlannerDialog {
 readonly ToolTextInput input;public string Feedback {get{return input.Editor.Text.Trim();}}
 public PlanFeedbackDialog(GoalPlan plan,PlanDay day):base("未完成反馈",420){TextLabel("重新安排接下来的计划",24,16,560,40,20,true);TextLabel(day.Date+" · "+day.Title,24,70,560,56,12,true);TextLabel("可以说说未完成的原因、卡住的内容或新的安排要求（可选）。",24,136,560,42);input=new ToolTextInput(true){Bounds=new Rectangle(24,184,560,86)};input.Editor.AccessibleName="未完成原因";Body.Controls.Add(input);Action("记录未完成并重新制定",24,300,280,()=>{DialogResult=DialogResult.OK;Close();},true);Action("返回",320,300,120,()=>Close());}
}
internal sealed class EarlyPlanCompletionDialog:PlannerDialog {
 public bool AdvanceRelated{get;private set;}
 public EarlyPlanCompletionDialog(PlanDay day):base("提前完成任务",240){
  TextLabel("提前完成任务",24,15,552,38,20,true);
  TextLabel(day.Title,24,59,552,36,11,true);
  TextLabel("原定 "+day.Date+"。可以让同主题的后续任务补位，也可以把这一天留作休息。",24,101,552,44,10);
  Action("完成并调整相关任务",24,153,212,()=>Choose(true),true);
  Action("只完成，原定当天休息",247,153,220,()=>Choose(false));
  Action("取消",478,153,98,Cancel);
  KeyPreview=true;KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape){e.SuppressKeyPress=true;Cancel();}};
 }
 void Choose(bool advanceRelated){AdvanceRelated=advanceRelated;DialogResult=DialogResult.OK;Close();}
 void Cancel(){DialogResult=DialogResult.Cancel;Close();}
}
public sealed class PlanReviewDialog:PlannerDialog {
 readonly Controller app;readonly SurfaceLabel question,heading,task,state;readonly ToolTextInput feedback;readonly SoftButton yes,no;PlanReviewTicket current;
 public PlanReviewDialog(Controller controller):base("每日计划复盘",460){app=controller;question=TextLabel("今天的计划完成了吗？",24,16,560,40,21,true);heading=TextLabel("",24,68,560,46,11,true);task=TextLabel("",24,120,560,58,11);TextLabel("未完成时可补充原因，Codex 会按剩余期限重新安排。",24,187,560,28,9);feedback=new ToolTextInput(true){Bounds=new Rectangle(24,220,560,72)};feedback.Editor.AccessibleName="计划复盘反馈";Body.Controls.Add(feedback);yes=Action("已完成 ✓",24,316,160,()=>{var unused=Answer(true);},true);no=Action("未完成，重新安排",198,316,230,()=>{var unused=Answer(false);});Action("稍后",442,316,142,()=>Close());state=TextLabel("",24,373,560,40,9);LoadTicket();}
 void LoadTicket(){current=app.PendingPlanReviews.OrderByDescending(t=>t.Day).FirstOrDefault(t=>app.FindPlan(t.PlanId)!=null&&app.FindPlan(t.PlanId).Days.Any(d=>d.Id==t.DayId&&d.State=="pending"));if(current==null){Close();return;}question.Text=current.Day==DateTime.Today?"今天的计划完成了吗？":"这一天的计划完成了吗？";var plan=app.FindPlan(current.PlanId);var day=plan.Days.Single(d=>d.Id==current.DayId);heading.Text=day.Date+" · "+plan.Goal;task.Text=day.Title+"\n建议投入 "+day.Minutes+" 分钟";feedback.Editor.Clear();state.Text="完成后会在日程表打勾；结束日期为 "+plan.Deadline+"。";}
 async Task Answer(bool done){if(current==null)return;yes.Enabled=no.Enabled=false;try{state.Text=done?"正在记录完成…":"正在记录反馈并重新安排…";string id=current.PlanId;await app.ReportPlanDay(id,current.DayId,done,feedback.Editor.Text,DateTime.Now);if(IsDisposed)return;var plan=app.FindPlan(id);if(!done&&plan!=null&&plan.NeedsReplan){app.ShowPanel();app.Panel.OpenPlan(id);}LoadTicket();}catch(Exception ex){if(!IsDisposed)state.Text=ex.Message;}finally{if(!IsDisposed)yes.Enabled=no.Enabled=true;}}
}
}
