using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

public class ManagerPreferences {
    public string GameRoot="",EditorRoot="",EditorKind="KKWE",MixName="War3Video.mix";
    public bool InstallTriggers=true,AllowUnrecognizedGame,AllowUnrecognizedEditor;
    public static string DefaultPath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"War3MixManager","settings.xml");}}
    public static ManagerPreferences Load(string path){if(!File.Exists(path))return new ManagerPreferences();using(var file=File.OpenRead(path))return (ManagerPreferences)new XmlSerializer(typeof(ManagerPreferences)).Deserialize(file);}
    public void Save(string path){
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{using(var file=File.Create(temp))new XmlSerializer(typeof(ManagerPreferences)).Serialize(file,this);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public string StartupRoot(string executableDirectory){
        if(!String.IsNullOrWhiteSpace(GameRoot)&&Directory.Exists(GameRoot))return Path.GetFullPath(GameRoot);
        if(File.Exists(Path.Combine(executableDirectory,"war3.exe")))return Path.GetFullPath(executableDirectory);
        try{using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Blizzard Entertainment\Warcraft III")){string path=key==null?null:key.GetValue("InstallPath") as string;if(!String.IsNullOrWhiteSpace(path)&&File.Exists(Path.Combine(path,"war3.exe")))return Path.GetFullPath(path);}}catch(System.Security.SecurityException){}
        return null;
    }
    public static List<string> VideoPackages(string root,out List<string> errors){
        var result=new List<string>();errors=new List<string>();
        foreach(string path in Directory.GetFiles(root,"*.mix")){
            try{
                using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){
                    if(f.Length<56)continue;f.Seek(-56,SeekOrigin.End);byte[] magic=new byte[8];if(f.Read(magic,0,8)!=8||Encoding.ASCII.GetString(magic)!="WVMIX001")continue;
                }
                MixPackage.Open(path);result.Add(path);
            }catch(IOException){errors.Add(Path.GetFileName(path));}catch(UnauthorizedAccessException){errors.Add(Path.GetFileName(path));}
        }
        return result;
    }
}
