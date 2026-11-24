using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;
namespace DragonCompanion {
public sealed class PlanDay {
 public string Id=Guid.NewGuid().ToString("N"),Date="",Title="",Notes="",State="pending",Feedback="",CompletedAt="";
 public string EarlyVacatedDate="";
 public string[] Topics=new string[0];
 public bool KeepEarlyDateFree;
 public int Minutes;
 [ScriptIgnore] public DateTime Day {get{return GoalPlan.ParseDate(Date)??DateTime.MinValue;}}
 [ScriptIgnore] public bool Done {get{return State=="done";}}
 [ScriptIgnore] public bool CompletedEarly {get{var completed=Done?ScheduleEntry.Parse(CompletedAt):null;return completed.HasValue&&completed.Value.Date<Day;}}
 internal PlanDay Copy(){var copy=(PlanDay)MemberwiseClone();copy.Topics=Topics==null?null:(string[])Topics.Clone();return copy;}
}
public sealed class GoalPlan {
 public string VocabularyDeckId="";
 public string Id=Guid.NewGuid().ToString("N"),Goal="",Deadline="",Summary="",LastAskedDate="",LastFeedback="",Error="";
 public int ReminderMinutes=9*60,ReviewMinutes=21*60,DailyMinutes=60,Revision;
 public bool Enabled=true,NeedsReplan=true;
 public List<PlanDay> Days=new List<PlanDay>();
 [ScriptIgnore] public DateTime End {get{return ParseDate(Deadline)??DateTime.MinValue;}}
 [ScriptIgnore] public int CompletedCount {get{return Days.Count(d=>d.Done);}}
 [ScriptIgnore] public bool Complete {get{return Days.Count>0&&!NeedsReplan&&!Days.Any(d=>d.State=="pending");}}
 public static string DateKey(DateTime day){return day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);}
 public static DateTime? ParseDate(string text){DateTime day;return DateTime.TryParseExact(text,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out day)?DateTime.SpecifyKind(day,DateTimeKind.Unspecified):(DateTime?)null;}
 public GoalPlan Copy(){var copy=(GoalPlan)MemberwiseClone();copy.Days=Days==null?null:Days.Select(day=>day==null?null:day.Copy()).ToList();return copy;}
 public void Validate(DateTime now){
  if(string.IsNullOrWhiteSpace(Goal)||Goal.Length>1000)throw new ArgumentException("请填写目标，最多 1000 字。");
  if(End<now.Date||End>now.Date.AddDays(366))throw new ArgumentException("结束日期请选择今天至未来一年内的日期。");
  if(ReminderMinutes<0||ReminderMinutes>=1440||ReviewMinutes<0||ReviewMinutes>=1440||ReviewMinutes<=ReminderMinutes)throw new ArgumentException("晚间询问时间应晚于每日提醒时间。");
  if(DailyMinutes<5||DailyMinutes>720)throw new ArgumentException("每天可投入时间请选择 5～720 分钟。");
 }
 public DateTime FirstAvailableDate(DateTime now){return now.AddMinutes(5)<now.Date.AddMinutes(ReviewMinutes)?now.Date:now.Date.AddDays(1);}
 public string TimeLabel(int minutes){return (minutes/60).ToString("00")+":"+(minutes%60).ToString("00");}
}
public sealed class PlanProposalDay {public string date="",title="",notes="";public string[] topics=new string[0];public int minutes;}
public sealed class PlanProposal {public string summary="";public List<PlanProposalDay> days=new List<PlanProposalDay>();}
public sealed class PlanRequest {
 public GoalPlan Plan;public DateTime Now,Start;public string Reason="";public List<string> Dates=new List<string>();
 public PlanRequest Batch(IEnumerable<string> dates){return new PlanRequest{Plan=Plan,Now=Now,Start=Start,Reason=Reason,Dates=dates.ToList()};}
}
public sealed class PlanReviewTicket {public string PlanId,DayId;public DateTime Day;}
public static class PlanEngine {
 public static PlanRequest Request(GoalPlan plan,DateTime now,string reason,DateTime? afterMissedDay=null){
  plan.Validate(now);DateTime start=plan.FirstAvailableDate(now);if(afterMissedDay.HasValue&&start<=afterMissedDay.Value.Date)start=afterMissedDay.Value.Date.AddDays(1);
  var reported=new HashSet<string>(plan.Days.Where(d=>d.State=="missed"||d.Done&&ReservesDate(d)).Select(d=>d.Date));var dates=new List<string>();
  for(DateTime date=start;date<=plan.End;date=date.AddDays(1))if(!reported.Contains(GoalPlan.DateKey(date)))dates.Add(GoalPlan.DateKey(date));
  if(dates.Count==0)throw new ArgumentException("剩余期限内没有可安排的日期，请延长结束日期；已完成的记录会保留。");
  return new PlanRequest{Plan=plan.Copy(),Now=now,Start=start,Reason=reason??"",Dates=dates};
 }
 public static PlanProposal Parse(string text,PlanRequest request){
  PlanProposal result;try{
   var raw=Json.Read(text);var rows=Json.Get(raw,"days") as object[];
   if(raw==null||raw.Count!=2||!(Json.Get(raw,"summary") is string)||rows==null)throw new FormatException();
   foreach(var value in rows){
    var row=value as Dictionary<string,object>;var minutes=Json.Num(row,"minutes");
    if(row==null||!(Json.Get(row,"date") is string)||!(Json.Get(row,"title") is string)||!(Json.Get(row,"notes") is string)||!minutes.HasValue||minutes.Value!=Math.Truncate(minutes.Value))throw new FormatException();
    if(row.ContainsKey("topics")){var topics=Json.Get(row,"topics") as object[];if(row.Count!=5||topics==null||topics.Any(topic=>!(topic is string)))throw new FormatException();}
    else if(row.Count!=4)throw new FormatException();
   }
   result=Json.Serializer().Deserialize<PlanProposal>(text);
  }catch(Exception ex){throw new FormatException("Codex 返回的计划格式不完整，未更新日程，请重试。",ex);}
  Validate(result,request);return result;
 }
 public static void Validate(PlanProposal proposal,PlanRequest request){
  if(proposal==null||proposal.days==null||proposal.days.Count!=request.Dates.Count||proposal.summary==null||proposal.summary.Length>1000)throw new FormatException("每日计划数量不完整，未更新日程，请重试。");
  var expected=new HashSet<string>(request.Dates);var seen=new HashSet<string>();
  foreach(var d in proposal.days){if(d==null||!GoalPlan.ParseDate(d.date).HasValue||!expected.Contains(d.date)||!seen.Add(d.date)||string.IsNullOrWhiteSpace(d.title)||d.title.Length>100||d.notes==null||d.notes.Length>800||d.minutes<1||d.minutes>request.Plan.DailyMinutes||d.topics==null||d.topics.Length>5||d.topics.Any(topic=>string.IsNullOrWhiteSpace(topic)||topic.Trim().Length>32))throw new FormatException("每日计划包含重复日期、超量任务或无效内容，未更新日程，请重试。");}
 }
 public static void Apply(GoalPlan plan,PlanRequest request,PlanProposal proposal){
  Validate(proposal,request);if(plan.Revision!=request.Plan.Revision)throw new InvalidOperationException("计划条件已更改，本次旧结果不会覆盖新计划。");
  plan.Days.RemoveAll(d=>d.Day>=request.Start&&d.State=="pending");
  foreach(var day in proposal.days.OrderBy(d=>d.date,StringComparer.Ordinal))plan.Days.Add(new PlanDay{Date=day.date,Title=day.title.Trim(),Notes=day.notes.Trim(),Minutes=day.minutes,Topics=PlanTopics.Clean(day.topics)});
  plan.Days=plan.Days.OrderBy(d=>d.Date,StringComparer.Ordinal).ToList();plan.Summary=proposal.summary.Trim();plan.NeedsReplan=false;plan.Error="";
 }
 // An early completion keeps its original date in history and releases the daily slot.
 static bool ReservesDate(PlanDay day){return !day.CompletedEarly||day.KeepEarlyDateFree;}
 internal static HashSet<string> AdvanceAfterEarlyCompletion(GoalPlan plan,PlanDay completed){
  var moved=new HashSet<string>();
  string slot=completed.Date;
  var related=PlanTopics.RelatedTo(completed);
  foreach(var next in plan.Days.Where(day=>day.State=="pending"&&string.CompareOrdinal(day.Date,completed.Date)>0&&string.CompareOrdinal(day.Date,plan.Deadline)<=0&&related(day)).OrderBy(day=>day.Date,StringComparer.Ordinal)){
   string previous=next.Date;
   next.Date=slot;
   slot=previous;
   moved.Add(next.Id);
  }
  completed.EarlyVacatedDate=moved.Count>0?slot:"";
  return moved;
 }
 internal static HashSet<string> RestoreEarlyCompletion(GoalPlan plan,PlanDay completed,DateTime today){
  var moved=new HashSet<string>();
  var occupied=plan.Days.Where(day=>day.Id!=completed.Id&&ReservesDate(day)).ToLookup(day=>day.Date);
  if(!occupied.Contains(completed.Date))return moved;
  DateTime start=completed.Day>today?completed.Day:today.AddDays(1);
  DateTime end=plan.End;
  var related=PlanTopics.RelatedTo(completed);
  var released=GoalPlan.ParseDate(completed.EarlyVacatedDate);
  var first=occupied[completed.Date].ToArray();
  if(completed.Day==start&&first.Length==1&&first[0].State=="pending"&&related(first[0])&&released.HasValue&&released.Value>=start&&released.Value<=end&&!occupied.Contains(completed.EarlyVacatedDate)){
    var shift=plan.Days.Where(day=>day.State=="pending"&&day.Day>=start&&day.Day<released.Value&&related(day)).OrderBy(day=>day.Date,StringComparer.Ordinal).ToArray();
    string vacant=completed.EarlyVacatedDate;
    for(int i=shift.Length-1;i>=0;i--){var task=shift[i];string previous=task.Date;task.Date=vacant;vacant=previous;moved.Add(task.Id);}
    return moved;
  }
  for(DateTime slot=start;slot<=end;slot=slot.AddDays(1)){
   var date=GoalPlan.DateKey(slot);
   if(occupied.Contains(date))continue;
   completed.Date=date;moved.Add(completed.Id);
   return moved;
  }
  throw new InvalidOperationException("结束日期前没有空余日期可恢复此任务，请先延长计划期限。");
 }
 public static void SyncReminders(ScheduleBook book,GoalPlan plan,DateTime now,bool retime=false,ISet<string> movedDays=null){
  var previous=book.Items.Where(e=>e.PlanId==plan.Id).GroupBy(e=>e.PlanDayId).ToDictionary(g=>g.Key,g=>g.First());
  book.Items.RemoveAll(e=>e.PlanId==plan.Id);
  foreach(var day in plan.Days){
   ScheduleEntry old;previous.TryGetValue(day.Id,out old);
   bool useExistingTime=old!=null&&!retime&&(movedDays==null||!movedDays.Contains(day.Id));
   DateTime at=useExistingTime?old.Start:day.Day.AddMinutes(plan.ReminderMinutes);
   if(!useExistingTime&&day.Day==now.Date&&at<=now)at=now.Date.AddMinutes(Math.Floor(now.TimeOfDay.TotalMinutes)+1);
   bool same=old!=null&&old.Start==at;
   string title=day.Title.Length>100?day.Title.Substring(0,100):day.Title;
   string notes="目标："+plan.Goal+"\n"+day.Notes+"\n建议投入 "+day.Minutes+" 分钟；晚间 "+plan.TimeLabel(plan.ReviewMinutes)+" 询问完成情况。";
   if(notes.Length>1000)notes=notes.Substring(0,997)+"…";
   var entry=new ScheduleEntry{Id=old==null?Guid.NewGuid().ToString("N"):old.Id,Title=title,Notes=notes,Start=at,Repeat="once",PlanId=plan.Id,PlanDayId=day.Id,
    Enabled=plan.Enabled&&!plan.NeedsReplan&&day.State=="pending"&&day.Day<=plan.End&&at>now,
    LastHandled=same?old.LastHandled:""};
   if(same&&old.LastHandled!="")entry.Enabled=false;
   book.Items.Add(entry);
  }
 }
 static PlanDay DueReviewDay(GoalPlan plan,DateTime now){
  if(!plan.Enabled||plan.NeedsReplan)return null;
  DateTime date=now.TimeOfDay.TotalMinutes>=plan.ReviewMinutes?now.Date:now.Date.AddDays(-1);
  DateTime review=date.AddMinutes(plan.ReviewMinutes);string key=GoalPlan.DateKey(date);
  if(now<review||now-review>TimeSpan.FromHours(24)||string.CompareOrdinal(plan.LastAskedDate,key)>=0)return null;
  return plan.Days.FirstOrDefault(d=>d.Date==key&&d.State=="pending");
 }
 internal static bool HasDueReview(ScheduleBook book,DateTime now){return book.Plans.Any(plan=>DueReviewDay(plan,now)!=null);}
 public static List<PlanReviewTicket> CollectReviews(ScheduleBook book,DateTime now){
  var due=new List<PlanReviewTicket>();
  foreach(var plan in book.Plans){
   var day=DueReviewDay(plan,now);if(day==null)continue;
   plan.LastAskedDate=day.Date;due.Add(new PlanReviewTicket{PlanId=plan.Id,DayId=day.Id,Day=day.Day});
  }
  return due;
 }
}
}
