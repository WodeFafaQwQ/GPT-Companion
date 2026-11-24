using System;
using System.Collections.Generic;
using System.Linq;

namespace DragonCompanion {
public sealed class VocabularyGoal {
 public int NewWords=20,ReviewWords=20,ReminderMinutes=18*60,ReviewMinutes=21*60;
 public string Deadline=GoalPlan.DateKey(DateTime.Today.AddDays(29));
 public bool LinkPlan;
 public VocabularyGoal Copy(){return (VocabularyGoal)MemberwiseClone();}
 public void Validate(DateTime now){
  if(NewWords<0||ReviewWords<0||NewWords>500||ReviewWords>500||NewWords+ReviewWords==0)throw new ArgumentException("新学与复习各为 0～500 词，至少一项大于 0。");
  if(ReminderMinutes<0||ReviewMinutes>=1440||ReviewMinutes<=ReminderMinutes)throw new ArgumentException("晚间询问时间应晚于学习提醒。");
  var end=GoalPlan.ParseDate(Deadline);
  if(LinkPlan&&(!end.HasValue||end.Value<now.Date||end.Value>now.Date.AddDays(366)))throw new ArgumentException("结束日期请选择今天至未来一年内。");
 }
}
public sealed class VocabularyDailyRecord {
 public string DeckId="",Date="";
 public List<string> NewWords=new List<string>(),Reviewed=new List<string>();
}
public sealed class VocabularyDailyStatus {
 public int NewTarget,ReviewTarget,NewDone,ReviewDone;
 public bool Complete {get{return NewDone>=NewTarget&&ReviewDone>=ReviewTarget;}}
 public string Label {get{return "今日新学 "+NewDone+" / "+NewTarget+" 词    ·    复习 "+ReviewDone+" / "+ReviewTarget+" 词";}}
}
public sealed partial class VocabularyLibrary {
 public VocabularyGoal Goal(string id){
  VocabularyGoal goal;
  return progress.Goals.TryGetValue(id,out goal)?goal.Copy():new VocabularyGoal();
 }
 public void SaveGoal(string id,VocabularyGoal goal,DateTime now){
  if(FindDeck(id)==null)throw new ArgumentException("词库不存在。");
  goal.Validate(now);var next=progress.Copy();next.Goals[id]=goal.Copy();Commit(next);
 }
 string LearnedOn(string id,string word){
  Dictionary<string,string> learned;string first;
  return progress.Learned.TryGetValue(id,out learned)&&learned.TryGetValue(word,out first)?first:"";
 }
 VocabularyDailyRecord DayRecord(string id,DateTime date){
  return progress.Daily.FirstOrDefault(d=>d.DeckId==id&&d.Date==GoalPlan.DateKey(date))??new VocabularyDailyRecord();
 }
 public VocabularyDailyStatus DailyStatus(string id,DateTime day){
  string date=GoalPlan.DateKey(day);var goal=Goal(id);var record=DayRecord(id,day);
  var available=Words(id).Where(w=>!IsMastered(id,w.Key)).ToArray();
  int unseen=available.Count(w=>LearnedOn(id,w.Key)=="");
  int review=available.Count(w=>LearnedOn(id,w.Key)!=""&&string.CompareOrdinal(LearnedOn(id,w.Key),date)<0&&!record.Reviewed.Contains(w.Key));
  return new VocabularyDailyStatus{
   NewDone=record.NewWords.Count,ReviewDone=record.Reviewed.Count,
   NewTarget=Math.Min(goal.NewWords,record.NewWords.Count+unseen),
   ReviewTarget=Math.Min(goal.ReviewWords,record.Reviewed.Count+review)
  };
 }
 public bool CanStudy(string id,string word,DateTime day){
  if(IsMastered(id,word))return false;
  var record=DayRecord(id,day);
  if(record.NewWords.Contains(word)||record.Reviewed.Contains(word))return false;
  var status=DailyStatus(id,day);string first=LearnedOn(id,word);
  return first==""?status.NewDone<status.NewTarget:
   string.CompareOrdinal(first,GoalPlan.DateKey(day))<0&&status.ReviewDone<status.ReviewTarget;
 }
 public VocabularyWord[] StudyWords(string id,DateTime day){
  var status=DailyStatus(id,day);string date=GoalPlan.DateKey(day);var record=DayRecord(id,day);
  var random=new Random();
  var available=Words(id).Where(w=>!IsMastered(id,w.Key)).OrderBy(w=>random.Next()).ToArray();
  return available.Where(w=>LearnedOn(id,w.Key)=="").Take(Math.Max(0,status.NewTarget-status.NewDone))
   .Concat(available.Where(w=>LearnedOn(id,w.Key)!=""&&string.CompareOrdinal(LearnedOn(id,w.Key),date)<0&&!record.Reviewed.Contains(w.Key)).Take(Math.Max(0,status.ReviewTarget-status.ReviewDone))).ToArray();
 }
 public void RecordStudy(string id,string word,DateTime day,bool mastered){
  if(!CanStudy(id,word,day)){if(mastered)SetMastered(id,word,true);return;}
  var next=progress.Copy();string date=GoalPlan.DateKey(day);
  Dictionary<string,string> learned;
  if(!next.Learned.TryGetValue(id,out learned))next.Learned.Add(id,learned=new Dictionary<string,string>());
  var record=next.Daily.FirstOrDefault(d=>d.DeckId==id&&d.Date==date);
  if(record==null){record=new VocabularyDailyRecord{DeckId=id,Date=date};next.Daily.Add(record);}
  if(!learned.ContainsKey(word)){learned.Add(word,date);record.NewWords.Add(word);}else record.Reviewed.Add(word);
  if(mastered){
   List<string> known;
   if(!next.Mastered.TryGetValue(id,out known))next.Mastered.Add(id,known=new List<string>());
   if(!known.Contains(word))known.Add(word);
  }
  Commit(next);
 }
}
}
