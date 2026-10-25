# 汉末英雄传 · 刘备篇 —— Unity Demo（第一章 桃园结义）

吞食天地风格的回合制 RPG 原型。全部用代码绘制（色块 + 文字），不需要任何美术资源，导入就能玩。

## 运行方法

1. 用 Unity Hub 新建一个项目（2D 模板即可）。支持 Unity 2021.3 LTS 及以上，包括 Unity 6。
2. 把压缩包里的 `Assets/LiuBeiRPG` 整个文件夹拖进项目的 `Assets` 目录。
3. 打开任意场景（默认的 SampleScene 就行），按 **Play**。
   不需要在场景里放任何东西，游戏会自动启动（`GameRoot.Boot`）。

**操作**：方向键 / WASD 移动，Z / 空格 / 回车 确认、对话，X / Esc 取消。战斗菜单也可以用鼠标点。

> 中文显示成方块时：找一个中文字体（比如 Noto Sans SC 的 .ttf/.otf），改名为 `ChineseFont`，
> 放进 `Assets/LiuBeiRPG/Resources/` 即可。

## Demo 内容

| 段落 | 内容 |
|---|---|
| 涿县 | 看榜文 → 遇张飞 → 酒馆遇关羽 → 桃园三结义（关张入队）→ 商人张世平赠金赠兵器 → 客栈休整 |
| 涿郡郊外 | 随机遇敌：黄巾兵 / 力士 / 术士（会妖术）/ 头目，练级 |
| 黄巾营寨 | Boss：程远志 + 邓茂 + 护卫，不可撤退 |
| 结束 | 章节结算，提示下一章「青州解围」 |

战斗系统：
- **兵力 = 血量，也影响伤害**：兵力越少输出越低，100 / 1000 / 10000 是明显台阶。
- **菜单**：下令（逐个武将选 攻击 / 策略 / 防御）、一齐攻击、撤退。
- **全队共享**等级、经验、策略点。成长型武将升级时只涨兵力，这一点和原作一样。
- **策略**：火计、疗兵（1 级），落石（3 级），鼓舞（4 级）。命中率看双方智力差。
- **全灭**后退回涿县客栈，金钱减半。

数值参考：1v1 自动模拟下，1 级打 Boss 胜率约 35%，2 级约 80%，3 级以上基本稳赢。正常玩法练到 2～3 级再去打比较合适。

## 代码结构

```
Scripts/
  Core/                 纯 C# 逻辑，不依赖 Unity，可以单独测试
    General.cs          武将数据（GeneralDef）和战斗实例（General）
    Party.cs            队伍：等级、经验、金钱、策略点
    Tactics.cs          策略表
    Formula.cs          伤害、命中、撤退公式 ← 调平衡改这里
    BattleSystem.cs     回合制战斗：下令 → 敌方 AI → 按速度结算 → 胜负
    GameDatabase.cs     武将数值表、随机遇敌、Boss 编队 ← 改数值改这里
  World/                剧情和地图，也是纯 C#
    MapData.cs          地图格子、NPC、地块图例
    IGameHost.cs        剧情脚本调用游戏的接口（对话、开战、换地图）
    Chapter1.cs         第一章全部地图、NPC、剧情 ← 写新章节照抄这个
  Game/                 Unity 表现层（IMGUI）
    GameRoot.cs         入口、状态切换、输入、对话框
    GameRoot.Map.cs     大地图移动、跟随队列、遇敌
    GameRoot.Battle.cs  战斗菜单和战报播放
    Gfx.cs              绘图工具
```

**改地图**：`Chapter1.cs` 里的字符串数组就是地图。`.` 草地，`,` 野外草地（会遇敌），`=` 路，`T` 树，`H` 房屋，`M` 山，`~` 水，`P` 桃树，`g` 城门，`C` 营寨。图例在 `MapData.cs` 的 `Tiles.Legend`。

**加新章节**：复制 `Chapter1.cs` 改成 `Chapter2.cs`，换地图字符串、NPC、剧情标记和 Boss 编队即可。

## 建议的下一步

1. **存档**：存 `Party` 加 `StoryFlags`，转成 JSON 写进 `Application.persistentDataPath`。
2. **换成正式美术**：把 Game 层换成 Tilemap + Sprite，Core 和 World 不用动。
3. **数据表化**：`GameDatabase` 改成 ScriptableObject 或 CSV，方便批量加武将。
4. **招降系统**：战胜敌将后有概率劝降入队，这是吞食天地的灵魂。
5. **阵型、装备、商店、后备队员**（原作 5 人上场 + 2 人后备）。
