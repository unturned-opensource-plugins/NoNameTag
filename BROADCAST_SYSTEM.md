# NoNameTag 轮播公告系统

## 概述

NoNameTag 现在支持统一的广播系统，同时管理**死亡消息**和**轮播公告**。

## 核心特性

### 1. 死亡消息可见性控制

死亡消息现在支持 4 种可见性模式：

- **All** - 全员可见（默认）
- **KillerOnly** - 仅击杀者可见
- **VictimOnly** - 仅被杀者可见
- **KillerAndVictimOnly** - 击杀者和被杀者可见

### 2. 独立轮播公告组

- 每个广播组独立计时和轮换
- 支持多个独立的广播组同时运行
- 仅支持全员可见（简化设计）
- 可以启用/禁用特定广播组

## 配置说明

### 死亡消息配置

```xml
<DeathMessage>
  <Enabled>true</Enabled>
  <Visibility>All</Visibility>  <!-- All, KillerOnly, VictimOnly, KillerAndVictimOnly -->
  <Format>{killer} 击杀了 {victim}</Format>
  <SelfKillFormat>{victim} 自杀了</SelfKillFormat>
  <!-- 其他死亡原因格式... -->
</DeathMessage>
```

### 广播组配置

```xml
<BroadcastGroups>
  <BroadcastGroup name="server_rules" enabled="true">
    <RotationInterval>120</RotationInterval>  <!-- 轮换间隔（秒） -->
    <Messages>
      <Message>欢迎来到服务器！请遵守规则。</Message>
      <Message>使用 /help 查看可用命令。</Message>
    </Messages>
  </BroadcastGroup>
</BroadcastGroups>
```

## 命令

### /nametag broadcasts

查看所有广播组的运行状态：

```
/nametag broadcasts
```

输出示例：
```
Broadcast Groups Status:
  server_rules: Running
  vip_announcements: Running
  death_tips: Stopped
Use /nametag reload to restart all broadcasts.
```

### /nametag reload

重新加载配置并重启所有广播组：

```
/nametag reload
```

## 性能优化

### 线程安全设计

- 使用 `System.Timers.Timer` 在后台线程运行
- 定时器回调仅执行轻量级操作（更新索引、准备消息）
- 所有游戏对象操作（发送消息）在主线程执行
- 使用 `lock` 机制确保线程安全

### 资源管理

- 每个广播组使用独立的定时器实例
- 插件卸载时自动释放所有定时器
- 使用 `UnityMainThreadDispatcher` 确保主线程安全

### 性能限制建议

- **广播组数量**：建议 ≤ 20 个
- **最小轮换间隔**：建议 ≥ 10 秒
- **单条消息长度**：建议 ≤ 200 字符
- **每组消息数量**：建议 ≤ 20 条

## 示例配置

完整示例配置文件：`NoNameTagConfiguration.example.xml`

## 技术实现

### 统一广播服务

`BroadcastService` 同时管理：

1. **死亡消息处理** - 支持可见性控制
2. **轮播公告管理** - 独立计时和轮换

### 主线程安全

使用 `UnityMainThreadDispatcher` 确保后台线程（Timer）中的操作在 Unity 主线程执行：

```csharp
UnityMainThreadDispatcher.Instance().Enqueue(() =>
{
    ChatManager.serverSendMessage(message, Color.white, null, null, EChatMode.GLOBAL, null, true);
});
```

### 服务初始化顺序

1. `PermissionService` - 权限解析
2. `NameTagManager` - 名称标签管理
3. `BroadcastService` - 广播系统（死亡消息 + 轮播公告）
4. `NameTagDisplayService` - 名称标签显示

## 事件处理

### 插件加载

1. 验证配置
2. 初始化所有服务
3. 启动轮播公告定时器
4. 注册事件处理器

### 插件卸载

1. 停止所有轮播公告定时器
2. 注销事件处理器
3. 清理名称标签
4. 释放资源

### 玩家死亡

1. 根据死亡原因格式化消息
2. 应用彩色名称
3. 根据可见性控制发送消息
4. 记录调试日志

### 广播组轮换

1. 定时器触发（后台线程）
2. 更新消息索引
3. 准备消息文本
4. 通过主线程调度器发送消息

## 配置验证

配置验证器会检查：

- 广播组名称不能为空
- 轮换间隔必须 > 0
- 每个广播组必须至少有一条消息
- 消息不能为空
- 延迟时间不能为负数

## 向后兼容性

- 保留原有的 `DeathMessage` 配置结构
- `BroadcastGroups` 是可选的，可以不配置
- 使用旧版配置文件时，插件正常加载，无广播组运行

## 调试模式

启用 `DebugMode: true` 后，会输出详细的日志：

- 广播组启动/停止
- 消息发送
- 死亡消息处理
- 定时器触发

## 常见问题

### Q: 为什么轮播公告仅支持全员可见？

A: 简化设计，避免过度复杂化。死亡消息已经支持细粒度的可见性控制，轮播公告通常用于服务器公告，全员可见是合理的默认行为。

### Q: 如何停止某个广播组？

A: 在配置中设置 `enabled="false"`，然后使用 `/nametag reload` 重新加载。

### Q: 如何手动发送广播？

A: 使用 `/nametag reload` 重新加载配置，或者通过控制台调用 `BroadcastService.SendBroadcast()`。

### Q: 定时器会阻塞主线程吗？

A: 不会。使用 `System.Timers.Timer` 在后台线程运行，通过 `UnityMainThreadDispatcher` 将游戏对象操作调度到主线程。

### Q: 如何添加新的广播组？

A: 在配置文件的 `<BroadcastGroups>` 中添加新的 `<BroadcastGroup>`，然后使用 `/nametag reload` 重新加载。

## 版本历史

- **1.0.0** - 添加轮播公告系统和死亡消息可见性控制

## 开发者说明

### 新增文件

- `Models/BroadcastConfig.cs` - 统一的广播配置模型
- `Services/BroadcastService.cs` - 统一的广播服务
- `Utilities/UnityMainThreadDispatcher.cs` - Unity 主线程调度器

### 修改文件

- `NoNameTagConfiguration.cs` - 添加 BroadcastConfig 属性
- `NoNameTagPlugin.cs` - 替换 DeathMessageService 为 BroadcastService
- `ConfigValidator.cs` - 添加广播配置验证
- `NameTagCommand.cs` - 添加 broadcasts 子命令

### 删除文件

- `Models/DeathMessageConfig.cs` - 已合并到 BroadcastConfig.cs
- `Services/DeathMessageService.cs` - 已合并到 BroadcastService.cs
- `Services/IDeathMessageService.cs` - 已合并到 BroadcastService.cs
