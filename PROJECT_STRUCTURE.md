# NoNameTag 项目结构

## 最终项目结构

```
NoNameTag/
├── NoNameTagPlugin.cs                    # 主插件入口点
├── NoNameTagConfiguration.cs             # 配置类
├── NoNameTagConfiguration.example.xml    # 配置文件示例
├── NoNameTag.csproj                      # 项目文件
├── NoNameTag.slnx                        # 解决方案文件
├── README.md                             # 用户文档
├── CLEANUP_REPORT.md                     # 代码清理历史
├── BROADCAST_SYSTEM.md                   # 轮播公告系统文档
├── IMPLEMENTATION_REPORT.md              # 实现报告
├── QUICK_START.md                        # 快速开始指南
├── PROJECT_STRUCTURE.md                  # 本文件
├── Commands/
│   └── NameTagCommand.cs                 # /nametag 命令处理器
├── Models/
│   ├── BroadcastConfig.cs                # 统一的广播配置模型
│   ├── DisplayEffectConfig.cs            # 视觉效果配置
│   └── PermissionGroupConfig.cs          # 权限组定义
├── Services/
│   ├── BroadcastService.cs               # 统一的广播服务
│   ├── IPermissionService.cs             # 权限服务接口
│   ├── INameTagManager.cs                # 名称标签管理器接口
│   ├── INameTagDisplayService.cs         # 名称标签显示服务接口
│   ├── PermissionService.cs              # 权限服务实现
│   ├── NameTagManager.cs                 # 名称标签管理器实现
│   └── NameTagDisplayService.cs          # 名称标签显示服务实现
├── Utilities/
│   ├── ConfigValidator.cs                # 配置验证工具
│   ├── Constants.cs                      # 全局常量
│   ├── Logger.cs                         # 结构化日志工具
│   ├── NameFormatter.cs                  # 名称格式化工具
│   └── UnityMainThreadDispatcher.cs      # Unity 主线程调度器
└── Data/                                 # 空目录
```

## 新增文件说明

### Models/BroadcastConfig.cs
定义统一的广播配置模型：
- `DeathMessageVisibility` - 死亡消息可见性枚举
- `BroadcastMessage` - 广播消息配置
- `BroadcastGroupConfig` - 广播组配置
- `DeathMessageConfig` - 增强的死亡消息配置
- `BroadcastConfig` - 统一的广播配置

### Services/BroadcastService.cs
统一广播服务实现：
- 处理死亡消息（支持可见性控制）
- 管理轮播公告（独立计时）
- 提供广播状态查询
- 线程安全的定时器管理

### Utilities/UnityMainThreadDispatcher.cs
Unity 主线程调度器：
- 在后台线程中安全地执行主线程操作
- 使用队列机制确保线程安全
- 自动管理游戏对象生命周期

## 修改文件说明

### NoNameTagConfiguration.cs
- 添加 `BroadcastGroups` 属性
- 添加 `Broadcast` 只读属性
- 更新 `LoadDefaults()` 方法

### NoNameTagPlugin.cs
- 替换 `DeathMessageService` 为 `BroadcastService`
- 在 `Load()` 中启动轮播公告
- 在 `Unload()` 中停止轮播公告
- 在 `ReloadServices()` 中重启轮播公告

### ConfigValidator.cs
- 添加 `ValidateBroadcastGroup()` 方法
- 在 `ValidateConfiguration()` 中验证广播组

### NameTagCommand.cs
- 添加 `broadcasts` 子命令
- 添加 `ShowHelp()` 方法
- 添加 `ExecuteBroadcasts()` 方法

## 删除文件说明

- `Models/DeathMessageConfig.cs` - 已合并到 BroadcastConfig.cs
- `Services/DeathMessageService.cs` - 已合并到 BroadcastService.cs
- `Services/IDeathMessageService.cs` - 已合并到 BroadcastService.cs

## 编译输出

```
bin/Debug/net48/NoNameTag.dll
```

## 依赖关系

### 外部依赖
- **Rocket.Unturned** (v3.x) - 插件框架
- **SDG.Unturned** - Unturned 游戏程序集
- **Steamworks** - Steam 集成
- **UnityEngine** - Unity 引擎类型

### 内部依赖关系

```
NoNameTagPlugin
├── PermissionService
├── NameTagManager
│   └── PermissionService
├── BroadcastService
│   └── NameTagManager
└── NameTagDisplayService
    └── NameTagManager
```

## 服务初始化顺序

1. **PermissionService** - 无依赖，提供权限解析功能
2. **NameTagManager** - 依赖 PermissionService
3. **BroadcastService** - 依赖 NameTagManager
4. **NameTagDisplayService** - 依赖 NameTagManager

## 事件处理流程

### 玩家连接
1. `OnPlayerConnected` → `ApplyPlayerEffects` → `NameTagManager.ApplyDisplayEffect`
2. 延迟应用名称标签（等待玩家初始化）

### 玩家断开
1. `OnPlayerDisconnected` → `CleanupPlayerData`
2. 移除显示效果、名称标签、权限缓存

### 玩家聊天
1. `OnPlayerChatted` → `BuildFormattedChatMessage`
2. 格式化消息并广播

### 玩家死亡
1. `OnPlayerDied` → `BroadcastService.HandlePlayerDeath`
2. 格式化死亡消息
3. 根据可见性控制发送消息

### 广播组轮换
1. `Timer.Elapsed` → `OnBroadcastTimerElapsed`
2. 更新消息索引
3. 通过 `UnityMainThreadDispatcher` 发送消息

## 配置验证流程

1. **插件加载时**：`ConfigValidator.ValidateConfiguration()`
2. **验证内容**：
   - 默认名称颜色格式
   - 每个权限组的权限字符串和优先级
   - 每个权限组的显示效果颜色
   - 每个广播组的名称、间隔、消息

## 日志类别

- `[PLUGIN]` - 插件通用操作
- `[PERM]` - 权限检查
- `[TAG]` - 名称标签操作
- `[DEATH]` - 死亡消息处理
- `[CMD]` - 命令执行
- `[CONFIG]` - 配置操作

## 命令系统

### /nametag reload
重新加载配置并重启所有服务

### /nametag refresh [player]
刷新指定玩家或所有玩家的显示效果

### /nametag check [player]
查看玩家的当前权限组和显示效果

### /nametag broadcasts
查看所有广播组的运行状态

## 性能优化

### 线程安全
- 使用 `lock` 机制确保并发安全
- 使用 `ConcurrentQueue` 管理主线程队列

### 资源管理
- 定时器及时 `Dispose()`
- 自动清理离线玩家缓存
- 限制缓存大小（最大 1000 条目）

### 主线程调度
- 后台线程仅执行轻量级操作
- 游戏对象操作在主线程执行
- 使用队列机制避免阻塞

## 配置示例

### 死亡消息可见性控制
```xml
<DeathMessage>
  <Enabled>true</Enabled>
  <Visibility>KillerOnly</Visibility>
  <Format>{killer} 击杀了 {victim}</Format>
</DeathMessage>
```

### 轮播公告组
```xml
<BroadcastGroups>
  <BroadcastGroup name="server_rules" enabled="true">
    <RotationInterval>120</RotationInterval>
    <Messages>
      <Message>欢迎来到服务器！</Message>
      <Message>请遵守规则。</Message>
    </Messages>
  </BroadcastGroup>
</BroadcastGroups>
```

## 文档文件

- **README.md** - 用户文档
- **CLEANUP_REPORT.md** - 代码清理历史
- **BROADCAST_SYSTEM.md** - 轮播公告系统详细文档
- **IMPLEMENTATION_REPORT.md** - 实现报告
- **QUICK_START.md** - 快速开始指南
- **PROJECT_STRUCTURE.md** - 项目结构说明
- **NoNameTagConfiguration.example.xml** - 配置文件示例

## 版本信息

- **版本**：1.0.0
- **实现日期**：2026-02-04
- **编译状态**：✅ 0 警告，0 错误
- **输出位置**：`bin/Debug/net48/NoNameTag.dll`
