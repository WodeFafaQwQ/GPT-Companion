using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace DragonCompanion {
public sealed partial class Dashboard {
 string selectedPlanId="";int plannerVersion=-1;FlowLayoutPanel plannerList;SurfaceLabel plannerStatus;SoftButton plannerRun,plannerToggle,plannerReview;
 CatalogPager plannerPager;
 readonly Dictionary<string,SurfaceLabel> plannerLabels=new Dictionary<string,SurfaceLabel>();
 public void OpenPlan(string id){selectedPlanId=id;Navigate("plan-detail");}
 static string PlanExcerpt(string text,int length){return string.IsNullOrEmpty(text)?"":text.Length<=length?text:text.Substring(0,length-1)+"…";}
 void ResetPlannerUi(){plannerList=null;plannerPager=null;plannerStatus=null;plannerRun=null;plannerToggle=null;plannerReview=null;plannerVersion=-1;plannerLabels.Clear();}
 void BuildPlans(){Label(content,"制定计划",0,0,490,52,23,Theme.Ink,true);Label(content,"目标拆成每天一件事，晚间反馈后继续调整。",1,58,650,36,10,Theme.Muted);Button(content,"返回实用工具",508,7,150,()=>Navigate("tools"));Button(content,"新建计划",0,104,164,()=>app.NewGoalPlan(),true);plannerReview=Button(content,"晚间复盘",184,104,180,()=>app.ShowPlanReview());Label(content,"完成记录随计划保留。",384,111,265,28,9,Theme.Muted);
  plannerList=CatalogList("目标计划列表",158,404);
  plannerPager=AddCatalogPager(index=>FillPlans(index));
  FillPlans();Label(content,"使用已登录的 Codex 生成；每日提醒和晚间询问时间由你设置。",1,620,650,30,9,Theme.Muted);
 }
 void FillPlans(int? requestedPage=null){
  plannerVersion=app.ScheduleVersion;
  var plans=app.Schedules.Plans;
  bool pageChanged=plannerPager.Update(plans.Count,requestedPage);
  RenderCatalogRows(plannerList,pageChanged,()=>{
   plannerLabels.Clear();
   foreach(var plan in plans.AsEnumerable().Reverse().Skip(plannerPager.Start).Take(CatalogPageSize)){
    var row=new RoundedPanel{Size=new Size(632,224),Margin=new Padding(0,0,0,12)};plannerList.Controls.Add(row);Label(row,PlanExcerpt(plan.Goal,65),20,14,590,46,13,Theme.Ink,true);plannerLabels[plan.Id]=Label(row,app.PlanStatus(plan.Id),20,67,590,44,9,Theme.Purple);Label(row,PlanExcerpt(plan.Summary,120),20,117,590,40,9,Theme.Muted);Button(row,"查看每日计划",20,170,180,()=>OpenPlan(plan.Id),true);Button(row,"修改期限与提醒",214,170,190,()=>app.EditGoalPlan(plan.Id));Button(row,plan.Enabled?"暂停计划":"恢复计划",418,170,190,()=>app.ToggleGoalPlan(plan.Id));
   }
   if(plans.Count==0){var row=new RoundedPanel{Size=new Size(632,160),Margin=Padding.Empty};plannerList.Controls.Add(row);Label(row,"还没有计划",22,23,580,34,16,Theme.Ink,true);Label(row,"填写目标与期限，让 Codex 制定每日安排并自动加入日程提醒。",22,78,580,52,10,Theme.Muted);}
  });
 }
 void BuildPlanDetail(){var plan=app.FindPlan(selectedPlanId);if(plan==null){page="plans";BuildPlans();return;}Label(content,"每日计划",0,0,490,52,23,Theme.Ink,true);Button(content,"返回计划列表",508,7,150,()=>Navigate("plans"));Label(content,PlanExcerpt(plan.Goal,100),1,58,650,50,11,Theme.Ink,true);
  Button(content,"修改期限与提醒",0,122,180,()=>app.EditGoalPlan(plan.Id));plannerRun=Button(content,plan.VocabularyDeckId!=""?"开始学习":app.PlanGenerating(plan.Id)?"停止生成":"重新安排",194,122,160,()=>{if(plan.VocabularyDeckId!=""){app.OpenVocabularyCards(plan.VocabularyDeckId);return;}if(app.PlanGenerating(plan.Id))app.CancelGoalGeneration(plan.Id);else{var unused=app.RegenerateGoalPlan(plan.Id,"用户要求重新安排未完成内容",DateTime.Now);}},true);plannerToggle=Button(content,plan.Enabled?"暂停计划":"恢复计划",368,122,135,()=>app.ToggleGoalPlan(plan.Id));Button(content,"删除计划",516,122,142,()=>app.DeleteGoalPlan(plan.Id));plannerStatus=Label(content,app.PlanStatus(plan.Id),1,171,650,48,10,Theme.Purple);
  plannerList=CatalogList("每日计划表",228,334);
  plannerPager=AddCatalogPager(index=>FillPlanDays(index));
  FillPlanDays();Label(content,"可提前勾选完成，再选择调整相关任务或在原定日休息。",1,620,650,30,9,Theme.Muted);
 }
 void FillPlanDays(int? requestedPage=null){
  var plan=app.FindPlan(selectedPlanId);if(plan==null)return;
  plannerVersion=app.ScheduleVersion;
  bool pageChanged=plannerPager.Update(plan.Days.Count,requestedPage);
  var today=DateTime.Today;
  RenderCatalogRows(plannerList,pageChanged,()=>{
   foreach(var day in plan.Days.OrderBy(d=>d.Date,StringComparer.Ordinal).Skip(plannerPager.Start).Take(CatalogPageSize)){
    var row=new RoundedPanel{Size=new Size(632,210),Margin=new Padding(0,0,0,12)};plannerList.Controls.Add(row);
    bool early=day.CompletedEarly;
    Label(row,day.Date+(early?(day.KeepEarlyDateFree?" · 提前完成 · 当天休息":" · 提前完成"):day.State=="missed"?" · 未完成，已反馈":""),20,14,450,28,12,Theme.Purple,true);
    var done=new SoftCheck{Checked=day.Done,Bounds=new Rectangle(500,13,112,30),Font=Theme.Font(9),AccessibleName=day.Date+(early?"提前完成状态":"完成状态")};row.Controls.Add(done);
    BindPlanDayCheck(done,plan.Id,day.Id);
    Label(row,day.Title,20,54,590,44,12,Theme.Ink,true);Label(row,PlanExcerpt(day.State=="missed"&&day.Feedback!=""?"反馈："+day.Feedback:day.Notes,170),20,106,590,44,9,Theme.Muted);Label(row,"建议 "+day.Minutes+" 分钟"+(day.Done?" · 完成 "+day.CompletedAt.Replace('T',' '):""),20,165,415,28,9,Theme.Muted);var report=Button(row,"未完成，重排",450,158,160,()=>app.AskPlanFeedback(plan.Id,day.Id));report.Enabled=!day.Done&&day.Day<=today;
   }
   if(plan.Days.Count==0){var row=new RoundedPanel{Size=new Size(632,132)};plannerList.Controls.Add(row);Label(row,"正在等待 Codex 的每日安排",22,22,590,34,15,Theme.Ink,true);Label(row,"生成完成后会自动加入日程；可离开此页，或点击上方停止生成。",22,74,590,42,10,Theme.Muted);}
  });
 }
 void BindPlanDayCheck(SoftCheck check,string planId,string dayId){
  Action updateCaption=()=>check.Text=check.Checked?"已完成":"未完成";
  updateCaption();
  bool restoring=false;
  check.CheckedChanged+=async(s,e)=>{
   updateCaption();
   if(restoring)return;
   check.Enabled=false;
   try{
    var now=DateTime.Now;
    if(!check.Checked){app.UndoPlanCompletion(planId,dayId,now);return;}
    var plan=app.FindPlan(planId);var day=plan==null?null:plan.Days.FirstOrDefault(d=>d.Id==dayId);
    bool advance=true;
    if(day!=null&&day.Day>now.Date){
     using(var dialog=new EarlyPlanCompletionDialog(day)){
      if(dialog.ShowDialog(this)!=DialogResult.OK)return;
      advance=dialog.AdvanceRelated;
     }
    }
    await app.ReportPlanDay(planId,dayId,true,"",DateTime.Now,advance);
   }catch(Exception ex){if(!IsDisposed)MessageBox.Show(this,ex.Message+"\n\n"+Theme.Credit,"完成情况未保存");}
   finally{
    if(!check.IsDisposed){
     var plan=app.FindPlan(planId);var day=plan==null?null:plan.Days.FirstOrDefault(d=>d.Id==dayId);
     restoring=true;check.Checked=day!=null&&day.Done;restoring=false;check.Enabled=true;
    }
   }
  };
 }
 void UpdatePlans(){if(page=="plans"&&plannerList!=null){if(plannerVersion!=app.ScheduleVersion)FillPlans();if(plannerReview!=null){plannerReview.Enabled=app.PendingPlanReviews.Count>0;plannerReview.Text=plannerReview.Enabled?"晚间复盘（"+app.PendingPlanReviews.Count+"）":"晚间复盘";}foreach(var pair in plannerLabels)if(!pair.Value.IsDisposed)pair.Value.Text=app.PlanStatus(pair.Key);}else if(page=="plan-detail"&&plannerList!=null){if(plannerVersion!=app.ScheduleVersion)FillPlanDays();if(plannerStatus!=null)plannerStatus.Text=app.PlanStatus(selectedPlanId);if(plannerToggle!=null&&app.FindPlan(selectedPlanId)!=null)plannerToggle.Text=app.FindPlan(selectedPlanId).Enabled?"暂停计划":"恢复计划";if(plannerRun!=null)plannerRun.Text=app.FindPlan(selectedPlanId)!=null&&app.FindPlan(selectedPlanId).VocabularyDeckId!=""?"开始学习":app.PlanGenerating(selectedPlanId)?"停止生成":"重新安排";}}
}
}
