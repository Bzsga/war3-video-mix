using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
class StartupManagementTests {
    static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static Type formType;
    static object Field(Form f,string n){return formType.GetField(n,flags).GetValue(f);}
    static Form Restore(string settings){var form=(Form)Activator.CreateInstance(formType,true);form.StartPosition=FormStartPosition.Manual;form.Location=new System.Drawing.Point(-3000,-3000);formType.GetMethod("RestoreStartup",flags).Invoke(form,new object[]{settings});return form;}
    static void Check(bool ok,string name){if(!ok){Console.WriteLine("FAIL "+name);throw new Exception(name);}Console.WriteLine("PASS "+name);}
    [STAThread]static int Main(string[] args){
        string run=args[0],demo=args[1],game=Path.Combine(run,"game"),settings=Path.Combine(run,"settings.xml");Directory.CreateDirectory(game);File.WriteAllText(Path.Combine(game,"war3.exe"),"");File.WriteAllText(Path.Combine(game,"unrelated.mix"),"other plugin");
        var clip=new List<MixEntry>{new MixEntry("demo.mp4",demo,0,new FileInfo(demo).Length)};QuickCreator.Create(QuickCreator.Plan(game,"existing"),clip,false,false,"","KKWE");
        new ManagerPreferences{GameRoot=game,InstallTriggers=false}.Save(settings);Application.EnableVisualStyles();formType=Assembly.LoadFrom(Path.Combine(run,"War3MixManager.exe")).GetType("MixManager");
        using(var form=Restore(settings)){
            Check(((TabControl)Field(form,"workflows")).SelectedTab.Text=="管理已有视频包","startup switches to renamed management page");
            Check(((ListView)Field(form,"list")).Items.Count==1&&((string)Field(form,"current")).EndsWith("existing.mix"),"startup opens only valid video package and ignores unrelated plugin");
            Check(((Button)Field(form,"save")).Text=="已是当前版本"&&!((Button)Field(form,"save")).Enabled,"management skips unchanged runtime");
            Check(ManagerPreferences.Load(settings).GameRoot==game,"game directory preferences roundtrip");
            string second=Path.Combine(run,"second.mp4");File.Copy(demo,second);formType.GetMethod("Import",flags).Invoke(form,new object[]{new[]{second}});
            bool saved=false;DateTime deadline=DateTime.UtcNow.AddSeconds(12);var timer=new Timer{Interval=100};
            timer.Tick+=delegate{saved=!(bool)Field(form,"busy")&&((Label)Field(form,"info")).Text.StartsWith("已保存视频包：");if(saved||DateTime.UtcNow>deadline){timer.Stop();formType.GetField("dirty",flags).SetValue(form,false);form.Close();}};
            form.Shown+=delegate{timer.Start();((Button)Field(form,"save")).PerformClick();};Application.Run(form);timer.Dispose();
            Check(saved&&MixPackage.Open(Path.Combine(game,"existing.mix")).Entries.Count==2,"actual footer save updates package and displays success");
        }
        string empty=Path.Combine(run,"empty");Directory.CreateDirectory(empty);File.WriteAllText(Path.Combine(empty,"war3.exe"),"");new ManagerPreferences{GameRoot=empty}.Save(settings);
        using(var form=Restore(settings)){Check(((TabControl)Field(form,"workflows")).SelectedIndex==0&&Field(form,"current")==null,"no package keeps quick creation page");Check(((Button)Field(form,"save")).Text=="生成视频包","quick page keeps generation action");}
        File.Copy(Path.Combine(game,"existing.mix"),Path.Combine(game,"another.mix"));new ManagerPreferences{GameRoot=game}.Save(settings);
        using(var form=Restore(settings)){Check(Field(form,"current")==null&&((Label)Field(form,"info")).Text.Contains("发现多个视频包"),"multiple packages require explicit choice");}
        File.WriteAllText(settings,"invalid XML");using(var form=Restore(settings)){Check(((TabControl)Field(form,"workflows")).SelectedIndex==0&&((Label)Field(form,"info")).Text.Contains("重新选择目录"),"corrupt settings safely fall back");}
        Check(new ManagerPreferences{GameRoot=Path.Combine(run,"missing")}.StartupRoot(empty)==empty,"missing saved directory can fall back to executable game directory");return 0;
    }
}
