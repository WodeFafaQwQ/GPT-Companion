using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace DragonCompanion {
public static partial class SelfTests {
 static Bitmap Cs2Filtered(Bitmap source,int width,int height){return (Bitmap)typeof(Cs2Art).GetMethod("AreaDownsample",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{source,width,height});}
 static void TestCs2Sampling(string folder){
  using(var source=new Bitmap(8,8,PixelFormat.Format32bppArgb)){for(int y=0;y<8;y++)for(int x=0;x<8;x++)source.SetPixel(x,y,(x+y)%2==0?Color.White:Color.Black);using(var filtered=Cs2Filtered(source,1,1)){var pixel=filtered.GetPixel(0,0);Check(pixel.A==255&&Math.Abs(pixel.R-128)<=1&&pixel.R==pixel.G&&pixel.G==pixel.B,"CS2 texture minification integrates every checkerboard texel instead of aliasing to one sample");}}
  using(var source=new Bitmap(3,1,PixelFormat.Format32bppArgb)){source.SetPixel(0,0,Color.White);source.SetPixel(1,0,Color.Black);source.SetPixel(2,0,Color.Black);using(var filtered=Cs2Filtered(source,2,1))Check(Math.Abs(filtered.GetPixel(0,0).R-170)<=1&&filtered.GetPixel(1,0).R==0,"CS2 area filtering weights fractional source-pixel footprints correctly");}
  using(var source=new Bitmap(4,1,PixelFormat.Format32bppArgb)){source.SetPixel(0,0,Color.White);for(int x=1;x<4;x++)source.SetPixel(x,0,Color.FromArgb(0,255,0,255));using(var filtered=Cs2Filtered(source,1,1)){var pixel=filtered.GetPixel(0,0);Check(Math.Abs(pixel.A-64)<=1&&pixel.R>=252&&pixel.G>=252&&pixel.B>=252,"CS2 transparent edges retain white colour at quarter coverage without dark or purple halos");}}
  foreach(int size in new[]{128,180,320})foreach(bool moving in new[]{false,true}){var input=new Cs2InputState();PointF hand=PointF.Empty;if(moving){input.Yaw=.65f;input.MouseX=4.35f;input.MouseY=-1.25f;input.MouseLeft=true;input.Key((int)System.Windows.Forms.Keys.W,true);hand=new PointF(-13.2f,-4.3f);}using(var scene=new Bitmap(380,500,PixelFormat.Format32bppArgb)){using(var g=Graphics.FromImage(scene))Cs2Art.Draw(g,380,0,size,"pixel",input,hand);var bounds=Cs2Art.Bounds(380,0,size);var crop=Rectangle.FromLTRB((int)Math.Floor(bounds.Left)-24,(int)Math.Floor(bounds.Top)-24,(int)Math.Ceiling(bounds.Right)+24,(int)Math.Ceiling(bounds.Bottom)+24);using(var actual=scene.Clone(crop,PixelFormat.Format32bppArgb)){int coverage=0,partial=0;bool clear=true;for(int y=0;y<actual.Height;y++)for(int x=0;x<actual.Width;x++){var pixel=actual.GetPixel(x,y);if(pixel.A>=128)coverage++;if(pixel.A>0&&pixel.A<255)partial++;if((x==0||y==0||x==actual.Width-1||y==actual.Height-1)&&pixel.A!=0)clear=false;}Check(coverage>size*size/10&&partial>100&&clear,"Half-size pixel CS2 has antialiased coverage and unclipped transparent margins: "+size+" / "+(moving?"motion":"idle"));actual.Save(Path.Combine(folder,"cs2-pixel-smooth-"+size+"-"+(moving?"motion":"idle")+".png"),ImageFormat.Png);}}}
 }
}
}
