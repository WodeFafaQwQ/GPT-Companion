using System;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace DragonCompanion {
public sealed class VocabularyFlashcard:ActionControl {
 readonly TextBox answer;
 VocabularyWord word;
 bool revealed;
 readonly Timer flipTimer=new Timer{Interval=16};
 readonly Stopwatch flipClock=new Stopwatch();
 bool flipping;
 float flipProgress;
 public string EmptyTitle="今日目标已完成";
 public bool Revealed {get{return revealed;}}
 public VocabularyWord Word {get{return word;}}
 public VocabularyFlashcard(){
  Font=Theme.Font(11);AccessibleName="单词卡片";
  answer=new TextBox{Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None,BackColor=Color.White,ForeColor=Theme.Ink,ScrollBars=ScrollBars.Vertical,TabStop=false,Visible=false,Font=Font,AccessibleName="单词卡片答案"};
  answer.MouseClick+=(s,e)=>{if(e.Button==MouseButtons.Left)Reveal();};
  Controls.Add(answer);
  Resize+=(s,e)=>Arrange();
  FontChanged+=(s,e)=>{if(answer!=null){answer.Font=Font;Arrange();}};
  flipTimer.Tick+=(s,e)=>{
   flipProgress=Math.Min(1,flipClock.ElapsedMilliseconds/360f);
   if(flipProgress>=1){flipTimer.Stop();flipping=false;revealed=true;answer.Visible=true;Arrange();}
   Invalidate();
  };
  Disposed+=(s,e)=>flipTimer.Dispose();
 }
 void Arrange(){float scale=Font.SizeInPoints/11f;answer.Bounds=new Rectangle((int)(24*scale),(int)(87*scale),Math.Max(30,Width-(int)(48*scale)),Math.Max(30,Height-(int)(131*scale)));}
 public void SetWord(VocabularyWord value){
  flipTimer.Stop();flipping=false;flipProgress=0;word=value;revealed=false;answer.Visible=false;
  answer.Text=VocabularyWord.DisplayText(value==null?"":"解释\n"+value.Explanation+"\n\n例句\n"+(value.Example==""?"词典未提供例句":value.Example)+"\n\n变形\n"+(value.Forms==""?"词典未提供词形信息":value.Forms));
  answer.SelectionStart=0;AccessibleDescription=value==null?"当前词库已全部掌握":value.Word;
  Invalidate();
 }
 public void Reveal(){
  if(word==null||revealed||flipping)return;
  flipping=true;flipProgress=0;flipClock.Restart();flipTimer.Start();Invalidate();
 }
 protected override void OnClick(EventArgs e){Reveal();base.OnClick(e);}
 protected override void OnPaint(PaintEventArgs e){
  base.OnPaint(e);
  var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
  var saved=g.Save();
  bool back=revealed||flipping&&flipProgress>=.5f;
  if(flipping){
   double eased=.5-.5*Math.Cos(Math.PI*flipProgress);
   float width=(float)Math.Max(.012,Math.Abs(Math.Cos(Math.PI*eased)));
   g.TranslateTransform(Width/2f,0);g.ScaleTransform(width,1);g.TranslateTransform(-Width/2f,0);
  }
  Theme.Box(g,new RectangleF(1,1,Width-3,Height-3),Color.White,18);
  using(var outline=new Pen(Color.FromArgb(185,158,228),1.5f))
  using(var path=Theme.Round(new RectangleF(1,1,Width-3,Height-3),18))g.DrawPath(outline,path);
  float scale=Font.SizeInPoints/11f;
  string title=word==null?EmptyTitle:word.Word;
  float points=(back?22:32)*scale;
  using(var text=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center}){
   using(var face=Theme.Font(points,true)){
    float measured=g.MeasureString(title,face).Width;
    if(measured>Width-48)points*=Math.Max(.38f,(Width-48)/measured);
   }
   using(var face=Theme.Font(points,true))using(var brush=new SolidBrush(Theme.Ink))
    g.DrawString(title,face,brush,new RectangleF(20,back?12*scale:Height*.26f,Width-40,back?60*scale:Height*.40f),text);
   using(var hint=Theme.Font(9*scale))using(var brush=new SolidBrush(Theme.Muted))
    g.DrawString(word==null?"返回词库可调整目标或查看学习记录":back?"空格或「下一张」继续":"点击翻转卡片",hint,brush,new RectangleF(20,Height-39*scale,Width-40,27*scale),text);
  }
  if(flipping&&back)using(var brush=new SolidBrush(Theme.Ink))g.DrawString(answer.Text,Font,brush,new RectangleF(24*scale,87*scale,Width-48*scale,Height-131*scale));
  g.Restore(saved);
 }
}
public sealed partial class Dashboard {
 string vocabularyDeck="",vocabularyQuery="";
 FlowLayoutPanel vocabularyList;
 CatalogPager vocabularyPager;
 ToolTextInput vocabularySearch;
 readonly Timer vocabularySearchTimer=new Timer{Interval=160};
 VocabularySession vocabularySession;
 VocabularyFlashcard vocabularyCard;
 SurfaceLabel vocabularyCount;
 SoftButton vocabularyNext,vocabularyMaster;
 SoftButton vocabularyBack,vocabularyGoalButton;
 SurfaceLabel vocabularyTitle,vocabularySubtitle,vocabularyHint;
 bool vocabularyLoading;
 SoftButton vocabularyStart;
 int vocabularyStudyWidth=658;

 void VocabularyHeading(string title,string subtitle){
  Label(content,title,0,0,490,52,23,Theme.Ink,true);
  Label(content,subtitle,1,58,650,36,10,Theme.Muted);
 }
 bool PrepareVocabularyPage(){
  if(app.Vocabulary!=null)return true;
  Heading("单词卡片","正在打开本地词库…");
  if(!vocabularyLoading)LoadVocabularyPage();
  return false;
 }
 async void LoadVocabularyPage(){
  vocabularyLoading=true;
  try{
   await app.LoadVocabulary();
   if(!IsDisposed&&page.StartsWith("vocabulary",StringComparison.Ordinal))Rebuild();
  }catch(Exception ex){
   if(!IsDisposed&&page.StartsWith("vocabulary",StringComparison.Ordinal)){
    Label(content,"词库打开失败："+ex.Message,0,120,650,100,11,Theme.Muted);
    Button(content,"重试",0,240,160,()=>{vocabularyLoading=false;Rebuild();});
    if(!rebuilding)ApplyAdaptivePage();
   }
  }finally{vocabularyLoading=false;}
 }
 void ResetVocabularyUi(){
  vocabularySearchTimer.Stop();vocabularyList=null;vocabularyPager=null;
  vocabularySearch=null;vocabularyCard=null;vocabularyCount=null;vocabularyNext=null;vocabularyMaster=null;
  vocabularyBack=null;vocabularyGoalButton=null;vocabularyTitle=null;vocabularySubtitle=null;vocabularyHint=null;
 }
 void BuildVocabulary(){
  if(!PrepareVocabularyPage())return;
  VocabularyHeading("单词卡片","勾选一个或多个词库，随机混合学习。");
  Button(content,"返回实用工具",508,7,150,()=>Navigate("tools"));
  Button(content,"新建自定义词库",0,103,210,NewVocabularyDeck,true);
  vocabularyStart=Button(content,"开始单词卡片",448,103,210,StartSelectedVocabulary,true);
  vocabularyStart.Enabled=app.Vocabulary.SelectedDecks.Length>0;
  vocabularyList=CatalogList("单词词库列表",158,404);
  vocabularyPager=AddCatalogPager(index=>FillVocabularyDecks(index));
  FillVocabularyDecks();
 }
 void FillVocabularyDecks(int? index=null){
  var decks=app.Vocabulary.Decks;
  bool pageChanged=vocabularyPager.Update(decks.Length,index);
  RenderCatalogRows(vocabularyList,pageChanged,()=>{
   foreach(var deck in decks.Skip(vocabularyPager.Start).Take(CatalogPageSize)){
    int count=app.Vocabulary.Words(deck.Id).Length;
    int mastered=app.Vocabulary.Words(deck.Id).Count(w=>app.Vocabulary.IsMastered(deck.Id,w.Key));
    var row=new RoundedPanel{Size=new Size(632,156),Margin=new Padding(0,0,0,12)};vocabularyList.Controls.Add(row);
    row.Controls.Add(new CatalogIcon{Symbol="vocabulary",Location=new Point(20,25),Size=new Size(56,56)});
    Label(row,deck.Name,94,19,330,34,17,Theme.Ink,true);
    Label(row,count+" 词 · 已掌握 "+mastered+" 词\n"+app.Vocabulary.DailyStatus(deck.Id,DateTime.Today).Label,94,58,500,42,9,Theme.Muted);
    Button(row,"浏览词库",94,104,198,()=>OpenVocabularyDeck(deck.Id));
    var selected=new SoftCheck{Text="选入学习",Checked=app.Vocabulary.SelectedDecks.Contains(deck.Id),Bounds=new Rectangle(476,18,140,32),Font=Theme.Font(10),AccessibleName=deck.Name+"词库选择"};
    bool restoring=false;
    selected.CheckedChanged+=(s,e)=>{
     if(restoring)return;
     try{app.Vocabulary.SelectDeck(deck.Id,selected.Checked);vocabularyStart.Enabled=app.Vocabulary.SelectedDecks.Length>0;}
     catch(Exception ex){restoring=true;selected.Checked=app.Vocabulary.SelectedDecks.Contains(deck.Id);restoring=false;VocabularyError(ex);}
    };
    row.Controls.Add(selected);
    if(deck.Custom)Button(row,"修改名称",310,104,198,()=>RenameVocabularyDeck(deck.Id));
   }
  });
 }
 void StartSelectedVocabulary(){
  var ids=app.Vocabulary.SelectedDecks;if(ids.Length==0)return;
  vocabularyDeck=ids[0];vocabularySession=new VocabularySession(app.Vocabulary,ids);Navigate("vocabulary-cards");
 }
 void RenameVocabularyDeck(string id){
  using(var dialog=new VocabularyDeckDialog(app.Vocabulary.FindDeck(id).Name)){
   if(dialog.ShowDialog(this)!=DialogResult.OK)return;
   try{
    app.Vocabulary.RenameDeck(id,dialog.DeckName);
    if(app.Vocabulary.Goal(id).LinkPlan)app.SyncVocabularyPlan(id,DateTime.Now);
    FillVocabularyDecks();
   }catch(Exception ex){VocabularyError(ex);}
  }
 }
 void OpenVocabularyDeck(string id){vocabularyDeck=id;vocabularyQuery="";Navigate("vocabulary-words");}
 void NewVocabularyDeck(){
  using(var dialog=new VocabularyDeckDialog()){
   if(dialog.ShowDialog(this)!=DialogResult.OK)return;
   try{OpenVocabularyDeck(app.Vocabulary.CreateDeck(dialog.DeckName));}
   catch(Exception ex){VocabularyError(ex);}
  }
 }
 void BuildVocabularyWords(){
  if(!PrepareVocabularyPage())return;
  var deck=app.Vocabulary.FindDeck(vocabularyDeck);
  if(deck==null){page="vocabulary";BuildVocabulary();return;}
  VocabularyHeading(deck.Name,"右上角勾选或解除掌握，可随时重新加入卡片。");
  Button(content,"返回词库",508,7,150,()=>Navigate("vocabulary"));
  vocabularySearch=new ToolTextInput{Bounds=new Rectangle(0,105,494,44)};
  vocabularySearch.Editor.AccessibleName="检索单词";vocabularySearch.Editor.Text=vocabularyQuery;
  content.Controls.Add(vocabularySearch);
  Button(content,deck.Custom?"添加单词":"清空检索",510,108,148,()=>{if(deck.Custom)EditVocabularyWord(null);else vocabularySearch.Editor.Clear();});
  vocabularyList=CatalogList("词库单词列表",166,396);
  vocabularyPager=AddCatalogPager(index=>FillVocabularyWords(index));
  Label(content,"输入单词检索；点击单词查看卡片。",1,620,650,29,9,Theme.Muted);
  vocabularySearch.Editor.TextChanged+=(s,e)=>{vocabularyQuery=vocabularySearch.Editor.Text;vocabularySearchTimer.Stop();vocabularySearchTimer.Start();};
  FillVocabularyWords();
  vocabularyList.Bounds=new Rectangle(0,216,658,346);
  Label(content,app.Vocabulary.DailyStatus(vocabularyDeck,DateTime.Today).Label,0,164,470,32,10,Theme.Purple);
  Button(content,"每日目标",492,163,166,()=>OpenVocabularyGoal(vocabularyDeck));
 }
 void FillVocabularyWords(int? index=null){
  if(vocabularyList==null||page!="vocabulary-words")return;
  var words=app.Vocabulary.Words(vocabularyDeck)
   .Where(w=>w.Word.IndexOf(vocabularyQuery.Trim(),StringComparison.OrdinalIgnoreCase)>=0)
   .OrderBy(w=>w.Word,StringComparer.OrdinalIgnoreCase).ToArray();
  bool changed=vocabularyPager.Update(words.Length,index);
  var deck=app.Vocabulary.FindDeck(vocabularyDeck);
  RenderCatalogRows(vocabularyList,changed,()=>{
   foreach(var word in words.Skip(vocabularyPager.Start).Take(CatalogPageSize)){
    var row=new RoundedPanel{Size=new Size(632,126),Margin=new Padding(0,0,0,12)};vocabularyList.Controls.Add(row);
    var name=Label(row,word.Word,20,13,440,34,16,Theme.Ink,true);name.Cursor=Cursors.Hand;
    name.Click+=(s,e)=>ShowVocabularyWord(word);
    bool mastered=app.Vocabulary.IsMastered(vocabularyDeck,word.Key);
    var check=new SoftCheck{Text=mastered?"已掌握":"未掌握",Checked=mastered,Bounds=new Rectangle(492,15,120,30),Font=Theme.Font(9),AccessibleName=word.Word+"掌握状态"};
    bool restoring=false;
    check.CheckedChanged+=(s,e)=>{
     check.Text=check.Checked?"已掌握":"未掌握";
     if(restoring)return;
     try{app.Vocabulary.SetMastered(vocabularyDeck,word.Key,check.Checked);}
     catch(Exception ex){restoring=true;check.Checked=app.Vocabulary.IsMastered(vocabularyDeck,word.Key);restoring=false;VocabularyError(ex);}
    };
    row.Controls.Add(check);
    Label(row,PlanExcerpt(word.Explanation.Replace("\n"," "),105),20,60,deck.Custom?448:590,48,10,Theme.Muted);
    if(deck.Custom)Button(row,"编辑",492,70,120,()=>EditVocabularyWord(word)).Height=32;
   }
   if(words.Length==0){
    var empty=new RoundedPanel{Size=new Size(632,126)};vocabularyList.Controls.Add(empty);
    Label(empty,vocabularyQuery==""?"词库还是空的，点击「添加单词」开始。":"没有找到这个单词。",22,28,580,60,12,Theme.Muted);
   }
  });
 }
 void EditVocabularyWord(VocabularyWord existing){
  using(var dialog=new VocabularyWordDialog(app.Vocabulary,existing,(term,token)=>CodexVocabularyClient.Generate(Store.FindCodex(app.Settings.CodexPath),term,token))){
   if(dialog.ShowDialog(this)!=DialogResult.OK)return;
   try{
    app.Vocabulary.SaveWord(vocabularyDeck,dialog.Result,existing==null?null:existing.Key);
    vocabularyQuery="";vocabularySearch.Editor.Clear();vocabularySearchTimer.Stop();FillVocabularyWords(0);
   }catch(Exception ex){VocabularyError(ex);}
  }
 }
 public void StartVocabulary(string id){
  vocabularyDeck=id;vocabularySession=new VocabularySession(app.Vocabulary,id);Navigate("vocabulary-cards");
 }
 void ShowVocabularyWord(VocabularyWord word){
  Navigate("vocabulary-cards");vocabularySession=null;vocabularyCard.SetWord(word);vocabularyCard.Reveal();
  vocabularyCount.Text="单词详情";vocabularyNext.Enabled=false;
  vocabularyMaster.Enabled=!app.Vocabulary.IsMastered(vocabularyDeck,word.Key);
 }
 void BuildVocabularyCards(){
  if(!PrepareVocabularyPage())return;
  var deck=app.Vocabulary.FindDeck(vocabularyDeck);
  if(deck==null){page="vocabulary";BuildVocabulary();return;}
  vocabularyTitle=Label(content,"单词卡片",0,0,658,52,23,Theme.Ink,true);
  vocabularySubtitle=Label(content,deck.Name,0,58,658,30,10,Theme.Muted);
  vocabularyTitle.TextAlign=vocabularySubtitle.TextAlign=ContentAlignment.MiddleCenter;
  vocabularyGoalButton=Button(content,"每日目标",508,103,150,()=>OpenVocabularyGoal(vocabularyDeck));
  vocabularyBack=Button(content,"返回词库",0,103,150,()=>Navigate(vocabularySession!=null&&vocabularySession.DeckIds.Length>1?"vocabulary":"vocabulary-words"));
  vocabularyCount=Label(content,"",0,153,650,32,9,Theme.Purple);
  vocabularyCard=new VocabularyFlashcard{Bounds=new Rectangle(0,145,658,376)};
  content.Controls.Add(vocabularyCard);
  vocabularyMaster=Button(content,"掌握，不再显示",78,547,236,MasterVocabularyWord);
  vocabularyNext=Button(content,"下一张",334,547,236,NextVocabularyWord,true);
  vocabularyHint=Label(content,"点击翻面；看过答案后按空格或「下一张」计入进度。",0,612,650,30,9,Theme.Muted);
  vocabularyHint.TextAlign=ContentAlignment.MiddleCenter;
  if(vocabularySession==null)vocabularySession=new VocabularySession(app.Vocabulary,vocabularyDeck);
  ArrangeVocabularyStudy();
  UpdateVocabularyCard();
 }
 void UpdateVocabularyCard(){
  var word=vocabularySession.Current;vocabularyCard.SetWord(word);
  if(vocabularySession.CurrentDeckIds.Length>0)vocabularyDeck=vocabularySession.CurrentDeckIds[0];
  vocabularySubtitle.Text=vocabularySession.DeckIds.Length>1?"混合 "+vocabularySession.DeckIds.Length+" 个词库 · "+app.Vocabulary.FindDeck(vocabularyDeck).Name:app.Vocabulary.FindDeck(vocabularyDeck).Name;
  vocabularyCount.Text=vocabularySession.Status.Label;
  vocabularyCount.TextAlign=ContentAlignment.MiddleCenter;
  vocabularyNext.Enabled=vocabularyMaster.Enabled=word!=null;
 }
 void NextVocabularyWord(){
  if(vocabularyCard==null||vocabularySession==null)return;
  try{
   if(vocabularySession.Day!=DateTime.Today){vocabularySession=new VocabularySession(app.Vocabulary,vocabularySession.DeckIds);UpdateVocabularyCard();return;}
   if(vocabularyCard.Revealed)vocabularySession.CompleteCurrent();else vocabularySession.Next();
   UpdateVocabularyCard();SyncVocabularyProgress();
  }catch(Exception ex){VocabularyError(ex);}
 }
 void MasterVocabularyWord(){
  if(vocabularyCard==null||vocabularyCard.Word==null)return;
  try{
   if(vocabularySession!=null&&vocabularySession.Day!=DateTime.Today){vocabularySession=new VocabularySession(app.Vocabulary,vocabularySession.DeckIds);UpdateVocabularyCard();return;}
   if(vocabularySession==null){app.Vocabulary.SetMastered(vocabularyDeck,vocabularyCard.Word.Key,true);vocabularyMaster.Enabled=false;vocabularyCount.Text="已掌握";}
   else{vocabularySession.MasterCurrent();UpdateVocabularyCard();SyncVocabularyProgress();}
  }catch(Exception ex){VocabularyError(ex);}
 }
 bool CaptureVocabularyKey(Keys key){
  if(page!="vocabulary-cards"||key!=Keys.Space||OwnedForms.Any(f=>f.Visible))return false;
  NextVocabularyWord();return true;
 }
 void VocabularyError(Exception ex){MessageBox.Show(this,ex.Message+"\n\n"+Theme.Credit,"单词卡片");}
 void SyncVocabularyProgress(){
  foreach(var id in vocabularySession==null?new[]{vocabularyDeck}:vocabularySession.DeckIds){
   if(!app.Vocabulary.Goal(id).LinkPlan||!app.Vocabulary.DailyStatus(id,DateTime.Today).Complete)continue;
   var plan=app.Schedules.Plans.FirstOrDefault(p=>p.VocabularyDeckId==id);
   if(plan!=null&&!plan.Days.Any(d=>d.Date==GoalPlan.DateKey(DateTime.Today)&&d.Done))app.SyncVocabularyPlan(id,DateTime.Now);
  }
 }
 void SetVocabularyBounds(Control control,Rectangle bounds){
  DesignControl item;
  if(design.TryGetValue(control,out item))item.Bounds=bounds;else control.Bounds=bounds;
 }
 bool ArrangeVocabularyStudy(){
  if(page!="vocabulary-cards"||vocabularyCard==null)return false;
  int width=AvailableHomeWidth();bool changed=vocabularyStudyWidth!=width;
  vocabularyStudyWidth=width;
  int cardWidth=Math.Min(600,width),buttonWidth=Math.Min(236,(width-16)/2);
  int start=(width-buttonWidth*2-16)/2;
  SetVocabularyBounds(vocabularyTitle,new Rectangle(0,0,width,52));
  SetVocabularyBounds(vocabularySubtitle,new Rectangle(0,58,width,30));
  SetVocabularyBounds(vocabularyBack,new Rectangle(0,103,148,38));
  SetVocabularyBounds(vocabularyGoalButton,new Rectangle(width-148,103,148,38));
  SetVocabularyBounds(vocabularyCard,new Rectangle((width-cardWidth)/2,197,cardWidth,340));
  SetVocabularyBounds(vocabularyCount,new Rectangle(0,151,width,36));
  SetVocabularyBounds(vocabularyMaster,new Rectangle(start,556,buttonWidth,38));
  SetVocabularyBounds(vocabularyNext,new Rectangle(start+buttonWidth+16,556,buttonWidth,38));
  SetVocabularyBounds(vocabularyHint,new Rectangle(0,608,width,42));
  return changed;
 }
 public void OpenVocabularyGoal(string id){
  var goal=app.Vocabulary.Goal(id);
  using(var dialog=new VocabularyGoalDialog(app.Vocabulary.FindDeck(id).Name,goal)){
   if(dialog.ShowDialog(this)!=DialogResult.OK)return;
   try{
    app.SaveVocabularyGoal(id,dialog.Result,DateTime.Now);
    if(vocabularySession!=null&&vocabularySession.DeckIds.Contains(id))vocabularySession=new VocabularySession(app.Vocabulary,vocabularySession.DeckIds);
    Rebuild();
   }catch(Exception ex){VocabularyError(ex);}
  }
 }
 void InitializeVocabularyUi(){
  vocabularySearchTimer.Tick+=(s,e)=>{vocabularySearchTimer.Stop();FillVocabularyWords(0);};
  Disposed+=(s,e)=>vocabularySearchTimer.Dispose();
 }
}
}
