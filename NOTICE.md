# 来源与依赖

本仓库提供自建视频播放器、MIX 启动器、管理器及编辑器触发器定义。便携管理器内嵌自建播放器、原生 DLL、触发器资源和可选注册表文件，不含 Warcraft III、用户地图、MP4、私有日志、缓存或第三方解码库。

媒体解码使用 Windows 系统媒体栈。源码构建需要 .NET Framework 4.x 与 Zig；编辑器由用户提供兼容 KKWE/YDWE。外部依赖的许可证不因本项目改变。

可选注册表文件只将当前 Windows 用户的 Warcraft III Allow Local Files 设为 1，软件先提示确认，再由 Windows 询问导入，不静默修改。

工程方法与实机/夹具边界见 docs/LESSONS.md 和 VALIDATION.md。迁移仓库及更新软件链接没有增加新游戏平台验证。
