using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DragonCompanion {
public static partial class SelfTests {
 static VocabularyWord GeneratedWord(string term){
  return new VocabularyWord{Word=term,Explanation="n. "+term+" 的中文解释",Example="This is an example.\n这是一个例句。",Forms="无常用词形变化"};
 }
 static void LeaveVocabularyWord(TextBox editor){
  typeof(Control).GetMethod("OnLeave",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(editor,new object[]{EventArgs.Empty});
 }
 static void PumpVocabulary(int milliseconds){
  var until=DateTime.UtcNow.AddMilliseconds(milliseconds);
  while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(3);}
 }
 static void TestVocabularyGeneration(Controller controller,string folder){
  var good=Json.Encode(new{word="microlearning",recognized=true,explanation="n. 微学习",example="Microlearning uses short lessons.\n微学习采用短课程。",forms="无常用词形变化"});
  Check(CodexVocabularyClient.Parse(good,"microlearning").Example.Contains("短课程"),"Generated entries validate the term, explanation, bilingual examples and word forms");
  bool invalid=false;
  try{CodexVocabularyClient.Parse(good,"another");}catch(FormatException){invalid=true;}
  Check(invalid,"A generated entry for another word is rejected");
  invalid=false;try{CodexVocabularyClient.Parse(good.Replace("\"recognized\":true","\"recognized\":false"),"microlearning");}catch(ArgumentException){invalid=true;}
  Check(invalid,"Unrecognized words are reported instead of saved as invented definitions");
  invalid=false;try{CodexVocabularyClient.Parse("{\"word\":\"microlearning\"}","microlearning");}catch(FormatException){invalid=true;}
  Check(invalid,"An incomplete generated reply cannot populate a word");
  int calls=0;
  var pending=new Dictionary<string,TaskCompletionSource<VocabularyWord>>();
  var tokens=new Dictionary<string,CancellationToken>();
  using(var dialog=new VocabularyWordDialog(controller.Vocabulary,generator:(term,token)=>{
   calls++;tokens[term]=token;
   var task=new TaskCompletionSource<VocabularyWord>();pending[term]=task;return task.Task;
  })){
   dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-3000,-3000);dialog.Show();Application.DoEvents();
   var editors=Descendants(dialog).OfType<TextBox>().ToArray();
   var term=editors.Single(e=>e.AccessibleName=="单词");
   var explanation=editors.Single(e=>e.AccessibleName=="单词解释");
   var example=editors.Single(e=>e.AccessibleName=="单词例句");
   term.Text="go";LeaveVocabularyWord(term);PumpVocabulary(30);
   Check(calls==0&&explanation.TextLength>0&&example.TextLength>0,"Known words automatically fill from the offline dictionary without a Codex request");
   term.Text="microlearning";LeaveVocabularyWord(term);PumpVocabulary(30);
   explanation.Text="我手动补充的解释";
   pending["microlearning"].SetResult(GeneratedWord("microlearning"));PumpVocabulary(60);
   Check(explanation.Text=="我手动补充的解释"&&example.Text.Contains("example"),"A generated reply fills missing fields while preserving edits typed during generation");
   explanation.Clear();term.Text="oldalpha";LeaveVocabularyWord(term);PumpVocabulary(20);
   term.Text="newbeta";LeaveVocabularyWord(term);PumpVocabulary(20);
   pending["oldalpha"].SetResult(GeneratedWord("oldalpha"));PumpVocabulary(40);
   Check(tokens["oldalpha"].IsCancellationRequested&&!explanation.Text.Contains("oldalpha"),"Changing the word cancels the old request and discards a late reply");
   pending["newbeta"].SetResult(GeneratedWord("newbeta"));PumpVocabulary(40);
   Check(explanation.Text.Contains("newbeta"),"The current word receives its own generated content");
   term.Text="cancelledword";LeaveVocabularyWord(term);PumpVocabulary(20);dialog.Close();
   pending["cancelledword"].SetResult(GeneratedWord("cancelledword"));PumpVocabulary(30);
   Check(tokens["cancelledword"].IsCancellationRequested&&dialog.Result==null,"Closing the editor cancels generation without saving a partial word");
  }
  using(var dialog=new VocabularyWordDialog(controller.Vocabulary,generator:(term,token)=>Task.FromResult(GeneratedWord(term)))){
   dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-3000,-3000);dialog.Show();Application.DoEvents();
   var term=Descendants(dialog).OfType<TextBox>().Single(e=>e.AccessibleName=="单词");
   term.Text="autosavedword";
   AdaptiveClick(FindText(dialog,"保存单词"));PumpVocabulary(60);
   Check(dialog.Result!=null&&dialog.Result.Explanation.Contains("autosavedword")&&dialog.Result.Example!=""&&dialog.Result.Forms!="","Saving a word before the debounce fires waits for automatic content generation");
  }
 }
}
}
