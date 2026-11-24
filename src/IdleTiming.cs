using System;
namespace DragonCompanion {
// A quiet, deterministic schedule: repeatable for previews, with different
// pauses in normal desktop use rather than a short metronomic animation loop.
internal static class IdleTiming {
 static readonly int[] tailCycles={18000,22500,20500,24000};
 static readonly int[] tailStarts={5000,7400,6100,9200};
 static readonly int[] blinks={2300,8100,13400,20650,21050,27400,33600,40900,47200,53000,53400,60200,67900,73700};
 internal static long TailPhase(long milliseconds){
  long t=Math.Max(0,milliseconds)%85000;
  for(int i=0;i<tailCycles.Length;i++){if(t<tailCycles[i]){long phase=t-tailStarts[i];return phase>=0&&phase<2800?phase:-1;}t-=tailCycles[i];}
  return -1;
 }
 internal static bool Blink(long milliseconds){long t=Math.Max(0,milliseconds)%78000;foreach(int start in blinks)if(t>=start&&t<start+200)return true;return false;}
 static float Ease(float t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 static float Look(long t,int start,int hold,int release,float direction){
  long phase=t-start;if(phase<0||phase>=350+hold+release)return 0;
  float amount=phase<350?Ease(phase/350f):phase<350+hold?1:1-Ease((phase-350-hold)/(float)release);
  return direction*.85f*amount;
 }
 internal static float Gaze(long milliseconds){
  long t=Math.Max(0,milliseconds)%36000;
  return Look(t,9500,600,450,1)+Look(t,15400,550,450,-1)+Look(t,29200,350,450,1);
 }
}
}
