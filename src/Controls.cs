using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DragonCompanion {
// No native BUTTON/checkbox painting or simulated transparent parent replay.
public class SurfaceControl:Control {
 public SurfaceControl(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);BackColor=Color.White;}
 protected override void OnPaintBackground(PaintEventArgs e){Theme.Background(this,e.Graphics);}
 protected override void OnTextChanged(EventArgs e){base.OnTextChanged(e);Invalidate();}
 protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);Invalidate();}
 protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
 protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
}
public sealed class SurfaceLabel:SurfaceControl {
 public ContentAlignment TextAlign=ContentAlignment.TopLeft;
 public SurfaceLabel(){TabStop=false;AccessibleRole=AccessibleRole.StaticText;}
 protected override void OnPaint(PaintEventArgs e){using(var brush=new SolidBrush(ForeColor))using(var format=new StringFormat{Trimming=StringTrimming.EllipsisCharacter,Alignment=TextAlign==ContentAlignment.MiddleCenter?StringAlignment.Center:StringAlignment.Near,LineAlignment=TextAlign==ContentAlignment.MiddleCenter?StringAlignment.Center:StringAlignment.Near})e.Graphics.DrawString(Text,Font,brush,ClientRectangle,format);}
}
public class ActionControl:SurfaceControl {
 public ActionControl(){SetStyle(ControlStyles.Selectable|ControlStyles.StandardClick,true);TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.PushButton;}
 protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left)Focus();base.OnMouseDown(e);}
 protected override bool IsInputKey(Keys keyData){return keyData==Keys.Enter||keyData==Keys.Space||base.IsInputKey(keyData);}
 protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){e.SuppressKeyPress=true;if(Enabled)OnClick(EventArgs.Empty);}else base.OnKeyDown(e);}
}
public class RoundedPanel:Panel {
 Color fill=Color.White;
 public Color FillColor {get{return fill;}set{if(fill==value)return;fill=value;Invalidate();}}
 public RoundedPanel(){SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);BackColor=Color.White;}
 protected override void OnPaintBackground(PaintEventArgs e){Theme.Background(this,e.Graphics);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(1,1,Width-3,Height-3);Theme.Box(e.Graphics,r,fill,14);using(var p=Theme.Round(r,14))using(var pen=new Pen(Color.FromArgb(216,202,239)))e.Graphics.DrawPath(pen,p);}
}
public sealed class CompanionMenu:ContextMenuStrip {
 public CompanionMenu(bool checks=false){AutoSize=false;ShowImageMargin=false;ShowCheckMargin=checks;Font=Theme.Font(10);Renderer=new ChoiceMenu.ChoiceRenderer();Padding=new Padding(6);}
 public void PrepareLayout(){int width=222,height=4;foreach(ToolStripItem item in Items)if(item.Available)width=Math.Max(width,TextRenderer.MeasureText(item.Text,Font).Width+48);SuspendLayout();foreach(ToolStripItem item in Items){item.AutoSize=false;item.Margin=Padding.Empty;item.Size=new Size(width-12,item is ToolStripSeparator?9:32);if(item.Available)height+=item.Height;}Size=new Size(width,height);ResumeLayout(true);PerformLayout();}
 protected override void OnOpening(System.ComponentModel.CancelEventArgs e){PrepareLayout();base.OnOpening(e);}
 protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<3||Height<3)return;var old=Region;using(var path=Theme.Round(new RectangleF(0,0,Width,Height),12))Region=new Region(path);if(old!=null)old.Dispose();}
}
public sealed class ChoiceMenu:ContextMenuStrip {
 public ChoiceMenu(int width,int count){AutoSize=false;Padding=new Padding(6);ShowImageMargin=false;ShowCheckMargin=true;Font=Theme.Font(10);Renderer=new ChoiceRenderer();Size=new Size(width,count*34+12);using(var path=Theme.Round(new RectangleF(0,0,Width,Height),12))Region=new Region(path);}
 protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<3||Height<3)return;var old=Region;using(var path=Theme.Round(new RectangleF(0,0,Width,Height),12))Region=new Region(path);if(old!=null)old.Dispose();}
 internal sealed class ChoiceRenderer:ToolStripProfessionalRenderer {
  protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){e.Graphics.Clear(Color.FromArgb(253,250,255));}
  protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){}
  protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Theme.Round(new RectangleF(1,1,e.ToolStrip.Width-3,e.ToolStrip.Height-3),11))using(var pen=new Pen(Color.FromArgb(182,159,230),1.5f))e.Graphics.DrawPath(pen,path);}
  protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){if(e.Item.Selected){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;bool companion=e.ToolStrip is CompanionMenu;Theme.Box(e.Graphics,new RectangleF(companion?6:0,2,e.Item.Width-(companion?7:1),e.Item.Height-4),Color.FromArgb(237,227,252),8);}}
  protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){if(e.ToolStrip is CompanionMenu){int inset=((CompanionMenu)e.ToolStrip).ShowCheckMargin?34:14;Theme.ControlText(e.Graphics,e.Text,e.TextFont,new Rectangle(inset,0,e.Item.Width-inset-22,e.Item.Height),e.Item.Enabled?Theme.Ink:Theme.Muted,false);}else{e.TextColor=e.Item.Enabled?Theme.Ink:Theme.Muted;base.OnRenderItemText(e);}}
  protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e){e.ArrowColor=Theme.Purple;base.OnRenderArrow(e);}
  protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e){using(var pen=new Pen(Color.FromArgb(225,213,242)))e.Graphics.DrawLine(pen,10,e.Item.Height/2,e.Item.Width-10,e.Item.Height/2);}
  protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float x=e.ImageRectangle.X+2,y=e.ImageRectangle.Y+2;using(var pen=new Pen(Theme.Purple,1.8f))e.Graphics.DrawLines(pen,new[]{new PointF(x,y+6),new PointF(x+4,y+10),new PointF(x+11,y+2)});}
 }
}
public sealed class SoftSelect:ActionControl {
 public readonly StringCollection Items=new StringCollection();
 int index=-1;ContextMenuStrip popup;
 public event EventHandler SelectedIndexChanged;
 public int SelectedIndex {get{return index;}set{int next=value>=0&&value<Items.Count?value:-1;if(index==next){Invalidate();return;}index=next;AccessibleDescription=SelectedItem??"暂无可选项";Invalidate();if(SelectedIndexChanged!=null)SelectedIndexChanged(this,EventArgs.Empty);}}
 public string SelectedItem {get{return index>=0&&index<Items.Count?Items[index]:null;}}
 public SoftSelect(){Height=36;Font=Theme.Font(10);AccessibleRole=AccessibleRole.ComboBox;}
 protected override void OnPaintBackground(PaintEventArgs e){Theme.Background(this,e.Graphics);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(1,1,Width-3,Height-3);Theme.Box(e.Graphics,r,Color.White,11);using(var p=Theme.Round(r,11))using(var pen=new Pen(Focused?Theme.Purple:Color.FromArgb(182,159,230),Focused?2:1.5f))e.Graphics.DrawPath(pen,p);Theme.ControlText(e.Graphics,SelectedItem??"暂无可选项",Font,new Rectangle(12,1,Width-42,Height-2),SelectedItem==null?Theme.Muted:Theme.Ink,false);using(var pen=new Pen(Theme.Purple,1.8f))e.Graphics.DrawLines(pen,new[]{new Point(Width-25,Height/2-2),new Point(Width-20,Height/2+3),new Point(Width-15,Height/2-2)});}
 protected override void OnClick(EventArgs e){base.OnClick(e);OpenChoices();}
 void OpenChoices(){if(Items.Count==0)return;if(popup!=null)popup.Dispose();int rowHeight=Math.Max(34,Font.Height+14);popup=new ChoiceMenu(Width,Items.Count){Font=Font,Height=Items.Count*rowHeight+12};for(int i=0;i<Items.Count;i++){int choice=i;var item=new ToolStripMenuItem(Items[i]){Checked=i==index,AutoSize=false,Size=new Size(Width-12,rowHeight)};item.Click+=(s,e)=>SelectedIndex=choice;popup.Items.Add(item);}popup.Show(this,new Point(0,Height+3));}
 protected override bool IsInputKey(Keys keyData){return keyData==Keys.Up||keyData==Keys.Down||base.IsInputKey(keyData);}
 protected override void OnKeyDown(KeyEventArgs e){if(e.Alt&&e.KeyCode==Keys.Down||e.KeyCode==Keys.F4){OpenChoices();e.Handled=true;}else if(e.KeyCode==Keys.Down||e.KeyCode==Keys.Up){if(Items.Count>0)SelectedIndex=Math.Max(0,Math.Min(Items.Count-1,index+(e.KeyCode==Keys.Down?1:-1)));e.Handled=true;}else base.OnKeyDown(e);}
 protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
 protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
 protected override void Dispose(bool disposing){if(disposing&&popup!=null)popup.Dispose();base.Dispose(disposing);}
}
public sealed class SoftNumber:RoundedPanel {
 readonly TextBox editor;readonly SoftButton minus,plus;decimal current;bool updating;
 bool compact;public bool Compact{get{return compact;}set{compact=value;Arrange();}}
 public decimal Minimum=0,Maximum=100;
 public event EventHandler ValueChanged;
 public decimal Value {get{return current;}set{SetValue(value,true);}}
 public SoftNumber(){Size=new Size(132,38);editor=new TextBox{BorderStyle=BorderStyle.None,BackColor=Color.White,ForeColor=Theme.Ink,Font=Theme.Font(11),TextAlign=HorizontalAlignment.Center,Text="0",AccessibleName="数值"};minus=new SoftButton("−"){Size=new Size(28,28),AccessibleName="减少"};plus=new SoftButton("+"){Size=new Size(28,28),AccessibleName="增加"};Controls.Add(editor);Controls.Add(minus);Controls.Add(plus);minus.Click+=(s,e)=>Value=current-1;plus.Click+=(s,e)=>Value=current+1;editor.TextChanged+=(s,e)=>{decimal parsed;if(!updating&&decimal.TryParse(editor.Text,out parsed)&&parsed==decimal.Truncate(parsed)&&parsed>=Minimum&&parsed<=Maximum)SetValue(parsed,false);};editor.Leave+=(s,e)=>Commit();editor.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Up||e.KeyCode==Keys.Down){Value=current+(e.KeyCode==Keys.Up?1:-1);e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Enter){Commit();e.SuppressKeyPress=true;}};Resize+=(s,e)=>Arrange();Arrange();}
 void Arrange(){if(editor==null)return;float scale=Height/38f;int inset=Math.Max(3,(int)Math.Round(5*scale)),side=Math.Max(18,(int)Math.Round(28*scale)),start=inset+side+Math.Max(2,(int)Math.Round(2*scale));minus.Visible=plus.Visible=!compact;if(compact){int margin=Math.Max(4,(int)Math.Round(8*scale));editor.SetBounds(margin,(Height-editor.PreferredHeight)/2,Math.Max(20,Width-margin*2),editor.PreferredHeight);return;}minus.Size=plus.Size=new Size(side,side);minus.Location=new Point(inset,(Height-side)/2);plus.Location=new Point(Width-inset-side,(Height-side)/2);editor.SetBounds(start,(Height-editor.PreferredHeight)/2,Math.Max(20,Width-start*2),editor.PreferredHeight);}
 protected override void OnFontChanged(EventArgs e){base.OnFontChanged(e);Arrange();}
 protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);Arrange();}
 void Commit(){decimal parsed;Value=decimal.TryParse(editor.Text,out parsed)?parsed:current;}
 void SetValue(decimal value,bool display){value=Math.Max(Minimum,Math.Min(Maximum,decimal.Truncate(value)));bool changed=current!=value;current=value;if(display){updating=true;editor.Text=current.ToString("0");updating=false;}minus.Enabled=current>Minimum;plus.Enabled=current<Maximum;if(changed&&ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}
}
public sealed class SoftCheck:ActionControl {
 bool isChecked;
 public event EventHandler CheckedChanged;
 public bool Checked{get{return isChecked;}set{if(isChecked==value)return;isChecked=value;Invalidate();if(CheckedChanged!=null)CheckedChanged(this,EventArgs.Empty);}}
 public SoftCheck(){AccessibleRole=AccessibleRole.CheckButton;}
 protected override void OnClick(EventArgs e){Checked=!Checked;base.OnClick(e);}
 protected override void OnPaintBackground(PaintEventArgs e){Theme.Background(this,e.Graphics);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(1,(Height-18)/2,18,18);Theme.Box(e.Graphics,r,Checked?Theme.Purple:Color.White,5);using(var p=Theme.Round(r,5))using(var pen=new Pen(Theme.Purple,1.3f))e.Graphics.DrawPath(pen,p);if(Checked)using(var pen=new Pen(Color.White,2))e.Graphics.DrawLines(pen,new[]{new PointF(r.X+4,r.Y+9),new PointF(r.X+8,r.Y+13),new PointF(r.X+14,r.Y+5)});Theme.ControlText(e.Graphics,Text,Font,new Rectangle(29,0,Width-29,Height),ForeColor,false);if(Focused)Theme.Focus(e.Graphics,new Rectangle(26,2,Width-28,Height-4));}
}
}
