using System;
using System.Linq;
using System.Windows.Forms;

namespace DragonCompanion {
public sealed partial class Controller {
 public async void OpenVocabularyCards(string deckId){
  try{await LoadVocabulary();ShowPanel("vocabulary");Panel.StartVocabulary(deckId);}
  catch(Exception ex){if(!closing)MessageBox.Show(Panel,ex.Message+"\n\n"+Theme.Credit,"单词卡片");}
 }
 public void SaveVocabularyGoal(string deckId,VocabularyGoal goal,DateTime now){
  Vocabulary.SaveGoal(deckId,goal,now);
  SyncVocabularyPlan(deckId,now,resume:true);
 }
 public string SyncVocabularyPlan(string deckId,DateTime now,bool resume=false){
  var goal=Vocabulary.Goal(deckId);var book=Schedules.Copy();
  var plan=book.Plans.FirstOrDefault(p=>p.VocabularyDeckId==deckId);
  if(!goal.LinkPlan){
   if(plan!=null&&plan.Enabled){plan.Enabled=false;PlanEngine.SyncReminders(book,plan,now);SavePlanBook(book);RefreshPlanUi();}
   return plan==null?"":plan.Id;
  }
  var deck=Vocabulary.FindDeck(deckId);if(deck==null)return "";
  if(plan==null){
   plan=new GoalPlan{VocabularyDeckId=deckId,Goal=deck.Name+" · 每日单词学习",NeedsReplan=false};
   book.Plans.Add(plan);
  }
  plan.Goal=deck.Name+" · 每日单词学习";
  if(resume)plan.Enabled=true;
  plan.NeedsReplan=false;plan.Error="";plan.Deadline=goal.Deadline;
  plan.ReminderMinutes=goal.ReminderMinutes;plan.ReviewMinutes=goal.ReviewMinutes;
  plan.DailyMinutes=Math.Max(5,Math.Min(720,goal.NewWords*2+goal.ReviewWords));
  plan.Summary="每天新学 "+goal.NewWords+" 词，复习 "+goal.ReviewWords+" 词。单词卡片达到当日目标后自动打勾。";
  plan.Days.RemoveAll(d=>d.Day>plan.End&&!d.Done&&d.State=="pending");
  for(var date=now.Date;date<=plan.End;date=date.AddDays(1)){
   string key=GoalPlan.DateKey(date);
   var day=plan.Days.FirstOrDefault(d=>d.Date==key);
   if(day==null){day=new PlanDay{Date=key};plan.Days.Add(day);}
   if(!day.Done&&day.State=="pending"){
    day.Title="新学 "+goal.NewWords+" 词 · 复习 "+goal.ReviewWords+" 词";
    day.Notes=deck.Name+"。在「实用工具 → 单词卡片」开始今日学习；可用词不足时以实际词数为目标。";
    day.Topics=new[]{"单词学习"};day.Minutes=plan.DailyMinutes;
   }
  }
  plan.Days=plan.Days.OrderBy(d=>d.Date,StringComparer.Ordinal).ToList();
  var status=Vocabulary.DailyStatus(deckId,now.Date);
  var today=plan.Days.FirstOrDefault(d=>d.Date==GoalPlan.DateKey(now));
  if(today!=null&&today.Done&&!status.Complete&&today.Feedback.StartsWith("今日新学",StringComparison.Ordinal)){
   today.State="pending";today.CompletedAt="";today.Feedback="";
   today.Title="新学 "+goal.NewWords+" 词 · 复习 "+goal.ReviewWords+" 词";
  }
  if(today!=null&&!today.Done&&status.Complete){
   today.State="done";today.CompletedAt=ScheduleEntry.Stamp(now);
   today.Feedback=status.Label;
   PendingPlanReviews.RemoveAll(t=>t.PlanId==plan.Id&&t.DayId==today.Id);
  }
  PlanEngine.SyncReminders(book,plan,now);
  SavePlanBook(book);RefreshPlanUi();return plan.Id;
 }
}
}
