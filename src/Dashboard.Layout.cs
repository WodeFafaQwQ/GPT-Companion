using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DragonCompanion {
public sealed partial class Dashboard {
 Panel sidebar,viewport;SurfaceLabel footer;TextBox homeEditor;
 string homeDraft="";bool rebuilding,adaptiveReady,compactSidebar;int homeLogicalWidth=658,homeLogicalHeight=660;
 float dpiScale=1,appliedFontFactor=1;
 readonly Dictionary<string,Font> adaptiveFonts=new Dictionary<string,Font>();
 readonly Timer resizeTimer=new Timer{Interval=16};
 readonly Timer windowSaveTimer=new Timer{Interval=300};
 DateTime lastResizeFrame;
 Size lastLayoutSize;
 bool draggingWindow,liveResizeQueued,openRefreshQueued;
 readonly Dictionary<Control,DesignControl> design=new Dictionary<Control,DesignControl>();
 sealed class BufferedPanel:Panel {public BufferedPanel(){DoubleBuffered=true;ResizeRedraw=true;}}
 public float InterfaceFontSize {get{return 10*FontFactor;}}
 public static float WindowFontScale(Size client,float deviceScale){float dpi=Math.Max(1,deviceScale);float fit=Math.Min(client.Width/(900f*dpi),client.Height/(716f*dpi));return Math.Max(.8f,Math.Min(1.6f,fit));}
 float FontFactor {get{return appliedFontFactor;}}
 void UpdateAdaptiveScale(){appliedFontFactor=WindowFontScale(ClientSize,dpiScale);}
 Font AdaptiveFont(float points,bool bold=false){float size=(float)Math.Round(points*FontFactor*20)/20;string key=size.ToString(System.Globalization.CultureInfo.InvariantCulture)+(bold?"b":"n");Font font;if(!adaptiveFonts.TryGetValue(key,out font)){font=Theme.Font(size,bold);adaptiveFonts.Add(key,font);}return font;}
 float UiUnit {get{return dpiScale*FontFactor;}}
 int Unit(float value){return Math.Max(1,(int)Math.Round(value*UiUnit));}
 bool UseCompactNavigation(){return ClientSize.Width<Unit(190+52+350);}
 void BuildNavigation(){compactSidebar=UseCompactNavigation();sidebar=new BufferedPanel{Location=Point.Empty,Size=new Size(190,716),BackColor=Color.FromArgb(241,236,250),AutoScroll=true};Controls.Add(sidebar);if(!compactSidebar){Label(sidebar,"✦",24,23,68,56,26,Theme.Purple,true);Label(sidebar,"GPT",24,82,150,34,18,Theme.Ink,true);Label(sidebar,"DESKTOP\nCOMPANION",25,121,160,42,8,Theme.Muted,true);Button(sidebar,"陪伴与换装",16,181,158,()=>Navigate("home"),page=="home");Button(sidebar,"任务与额度",16,232,158,()=>Navigate("tasks"),page=="tasks");Button(sidebar,"桌宠设置",16,283,158,()=>Navigate("settings"),page=="settings"||page=="native"||page=="positions"||page=="appearance");Button(sidebar,"游戏中心",16,334,158,()=>Navigate("games"),page=="games"||page.StartsWith("game-"));Button(sidebar,"实用工具",16,385,158,()=>Navigate("tools"),page=="tools"||page=="schedules"||page=="plans"||page=="plan-detail"||page.StartsWith("vocabulary",StringComparison.Ordinal));sidebar.Controls.Add(new ArtView{Location=new Point(40,429),Size=new Size(110,114),Style=app.Settings.Style,Circle=false});Label(sidebar,"小小陪伴，\n认真做事。",16,548,158,52,9,Theme.Muted).TextAlign=ContentAlignment.MiddleCenter;Label(sidebar,"独立桌面软件 · v"+AppVersion.Current+"\n非官方社区灵感创作",24,612,155,48,8,Theme.Muted);}else{Label(sidebar,"✦",6,22,52,56,26,Theme.Purple,true).TextAlign=ContentAlignment.MiddleCenter;Label(sidebar,"GPT",6,83,52,34,12,Theme.Ink,true).TextAlign=ContentAlignment.MiddleCenter;string[] names={"陪伴","任务","设置","游戏","工具"},pages={"home","tasks","settings","games","tools"},full={"陪伴与换装","任务与额度","桌宠设置","游戏中心","实用工具"};for(int i=0;i<names.Length;i++){string targetPage=pages[i];var button=Button(sidebar,names[i],6,140+i*53,52,()=>Navigate(targetPage),page==targetPage);button.AccessibleName=full[i];}sidebar.Controls.Add(new ArtView{Location=new Point(4,411),Size=new Size(56,82),Style=app.Settings.Style,Circle=false});Label(sidebar,"v"+AppVersion.Current,3,511,58,24,7,Theme.Muted).TextAlign=ContentAlignment.MiddleCenter;}}
 public static Size FitClientToScreen(Size requested,Rectangle work,Size frame){int maxWidth=Math.Max(240,work.Width-frame.Width),maxHeight=Math.Max(200,work.Height-frame.Height);return new Size(Math.Max(Math.Min(640,maxWidth),Math.Min(maxWidth,requested.Width)),Math.Max(Math.Min(440,maxHeight),Math.Min(maxHeight,requested.Height)));}
 void InitializeAdaptiveWindow(){using(var g=CreateGraphics())dpiScale=Math.Max(1,g.DpiX/96f);var work=Screen.FromControl(this).WorkingArea;var frame=Size-ClientSize;MinimumSize=new Size(Math.Min(work.Width,640+frame.Width),Math.Min(work.Height,440+frame.Height));MaximumSize=Size.Empty;var wanted=new Size(app.Settings.PanelWidth>0?app.Settings.PanelWidth:(int)(900*dpiScale),app.Settings.PanelHeight>0?app.Settings.PanelHeight:(int)(716*dpiScale));ClientSize=FitClientToScreen(wanted,work,frame);Location=new Point(work.Left+(work.Width-Width)/2,work.Top+(work.Height-Height)/2);}
 // During native edge drags follow the latest client size on the next frame.
 // Only isolated size changes (such as maximizing) need the short font transition.
 void RefreshAdaptiveLayout(bool animate=false,bool force=false){
  if(IsDisposed||rebuilding||WindowState==FormWindowState.Minimized)return;
  Size client=ClientSize;float previous=FontFactor,target=WindowFontScale(client,dpiScale);
  DateTime now=DateTime.UtcNow;double elapsed=Math.Max(1,Math.Min(80,(now-lastResizeFrame).TotalMilliseconds));lastResizeFrame=now;
  appliedFontFactor=animate?(float)(previous+(target-previous)*(1-Math.Exp(-elapsed/45))):target;
  if(Math.Abs(target-FontFactor)<.0005f)appliedFontFactor=target;
  bool scaleChanged=previous!=FontFactor;
  if(!force&&client==lastLayoutSize&&!scaleChanged)return;
  SuspendLayout();viewport.SuspendLayout();try{
   bool changedNavigation=UseCompactNavigation()!=compactSidebar;
   if(changedNavigation){var old=sidebar;foreach(var control in design.Keys.Where(c=>c==old||IsChildOf(c,old)).ToArray())design.Remove(control);old.Dispose();BuildNavigation();}
   ArrangeViewport();
   bool changedHome=page=="home"&&UpdateHomeGeometry();
   bool changedVocabulary=ArrangeVocabularyStudy();
   if(force||scaleChanged||changedNavigation)ScaleDesign(sidebar);
   if(force||scaleChanged||changedHome||changedVocabulary)ScaleDesign(content);
   SetAdaptiveContentSize();
  }finally{viewport.ResumeLayout(true);ResumeLayout(true);}
  lastLayoutSize=client;
 }
 internal void ShowAndRefresh(){
  if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;
  Show();
  Activate();
  if(openRefreshQueued)return;
  openRefreshQueued=true;
  // Opening creates child handles and can change the actual viewport size.
  // Reconcile the layout after those messages, then repaint existing children.
  BeginInvoke((Action)(()=>{
   openRefreshQueued=false;
   if(IsDisposed||!Visible||WindowState==FormWindowState.Minimized)return;
   resizeTimer.Stop();
   RefreshAdaptiveLayout(force:true);
   Invalidate(true);
  }));
 }
 static bool IsChildOf(Control child,Control parent){for(var p=child.Parent;p!=null;p=p.Parent)if(p==parent)return true;return false;}
 void EnableAdaptiveWindow(){
  adaptiveReady=true;
  resizeTimer.Tick+=(s,e)=>{RefreshAdaptiveLayout(true);if(WindowState==FormWindowState.Minimized||Math.Abs(WindowFontScale(ClientSize,dpiScale)-FontFactor)<.0005f)resizeTimer.Stop();};
  windowSaveTimer.Tick+=(s,e)=>{windowSaveTimer.Stop();RememberWindow();};
  ClientSizeChanged+=(s,e)=>{if(!adaptiveReady||rebuilding||WindowState==FormWindowState.Minimized)return;if(draggingWindow){QueueLiveResize();return;}if(!resizeTimer.Enabled){lastResizeFrame=DateTime.UtcNow;resizeTimer.Start();}QueueWindowSave();};
  ResizeBegin+=(s,e)=>{draggingWindow=true;resizeTimer.Stop();windowSaveTimer.Stop();};
  ResizeEnd+=(s,e)=>{draggingWindow=false;resizeTimer.Stop();RefreshAdaptiveLayout();Invalidate(true);QueueWindowSave();};
  Disposed+=(s,e)=>{resizeTimer.Dispose();windowSaveTimer.Dispose();foreach(var font in adaptiveFonts.Values)font.Dispose();adaptiveFonts.Clear();};
  Shown+=(s,e)=>{if(app.Settings.PanelMaximized&&!app.Diagnostics)WindowState=FormWindowState.Maximized;};
 }
 void QueueLiveResize(){
  if(liveResizeQueued||!IsHandleCreated)return;
  liveResizeQueued=true;
  BeginInvoke((Action)(()=>{liveResizeQueued=false;if(draggingWindow)RefreshAdaptiveLayout();}));
 }
 void QueueWindowSave(){windowSaveTimer.Stop();windowSaveTimer.Start();}
 void CaptureHomeDraft(){if(homeEditor!=null&&!homeEditor.IsDisposed)homeDraft=homeEditor.Text;homeEditor=null;}
 int AvailableHomeWidth(){if(viewport==null)return 658;return Math.Max(300,(int)Math.Floor((viewport.Width-SystemInformation.VerticalScrollBarWidth-2)/UiUnit));}
 void ArrangeViewport(){if(sidebar==null||viewport==null)return;int side=Unit(compactSidebar?64:190)+(ClientSize.Height<Unit(compactSidebar?555:686)?SystemInformation.VerticalScrollBarWidth:0),gap=Unit(26),top=Unit(23),credit=Unit(24);sidebar.SetBounds(0,0,side,ClientSize.Height);sidebar.AutoScrollMinSize=new Size(0,Unit(compactSidebar?555:686));int left=side+gap;viewport.SetBounds(left,top,Math.Max(120,ClientSize.Width-left-gap),Math.Max(100,ClientSize.Height-top-credit-Unit(6)));if(footer!=null){footer.SetBounds(left,ClientSize.Height-credit,Math.Max(120,ClientSize.Width-left-gap),credit);var font=AdaptiveFont(9);if(!ReferenceEquals(footer.Font,font))footer.Font=font;}}
 sealed class DesignControl {public Control Control;public Rectangle Bounds;public Padding Margin,Padding;public float FontSize;public FontStyle FontStyle;public int[] Columns;}
 void CaptureDesign(Control root,List<DesignControl> controls){foreach(Control child in root.Controls){DesignControl item;if(!design.TryGetValue(child,out item)){var list=child as ListView;item=new DesignControl{Control=child,Bounds=child.Bounds,Margin=child.Margin,Padding=child.Padding,FontSize=child.Font.SizeInPoints,FontStyle=child.Font.Style,Columns=list==null?null:list.Columns.Cast<ColumnHeader>().Select(c=>c.Width).ToArray()};design.Add(child,item);}controls.Add(item);CaptureDesign(child,controls);}}
 static Padding ScalePadding(Padding p,float scale){return new Padding((int)Math.Round(p.Left*scale),(int)Math.Round(p.Top*scale),(int)Math.Round(p.Right*scale),(int)Math.Round(p.Bottom*scale));}
 void ScaleDesign(Control root){
  var controls=new List<DesignControl>();CaptureDesign(root,controls);float unit=UiUnit;
  root.SuspendLayout();foreach(var item in controls)item.Control.SuspendLayout();try{
   foreach(var item in controls){
    var c=item.Control;var r=item.Bounds;
    var bounds=new Rectangle((int)Math.Round(r.X*unit),(int)Math.Round(r.Y*unit),Math.Max(1,(int)Math.Round(r.Right*unit)-(int)Math.Round(r.X*unit)),Math.Max(1,(int)Math.Round(r.Bottom*unit)-(int)Math.Round(r.Y*unit)));
    if(c.Bounds!=bounds)c.Bounds=bounds;
    var margin=ScalePadding(item.Margin,unit);if(c.Margin!=margin)c.Margin=margin;
    var padding=ScalePadding(item.Padding,unit);if(c.Padding!=padding)c.Padding=padding;
    var font=AdaptiveFont(item.FontSize,(item.FontStyle&FontStyle.Bold)!=0);if(!ReferenceEquals(c.Font,font))c.Font=font;
    var list=c as ListView;if(list!=null&&item.Columns!=null)for(int i=0;i<list.Columns.Count;i++){int width=(int)Math.Round(item.Columns[i]*unit);if(list.Columns[i].Width!=width)list.Columns[i].Width=width;}
   }
  }finally{
   for(int i=controls.Count-1;i>=0;i--)controls[i].Control.ResumeLayout(true);
   root.ResumeLayout(true);
  }
 }
 void SetAdaptiveContentSize(){content.Size=new Size(page=="home"?Unit(homeLogicalWidth):page=="vocabulary-cards"?Unit(vocabularyStudyWidth):Math.Max(Unit(658),viewport.ClientSize.Width),Unit(page=="home"?homeLogicalHeight:660));viewport.AutoScrollMinSize=content.Size;}
 void ApplyAdaptivePage(){ScaleDesign(sidebar);ScaleDesign(content);ArrangeViewport();SetAdaptiveContentSize();lastLayoutSize=ClientSize;}
 void ScaleLiveCatalogRow(Control row){foreach(var dead in design.Keys.Where(c=>c.IsDisposed).ToArray())design.Remove(dead);if(!design.ContainsKey(row))design.Add(row,new DesignControl{Control=row,Bounds=row.Bounds,Margin=row.Margin,Padding=row.Padding,FontSize=row.Font.SizeInPoints,FontStyle=row.Font.Style});var item=design[row];row.Size=new Size(Unit(item.Bounds.Width),Unit(item.Bounds.Height));row.Margin=ScalePadding(item.Margin,UiUnit);ScaleDesign(row);}
 public void SetWindowClientSize(Size requested){resizeTimer.Stop();WindowState=FormWindowState.Normal;var work=Screen.FromControl(this).WorkingArea;ClientSize=FitClientToScreen(requested,work,Size-ClientSize);Location=new Point(Math.Max(work.Left,Math.Min(work.Right-Width,Left)),Math.Max(work.Top,Math.Min(work.Bottom-Height,Top)));RefreshAdaptiveLayout();resizeTimer.Stop();windowSaveTimer.Stop();RememberWindow();}
 void RememberWindow(){if(!adaptiveReady||rebuilding||WindowState==FormWindowState.Minimized||app.Diagnostics)return;app.Settings.PanelMaximized=WindowState==FormWindowState.Maximized;if(WindowState==FormWindowState.Normal){app.Settings.PanelWidth=ClientSize.Width;app.Settings.PanelHeight=ClientSize.Height;}app.Save();}
 protected override void WndProc(ref Message message){base.WndProc(ref message);if(message.Msg==0x7e&&adaptiveReady&&!IsDisposed)BeginInvoke((Action)(()=>{if(IsDisposed)return;if(WindowState==FormWindowState.Normal)SetWindowClientSize(ClientSize);else ArrangeViewport();}));}
 void BuildAppearance(){Label(content,"界面与窗口",0,0,490,52,23,Theme.Ink,true);Label(content,"缩放窗口时，文字与控件一起自适应；窗口大小自动记住。",1,58,640,36,10,Theme.Muted);Button(content,"返回桌宠设置",508,7,150,()=>Navigate("settings"));var text=Card(content,0,104,658,123);Label(text,"字体随窗口自动缩放",22,17,610,31,15,Theme.Ink,true);Label(text,"无需单独调整字号。窗口放大时文字随之放大，缩小时同步缩小。",22,61,610,44,10,Theme.Muted);
  var window=Card(content,0,243,658,263);Label(window,"窗口大小",22,17,250,31,15,Theme.Ink,true);Label(window,"可拖动窗口边缘调整大小，也支持最大化和以下尺寸。",22,56,610,29,10,Theme.Muted);var width=new SoftNumber{Location=new Point(108,101),Width=185,Minimum=640,Maximum=7680,Value=ClientSize.Width,AccessibleName="窗口宽度"};var height=new SoftNumber{Location=new Point(402,101),Width=185,Minimum=440,Maximum=4320,Value=ClientSize.Height,AccessibleName="窗口高度"};Label(window,"宽度",22,108,74,28,11,Theme.Ink);Label(window,"高度",316,108,74,28,11,Theme.Ink);window.Controls.Add(width);window.Controls.Add(height);Button(window,"应用大小",22,157,190,()=>SetWindowClientSize(new Size((int)width.Value,(int)height.Value)),true);Button(window,"适应当前屏幕",228,157,204,()=>{var area=Screen.FromControl(this).WorkingArea;SetWindowClientSize(new Size((int)(area.Width*.85),(int)(area.Height*.85)));});Button(window,"恢复默认窗口",448,157,188,()=>SetWindowClientSize(new Size((int)(900*dpiScale),(int)(716*dpiScale))));Label(window,"窗口超出屏幕时会自动收回可见区域，重启后沿用上次大小。",22,212,612,33,9,Theme.Muted);Label(content,"字体与控件按可用宽高等比例缩放，窄窗口会自动切换单列布局。\n内容较多时可滚动查看，输入中的文字会保留。",2,530,650,78,11,Theme.Muted);
 }
}
}
