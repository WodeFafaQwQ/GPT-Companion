using System;
using System.Drawing;
using System.Windows.Forms;

namespace DragonCompanion {
public sealed class VocabularyGoalDialog:PlannerDialog {
 readonly SoftNumber fresh,review,remindHour,remindMinute,reviewHour,reviewMinute;
 readonly SoftNumber year,month,day;
 readonly SoftCheck linked;
 readonly SurfaceLabel error;
 public VocabularyGoal Result;
 public VocabularyGoalDialog(string name,VocabularyGoal goal):base("每日单词目标",650){
  TextLabel("每日学习目标",24,16,560,40,21,true);
  TextLabel(name,24,65,560,28);
  TextLabel("每天新学",24,115,130,28,11,true);
  fresh=Number("每日新学词数",166,108,146,0,500,goal.NewWords);
  TextLabel("词",330,115,44,28);
  TextLabel("每天复习",24,175,130,28,11,true);
  review=Number("每日复习词数",166,168,146,0,500,goal.ReviewWords);
  TextLabel("词",330,175,44,28);
  TextLabel("复习会从以前学过且尚未掌握的词中选取。",24,224,560,32,9);
  linked=new SoftCheck{Text="加入制定计划和日程提醒",Checked=goal.LinkPlan,Bounds=new Rectangle(24,274,560,34),Font=Theme.Font(11)};
  Body.Controls.Add(linked);
  TextLabel("结束日期",24,329,120,28,11,true);
  var end=GoalPlan.ParseDate(goal.Deadline)??DateTime.Today.AddDays(29);
  if(end<DateTime.Today)end=DateTime.Today.AddDays(29);
  year=Number("单词计划年份",166,322,110,2000,2099,end.Year);
  month=Number("单词计划月份",317,322,76,1,12,end.Month);
  day=Number("单词计划日期",434,322,76,1,31,end.Day);
  TextLabel("年",284,329,25,28);TextLabel("月",401,329,25,28);TextLabel("日",518,329,25,28);
  year.ValueChanged+=(s,e)=>ClampDay();month.ValueChanged+=(s,e)=>ClampDay();ClampDay();
  TextLabel("学习提醒",24,390,130,28,11,true);
  remindHour=Number("单词提醒小时",166,383,88,0,23,goal.ReminderMinutes/60);
  TextLabel("时",266,390,28,28);
  remindMinute=Number("单词提醒分钟",302,383,88,0,59,goal.ReminderMinutes%60);
  TextLabel("分",402,390,28,28);
  TextLabel("晚间询问",24,446,130,28,11,true);
  reviewHour=Number("单词询问小时",166,439,88,0,23,goal.ReviewMinutes/60);
  TextLabel("时",266,446,28,28);
  reviewMinute=Number("单词询问分钟",302,439,88,0,59,goal.ReviewMinutes%60);
  TextLabel("分",402,446,28,28);
  error=TextLabel("",24,490,560,40,9);
  Action("保存目标",24,553,200,Save,true);
  Action("返回",240,553,140,()=>Close());
 }
 void Save(){
  try{
   var goal=new VocabularyGoal{NewWords=(int)fresh.Value,ReviewWords=(int)review.Value,LinkPlan=linked.Checked,Deadline=GoalPlan.DateKey(new DateTime((int)year.Value,(int)month.Value,(int)day.Value)),ReminderMinutes=(int)remindHour.Value*60+(int)remindMinute.Value,ReviewMinutes=(int)reviewHour.Value*60+(int)reviewMinute.Value};
   goal.Validate(DateTime.Now);Result=goal;DialogResult=DialogResult.OK;Close();
  }catch(ArgumentException ex){error.Text=ex.Message;}
 }
 void ClampDay(){if(day!=null)day.Maximum=DateTime.DaysInMonth((int)year.Value,(int)month.Value);}
}
}
