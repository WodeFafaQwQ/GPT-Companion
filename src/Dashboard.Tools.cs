using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace DragonCompanion {
public sealed partial class Dashboard {
 const int CatalogPageSize=8;
 int scheduleVersion=-1;
 DateTime scheduleMinute;
 FlowLayoutPanel scheduleList;
 SurfaceLabel toolsClock;
 CatalogPager schedulePager;
 readonly List<ScheduleCard> scheduleCards=new List<ScheduleCard>();

 sealed class ScheduleCard {
  public ScheduleEntry Entry;
  public PlanDay Day;
  public SurfaceLabel Timing;
  public SoftButton Feedback;
  public void UpdateTime(DateTime? next,DateTime now){
   string timing=Day!=null&&Day.Done?"已打勾完成":next.HasValue?"下次："+next.Value.ToString("yyyy-MM-dd HH:mm"):
    Entry.Repeat=="once"&&Entry.Start<=now?(Day!=null?"待记录完成情况":"已结束；编辑时间可重新启用"):"已暂停提醒";
   if(Timing.Text!=timing)Timing.Text=timing;
   if(Feedback!=null)Feedback.Enabled=!Day.Done&&Day.Day<=now.Date;
  }
 }

 void ResetScheduleUi(){
  scheduleVersion=-1;scheduleMinute=default(DateTime);
  scheduleList=null;toolsClock=null;schedulePager=null;
  scheduleCards.Clear();
 }
 FlowLayoutPanel CatalogList(string name,int y=102,int height=532){var list=new FlowLayoutPanel{Location=new Point(0,y),Size=new Size(658,height),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,TabStop=true,AccessibleName=name,BackColor=BackColor,Padding=new Padding(0)};list.MouseEnter+=(s,e)=>list.Focus();content.Controls.Add(list);return list;}
 sealed class CatalogPager {
  public SoftButton Previous,Next;
  public SurfaceLabel Status;
  public int Index{get;private set;}
  public int Start{get{return Index*CatalogPageSize;}}
  public bool Update(int count,int? requestedPage){
   int pages=Math.Max(1,(count+CatalogPageSize-1)/CatalogPageSize);
   int previousIndex=Index;
   Index=Math.Max(0,Math.Min(pages-1,requestedPage??Index));
   Previous.Enabled=Index>0;
   Next.Enabled=Index+1<pages;
   Status.Text="第 "+(Index+1)+" / "+pages+" 页 · 共 "+count+" 项";
   return Index!=previousIndex;
  }
 }
 CatalogPager AddCatalogPager(Action<int> navigate){
  var pager=new CatalogPager();
  pager.Previous=Button(content,"上一页",0,570,112,()=>navigate(pager.Index-1));
  pager.Status=Label(content,"",124,570,384,38,10,Theme.Purple);pager.Status.TextAlign=ContentAlignment.MiddleCenter;
  pager.Next=Button(content,"下一页",520,570,112,()=>navigate(pager.Index+1));
  return pager;
 }
 void RenderCatalogRows(FlowLayoutPanel list,bool resetScroll,Action build){
  var oldScroll=list.AutoScrollPosition;
  list.SuspendLayout();
  try{
   list.AutoScrollPosition=Point.Empty;
   foreach(Control child in list.Controls.Cast<Control>().ToArray())child.Dispose();
   build();
   if(!rebuilding)foreach(Control row in list.Controls)ScaleLiveCatalogRow(row);
  }finally{list.ResumeLayout(true);}
  if(!resetScroll)list.AutoScrollPosition=new Point(-oldScroll.X,-oldScroll.Y);
 }
 void CatalogCard(FlowLayoutPanel list,string title,string summary,string hint,Action open,string symbol,string gameId="",string gamePath=""){var row=new RoundedPanel{Size=new Size(632,148),Margin=new Padding(0,0,0,14),Cursor=Cursors.Hand};list.Controls.Add(row);var icon=new CatalogIcon{Location=new Point(22,36),Symbol=symbol,GameId=gameId,GamePath=gamePath,AccessibleName=title+"图标"};row.Controls.Add(icon);var name=Label(row,title,106,17,312,34,17,Theme.Ink,true);var description=Label(row,summary,106,61,312,27,10,Theme.Muted);var statusLabel=Label(row,hint,106,102,312,24,9,Theme.Purple);row.Click+=(s,e)=>open();icon.Click+=(s,e)=>open();name.Click+=(s,e)=>open();description.Click+=(s,e)=>open();statusLabel.Click+=(s,e)=>open();Button(row,gameId==""?"打开工具  →":"游戏设置  →",440,54,168,open,true);}
 void BuildTools(){Heading("实用工具","选择需要的工具；列表支持鼠标滚轮上下浏览。");var list=CatalogList("实用工具列表",102,496);foreach(var tool in UtilityCatalog.Items){var selected=tool;CatalogCard(list,tool.Name,tool.Summary,tool.Hint,()=>{if(selected.Id=="pomodoro")app.ShowCountdown();else Navigate(selected.Id);},tool.Icon);}toolsClock=Label(content,"",1,612,650,27,10,Theme.Purple);UpdateTools();}
 void UpdateTools(){UpdatePlans();if(page=="tools"&&toolsClock!=null)toolsClock.Text=app.Countdown.Active?"番茄钟 · "+(app.Countdown.Paused?"已暂停 · ":"剩余 · ")+app.Countdown.Display:"先专注一会，再安排下一件事。";RefreshSchedules(DateTime.Now);}
 void BuildSchedules(){
  Label(content,"日程提醒",0,0,490,52,23,Theme.Ink,true);Label(content,"常规日程、特殊事件和每日计划，都在这里安排。",1,58,640,36,10,Theme.Muted);Button(content,"返回实用工具",508,7,150,()=>Navigate("tools"));Button(content,"添加日程",0,104,164,()=>app.EditSchedule(),true);Label(content,"计划任务可直接打勾，完成记录同步保存。",184,111,465,27,9,Theme.Muted);
  scheduleList=CatalogList("日程列表",158,404);
  schedulePager=AddCatalogPager(index=>RefreshSchedules(DateTime.Now,index));
  Label(content,"按本机时间提醒；软件关闭期间不会弹窗，重启补发 24 小时内最近一次提醒。",1,620,650,35,9,Theme.Muted);
  RefreshSchedules(DateTime.Now);
 }
 internal void RefreshSchedules(DateTime now,int? requestedPage=null){
  if(scheduleList==null||scheduleList.IsDisposed)return;
  var minute=now.AddTicks(-(now.Ticks%TimeSpan.TicksPerMinute));
  bool dataChanged=scheduleVersion!=app.ScheduleVersion;
  if(!dataChanged&&scheduleMinute==minute&&!requestedPage.HasValue)return;
  scheduleVersion=app.ScheduleVersion;scheduleMinute=minute;
  var items=app.Schedules.Items;
  bool pageChanged=schedulePager.Update(items.Count,requestedPage);
  var visible=items.Select(entry=>new{Entry=entry,Next=entry.Next(now)})
   .OrderBy(item=>item.Entry.Enabled?0:1).ThenBy(item=>item.Next??DateTime.MaxValue)
   .Skip(schedulePager.Start).Take(CatalogPageSize).ToArray();

  if(!dataChanged&&visible.Select(item=>item.Entry).SequenceEqual(scheduleCards.Select(card=>card.Entry))){
   for(int i=0;i<visible.Length;i++)scheduleCards[i].UpdateTime(visible[i].Next,now);
   return;
  }

  RenderCatalogRows(scheduleList,pageChanged,()=>{
   scheduleCards.Clear();
   if(visible.Length==0){
    var empty=new RoundedPanel{Size=new Size(632,156),Margin=new Padding(0)};scheduleList.Controls.Add(empty);
    Label(empty,"还没有日程",24,24,580,32,16,Theme.Ink,true);Label(empty,"点击上方“添加日程”，或在“制定计划”生成每日任务。",24,76,580,50,10,Theme.Muted);
   }
   foreach(var item in visible)AddScheduleCard(item.Entry,item.Next,now);
  });
 }

 void AddScheduleCard(ScheduleEntry entry,DateTime? next,DateTime now){
   var goal=app.FindPlan(entry.PlanId);var planned=goal==null?null:goal.Days.FirstOrDefault(d=>d.Id==entry.PlanDayId);bool linked=planned!=null;
   var row=new RoundedPanel{Size=new Size(632,174),Margin=new Padding(0,0,0,12)};scheduleList.Controls.Add(row);Label(row,(linked&&planned.Done?"✓ ":"")+entry.Title,20,14,400,30,13,Theme.Ink,true);Label(row,linked?planned.CompletedEarly?(planned.KeepEarlyDateFree?"提前完成 · 当天休息":"计划已提前完成"):planned.Done?"计划已完成":planned.State=="missed"?"未完成，已反馈":"每日计划":entry.Enabled?"提醒已开启":"提醒已关闭",450,18,160,24,9,Theme.Purple);Label(row,entry.RuleLabel,20,51,590,24,10,Theme.Purple);Label(row,string.IsNullOrWhiteSpace(entry.Notes)?"无备注":entry.Notes,20,84,400,48,9,Theme.Muted);
   var timing=Label(row,"",20,139,400,24,9,Theme.Muted);
   var check=new SoftCheck{Checked=linked?planned.Done:entry.Enabled,Location=new Point(450,82),Size=new Size(160,30),Font=Theme.Font(9),AccessibleName=linked?planned.Date+"日程完成状态":"启用提醒"};
   if(linked)BindPlanDayCheck(check,goal.Id,planned.Id);
   else{
    check.Text="启用提醒";
    bool restoringCheck=false;
    check.CheckedChanged+=(s,e)=>{
     if(restoringCheck)return;
     try{
      app.ToggleSchedule(entry.Id,check.Checked);RefreshSchedules(DateTime.Now);
     }catch(Exception ex){if(!IsDisposed)MessageBox.Show(this,ex.Message+"\n\n"+Theme.Credit,"日程未更新");}
     finally{
      // Cancelling the date editor for a past reminder leaves it disabled.
      if(!check.IsDisposed&&check.Checked!=entry.Enabled){restoringCheck=true;check.Checked=entry.Enabled;restoringCheck=false;}
     }
    };
   }
   row.Controls.Add(check);
   Button(row,linked?"计划":"编辑",450,122,72,()=>{if(linked)OpenPlan(goal.Id);else app.EditSchedule(entry);}).Height=32;
   var last=Button(row,linked?"未完成":"删除",534,122,76,()=>{if(linked)app.AskPlanFeedback(goal.Id,planned.Id);else app.DeleteSchedule(entry);});last.Height=32;
   var card=new ScheduleCard{Entry=entry,Day=planned,Timing=timing,Feedback=linked?last:null};
   card.UpdateTime(next,now);scheduleCards.Add(card);
 }
}
}
