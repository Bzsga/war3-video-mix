using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
class PackageGuardTests {
    static void Reject(Action action,string label){try{action();}catch(IOException){Console.WriteLine("PASS "+label);return;}throw new Exception("Expected rejection: "+label);}
    static int Main(string[] args){
        string root=args[0],game=Path.Combine(root,"game"),demo=args[1];Directory.CreateDirectory(game);File.WriteAllText(Path.Combine(game,"war3.exe"),"");
        var plan=QuickCreator.Plan(game,"guarded");var good=new List<MixEntry>{new MixEntry("demo.mp4",demo,0,new FileInfo(demo).Length)};
        Reject(()=>QuickCreator.Create(plan,new List<MixEntry>(),false,false,"","KKWE"),"empty video list blocks creation");
        Reject(()=>QuickCreator.Plan("","guarded"),"missing game directory blocks creation");
        Reject(()=>QuickCreator.Plan(Path.Combine(root,"missing"),"guarded"),"nonexistent game directory blocks creation");
        Reject(()=>QuickCreator.Create(plan,good,false,true,"","KKWE"),"checked installation requires editor directory");
        Reject(()=>QuickCreator.Create(plan,good,false,true,Path.Combine(root,"missing-editor"),"KKWE",true),"confirmed editor path must still exist");
        Reject(()=>QuickCreator.Create(plan,new List<MixEntry>{new MixEntry("missing.mp4",Path.Combine(root,"missing.mp4"),0,1)},false,false,"","KKWE"),"missing video source blocks creation");
        Reject(()=>QuickCreator.Create(plan,new List<MixEntry>{new MixEntry("empty.mp4",demo,0,0)},false,false,"","KKWE"),"zero-length video blocks creation");
        Reject(()=>QuickCreator.Create(plan,new List<MixEntry>{new MixEntry("demo.mp4",demo,0,new FileInfo(demo).Length+1)},false,false,"","KKWE"),"incomplete video source blocks creation");
        if(File.Exists(plan.Target)||Directory.GetFiles(game,"*.pending").Any())throw new Exception("Rejected input created a package or stage");Console.WriteLine("PASS rejection leaves no package or stage");
        var result=QuickCreator.Create(plan,good,false,false,"","KKWE");if(!File.Exists(result.Path)||result.EditorInstalled||result.EditorError!=null)throw new Exception("Optional editor creation failed");Console.WriteLine("PASS valid video and game directory create with editor installation unchecked");
        return 0;
    }
}
