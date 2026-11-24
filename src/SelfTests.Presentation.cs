using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace DragonCompanion {
public static partial class SelfTests {
 sealed class GameFixture:IGameProfile {
  public string Id {get;set;}public string Name {get{return Id;}}public string IconPath {get{return "";}}public bool Enabled {get;set;}public bool Running {get;set;}public bool Foreground {get;set;}public Rectangle ScreenBounds {get{return Rectangle.Empty;}}public string Corner {get;set;}public bool FixedPosition {get;set;}public Point SavedPosition {get;set;}public void Poll(){}
 }
 static void TestUpdatedGamePresentation(string folder){
  var osu=new GameFixture{Id="osu-mania",Enabled=true};var cs2=new GameFixture{Id="cs2",Enabled=true,Running=true};var games=new IGameProfile[]{osu,cs2};
  Check(GameCenter.Resolve(games,"osu-mania")==cs2,"A remembered stopped osu!mania choice no longer blocks automatic CS2 entry");osu.Running=true;cs2.Foreground=true;Check(GameCenter.Resolve(games,"osu-mania")==cs2,"Foreground CS2 takes priority when both games are running");cs2.Enabled=false;Check(GameCenter.Resolve(games,"auto")==osu&&GameCenter.Resolve(games,"off")==null,"Disabled game profiles and an explicit global pause remain respected");
  var legacy=Json.Serializer().Deserialize<Settings>("{\"GameModeSelection\":\"osu-mania\"}");Check(legacy.Cs2Enabled,"Legacy settings without CS2 fields retain automatic detection enabled by default");
  Check(Math.Abs(Cs2Art.Scale(128)-.4f)<.001f&&Math.Abs(Cs2Art.Scale(320)-.52f)<.001f,"All CS2 scene scales are exactly half their previous size");
  var input=new Cs2InputState();Check(Cs2Art.MouseBounds(input).Right<Cs2Art.KeyboardBounds.Left,"Mouse stays on the original left and keyboard on the original right");
  var space=Cs2Art.KeycapBox(Cs2Art.Keys.Single(x=>x.Key==(int)System.Windows.Forms.Keys.Space));var escape=Cs2Art.KeycapBox(Cs2Art.Keys.Single(x=>x.Key==(int)System.Windows.Forms.Keys.Escape));Check(space.Bottom<escape.Top,"The keyboard faces GPT with Space nearest her and function keys at the far edge");
  var w=Cs2Art.KeycapBox(Cs2Art.Keys.Single(x=>x.Key==(int)System.Windows.Forms.Keys.W));var t=Cs2Art.KeycapBox(Cs2Art.Keys.Single(x=>x.Key==(int)System.Windows.Forms.Keys.T));Check(w.Left>t.Left,"Keyboard columns turn with the device instead of swapping its side");
  Check(Cs2Art.MouseBounds(input).Top<231&&Cs2Art.MouseBounds(input).Bottom-17>231,"Mouse has a plain rear shell under the palm and wheel/buttons on the fingertip side");var rest=Cs2Art.MouseBounds(input);input.MouseX=4;input.MouseY=-2;input.MouseLeft=true;var moved=Cs2Art.MouseBounds(input);var hand=Cs2Art.MouseHand(input);Check(Math.Abs(moved.X-rest.X-hand.X)<.001f&&Math.Abs(moved.Y-rest.Y-hand.Y)<.001f,"Mouse translation and click depression stay rigidly attached to the hand");
  foreach(int size in new[]{128,192,256,320}){var scene=Cs2Art.Bounds(380,246,size);var area=new Rectangle(0,0,1920,1080);var clamped=LayeredPet.ClampScene(new Point(-2000,-2000),area,scene);Check(scene.Left+clamped.X>=5&&scene.Top+clamped.Y>=5&&scene.Right+clamped.X<1920&&scene.Bottom+clamped.Y<1080,"Half-size CS2 positioning retains valid screen bounds: "+size);}
  using(var source=new Bitmap(128,128)){using(var g=Graphics.FromImage(source)){g.Clear(Color.Orange);g.FillRectangle(Brushes.MidnightBlue,64,0,64,128);g.FillEllipse(Brushes.White,33,30,60,60);}using(var icon=LineIcons.OutlineGame(source,128)){icon.Save(Path.Combine(folder,"icon-shape-regression.png"),ImageFormat.Png);Check(icon.GetPixel(0,0).A==0&&icon.GetPixel(64,0).A>0&&icon.GetPixel(0,64).A>0,"Square game icons keep their original footprint and remove only rounded corners");var leftEdge=icon.GetPixel(1,64);var rightEdge=icon.GetPixel(126,64);Check(leftEdge.G<180&&leftEdge.B>160&&rightEdge.G<180&&rightEdge.B>160&&icon.GetPixel(64,1).G<180&&icon.GetPixel(64,126).G<180,"Square icons retain one outer purple rounded frame with transparent clipped corners");var centerSeam=icon.GetPixel(64,20);Check(centerSeam.R>240&&centerSeam.G>240&&centerSeam.B>240,"Colour-panel seams do not create a spurious inner CS2 square or divider");bool smooth=false;for(int y=30;y<90&&!smooth;y++)for(int x=30;x<100;x++){var color=icon.GetPixel(x,y);if(color.A>240&&color.G>Theme.Purple.G+10&&color.G<235){smooth=true;break;}}Check(smooth,"Vector-traced game silhouettes include smooth antialiased coverage");}}
  var cs2Path=new Cs2Profile(new Settings()).IconPath;if(File.Exists(cs2Path))using(var original=LineIcons.OriginalGameIcon(cs2Path)){using(var icon=LineIcons.OutlineGame(original,256))icon.Save(Path.Combine(folder,"icon-cs2-smooth-256.png"),ImageFormat.Png);using(var icon=LineIcons.OutlineGame(original,64))icon.Save(Path.Combine(folder,"icon-cs2-smooth-64.png"),ImageFormat.Png);}
 }
}
}
