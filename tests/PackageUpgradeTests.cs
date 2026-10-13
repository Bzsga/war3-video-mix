using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Collections.Generic;
class PackageUpgradeTests {
    static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Field(Form f,string n){return f.GetType().GetField(n,flags).GetValue(f);}
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
    static void Reject(Action a,string name){try{a();}catch(IOException){Console.WriteLine("PASS "+name);return;}catch(InvalidDataException){Console.WriteLine("PASS "+name);return;}throw new Exception("not rejected: "+name);}
    static string Hash(byte[] b){using(var h=SHA256.Create())return Convert.ToBase64String(h.ComputeHash(b));}
    static Dictionary<string,string> Clips(string path){var result=new Dictionary<string,string>();using(var f=File.OpenRead(path))foreach(var e in MixPackage.Open(path).Entries){f.Position=e.Offset;using(var m=new MemoryStream()){MixPackage.Copy(f,m,e.Length);result[e.Name]=Hash(m.ToArray());}}return result;}
    [STAThread] static void Main(string[] args){
        string root=args[0],old=args[1],future=args[2],path=Path.Combine(root,"game","existing.mix");Directory.CreateDirectory(Path.GetDirectoryName(path));File.Copy(old,path);File.WriteAllText(Path.Combine(root,"game","war3.exe"),"");
        var before=Clips(path);var oldPackage=MixPackage.Open(path);
        Check(PackageUpgrade.Inspect(path)==PackageRuntime.UpdateAvailable,"old runtime detected");
        Application.EnableVisualStyles();var type=typeof(PackageUpgrade).Assembly.GetType("MixManager");
        string settings=Path.Combine(root,"settings.xml");new ManagerPreferences{GameRoot=Path.GetDirectoryName(path),InstallTriggers=false}.Save(settings);
        using(var form=(Form)Activator.CreateInstance(type,true)){
            form.StartPosition=FormStartPosition.Manual;form.Location=new System.Drawing.Point(-3000,-3000);
            type.GetMethod("RestoreStartup",flags).Invoke(form,new object[]{settings});
            Check(((Button)Field(form,"save")).Text=="升级视频包"&&((TabControl)Field(form,"workflows")).SelectedIndex==1,"startup opens existing package with upgrade action");
            bool done=false;DateTime deadline=DateTime.UtcNow.AddSeconds(20);var timer=new Timer{Interval=50};
            timer.Tick+=delegate{done=!(bool)Field(form,"busy")&&((Label)Field(form,"info")).Text.StartsWith("升级完成");if(done||DateTime.UtcNow>deadline){timer.Stop();form.Close();}};
            form.Shown+=delegate{timer.Start();((Button)Field(form,"save")).PerformClick();};Application.Run(form);timer.Dispose();
            Check(done,"real WinForms upgrade button completes");
            Check(!((Button)Field(form,"save")).Enabled&&((Button)Field(form,"save")).Text=="已是当前版本","unchanged package has no redundant save action");
        }
        Check(before.OrderBy(p=>p.Key).SequenceEqual(Clips(path).OrderBy(p=>p.Key)),"all existing video names and bytes preserved");
        Check(MixPackage.Open(path+".bak").Id==oldPackage.Id,"original package retained as backup");
        var stamp=File.GetLastWriteTimeUtc(path);var id=MixPackage.Open(path).Id;
        Check(!PackageUpgrade.Refresh(path,path)&&File.GetLastWriteTimeUtc(path)==stamp&&MixPackage.Open(path).Id==id,"repeat upgrade writes nothing and retains cache identity");
        Reject(()=>PackageUpgrade.Refresh(path,path,oldPackage.Id),"stale opened package rejected even when runtime is current");
        using(var held=File.Open(path,FileMode.Open,FileAccess.Read,FileShare.Read)){
            string locked=Path.Combine(root,"locked.mix");File.Copy(old,locked);
            using(var lockOld=File.Open(locked,FileMode.Open,FileAccess.Read,FileShare.Read))Reject(()=>PackageUpgrade.Refresh(locked,locked),"occupied old package remains protected");
            Check(MixPackage.Open(locked).Id==oldPackage.Id,"failed replacement leaves old package intact");
        }
        string newer=Path.Combine(root,"newer.mix");using(var dll=typeof(PackageUpgrade).Assembly.GetManifestResourceStream("bootstrap"))using(var player=File.OpenRead(future))MixPackage.Build(newer,dll,player,MixPackage.Open(path).Entries);
        Check(PackageUpgrade.Inspect(newer)==PackageRuntime.Newer,"future player version recognized");Reject(()=>PackageUpgrade.Refresh(newer,newer),"automatic downgrade rejected");
        string unrelated=Path.Combine(root,"plugin.mix");File.WriteAllText(unrelated,"other plugin");Reject(()=>PackageUpgrade.Refresh(path,unrelated),"unrelated MIX not overwritten");
        // Unsaved edits and runtime upgrade share the ordinary save transaction.
        File.Copy(old,path,true);
        using(var form=(Form)Activator.CreateInstance(type,true)){
            form.StartPosition=FormStartPosition.Manual;form.Location=new System.Drawing.Point(-3000,-3000);type.GetMethod("RestoreStartup",flags).Invoke(form,new object[]{settings});
            string extra=Path.Combine(root,"added.mp4");File.WriteAllBytes(extra,new byte[]{1,2,3,4});type.GetMethod("Import",flags).Invoke(form,new object[]{new[]{extra}});
            Check(((Button)Field(form,"save")).Text=="保存并升级","unsaved edits merged with upgrade action");
            bool done=false;DateTime deadline=DateTime.UtcNow.AddSeconds(20);var timer=new Timer{Interval=50};timer.Tick+=delegate{done=!(bool)Field(form,"busy")&&((Label)Field(form,"info")).Text.StartsWith("已保存视频包");if(done||DateTime.UtcNow>deadline){timer.Stop();type.GetField("dirty",flags).SetValue(form,false);form.Close();}};
            form.Shown+=delegate{timer.Start();((Button)Field(form,"save")).PerformClick();};Application.Run(form);timer.Dispose();Check(done&&MixPackage.Open(path).Entries.Any(e=>e.Name=="added.mp4")&&PackageUpgrade.Inspect(path)==PackageRuntime.Current,"save preserves pending addition and upgrades runtime");
        }
        Check(!Directory.GetFiles(root,"*.tmp",SearchOption.AllDirectories).Any(),"no partial package files left");
    }
}
