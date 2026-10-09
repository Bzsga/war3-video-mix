# 定向验证源码

这两项验证只使用独立目录、文件标记和管理器界面，不启动魔兽或编辑器，不修改注册表。

先从本仓库构建 War3MixManager.exe。为测试新建独立目录，把该EXE复制进去；提供自己可使用的非空MP4测试文件。

使用 .NET Framework csc 编译：StartupManagementTests.cs 引用 War3MixManager.exe、System.Windows.Forms.dll 和 System.Drawing.dll；PackageGuardTests.cs 引用 War3MixManager.exe。测试EXE输出到同一个独立目录。

运行参数：测试EXE `<独立目录绝对路径>` `<MP4绝对路径>`。目录应是新建空目录，不使用游戏或编辑器安装目录。StartupManagementTests 创建后台位置的真实WinForms界面并验证保存；PackageGuardTests 验证错误输入不写包。通过以退出码0及PASS输出判断。
