# Punchy — Server-Side Bare-Handed Punching for A Township Tale

**Punchy** adds authoritative, purely server-side bare-handed punching to *A Township Tale* servers running MelonLoader (`v0.7.3+` / Mono). Connecting players require **zero client-side mods**.

---

## Key Features

- **100% Server-Side Authority:** All hand velocity sampling, empty-hand gating, fist pose evaluation, cooldown checks, self-hit exclusion, and damage application run strictly on the server (`NetworkSceneManager.IsServer`).
- **Native ATT Damage Integration:** Routes hits through ATT's native `ImpactDetector.OnImpactHit` / `HealthObject.DetectImpact` pipeline as `ImpactType.Blunt`:
  - Default **0.10 HP** base damage (20 punches to defeat a default `2.0` HP player), optionally scaling up to **0.15 HP** with swing speed.
  - Automatically respects server PvP rules (`IsPVPEnabled`, `PVPMultiplier`, `PVPCrippleMultiplier`), armor mitigation, `DamageX`, death attribution (`KilledData.killerPlayer`), AI creature aggro (`Agent.HealthChanged`), player hurt flash (`HurtVignette`), and breakable static objects.
- **Anti-Exploit & False-Positive Guards:**
  - **Empty-Hand Lock:** Swinging while holding or interacting with any weapon, tool, torch, pouch, or world interactable never triggers fist damage (plus a `0.15s` post-release grace window).
  - **Closed-Fist Verification:** Reads ATT's replicated `HandGestureController` state across OpenXR/Quest grip axis, explicit gesture bitmasks, and Valve Index finger curls (with a `0.12s` pose-lag grace window and graceful fallback).
  - **Windowed Velocity & Teleport Guard:** Prevents `NetworkTransform` packet-clumping spikes and rejects teleport/respawn jumps (`> 1.5m` per tick).
  - **Independent Per-Hand Cooldown:** `0.40s` default cooldown per hand prevents controller-wiggle rapid-fire while still allowing natural left-right one-two combos.
  - **Self-Hit Exclusion:** Ignores the punching player's own character hierarchy, `HealthObject`, holstered/belt-docked gear, and items held in the opposite hand.

---

## Server Installation

1. Ensure your *A Township Tale* server (Mono PCVR build) has **MelonLoader** (`v0.7.3`) installed.
2. Copy `Punchy.dll` into the server's `Mods\` folder:
   ```text
   <A Township Tale Server>\Mods\Punchy.dll
   ```
3. Start the server once to generate the `[Punchy]` section in `UserData\MelonPreferences.cfg`.
4. Clients connect normally with an unmodded game client.

---

## Configuration (`UserData/MelonPreferences.cfg`)

All settings live under `[Punchy]` and reload cleanly via MelonLoader preferences:

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

---

## Building & Running Unit Tests

From `Source\Punchy\`:

```powershell
# Build Punchy.Core, Punchy.dll (net472), and Punchy.Tests (net8.0)
dotnet build Punchy.sln -c Release

# Execute automated unit tests
dotnet test Punchy.sln -c Release --verbosity normal
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for the complete verified ATT assembly breakdown, Harmony hook targets, and network replication analysis.
