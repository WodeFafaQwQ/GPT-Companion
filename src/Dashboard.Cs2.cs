using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DragonCompanion {
public sealed class Cs2SceneView:SurfaceControl {
 public string Style="anime";public Cs2InputState Input=new Cs2InputState();
 protected override void OnPaintBackground(PaintEventArgs e){Theme.Background(this,e.Graphics);}
 protected override void OnPaint(PaintEventArgs e){var saved=e.Graphics.Save();e.Graphics.ScaleTransform(.80f,.80f);Cs2Art.Draw(e.Graphics,350,20,224,Style,Input,Cs2Art.KeyboardHand(Input.LatestKey));e.Graphics.Restore(saved);}
}
public sealed partial class Dashboard {
 SurfaceLabel cs2Status,cs2Position,initialStatus;
 void BuildCs2(){Label(content,"Counter-Strike 2",0,0,490,52,23,Theme.Ink,true);Label(content,"正面 Q 版 GPT，实时回应你的键盘和鼠标操作。",1,58,640,36,10,Theme.Muted);Button(content,"返回游戏列表",508,7,150,()=>Navigate("games"));var profile=app.Games.Cs2;
  var detect=Card(content,0,102,658,225);Label(detect,"游戏检测",20,15,250,32,16,Theme.Ink,true);Check(detect,"检测到 CS2 时自动进入",316,19,profile.Enabled,v=>{profile.Enabled=v;if(v)app.Games.Selection="auto";app.RefreshGameMode();});cs2Status=Label(detect,"",20,55,612,28,10,Theme.Purple);Label(detect,profile.ExecutableLabel,20,90,612,38,9,Theme.Muted);
  Button(detect,"选择游戏程序…",20,142,190,()=>{using(var dialog=new OpenFileDialog{Title="选择 CS2 的 cs2.exe",Filter="Windows 程序 (*.exe)|*.exe",CheckFileExists=true,Multiselect=false,InitialDirectory=File.Exists(profile.EffectiveExePath)?Path.GetDirectoryName(profile.EffectiveExePath):""})if(dialog.ShowDialog(this)==DialogResult.OK){profile.ExePath=dialog.FileName;app.Save();app.RefreshGameMode();Rebuild();}},true);Button(detect,"使用自动识别",225,142,180,()=>{profile.ExePath="";app.Save();app.RefreshGameMode();Rebuild();});
  var position=Card(content,0,343,658,145);Label(position,"停靠位置",20,13,220,29,14,Theme.Ink,true);Button(position,"左下角",316,10,150,()=>{app.ResetGamePosition("left","cs2");Rebuild();},profile.Corner=="left");Button(position,"右下角",478,10,160,()=>{app.ResetGamePosition("right","cs2");Rebuild();},profile.Corner=="right");
  Button(position,"设置固定位置",20,58,190,()=>app.BeginGamePlacement("cs2"),true);Button(position,"恢复默认位置",225,58,180,()=>{app.ResetGamePosition(null,"cs2");UpdateLive();});Button(position,"取消位置设置",420,58,218,()=>{app.Pet.CancelPlacement();UpdateLive();});cs2Position=Label(position,"",20,106,612,27,9,Theme.Purple);
  var info=Card(content,0,504,658,121);Label(info,"手臂随操作联动",20,14,610,28,14,Theme.Ink,true);Label(info,"左手移动到键位并下按，右手随鼠标移动和点击；头部跟随鼠标 X 轴。\nWASD 使用紫色半透明键帽。切出游戏暂停反馈，关闭游戏恢复普通状态。",20,52,612,57,10,Theme.Muted);Label(content,"仅在 CS2 前台时读取即时输入，不记录按键内容。",1,635,650,23,9,Theme.Muted);
 }
 void BuildPositions(){Label(content,"桌宠位置",0,0,490,52,23,Theme.Ink,true);Label(content,"普通桌宠的初始位置与各个游戏的位置分别保存。",1,58,640,36,10,Theme.Muted);Button(content,"返回桌宠设置",508,7,150,()=>Navigate("settings"));var card=Card(content,0,105,658,275);Label(card,"自定义初始位置",24,22,610,37,18,Theme.Ink,true);Label(card,"点击下方按钮后，同时按住鼠标左右键，拖动紫框中的 GPT。\n松开任意一键保存位置，之后每次启动都会回到这里。",24,77,610,65,11,Theme.Muted);Button(card,"自定义初始位置",24,158,190,()=>{app.Pet.BeginInitialPlacement();UpdateLive();},true);Button(card,"恢复默认初始位置",228,158,204,()=>{app.Pet.ResetNormalPosition();app.RefreshGameMode();UpdateLive();});Button(card,"取消位置设置",446,158,188,()=>{app.Pet.CancelPlacement();UpdateLive();});initialStatus=Label(card,"",24,219,610,35,10,Theme.Purple);Label(content,"散步或手动拖动不会覆盖你设置的初始位置。\n游戏中的位置请在游戏中心分别设置。",2,407,650,80,11,Theme.Muted);}
 void UpdateExtraPages(){if(page=="game-cs2"&&cs2Status!=null&&!cs2Status.IsDisposed){var profile=app.Games.Cs2;cs2Status.Text=!profile.Enabled?"CS2 自动检测已关闭":app.Games.Selection=="off"?"游戏模式已退出，右键选择自动检测可恢复":!profile.Running?"等待 CS2 启动…":app.Pet.InputError!=""?app.Pet.InputError:profile.Foreground?"已检测到 CS2 · 键鼠反馈中":"已检测到 CS2 · 切回游戏后显示反馈";cs2Position.Text=app.Pet.PositioningGame&&app.Pet.GameId=="cs2"?"同时按住鼠标左右键拖动紫框内任意处，松开保存。":profile.FixedPosition?"固定位置：（"+profile.SavedPosition.X+", "+profile.SavedPosition.Y+"）":"默认："+(profile.Corner=="left"?"左下角":"右下角");}if(page=="positions"&&initialStatus!=null&&!initialStatus.IsDisposed)initialStatus.Text=app.Pet.Positioning&&!app.Pet.PositioningGame?"正在设置：同时按住左右键拖动，松开保存。":app.Pet.HasInitialPosition?"初始位置：（"+app.Settings.InitialX+", "+app.Settings.InitialY+"）":"当前使用默认初始位置。";}
}
}
