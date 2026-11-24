using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace DragonCompanion {
public sealed partial class Controller {
 sealed class PlanJob {public CancellationTokenSource Cancel=new CancellationTokenSource();public string Message="正在连接 Codex…";}
 readonly Dictionary<string,PlanJob> planJobs=new Dictionary<string,PlanJob>();
 readonly SemaphoreSlim earlyCompletionGate=new SemaphoreSlim(1,1);
 int pendingEarlyCompletions;
 bool quitAfterEarlyCompletions;
 public readonly List<PlanReviewTicket> PendingPlanReviews=new List<PlanReviewTicket>();
 PlanReviewDialog planReviewDialog;
 public Func<string,PlanRequest,CancellationToken,IProgress<string>,Task<PlanProposal>> PlanGenerator=CodexPlanClient.Generate;
 public GoalPlan FindPlan(string id){return Schedules.Plans.FirstOrDefault(p=>p.Id==id);}
 public bool PlanGenerating(string id){return planJobs.ContainsKey(id);}
 public string PlanStatus(string id){var plan=FindPlan(id);if(plan==null)return "计划不存在";PlanJob job;if(planJobs.TryGetValue(id,out job))return job.Message;if(plan.Error!="")return plan.Error;if(!plan.Enabled)return "已暂停每日提醒和晚间询问";if(plan.NeedsReplan)return "需要重新安排；完成记录已保留";if(plan.Complete)return "计划已全部完成 ✓";return "已完成 "+plan.CompletedCount+" 项 · 结束 "+plan.Deadline+" · 每晚 "+plan.TimeLabel(plan.ReviewMinutes)+" 询问完成情况";}
 void SavePlanBook(ScheduleBook book){Store.Save("schedules.json",book);PublishPlanBook(book);}
 void PublishPlanBook(ScheduleBook book){Schedules=book;ScheduleVersion++;}
 void RefreshPlanUi(){if(!closing&&Panel!=null&&!Panel.IsDisposed)Panel.UpdateLive();}
 void CancelOldPlanJob(string id){PlanJob old;if(planJobs.TryGetValue(id,out old))old.Cancel.Cancel();}
 public async void NewGoalPlan(){try{using(var dialog=new GoalPlanDialog())if(dialog.ShowDialog(Panel!=null&&!Panel.IsDisposed&&Panel.Visible?(IWin32Window)Panel:Pet)==DialogResult.OK)await CreateGoalPlan(dialog.Result,DateTime.Now);}catch(Exception ex){if(!closing)MessageBox.Show(Panel,ex.Message+"\n\n"+Theme.Credit,"计划未创建");}}
 public async Task<string> CreateGoalPlan(GoalPlan draft,DateTime now){
  var plan=draft.Copy();plan.Goal=plan.Goal.Trim();plan.Validate(now);PlanEngine.Request(plan,now,"首次制定计划");
  var book=Schedules.Copy();if(book.Plans.Any(p=>p.Id==plan.Id))throw new ArgumentException("计划已存在。");book.Plans.Add(plan);SavePlanBook(book);
  if(!Diagnostics){ShowPanel();Panel.OpenPlan(plan.Id);}await RegenerateGoalPlan(plan.Id,"首次制定计划",now);return plan.Id;
 }
 public async void EditGoalPlan(string id){var plan=FindPlan(id);if(plan==null)return;try{if(plan.VocabularyDeckId!=""){await LoadVocabulary();ShowPanel("vocabulary");Panel.OpenVocabularyGoal(plan.VocabularyDeckId);return;}using(var dialog=new GoalPlanDialog(plan))if(dialog.ShowDialog(Panel!=null&&!Panel.IsDisposed&&Panel.Visible?(IWin32Window)Panel:Pet)==DialogResult.OK)await SaveGoalSettings(dialog.Result,DateTime.Now);}catch(Exception ex){if(!closing)MessageBox.Show(Panel,ex.Message+"\n\n"+Theme.Credit,"计划未更新");}}
 public async Task SaveGoalSettings(GoalPlan settings,DateTime now){
  settings.Validate(now);var book=Schedules.Copy();var plan=book.Plans.FirstOrDefault(p=>p.Id==settings.Id);if(plan==null)return;
  bool replan=plan.Deadline!=settings.Deadline||plan.DailyMinutes!=settings.DailyMinutes;
  bool retime=plan.ReminderMinutes!=settings.ReminderMinutes;
  plan.Deadline=settings.Deadline;plan.DailyMinutes=settings.DailyMinutes;plan.ReminderMinutes=settings.ReminderMinutes;plan.ReviewMinutes=settings.ReviewMinutes;
  if(replan){CancelOldPlanJob(plan.Id);plan.Revision++;plan.NeedsReplan=true;plan.Error="";}
  PlanEngine.SyncReminders(book,plan,now,retime);SavePlanBook(book);RefreshPlanUi();
  if(replan)await RegenerateGoalPlan(plan.Id,"用户修改结束日期或每日可投入时间，按新条件重新安排尚未完成内容",now);
 }
 public async Task<bool> RegenerateGoalPlan(string id,string reason,DateTime now,DateTime? missedDate=null){
  var linked=FindPlan(id);
  if(linked!=null&&linked.VocabularyDeckId!=""){await LoadVocabulary();SyncVocabularyPlan(linked.VocabularyDeckId,now);return true;}
  var started=DateTime.UtcNow;
  CancelOldPlanJob(id);var book=Schedules.Copy();var plan=book.Plans.FirstOrDefault(p=>p.Id==id);if(plan==null||closing)return false;
  plan.Revision++;plan.NeedsReplan=true;plan.Error="";PlanRequest request;
  try{request=PlanEngine.Request(plan,now,reason,missedDate);}catch(ArgumentException ex){plan.Error=ex.Message;PlanEngine.SyncReminders(book,plan,now);SavePlanBook(book);RefreshPlanUi();return false;}
  PlanEngine.SyncReminders(book,plan,now);SavePlanBook(book);var job=new PlanJob();planJobs[id]=job;RefreshPlanUi();
  try{
   var progress=new Progress<string>(message=>{PlanJob current;if(!closing&&planJobs.TryGetValue(id,out current)&&current==job){job.Message=message;RefreshPlanUi();}});
   var result=await PlanGenerator(Store.FindCodex(Settings.CodexPath),request,job.Cancel.Token,progress);
   job.Cancel.Token.ThrowIfCancellationRequested();if(closing||FindPlan(id)==null||FindPlan(id).Revision!=request.Plan.Revision)return false;
   var effectiveNow=now+(DateTime.UtcNow-started);if(request.Dates.Any(d=>(GoalPlan.ParseDate(d)??DateTime.MinValue)<effectiveNow.Date))throw new InvalidOperationException("生成期间日期已变化，请重新安排，完成记录会保留。");
   var saved=Schedules.Copy();var target=saved.Plans.Single(p=>p.Id==id);PlanEngine.Apply(target,request,result);PlanEngine.SyncReminders(saved,target,effectiveNow);SavePlanBook(saved);
   Receive(new PetEvent{Kind="complete",Source="制定计划",Message="每日计划已安排好，"+result.days.Count+" 项任务已同步到日程提醒。"});return true;
  }catch(OperationCanceledException){return false;}
  catch(Exception ex){
   if(!closing&&FindPlan(id)!=null&&FindPlan(id).Revision==request.Plan.Revision){var failed=Schedules.Copy();var target=failed.Plans.Single(p=>p.Id==id);target.Error=ex.Message;target.NeedsReplan=true;PlanEngine.SyncReminders(failed,target,now);SavePlanBook(failed);Store.Log("计划生成："+ex.GetType().Name);}
   return false;
  }finally{PlanJob current;if(planJobs.TryGetValue(id,out current)&&current==job)planJobs.Remove(id);job.Cancel.Dispose();RefreshPlanUi();}
 }
 public void CancelGoalGeneration(string id){CancelOldPlanJob(id);var book=Schedules.Copy();var plan=book.Plans.FirstOrDefault(p=>p.Id==id);if(plan==null)return;plan.Revision++;plan.NeedsReplan=true;plan.Error="生成已停止，可点击重新安排继续。";PlanEngine.SyncReminders(book,plan,DateTime.Now);SavePlanBook(book);RefreshPlanUi();}
 public async Task ReportPlanDay(string planId,string dayId,bool complete,string feedback,DateTime now,bool advanceRelated=true){
  var current=FindPlan(planId);
  var currentDay=current==null?null:current.Days.FirstOrDefault(d=>d.Id==dayId);
  if(currentDay==null||complete&&currentDay.Done)return;
  if(complete&&currentDay.Day>now.Date){await CompletePlanDayEarly(planId,dayId,feedback,now,advanceRelated);return;}
  var book=Schedules.Copy();var plan=book.Plans.FirstOrDefault(p=>p.Id==planId);var day=plan==null?null:plan.Days.FirstOrDefault(d=>d.Id==dayId);if(day==null)return;
  if(!complete&&day.Day>now.Date)throw new ArgumentException("这一天尚未开始，可先修改期限或重新安排。");
  bool regenerate=!complete||PlanGenerating(planId)||plan.NeedsReplan||day.State=="missed";
  SetPlanDayResult(plan,day,complete,feedback,now);
  if(!complete){plan.LastFeedback=day.Date+" · "+day.Title+" · "+(day.Feedback==""?"未完成，需要重新安排":day.Feedback);plan.NeedsReplan=true;}
  PlanEngine.SyncReminders(book,plan,now);SavePlanBook(book);OnPlanDaySaved(planId,dayId);
  if(regenerate)await RegenerateGoalPlan(planId,complete?"用户更新了完成记录，请保留打勾内容并更新剩余安排":"用户反馈未完成："+plan.LastFeedback,now,complete?(DateTime?)null:day.Day);
 }
 static void SetPlanDayResult(GoalPlan plan,PlanDay day,bool complete,string feedback,DateTime now){
  plan.Revision++;day.State=complete?"done":"missed";day.CompletedAt=complete?ScheduleEntry.Stamp(now):"";
  day.Feedback=(feedback??"").Trim();if(day.Feedback.Length>1000)day.Feedback=day.Feedback.Substring(0,1000);
 }
 void OnPlanDaySaved(string planId,string dayId){
  CancelOldPlanJob(planId);PendingPlanReviews.RemoveAll(ticket=>ticket.PlanId==planId&&ticket.DayId==dayId);RefreshPlanUi();
 }
 async Task CompletePlanDayEarly(string planId,string dayId,string feedback,DateTime now,bool advanceRelated){
  pendingEarlyCompletions++;
  await earlyCompletionGate.WaitAsync();
  try{
   while(!closing){
    var snapshot=Schedules;
    var plan=FindPlan(planId);
    var day=plan==null?null:plan.Days.FirstOrDefault(d=>d.Id==dayId);
    if(day==null||day.Done)return;
    if(day.Day<=now.Date){await ReportPlanDay(planId,dayId,true,feedback,now);return;}
    bool generating=PlanGenerating(planId);
    ScheduleBook updated=null;
    // Prepare the complete new file off the UI thread. Only publish if the
    // snapshot is still current so other schedule edits cannot be overwritten.
    using(var save=await Task.Run(()=>{
     updated=snapshot.Copy();
     var target=updated.Plans.Single(p=>p.Id==planId);
     var completed=target.Days.Single(d=>d.Id==dayId);
     SetPlanDayResult(target,completed,true,feedback,now);
     completed.KeepEarlyDateFree=!advanceRelated;
     if(generating)target.Error="已记录提前完成。之前的生成已停止，可点击重新安排继续。";
     var moved=advanceRelated?PlanEngine.AdvanceAfterEarlyCompletion(target,completed):null;
     PlanEngine.SyncReminders(updated,target,now,movedDays:moved);
     return Store.PrepareSave("schedules.json",updated);
    })){
     for(int attempt=0;ReferenceEquals(Schedules,snapshot);attempt++){
      bool committed=false;
      try{save.Commit();committed=true;}
      catch(IOException){if(attempt>=4)throw;}
      if(committed){PublishPlanBook(updated);OnPlanDaySaved(planId,dayId);return;}
      await Task.Delay(30*(1<<attempt));
     }
    }
   }
  }finally{
   earlyCompletionGate.Release();
   if(--pendingEarlyCompletions==0&&quitAfterEarlyCompletions)Quit();
  }
 }
 public void UndoPlanCompletion(string planId,string dayId,DateTime now){
  var book=Schedules.Copy();var plan=book.Plans.FirstOrDefault(p=>p.Id==planId);var day=plan==null?null:plan.Days.FirstOrDefault(d=>d.Id==dayId);if(day==null||!day.Done)return;
  var moved=day.CompletedEarly?PlanEngine.RestoreEarlyCompletion(plan,day,now.Date):null;
  plan.Revision++;day.State="pending";day.CompletedAt="";day.EarlyVacatedDate="";day.KeepEarlyDateFree=false;
  PlanEngine.SyncReminders(book,plan,now,movedDays:moved);SavePlanBook(book);CancelOldPlanJob(planId);RefreshPlanUi();
 }
 public async void AskPlanFeedback(string planId,string dayId){var plan=FindPlan(planId);var day=plan==null?null:plan.Days.FirstOrDefault(d=>d.Id==dayId);if(day==null)return;try{using(var dialog=new PlanFeedbackDialog(plan,day))if(dialog.ShowDialog(Panel!=null&&Panel.Visible?(IWin32Window)Panel:Pet)==DialogResult.OK)await ReportPlanDay(planId,dayId,false,dialog.Feedback,DateTime.Now);}catch(Exception ex){if(!closing)MessageBox.Show(Panel,ex.Message+"\n\n"+Theme.Credit,"反馈未保存");}}
 public async void ToggleGoalPlan(string id){
  try{
   var linked=FindPlan(id);
   if(linked!=null&&linked.VocabularyDeckId!=""){
    bool enabled=!linked.Enabled;
    await LoadVocabulary();
    var goal=Vocabulary.Goal(linked.VocabularyDeckId);goal.LinkPlan=enabled;
    SaveVocabularyGoal(linked.VocabularyDeckId,goal,DateTime.Now);return;
   }
   var book=Schedules.Copy();var plan=book.Plans.FirstOrDefault(p=>p.Id==id);if(plan==null)return;
   plan.Enabled=!plan.Enabled;PlanEngine.SyncReminders(book,plan,DateTime.Now);SavePlanBook(book);
   if(!plan.Enabled)PendingPlanReviews.RemoveAll(t=>t.PlanId==id);else RestorePendingPlanReviews(DateTime.Now);
   RefreshPlanUi();
  }catch(Exception ex){if(!closing)MessageBox.Show(Panel,ex.Message+"\n\n"+Theme.Credit,"计划未更新");}
 }
 public async void DeleteGoalPlan(string id){
  var plan=FindPlan(id);if(plan==null)return;
  if(MessageBox.Show(Panel,"删除这个计划及其生成的日程提醒？\n\n"+Theme.Credit,"删除计划",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)return;
  try{
   if(plan.VocabularyDeckId!=""){
    await LoadVocabulary();var goal=Vocabulary.Goal(plan.VocabularyDeckId);goal.LinkPlan=false;
    Vocabulary.SaveGoal(plan.VocabularyDeckId,goal,DateTime.Now);
   }
   CancelOldPlanJob(id);var book=Schedules.Copy();
   book.Plans.RemoveAll(p=>p.Id==id);book.Items.RemoveAll(e=>e.PlanId==id);SavePlanBook(book);
   PendingPlanReviews.RemoveAll(t=>t.PlanId==id);
   if(Panel!=null&&!Panel.IsDisposed)Panel.Navigate("plans");
  }catch(Exception ex){if(!closing)MessageBox.Show(Panel,ex.Message+"\n\n"+Theme.Credit,"计划未删除");}
 }
 internal void RestorePendingPlanReviews(DateTime now){
  foreach(var plan in Schedules.Plans.Where(p=>p.Enabled&&!p.NeedsReplan&&p.LastAskedDate!="")){
   var day=plan.Days.FirstOrDefault(d=>d.Date==plan.LastAskedDate&&d.State=="pending");if(day==null)continue;
   DateTime when=day.Day.AddMinutes(plan.ReviewMinutes);if(when<=now&&now-when<=TimeSpan.FromHours(24)&&!PendingPlanReviews.Any(t=>t.PlanId==plan.Id&&t.DayId==day.Id))PendingPlanReviews.Add(new PlanReviewTicket{PlanId=plan.Id,DayId=day.Id,Day=day.Day});
  }
 }
 public void PollPlanReviews(DateTime now){
  PendingPlanReviews.RemoveAll(t=>{var plan=FindPlan(t.PlanId);return plan==null||!plan.Enabled||now-t.Day.AddMinutes(plan.ReviewMinutes)>TimeSpan.FromHours(24);});
  if(!PlanEngine.HasDueReview(Schedules,now))return;
  var book=Schedules.Copy();var due=PlanEngine.CollectReviews(book,now);if(due.Count==0)return;SavePlanBook(book);
  foreach(var ticket in due)if(!PendingPlanReviews.Any(t=>t.PlanId==ticket.PlanId&&t.DayId==ticket.DayId))PendingPlanReviews.Add(ticket);
  Receive(new PetEvent{Kind="waiting",Source="计划复盘",Message=(due.All(t=>t.Day==now.Date)?"今天的计划完成了吗？":"有一项计划需要复盘。")+"完成后打勾，未完成可填写原因并重新安排。"});
  if(!Diagnostics&&!Settings.Quiet)ShowPlanReview();
 }
 public void ShowPlanReview(){
  PendingPlanReviews.RemoveAll(t=>{var plan=FindPlan(t.PlanId);return plan==null||!plan.Enabled||!plan.Days.Any(d=>d.Id==t.DayId&&d.State=="pending");});
  if(PendingPlanReviews.Count==0){ShowPanel();Panel.Navigate("plans");return;}
  if(planReviewDialog!=null&&!planReviewDialog.IsDisposed){planReviewDialog.Activate();return;}
  planReviewDialog=new PlanReviewDialog(this);planReviewDialog.FormClosed+=(s,e)=>planReviewDialog=null;planReviewDialog.Show(Pet);
 }
 void StopPlanJobs(){foreach(var job in planJobs.Values.ToArray())job.Cancel.Cancel();if(planReviewDialog!=null&&!planReviewDialog.IsDisposed)planReviewDialog.Dispose();}
}
}
