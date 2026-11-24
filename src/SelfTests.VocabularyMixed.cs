using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace DragonCompanion {
public static partial class SelfTests {
 static void TestMixedVocabulary(Controller controller,Dashboard panel,string folder){
  var library=controller.Vocabulary;
  string first=library.CreateDeck("计算机词汇"),second=library.CreateDeck("商务词汇");
  library.SaveWord(first,library.Lookup("go"));library.SaveWord(first,library.Lookup("tree"));
  library.SaveWord(second,library.Lookup("go"));library.SaveWord(second,library.Lookup("book"));
  library.SetMastered(first,"tree",true);
  library.RenameDeck(first,"编程词汇");
  var loaded=new VocabularyLibrary(Store.Root);
  Check(loaded.FindDeck(first).Name=="编程词汇"&&loaded.Words(first).Length==2&&loaded.IsMastered(first,"tree"),"Renaming one of several custom dictionaries preserves its ID, words and mastery");
  panel.Navigate("vocabulary");
  Check(!Descendants(panel).OfType<SoftButton>().Any(button=>button.Text=="开始卡片")&&Descendants(panel).OfType<SoftButton>().Count(button=>button.Text=="开始单词卡片")==1,"The vocabulary home has one shared start button and no per-dictionary start buttons");
  AdaptiveClick(Descendants(panel).OfType<SoftCheck>().Single(box=>box.AccessibleName=="编程词汇词库选择"));
  AdaptiveClick(Descendants(panel).OfType<SoftCheck>().Single(box=>box.AccessibleName=="商务词汇词库选择"));
  Check(new VocabularyLibrary(Store.Root).SelectedDecks.Contains(first)&&library.SelectedDecks.Contains(second),"Multiple selected dictionaries persist across reload");
  PlannerScreenshot(panel,folder,"ui-vocabulary-multi-select.png");
  AdaptiveClick(FindText(panel,"开始单词卡片"));
  var card=Descendants(panel).OfType<VocabularyFlashcard>().Single();
  Check(card.Word!=null,"The shared start button launches mixed vocabulary cards");
  var session=new VocabularySession(library,new[]{first,second});
  var words=new System.Collections.Generic.List<string>();
  while(session.Current!=null&&words.Count<10){words.Add(session.Current.Key);session.MasterCurrent();}
  Check(words.Count==2&&words.Distinct().Count()==2&&words.Contains("go")&&words.Contains("book")&&!words.Contains("tree"),"Mixed cards deduplicate shared words and skip mastered words across selected dictionaries");
  Check(library.IsMastered(first,"go")&&library.IsMastered(second,"go"),"Mastering a shared mixed card updates both selected dictionaries");
  panel.SetWindowClientSize(new Size(760,560));panel.Navigate("vocabulary");
  Check(NoSiblingOverlap(panel),"The multi-select vocabulary page fits a small window without overlapping buttons");
 }
}
}
