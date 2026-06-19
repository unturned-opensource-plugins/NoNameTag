# NoNameTag

NoNameTag is a Rocket.Unturned plugin context for formatting player names in server messages, publishing server messages, and tracking player combat statistics. The project name is historical; the feature language should avoid implying that the plugin changes in-world overhead name tags.

## Language

**Formatted Name**:
A player's display name as shown inside plugin-controlled messages, optionally decorated with a display effect and player stats.
_Avoid_: Name tag, overhead name, head name

**Display Effect**:
A configured visual treatment for a formatted name, such as prefix, suffix, color, and font size.
_Avoid_: Rank tag, name tag effect

**Permission Group**:
A configured group selected by the player's Rocket permissions that determines which display effect applies.
_Avoid_: Role, rank, class

**Player Stats**:
A player's tracked combat totals and current killstreak.
_Avoid_: Score, profile, leaderboard entry

**Death Attribution**:
The resolved relationship between a player death and the player, weapon, and distance considered responsible for that death.
_Avoid_: Kill credit, damage owner

**Broadcast Group**:
A configured set of server messages rotated on a schedule.
_Avoid_: Announcement list, message queue

**Color Value**:
A configured text color accepted by plugin-controlled messages, expressed either as a hex color or a Unity color name.
_Avoid_: CSS color, arbitrary rich text

**Trusted Message Text**:
Message text supplied by server configuration and allowed to contain supported rich text markup.
_Avoid_: Player input, display name

**Untrusted Player Text**:
Text supplied by or derived from a player, such as chat message bodies and player display names; it must not be allowed to inject rich text markup.
_Avoid_: Config text, server-authored text
