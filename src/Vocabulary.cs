using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DragonCompanion {
public sealed class VocabularyWord {
 public string Word="",Explanation="",Example="",Forms="";
 public VocabularyWord Copy(){return (VocabularyWord)MemberwiseClone();}
 public static string DisplayText(string text){
  return (text??"").Replace("\r\n","\n").Replace("\r","\n").Replace("\n","\r\n");
 }
 public void Validate(){
  Word=(Word??"").Trim();
  Explanation=(Explanation??"").Trim();
  Example=(Example??"").Trim();
  Forms=(Forms??"").Trim();
  if(Word.Length==0||Word.Length>120)throw new ArgumentException("请填写单词，最多 120 字。");
  if(Explanation.Length==0)throw new ArgumentException("请填写单词解释。");
  if(Explanation.Length>4000||Example.Length>4000||Forms.Length>2000)throw new ArgumentException("内容过长，请缩短后保存。");
 }
 [ScriptIgnore] public string Key {get{return Word.Trim().ToLowerInvariant();}}
}
public sealed class VocabularyDeck {
 public string Id="",Name="";
 public bool Custom;
 public List<string> Words=new List<string>();
}
public sealed class VocabularyCatalog {
 public List<VocabularyWord> Words=new List<VocabularyWord>();
 public List<VocabularyDeck> Decks=new List<VocabularyDeck>();
 public static VocabularyCatalog Load(){
  using(var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("vocabulary.json.gz")){
   if(resource==null)throw new IOException("内置词库资源缺失。");
   using(var gzip=new GZipStream(resource,CompressionMode.Decompress))
   using(var reader=new StreamReader(gzip,Encoding.UTF8)){
    var serializer=Json.Serializer();serializer.MaxJsonLength=32*1024*1024;
    return serializer.Deserialize<VocabularyCatalog>(reader.ReadToEnd());
   }
  }
 }
}
public sealed class CustomVocabularyDeck {
 public string Id=Guid.NewGuid().ToString("N"),Name="自定义词库";
 public List<VocabularyWord> Words=new List<VocabularyWord>();
}
public sealed class VocabularyProgress {
 public List<string> SelectedDecks=new List<string>();
 public List<CustomVocabularyDeck> CustomDecks=new List<CustomVocabularyDeck>{new CustomVocabularyDeck{Id="custom-default"}};
 public Dictionary<string,List<string>> Mastered=new Dictionary<string,List<string>>();
 public Dictionary<string,VocabularyGoal> Goals=new Dictionary<string,VocabularyGoal>();
 public Dictionary<string,Dictionary<string,string>> Learned=new Dictionary<string,Dictionary<string,string>>();
 public List<VocabularyDailyRecord> Daily=new List<VocabularyDailyRecord>();
 public VocabularyProgress Copy(){return Json.Serializer().Deserialize<VocabularyProgress>(Json.Encode(this));}
}
public sealed partial class VocabularyLibrary {
 readonly string root;
 readonly VocabularyCatalog catalog;
 readonly Dictionary<string,VocabularyWord> dictionary;
 readonly Dictionary<string,HashSet<string>> masteredIndex=new Dictionary<string,HashSet<string>>();
 VocabularyProgress progress;
 const string FileName="vocabulary.json";
 public VocabularyLibrary(string profile){
  root=profile;catalog=VocabularyCatalog.Load();
  dictionary=catalog.Words.ToDictionary(w=>w.Key,StringComparer.Ordinal);
  progress=LoadProgress();IndexMastery();
 }
 VocabularyProgress LoadProgress(){
  string file=Path.Combine(root,FileName),backup=Path.Combine(root,"vocabulary.recovery.json");
  foreach(var path in new[]{file,backup}){
   if(!File.Exists(path))continue;
   try{
    var raw=File.ReadAllText(path,Encoding.UTF8);
    var fields=Json.Read(raw);
    if(fields==null||!fields.ContainsKey("CustomDecks")||!fields.ContainsKey("Mastered"))throw new FormatException();
    var loaded=Json.Serializer().Deserialize<VocabularyProgress>(raw);
    if(loaded.CustomDecks==null||loaded.Mastered==null||loaded.Goals==null||loaded.Learned==null||loaded.Daily==null||loaded.Daily.Any(d=>d==null||d.NewWords==null||d.Reviewed==null)||loaded.Mastered.Values.Any(list=>list==null)||loaded.Goals.Values.Any(g=>g==null)||loaded.Learned.Values.Any(d=>d==null)||loaded.CustomDecks.Any(d=>d==null||d.Words==null||d.Words.Any(w=>w==null||string.IsNullOrWhiteSpace(w.Word))))throw new FormatException();
    if(path==backup){
     string original=File.Exists(file)?file+".unreadable-"+Guid.NewGuid().ToString("N"):null;
     using(var save=new Store.StagedSave(file,loaded))save.Commit(original);
    }
    return loaded;
   }catch(ArgumentException){ }
   catch(FormatException){ }
   catch(InvalidOperationException){ }
  }
  if(File.Exists(file)||File.Exists(backup))throw new IOException("单词学习记录读取失败，原文件已保留。");
  return new VocabularyProgress();
 }
 void IndexMastery(){
  masteredIndex.Clear();
  foreach(var pair in progress.Mastered)masteredIndex.Add(pair.Key,new HashSet<string>(pair.Value,StringComparer.Ordinal));
 }
 void Commit(VocabularyProgress next){
  Directory.CreateDirectory(root);
  using(var save=new Store.StagedSave(Path.Combine(root,FileName),next))save.Commit();
  progress=next;IndexMastery();
 }
 public VocabularyDeck[] Decks {
  get{return catalog.Decks.Concat(progress.CustomDecks.Select(d=>new VocabularyDeck{Id=d.Id,Name=d.Name,Custom=true,Words=d.Words.Select(w=>w.Key).ToList()})).ToArray();}
 }
 public VocabularyDeck FindDeck(string id){return Decks.FirstOrDefault(d=>d.Id==id);}
 public VocabularyWord Lookup(string word){
  VocabularyWord result;
  return dictionary.TryGetValue((word??"").Trim().ToLowerInvariant(),out result)?result.Copy():null;
 }
 public VocabularyWord[] Words(string deckId){
  var custom=progress.CustomDecks.FirstOrDefault(d=>d.Id==deckId);
  if(custom!=null)return custom.Words.Select(w=>w.Copy()).ToArray();
  var deck=catalog.Decks.FirstOrDefault(d=>d.Id==deckId);
  return deck==null?new VocabularyWord[0]:deck.Words.Select(key=>dictionary[key].Copy()).ToArray();
 }
 public bool IsMastered(string deckId,string word){
  HashSet<string> known;
  return masteredIndex.TryGetValue(deckId,out known)&&known.Contains(word.Trim().ToLowerInvariant());
 }
 public void SetMastered(string deckId,string word,bool value){
  var key=word.Trim().ToLowerInvariant();
  if(!Words(deckId).Any(w=>w.Key==key))throw new ArgumentException("这个单词不在当前词库中。");
  if(IsMastered(deckId,key)==value)return;
  var next=progress.Copy();List<string> known;
  if(!next.Mastered.TryGetValue(deckId,out known))next.Mastered.Add(deckId,known=new List<string>());
  if(value)known.Add(key);else known.Remove(key);
  Commit(next);
 }
 public string CreateDeck(string name){
  name=(name??"").Trim();
  if(name.Length==0||name.Length>40)throw new ArgumentException("词库名称为 1～40 字。");
  if(Decks.Any(d=>d.Name.Equals(name,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("已有同名词库。");
  var next=progress.Copy();var deck=new CustomVocabularyDeck{Name=name};
  next.CustomDecks.Add(deck);Commit(next);return deck.Id;
 }
 public string[] SelectedDecks {get{return (progress.SelectedDecks??new List<string>()).Where(id=>FindDeck(id)!=null).Distinct().ToArray();}}
 public void SelectDeck(string id,bool selected){
  var next=progress.Copy();next.SelectedDecks=SelectedDecks.ToList();
  if(selected&&!next.SelectedDecks.Contains(id))next.SelectedDecks.Add(id);
  if(!selected)next.SelectedDecks.Remove(id);
  Commit(next);
 }
 public void RenameDeck(string id,string name){
  name=(name??"").Trim();
  if(name.Length==0||name.Length>40)throw new ArgumentException("词库名称为 1～40 字。");
  if(Decks.Any(d=>d.Id!=id&&d.Name.Equals(name,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("已有同名词库。");
  var next=progress.Copy();var deck=next.CustomDecks.FirstOrDefault(d=>d.Id==id);
  if(deck==null)throw new ArgumentException("只能修改自定义词库的名称。");
  deck.Name=name;Commit(next);
 }
 public void SaveWord(string deckId,VocabularyWord word,string originalKey=null){
  word=word.Copy();word.Validate();
  var next=progress.Copy();var deck=next.CustomDecks.FirstOrDefault(d=>d.Id==deckId);
  if(deck==null)throw new ArgumentException("请选择自定义词库。");
  if(deck.Words.Any(w=>w.Key==word.Key&&w.Key!=originalKey))throw new ArgumentException("词库中已有这个单词，可打开它进行编辑。");
  int index=deck.Words.FindIndex(w=>w.Key==originalKey);
  if(index>=0)deck.Words[index]=word;else deck.Words.Add(word);
  List<string> known;
  if(originalKey!=null&&originalKey!=word.Key&&next.Mastered.TryGetValue(deckId,out known))known.Remove(originalKey);
  Commit(next);
 }
}
public sealed class VocabularySession {
 sealed class Entry {public VocabularyWord Word;public string[] Decks;}
 readonly VocabularyLibrary library;
 readonly List<Entry> queue;
 readonly DateTime day;
 int index=-1;
 Entry current;
 public string[] DeckIds {get;private set;}
 public string[] CurrentDeckIds {get{return current==null?new string[0]:current.Decks;}}
 public VocabularyWord Current {get{return current==null?null:current.Word;}}
 public VocabularyDailyStatus Status {
  get{
   var statuses=DeckIds.Select(id=>library.DailyStatus(id,day)).ToArray();
   return new VocabularyDailyStatus{NewDone=statuses.Sum(s=>s.NewDone),NewTarget=statuses.Sum(s=>s.NewTarget),ReviewDone=statuses.Sum(s=>s.ReviewDone),ReviewTarget=statuses.Sum(s=>s.ReviewTarget)};
  }
 }
 public DateTime Day {get{return day;}}
 public int Remaining {get{var status=Status;return Math.Max(0,status.NewTarget-status.NewDone)+Math.Max(0,status.ReviewTarget-status.ReviewDone);}}
 public VocabularySession(VocabularyLibrary source,string id,DateTime? today=null):this(source,new[]{id},today){}
 public VocabularySession(VocabularyLibrary source,IEnumerable<string> ids,DateTime? today=null){
  library=source;DeckIds=ids.Distinct().Where(id=>source.FindDeck(id)!=null).ToArray();day=(today??DateTime.Now).Date;
  var membership=new Dictionary<string,List<string>>();
  foreach(var id in DeckIds)foreach(var word in library.Words(id)){
   List<string> decks;
   if(!membership.TryGetValue(word.Key,out decks))membership.Add(word.Key,decks=new List<string>());
   decks.Add(id);
  }
  queue=DeckIds.SelectMany(id=>library.StudyWords(id,day)).GroupBy(word=>word.Key)
   .Select(group=>new Entry{Word=group.First(),Decks=membership[group.Key].ToArray()}).ToList();
  var random=new Random();
  for(int i=queue.Count-1;i>0;i--){int j=random.Next(i+1);var swap=queue[i];queue[i]=queue[j];queue[j]=swap;}
  Next();
 }
 public void Next(){
  current=null;
  for(int i=0;i<queue.Count;i++){
   index=(index+1)%queue.Count;
   var candidate=queue[index];
   if(candidate.Decks.Any(id=>library.CanStudy(id,candidate.Word.Key,day))){current=candidate;return;}
  }
 }
 public void CompleteCurrent(){Record(false);}
 public void MasterCurrent(){Record(true);}
 void Record(bool mastered){
  if(current==null)return;
  foreach(var id in current.Decks)library.RecordStudy(id,current.Word.Key,day,mastered);
  Next();
 }
}
public sealed partial class Controller {
 Task<VocabularyLibrary> vocabularyLoad;
 public VocabularyLibrary Vocabulary {get;private set;}
 public async Task LoadVocabulary(){
  if(Vocabulary!=null)return;
  if(vocabularyLoad==null){string root=Store.Root;vocabularyLoad=Task.Run(()=>new VocabularyLibrary(root));}
  try{
   Vocabulary=await vocabularyLoad;
   foreach(var deck in Vocabulary.Decks.Where(d=>Vocabulary.Goal(d.Id).LinkPlan))SyncVocabularyPlan(deck.Id,DateTime.Now);
  }catch{vocabularyLoad=null;throw;}
 }
}
}
