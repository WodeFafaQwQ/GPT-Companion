using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DragonCompanion {
public static partial class SelfTests {
 static IEnumerable<Control> Descendants(Control root){foreach(Control child in root.Controls){yield return child;foreach(var descendant in Descendants(child))yield return descendant;}}
 static string AdaptiveControlName(Control control){return control.GetType().Name+" '"+(string.IsNullOrEmpty(control.AccessibleName)?control.Text:control.AccessibleName)+"' "+control.Bounds;}
 static string AdaptiveOverlap(Control root){var children=root.Controls.Cast<Control>().Where(c=>c.Visible&&!(c is ScrollBar)).ToArray();for(int i=0;i<children.Length;i++){for(int j=i+1;j<children.Length;j++)if(children[i].Bounds.IntersectsWith(children[j].Bounds))return AdaptiveControlName(children[i])+" / "+AdaptiveControlName(children[j]);var nested=AdaptiveOverlap(children[i]);if(nested!="")return nested;}return "";}
 static void AdaptiveClick(Control control){Check(control!=null,"Adaptive action exists");typeof(Control).GetMethod("OnClick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(control,new object[]{EventArgs.Empty});Application.DoEvents();}
 static void AdaptiveSnapshot(Dashboard panel,string folder,string name){using(var bitmap=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine(folder,name+".png"),ImageFormat.Png);}}
 static TextBox AdaptiveEditor(Dashboard panel){return Descendants(panel).OfType<ChatInput>().Single().Editor;}
 static void TestDashboardReopen(Controller controller,Dashboard panel){
  panel.Navigate("home");
  var editor=AdaptiveEditor(panel);
  editor.Text="重开后保留的草稿";
  var heading=FindText(panel,"今天，也一起好好做事。");
  var navigation=FindText(panel,"陪伴与换装")??Descendants(panel).First(c=>c.AccessibleName=="陪伴与换装");
  int invalidations=0;
  InvalidateEventHandler onInvalidate=(s,e)=>invalidations++;
  panel.Invalidated+=onInvalidate;
  try{
   panel.Close();
   Check(!panel.Visible&&!panel.IsDisposed,"Closing the companion center hides the retained window");
   panel.ClientSize=new Size(900,716);
   controller.ShowPanel();
   Application.DoEvents();
   Check(panel.Visible&&heading.Visible&&navigation.Visible&&invalidations>0,"Reopening refreshes the companion center and invalidates its child surfaces for repaint");
   Check(ReferenceEquals(editor,AdaptiveEditor(panel))&&editor.Text=="重开后保留的草稿","Reopening preserves the active page and unsent chat draft");
   invalidations=0;
   panel.WindowState=FormWindowState.Minimized;
   controller.ShowPanel();
   Application.DoEvents();
   Check(panel.WindowState==FormWindowState.Normal&&invalidations>0&&heading.Visible,"Opening a minimized companion center restores and repaints it");
  }finally{panel.Invalidated-=onInvalidate;editor.Clear();}
 }
 static bool AdaptiveReachable(Control target){
  // Scroll only ancestors of the requested control. Its position in a large
  // scrollable page is valid; clipping by a non-scrollable card is not.
  var ancestors=new List<Control>();for(Control parent=target.Parent;parent!=null;parent=parent.Parent)ancestors.Add(parent);
  foreach(var ancestor in ancestors.AsEnumerable().Reverse()){var scroll=ancestor as ScrollableControl;if(scroll!=null&&scroll.AutoScroll)scroll.ScrollControlIntoView(target);}
  Application.DoEvents();
  var rectangle=target.RectangleToScreen(target.ClientRectangle);
  foreach(var ancestor in ancestors){var scroll=ancestor as ScrollableControl;var available=ancestor.RectangleToScreen(ancestor.ClientRectangle);if(scroll!=null&&scroll.AutoScroll){if(!available.IntersectsWith(rectangle))return false;}else if(!available.Contains(rectangle)){results.Add("CLIPPED "+AdaptiveControlName(target)+" rectangle="+rectangle+" parent="+AdaptiveControlName(ancestor)+" available="+available);return false;}}
  return target.Visible&&target.Enabled;
 }
 static bool AdaptiveTextFits(Control control){using(var graphics=control.CreateGraphics())using(var format=new StringFormat()){var measured=graphics.MeasureString(control.Text,control.Font,new SizeF(Math.Max(1,control.ClientSize.Width),10000),format);return measured.Height<=control.ClientSize.Height+2;}}
 static void TestSchedulePages(Controller controller,Dashboard panel,string folder){
  var original=controller.Schedules;
  var start=DateTime.Now.Date.AddDays(2).AddHours(9);
  var book=new ScheduleBook();
  for(int i=1;i<=365;i++)book.Items.Add(new ScheduleEntry{Title="大量日程验证 "+i,Start=start.AddDays(i-1)});
  book.Items.Reverse();
  controller.Schedules=book;controller.ScheduleVersion++;
  try{
   panel.Navigate("tools");
   var watch=System.Diagnostics.Stopwatch.StartNew();
   AdaptiveClick(FindText(panel,"日程提醒"));
   panel.Update();watch.Stop();
   File.WriteAllText(Path.Combine(folder,"schedule-open-time.txt"),"365 entries, tools click to render: "+watch.ElapsedMilliseconds+" ms");
   var list=Descendants(panel).OfType<FlowLayoutPanel>().Single();
   var firstRows=list.Controls.Cast<Control>().ToArray();
   Check(firstRows.Length==8&&FindText(list,"大量日程验证 1")!=null&&FindText(list,"大量日程验证 9")==null,"Opening 365 schedules builds only the first eight cards in time order");
   Check(!FindText(panel,"上一页").Enabled&&FindText(panel,"下一页").Enabled&&FindText(panel,"第 1 / 46 页 · 共 365 项")!=null,"Schedule page controls show the item count and available directions");
   panel.RefreshSchedules(DateTime.Now.AddMinutes(2));
   Check(firstRows.SequenceEqual(list.Controls.Cast<Control>()),"An unchanged minute refresh preserves all visible schedule card controls");
   AdaptiveClick(FindText(panel,"下一页"));
   Check(list.Controls.Count==8&&FindText(list,"大量日程验证 9")!=null&&FindText(list,"大量日程验证 1")==null,"Next schedule page loads the next eight entries");
   panel.RefreshSchedules(DateTime.Now,45);
   Check(list.Controls.Count==5&&!FindText(panel,"下一页").Enabled&&FindText(list,"大量日程验证 365")!=null,"The final schedule page is reachable and shows all remaining entries");
   AdaptiveSnapshot(panel,folder,"ui-schedule-pagination");
   book.Items.RemoveAll(entry=>entry.Start>=start.AddDays(360));controller.ScheduleVersion++;
   panel.UpdateLive();
   Check(list.Controls.Count==8&&FindText(panel,"第 45 / 45 页 · 共 360 项")!=null&&FindText(list,"大量日程验证 360")!=null,"Removing the final page clamps navigation to the new last page");
   AdaptiveClick(FindText(panel,"上一页"));
   Check(FindText(list,"大量日程验证 345")!=null,"Previous schedule page navigates back and displays the correct reminders");
  }finally{controller.Schedules=original;controller.ScheduleVersion++;panel.Navigate("home");}
 }
 static void TestAdaptiveDashboard(Controller controller,Dashboard panel,string folder){
  Check(controller.Diagnostics,"Responsive UI checks use an isolated diagnostic controller");
  var defaults=new Settings();Check(defaults.PanelWidth==0&&defaults.PanelHeight==0&&!defaults.PanelMaximized&&typeof(Settings).GetField("PanelFontSize")==null,"Window defaults have automatic typography and no manual font preference");
  var legacy=Json.Serializer().Deserialize<Settings>("{\"PanelFontSize\":16,\"PanelWidth\":1080,\"PanelHeight\":700}");Check(legacy.PanelWidth==1080&&legacy.PanelHeight==700&&!Json.Encode(legacy).Contains("PanelFontSize"),"Legacy fixed-font settings are ignored without losing stored window dimensions");
  var stored=new Settings{PanelWidth=1080,PanelHeight=700,PanelMaximized=true};Store.Save("adaptive-settings-test.json",stored);var restored=Store.Load<Settings>("adaptive-settings-test.json");Check(restored.PanelWidth==1080&&restored.PanelHeight==700&&restored.PanelMaximized,"Window dimensions and maximize preference survive settings serialization");
  foreach(var work in new[]{new Rectangle(0,0,1366,728),new Rectangle(-800,80,800,560)})foreach(var requested in new[]{new Size(900,716),new Size(760,560),new Size(1280,820),new Size(0,0),new Size(7680,4320)}){var frame=new Size(16,39);var fitted=Dashboard.FitClientToScreen(requested,work,frame);Check(fitted.Width>0&&fitted.Height>0&&fitted.Width+frame.Width<=work.Width&&fitted.Height+frame.Height<=work.Height,"Complete window fits simulated "+work.Size+" for requested "+requested);}
  Check(Dashboard.WindowFontScale(new Size(640,440),1)<Dashboard.WindowFontScale(new Size(900,716),1)&&Dashboard.WindowFontScale(new Size(900,716),1)<Dashboard.WindowFontScale(new Size(1280,820),1),"Automatic type size grows from a small window through default to a larger window");
  Check(Dashboard.WindowFontScale(new Size(1800,1432),2)==Dashboard.WindowFontScale(new Size(900,716),1),"Automatic typography uses logical window size consistently across display DPI");
  Check(Dashboard.WindowFontScale(new Size(1000,717),1)>Dashboard.WindowFontScale(new Size(1000,716),1),"One-pixel window changes have a continuous font target rather than a two-percent jump");
  Check(panel.FormBorderStyle==FormBorderStyle.Sizable&&panel.MaximizeBox&&panel.MaximumSize==Size.Empty,"The companion center supports edge resizing and maximizing");
  var originalSize=panel.ClientSize;string originalStyle=controller.Settings.Style,draft="回头继续写 · 尚未发送的内容";
  try{
   TestDashboardReopen(controller,panel);
   panel.Navigate("home");AdaptiveEditor(panel).Text=draft;float smallHeading=0,largeHeading=0;
   foreach(var requested in new[]{new Size(640,440),new Size(760,560),new Size(900,716),new Size(1100,760),new Size(1280,820)}){
    panel.SetWindowClientSize(requested);panel.Location=new Point(-3000,-3000);Application.DoEvents();string context="Automatic home "+panel.ClientSize;var work=Screen.FromControl(panel).WorkingArea;Check(panel.Width<=work.Width&&panel.Height<=work.Height,context+" stays inside screen bounds");string conflict=AdaptiveOverlap(panel);Check(conflict=="",context+" has no overlapping controls"+(conflict==""?"":": "+conflict));var editor=AdaptiveEditor(panel);Check(editor.Text==draft,context+" preserves its unsent draft");
    var heading=FindText(panel,"今天，也一起好好做事。");var navigation=FindText(panel,"陪伴与换装")??Descendants(panel).First(c=>c.AccessibleName=="陪伴与换装");float scale=panel.InterfaceFontSize/10f;Check(Math.Abs(editor.Font.SizeInPoints-11*scale)<.15&&Math.Abs(heading.Font.SizeInPoints-23*scale)<.15&&Math.Abs(navigation.Font.SizeInPoints-10*scale)<.15,context+" synchronizes text sizes with the window scale");if(requested.Width==640)smallHeading=heading.Font.SizeInPoints;if(requested.Width==1280)largeHeading=heading.Font.SizeInPoints;
    var pixel=FindText(panel,"像素风");var anime=FindText(panel,"二次元风");Check(AdaptiveTextFits(heading)&&AdaptiveTextFits(pixel)&&AdaptiveTextFits(anime),context+" keeps heading and style names complete");if(panel.ClientSize.Width<=800)Check(pixel.Parent.Left==anime.Parent.Left&&anime.Parent.Top>=pixel.Parent.Bottom,context+" reflows style cards to one column");
    foreach(var action in Descendants(panel).OfType<SoftButton>().ToArray())Check(AdaptiveReachable(action),context+" can access "+action.Text);Check(AdaptiveReachable(editor),context+" can access the chat input");
    if(requested.Width==760||requested.Width==1280){foreach(var scroll in Descendants(panel).OfType<ScrollableControl>().Where(c=>c.AutoScroll))scroll.AutoScrollPosition=Point.Empty;AdaptiveSnapshot(panel,folder,requested.Width==760?"ui-home-auto-small":"ui-home-auto-large");}
    panel.Navigate("settings");AdaptiveClick(FindText(panel,"界面设置"));Check(FindText(panel,"字体随窗口自动缩放")!=null&&FindText(panel,"恢复默认字体")==null&&!Descendants(panel).Any(c=>c.AccessibleName=="界面字体大小"),context+" offers automatic typography without a separate font selector");panel.Navigate("home");Check(AdaptiveEditor(panel).Text==draft,context+" retains draft through page changes");
   }
   Check(largeHeading>smallHeading,"Rendered heading text actually enlarges when the window enlarges");
   panel.SetWindowClientSize(new Size(1280,820));var retained=Descendants(panel).ToArray();var retainedEditor=AdaptiveEditor(panel);retainedEditor.SelectionStart=2;retainedEditor.SelectionLength=3;IntPtr editorHandle=retainedEditor.Handle;int recreated=0,disposed=0;foreach(var c in retained){c.HandleCreated+=(s,e)=>recreated++;c.Disposed+=(s,e)=>disposed++;}
   var resizeHeading=FindText(panel,"今天，也一起好好做事。");var samples=new List<float>();var frameTimes=new List<double>();var frameWatch=System.Diagnostics.Stopwatch.StartNew();EventHandler sampleFrame=(s,e)=>{samples.Add(panel.InterfaceFontSize);frameTimes.Add(frameWatch.Elapsed.TotalMilliseconds);};resizeHeading.FontChanged+=sampleFrame;float startScale=panel.InterfaceFontSize;panel.ClientSize=new Size(760,560);var finish=DateTime.UtcNow.AddMilliseconds(450);while(DateTime.UtcNow<finish){Application.DoEvents();System.Threading.Thread.Sleep(10);}
   resizeHeading.FontChanged-=sampleFrame;float endScale=panel.InterfaceFontSize;File.WriteAllText(Path.Combine(folder,"resize-transition.txt"),"RESIZE samples start="+startScale+" end="+endScale+" values="+string.Join(",",samples.Select(f=>f.ToString("0.000")))+" times-ms="+string.Join(",",frameTimes.Select(t=>t.ToString("0"))));Check(samples.Any(f=>f<startScale-.01f&&f>endScale+.01f)&&samples.Zip(samples.Skip(1),(a,b)=>b<=a+.0001f).All(ok=>ok),"Native resize follows a smooth monotonic transition with intermediate sizes");
   Check(retained.SequenceEqual(Descendants(panel))&&recreated==0&&disposed==0&&retainedEditor.Handle==editorHandle,"Resizing preserves every home control and native editor handle without recreating or disposing controls");
   Check(retainedEditor.Text==draft&&retainedEditor.SelectionStart==2&&retainedEditor.SelectionLength==3,"Smooth resize preserves the draft and selected text");
   var formFlags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
   var sizing=typeof(Form).GetMethod("OnResizeBegin",formFlags);var released=typeof(Form).GetMethod("OnResizeEnd",formFlags);
   var resizeDpi=(float)typeof(Dashboard).GetField("dpiScale",formFlags).GetValue(panel);
   var queued=typeof(Dashboard).GetField("liveResizeQueued",formFlags);
   var liveScale=new List<string>();bool caughtUp=true,idleAfterFrame=false;
   sizing.Invoke(panel,new object[]{EventArgs.Empty});
   try{
    for(int i=1;i<=12;i++){
     panel.ClientSize=new Size(758+43*i,558+21*i);
     panel.ClientSize=new Size(760+43*i,560+21*i);
     var expected=10*Dashboard.WindowFontScale(panel.ClientSize,resizeDpi);
     Application.DoEvents();
     caughtUp&=Math.Abs(panel.InterfaceFontSize-expected)<.001f&&!(bool)queued.GetValue(panel);
     liveScale.Add(panel.InterfaceFontSize.ToString("0.000"));
    }
    idleAfterFrame=!((System.Windows.Forms.Timer)typeof(Dashboard).GetField("resizeTimer",formFlags).GetValue(panel)).Enabled;
   }finally{released.Invoke(panel,new object[]{EventArgs.Empty});}
   File.AppendAllText(Path.Combine(folder,"resize-transition.txt"),Environment.NewLine+"LIVE-DRAG latest size samples: "+string.Join(",",liveScale));
   Check(caughtUp&&idleAfterFrame&&retainedEditor.Handle==editorHandle&&retainedEditor.Text==draft,"Live resize matches the newest size in one layout frame, idles between inputs, and retains the editor");
   panel.SetWindowClientSize(new Size(760,560));
   var stableBounds=new Dictionary<Control,Rectangle>();foreach(var c in retained)stableBounds.Add(c,c.Bounds);for(int i=0;i<12;i++){panel.SetWindowClientSize(new Size(1280,820));panel.SetWindowClientSize(new Size(760,560));}Check(retained.All(c=>c.Bounds==stableBounds[c])&&disposed==0,"Repeated resize cycles return to identical geometry without cumulative scaling drift or control replacement");
   panel.SetWindowClientSize(new Size(1280,820));panel.Location=new Point(-3000,-3000);float before=FindText(panel,"今天，也一起好好做事。").Font.SizeInPoints;panel.ClientSize=new Size(760,560);var until=DateTime.UtcNow.AddMilliseconds(350);while(DateTime.UtcNow<until){Application.DoEvents();System.Threading.Thread.Sleep(10);}Check(FindText(panel,"今天，也一起好好做事。").Font.SizeInPoints<before&&AdaptiveEditor(panel).Text==draft,"Native resize events automatically rescale text and preserve draft without a manual font action");
   foreach(var requested in new[]{new Size(760,560),new Size(1280,820)}){panel.SetWindowClientSize(requested);panel.Location=new Point(-3000,-3000);foreach(string page in new[]{"tasks","settings","native","games","game-osu","game-cs2","positions","appearance","tools","schedules","plans"}){panel.Navigate(page);Application.DoEvents();string conflict=AdaptiveOverlap(panel);Check(conflict=="","Automatic page "+page+" "+panel.ClientSize+" has no overlaps"+(conflict==""?"":": "+conflict));foreach(var action in Descendants(panel).OfType<SoftButton>().Where(c=>c.Visible&&c.Enabled).ToArray())Check(AdaptiveReachable(action),page+" remains operable after automatic font scaling: "+action.Text);}}
   panel.SetWindowClientSize(new Size(1280,820));panel.Location=new Point(-3000,-3000);panel.Navigate("schedules");var list=Descendants(panel).OfType<FlowLayoutPanel>().Single();int rowWidth=list.Controls[0].Width;float rowFont=list.Controls[0].Controls.OfType<SurfaceLabel>().First().Font.SizeInPoints;controller.ScheduleVersion++;panel.UpdateLive();Check(list.Controls[0].Width==rowWidth&&Math.Abs(list.Controls[0].Controls.OfType<SurfaceLabel>().First().Font.SizeInPoints-rowFont)<.01,"Live schedule refresh keeps the current automatically scaled row geometry and text");
   var refreshedRow=list.Controls[0];var refreshedChildren=Descendants(refreshedRow).ToArray();panel.SetWindowClientSize(new Size(760,560));panel.SetWindowClientSize(new Size(1280,820));Check(ReferenceEquals(refreshedRow,list.Controls[0])&&refreshedChildren.SequenceEqual(Descendants(refreshedRow))&&refreshedRow.Width==rowWidth&&Math.Abs(refreshedRow.Controls.OfType<SurfaceLabel>().First().Font.SizeInPoints-rowFont)<.01,"Dynamically refreshed schedule rows keep their controls and logical scale through resize cycles");
   panel.Navigate("game-osu");var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(Dashboard).GetMethod("BeginBinding",flags).Invoke(panel,null);typeof(Dashboard).GetMethod("CaptureGameKey",flags).Invoke(panel,new object[]{Keys.A});panel.SetWindowClientSize(new Size(1100,760));var captured=(int[])typeof(Dashboard).GetField("captureKeys",flags).GetValue(panel);Check((bool)typeof(Dashboard).GetField("binding",flags).GetValue(panel)&&captured[0]==(int)Keys.A&&(int)typeof(Dashboard).GetField("captureIndex",flags).GetValue(panel)==1,"Window and font rescaling preserve an incomplete key-binding sequence");
   panel.Navigate("appearance");AdaptiveSnapshot(panel,folder,"ui-window-auto-settings");panel.Navigate("home");Check(AdaptiveEditor(panel).Text==draft,"Appearance page keeps the unsent chat draft");var styleAction=FindText(panel,originalStyle=="anime"?"像素风":"二次元风").Parent.Controls.OfType<SoftButton>().Single();AdaptiveClick(styleAction);Check(controller.Settings.Style!=originalStyle&&AdaptiveEditor(panel).Text==draft,"Style switching remains functional after automatic layout changes");AdaptiveEditor(panel).Text="你好";AdaptiveClick(FindText(panel,"发送"));Check(AdaptiveEditor(panel).Text==""&&controller.BubbleTitle=="GPT 回复","Chat sends normally after automatic font scaling");panel.Navigate("appearance");panel.Navigate("home");Check(AdaptiveEditor(panel).Text=="","Sent input does not return after layout rebuilding");
   TestSchedulePages(controller,panel,folder);
  }finally{controller.Settings.Style=originalStyle;panel.SetWindowClientSize(originalSize);panel.Location=new Point(-3000,-3000);}
 }
}
}
