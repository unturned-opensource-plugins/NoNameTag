# NoNameTag

NoNameTag 是一个 Rocket.Unturned 服务器插件，用于管理玩家名称标签，提供自定义显示效果、权限组管理、聊天消息格式化和死亡消息自定义等功能。

## 功能特性

- **自定义名称标签**：为不同权限组的玩家设置前缀、后缀和颜色
- **权限组管理**：支持多个权限组，可配置优先级
- **聊天消息格式化**：在聊天消息中显示玩家的权限标签和颜色
- **死亡消息自定义**：支持 13 种不同死亡原因的自定义消息
- **轮播公告系统**：定期向所有玩家发送公告消息
- **头像显示**：在聊天消息和公告中显示玩家 Steam 头像或自定义头像

## 安装

1. 编译项目：使用 `Ctrl + Shift + B` 或 `Build` -> `Build Solution`
2. 复制 DLL：将 `bin/Debug/net48/NoNameTag.dll` 复制到服务器的 `Rocket/Plugins` 文件夹
3. 配置文件：编辑 `Rocket/Plugins/NoNameTag/NoNameTag.configuration.xml` 进行配置

## 配置

### 基本设置

```xml
<Enabled>true</Enabled>                    <!-- 启用/禁用插件 -->
<DebugMode>false</DebugMode>               <!-- 调试模式 -->
<DefaultNameColor>#FFFFFF</DefaultNameColor> <!-- 默认名称颜色 -->
<ApplyToChatMessages>true</ApplyToChatMessages> <!-- 应用到聊天消息 -->
<ApplyToNameTags>true</ApplyToNameTags>    <!-- 应用到名称标签 -->
<PriorityMode>HighestPriority</PriorityMode> <!-- 优先级模式 -->
```

### 权限组配置

```xml
<PermissionGroup permission="nonametag.vip" priority="10">
  <DisplayEffect>
    <Prefix>[VIP] </Prefix>
    <PrefixColor>#FFD700</PrefixColor>
    <NameColor>#00FF00</NameColor>
    <Suffix></Suffix>
    <SuffixColor>#FFFFFF</SuffixColor>
  </DisplayEffect>
</PermissionGroup>
```

### 死亡消息配置

```xml
<DeathMessage>
  <Enabled>true</Enabled>
  <Format>{killer} 击杀了 {victim}</Format>
  <SelfKillFormat>{victim} 自杀了</SelfKillFormat>
  <ZombieFormat>{victim} 被僵尸击杀</ZombieFormat>
  <!-- 更多死亡原因... -->
</DeathMessage>
```

### 广播组配置

```xml
<BroadcastGroup name="server_rules" enabled="true">
  <RotationInterval>120</RotationInterval>
  <Messages>
    <Message>
      <Message>欢迎来到服务器！</Message>
      <DelaySeconds>0</DelaySeconds>
      <Avatar>{server_icon}</Avatar>
      <AvatarPosition>Left</AvatarPosition>
    </Message>
  </Messages>
</BroadcastGroup>
```

## 命令

- `/nametag reload` - 重新加载配置并刷新所有玩家
- `/nametag refresh [player]` - 刷新指定玩家或所有玩家的显示效果
- `/nametag check [player]` - 查看玩家的权限组和显示效果

需要权限：`nonametag.admin`

## 权限

- `nonametag.admin` - 管理员权限（使用命令）
- `nonametag.vip` - VIP 权限组
- `nonametag.mvp` - MVP 权限组
- 自定义权限组（在配置文件中定义）

## 构建

### 使用 Visual Studio

按 `Ctrl + Shift + B` 或选择 `Build` -> `Build Solution`

### 使用命令行

```bash
dotnet build NoNameTag.slnx -p:Configuration=Debug
```

### 输出位置

- Debug 版本：`bin/Debug/net48/NoNameTag.dll`
- Release 版本：`bin/Release/net48/NoNameTag.dll`

## 测试

1. 启动 Unturned 服务器（已安装 Rocket）
2. 检查控制台是否显示加载消息：
   ```
   [loading] NoNameTag
   [timestamp] [Info] NoNameTag >> NoNameTag 1.0.0 has been loaded!
   ```
3. 以具有配置权限的玩家身份连接
4. 验证名称标签、聊天消息和死亡消息是否正确显示

## 架构

### 核心组件

- **NoNameTagPlugin.cs** - 主插件入口点
- **PermissionService** - 权限解析服务
- **NameTagManager** - 名称标签管理
- **NameTagDisplayService** - 名称标签显示
- **DeathMessageService** - 死亡消息处理
- **BroadcastService** - 轮播公告系统

### 数据模型

- **DisplayEffectConfig** - 显示效果配置
- **PermissionGroupConfig** - 权限组配置
- **DeathMessageConfig** - 死亡消息配置
- **BroadcastGroupConfig** - 广播组配置
- **AvatarConfig** - 头像配置

## 依赖

- Rocket.Unturned (v3.x)
- SDG.Unturned
- Steamworks
- UnityEngine

## 常见问题

### Q: 如何添加新的权限组？
A: 编辑 `NoNameTag.configuration.xml`，在 `<PermissionGroups>` 中添加新的 `<PermissionGroup>` 元素，然后使用 `/nametag reload` 重新加载。

### Q: 如何自定义死亡消息？
A: 编辑 `NoNameTag.configuration.xml` 中的 `<DeathMessage>` 部分，使用 `{victim}` 和 `{killer}` 占位符。

### Q: 如何启用调试模式？
A: 在配置文件中设置 `<DebugMode>true</DebugMode>`，然后重新加载配置。

## 许可证

本项目遵循相关许可证。

## 感谢

感谢以下项目的参考和灵感：
- [RichMessageAnnouncer](https://github.com/RestoreMonarchyPlugins/RichMessageAnnouncer) - 提供了聊天消息和头像显示的实现参考
