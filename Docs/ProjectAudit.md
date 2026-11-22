# SOLEDAR — Project audit against the full blueprint

Date: 2026-11-19 · Editor: Unity 6.3 (6000.3.x) · Scope: read of every script, scene, prefab and
ScriptableObject in the repo, plus live play-mode checks where noted.

This audit answers the blueprint's "first task" (inspect the project before building more) and
sets the order for the next phases. It records what is actually in the project, not what is planned.

## 1. What exists and works

| System | State | Where |
| --- | --- | --- |
| Movement: run, variable jump, coyote time, jump buffer, wall slide/jump, dash (+ trail), double jump gated on `windOfAtlas` | Built, verified in earlier phases | `Player/PlayerMovement.cs`, `PlayerInputReader.cs` |
| Input: keyboard + gamepad (A/B/X/Y, Start), Input System actions asset | Built | `Assets/InputSystem_Actions.inputactions` |
| Combat: 3-hit Khoumya combo, up/down/dash attacks, hitboxes, i-frames, knockback, hit-stop, camera shake | Built | `Player/PlayerAttack.cs`, `Combat/*` |
| Health: 5 masks, damage flash, respawn, death pit | Built, verified | `Player/PlayerHealth.cs`, `Player/PlayerRespawn.cs` |
| Enemies: Dust Crawler, Medina Guard, Medina Warden, Lost Soul (ranged) with data assets and state AI | Built | `Enemies/*`, `ScriptableObjects/Enemies/*` |
| Boss: The Gatekeeper with phases, arena lock, boss bar, intro/death, reward | Built, verified | `Enemies/GatekeeperBoss.cs`, `World/BossArena.cs` |
| World: Sunken Medina, Silent Palace, doors, keys, benches (save/rest/cp), room discovery, scene transitions | Built, verified | `Scenes/*`, `World/*` |
| Persistence: versioned JSON save, health/position/keys/doors/abilities/bosses/rooms/items/dirhams/scene checkpoint, corrupt-file fallback | Built, verified | `Core/SaveManager.cs`, `Core/SaveApplier.cs` |
| Dialogue: data assets, typing, skip, speaker/body/hint hierarchy, NPC + one-item merchant chain | Built | `Dialogue/*` |
| UI: main menu (New Game/Continue), pause (Resume/Main Menu/Quit), masks, Echo Energy meter, currency, boss bar, prompts, dialogue, banners, ink fader | Built, verified | `UI/*` |
| Echo Energy + Lantern healing: energy from landing hits, hold-to-focus heal, bench refill, HUD meter | Built, verified 2026-11-22 | `Player/PlayerEnergy.cs`, `Player/PlayerHealing.cs`, `UI/EnergyUI.cs` |
| Camera follow + shake | Built (pre-Cinemachine) | `Combat/CameraShake.cs`, camera rig in scenes |

Placeholder art across the board (`Docs/AssetRegister.md`): tiles, characters and UI are original
temporary shapes, not production art.

## 2. Gaps against the blueprint

Ordered by what blocks the 15–30 minute vertical slice, not by ambition.

1. ~~No Echo Energy and no healing.~~ Done 2026-11-22: energy, focus healing, bench refill and HUD meter.
   Still open: Lantern upgrades (heal count, focus speed) once there is an inventory.
2. **No map.** `discoveredRooms` is persisted but nothing draws rooms, player position or markers.
3. **No inventory / charms / relics.** Dirhams exist and the merchant sells one ability; there is no
   item surface, no slots, no charm effects.
4. **No settings.** No volumes, resolution, fullscreen, screen-shake toggle, remapping.
5. **No audio.** No AudioSources, mixer, music or SFX anywhere in the project (`AssetRegister` notes this).
6. **Pause menu is four actions.** Blueprint asks for Inventory / Map / Abilities / Quests / Settings.
   Abilities page done 2026-11-22 (base kit + saved unlocks, names/descriptions, Back); Inventory, Map,
   Quests and Settings remain.
7. **Two gameplay regions, not six.** Sunken Medina and Silent Palace are built; Kasbah, Dunes,
   Cistern, Sanctum are not. The vertical slice only needs Medina, so this is intentional for now.
8. **No quest system, no save slots, no language/localisation, no Steam layer.** Expected later.
9. **No automated tests.** 0 test files; verification has been manual/MCP-driven per phase.
10. **Ability set is partial.** Dash and double jump exist; Shadow Passage, Lantern heal and
    Descent of Ash (ground slam) are missing. Ability gates exist (`Core/AbilityGate.cs`) for doors.

## 3. Recommended order for the next increments

Each is small enough to finish and verify in one pass, and each unblocks the next.

1. ~~**Pause: Abilities panel**~~ Done 2026-11-22: `Core/AbilityCatalog.cs`, `UI/AbilitiesPanel.cs`,
   pause button + page wired in both regions, HUD now sits under the pause overlay.
   Remaining pause submenus: Map, Inventory, Quests, Settings.
2. **Map: single-region room map** — rooms as simple authored rects per scene, fog-of-war driven by
   `discoveredRooms`, player marker, bench markers. Same UI language as the pause menu.
3. **Settings: audio/video basics** — master/music/SFX volume, fullscreen, screen-shake toggle,
   persisted in `SaveManager`.
4. **Audio pass** — music per region and combat/movement SFX once licensed or original assets exist.
5. **Charms/inventory** — item definitions, slots, effects, merchant UI.

## 4. Technical notes for whoever continues

- Legacy `UnityEngine.UI.Text` only; no TMP in this project yet. Type hierarchy comes from
  `UI/SoledarUITheme.cs` tokens (display 84 / title 40 / heading 22 / body 21 / label 20 / small 15).
- The MCP `RunCommand` reports only its own assembly; after editing project scripts, check
  `Unity_GetConsoleLogs` for real compile errors before trusting "command executed".
- Play-mode frames freeze when the editor is unfocused: set `Application.runInBackground = true`
  during a test and revert `PlayerSettings.runInBackground` afterwards.
- Game view is 568×473; use a temporary camera + RenderTexture for readable HUD captures, and read
  colors numerically — the offscreen capture can render darker than the real overlay canvas.
- No commits made; working tree holds all changes. `Assets/_Recovery/`, `.agents/` and
  `ProjectSettings/Packages/com.unity.ai.assistant/` are pre-existing untracked files.
