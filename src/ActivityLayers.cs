using System.Drawing;
namespace DragonCompanion {
public static class ActivityLayers {
 public static Bitmap[] Stabilize(Bitmap[] frames,string key,string style,int count){
  if(key=="read"){for(int i=1;i<frames.Length;i++){frames[i].Dispose();frames[i]=(Bitmap)frames[0].Clone();using(var graphics=Graphics.FromImage(frames[i]))ReadingBook.DrawPage(graphics,frames[i].Size,style,i/7f);}return frames;}
  if(ActivityModels.Has(key)){var models=new Bitmap[count];for(int i=0;i<count;i++)models[i]=(Bitmap)ActivityModels.Frame(style,key,i).Clone();foreach(var frame in frames)frame.Dispose();return models;}
  var result=ActivityRig.Create(frames[key=="tea"?7:0],key,style,count);foreach(var frame in frames)frame.Dispose();return result;
 }
}
}
