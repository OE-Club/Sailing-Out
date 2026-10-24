# 帆船美术与随机生成交付

仓库目标版本：Unity 6000.6.1f1。交付包和 Windows x64 演示已在 Unity 6000.6.7f1 上验证，未在目标补丁版本 6000.6.1f1 上实测。

- **SailingOut-ArtGeneration.unitypackage**：在另一个干净 Unity 6 工程中导入的完整资源包，含代码、原始 PNG、Sprite Sheet、动画、Prefab、配置、演示和中文说明。
- **art-atlas.png**：三种浮标、礁石、海鸟、补给和漂浮装饰的美术预览。
- **Verification/**：实际运行画面和验收数据。
- **../Builds/SailingOutDemo/SailingOutDemo.exe**：Windows 可玩演示，保留整个同目录文件夹。
- **../Assets/SailingOut/Documentation/接入与制作指南.md**：主程接入方法、控制、种子调试、配置与制作流程。

本工程直接打开 `Assets/SailingOut/Generated/Demo/SailingOutGenerationDemo.unity`，点击 Play 即可。请勿把交付包再次导回本工程，以免与已存在的嵌入式模块产生重复程序集。

操作：A/D 或左右方向键操船，Space 自动导航，R 相同种子重来，N 换种子，F1 查看生成数量。

独立演示若遇到本机显卡的默认窗口模式问题，可在启动时附加 `-force-d3d11 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080`。美术与生成逻辑不依赖特定图形接口。
