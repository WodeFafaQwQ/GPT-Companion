using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DragonCompanion {
public sealed class Cs2Keycap {
 public int Key;public RectangleF Box;
 public Cs2Keycap(Keys key,float x,float y,float width){Key=(int)key;Box=new RectangleF(x,y,width,10);}
 public bool Movement {get{return Key==(int)Keys.W||Key==(int)Keys.A||Key==(int)Keys.S||Key==(int)Keys.D;}}
}
public static class Cs2Art {
 static readonly Dictionary<string,Lazy<Bitmap>> sprites=CreateSprites();
 static Dictionary<string,Lazy<Bitmap>> CreateSprites(){var cache=new Dictionary<string,Lazy<Bitmap>>();foreach(string style in new[]{"pixel","anime"}){string name=style;cache.Add(name,new Lazy<Bitmap>(()=>ReadSprite(name),true));}return cache;}
 static Bitmap GetSprite(string style){Lazy<Bitmap> entry;return sprites.TryGetValue(style,out entry)?entry.Value:null;}
 public static int LoadedAssetCount {get{return sprites.Values.Count(x=>x.IsValueCreated);}}
 sealed class PoseCache {public string Key;public Bitmap Image;}
 static readonly Dictionary<string,PoseCache> poses=new Dictionary<string,PoseCache>();
 public static readonly Cs2Keycap[] Keys=MakeKeys();
 static Cs2Keycap[] MakeKeys(){var keys=new List<Cs2Keycap>();keys.Add(new Cs2Keycap(System.Windows.Forms.Keys.Escape,0,0,13));for(int i=0;i<5;i++)keys.Add(new Cs2Keycap((Keys)((int)System.Windows.Forms.Keys.F1+i),24+i*16,0,13));
  AddRow(keys,new[]{System.Windows.Forms.Keys.Oemtilde,System.Windows.Forms.Keys.D1,System.Windows.Forms.Keys.D2,System.Windows.Forms.Keys.D3,System.Windows.Forms.Keys.D4,System.Windows.Forms.Keys.D5,System.Windows.Forms.Keys.D6},14,13,13);
  AddRow(keys,new[]{System.Windows.Forms.Keys.Tab,System.Windows.Forms.Keys.Q,System.Windows.Forms.Keys.W,System.Windows.Forms.Keys.E,System.Windows.Forms.Keys.R,System.Windows.Forms.Keys.T},28,20,13);
  AddRow(keys,new[]{System.Windows.Forms.Keys.Capital,System.Windows.Forms.Keys.A,System.Windows.Forms.Keys.S,System.Windows.Forms.Keys.D,System.Windows.Forms.Keys.F,System.Windows.Forms.Keys.G},42,24,13);
  AddRow(keys,new[]{System.Windows.Forms.Keys.LShiftKey,System.Windows.Forms.Keys.Z,System.Windows.Forms.Keys.X,System.Windows.Forms.Keys.C,System.Windows.Forms.Keys.V,System.Windows.Forms.Keys.B},56,30,13);
  keys.Add(new Cs2Keycap(System.Windows.Forms.Keys.LControlKey,0,70,20));keys.Add(new Cs2Keycap(System.Windows.Forms.Keys.LWin,23,70,17));keys.Add(new Cs2Keycap(System.Windows.Forms.Keys.LMenu,43,70,18));keys.Add(new Cs2Keycap(System.Windows.Forms.Keys.Space,64,70,44));return keys.ToArray();
 }
 static void AddRow(List<Cs2Keycap> keys,Keys[] row,float y,float first,float normal){float x=0;for(int i=0;i<row.Length;i++){float width=i==0?first:normal;keys.Add(new Cs2Keycap(row[i],x,y,width));x+=width+3;}}
 public static void Load(){GetSprite("anime");GetSprite("pixel");}
 static Bitmap ReadSprite(string style){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(style+"-cs2.png")){if(stream==null)return null;using(var raw=new Bitmap(stream))using(var isolated=Art.IsolateCharacter(raw)){if(style=="pixel")return AreaDownsample(isolated,216,200);var result=new Bitmap(432,400,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(result)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(isolated,new Rectangle(0,0,result.Width,result.Height));}return result;}}}
 // At the half-size CS2 scale a display pixel covers many source texels.
 // Integrate its complete footprint instead of selecting one nearest sample.
 // Filter colour and coverage together so transparent edges cannot add dark halos.
 static Bitmap AreaDownsample(Bitmap source,int width,int height){int sourceWidth=source.Width,sourceHeight=source.Height;var data=source.LockBits(new Rectangle(0,0,sourceWidth,sourceHeight),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);var input=new byte[sourceWidth*sourceHeight*4];try{for(int y=0;y<sourceHeight;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),input,y*sourceWidth*4,sourceWidth*4);}finally{source.UnlockBits(data);}var output=new byte[width*height*4];double scaleX=sourceWidth/(double)width,scaleY=sourceHeight/(double)height,area=scaleX*scaleY;
  for(int y=0;y<height;y++){double top=y*scaleY,bottom=(y+1)*scaleY;for(int x=0;x<width;x++){double left=x*scaleX,right=(x+1)*scaleX,b=0,g=0,r=0,a=0;for(int sy=(int)top;sy<Math.Min(sourceHeight,(int)Math.Ceiling(bottom));sy++){double wy=Math.Min(sy+1,bottom)-Math.Max(sy,top);for(int sx=(int)left;sx<Math.Min(sourceWidth,(int)Math.Ceiling(right));sx++){double weight=wy*(Math.Min(sx+1,right)-Math.Max(sx,left));int offset=(sy*sourceWidth+sx)*4;b+=input[offset]*weight;g+=input[offset+1]*weight;r+=input[offset+2]*weight;a+=input[offset+3]*weight;}}int target=(y*width+x)*4;output[target]=(byte)(b/area+.5);output[target+1]=(byte)(g/area+.5);output[target+2]=(byte)(r/area+.5);output[target+3]=(byte)(a/area+.5);}}
  var result=new Bitmap(width,height,PixelFormat.Format32bppPArgb);var pixels=result.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);try{for(int y=0;y<height;y++)Marshal.Copy(output,y*width*4,IntPtr.Add(pixels.Scan0,y*pixels.Stride),width*4);}finally{result.UnlockBits(pixels);}return result;
 }
 public static bool Available(string style){return GetSprite(style)!=null;}
 public static float Scale(int size){return .5f*Math.Min(1.04f,Math.Max(.80f,size/180f));}
 public static RectangleF Bounds(int width,int petTop,int size){float scale=Scale(size);return new RectangleF((width-320*scale)/2,petTop+size-249*scale,320*scale,295*scale);}
 static float Smooth(float a,float b,float x){float f=Math.Max(0,Math.Min(1,(x-a)/(b-a)));return f*f*(3-2*f);}
 // Smooth weights anchor sleeves at the shoulders and move the continuous
 // forearm/wrist texture. No detached hand cutouts are composited over the body.
 public static PointF Deform(float u,float v,float yaw,PointF mouse,PointF keyboard){float head=1-Smooth(.55f,.72f,v);float face=(float)Math.Sin(u*Math.PI);float arm=Smooth(.69f,.95f,v);float left=(1-Smooth(.35f,.51f,u))*arm,right=Smooth(.54f,.73f,u)*arm;return new PointF(yaw*7*head*face+left*mouse.X+right*keyboard.X,head*yaw*(u-.5f)*2+left*mouse.Y+right*keyboard.Y);}
 public static PointF KeyboardHand(int key){Cs2Keycap cap=Array.Find(Keys,k=>k.Key==key);if(cap==null)return PointF.Empty;var r=KeycapBox(cap);return new PointF(Math.Max(-26,Math.Min(24,(r.X+r.Width/2-254)*.65f)),Math.Max(-12,Math.Min(16,(r.Y+r.Height/2-236)*.65f))+2);}
 // The keyboard faces GPT: Space is nearest her, the function row farthest.
 // Rotate the device including its labels; do not swap its side of the scene.
 static PointF KeyboardPoint(float x,float y){float u=1-x/109f,v=1-y/80f;return new PointF(186-16*v+(110+32*v)*u,210+62*v);}
 public static RectangleF KeycapBox(Cs2Keycap cap){var box=cap.Box;var points=new[]{KeyboardPoint(box.Left,box.Top),KeyboardPoint(box.Right,box.Top),KeyboardPoint(box.Right,box.Bottom),KeyboardPoint(box.Left,box.Bottom)};float x=points.Min(p=>p.X),y=points.Min(p=>p.Y);return new RectangleF(x,y,points.Max(p=>p.X)-x,points.Max(p=>p.Y)-y);}
 public static RectangleF KeyboardBounds {get{return new RectangleF(166,207,150,72);}}
 public static PointF MouseHand(Cs2InputState input){return new PointF(input.MouseX,input.MouseY+(input.MouseLeft||input.MouseRight?2:0));}
 public static RectangleF MouseBounds(Cs2InputState input){var hand=MouseHand(input);return new RectangleF(68+hand.X,216+hand.Y,32,44);}
 static void DrawSprite(Graphics g,Bitmap sprite,string style,Cs2InputState input,PointF keyboard){var rect=new RectangleF(24,0,270,249);var mouse=MouseHand(input);g.InterpolationMode=style=="pixel"?InterpolationMode.HighQualityBicubic:InterpolationMode.HighQualityBilinear;g.PixelOffsetMode=PixelOffsetMode.Half;
  if(Math.Abs(input.Yaw)<.001f&&Math.Abs(mouse.X)+Math.Abs(mouse.Y)+Math.Abs(keyboard.X)+Math.Abs(keyboard.Y)<.05f){g.DrawImage(sprite,rect);return;}
  string key=Math.Round(input.Yaw,3)+"|"+Math.Round(mouse.X,1)+"|"+Math.Round(mouse.Y,1)+"|"+Math.Round(keyboard.X,1)+"|"+Math.Round(keyboard.Y,1);PoseCache cache;if(!poses.TryGetValue(style,out cache)){cache=new PoseCache();poses[style]=cache;}if(cache.Key!=key){if(cache.Image!=null)cache.Image.Dispose();cache.Image=Warp(sprite,input.Yaw,mouse,keyboard);cache.Key=key;}g.DrawImage(cache.Image,new RectangleF(rect.X-40,rect.Y-40,rect.Width+80,rect.Height+80));
 }
 sealed class Texture {public int Width,Height;public byte[] Pixels;}
 static readonly Dictionary<Bitmap,Texture> textures=new Dictionary<Bitmap,Texture>();
 static Texture TextureOf(Bitmap bitmap){Texture texture;if(textures.TryGetValue(bitmap,out texture))return texture;bool premultiplied=bitmap.PixelFormat==PixelFormat.Format32bppPArgb;var pixels=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,premultiplied?PixelFormat.Format32bppPArgb:PixelFormat.Format32bppArgb);var bytes=new byte[bitmap.Width*bitmap.Height*4];for(int y=0;y<bitmap.Height;y++)Marshal.Copy(IntPtr.Add(pixels.Scan0,y*pixels.Stride),bytes,y*bitmap.Width*4,bitmap.Width*4);bitmap.UnlockBits(pixels);if(!premultiplied)for(int i=0;i<bytes.Length;i+=4)for(int c=0;c<3;c++)bytes[i+c]=(byte)((bytes[i+c]*bytes[i+3]+127)/255);texture=new Texture{Width=bitmap.Width,Height=bitmap.Height,Pixels=bytes};textures[bitmap]=texture;return texture;}
 static Bitmap Warp(Bitmap sprite,float yaw,PointF mouse,PointF keyboard){var texture=TextureOf(sprite);const int columns=16,rows=22;int width=texture.Width,height=texture.Height,padX=(int)Math.Round(width*40/270f),padY=(int)Math.Round(height*40/249f),outputWidth=width+padX*2,outputHeight=height+padY*2;var output=new byte[outputWidth*outputHeight*4];var vertices=new PointF[(columns+1)*(rows+1)];for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++){float u=x/(float)columns,v=y/(float)rows;var d=Deform(u,v,yaw,mouse,keyboard);vertices[y*(columns+1)+x]=new PointF(padX+u*width+d.X*width/270f,padY+v*height+d.Y*height/249f);}
  for(int y=0;y<rows;y++)for(int x=0;x<columns;x++){int a=y*(columns+1)+x;var p=vertices[a];var q=vertices[a+1];var r=vertices[a+columns+1];var t=vertices[a+columns+2];float u=x*width/(float)columns,v=y*height/(float)rows,du=width/(float)columns,dv=height/(float)rows;Triangle(output,outputWidth,outputHeight,texture,p,q,r,new PointF(u,v),new PointF(u+du,v),new PointF(u,v+dv));Triangle(output,outputWidth,outputHeight,texture,q,t,r,new PointF(u+du,v),new PointF(u+du,v+dv),new PointF(u,v+dv));}
  var result=new Bitmap(outputWidth,outputHeight,PixelFormat.Format32bppPArgb);var data=result.LockBits(new Rectangle(0,0,outputWidth,outputHeight),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);Marshal.Copy(output,0,data.Scan0,output.Length);result.UnlockBits(data);return result;
 }
 // Rasterize both halves of each mesh quad with shared vertices. GDI+ clipped
 // parallelograms leave one-pixel seams under non-affine arm bends; sampling a
 // continuous premultiplied texture avoids those holes and alpha fringes.
 static void Triangle(byte[] output,int width,int height,Texture source,PointF a,PointF b,PointF c,PointF sa,PointF sb,PointF sc){float determinant=(b.Y-c.Y)*(a.X-c.X)+(c.X-b.X)*(a.Y-c.Y);if(Math.Abs(determinant)<.001f)return;int left=Math.Max(0,(int)Math.Floor(Math.Min(a.X,Math.Min(b.X,c.X)))),right=Math.Min(width-1,(int)Math.Ceiling(Math.Max(a.X,Math.Max(b.X,c.X)))),top=Math.Max(0,(int)Math.Floor(Math.Min(a.Y,Math.Min(b.Y,c.Y)))),bottom=Math.Min(height-1,(int)Math.Ceiling(Math.Max(a.Y,Math.Max(b.Y,c.Y))));
  for(int y=top;y<=bottom;y++)for(int x=left;x<=right;x++){float u=((b.Y-c.Y)*(x+.5f-c.X)+(c.X-b.X)*(y+.5f-c.Y))/determinant,v=((c.Y-a.Y)*(x+.5f-c.X)+(a.X-c.X)*(y+.5f-c.Y))/determinant,w=1-u-v;if(u<-.0001f||v<-.0001f||w<-.0001f)continue;float sx=Math.Max(0,Math.Min(source.Width-1,sa.X*u+sb.X*v+sc.X*w-.5f)),sy=Math.Max(0,Math.Min(source.Height-1,sa.Y*u+sb.Y*v+sc.Y*w-.5f));int offset=(y*width+x)*4;int ix=(int)sx,iy=(int)sy,nx=Math.Min(source.Width-1,ix+1),ny=Math.Min(source.Height-1,iy+1);float fx=sx-ix,fy=sy-iy;int p0=(iy*source.Width+ix)*4,p1=(iy*source.Width+nx)*4,p2=(ny*source.Width+ix)*4,p3=(ny*source.Width+nx)*4;for(int channel=0;channel<4;channel++)output[offset+channel]=(byte)(source.Pixels[p0+channel]*(1-fx)*(1-fy)+source.Pixels[p1+channel]*fx*(1-fy)+source.Pixels[p2+channel]*(1-fx)*fy+source.Pixels[p3+channel]*fx*fy+.5f);}
 }
 static readonly PoseCache pixelScene=new PoseCache();
 // Rasterize tiny hardware and outlines at 2x, then integrate each 2x2 display
 // footprint. Cache the complete scene so a stationary pet does no extra work.
 // Padding retains the same deformation room as the mesh renderer.
 static void DrawPixelScene(Graphics g,float scale,Cs2InputState input,PointF hand){ulong held=0;for(int i=0;i<Keys.Length;i++)if(input.Down(Keys[i].Key))held|=1UL<<i;string key=scale+"|"+input.Yaw+"|"+input.MouseX+"|"+input.MouseY+"|"+input.MouseLeft+"|"+input.MouseRight+"|"+hand.X+"|"+hand.Y+"|"+held+"|"+input.LatestKey;int padding=(int)Math.Ceiling(40*scale);if(pixelScene.Key!=key){int width=(int)Math.Ceiling(320*scale)+padding*2,height=(int)Math.Ceiling(295*scale)+padding*2;using(var canvas=new Bitmap(width*2,height*2,PixelFormat.Format32bppPArgb)){using(var scene=Graphics.FromImage(canvas)){scene.Clear(Color.Transparent);scene.TranslateTransform(padding*2,padding*2);scene.ScaleTransform(2*scale,2*scale);DrawScene(scene,"pixel",input,hand);}var image=AreaDownsample(canvas,width,height);if(pixelScene.Image!=null)pixelScene.Image.Dispose();pixelScene.Image=image;pixelScene.Key=key;}}g.DrawImageUnscaled(pixelScene.Image,-padding,-padding);}
 public static void Draw(Graphics g,int width,int petTop,int size,string style,Cs2InputState input,PointF hand){var bounds=Bounds(width,petTop,size);var saved=g.Save();g.TranslateTransform(bounds.X,bounds.Y);if(style=="pixel")DrawPixelScene(g,Scale(size),input,hand);else{g.ScaleTransform(Scale(size),Scale(size));DrawScene(g,style,input,hand);}g.Restore(saved);}
 static void DrawScene(Graphics g,string style,Cs2InputState input,PointF hand){g.SmoothingMode=SmoothingMode.AntiAlias;
  Theme.Box(g,new RectangleF(18,219,298,72),Color.FromArgb(205,241,233,252),12);using(var pen=new Pen(Color.FromArgb(175,170,141,211),1.2f))using(var path=Theme.Round(new RectangleF(18,219,298,72),12))g.DrawPath(pen,path);
  var chassis=new[]{new PointF(182,207),new PointF(300,207),new PointF(316,277),new PointF(166,277)};using(var brush=new SolidBrush(Color.FromArgb(191,176,209)))g.FillPolygon(brush,chassis);using(var edge=new Pen(Color.FromArgb(155,134,182),1.2f))g.DrawPolygon(edge,chassis);using(var bevel=new Pen(Color.FromArgb(151,129,175),2.2f))g.DrawLine(bevel,167,277,315,277);
  foreach(var key in Keys){bool down=input.Down(key.Key);var r=KeycapBox(key);if(down)r.Y+=.9f;Theme.Box(g,new RectangleF(r.X,r.Y+1.1f,r.Width,r.Height),Color.FromArgb(154,139,178),1.5f);Theme.Box(g,r,down?Color.FromArgb(230,158,119,233):key.Movement?Color.FromArgb(165,175,137,227):Color.FromArgb(255,252,255),1.5f);using(var border=new Pen(Color.FromArgb(180,157,197),.6f))using(var path=Theme.Round(r,1.5f))g.DrawPath(border,path);string label=Cs2InputState.KeyLabel(key.Key);var text=g.Save();g.TranslateTransform(r.X+r.Width/2,r.Y+r.Height/2);g.RotateTransform(180);using(var font=Theme.Font(label.Length>3?2.8f:3.3f,true))Theme.ControlText(g,label,font,new Rectangle((int)(-r.Width/2),(int)(-r.Height/2),(int)r.Width,(int)r.Height),down?Color.White:Theme.Ink,true);g.Restore(text);}
  var mouse=MouseBounds(input);Theme.Box(g,mouse,Color.FromArgb(245,243,251),13);using(var pen=new Pen(Color.FromArgb(163,141,193),1.2f))using(var path=Theme.Round(mouse,13))g.DrawPath(pen,path);float buttons=mouse.Bottom-19;if(input.MouseLeft)Theme.Box(g,new RectangleF(mouse.X+17,buttons,12,16),Color.FromArgb(170,174,136,234),5);if(input.MouseRight)Theme.Box(g,new RectangleF(mouse.X+3,buttons,12,16),Color.FromArgb(170,174,136,234),5);using(var pen=new Pen(Color.FromArgb(171,145,205),1)){float tip=mouse.Bottom,direction=1;g.DrawLine(pen,mouse.X+16,tip-direction*2,mouse.X+16,tip-direction*19);g.DrawBezier(pen,mouse.X+16,tip,mouse.X+16,tip+direction*8,mouse.X+6,tip+direction*10,mouse.X+10,tip+direction*17);}Theme.Box(g,new RectangleF(mouse.X+14,mouse.Bottom-17,4,7),Color.FromArgb(158,130,197),2);
  var sprite=GetSprite(style);if(sprite!=null)DrawSprite(g,sprite,style,input,hand);
  if(input.Bubble!=""){float bubbleWidth;using(var font=Theme.Font(11,true))bubbleWidth=Math.Max(36,g.MeasureString(input.Bubble,font).Width+20);var bubble=new RectangleF(1,92,bubbleWidth,31);Theme.Box(g,bubble,Color.FromArgb(249,255,252,255),10);using(var border=new Pen(Theme.Purple,1.2f))using(var path=Theme.Round(bubble,10))g.DrawPath(border,path);using(var font=Theme.Font(11,true))Theme.ControlText(g,input.Bubble,font,Rectangle.Round(bubble),Theme.Purple,true);}
 }
}
}
