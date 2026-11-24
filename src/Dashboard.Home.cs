using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DragonCompanion {
public sealed partial class Dashboard {
 readonly Dictionary<string,Control> homeNodes=new Dictionary<string,Control>();
 readonly Dictionary<int,int[]> homeTextHeights=new Dictionary<int,int[]>();
 bool homeTwoColumns;
 T HomeNode<T>(string key,Control parent,Func<T> create,int x,int y,int width,int height,float fontSize=0) where T:Control {Control node;if(!homeNodes.TryGetValue(key,out node)){node=create();parent.Controls.Add(node);homeNodes.Add(key,node);}var bounds=new Rectangle(x,y,width,height);DesignControl item;if(design.TryGetValue(node,out item)){item.Bounds=bounds;if(fontSize>0)item.FontSize=fontSize;}else node.Bounds=bounds;return (T)node;}
 SurfaceLabel HomeLabel(string key,Control parent,string text,int x,int y,int w,int h,float size,Color color,bool bold=false){return HomeNode(key,parent,()=>new SurfaceLabel{Text=text,Font=Theme.Font(size,bold),ForeColor=color,BackColor=Color.FromArgb(251,250,254)},x,y,w,h,size);}
 SoftButton HomeButton(string key,Control parent,string text,int x,int y,int w,Action action,bool primary=false){return HomeNode(key,parent,()=>{var b=new SoftButton(text,primary);b.Click+=(s,e)=>action();return b;},x,y,w,38);}
 RoundedPanel HomeCard(string key,Control parent,int x,int y,int w,int h){return HomeNode(key,parent,()=>new RoundedPanel(),x,y,w,h);}
 static int LogicalTextHeight(string text,float size,int width,bool bold=false){using(var bitmap=new Bitmap(1,1)){bitmap.SetResolution(96,96);using(var g=Graphics.FromImage(bitmap))using(var font=Theme.Font(size,bold))return (int)Math.Ceiling(g.MeasureString(text,font,Math.Max(60,width)).Height)+6;}}
 const string HomeTitle="今天，也一起好好做事。", HomeIntroduction="你的桌面伙伴已就位。提醒、陪伴，还有一点点龙的小脾气。", HomeHint="本地互动口令：你好 / 谢谢 / 额度 / 番茄钟 / 日程 / 休息";
 bool UseHomeColumns(int width){return width>=620&&ClientSize.Width/dpiScale>=850;}
 bool UpdateHomeGeometry(){int width=AvailableHomeWidth();if(width==homeLogicalWidth&&UseHomeColumns(width)==homeTwoColumns)return false;BuildHome();return true;}
 int[] HomeTextHeights(int width){
  // Round down so a cached measurement always fits the current width.
  int measured=width-width%8;int[] heights;
  if(!homeTextHeights.TryGetValue(measured,out heights)){
   heights=new[]{Math.Max(52,LogicalTextHeight(HomeTitle,23,measured,true)),Math.Max(30,LogicalTextHeight(HomeIntroduction,10,measured-2)),Math.Max(24,LogicalTextHeight(HomeHint,8,measured))};
   homeTextHeights.Add(measured,heights);
  }
  return heights;
 }
 void BuildHome(){int w=homeLogicalWidth=AvailableHomeWidth();bool twoColumns=homeTwoColumns=UseHomeColumns(w);bool narrow=!twoColumns;var heights=HomeTextHeights(w);int titleHeight=heights[0];HomeLabel("title",content,HomeTitle,0,0,w,titleHeight,23,Theme.Ink,true);int subtitleHeight=heights[1];HomeLabel("intro",content,HomeIntroduction,1,titleHeight+6,w-2,subtitleHeight,10,Theme.Muted);int y=titleHeight+subtitleHeight+18;
  int heroHeight=narrow?228:204;var hero=HomeCard("hero",content,0,y,w,heroHeight);hero.FillColor=Color.FromArgb(244,240,253);int pictureWidth=narrow?(w<340?90:114):190,pictureHeight=narrow?150:184,textWidth=w-pictureWidth-52;HomeLabel("english",hero,"YOUR LITTLE COMPANION",22,20,textWidth,25,8,Theme.Purple,true);status=HomeLabel("status",hero,app.StatusLabel,22,54,textWidth,40,narrow?18:22,Theme.Ink,true);HomeLabel("instructions",hero,"单击摸摸头 · 同按左右键拖动\n双击打开这里 · 右键查看更多操作",24,102,textWidth-2,narrow?68:48,narrow?9:10,Theme.Muted);int buttonsY=heroHeight-51;HomeButton("pat",hero,"摸摸头",24,buttonsY,112,()=>app.Pat(),true);HomeButton("tools",hero,"实用工具",148,buttonsY,Math.Min(148,w-168),()=>Navigate("tools"));HomeNode("portrait",hero,()=>new ArtView{Style=app.Settings.Style},w-pictureWidth-12,10,pictureWidth,pictureHeight);
  y+=heroHeight+18;HomeLabel("styles",content,"选择我的样子",0,y,narrow?w:Math.Max(250,w-310),30,14,Theme.Ink,true);int selectorY=narrow?y+39:y;HomeLabel("dialogue-label",content,"对话风格",narrow?0:w-286,selectorY+5,82,24,9,Theme.Muted);HomeNode("dialogue",content,()=>{var dialogue=new SoftSelect{AccessibleName="对话风格"};dialogue.Items.AddRange(Dialogue.Names);dialogue.SelectedIndex=Math.Max(0,Array.IndexOf(Dialogue.Keys,app.Settings.DialogueStyle));dialogue.SelectedIndexChanged+=(s,e)=>{app.Settings.DialogueStyle=Dialogue.Keys[dialogue.SelectedIndex];app.Save();};return dialogue;},narrow?102:w-194,selectorY,narrow?w-102:194,38);y=selectorY+46;
  if(twoColumns){int cardWidth=(w-16)/2;StyleCard("pixel","像素风","复古像素 / 轻巧可爱",0,y,cardWidth);StyleCard("anime","二次元风","GPT / 细腻立绘",cardWidth+16,y,cardWidth);y+=208;}else{StyleCard("pixel","像素风","复古像素 / 轻巧可爱",0,y,w);StyleCard("anime","二次元风","GPT / 细腻立绘",0,y+214,w);y+=428;}
  var input=HomeNode("input",content,()=>{var c=new ChatInput{AccessibleName="主页聊天输入框"};var editor=c.Editor;editor.Text=homeDraft;editor.TextChanged+=(s,e)=>homeDraft=editor.Text;editor.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){app.Chat(editor.Text);editor.Clear();e.SuppressKeyPress=true;}};return c;},0,y,w-123,44);homeEditor=input.Editor;DesignControl editorDesign;if(design.TryGetValue(homeEditor,out editorDesign))editorDesign.Bounds=new Rectangle(14,11,w-151,editorDesign.Bounds.Height);HomeButton("send",content,"发送",w-110,y+3,110,()=>{app.Chat(homeEditor.Text);homeEditor.Clear();},true);int hintHeight=heights[2];HomeLabel("hint",content,HomeHint,0,y+53,w,hintHeight,8,Theme.Muted);homeLogicalHeight=Math.Max(660,y+hintHeight+60);
 }
 void StyleCard(string style,string title,string subtitle,int x,int y,int width){var card=HomeCard(style,content,x,y,width,198);int imageWidth=width<310?105:125,textX=imageWidth+21,textWidth=width-textX-12;HomeNode(style+"-art",card,()=>new ArtView{Style=style,Circle=false},11,10,imageWidth,178);HomeLabel(style+"-title",card,title,textX,24,textWidth,30,16,Theme.Ink,true);HomeLabel(style+"-subtitle",card,subtitle,textX,66,textWidth,46,9,Theme.Muted);HomeButton(style+"-choose",card,app.Settings.Style==style?"使用中  ✓":"切换风格",textX,137,Math.Max(80,textWidth-13),()=>app.SetStyle(style),app.Settings.Style==style);}
}
}
