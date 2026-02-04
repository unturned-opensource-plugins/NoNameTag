# NoNameTag 轮播公告系统 - 快速开始

## 快速安装

### 1. 编译插件
```bash
dotnet build NoNameTag.slnx -p:Configuration=Debug
```

编译后的 DLL 位于：`bin/Debug/net48/NoNameTag.dll`

### 2. 部署插件
将 `NoNameTag.dll` 复制到服务器的 `Rocket/Plugins/NoNameTag/` 目录。

### 3. 配置文件
首次运行会生成默认配置文件：`Rocket/Plugins/NoNameTag/NoNameTagConfiguration.xml`

## 基本配置

### 启用死亡消息可见性控制

```xml
<DeathMessage>
  <Enabled>true</Enabled>
  <Visibility>All</Visibility>  <!-- All, KillerOnly, VictimOnly, KillerAndVictimOnly -->
  <Format>{killer} 击杀了 {victim}</Format>
</DeathMessage>
```

### 添加轮播公告组

```xml
<BroadcastGroups>
  <BroadcastGroup name="server_rules" enabled="true">
    <RotationInterval>120</RotationInterval>  <!-- 120秒轮换一次 -->
    <Messages>
      <Message>欢迎来到服务器！请遵守规则。</Message>
      <Message>使用 /help 查看可用命令。</Message>
      <Message>禁止恶意破坏和骚扰其他玩家。</Message>
    </Messages>
  </BroadcastGroup>
</BroadcastGroups>
```

## 常用命令

### 查看广播组状态
```
/nametag broadcasts
```

### 重新加载配置
```
/nametag reload
```

## 使用示例

### 示例 1: 仅击杀者可见的死亡消息

```xml
<DeathMessage>
  <Enabled>true</Enabled>
  <Visibility>KillerOnly</Visibility>
  <Format>{killer} 击杀了 {victim}</Format>
</DeathMessage>
```

效果：只有击杀者能看到死亡消息。

### 示例 2: 多个独立的广播组

```xml
<BroadcastGroups>
  <!-- 服务器规则组：每2分钟轮换一次 -->
  <BroadcastGroup name="server_rules" enabled="true">
    <RotationInterval>120</RotationInterval>
    <Messages>
      <Message>欢迎来到服务器！</Message>
      <Message>请遵守服务器规则。</Message>
    </Messages>
  </BroadcastGroup>

  <!-- VIP公告组：每3分钟轮换一次 -->
  <BroadcastGroup name="vip_announcements" enabled="true">
    <RotationInterval>180</RotationInterval>
    <Messages>
      <Message>VIP玩家每日登录获得奖励！</Message>
      <Message>VIP玩家可使用特殊前缀。</Message>
    </Messages>
  </BroadcastGroup>

  <!-- 禁用的广播组 -->
  <BroadcastGroup name="death_tips" enabled="false">
    <RotationInterval>300</RotationInterval>
    <Messages>
      <Message>提示：死亡后可以使用 /respawn 快速复活</Message>
    </Messages>
  </BroadcastGroup>
</BroadcastGroups>
```

效果：
- `server_rules` 组每 2 分钟发送一条消息
- `vip_announcements` 组每 3 分钟发送一条消息
- `death_tips` 组不会发送消息（因为 `enabled="false"`）

### 示例 3: 自定义死亡消息格式

```xml
<DeathMessage>
  <Enabled>true</Enabled>
  <Visibility>All</Visibility>
  <Format><color=#FF0000>💥</color> {killer} 击杀了 {victim}</Format>
  <SelfKillFormat><color=#FFFF00>💀</color> {victim} 自杀了</SelfKillFormat>
  <ZombieFormat><color=#00FF00>🧟</color> {victim} 被僵尸击杀</ZombieFormat>
</DeathMessage>
```

## 调试模式

在配置文件中启用调试模式：

```xml
<DebugMode>true</DebugMode>
```

调试模式会输出详细的日志：
- 广播组启动/停止
- 消息发送
- 死亡消息处理
- 定时器触发

## 性能建议

### 广播组数量
- 建议 ≤ 20 个广播组
- 过多的广播组会增加定时器开销

### 轮换间隔
- 建议 ≥ 10 秒
- 过短的间隔会导致频繁触发

### 消息长度
- 建议 ≤ 200 字符
- 过长的消息会增加网络开销

### 消息数量
- 建议每组 ≤ 20 条消息
- 过多的消息会增加内存占用

## 故障排除

### 问题：广播组没有发送消息

**解决方案：**
1. 检查 `enabled` 是否为 `true`
2. 检查 `RotationInterval` 是否 > 0
3. 检查 `Messages` 是否有内容
4. 使用 `/nametag broadcasts` 查看状态
5. 启用 `DebugMode` 查看详细日志

### 问题：死亡消息没有发送

**解决方案：**
1. 检查 `DeathMessage.Enabled` 是否为 `true`
2. 检查 `DeathMessage.Visibility` 设置是否正确
3. 检查玩家是否有对应的权限组
4. 启用 `DebugMode` 查看详细日志

### 问题：配置重载后没有生效

**解决方案：**
1. 检查配置文件语法是否正确
2. 检查控制台是否有错误日志
3. 使用 `/nametag reload` 重新加载
4. 重启服务器

### 问题：编译错误

**解决方案：**
1. 确保安装了 .NET SDK
2. 检查项目依赖是否完整
3. 清理并重新编译：
   ```bash
   dotnet clean
   dotnet build NoNameTag.slnx -p:Configuration=Debug
   ```

## 下一步

- 查看 `BROADCAST_SYSTEM.md` 了解详细文档
- 查看 `IMPLEMENTATION_REPORT.md` 了解实现细节
- 查看 `NoNameTagConfiguration.example.xml` 查看完整配置示例

## 获取帮助

如果遇到问题：
1. 检查控制台日志
2. 启用 `DebugMode` 获取详细日志
3. 查看文档中的故障排除部分
4. 检查配置文件语法

---

**版本**：1.0.0
**更新日期**：2026-02-04
