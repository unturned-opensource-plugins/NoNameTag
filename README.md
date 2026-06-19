# NoNameTag

NoNameTag 是一个 Rocket.Unturned 服务器插件。项目名保留历史兼容，但当前功能重点是 **Formatted Name（格式化玩家名）**、聊天格式化、死亡消息、欢迎/离开消息、公告轮播和玩家击杀统计；它仅影响插件发送的消息，不修改游戏本体玩家名称显示。

## 功能

- **格式化玩家名**：按 Rocket 权限组为玩家名添加前缀、后缀、颜色、字号和可选统计后缀。
- **聊天格式化**：重写聊天消息中的玩家名，并保留 GLOBAL / LOCAL / GROUP 范围语义。
- **死亡消息**：支持多种死亡原因、武器、距离、爆头标签和可见性控制。
- **公告轮播**：按广播组定期发送服务器消息。
- **欢迎/离开消息**：玩家加入或离开时发送服务器配置消息。
- **玩家统计**：使用 LiteDB 记录总击杀、总死亡和当前连杀。
- **文本/网站命令**：通过配置添加 `/text <name>` 与 `/web <name>` 快捷命令。

## 安装

1. 从 Release 下载 `NoNameTag.dll`。
2. 将 `NoNameTag.dll` 复制到服务器的 `Rocket/Plugins` 文件夹。
3. 首次运行后编辑插件生成的配置，或参考 `NoNameTagConfiguration.example.xml`。
4. 重启服务器，或使用 Rocket 的重载命令。

`LiteDB` 已内嵌到 `NoNameTag.dll`，不需要额外复制 `LiteDB.dll`。

## 构建

```bash
dotnet restore
dotnet build --configuration Release
```

输出：

```text
bin/Release/net48/NoNameTag.dll
```

## 配置

权威模板：`NoNameTagConfiguration.example.xml`。

颜色值支持：

- Hex：`#FFFFFF` 或 `FFFFFF`
- Unity 颜色名：`white`, `red`, `yellow`, `green`, `blue`, `cyan`, `magenta`, `black`, `gray`, `grey`

服务器配置文本是 trusted message text，可以使用支持的 rich text。玩家聊天内容和玩家显示名会作为 untrusted player text 清洗，避免注入 rich text。

### 权限组示例

```xml
<PermissionGroup permission="nonametag.vip" priority="10">
  <DisplayEffect>
    <Prefix>[VIP] </Prefix>
    <PrefixColor>#FFD700</PrefixColor>
    <NameColor>green</NameColor>
    <Suffix></Suffix>
    <SuffixColor>#FFFFFF</SuffixColor>
  </DisplayEffect>
</PermissionGroup>
```

### 死亡消息可见性

```xml
<DeathMessage>
  <Enabled>true</Enabled>
  <DisplayMode>Chat</DisplayMode>
  <Visibility>All</Visibility>
  <Format>{killer} 击杀了 {victim}</Format>
</DeathMessage>
```

`Visibility` 可选：

- `All`
- `KillerOnly`
- `VictimOnly`
- `KillerAndVictimOnly`

### 公告组示例

```xml
<BroadcastGroup name="server_tips" enabled="true" displayMode="Chat">
  <RotationInterval>300</RotationInterval>
  <Messages>
    <Message>
      <Message>欢迎来到服务器！请遵守规则。</Message>
      <FontColor>white</FontColor>
      <Avatar>{server_icon}</Avatar>
      <AvatarPosition>Left</AvatarPosition>
    </Message>
  </Messages>
</BroadcastGroup>
```

广播组是固定间隔轮播；不支持单条消息延迟。

## 命令

管理员命令保留历史名称：

```text
/nametag reload
/nametag refresh [player]
/nametag check [player]
/nametag broadcasts
/nametag stats [player]
/nt ...
```

权限：

```text
nonametag.admin
```

玩家命令：

```text
/text <configured-name>
/web <configured-name>
```

## 数据

玩家统计默认保存到：

```text
Data/nonametag.litedb
```

记录：

- `CurrentKillstreak`
- `TotalKills`
- `TotalDeaths`

## 开发验证

Python 契约测试：

```bash
python3 tests/performance_contract_tests.py
```

C# 测试和构建由 GitHub Actions 在 Windows runner 上验证。

## 许可证

MIT。详见 `LICENSE`。
