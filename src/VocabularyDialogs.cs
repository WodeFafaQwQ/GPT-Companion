using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DragonCompanion {
public sealed class VocabularyDeckDialog:PlannerDialog {
 readonly ToolTextInput name;
 public string DeckName {get{return name.Editor.Text.Trim();}}
 public VocabularyDeckDialog(string existing=null):base(existing==null?"新建词库":"修改词库名称",260){
  TextLabel(existing==null?"新建自定义词库":"修改词库名称",24,15,560,40,21,true);
  TextLabel("词库名称",24,68,560,25);
  name=new ToolTextInput{Bounds=new Rectangle(24,99,560,44)};
  name.Editor.MaxLength=40;name.Editor.AccessibleName="词库名称";name.Editor.Text=existing??"";Body.Controls.Add(name);
  Action(existing==null?"创建词库":"保存名称",24,163,170,()=>{if(DeckName.Length>0){DialogResult=DialogResult.OK;Close();}},true);
  Action("返回",210,163,120,()=>Close());
 }
}
public sealed class VocabularyWordDialog:PlannerDialog {
 readonly ToolTextInput word,explanation,example,forms;
 readonly SurfaceLabel status;
 readonly VocabularyLibrary library;
 readonly Func<string,CancellationToken,Task<VocabularyWord>> generate;
 readonly SoftButton saveButton;
 readonly System.Windows.Forms.Timer debounce=new System.Windows.Forms.Timer{Interval=1000};
 readonly Dictionary<string,VocabularyWord> cache=new Dictionary<string,VocabularyWord>();
 readonly string[] managed=new string[3];
 CancellationTokenSource requestCancel;
 Task<bool> activeFill;
 string activeWord="",lastWord="";
 bool saving;
 public VocabularyWord Result;
 TextBox[] Fields {get{return new[]{explanation.Editor,example.Editor,forms.Editor};}}
 public VocabularyWordDialog(VocabularyLibrary source,VocabularyWord existing=null,Func<string,CancellationToken,Task<VocabularyWord>> generator=null):base(existing==null?"添加单词":"编辑单词",724){
  library=source;generate=generator??((term,token)=>CodexVocabularyClient.Generate(Store.FindCodex(""),term,token));
  TextLabel(existing==null?"添加单词":"编辑单词",24,12,560,40,21,true);
  TextLabel("单词",24,64,560,24);
  word=Input("单词",24,92,352,44,false,existing==null?"":existing.Word);
  word.Editor.MaxLength=120;
  Action("自动补全",392,95,192,async()=>{await Fill(true);});
  TextLabel("解释",24,150,560,24);
  explanation=Input("单词解释",24,179,560,92,true,existing==null?"":existing.Explanation);
  TextLabel("例句",24,284,560,24);
  example=Input("单词例句",24,313,560,124,true,existing==null?"":existing.Example);
  TextLabel("变形",24,450,560,24);
  forms=Input("单词变形",24,479,560,80,true,existing==null?"":existing.Forms);
  forms.Editor.MaxLength=2000;
  status=TextLabel("输入单词后自动补全；生成结果可编辑。",24,570,560,40,9);
  saveButton=Action("保存单词",24,625,170,Save,true);
  Action("返回",210,625,120,()=>Close());
  for(int i=0;i<managed.Length;i++)managed[i]=Fields[i].Text;
  lastWord=Key;
  word.Editor.TextChanged+=(s,e)=>WordChanged();
  word.Editor.Leave+=async(s,e)=>{debounce.Stop();if(Key!=lastWord||Fields.Any(field=>field.TextLength==0))await Fill(false);};
  word.Editor.KeyDown+=async(s,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;debounce.Stop();await Fill(false);}};
  debounce.Tick+=async(s,e)=>{debounce.Stop();await Fill(false);};
  FormClosed+=(s,e)=>CancelFill();
  Disposed+=(s,e)=>{CancelFill();debounce.Dispose();};
 }
 string Key {get{return word.Editor.Text.Trim().ToLowerInvariant();}}
 ToolTextInput Input(string name,int x,int y,int width,int height,bool multiline,string value){
  var input=new ToolTextInput(multiline){Bounds=new Rectangle(x,y,width,height)};
  input.Editor.Text=VocabularyWord.DisplayText(value);input.Editor.MaxLength=4000;input.Editor.AccessibleName=name;Body.Controls.Add(input);return input;
 }
 void CancelFill(){debounce.Stop();if(requestCancel!=null)requestCancel.Cancel();}
 void WordChanged(){
  if(Key==lastWord)return;
  lastWord=Key;CancelFill();
  var fields=Fields;
  for(int i=0;i<fields.Length;i++){
   if(fields[i].Text==managed[i])fields[i].Clear();
   managed[i]="";
  }
  status.Text=Key==""?"输入单词后自动补全；生成结果可编辑。":"正在等待输入完成…";
  if(Key!="")debounce.Start();
 }
 Task<bool> Fill(bool replace){
  debounce.Stop();
  if(IsDisposed||Key=="")return Task.FromResult(false);
  if(!replace&&activeWord==Key&&activeFill!=null&&!activeFill.IsCompleted)return activeFill;
  CancelFill();
  activeWord=Key;
  var cancellation=new CancellationTokenSource();requestCancel=cancellation;
  activeFill=FillAsync(word.Editor.Text.Trim(),replace,cancellation);
  return activeFill;
 }
 bool Current(string key,CancellationTokenSource cancellation){
  return !IsDisposed&&!cancellation.IsCancellationRequested&&Key==key.ToLowerInvariant();
 }
 void Apply(VocabularyWord entry,bool replace,string[] before){
  string[] values={entry.Explanation,entry.Example,entry.Forms};
  var fields=Fields;
  for(int i=0;i<fields.Length;i++){
   if(string.IsNullOrWhiteSpace(values[i])||fields[i].Text!=before[i])continue;
   if(!replace&&before[i]!=""&&before[i]!=managed[i])continue;
   managed[i]=VocabularyWord.DisplayText(values[i]);fields[i].Text=managed[i];
  }
 }
 async Task<bool> FillAsync(string term,bool replace,CancellationTokenSource cancellation){
  try{
   var local=library.Lookup(term);
   if(local!=null)Apply(local,replace,Fields.Select(field=>field.Text).ToArray());
   if(Fields.All(field=>field.TextLength>0)){
    status.Text="已补全，可编辑后保存。";return true;
   }
   VocabularyWord generated;
   var before=Fields.Select(field=>field.Text).ToArray();
   if(!cache.TryGetValue(term.ToLowerInvariant(),out generated)){
    status.Text="Codex 正在生成解释、例句和变形…";
    generated=await generate(term,cancellation.Token);
    if(!Current(term,cancellation))return false;
    generated.Validate();
    if(!string.Equals(generated.Key,term.ToLowerInvariant(),StringComparison.Ordinal))throw new FormatException("词条与输入不一致，请重试。");
    cache[generated.Key]=generated.Copy();
   }
   if(!Current(term,cancellation))return false;
   Apply(generated,replace,before);
   status.Text="已自动补全，生成内容可编辑后保存。";
   return true;
  }catch(OperationCanceledException){return false;}
  catch(Exception ex){
   if(Current(term,cancellation))status.Text="自动补全失败："+ex.Message;
   return false;
  }finally{
   if(ReferenceEquals(requestCancel,cancellation))requestCancel=null;
   cancellation.Dispose();
  }
 }
 async void Save(){
  if(saving)return;
  saving=true;saveButton.Enabled=false;
  string requested=Key;
  try{
   bool completed=true;
   if(activeFill!=null&&!activeFill.IsCompleted)completed=await activeFill;
   else if(explanation.Editor.TextLength==0)completed=await Fill(false);
   if(IsDisposed||Key!=requested)return;
   if(!completed&&explanation.Editor.TextLength==0)return;
   var result=new VocabularyWord{Word=word.Editor.Text,Explanation=explanation.Editor.Text,Example=example.Editor.Text,Forms=forms.Editor.Text};
   result.Validate();Result=result;DialogResult=DialogResult.OK;Close();
  }catch(ArgumentException ex){if(!IsDisposed)status.Text=ex.Message;}
  finally{saving=false;if(!IsDisposed)saveButton.Enabled=true;}
 }
}
}
