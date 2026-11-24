using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace DragonCompanion {
public static partial class SelfTests {
 static void TestVocabulary(Controller controller,Dashboard panel,string folder){
  var scheduleFile=System.IO.Path.Combine(Store.Root,"schedules.json");
  string scheduleBefore=System.IO.File.ReadAllText(scheduleFile);
  FinishPlannerTask(controller.LoadVocabulary());
  var library=controller.Vocabulary;
  Check(library.FindDeck("gaokao").Words.Count>=3500&&library.FindDeck("cet4").Words.Count>3000&&library.FindDeck("cet6").Words.Count>2000&&library.FindDeck("ielts").Words.Count>3000,"Four full offline exam dictionaries are available");
  foreach(var id in new[]{"gaokao","cet4","cet6","ielts"}){
   var words=library.Words(id);
   Check(words.All(w=>w.Word!=""&&w.Explanation!="")&&words.Select(w=>w.Key).Distinct().Count()==words.Length,"Offline deck "+id+" has unique words with explanations");
  }
  var go=library.Lookup("go");var book=library.Lookup("book");
  Check(go!=null&&go.Example.Length>0&&go.Forms.Contains("went")&&book!=null&&book.Forms.Contains("books"),"Examples and irregular and regular word forms come from the dictionary");
  panel.Navigate("tools");
  AdaptiveClick(FindText(panel,"单词卡片"));
  Check(FindText(panel,"高考3500")!=null&&FindText(panel,"大学英语四级")!=null,"Vocabulary opens through the normal utility catalog");
  PlannerScreenshot(panel,folder,"ui-vocabulary-libraries.png");
  var list=Descendants(panel).OfType<FlowLayoutPanel>().Single();
  var customRow=list.Controls.Cast<Control>().Single(row=>FindText(row,"自定义词库")!=null);
  AdaptiveClick(FindText(customRow,"浏览词库"));
  Check(FindText(panel,"词库还是空的，点击「添加单词」开始。")!=null,"The default custom dictionary opens empty and offers adding words");
  library.SaveWord("custom-default",go);library.SaveWord("custom-default",book);
  panel.Navigate("vocabulary-words");
  var search=Descendants(panel).OfType<TextBox>().Single(box=>box.AccessibleName=="检索单词");
  search.Text=" BOO ";
  var until=DateTime.UtcNow.AddMilliseconds(400);
  while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(5);}
  Check(FindText(panel,"book")!=null&&FindText(panel,"go")==null,"Dictionary search is case-insensitive, trims whitespace, and filters to matching words");
  PlannerScreenshot(panel,folder,"ui-vocabulary-search.png");
  var known=Descendants(panel).OfType<SoftCheck>().Single(box=>box.AccessibleName=="book掌握状态");
  AdaptiveClick(known);
  var restored=new VocabularyLibrary(Store.Root);
  Check(restored.IsMastered("custom-default","book")&&!restored.IsMastered("cet4","book"),"Mastery is persisted per dictionary across restart");
  panel.StartVocabulary("custom-default");Application.DoEvents();
  var card=Descendants(panel).OfType<VocabularyFlashcard>().Single();
  Check(card.Word.Key=="go"&&!card.Revealed,"Study starts on the word side and skips mastered entries");
  PlannerScreenshot(panel,folder,"ui-vocabulary-front.png");
  var command=typeof(Dashboard).GetMethod("ProcessCmdKey",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
  var message=Message.Create(panel.Handle,0x100,new IntPtr((int)Keys.Space),IntPtr.Zero);
  var args=new object[]{message,Keys.Space};
  FindText(panel,"掌握，不再显示").Focus();
  bool handled=(bool)command.Invoke(panel,args);
  Check(handled&&!card.Revealed&&!library.IsMastered("custom-default","go"),"Space advances to a fresh front even when the mastery button has focus, without marking the word");
  AdaptiveClick(card);
  Check(!card.Revealed,"A flashcard starts an animated flip instead of switching its face immediately");
  until=DateTime.UtcNow.AddMilliseconds(500);
  while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(5);}
  Check(card.Revealed&&Descendants(card).OfType<TextBox>().Single().Text.Contains("went"),"The flip animation ends on explanations, examples and forms");
  PlannerScreenshot(panel,folder,"ui-vocabulary-back.png");
  AdaptiveClick(FindText(panel,"掌握，不再显示"));
  Check(card.Word==null&&!FindText(panel,"下一张").Enabled,"Mastering the last available word finishes study without repeating mastered words");
  AdaptiveClick(FindText(panel,"返回词库"));search=Descendants(panel).OfType<TextBox>().Single(box=>box.AccessibleName=="检索单词");search.Clear();
  until=DateTime.UtcNow.AddMilliseconds(350);while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(5);}
  known=Descendants(panel).OfType<SoftCheck>().Single(box=>box.AccessibleName=="book掌握状态");AdaptiveClick(known);
  panel.StartVocabulary("custom-default");Application.DoEvents();card=Descendants(panel).OfType<VocabularyFlashcard>().Single();
  Check(card.Word.Key=="book"&&!card.Revealed,"Unchecking mastery in the dictionary makes the word available again");
  bool duplicate=false;try{library.SaveWord("custom-default",new VocabularyWord{Word="BOOK",Explanation="重复"});}catch(ArgumentException){duplicate=true;}
  Check(duplicate&&library.Words("custom-default").Length==2,"Custom dictionaries reject case-insensitive duplicate words");
  var edited=book.Copy();edited.Example="I read a book every evening.\n我每天晚上读书。";
  library.SaveWord("custom-default",edited,book.Key);
  restored=new VocabularyLibrary(Store.Root);
  Check(restored.Words("custom-default").Single(w=>w.Key=="book").Example==edited.Example,"Custom word edits survive reload");
  var vocabularyFile=System.IO.Path.Combine(Store.Root,"vocabulary.json");
  System.IO.File.Delete(vocabularyFile);
  restored=new VocabularyLibrary(Store.Root);
  Check(restored.Words("custom-default").Single(w=>w.Key=="book").Example==edited.Example&&restored.IsMastered("custom-default","go"),"A missing vocabulary file recovers the latest custom words and mastery from its backup");
  System.IO.File.WriteAllText(vocabularyFile,"{");
  restored=new VocabularyLibrary(Store.Root);
  Check(restored.Words("custom-default").Single(w=>w.Key=="book").Example==edited.Example&&System.IO.Directory.GetFiles(Store.Root,"vocabulary.json.unreadable-*").Length==1,"A damaged vocabulary file is retained separately and restored from the recovery copy");
  using(var dialog=new VocabularyWordDialog(library,go)){
   dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-3000,-3000);dialog.Show();Application.DoEvents();
   Check(NoSiblingOverlap(dialog),"The custom word editor fields, buttons and credit do not overlap");
   PlannerScreenshot(dialog,folder,"ui-vocabulary-word-editor.png");
  }
  foreach(var size in new[]{new Size(760,560),new Size(1100,760)}){
   panel.SetWindowClientSize(size);panel.Location=new Point(-3000,-3000);
   foreach(var page in new[]{"vocabulary","vocabulary-words","vocabulary-cards"}){
    panel.Navigate(page);Application.DoEvents();
    Check(NoSiblingOverlap(panel),"Vocabulary page "+page+" fits at "+size);
   }
  }
  Check(System.IO.File.ReadAllText(scheduleFile)==scheduleBefore,"Vocabulary edits leave existing plans and schedules intact");
  TestVocabularyGoals(controller,panel,folder);
  TestVocabularyGeneration(controller,folder);TestMixedVocabulary(controller,panel,folder);
  panel.Navigate("settings");
 }
 static void TestVocabularyGoals(Controller controller,Dashboard panel,string folder){
  var library=controller.Vocabulary;var original=controller.Schedules;
  string id=library.CreateDeck("每日目标测试");
  foreach(var word in new[]{"go","book","tree"})library.SaveWord(id,library.Lookup(word));
  var today=DateTime.Today;
  library.RecordStudy(id,"go",today.AddDays(-1),false);
  var goal=new VocabularyGoal{NewWords=1,ReviewWords=1,LinkPlan=true,Deadline=GoalPlan.DateKey(today.AddDays(2))};
  try{
   controller.SaveVocabularyGoal(id,goal,today.AddHours(8));
   var plan=controller.Schedules.Plans.Single(p=>p.VocabularyDeckId==id);
   Check(plan.Days.Count==3&&controller.Schedules.Items.Count(e=>e.PlanId==plan.Id)==3&&!plan.Days.First().Done,"Daily vocabulary goals create a linked plan and reminders through the selected deadline");
   var session=new VocabularySession(library,id,today);
   Check(session.Status.NewTarget==1&&session.Status.ReviewTarget==1,"Daily study selects one unseen word and one previously learned unmastered word");
   session.CompleteCurrent();session.CompleteCurrent();
   controller.SyncVocabularyPlan(id,today.AddHours(9));
   var status=library.DailyStatus(id,today);
   var completed=controller.FindPlan(plan.Id).Days.Single(d=>d.Date==GoalPlan.DateKey(today));
   Check(status.NewDone==1&&status.ReviewDone==1&&completed.Done&&!controller.Schedules.Items.Single(e=>e.PlanDayId==completed.Id).Enabled,"Reaching the daily study and review targets checks the linked plan and stops its reminder");
   Check(new VocabularySession(new VocabularyLibrary(Store.Root),id,today).Current==null,"Restarting retains the completed daily targets without counting words twice");
   var tomorrow=new VocabularySession(library,id,today.AddDays(1));
   Check(tomorrow.Current!=null&&tomorrow.Status.NewDone==0&&tomorrow.Status.ReviewDone==0,"A new date starts fresh daily counts while retaining vocabulary learning history");
   using(var dialog=new VocabularyGoalDialog("每日目标测试",goal)){
    dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-3000,-3000);dialog.Show();Application.DoEvents();
    Check(NoSiblingOverlap(dialog),"Daily word targets, linked plan settings and footer do not overlap");
    PlannerScreenshot(dialog,folder,"ui-vocabulary-goals.png");
   }
   panel.OpenPlan(plan.Id);Application.DoEvents();
   Check(FindText(panel,"开始学习")!=null,"The linked daily plan offers a direct entry into vocabulary study");
   PlannerScreenshot(panel,folder,"ui-vocabulary-linked-plan.png");
  }finally{
   goal.LinkPlan=false;library.SaveGoal(id,goal,today);
   controller.Schedules=original;controller.ScheduleVersion++;Store.Save("schedules.json",original);
  }
 }
}
}
