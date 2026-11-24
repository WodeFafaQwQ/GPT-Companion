using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace DragonCompanion {
public sealed class ToolTextInput:UserControl {
 public readonly TextBox Editor;
 public ToolTextInput(bool multiline=false){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Color.FromArgb(251,250,254);Size=new Size(572,multiline?84:44);Editor=new TextBox{BorderStyle=BorderStyle.None,BackColor=Color.White,ForeColor=Theme.Ink,Font=Theme.Font(10),Multiline=multiline,MaxLength=multiline?1000:120,ScrollBars=multiline?ScrollBars.Vertical:ScrollBars.None};Controls.Add(Editor);Editor.GotFocus+=(s,e)=>Invalidate();Editor.LostFocus+=(s,e)=>Invalidate();Resize+=(s,e)=>Arrange();Arrange();}
 void Arrange(){if(Editor!=null)Editor.Bounds=new Rectangle(12,11,Width-24,Height-22);}
 protected override void OnPaintBackground(PaintEventArgs e){Theme.Background(this,e.Graphics);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var rect=new RectangleF(1,1,Width-3,Height-3);Theme.Box(e.Graphics,rect,Color.White,12);using(var pen=new Pen(Editor.Focused?Theme.Purple:Color.FromArgb(190,170,226),Editor.Focused?2:1.2f))using(var path=Theme.Round(rect,12))e.Graphics.DrawPath(pen,path);}
}
public sealed class ScheduleDialog:Form {
 readonly SoftNumber year,month,day,hour,minute;readonly ToolTextInput title,notes;readonly SoftSelect repeat;readonly SoftCheck[] weekdays=new SoftCheck[7];readonly Panel weekly;readonly SurfaceLabel error;readonly ScheduleEntry original;
 public ScheduleEntry Result;
 public ScheduleDialog(ScheduleEntry entry=null){original=entry==null?new ScheduleEntry():entry.Copy();Text=entry==null?"GPT · 添加日程":"GPT · 编辑日程";ClientSize=new Size(620,658);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(251,250,254);Font=Theme.Font(10);ForeColor=Theme.Ink;AutoScaleMode=AutoScaleMode.None;Icon=Art.MakeIcon();
  Label(entry==null?"添加日程":"编辑日程",24,18,572,42,20,true);Label("日期与时间按本机时区提醒。",24,62,572,24,9);Label("日期",24,87,300,23,10,true);Label("时间",370,87,200,23,10,true);
  var date=entry==null?DateTime.Now.AddMinutes(5):entry.Start;
  year=Number(24,114,90,2000,2099,date.Year,"年份");month=Number(148,114,60,1,12,date.Month,"月份");day=Number(242,114,60,1,31,date.Day,"日期");hour=Number(370,114,70,0,23,date.Hour,"小时");minute=Number(465,114,70,0,59,date.Minute,"分钟");Label("年",119,121,24,24);Label("月",214,121,24,24);Label("日",308,121,24,24);Label(":",446,120,16,24,12,true);
  year.ValueChanged+=(s,e)=>ClampDay();month.ValueChanged+=(s,e)=>ClampDay();ClampDay();
  Label("事件",24,164,572,24,10,true);title=new ToolTextInput{Location=new Point(24,194)};title.Editor.Text=original.Title;title.Editor.AccessibleName="事件";Controls.Add(title);
  Label("备注",24,250,572,24,10,true);notes=new ToolTextInput(true){Location=new Point(24,282)};notes.Editor.Text=original.Notes;notes.Editor.AccessibleName="备注";Controls.Add(notes);
  Label("重复",24,382,84,24,10,true);repeat=new SoftSelect{Location=new Point(130,374),Width=200,AccessibleName="重复方式"};repeat.Items.AddRange(ScheduleEntry.RepeatNames);repeat.SelectedIndex=Math.Max(0,Array.IndexOf(ScheduleEntry.RepeatKeys,original.Repeat));Controls.Add(repeat);
  weekly=new Panel{Location=new Point(24,430),Size=new Size(572,38),BackColor=BackColor};Controls.Add(weekly);
  for(int i=0;i<7;i++){int dow=(i+1)%7;var check=new SoftCheck{Text=new[]{"周日","周一","周二","周三","周四","周五","周六"}[dow],Location=new Point(i*81,0),Size=new Size(76,32),Font=Theme.Font(9),Checked=entry!=null?original.Days.Contains(dow):(int)date.DayOfWeek==dow};weekdays[dow]=check;weekly.Controls.Add(check);}
  repeat.SelectedIndexChanged+=(s,e)=>{weekly.Visible=repeat.SelectedIndex==2;if(weekly.Visible&&entry==null){var selected=SelectedTime();foreach(int dow in Enumerable.Range(0,7))weekdays[dow].Checked=(int)selected.DayOfWeek==dow;}};weekly.Visible=repeat.SelectedIndex==2;
  Label("每周可选多个星期；每月、每年遇到不存在的日期，按当月最后一天提醒。",24,480,572,40,9);error=Label("",24,526,572,30,9);error.ForeColor=Color.FromArgb(176,72,104);
  var save=new SoftButton("保存日程",true){Location=new Point(24,566),Width=170};save.Click+=(s,e)=>SaveEntry();Controls.Add(save);var cancel=new SoftButton("返回"){Location=new Point(208,566),Width=120};cancel.Click+=(s,e)=>{DialogResult=DialogResult.Cancel;Close();};Controls.Add(cancel);CancelButton=null;
  Label(Theme.Credit,24,626,572,24,9);
 }
 SurfaceLabel Label(string text,int x,int y,int w,int h,float size=10,bool bold=false){var label=new SurfaceLabel{Text=text,Bounds=new Rectangle(x,y,w,h),Font=Theme.Font(size,bold),ForeColor=bold?Theme.Ink:Theme.Muted};Controls.Add(label);return label;}
 SoftNumber Number(int x,int y,int w,int min,int max,int value,string name){var number=new SoftNumber{Compact=true,Location=new Point(x,y),Width=w,Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),AccessibleName=name};Controls.Add(number);return number;}
 void ClampDay(){day.Maximum=DateTime.DaysInMonth((int)year.Value,(int)month.Value);if(day.Value>day.Maximum)day.Value=day.Maximum;}
 DateTime SelectedTime(){return new DateTime((int)year.Value,(int)month.Value,(int)day.Value,(int)hour.Value,(int)minute.Value,0,DateTimeKind.Unspecified);}
 void SaveEntry(){try{var entry=original.Copy();entry.Title=title.Editor.Text.Trim();entry.Notes=notes.Editor.Text.Trim();entry.Start=SelectedTime();entry.Repeat=ScheduleEntry.RepeatKeys[repeat.SelectedIndex];entry.WeeklyDays=Enumerable.Range(0,7).Where(d=>weekdays[d].Checked).ToArray();entry.Enabled=true;entry.Validate(DateTime.Now);Result=entry;DialogResult=DialogResult.OK;Close();}catch(ArgumentException ex){error.Text=ex.Message;}}
}
}
