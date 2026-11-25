# Punchy — Server-Side Bare-Handed Punching for A Township Tale

**Punchy** adds authoritative, purely server-side bare-handed punching to *A Township Tale* servers running MelonLoader (`v0.7.3+` / Mono). Connecting players require **zero client-side mods**.

---

## Key Features

- **100% Server-Side Authority:** All hand velocity sampling, empty-hand gating, fist pose evaluation, cooldown checks, self-hit exclusion, ragdoll triggers, and damage application run strictly on the server (`NetworkSceneManager.IsServer`).
- **Native ATT Damage Integration:** Routes hits through ATT's native `ImpactDetector.OnImpactHit` / `HealthObject.DetectImpact` pipeline as `ImpactType.Blunt`:
  - Default **0.10 HP** base damage (20 punches to defeat a default `2.0` HP player), optionally scaling up to **0.15 HP** with swing speed.
  - Automatically respects server PvP rules (`IsPVPEnabled`, `PVPMultiplier`, `PVPCrippleMultiplier`), armor mitigation, `DamageX`, death attribution (`KilledData.killerPlayer`), AI creature aggro (`Agent.HealthChanged`), player hurt flash (`HurtVignette`), combat dummies, and breakable static objects.
- **Native Hit VFX/SFX Replication & Kinetic Impact Feedback:**
  - Broadcasts native creature/prop hit effects (such as the orange Spriggull hit burst and blunt sound) to unmodded clients via ATT's `ImpactDetectorSync` pipeline.
  - Layers pooled kinetic shockwave, spark, and dust impact particles plus `FistBump` / `WoodThump` audio on non-headless hosts.
- **Realistic `RagdollLib` Physics Integration (v1.2.2+):**
  - Integrates softly with `RagdollLib.dll` (`[assembly: MelonOptionalDependencies("RagdollLib")]`) — runs **authoritatively on the server** for AI/root physics and **visually on modded clients** (because ATT does not replicate procedural bone transforms across the network; unmodded clients still take/deal punch damage normally without ragdoll physics):
    - **Small Creatures (`Spriggull`, `Babu`, `Schmeechee`, `FireBug`, `Dragonfly`, `Wyrm`):** Any normal punch (`>= 2.2 m/s`) ragdolls them with realistic grounded tumble impulse (`85`–`135 N*s`, `2.6s` recovery).
    - **Players:** Require a heavy **haymaker** (`>= 5.0 m/s`) to ragdoll (`105`–`160 N*s`, `2.8s` recovery); everyday light jabs will damage without ragdolling.
    - **Large Creatures (`Gotera`, `GreaterGotera`, `Turabada`, `Phantom`):** Require a heavy haymaker (`>= 5.5 m/s`, `120`–`180 N*s`, `2.5s` recovery).
    - **Anti-Moon Physics & Torque Targeting:** Targets the entity `PrefabRoot` at an upper-torso torque height (`>= +0.55–0.60m`) so limbs and spine articulate naturally, clamps vertical punch components (`<= 0.50`), and caps impulse magnitudes (`<= 350 N*s`).
- **Anti-Exploit & False-Positive Guards:**
  - **Empty-Hand Lock:** Swinging while holding or interacting with any weapon, tool, torch, pouch, or world interactable never triggers fist damage (plus a `0.15s` post-release grace window).
  - **Closed-Fist Verification:** Reads ATT's replicated `HandGestureController` state across OpenXR/Quest grip axis, explicit gesture bitmasks, and Valve Index finger curls (with a `0.12s` pose-lag grace window and graceful fallback).
  - **Windowed Velocity & Teleport Guard:** Prevents `NetworkTransform` packet-clumping spikes and rejects teleport/respawn jumps (`> 1.5m` per tick).
  - **Independent Per-Hand Cooldown:** `0.40s` default cooldown per hand prevents controller-wiggle rapid-fire while still allowing natural left-right one-two combos.
  - **Self-Hit Exclusion:** Ignores the punching player's own character hierarchy, `HealthObject`, holstered/belt-docked gear, and items held in the opposite hand.

---

## Server & Client Installation

1. Ensure your *A Township Tale* server (Mono PCVR build) has **MelonLoader** (`v0.7.3+`) installed.
2. Copy `Punchy.dll` into the server's `Mods\` folder:
   ```text
   <A Township Tale Server>\Mods\Punchy.dll
   ```
   > Players can connect with an **unmodded client** and bare-handed combat, damage, PvP, death attribution, and native hit VFX/SFX still work 100% server-side.
3. *(Optional, Recommended for Ragdoll Knockdowns)* Install **both `RagdollLib.dll` and `Punchy.dll`** into `Mods\` on the **server AND on players' clients**:
   ```text
   <A Township Tale>\Mods\RagdollLib.dll
   <A Township Tale>\Mods\Punchy.dll
   ```
   > **Why both server & client?** *A Township Tale* only syncs entity root transforms over the network, not individual skeletal ragdoll bones. Running `Punchy.dll` + `RagdollLib.dll` on the client suspends `NetworkRigidbody` & `Animator` locally so you see the full floppy ragdoll rig tumble instead of stiffly sliding! On clients, `Punchy` runs **strictly visual ragdoll triggers only** and never modifies health or deals damage.
   >
   > **Player Haymaker Ragdolls:** To allow haymaker punches to ragdoll `PlayerCharacter` rigs, also set `AllowPlayerRagdoll = true` under `[RagdollLib]` in `UserData\MelonPreferences.cfg`.
4. Start the server/client once to generate `[Punchy]` in `UserData\MelonPreferences.cfg`.

---

## Configuration (`UserData/MelonPreferences.cfg`)

All settings live under `[Punchy]` and reload cleanly via MelonLoader preferences:

### Core Combat & Feedback

| Option | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `true` | Enables or disables bare-handed punching server-wide. |
| `BasePunchDamage` | `float` | `0.1` | Base health damage at `MinPunchSpeed` (`0.1` = 20 hits vs. 2.0 HP). |
| `MaxPunchDamage` | `float` | `0.15` | Maximum scaled damage at high punch speed (`6.0 m/s`). |
| `EnableVelocityScaling` | `bool` | `true` | Linearly scales damage from `BasePunchDamage` to `MaxPunchDamage` with speed. |
| `MinPunchSpeed` | `float` | `2.2` | Minimum world-space hand speed (`m/s`) required to register a punch. |
| `PunchCooldownSeconds` | `float` | `0.4` | Minimum seconds between hits per individual hand (`Left` / `Right`). |
| `RequireClosedFist` | `bool` | `true` | Requires a closed fist from replicated gesture/grip state (falls back cleanly if unavailable). |
| `AllowPvPPunching` | `bool` | `true` | Permits punching other players (still obeys native `ServerSettings.IsPVPEnabled`). |
| `EnablePunchVfx` | `bool` | `true` | Spawns layered shockwave ring, kinetic spark, and dust puff VFX on punch impacts (non-headless builds). |
| `PunchVfxScale` | `float` | `1.0` | Scale multiplier (`0.1` to `3.0`) for kinetic punch impact VFX. |
| `EnableSurfaceImpactVfx` | `bool` | `true` | Plays subtle spark/dust VFX and thud SFX when punching solid world surfaces. |
| `DebugLogging` | `bool` | `false` | Logs throttled server punch hit and swing rejection diagnostics to the MelonLoader console. |

### `RagdollLib` Physics Settings (`[Punchy]`)

| Option | Type | Default | Description |
|---|---|---|---|
| `EnableRagdoll` | `bool` | `true` | Master toggle for triggering `RagdollLib` physics on punches (no-op if `RagdollLib.dll` is not installed). |
| `RagdollPlayersOnHaymaker` | `bool` | `true` | Enables ragdolling players on heavy haymakers (also requires `[RagdollLib] AllowPlayerRagdoll = true`). |
| `PlayerRagdollMinSpeed` | `float` | `5.0` | Minimum haymaker hand speed (`m/s`) required to ragdoll a player. |
| `PlayerRagdollBaseImpulse` | `float` | `105.0` | Base impulse (`N*s`) applied at `PlayerRagdollMinSpeed` (clamped `<= 350`). |
| `PlayerRagdollMaxImpulse` | `float` | `160.0` | Maximum impulse (`N*s`) for fastest player haymakers (clamped `<= 350`). |
| `PlayerRagdollDuration` | `float` | `2.8` | Seconds before a haymaker-ragdolled player recovers. |
| `RagdollSmallCreatures` | `bool` | `true` | Enables ragdolling small creatures (`Spriggull`, `Babu`, `Schmeechee`, `FireBug`, `Dragonfly`, `Wyrm`). |
| `SmallCreatureRagdollMinSpeed` | `float` | `2.2` | Minimum punch speed (`m/s`) to ragdoll small creatures (`2.2` = any normal punch). |
| `SmallCreatureRagdollBaseImpulse` | `float` | `85.0` | Base impulse (`N*s`) for normal punches on Spriggulls and small creatures. |
| `SmallCreatureRagdollMaxImpulse` | `float` | `135.0` | Maximum capped impulse (`N*s`) for heavy punches on small creatures. |
| `SmallCreatureRagdollDuration` | `float` | `2.6` | Seconds before a ragdolled small creature recovers. |
| `RagdollLargeCreatures` | `bool` | `true` | Enables ragdolling large creatures (`Gotera`, `GreaterGotera`, `Turabada`, `Phantom`). |
| `LargeCreatureRagdollMinSpeed` | `float` | `5.5` | Minimum haymaker speed (`m/s`) required to ragdoll large creatures. |
| `LargeCreatureRagdollBaseImpulse` | `float` | `120.0` | Base impulse (`N*s`) applied on large-creature haymakers. |
| `LargeCreatureRagdollMaxImpulse` | `float` | `180.0` | Maximum capped impulse (`N*s`) on large-creature haymakers. |
| `LargeCreatureRagdollDuration` | `float` | `2.5` | Seconds before a ragdolled large creature recovers. |
| `RagdollUpwardBoost` | `float` | `0.22` | Subtle upward lift ratio (`0.0` to `0.50`) for grounded full-rig knockdowns. |

---

## Building & Running Unit Tests

From `Source\Punchy\`:

```powershell
# Build Punchy.Core, Punchy.dll (net472), and Punchy.Tests (net8.0)
dotnet build Punchy.sln -c Release

# Execute automated unit tests
dotnet test Punchy.sln -c Release --verbosity normal
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for the complete verified ATT assembly breakdown, `RagdollLib` integration details, Harmony hook targets, and network replication analysis.


<p>
    <img src="https://img.shields.io/github/downloads/JonathanDonkerlot/Punchy-att/total?style=for-the-badge&logo=github&color=7fff6e">
</p>
