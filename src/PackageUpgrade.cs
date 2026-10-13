using System;
using System.IO;
using System.Reflection;

public enum PackageRuntime { Current, UpdateAvailable, Newer }

public static class PackageUpgrade {
    // Inspect only the small runtime prefix; video data is never read for this check.
    public static PackageRuntime Inspect(string path,string expectedId=null) {
        using(var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) {
            var package=MixPackage.Open(path);
            if(expectedId!=null && package.Id!=expectedId)throw new IOException("视频包已被其他工具更新，请重新打开后再操作。");
            var assembly=typeof(PackageUpgrade).Assembly;
            using(var dll=assembly.GetManifestResourceStream("bootstrap"))
            using(var player=assembly.GetManifestResourceStream("player")) {
                if(Equal(input,0,package.PlayerOffset,dll)&&Equal(input,package.PlayerOffset,package.PlayerLength,player))return PackageRuntime.Current;
            }
            // GetAssemblyName reads metadata without loading or executing package code.
            if(package.PlayerLength>16*1024*1024)throw new InvalidDataException("包内播放器过大，请使用与此视频包对应的新版工具。");
            input.Position=package.PlayerOffset;
            var version=ReadVersion(input,package.PlayerLength);
            using(var player=assembly.GetManifestResourceStream("player"))
                return version>ReadVersion(player,player.Length)?PackageRuntime.Newer:PackageRuntime.UpdateAvailable;
        }
    }
    static Version ReadVersion(Stream source,long length) {
        string temp=Path.Combine(Path.GetTempPath(),"War3Video-version-"+Guid.NewGuid().ToString("N")+".exe");
        try {
            using(var output=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))MixPackage.Copy(source,output,length);
            return AssemblyName.GetAssemblyName(temp).Version;
        } finally {if(File.Exists(temp))File.Delete(temp);}
    }
    static bool Equal(Stream input,long offset,long length,Stream expected) {
        if(length!=expected.Length)return false;
        input.Position=offset;expected.Position=0;var a=new byte[16384];var b=new byte[16384];
        while(length>0){int size=(int)Math.Min(length,a.Length);Read(input,a,size);Read(expected,b,size);for(int i=0;i<size;i++)if(a[i]!=b[i])return false;length-=size;}
        return true;
    }
    static void Read(Stream stream,byte[] buffer,int count){int done=0;while(done<count){int n=stream.Read(buffer,done,count-done);if(n==0)throw new EndOfStreamException();done+=n;}}
    public static PackageRuntime CheckWritable(string path,string expectedId=null) {
        var status=Inspect(path,expectedId);
        if(status==PackageRuntime.Newer)throw new IOException("视频包由更新版本制作，请使用新版管理器，避免降级播放器。");
        return status;
    }
    public static bool Refresh(string source,string destination,string expectedId=null) {
        source=Path.GetFullPath(source);destination=Path.GetFullPath(destination);
        using(MixPackage.AcquireWriter(Path.GetDirectoryName(destination))) {
            var package=MixPackage.Open(source);
            var status=CheckWritable(source,expectedId??package.Id);
            if(source.Equals(destination,StringComparison.OrdinalIgnoreCase)&&status==PackageRuntime.Current)return false;
            if(File.Exists(destination)&&!source.Equals(destination,StringComparison.OrdinalIgnoreCase))CheckWritable(destination);
            var assembly=typeof(PackageUpgrade).Assembly;
            using(var dll=assembly.GetManifestResourceStream("bootstrap"))using(var player=assembly.GetManifestResourceStream("player"))MixPackage.Build(destination,dll,player,package.Entries);
            return true;
        }
    }
}
