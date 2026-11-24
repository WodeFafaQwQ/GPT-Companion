using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;

namespace DragonCompanion {
public sealed class ScheduleEntry {
 public string Id=Guid.NewGuid().ToString("N"),Title="",Notes="",LocalStart="",Repeat="once",LastHandled="";
 public string PlanId="",PlanDayId="";
 public bool Enabled=true;
 public int[] WeeklyDays=new int[0];
 public static readonly string[] RepeatKeys={"once","daily","weekly","monthly","yearly"};
 public static readonly string[] RepeatNames={"单次","每天","每周","每月","每年"};
 public static string Stamp(DateTime value){return value.ToString("yyyy-MM-dd'T'HH:mm:ss",CultureInfo.InvariantCulture);}
 public static DateTime? Parse(string value){DateTime result;return DateTime.TryParseExact(value,"yyyy-MM-dd'T'HH:mm:ss",CultureInfo.InvariantCulture,DateTimeStyles.None,out result)?DateTime.SpecifyKind(result,DateTimeKind.Unspecified):(DateTime?)null;}
 [ScriptIgnore]public DateTime Start {get{return Parse(LocalStart)??DateTime.MinValue;}set{LocalStart=Stamp(value);}}
 [ScriptIgnore]public int[] Days {get{return WeeklyDays!=null&&WeeklyDays.Any(d=>d>=0&&d<7)?WeeklyDays.Where(d=>d>=0&&d<7).Distinct().OrderBy(d=>(d+6)%7).ToArray():new[]{(int)Start.DayOfWeek};}}
 public ScheduleEntry Copy(){var copy=(ScheduleEntry)MemberwiseClone();copy.WeeklyDays=WeeklyDays==null?null:(int[])WeeklyDays.Clone();return copy;}
 public void Validate(DateTime now){if(string.IsNullOrWhiteSpace(Title))throw new ArgumentException("请填写事件名称。");if(Title.Length>120||Notes!=null&&Notes.Length>1000)throw new ArgumentException("事件最多 120 字，备注最多 1000 字。");if(!Parse(LocalStart).HasValue||!RepeatKeys.Contains(Repeat))throw new ArgumentException("请选择有效的日期、时间和重复方式。");if(Repeat=="once"&&Start<=now)throw new ArgumentException("单次提醒请选择未来的时间。");if(Repeat=="weekly"&&(WeeklyDays==null||!WeeklyDays.Any(d=>d>=0&&d<7)))throw new ArgumentException("请至少选择一个星期。");}
 DateTime At(DateTime date){return date.Date.Add(Start.TimeOfDay);}
 DateTime Monthly(int year,int month){return new DateTime(year,month,Math.Min(Start.Day,DateTime.DaysInMonth(year,month))).Add(Start.TimeOfDay);}
 DateTime Yearly(int year){return Monthly(year,Start.Month);}
 public DateTime? Latest(DateTime now){if(Start==DateTime.MinValue||now<Start)return null;DateTime candidate;
  switch(Repeat){
   case "daily":candidate=At(now);if(candidate>now)candidate=candidate.AddDays(-1);break;
   case "weekly":return Days.Select(day=>At(now.Date.AddDays(-(((int)now.DayOfWeek-day+7)%7)))).Select(d=>d>now?d.AddDays(-7):d).Where(d=>d>=Start).Select(d=>(DateTime?)d).OrderByDescending(d=>d).FirstOrDefault();
   case "monthly":candidate=Monthly(now.Year,now.Month);if(candidate>now){var previous=new DateTime(now.Year,now.Month,1).AddMonths(-1);candidate=Monthly(previous.Year,previous.Month);}break;
   case "yearly":candidate=Yearly(now.Year);if(candidate>now)candidate=Yearly(now.Year-1);break;
   case "once":candidate=Start;break;
   default:return null;
  }return candidate>=Start?(DateTime?)candidate:null;
 }
 public DateTime? Next(DateTime after){if(!Enabled||Start==DateTime.MinValue)return null;if(after<Start){if(Repeat=="weekly")after=Start.AddTicks(-1);else return Start;}DateTime candidate;
  switch(Repeat){
   case "daily":candidate=At(after);if(candidate<=after)candidate=candidate.AddDays(1);return candidate;
   case "weekly":return Days.Select(day=>At(after.Date.AddDays((day-(int)after.DayOfWeek+7)%7))).Select(d=>d<=after?d.AddDays(7):d).Where(d=>d>=Start).OrderBy(d=>d).Select(d=>(DateTime?)d).FirstOrDefault();
   case "monthly":candidate=Monthly(after.Year,after.Month);if(candidate<=after){var next=new DateTime(after.Year,after.Month,1).AddMonths(1);candidate=Monthly(next.Year,next.Month);}return candidate;
   case "yearly":candidate=Yearly(after.Year);return candidate<=after?Yearly(after.Year+1):candidate;
   default:return null;
  }
 }
 [ScriptIgnore]public string RuleLabel {get{var index=Array.IndexOf(RepeatKeys,Repeat);string rule=index<0?"未知":RepeatNames[index];if(Repeat=="weekly")rule+=" · "+string.Join("、",Days.Select(d=>new[]{"周日","周一","周二","周三","周四","周五","周六"}[d]));else if(Repeat=="monthly")rule+=" · "+Start.Day+" 日";else if(Repeat=="yearly")rule+=" · "+Start.ToString("MM-dd");else if(Repeat=="once")rule+=" · "+Start.ToString("yyyy-MM-dd");return rule+" · "+Start.ToString("HH:mm");}}
}
public sealed class ScheduleOccurrence {public ScheduleEntry Entry;public DateTime Time;}
public sealed class ScheduleBook {
 public List<GoalPlan> Plans=new List<GoalPlan>();
 public List<ScheduleEntry> Items=new List<ScheduleEntry>();
 public void Normalize(){Items=(Items??new List<ScheduleEntry>()).Where(x=>x!=null&&ScheduleEntry.Parse(x.LocalStart).HasValue&&ScheduleEntry.RepeatKeys.Contains(x.Repeat)).GroupBy(x=>x.Id).Select(g=>g.First()).ToList();Plans=(Plans??new List<GoalPlan>()).Where(p=>p!=null&&!string.IsNullOrEmpty(p.Id)).GroupBy(p=>p.Id).Select(g=>g.First()).ToList();foreach(var p in Plans){p.Days=(p.Days??new List<PlanDay>()).Where(d=>d!=null&&GoalPlan.ParseDate(d.Date).HasValue).GroupBy(d=>d.Id).Select(g=>g.First()).ToList();}}
 public ScheduleBook Copy(){return new ScheduleBook{Plans=Plans==null?null:Plans.Select(plan=>plan==null?null:plan.Copy()).ToList(),Items=Items==null?null:Items.Select(entry=>entry==null?null:entry.Copy()).ToList()};}
 static DateTime? UnhandledOccurrence(ScheduleEntry entry,DateTime now){
  if(!entry.Enabled)return null;
  var candidate=entry.Latest(now);
  if(!candidate.HasValue)return null;
  var handled=ScheduleEntry.Parse(entry.LastHandled);
  return handled.HasValue&&candidate.Value<=handled.Value?null:candidate;
 }
 internal bool HasUnhandledOccurrence(DateTime now){return Items.Any(entry=>UnhandledOccurrence(entry,now).HasValue);}
 public List<ScheduleOccurrence> CollectDue(DateTime now,out bool changed){changed=false;var due=new List<ScheduleOccurrence>();foreach(var item in Items){var candidate=UnhandledOccurrence(item,now);if(!candidate.HasValue)continue;item.LastHandled=ScheduleEntry.Stamp(candidate.Value);if(item.Repeat=="once")item.Enabled=false;changed=true;if(now-candidate.Value<=TimeSpan.FromHours(24))due.Add(new ScheduleOccurrence{Entry=item.Copy(),Time=candidate.Value});}return due.OrderBy(x=>x.Time).ToList();}
}
}
