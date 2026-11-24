using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace DragonCompanion {
public static class CelebrationFx {
 public const float Gravity=275;
 // Two modest hops followed by a longer grounded pause. A launch is an impulse;
 // ascent decelerates and descent accelerates under the same fixed gravity.
 static bool Flight(long milliseconds,out float time,out float duration){long phase=Math.Max(0,milliseconds)%2800;time=0;duration=0;if(phase>=180&&phase<580){time=(phase-180)/1000f;duration=.4f;return true;}if(phase>=1050&&phase<1410){time=(phase-1050)/1000f;duration=.36f;return true;}return false;}
 public static float Jump(long milliseconds){float t,duration;return Flight(milliseconds,out t,out duration)?Math.Max(0,.5f*Gravity*t*(duration-t)):0;}
 public static float JumpVelocity(long milliseconds){float t,duration;return Flight(milliseconds,out t,out duration)?Gravity*(duration*.5f-t):0;}
 static float Ease(float t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 // Analytic trajectories follow the render clock, independent of pose frames.
 // Fade-in/out makes each particle's respawn continuous.
 public static void Draw(Graphics graphics,Rectangle bounds,long milliseconds){var saved=graphics.Save();graphics.SmoothingMode=SmoothingMode.AntiAlias;float unit=Math.Min(bounds.Width,bounds.Height)/192f;graphics.TranslateTransform(bounds.X+(bounds.Width-192*unit)/2,bounds.Y+(bounds.Height-192*unit));graphics.ScaleTransform(unit,unit);for(int i=0;i<18;i++){float life=2600+i%4*230,phase=(milliseconds+i*193)%life/life,fade=Ease(phase/.12f)*(1-Ease((phase-.78f)/.22f));float side=i%2==0?-1:1,x=96+side*(35+phase*32)+5*(float)Math.Sin(phase*7+i),y=92-80*phase+70*phase*phase+(i%3-1)*16;var state=graphics.Save();graphics.TranslateTransform(x,y);graphics.RotateTransform((float)(phase*180*(i%2==0?1:-1)+i*23));Color color=i%3==0?Color.FromArgb((int)(190*fade),166,125,221):i%3==1?Color.FromArgb((int)(185*fade),236,178,214):Color.FromArgb((int)(185*fade),234,205,124);using(var brush=new SolidBrush(color)){if(i%3==0){for(int petal=0;petal<5;petal++){graphics.RotateTransform(72);graphics.FillEllipse(brush,-1.2f,-4.2f,2.4f,4);}}else graphics.FillRectangle(brush,-1,-3,2,6);}graphics.Restore(state);}graphics.Restore(saved);}
}
}
