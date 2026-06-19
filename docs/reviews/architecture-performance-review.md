# NoNameTag 架构与性能审查报告

审查范围：当前 `main` 分支完整源码、配置样例、README、Release workflow、最近加入的性能契约测试。  
审查方式：静态阅读 + 调用链追踪 + 配置/文档对照 + 可运行的 Python 性能契约测试。  
未做事项：没有修改业务代码；没有本地 C# 编译（当前机器无 `dotnet/msbuild`）。GitHub Actions 已能构建 release。

## 结论摘要

当前最高频路径（聊天、伤害）在最近优化后已经明显改善，短期没有明显性能 blocker。真正值得优先处理的是：

1. **死亡消息定向发送疑似参数错误**：`KillerOnly/VictimOnly` 可能不按预期只发给目标玩家。
2. **文档/配置/实现漂移严重**：头顶名称、AvatarSettings、旧配置根节点、README 架构描述都有墓碑内容。
3. **`NoNameTagPlugin` 过胖**：事件编排、聊天重写、伤害归因辅助、统计刷新混在主插件入口里。
4. **死亡归因逻辑重复**：统计路径和死亡消息路径各自解析 killer，长期会产生不一致。
5. **Stats 缓存无上限**：短期可接受，长期服务器会积累历史玩家内存缓存。

建议路线：先修 P0 正确性与配置漂移，再做架构瘦身，最后做低收益性能整理。

---

## 当前架构速写

```text
Rocket/Unturned events
  -> NoNameTagPlugin
     -> PermissionService
     -> NameTagManager
     -> PlayerStatsService / LiteDB
     -> DamageAttributionService
     -> BroadcastService
        -> DeathMessageService
        -> BroadcastRotationService
        -> WelcomeMessageService
```

关键运行流：

- 玩家连接：预加载 stats，解析权限组，缓存 formatted name，广播欢迎消息。
- 聊天：取消原聊天，读取 cached formatted name，按聊天模式重新发送。
- 伤害：记录最近攻击者、武器和距离，用于后续归因。
- 死亡：解析 killer，更新 stats，刷新显示名缓存，生成死亡消息，清理归因。
- 公告：Timer 触发，主线程队列发送全局消息。

---

## P0：需要优先确认/修复

### P0-1 死亡消息私聊发送疑似参数传反

位置：`Services/DeathMessageService.cs`

```csharp
private void SendToPlayer(UnturnedPlayer player, string message)
{
    if (player == null) return;
    var steamPlayer = BroadcastHelper.GetSteamPlayer(player.CSteamID);
    if (steamPlayer != null)
        ChatManager.serverSendMessage(message, Color.white, steamPlayer, null, EChatMode.SAY, null, true);
}
```

项目其他地方的调用模式显示：

- 给指定玩家发送：`ChatManager.serverSendMessage(message, ..., null, player.SteamPlayer(), ...)`
- 以某玩家为发送者发给某接收者：`ChatManager.serverSendMessage(message, ..., sender, recipient, ...)`

因此这里把 `steamPlayer` 放在第三个参数，很可能是在设置“发送者”，而不是“接收者”。这会影响：

- `DeathMessageVisibility.KillerOnly`
- `DeathMessageVisibility.VictimOnly`
- `DeathMessageVisibility.KillerAndVictimOnly`

建议：先用服务器实测确认 API 语义；若确认，改为：

```csharp
ChatManager.serverSendMessage(message, Color.white, null, steamPlayer, EChatMode.SAY, null, true);
```

并加一个源码契约测试或最小集成验证。

---

## P1：架构与一致性问题

### P1-1 `NoNameTagPlugin` 职责过重

位置：`NoNameTagPlugin.cs`，约 580 行。

当前它承担：

- 生命周期管理
- 事件注册
- 玩家连接/断开处理
- 聊天重写
- 聊天模式分发
- 聊天输入清洗
- 伤害归因辅助
- 死亡统计刷新
- killer 解析
- weapon/distance fallback

建议拆分为：

- `ChatMessageService`：聊天格式化、过滤、按范围发送。
- `PlayerLifecycleService`：连接/断开处理。
- `DeathEventService`：死亡事件入口，统一组装死亡上下文。
- `DamageEventRecorder`：从 `DamagePlayerParameters` 提取归因信息。

收益：减少入口类变更频率，降低回归范围，提高可测试性。

### P1-2 死亡归因解析重复

位置：

- `NoNameTagPlugin.ResolveKillerSteamId`
- `DeathMessageService.ResolveDeathAttribution`

当前统计路径先解析一次 killer，死亡消息路径又解析一次 attribution。两处都处理：

- direct instigator
- bleeding attribution
- recent attribution
- self-kill 排除

风险：以后改一处漏一处，导致“统计认为 A 击杀，死亡消息显示环境死亡”。

建议引入一个只读上下文：

```csharp
DeathAttributionContext
- VictimSteamId
- KillerSteamId
- KillerPlayer
- Cause
- Limb
- WeaponName
- DistanceMeters
```

由一个服务解析一次，再传给 stats 和 death message。

### P1-3 “NameTag” 术语已经误导

事实：用户已确认头顶名称不可实现；代码也没有真正修改头顶 name tag，只做聊天显示名、死亡消息显示名和缓存。

受影响位置：

- `NameTagManager`
- `INameTagManager`
- README “名称标签/头顶名称标签”
- 配置项 `ApplyToNameTags`
- `Constants.DefaultApplyToNameTags`
- README 的 `NameTagDisplayService`

建议：短期文档改口为 **Formatted Name / Chat Display Name**；长期考虑把 `NameTagManager` 重命名为 `FormattedNameManager` 或 `DisplayNameManager`。

这属于需要讨论的领域词汇。如果后续确认术语，建议创建 `CONTEXT.md` 记录。

---

## P1：配置与文档漂移

### P1-4 三份配置文件不一致

文件：

- `NoNameTagConfiguration.cs`
- `NoNameTagConfiguration.xml`
- `NoNameTagConfiguration.example.xml`
- `NoNameTag.configuration.xml`

问题：

- `NoNameTag.configuration.xml` 根节点是 `<Configuration>`，而当前配置类默认序列化根更像 `<NoNameTagConfiguration>`。
- `NoNameTagConfiguration.xml` 包含 `<AvatarSettings />`，但配置类没有 `AvatarSettings`。
- `NoNameTag.configuration.xml` 和 example 都含 `<ApplyToNameTags>`，配置类没有该属性。
- example 中广播写法使用 `<Message avatar="...">文本</Message>`，但模型是 `BroadcastMessage.Message` 元素 + `Avatar` 元素；该示例可能无法按预期反序列化。

建议：

1. 明确唯一权威配置样例，只保留一个可用模板。
2. 删除或标注旧配置文件。
3. 用当前模型重新生成 example。
4. 移除不可实现配置：`ApplyToNameTags`、`AvatarSettings`，除非准备实现。

### P1-5 README 明显过期

README 仍提到：

- `ApplyToNameTags`
- `NameTagDisplayService`
- 头顶名称标签
- `NoNameTag 1.0.0 has been loaded!`

这些和当前实现/版本不一致。建议把 README 改成：

- 聊天显示名插件
- 死亡消息插件
- 公告/欢迎消息插件
- stats 插件

不要继续承诺头顶名称。

---

## P2：性能审查

### P2-1 聊天路径当前可接受

最近优化后聊天路径为：

```text
OnPlayerChatted
  -> BuildFormattedChatMessage
     -> NameTagManager.GetFormattedPlayerName
     -> SanitizeChatMessage
  -> 按 chatMode 发送
```

优点：

- 不再每条聊天读 stats/LiteDB。
- 不再每个 group client 包装 `UnturnedPlayer`。
- `player.SteamPlayer()` 每条消息只调用一次。
- 输入过滤单次扫描。

剩余风险：

- cache miss 时不显示 stats suffix，这是有意的性能取舍。
- 本路径仍依赖 `Provider.clients` 线性扫描来处理 LOCAL/GROUP；Unturned 服务器人数通常有限，可接受。

### P2-2 伤害路径当前可接受

当前每次可归因伤害：

- 只解析一次 attacker player。
- 记录 weapon/distance。
- 保存 recent attribution。

可进一步优化但不急：

- 如果死亡消息配置不包含 `{weapon}` / `{distance}`，可跳过 weapon/distance 计算。
- 对纯 stats 场景可以只记录 attacker/victim/cause。

但这会增加配置感知复杂度，收益未必值得。

### P2-3 公告路径不是性能热点

`BroadcastRotationService` 低频 Timer 触发，正常 `120s/300s` 配置无压力。

可优化点：

- `OnTimerElapsed` 的 lock 范围偏大：现在 lock 内做变量替换、样式包装、enqueue、debug log。
- `DelaySeconds` 字段被校验但未使用。

建议：低优先级。除非用户把公告组配置到很高频，否则不需要动。

### P2-4 LiteDB stats 缓存无上限

位置：`PlayerStatsService._cachedStats`

风险：

- 每个见过的玩家记录会缓存到插件卸载。
- 长期运行大服可能积累历史玩家对象。

已决策：本轮纳入修复。新增 `ReleasePlayer(ulong steamId)`，在玩家断开时释放 `_cachedStats` 中的玩家记录；如果玩家有 dirty stats，先 flush 再释放。断开不清理 current killstreak，因为断线不等于死亡，重连是否保留连杀应维持当前语义。

注意：不要让聊天路径重新触发 DB miss。

---

## P2：冗余与墓碑代码

### P2-5 未使用或半使用成员

候选：

- `NameTagManager._config`：保存但未使用。
- `DamageAttributionService.TrackDamage`：接口暴露，但当前事件入口直接调用 `RecordAttributedHit`。
- `DamageAttributionService.RecordBleedCapableHit`、`TryResolveBleedKiller`：可能是历史 API。
- `PlayerStatsService.RecordDeath`、`ClearSession`：接口暴露但当前命令/入口未用。
- `BroadcastService.StopAllBroadcasts`、`ReloadBroadcasts`、`SendBroadcast`：接口存在，命令只读 status，没有手动触发广播。
- `NameFormatter.FormatNameWithAvatar`、`FormatPlainName`、`ParseColor`、`FormatBroadcastMessageWithAvatar`：当前主路径未使用。
- `Constants.DefaultApplyToNameTags`：墓碑常量。

建议：先不要立刻删。下一轮可以按“public API 是否需要兼容”分组：

- 内部 private/internal 未用：直接删。
- public interface 未用：先确认是否是插件外部依赖。
- 文档中承诺但代码没用：修文档或补功能。

### P2-6 `DelaySeconds` 配置字段未实现

模型和配置说明都存在 `DelaySeconds`，`ConfigValidator` 也校验非负，但 `BroadcastRotationService.OnTimerElapsed` 完全不使用。

建议二选一：

- 删除字段和文档，简化广播模型。
- 实现 per-message delay，但要注意 Timer 重入和主线程调度。

已决策：阶段 1 彻底删除 `DelaySeconds`，包括模型字段、构造函数参数、校验逻辑、配置模板和 README 描述；不保留兼容字段。

---

## P2：安全/鲁棒性

### P2-7 富文本注入防护只覆盖聊天正文

聊天正文会过滤 `< > { }`，但玩家名进入：

- 聊天 formatted name
- death message formatted name
- welcome message `{player}`
- leave message `{player}`

如果 Unturned DisplayName 可包含 rich text 控制字符，可能造成格式注入。风险取决于游戏端是否限制名称。

已决策：阶段 1 纳入富文本注入防护。抽统一 `RichTextSanitizer`；玩家聊天正文、玩家显示名、welcome/leave 的 `{player}` 替换值、death message 的 victim/killer 名字都视为 untrusted player text。配置文本视为 trusted message text，保留管理员配置的 rich text。

### P2-8 配置颜色校验与 formatter 支持不一致

`NameFormatter` 支持 hex 和 Unity 颜色名；`ConfigValidator.ValidateDisplayEffect` 只接受 hex。当前 config 中 welcome color 用 `white`，但 welcome color 没被 validator 校验。

已决策：统一允许 hex color 与 Unity color name。阶段 1 需要让 validator、README、配置模板与 `NameFormatter` 行为一致。

---

## P3：测试与工程化

### P3-1 当前测试是源码契约测试，不是行为测试

`tests/performance_contract_tests.py` 能防止热点路径回退，但它是字符串匹配，不证明运行时行为。

建议下一步：

已决策：阶段 1 采用混合测试策略。保留 Python 契约测试覆盖 Rocket/Unity 难测调用形状；新增 C# 测试项目只测试纯逻辑，不强行模拟 Unity/Rocket 运行时。

1. 保留 Python 契约测试作为“无 Unity 环境的低成本 guard”。
2. 抽纯函数后增加真正单元测试：
   - `SanitizeChatMessage`
   - `NameFormatter`
   - `BroadcastHelper.ReplaceVariables` 的纯替换部分
   - death format 选择逻辑
3. 如果可行，建一个独立测试项目，不引用 Unity/Rocket 重依赖。

### P3-2 CI 没跑性能契约测试

GitHub Action release 只 restore/build/release，不跑：

```bash
python3 tests/performance_contract_tests.py
```

已决策：阶段 1 新增普通 CI workflow，并让 release workflow 发布前也运行测试。CI 在 push/main 和 pull_request 上执行 Python 契约测试、C# 单元测试和 Release build；release workflow 在发布前重复关键测试，保证发布产物来自已验证代码。

---

## 建议优化路线图

### 第一批：低风险修正

1. 修 `DeathMessageService.SendToPlayer` 参数语义并验证。
2. 清理 README 中头顶名称/`NameTagDisplayService`/旧版本描述。
3. 明确唯一配置模板，并在阶段 1 直接删除旧 `NoNameTag.configuration.xml`。
4. 移除 `ApplyToNameTags` / `AvatarSettings` 墓碑配置。

### 第二批：架构收敛

已决策：本轮 PRD 纳入架构收敛，但分阶段实施。阶段 1 处理正确性、配置漂移和 stats cache；阶段 2 再抽 `ChatMessageService`，并通过 `DeathAttributionResolver` 统一生成 `DeathAttributionContext`。`ChatMessageService` 必须包含可测试的发送 seam（如 `IChatMessageSender`），使 LOCAL/GROUP/GLOBAL 路由可在无 Rocket/Unturned 运行时下测试。每个阶段都必须走 TDD + Review。Review 必须使用 subagent 分别做 Spec Review 与 Standards Review，并把结果作为阶段完成门槛。

1. 抽 `ChatMessageService`。
2. 抽 `DeathAttributionResolver` 和统一 `DeathAttributionContext`。
3. 把 death stats 和 death message 都消费同一个 context。
4. 把 `NoNameTagPlugin` 降级成生命周期和事件 wiring。

### 第三批：缓存与测试

1. PlayerStatsService 区分 online cache / persisted cache。
2. CI 跑契约测试。
3. 抽纯函数后补真实单元测试。
4. 广播 Timer lock 缩小、处理或删除 `DelaySeconds`。

---

## 建议暂时不要做的事

- 不要为了公告性能大改。公告不是热点。
- 不要立刻把所有 `ConcurrentDictionary` 换成 `Dictionary`；Timer/后台 flush 存在线程边界，收益不大但风险高。
- 不要一口气重命名整个项目/命名空间。先修文档和领域词，再决定是否做破坏性重命名。
- 不要把所有服务都抽接口。当前接口已有一些未使用方法，继续抽象会加重噪音。

---

## 需要讨论的术语

如果后续要补 `CONTEXT.md`，建议先确认这些词：

- **Formatted Name**：带权限效果和可选 stats 后缀的玩家显示名。
- **Display Effect**：权限组对应的前缀、后缀、颜色、字号配置。
- **Permission Group**：通过 Rocket 权限匹配出的显示效果分组。
- **Death Attribution**：把一次死亡归因到 killer/weapon/distance 的过程。
- **Broadcast Group**：按固定间隔轮播的一组公告消息。
- **Player Stats**：玩家击杀、死亡、当前连杀数据。

建议避免继续使用“NameTag”指代头顶名称，因为当前项目实际做的是聊天/消息里的 formatted name。
