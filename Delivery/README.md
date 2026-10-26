# 帆船美术与随机生成交付

仓库目标版本：Unity 6000.6.1f1。交付包已在该版本导出并在干净工程中重新导入、验证场景引用。Windows x64 演示需要完整安装同版本的 Windows Standalone Build Support 后按下方步骤构建。

- **SailingOut-ArtGeneration.unitypackage**：在另一个干净 Unity 6 工程中导入的完整资源包，含代码、原始 PNG、Sprite Sheet、动画、Prefab、配置、演示和中文说明。
- **art-atlas.png**：三种浮标、礁石、海鸟、补给和漂浮装饰的美术预览。
- **Verification/**：实际运行画面和验收数据。
- **../Builds/SailingOutDemo/SailingOutDemo.exe**：运行下方脚本后生成的 Windows 可玩演示；运行时保留整个同目录文件夹。`Builds/` 被 Git 忽略，克隆仓库后需要自行构建。
- **../Assets/SailingOut/Documentation/接入与制作指南.md**：主程接入方法、控制、种子调试、配置与制作流程。

## 重新生成交付物

安装仓库指定的 Unity 6000.6.1f1（含 Windows Standalone Build Support），然后在仓库根目录运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Build-Delivery.ps1
```

脚本用独立的临时工程导出 `.unitypackage`，在干净工程中导入并检查场景引用，再从导入的场景构建 `Builds/SailingOutDemo/SailingOutDemo.exe`。日志位于 `Logs/delivery-*.log`。

本工程直接打开 `Assets/SailingOut/Generated/Demo/SailingOutGenerationDemo.unity`，点击 Play 即可。请勿把交付包再次导回本工程，以免与已存在的嵌入式模块产生重复程序集。

操作：A/D 或左右方向键操船，Space 自动导航，W 触发每局 3 次、每次 3 秒的双倍前进加速（右上角闪电显示剩余次数），R 相同种子重来，N 换种子，F1 查看生成数量。

独立演示若遇到本机显卡的默认窗口模式问题，可在启动时附加 `-force-d3d11 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080`。美术与生成逻辑不依赖特定图形接口。
