using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
class PackageAuditTests {
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
    static void Reject(Action action,string name){try{action();}catch(IOException){Console.WriteLine("PASS "+name);return;}catch(InvalidDataException){Console.WriteLine("PASS "+name);return;}throw new Exception("not rejected: "+name);}
    static void Build(string target,IList<MixEntry> entries){using(var dll=typeof(MixPackage).Assembly.GetManifestResourceStream("bootstrap"))using(var player=typeof(MixPackage).Assembly.GetManifestResourceStream("player"))MixPackage.Build(target,dll,player,entries);}
    static int Main(string[] args){
        string root=args[0],demo=args[1];Directory.CreateDirectory(root);string package=Path.Combine(root,"test.mix");
        var entries=new List<MixEntry>{new MixEntry("demo.mp4",demo,0,new FileInfo(demo).Length)};Build(package,entries);
        var stale=MixPackage.Open(package);Build(package,entries);string currentId=MixPackage.Open(package).Id;
        Reject(()=>Build(package,stale.Entries),"stale package selection cannot overwrite newer package");
        Check(MixPackage.Open(package).Id==currentId,"conflict preserves current package");
        string other=Path.Combine(root,"other.mix");File.WriteAllText(other,"unrelated plugin");Reject(()=>Build(other,entries),"unrelated MIX cannot be overwritten");Check(File.ReadAllText(other)=="unrelated plugin","unrelated plugin preserved");
        var held=new ManualResetEvent(false);var release=new ManualResetEvent(false);var writer=new Thread(()=>{using(MixPackage.AcquireWriter(root)){held.Set();release.WaitOne();}});writer.Start();held.WaitOne();
        try{Reject(()=>Build(package,entries),"parallel save and create share directory writer gate");}finally{release.Set();writer.Join();held.Dispose();release.Dispose();}
        Check(MixPackage.Open(package).Id==currentId,"parallel rejection preserves package");
        var shortSource=new List<MixEntry>{new MixEntry("demo.mp4",demo,0,new FileInfo(demo).Length+1)};Reject(()=>Build(package,shortSource),"incomplete source rejected before commit");
        string export=Path.Combine(root,"export.mp4");File.WriteAllText(export,"original");using(var cancel=new CancellationTokenSource()){cancel.Cancel();try{MixPackage.Export(MixPackage.Open(package).Entries[0],export,cancel.Token);throw new Exception("not cancelled");}catch(OperationCanceledException){}}
        Check(File.ReadAllText(export)=="original"&&!Directory.GetFiles(root,"*.tmp").Any(),"cancelled extraction preserves existing file and removes partial data");
        var fresh=MixPackage.Open(package);Build(package,fresh.Entries);Check(MixPackage.Open(package).Entries[0].Length==entries[0].Length,"same-package refresh still works");
        string game=Path.Combine(root,"game");Directory.CreateDirectory(game);File.WriteAllText(Path.Combine(game,"war3.exe"),"");var first=QuickCreator.Create(QuickCreator.Plan(game,"first"),entries,false,false,"","KKWE");
        var original=MixPackage.Open(first.Path);QuickCreator.Create(QuickCreator.Plan(game,"renamed"),original.Entries,false,false,"","KKWE");
        Check(Directory.GetFiles(game,"*.mix").Length==1&&File.Exists(first.Path+".bak"),"rename in game root keeps one enabled package with original backup");return 0;
    }
}
