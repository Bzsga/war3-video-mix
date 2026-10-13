# 定向验证源码

这些验证只使用独立目录、文件标记和管理器界面，不启动魔兽或编辑器，不修改注册表。

先从本仓库构建 War3MixManager.exe。为测试新建独立目录，把该EXE复制进去；提供自己可使用的非空MP4测试文件。

使用 .NET Framework csc 编译：StartupManagementTests.cs 引用 War3MixManager.exe、System.Windows.Forms.dll 和 System.Drawing.dll；PackageGuardTests.cs 与 PackageAuditTests.cs 引用 War3MixManager.exe。测试EXE输出到同一个独立目录。

运行参数：测试EXE `<独立目录绝对路径>` `<MP4绝对路径>`。目录应是新建空目录，不使用游戏或编辑器安装目录。StartupManagementTests 创建后台位置的真实WinForms界面并验证保存；PackageGuardTests 验证错误输入不写包。通过以退出码0及PASS输出判断。

PackageAuditTests.cs 覆盖旧包列表冲突、跨线程目录互斥、其他 MIX 保护、来源范围检查、取消导出清理、同包刷新以及根目录改名备份。此测试也仅使用独立临时目录，不修改实际游戏或编辑器。


PackageUpgradeTests.cs 另引用 System.Windows.Forms.dll、System.Drawing.dll 及 War3MixManager.exe。参数为 `<全新独立目录>` `<旧版本视频包路径>` `<未来版本程序集夹具路径>`。测试只读旧包并复制到独立目录；不要将目录指向游戏或编辑器。未来版本夹具可用 csc /target:library 编译仅包含 `[assembly:System.Reflection.AssemblyVersion("9.0.0.0")] public class FuturePlayer {}` 的 C# 文件。旧包须含旧运行组件；若由当前源码重建，不能拿它冒充旧版。测试覆盖实际升级/保存按钮、视频字节保留、无操作刷新、占用与冲突保护及防降级；不启动播放器或游戏。
