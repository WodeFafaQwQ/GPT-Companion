using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace DragonCompanion {
public static partial class SelfTests {
 static void TestScheduleRecovery(string folder){
  string root=Store.Root;
  try{
   Store.Root=Path.Combine(folder,"recovery-"+Guid.NewGuid().ToString("N"));Store.Init();
   string main=Path.Combine(Store.Root,"schedules.json"),backup=Path.Combine(Store.Root,"schedules.recovery.json");
   Check(Store.LoadSchedules().Plans.Count==0&&!File.Exists(main),"A fresh schedule profile has no schedules to recover");
   var day=new PlanDay{Title="恢复测试",Date="2026-11-20",State="done",Feedback="已完成"};
   var plan=new GoalPlan{Goal="本地恢复测试"};plan.Days.Add(day);
   var book=new ScheduleBook();book.Plans.Add(plan);
   Store.Save("schedules.json",book);
   day.Feedback="较新的反馈";
   using(var prepared=Store.PrepareSave("schedules.json",book)){Check(Store.LoadSchedules().Plans.Single().Days.Single().Feedback=="已完成","An uncommitted schedule snapshot does not alter the saved plan");prepared.Commit();}
   var saved=File.ReadAllText(main);
   Check(File.ReadAllText(backup)==saved,"The recovery copy stores the latest committed plan");
   File.Delete(main);
   Check(Store.LoadSchedules().Plans.Single().Days.Single().Feedback=="较新的反馈"&&File.ReadAllText(main)==saved,"Restores a missing schedule file with the latest completed task and feedback");
   File.WriteAllText(main,"{}",Encoding.UTF8);
   Check(Store.LoadSchedules().Plans.Count==1&&File.ReadAllText(main)==saved&&Directory.GetFiles(Store.Root,"schedules.unreadable-*.json").Length==1,"A damaged schedule is quarantined, then recovered");
   File.Delete(backup);
   Check(Store.LoadSchedules().Plans.Count==1&&File.ReadAllText(backup)==saved,"An existing schedule without a recovery copy is backed up on startup");
   using(var held=new FileStream(backup,FileMode.Open,FileAccess.Read,FileShare.Read)){
    var release=Task.Run(()=>{Thread.Sleep(35);held.Dispose();});
    book.Plans.Clear();Store.Save("schedules.json",book);release.GetAwaiter().GetResult();
   }
   File.Delete(main);
   Check(Store.LoadSchedules().Plans.Count==0,"Recovery does not resurrect an intentionally deleted plan");
   File.WriteAllText(main,"{");File.WriteAllText(backup,"{");
   bool rejected=false;try{Store.LoadSchedules();}catch(IOException){rejected=true;}
   Check(rejected&&File.ReadAllText(main)=="{"&&File.ReadAllText(backup)=="{","Two unreadable schedule files cannot be replaced with a silent empty book");
  }finally{Store.Root=root;}
 }
 static PlanProposal PlanFixture(PlanRequest req,string prefix){return new PlanProposal{summary="先分解目标，再逐步练习和验收。",days=req.Dates.Select((date,index)=>new PlanProposalDay{date=date,title=prefix+" · "+(index+1),notes="整理本日材料，完成一项练习并记录结果。",minutes=20}).ToList()};}
 static void FinishPlannerTask(Task task){var watch=Stopwatch.StartNew();while(!task.IsCompleted&&watch.ElapsedMilliseconds<4000){Application.DoEvents();Thread.Sleep(5);}if(!task.IsCompleted)throw new TimeoutException("Planner test continuation did not return to the UI thread");task.GetAwaiter().GetResult();}
 sealed class PlannerTestContext:SynchronizationContext {
  readonly Control target;
  public PlannerTestContext(Control control){target=control;}
  public override void Post(SendOrPostCallback callback,object state){target.BeginInvoke((Action)(()=>callback(state)));}
 }
 static void PlannerScreenshot(Form form,string folder,string name){using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,image.Width,image.Height));image.Save(Path.Combine(folder,name),ImageFormat.Png);}}
 static void ChooseEarlyCompletion(SoftCheck check,string option,Action<Form> beforeChoosing=null){
  var onClick=typeof(Control).GetMethod("OnClick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
  bool found=false;
  using(var chooser=new System.Windows.Forms.Timer{Interval=10}){
   chooser.Tick+=(s,e)=>{
    var dialog=Application.OpenForms.OfType<EarlyPlanCompletionDialog>().SingleOrDefault();
    if(dialog==null)return;
    chooser.Stop();
    var action=FindText(dialog,option);if(action==null){dialog.Close();return;}
    found=true;
    if(beforeChoosing!=null)beforeChoosing(dialog);
    onClick.Invoke(action,new object[]{EventArgs.Empty});
   };
   var previous=SynchronizationContext.Current;
   SynchronizationContext.SetSynchronizationContext(new PlannerTestContext(check.FindForm()));
   try{chooser.Start();onClick.Invoke(check,new object[]{EventArgs.Empty});}
   finally{SynchronizationContext.SetSynchronizationContext(previous);}
  }
  Check(found,"The early completion checkbox offers the requested choice");
 }
 static void TestDailyPlanPages(Controller controller,Dashboard panel,string folder){
  var original=controller.Schedules;
  var book=new ScheduleBook();
  var first=DateTime.Today;
  var target=new GoalPlan{Goal="年度计划分页验证",Deadline=GoalPlan.DateKey(first.AddDays(364)),NeedsReplan=false};
  for(int i=364;i>=0;i--)target.Days.Add(new PlanDay{Date=GoalPlan.DateKey(first.AddDays(i)),Title="每日任务 "+(i+1),Minutes=20,Topics=new[]{"年度技能"}});
  for(int i=1;i<=9;i++)book.Plans.Add(new GoalPlan{Goal="分页目标 "+i});
  book.Plans.Add(target);
  controller.Schedules=book;controller.ScheduleVersion++;
  try{
   panel.Navigate("plans");
   var plans=Descendants(panel).OfType<FlowLayoutPanel>().Single();
   Check(plans.Controls.Count==8&&FindText(panel,"第 1 / 2 页 · 共 10 项")!=null&&FindText(plans,"年度计划分页验证")!=null,"Goal catalog builds only the current page and retains newest-first order");
   AdaptiveClick(FindText(panel,"下一页"));
   Check(plans.Controls.Count==2&&FindText(plans,"分页目标 1")!=null,"Goal catalog can reach the oldest goals on the last page");
   AdaptiveClick(FindText(panel,"上一页"));
   var watch=Stopwatch.StartNew();
   AdaptiveClick(FindText(plans.Controls[0],"查看每日计划"));
   panel.Update();watch.Stop();
   File.WriteAllText(Path.Combine(folder,"daily-plan-open-time.txt"),"365 days, goal click to render: "+watch.ElapsedMilliseconds+" ms");
   var days=Descendants(panel).OfType<FlowLayoutPanel>().Single();
   Check(days.Controls.Count==8&&FindText(days,"每日任务 1")!=null&&FindText(days,"每日任务 9")==null,"Opening a 365-day plan builds only eight days in chronological order");
   Check(FindText(panel,"第 1 / 46 页 · 共 365 项")!=null&&NoSiblingOverlap(panel),"Daily plan count and pagination are visible without overlapping controls");
   AdaptiveClick(FindText(panel,"下一页"));
   Check(days.Controls.Count==8&&FindText(days,"每日任务 9")!=null,"Daily plan navigation opens the next date range");
   var ninth=Descendants(days).OfType<SoftCheck>().First();
   ChooseEarlyCompletion(ninth,"取消");
   Check(!ninth.Checked&&ninth.Text=="未完成"&&ninth.Enabled&&!controller.FindPlan(target.Id).Days.Single(day=>day.Title=="每日任务 9").Done,"Cancelling the early completion choice restores the pending checkbox and caption");
   bool optimistic=false;
   ChooseEarlyCompletion(ninth,"完成并调整相关任务",dialog=>{
    PlannerScreenshot(dialog,folder,"ui-plan-early-completion.png");
    optimistic=ninth.Checked&&!ninth.Enabled;watch.Restart();
   });
   long clickTime=watch.ElapsedMilliseconds;
   Check(optimistic,"An early click checks the card immediately and disables repeated clicks while the choice is active");
   while(!controller.FindPlan(target.Id).Days.Single(day=>day.Title=="每日任务 9").Done&&watch.ElapsedMilliseconds<4000){Application.DoEvents();Thread.Sleep(5);}
   panel.Update();watch.Stop();
   File.WriteAllText(Path.Combine(folder,"early-completion-time.txt"),"365 tasks, 356 related moves: click handler "+clickTime+" ms, click to persisted plan and render "+watch.ElapsedMilliseconds+" ms");
   Check(FindText(panel,"第 2 / 46 页 · 共 365 项")!=null&&Descendants(days).OfType<SoftCheck>().Any(check=>check.Checked&&check.Text=="已完成")&&controller.FindPlan(target.Id).Days.Single(day=>day.Title=="每日任务 9").Done,"Marking a task complete updates its caption and retains the daily plan page");
   Check(Store.Load<ScheduleBook>("schedules.json").Plans.Single(p=>p.Id==target.Id).Days.Single(day=>day.Title=="每日任务 365").Day==first.AddDays(363),"A long advance reaches the last related day and persists it immediately");
   Application.DoEvents();
   var completedCheck=Descendants(days).OfType<SoftCheck>().Single(check=>check.Checked);
   AdaptiveClick(completedCheck);
   var pendingCheck=Descendants(days).OfType<SoftCheck>().First();
   var restoredDay=Store.LoadSchedules().Plans.Single(p=>p.Id==target.Id).Days.Single(day=>day.Title=="每日任务 9");
   Check(!pendingCheck.Checked&&pendingCheck.Text=="未完成"&&!restoredDay.Done&&restoredDay.CompletedAt=="","Undoing completion from the daily plan resets the checkbox caption and persisted completion time");
   var shortened=controller.FindPlan(target.Id);
   shortened.Days.RemoveAll(day=>day.Day>=first.AddDays(8));controller.ScheduleVersion++;
   panel.UpdateLive();Application.DoEvents();
   Check(days.Controls.Count==8&&!FindText(panel,"下一页").Enabled&&FindText(panel,"第 1 / 1 页 · 共 8 项")!=null,"Shortening a plan returns to the last available page");
  }finally{controller.Schedules=original;controller.ScheduleVersion++;Store.Save("schedules.json",original);panel.Navigate("settings");}
 }
 static void TestRelatedEarlyCompletion(Controller controller,Dashboard panel){
  var original=controller.Schedules;
  var generator=controller.PlanGenerator;
  int generationCalls=0;
  controller.PlanGenerator=(exe,req,cancel,progress)=>{generationCalls++;return Task.FromResult(PlanFixture(req,"生成验证"));};
  var now=DateTime.Today.AddHours(10);
  var plan=new GoalPlan{Goal="算法学习",Deadline=GoalPlan.DateKey(now.AddDays(8)),NeedsReplan=false};
  var today=new PlanDay{Date=GoalPlan.DateKey(now),Title="今天的任务"};
  var learn=new PlanDay{Date=GoalPlan.DateKey(now.AddDays(2)),Title="学习并查集"};
  var unrelated=new PlanDay{Date=GoalPlan.DateKey(now.AddDays(3)),Title="复习二分查找"};
  var practice=new PlanDay{Date=GoalPlan.DateKey(now.AddDays(4)),Title="手写并查集练习洛谷 P3376",Topics=new[]{"并查集"}};
  var review=new PlanDay{Date=GoalPlan.DateKey(now.AddDays(6)),Title="复习并查集",Topics=new[]{"并查集"}};
  var explicitOther=new PlanDay{Date=GoalPlan.DateKey(now.AddDays(7)),Title="复习并查集相关题",Topics=new[]{"Dijkstra"}};
  plan.Days.AddRange(new[]{today,learn,unrelated,practice,review,explicitOther});
  var second=new GoalPlan{Goal="英语",Deadline=plan.Deadline,NeedsReplan=false};
  second.Days.Add(new PlanDay{Date=learn.Date,Title="听写"});
  var standalone=new ScheduleEntry{Title="固定事件",Start=now.AddDays(2)};
  var book=new ScheduleBook();book.Plans.AddRange(new[]{plan,second});book.Items.Add(standalone);
  PlanEngine.SyncReminders(book,plan,now);PlanEngine.SyncReminders(book,second,now);
  var originalReminder=book.Items.Single(e=>e.PlanDayId==practice.Id);
  controller.Schedules=book;controller.ScheduleVersion++;
  try{
   panel.Navigate("settings");
   var pending=ReportPlanDayForTest(controller,plan.Id,learn.Id,now);
   var concurrent=new ScheduleEntry{Title="重排期间新增的事件",Start=now.AddDays(5)};
   controller.SaveSchedule(concurrent,now);
   FinishPlannerTask(pending);
   var saved=Store.Load<ScheduleBook>("schedules.json");var updated=saved.Plans.Single(p=>p.Id==plan.Id);
   Check(updated.Days.Single(d=>d.Id==learn.Id).CompletedEarly&&updated.Days.Single(d=>d.Id==practice.Id).Date==learn.Date&&updated.Days.Single(d=>d.Id==review.Id).Date==practice.Date&&generationCalls==0,"Early completion records actual time and advances only matching practice and review without running the generator");
   Check(updated.Days.Single(d=>d.Id==today.Id).Date==today.Date&&updated.Days.Single(d=>d.Id==unrelated.Id).Date==unrelated.Date&&updated.Deadline==plan.Deadline&&saved.Plans.Single(p=>p.Id==second.Id).Days.Single().Date==learn.Date&&saved.Items.Single(e=>e.Id==standalone.Id).Start==standalone.Start,"Today, unrelated tasks, the original deadline, other plans and standalone schedules keep their dates");
   Check(saved.Items.Any(entry=>entry.Id==concurrent.Id),"A background plan save retains a schedule added while dates were being prepared");
   var movedReminder=saved.Items.Single(e=>e.PlanDayId==practice.Id);
   Check(!saved.Items.Single(e=>e.PlanDayId==learn.Id).Enabled&&movedReminder.Id==originalReminder.Id&&movedReminder.Start==learn.Day.AddMinutes(plan.ReminderMinutes)&&movedReminder.Enabled,"Completing early disables its own reminder and atomically retimes linked reminders with stable IDs");
   controller.UndoPlanCompletion(plan.Id,learn.Id,now);
   updated=controller.FindPlan(plan.Id);
   Check(!updated.Days.Single(d=>d.Id==learn.Id).Done&&updated.Days.Single(d=>d.Id==practice.Id).Date==practice.Date&&updated.Days.Single(d=>d.Id==review.Id).Date==review.Date&&updated.Days.Single(d=>d.Id==unrelated.Id).Date==unrelated.Date,"Undo restores the related slots without shifting an unrelated day between them");
   FinishPlannerTask(ReportPlanDayForTest(controller,plan.Id,practice.Id,now));
   updated=controller.FindPlan(plan.Id);
   Check(updated.Days.Single(d=>d.Id==review.Id).Date==practice.Date&&updated.Days.Single(d=>d.Id==explicitOther.Id).Date==explicitOther.Date,"Explicit topic tags distinguish a separate topic even when its title contains similar text");
   var later=review.Day.AddMinutes(plan.ReviewMinutes);
   Check(PlanEngine.CollectReviews(controller.Schedules.Copy(),later).All(ticket=>ticket.DayId!=review.Id)&&PlanEngine.CollectReviews(controller.Schedules.Copy(),practice.Day.AddMinutes(plan.ReviewMinutes)).Any(ticket=>ticket.DayId==review.Id),"Evening review follows the moved day instead of the old date");
   controller.UndoPlanCompletion(plan.Id,practice.Id,now);
   FinishPlannerTask(ReportPlanDayForTest(controller,plan.Id,learn.Id,now,advanceRelated:false));
   saved=Store.Load<ScheduleBook>("schedules.json");updated=saved.Plans.Single(p=>p.Id==plan.Id);
   Check(updated.Days.Single(d=>d.Id==learn.Id).KeepEarlyDateFree&&updated.Days.Single(d=>d.Id==practice.Id).Date==practice.Date&&updated.Days.Single(d=>d.Id==review.Id).Date==review.Date,"Rest choice records early completion while all other tasks retain their exact dates");
   Check(!PlanEngine.Request(updated,now,"重排").Dates.Contains(learn.Date)&&!saved.Items.Single(e=>e.PlanDayId==learn.Id).Enabled&&PlanEngine.CollectReviews(saved,learn.Day.AddMinutes(plan.ReviewMinutes)).All(ticket=>ticket.DayId!=learn.Id),"A chosen rest day stays free of tasks, reminders and reviews during a later regeneration");
   controller.UndoPlanCompletion(plan.Id,learn.Id,now);
   Check(!controller.FindPlan(plan.Id).Days.Single(d=>d.Id==learn.Id).KeepEarlyDateFree&&controller.Schedules.Items.Single(e=>e.PlanDayId==learn.Id).Enabled,"Undo clears the rest-day choice and restores the original reminder");
  }finally{controller.PlanGenerator=generator;controller.Schedules=original;controller.ScheduleVersion++;Store.Save("schedules.json",original);}
 }
 static Task ReportPlanDayForTest(Controller controller,string planId,string dayId,DateTime now,bool advanceRelated=true){
  var previous=SynchronizationContext.Current;
  SynchronizationContext.SetSynchronizationContext(new PlannerTestContext(controller.Panel));
  try{return controller.ReportPlanDay(planId,dayId,true,"",now,advanceRelated);}
  finally{SynchronizationContext.SetSynchronizationContext(previous);}
 }
 static void TestPlanning(Controller controller,Dashboard panel,string folder){
  // Self-tests pump messages manually instead of running the application loop.
  var previousContext=SynchronizationContext.Current;
  SynchronizationContext.SetSynchronizationContext(new PlannerTestContext(panel));
  try{TestPlanningOnUiThread(controller,panel,folder);}
  finally{SynchronizationContext.SetSynchronizationContext(previousContext);}
 }
 static void TestPlanningOnUiThread(Controller controller,Dashboard panel,string folder){
  var previousBook=controller.Schedules;var previousGenerator=controller.PlanGenerator;var history=controller.History.ToList();bool quiet=controller.Settings.Quiet;controller.Settings.Quiet=true;
  try{
   var now=DateTime.Today.AddHours(8);var draft=new GoalPlan{Goal="用四天完成一份英语自我介绍",Deadline=GoalPlan.DateKey(now.AddDays(3)),DailyMinutes=30,ReviewMinutes=20*60+15};var calls=new List<PlanRequest>();
   controller.PlanGenerator=(exe,request,cancel,progress)=>{calls.Add(request);return Task.FromResult(PlanEngine.Parse(Json.Encode(PlanFixture(request,"每日口语练习")),request));};
   var created=controller.CreateGoalPlan(draft,now);FinishPlannerTask(created);string id=created.Result;var plan=controller.FindPlan(id);
   Check(plan!=null&&!plan.NeedsReplan&&plan.Days.Count==4&&controller.Schedules.Items.Count(e=>e.PlanId==id)==4&&controller.Schedules.Items.Count(e=>e.PlanId=="")==previousBook.Items.Count(e=>e.PlanId==""),"Creating a goal uses the generator and inserts one linked reminder per date without touching existing schedules");
   Check(Store.Load<ScheduleBook>("schedules.json").Plans.Single(p=>p.Id==id).Days.Count==4&&plan.Days.Last().Date==draft.Deadline,"Generated daily tasks and deadline persist with their schedules in one file");
   var first=plan.Days[0];var review=now.Date.AddMinutes(draft.ReviewMinutes);int priorHistory=controller.History.Count;
   controller.PollPlanReviews(review.AddSeconds(-1));Check(controller.PendingPlanReviews.Count==0&&controller.History.Count==priorHistory,"Evening check-in does not fire before the selected local time");
   controller.PollPlanReviews(review);controller.PollPlanReviews(review.AddSeconds(5));var persisted=Store.Load<ScheduleBook>("schedules.json");
   Check(controller.PendingPlanReviews.Count==1&&controller.History.Count==priorHistory+1&&PlanEngine.CollectReviews(persisted,review.AddMinutes(1)).Count==0,"One evening prompt is delivered at the selected time and deduplicated across polling and reload");
   controller.PendingPlanReviews.Clear();controller.RestorePendingPlanReviews(review.AddMinutes(1));controller.RestorePendingPlanReviews(review.AddMinutes(2));Check(controller.PendingPlanReviews.Count==1&&controller.History.Count==priorHistory+1,"Unanswered review remains available after restart without replaying its notification");
   using(var popup=new PlanReviewDialog(controller)){popup.StartPosition=FormStartPosition.Manual;popup.Location=new Point(-3000,-3000);popup.Show();Application.DoEvents();Check(NoSiblingOverlap(popup),"Evening review keeps both answers, feedback field and footer clear");PlannerScreenshot(popup,folder,"ui-planner-review.png");AdaptiveClick(FindText(popup,"已完成 ✓"));Application.DoEvents();}
   plan=controller.FindPlan(id);var complete=plan.Days.Single(d=>d.Id==first.Id);string stamp=complete.CompletedAt;Check(complete.Done&&stamp!=""&&!controller.Schedules.Items.Single(e=>e.PlanDayId==first.Id).Enabled&&controller.PendingPlanReviews.Count==0,"Clicking completed records the day and disables its outstanding reminder");
   panel.Navigate("schedules");Application.DoEvents();
   Func<SoftCheck> findScheduledDay=()=>Descendants(panel).OfType<SoftCheck>().SingleOrDefault(c=>c.AccessibleName==first.Date+"日程完成状态");
   Func<SoftCheck> findOnAnyPage=()=>{
    SoftCheck match;
    while((match=findScheduledDay())==null){
     var next=FindText(panel,"下一页");if(!next.Enabled)break;AdaptiveClick(next);
    }
    return match;
   };
   var checkedRow=findOnAnyPage();
   Check(checkedRow!=null&&checkedRow.Checked&&checkedRow.Text=="已完成","The schedule table shows a checked completion box and completed caption");PlannerScreenshot(panel,folder,"ui-schedule-planner-checked.png");
   AdaptiveClick(checkedRow);
   var uncheckedRow=findScheduledDay();
   if(uncheckedRow==null){
    panel.Navigate("schedules");
    uncheckedRow=findOnAnyPage();
   }
   Check(uncheckedRow!=null&&!uncheckedRow.Checked&&uncheckedRow.Text=="未完成","Undoing from the schedule table restores the pending caption");
   AdaptiveClick(uncheckedRow);
   plan=controller.FindPlan(id);stamp=plan.Days.Single(d=>d.Id==first.Id).CompletedAt;
   var second=plan.Days[1];var secondReview=review.AddDays(1);FinishPlannerTask(controller.ReportPlanDay(id,second.Id,false,"没有练完开场，明天拆成两小段",secondReview));plan=controller.FindPlan(id);var feedback=calls.Last();
   Check(plan.Days.Single(d=>d.Id==first.Id).CompletedAt==stamp&&plan.Days.Single(d=>d.Id==second.Id).State=="missed"&&feedback.Dates.First()==GoalPlan.DateKey(secondReview.AddDays(1))&&feedback.Plan.LastFeedback.Contains("两小段"),"Unfinished feedback reaches the generator, preserves the checked history and only replaces remaining dates");
   var earlyEdit=PlanEngine.Request(plan,secondReview.Date.AddHours(10),"deadline changed earlier the same day");Check(!earlyEdit.Dates.Contains(second.Date)&&earlyEdit.Plan.Days.Any(d=>d.Id==second.Id&&d.State=="missed"),"A same-day deadline change does not replace a date that already has explicit unfinished feedback");
   var changed=plan.Copy();changed.Deadline=GoalPlan.DateKey(now.AddDays(5));FinishPlannerTask(controller.SaveGoalSettings(changed,secondReview));plan=controller.FindPlan(id);
   Check(plan.Deadline==changed.Deadline&&plan.Days.Last().Date==changed.Deadline&&controller.Schedules.Items.Any(e=>e.PlanId==id&&e.Start.Date==plan.End)&&plan.Days.Single(d=>d.Id==first.Id).Done,"Extending the deadline automatically generates and imports the additional dates while retaining completion");
   var stale=new TaskCompletionSource<PlanProposal>();PlanRequest staleRequest=null;
   controller.PlanGenerator=(exe,req,cancel,progress)=>{staleRequest=req;return stale.Task;};var staleTask=controller.RegenerateGoalPlan(id,"old request",secondReview);
   controller.PlanGenerator=(exe,req,cancel,progress)=>Task.FromResult(PlanFixture(req,"新期限安排"));changed=controller.FindPlan(id).Copy();changed.Deadline=GoalPlan.DateKey(now.AddDays(2));FinishPlannerTask(controller.SaveGoalSettings(changed,secondReview));stale.SetResult(PlanFixture(staleRequest,"过期结果"));FinishPlannerTask(staleTask);plan=controller.FindPlan(id);
   Check(!staleTask.Result&&plan.Deadline==changed.Deadline&&plan.Days.Last().Title.StartsWith("新期限安排")&&controller.Schedules.Items.Where(e=>e.PlanId==id&&e.Enabled).All(e=>e.Start.Date<=plan.End)&&plan.Days.Single(d=>d.Id==first.Id).CompletedAt==stamp,"A shortened deadline removes stale future reminders and an in-flight older response cannot overwrite it");
   controller.PlanGenerator=(exe,req,cancel,progress)=>Task.FromResult(new PlanProposal());var failed=controller.RegenerateGoalPlan(id,"invalid partial result",secondReview);FinishPlannerTask(failed);plan=controller.FindPlan(id);
   Check(!failed.Result&&plan.NeedsReplan&&plan.Error!=""&&plan.Days.Any(d=>d.Id==first.Id&&d.Done)&&!controller.Schedules.Items.Any(e=>e.PlanId==id&&e.Enabled),"Incomplete Codex output cannot write partial reminders or erase completion history; replan remains retryable");
   controller.PlanGenerator=(exe,req,cancel,progress)=>Task.FromResult(PlanFixture(req,"最终安排"));FinishPlannerTask(controller.RegenerateGoalPlan(id,"retry",secondReview));
   foreach(string page in new[]{"plans","plan-detail"}){if(page=="plans")panel.Navigate(page);else panel.OpenPlan(id);Application.DoEvents();Check(NoSiblingOverlap(panel),"Planner page controls do not overlap: "+page);PlannerScreenshot(panel,folder,"ui-"+page+".png");}
   using(var editor=new GoalPlanDialog(controller.FindPlan(id))){editor.StartPosition=FormStartPosition.Manual;editor.Location=new Point(-3000,-3000);editor.Show();Application.DoEvents();Check(NoSiblingOverlap(editor),"Goal, deadline and both adjustable reminder times are visible without overlapping");PlannerScreenshot(editor,folder,"ui-plan-editor.png");var time=Descendants(editor).OfType<SoftNumber>().Single(c=>c.AccessibleName=="晚间询问分钟");time.Value=35;AdaptiveClick(FindText(editor,"保存修改"));Check(editor.Result!=null&&editor.Result.ReviewMinutes==20*60+35,"Editing the evening question time retains the chosen local hour and minute");}
   var final=controller.FindPlan(id);var deadline=final.End.AddMinutes(final.ReviewMinutes);bool noDates=false;try{PlanEngine.Request(final,deadline,"last day unfinished",final.End);}catch(ArgumentException){noDates=true;}Check(noDates,"No plan is silently extended past its deadline when the last day is unfinished");
  }finally{controller.PlanGenerator=previousGenerator;controller.Schedules=previousBook;controller.ScheduleVersion++;Store.Save("schedules.json",previousBook);controller.PendingPlanReviews.Clear();controller.History=history;Store.Save("history.json",history);controller.Settings.Quiet=quiet;panel.Navigate("settings");}
  TestDailyPlanPages(controller,panel,folder);
  TestRelatedEarlyCompletion(controller,panel);
 }
}
}
