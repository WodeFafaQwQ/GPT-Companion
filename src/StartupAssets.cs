using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace DragonCompanion {
public static class StartupAssets {
 public static int LoadedAssetCount {get{return Art.LoadedAssetCount+RigArt.LoadedAssetCount+Cs2Art.LoadedAssetCount+ActivityArt.LoadedAssetCount;}}
 public static void LoadAll(){Art.Load();IdleArt.Prepare("pixel");IdleArt.Prepare("anime");Art.LoadPickup();Art.LoadMania();RigArt.Load();Cs2Art.Load();ActivityArt.Load();}
 public static async Task PrepareMotionLater(string style){
  // Let the first pet/tray paint and the dashboard finish before preparing the
  // current outfit's common interactions. Other outfits and game art stay lazy.
  await Task.Delay(1500).ConfigureAwait(false);
  try{await Task.Run(()=>Art.PrepareMotion(style)).ConfigureAwait(false);}catch(Exception e){Store.Log("动作素材预加载："+e.GetType().Name);}
 }
}
public static class StartupDiagnostics {
 // Run in a fresh process for a cold in-process asset cache. The diagnostic
 // profile stays inside the output folder and never changes the user's data.
 public static int Run(string folder,bool eager,string style,Stopwatch clock){
  folder=Path.GetFullPath(folder);Directory.CreateDirectory(folder);
  Store.Root=Path.Combine(folder,"isolated-profile-"+Guid.NewGuid().ToString("N"));Store.Init();
  var report=new Dictionary<string,object>();report["mode"]=eager?"eager":"lazy";report["style"]=style;report["ui_initialization_ms"]=clock.ElapsedMilliseconds;
  try{
   Store.Save("settings.json",new Settings{Style=style});
   if(eager)StartupAssets.LoadAll();
   report["assets_before_controller"]=StartupAssets.LoadedAssetCount;
   using(var controller=new Controller(true,true)){
    report["controller_ready_ms"]=clock.ElapsedMilliseconds;
    using(var first=controller.Pet.Compose()){report["first_pet_ready_ms"]=clock.ElapsedMilliseconds;first.Save(Path.Combine(folder,"first-pet.png"),ImageFormat.Png);}
    report["initial_asset_count"]=StartupAssets.LoadedAssetCount;
    var remaining=Stopwatch.StartNew();StartupAssets.LoadAll();remaining.Stop();
    report["remaining_assets_ms"]=remaining.ElapsedMilliseconds;report["all_asset_count"]=StartupAssets.LoadedAssetCount;
    controller.Quit();
   }
   File.WriteAllText(Path.Combine(folder,"startup.json"),Json.Encode(report));return 0;
  }catch(Exception e){File.WriteAllText(Path.Combine(folder,"startup-error.txt"),e.ToString());return 1;}
 }
}
}
