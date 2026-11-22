# SOLEDAR — Development Log

*Project: SOLEDAR — Echoes of a Forgotten Kingdom (Unity 6 LTS 6000.3.4f1, 2D Metroidvania)*

---

## Phase 1 — Foundation (code complete, awaiting user verification)

### Project audit (pre-Phase 1 findings)

**Working prototype found in `TestScene.unity`:**
- Player: Rigidbody2D (gravity 3, rotation frozen) + BoxCollider2D + SpriteRenderer + PlayerMovement (fully wired) + PlayerHealth (5 HP)
- Movement kit working: run, jump (coyote + buffer), wall slide/jump, dash + ghost trail, cooldowns
- Single attack: OverlapCircle on 'Enemy ' layer, 1 damage + knockback, 0.3 s cooldown
- Health: contact damage enemy→player, i-frames, stun, knockback both directions
- UI Canvas (1920×1080) + HealthUI wired (playerHealth, heartPrefab, heartsParent)
- Ground tilemap on layer 'Ground ', enemy on layer 'Enemy ', EventSystem (Input System UI module)

**Defects found:**

| # | Severity | Finding | Status |
|---|----------|---------|--------|
| 1 | Critical | HealthUI never updated — `UpdateHearts()` only called in `Start()` | ✅ Fixed (event-driven) |
| 2 | Critical | `SampleScene.unity` broken (stale serialization + missing PlayerHealth → NREs every frame) | ✅ Deleted (user-approved), via setup tool |
| 3 | Critical | No camera follow (static camera, no Cinemachine) | ✅ Fixed (Cinemachine rig) |
| 4 | High | No controller support; legacy `Input` API despite Input System package installed | ✅ Migrated (keyboard + gamepad) |
| 5 | High | Death = instant `Destroy(gameObject)` → UI/camera refs broken, no respawn | ✅ Fixed (respawn system) |
| 6 | Medium | No enemy AI (static contact-damage box) | ⏳ Phase 2 |
| 7 | Medium | `Invoke()` timers, trailing-space names, no player prefab | ✅ Fixed |
| 8 | Low | Player Rigidbody no interpolation; non-uniform player scale in old scene | ⏳ Noted (revisit with sprites) |

### Phase 1 changes

**Packages** — added `com.unity.cinemachine` 3.1.7 (official release for Unity 6000.3).

**Input (`Assets/InputSystem_Actions.inputactions`)** — adapted the default template in place (UI map untouched — EventSystem depends on it):
- Player map now: `Move, Attack, Jump, Dash, Interact`
- Removed unused template actions: Look, Crouch, Previous, Next (+ their bindings)
- Renamed Sprint → **Dash**: LeftShift + gamepad B (buttonEast) + RB (rightShoulder)
- Added keyboard **J** to Attack (prototype binding) alongside Enter, mouse-left, gamepad X

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Move | WASD / arrows | Left stick |
| Jump | Space | A (south) |
| Dash | Left Shift | B (east) / RB |
| Attack | J / Enter / LMB | X (west) |
| Interact | E | Y (north) — reserved for NPCs (Phase 2) |

**Scripts** (organized into `Scripts/Player`, `Scripts/World`, `Scripts/UI`, `Scripts/Editor`; `.meta` files moved with the `.cs` files so GUIDs and scene references survive):

- **`Player/PlayerInputReader.cs` (new)** — single input gateway. Polls the Input System asset (no callbacks), exposes `MoveInput`, `JumpWasPressed`, `DashWasPressed`, `AttackWasPressed`, `InteractWasPressed`. Enables/disables ONLY the `Player` action map (UI map is owned by EventSystem).
- **`Player/PlayerMovement.cs` (rewritten)** — identical feel and tuning; reads `PlayerInputReader` instead of legacy `Input`. All serialized field names preserved (`moveSpeed`, `jumpForce`, `coyoteTime`, `jumpBufferTime`, `wallSlideSpeed`, `wallJumpForce`, `wallJumpLockTime`, `dashSpeed`, `dashDuration`, `dashCooldown`, `groundCheck`, `wallCheck`, `checkDistance`, `groundLayer`, `dashGhostPrefab`, `ghostInterval`). Adds `FacingDirection`, `ResetState()` (clears stuck dash state after death), graceful null checks with setup instructions. Attack logic **removed** (moved to PlayerAttack).
- **`Player/PlayerAttack.cs` (new)** — the light slash, split from movement. Serialized values migrated by setup tool (attackRange 0.5, attackCooldown 0.3, enemyLayer = Enemy, attackPoint child ref). Small deliberate fix: attack is blocked while stunned (was allowed before).
- **`Player/PlayerHealth.cs` (rewritten)** — same serialized names (`maxHealth`, `invincibilityTime`, `stunTime`, `knockbackForce`). New: `OnHealthChanged(current,max)` / `OnDamaged` / `OnDied` events, timer-based i-frames/stun (no more `Invoke`), hurt sprite flash, **no Destroy on death** — raises `OnDied` instead.
- **`Player/PlayerRespawn.cs` (new)** — on death: freezes input/movement/attack/physics → fade out → teleport to checkpoint → `ResetHealth()` + `movement.ResetState()` → fade in → unfreeze. Player object persists (camera + UI refs stay valid).
- **`World/Checkpoint.cs` (new)** — trigger that moves the respawn point (foundation for Forgotten Benches in Phase 3).
- **`UI/HealthUI.cs` (rewritten)** — subscribes to `PlayerHealth.OnHealthChanged`, rebuilds hearts when max changes. Same serialized refs (playerHealth, heartPrefab, heartsParent).
- **`Editor/SoledarSetupUtility.cs` (new)** — one-click scene wiring, see below.
- **`EnemyHealth.cs`** — untouched this phase (works; AI + damage reactions come in Phase 2).

**One-click setup tool** — menu `Tools > Soledar > Setup Phase 1 Scene` (idempotent):
1. Renames `DashGhost .prefab` → `DashGhost.prefab`, `Heart .prefab` → `Heart.prefab`
2. Trims layer names `'Ground '` → `Ground`, `'Enemy '` → `Enemy` (serialized LayerMask bits unaffected)
3. Adds `PlayerInputReader` + assigns `InputSystem_Actions.inputactions`
4. Adds `PlayerAttack` + wires attackPoint / enemyLayer / ranges; re-checks movement check refs
5. Adds `PlayerRespawn` + creates `SpawnPoint` (at player) + demo `Checkpoint` trigger
6. Camera: `CinemachineBrain` on Main Camera + `PlayerFollowCamera` (CinemachineCamera + Position Composer, ortho 5, damping 0.5/0.5, look-ahead 0.1)
7. Saves `Assets/Prefabs/Player/Player.prefab` from the wired player
8. Sets TestScene as build scene 0
9. Deletes broken `SampleScene.unity` + empty `Scenes 1` folder (user-approved)
10. Saves the scene

### Phase 1 test checklist (run in Play mode, TestScene)

- [ ] **Compile**: no errors in Console after Unity refreshes
- [ ] **Console after running setup tool**: green "[SoledarSetup] Phase 1 scene setup COMPLETE."
- [ ] **Move**: WASD/arrows and/or gamepad stick — speed and feel identical to before
- [ ] **Jump**: Space/gamepad A — coyote time + buffer still work; holding = higher jump
- [ ] **Wall**: slide on a wall + wall jump pushes away
- [ ] **Dash**: Shift/gamepad B/RB — trail still spawns, 0.5 s cooldown
- [ ] **Attack**: J/LMB/gamepad X — enemy takes 1 damage + knockback, dies after 3 hits
- [ ] **Health UI**: take 1 contact hit → heart icons update immediately; red flash on player
- [ ] **Death/respawn**: die (5 hits) → fade out → back at SpawnPoint → full 5 hearts → controllable again (camera never lost the player)
- [ ] **Checkpoint**: walk into the Checkpoint trigger → Console "Respawn point updated" → walk off and die → respawn at checkpoint
- [ ] **Camera**: smooth follow with slight look-ahead; no jitter
- [ ] **Gamepad**: if available, verify stick + A/B/X

### Known issues / deferred (tracked for Phase 2+)

- ~~Hit during dash: knockback is overwritten by dash velocity~~ — **improved in Phase 2**: hit-stop + shake on attack connect; dash-knockback interplay still to polish when abilities land (Phase 5).
- ~~Enemy has no AI~~ — ✅ **Phase 2** (EnemyAI FSM + EnemyAttack lunge).
- ~~Single-ray ground check can miss ledge edges~~ — still open for the *player*; **enemies** got 2-probe edge/wall checks in Phase 2.
- Camera has no room bounds/confiner — added with the first real region (Phase 3).
- Non-uniform player scale (0.6, 1.2) in TestScene — clean up when real sprites arrive (keep uniform scale + collider size).
- ~~Interact action exists but has no consumer~~ — ✅ **Phase 2** (PlayerInteractor + DialogueUI).
- Scene file still at `Assets/TestScene.unity` (relocate to `Assets/Scenes/` when the editor is closed).

---

## Phase 2 — Combat & Interaction

### Scope delivered

- [x] Weapon combos (3-hit Ashen Khoumya combo + up/down aimed attacks)
- [x] Enemy health & AI (Idle → Patrol → Chase → Attack, LOS + ledge/wall probes)
- [x] Enemy attack pattern (telegraphed lunge: windup tint → lunge → recovery → cooldown)
- [x] Damage animations (hurt flash + knockback + AI stun on enemies; death fade-out; player already had flash/stun from Phase 1)
- [x] Hit-stop (micro freeze via timeScale, pause-menu-safe)
- [x] Camera shake (Cinemachine impulse)
- [x] Interactable objects (IInteractable + PlayerInteractor + HUD prompt)
- [x] NPC interaction + dialogue system (typed lines, advance/close, gameplay freeze during talk)

### New scripts

| Script | Folder | Role |
|--------|--------|------|
| `EnemyData.cs` | Scripts/Enemies | SO per enemy type: HP, contact damage, speeds, detection, attack timing, knockback resist, ground mask, placeholder tint |
| `EnemyHealth.cs` | Scripts/Enemies | Rewritten (same GUID, same serialized `maxHealth`/`knockbackForce` fallbacks): reads EnemyData when assigned, hurt flash + knockback + AI stun, death = fade-out then destroy; events `OnHealthChanged/OnDamaged/OnDied` |
| `ContactDamage.cs` | Scripts/Enemies | Contact damage split out of EnemyHealth: Enter+Stay with own cooldown, reads EnemyData |
| `EnemyAI.cs` | Scripts/Enemies | FSM Idle/Patrol/Chase/Attack; detection = distance + line-of-sight (ground layer blocks); patrol never walks off edges (edge + wall probes); movement intent in FixedUpdate; gizmos for ranges |
| `EnemyAttack.cs` | Scripts/Enemies | Lunge: Windup (red tint telegraph) → Lunge (velocity burst) → Recovery → cooldown. Damage via ContactDamage. Must be BELOW EnemyAI in component order |
| `HitStopController.cs` | Scripts/Combat | Static `Freeze(duration)` — timeScale dip using unscaled time; stores previous timeScale (pause-menu-safe); hidden DontDestroyOnLoad driver restores it |
| `AttackComboData.cs` | Scripts/Combat | SO: ordered steps {damage, range, recoveryTime, comboWindow, knockbackMultiplier, hitStopDuration, shakeForce} |
| `CameraShake.cs` | Scripts/Combat | Wrapper over CinemachineImpulseSource (on Player); `Shake(force)` fires an impulse |
| `IInteractable.cs` | Scripts/Interaction | Interface: `GetPrompt()`, `CanInteract`, `Interact(GameObject interactor)` |
| `PlayerInteractor.cs` | Scripts/Interaction | Finds nearest IInteractable (OverlapCircle on Interactable layer), shows HUD prompt, triggers on Interact press; freezes movement+attack while dialogue is open; 0.2 s debounce after closing |
| `DialogueData.cs` | Scripts/Dialogue | SO: speakerName + lines[] |
| `DialogueUI.cs` | Scripts/Dialogue | Panel control + typing effect (unscaled time, 45 cps); advance with Interact; skips typing on first press; `inputLockAfterOpen` 0.15 s so the opening press doesn't advance; raises `OnDialogueClosed`; exposes `LastClosedTime` |
| `NPCInteractable.cs` | Scripts/Dialogue | IInteractable that opens a DialogueUI with its DialogueData |
| `PlayerAttack.cs` | Scripts/Player | **Rewritten**: consumes AttackComboData (3 steps), combo windowing, up/down aimed attacks (MoveInput.y), hit-stop + shake ONLY on connect; fallback single-slash when no combo assigned; still blocked while stunned/dead (combo resets on stun) |

**Setup tool** — second menu item `Tools > Soledar > Setup Phase 2 Combat & NPCs` (requires Phase 1 wiring present; idempotent; re-running resets data-asset values to defaults):
1. Adds layer `Interactable` (first free user slot; expected index 8)
2. Creates placeholder art `Assets/Art/Placeholder/PlaceholderSquare.png` (64×64 white, point filter, sprite import)
3. Creates data assets:
   - `Assets/ScriptableObjects/Combat/KhoumyaCombo.asset` — steps 1/1/2 dmg, ranges 0.5/0.55/0.6, recovery 0.22/0.22/0.4, combo window 0.3, knockback ×1/1.1/1.6, hit-stop 0.03/0.035/0.07, shake 0.25/0.3/0.6
   - `Assets/ScriptableObjects/Enemies/DustCrawler.asset` — HP 3, contact 1, patrol 1.5, chase 2.5, detect 5, lunge 6, no knockback resist, dusty-brown tint
   - `Assets/ScriptableObjects/Enemies/MedinaGuard.asset` — HP 4, contact 1, patrol 2, chase 3.5, detect 7, lunge 7, 30% knockback resist, guard-blue tint
   - `Assets/ScriptableObjects/Dialogue/CartographerIntro.asset` — "The Old Cartographer", 4 lines
   - Both enemy assets get `groundMask` = layer `Ground`
4. Player: assigns `comboData`; adds `CinemachineImpulseSource` + `CameraShake`; adds `PlayerInteractor` (radius 1.3, mask = Interactable layer, prompt ref)
5. Camera: `CinemachineImpulseListener` on `PlayerFollowCamera`
6. Canvas `UI`: `DialogueUI` component + `DialoguePanel` (dark Image, bottom, 250 px, hidden) with `SpeakerText` / `BodyText` / `HintText` ("E - continue") + `InteractionPrompt` text (bottom-center, hidden until an interactable is near). Legacy UI.Text with built-in `LegacyRuntime.ttf` — deliberately **no TMP** (no Essentials dependency) until Phase 4 UI polish
7. Scene enemy `'Enemy '` → renamed `DustCrawler`, wired: EnemyHealth + ContactDamage + EnemyAI + EnemyAttack (in that order) all sharing the DustCrawler data; placeholder sprite + tint; Rigidbody gravity 3 / rotation frozen / interpolated
8. New `MedinaGuard` created at DustCrawler position + (3, 0), same component stack, MedinaGuard data
9. Saves `Assets/Prefabs/Enemies/DustCrawler.prefab` and `MedinaGuard.prefab` (prefab instance connected in scene)
10. NPC `OldCartographer` at SpawnPoint + (2.5, 0): layer Interactable, amber placeholder sprite, trigger box 1.2×2, `NPCInteractable` wired to CartographerIntro + DialogueUI
11. Project setting: Enemy↔Enemy layer collisions ignored (enemies pass through each other)
12. Saves the scene

### Phase 2 test checklist (run in Play mode, TestScene)

Setup: run `Setup Phase 1 Scene` then `Setup Phase 2 Combat & NPCs` (both green "COMPLETE" in Console, no red errors).

**Combat**
- [ ] **Combo**: stand next to DustCrawler, press J three times quickly → 3rd hit deals 2 damage (enemy 3 HP → dies after 1-2-3); if you pause > 0.3 s between presses the combo restarts at step 1
- [ ] **Feel**: every hit that connects → tiny freeze-frame + small camera shake; misses produce neither
- [ ] **Aiming**: hold W/Up while pressing J → hitbox checks above (attack a taller enemy); hold S/Down → below
- [ ] **Fallback**: temporarily clear `PlayerAttack.comboData` in Inspector → single 1-damage slash with 0.3 s cadence still works (restore after)

**Enemies**
- [ ] **DustCrawler**: patrols; turn at ledges/walls; notice you within ~5 units → chase; close range → red windup tint ~0.35 s → lunge; then recovery + ~1.5 s cooldown
- [ ] **MedinaGuard**: patrols around its spawn (+3 units from DustCrawler), faster chase; knockback visibly smaller when hit (30% resist)
- [ ] **Hurt**: enemy flash white on hit + brief stagger (AI stops moving)
- [ ] **Death**: fade-out after final hit; enemy disabled/removed; no errors in Console
- [ ] **Contact**: touching an enemy while it walks → 1 damage + knockback + i-frames; standing inside doesn't machine-gun (per-enemy cooldown ~0.8 s)

**Interaction & dialogue**
- [ ] **Prompt**: walk near OldCartographer → "E  Talk to The Old Cartographer" appears bottom-center
- [ ] **Dialogue**: E → panel slides in bottom, speaker "The Old Cartographer", text types out
- [ ] **Advance**: E while typing → completes line instantly; E again → next line; after last line → panel closes, player can move again
- [ ] **Freeze**: during dialogue the player cannot move/jump/attack; resumes cleanly after
- [ ] **Debounce**: the E press that CLOSES the dialogue must not instantly re-open it

**Phase 1 re-check** (they share the scene): hearts still update, death/respawn + checkpoint still work, camera still follows (now with shake).

### Known issues / deferred (Phase 3+)

- Enemy sprites are placeholder squares with tints — real art in Phase 6; attack VFX (slash arc) also deferred there.
- Dialogue has no choices yet — Phase 4 (quest hooks) extends DialogueData.
- Player can fall slightly while talking if dialogue starts mid-air (movement disabled, gravity still applies) — acceptable for now; will hard-freeze with the pause system in Phase 4.
- Down-attack has no pogo bounce — comes with the Descent of Ash ability (Phase 5).
- Hit-stop vs PlayerRespawn freeze: both manage timeScale; overlap is practically impossible (hit-stop only fires while attacking) but worth revisiting when Phase 4 pause menu lands.
- Balance values (combo/enemy data) reset if the Phase 2 tool is re-run — tweak in Inspector after that, not by re-running the tool.

---

## Phase 3 — First Region: "Medina Outskirts"

### Scope delivered

- [x] First real region as a **tilemap level** (Grid + Ground/Background tilemaps, composite collider)
- [x] Placeholder tile art (generated sandstone ground + darker wall sprites; fixed the broken `GroundTile.asset` RuleTile reference)
- [x] Camera confinement (CinemachineConfiner2D + region polygon)
- [x] Forgotten Benches — real save points (E to rest: full heal + respawn point)
- [x] Key → locked gate progression loop (Medina Gate + Medina Gate Key)
- [x] Sealed Passage teaser (permanent gate = Phase 4+ region exit)
- [x] Death pits (kill volumes that bypass i-frames)
- [x] Enemy encounters placed from the Phase 2 prefabs

### Region layout (96 × 22 tiles, all jumps respect apex ≈ 2.45 tiles)

- **Plaza** (x 2–14): spawn, Old Cartographer NPC, DustCrawler_Plaza nearby, Bench_Plaza
- **Teaching pit** (x 15–18): 2 deep, climbable out
- **Step up** (x 25–30, y=4) → **Elevated street** (x 31–47, y=5): MedinaGuard_Street patrols; Bench_Street before it
- **Key alcove** (x 35–37, top y=8, via stepping stone at x 38): Medina Gate Key floats there
- **Medina Gate** (x 40–41, y 5–11): locked until the key; DustCrawler_InnerGate waits behind it
- **Descend** (x 48–50 → y=4, then y=3)
- **Dash gaps** (x 56–57 and x 71–72): open pits → death pit strip below (y −2.5, full width)
- **Lower street** (x 58–70): DustCrawler_LowerStreet patrols between the gaps
- **Climb** (platforms at x 73–74 y=5, x 76–77 y=7, x 79–80 y=9)
- **City wall plateau** (x 82–93, top y=10): Bench_CityWall + **Sealed Passage** (x 86–87) — permanently locked region exit

Death recovery: 3 benches; dying after resting respawns you at the last bench with full health.

### New scripts

| Script | Folder | Role |
|--------|--------|------|
| `ForgottenBench.cs` | Scripts/World | IInteractable: rest = full heal (`ResetHealth`) + `SetRespawnPoint(bench)`; prompt "Rest at …" |
| `CollectibleKey.cs` | Scripts/World | Walk-over trigger pickup with idle bob; calls `targetDoor.Unlock(keyName)`; direct Door reference (no statics) |
| `Door.cs` | Scripts/World | Solid gate on the **Ground layer** (blocks player + enemy probes); `Unlock()` disables collider + fades sprite; a gate with no key stays sealed |
| `DeathPit.cs` | Scripts/World | Invisible trigger strip under the region; calls `PlayerHealth.Kill()` |
| `PlayerHealth.Kill()` | Scripts/Player | New method: kills through i-frames (falling must always be lethal) |

**Setup tool** — third menu item `Tools > Soledar > Setup Phase 3 Region (Medina Outskirts)` (requires Phases 1+2; idempotent):
1. Creates placeholder tile sprites `Art/Placeholder/PlaceholderGround.png` + `PlaceholderWall.png` (64×64, sandstone + wall tints)
2. Repairs `Assets/GroundTile.asset` (RuleTile) — its default sprite reference was broken (guid not found in the project); assigns the ground sprite
3. Creates `Art/Placeholder/WallTile.asset` (new RuleTile, collider off) for the background layer
4. Builds `Level` (Grid) with children `BackgroundTiles` (order −10, darker tint, no collider) and `GroundTiles` (order −5, layer **Ground**, TilemapCollider2D merged via CompositeCollider2D + static Rigidbody2D)
5. Paints the region from the layout above (~950 solid tiles, 2112 background tiles)
6. Disables superseded objects: old prototype `Ground` box + demo `Checkpoint` (replaced; reversible)
7. Camera: adds `CinemachineConfiner2D` to `PlayerFollowCamera`, wires `CameraBounds` polygon (trigger-only, so falls aren't physically blocked) — region x 1..95, y 0..20
8. Moves Player + SpawnPoint to the plaza (5, 3.6); repositions OldCartographer (9, 3.6)
9. Places 3 Forgotten Benches, MedinaGate (+ key at the alcove), SealedPassage, DeathPit strip
10. Enemies: reuses the two Phase 2 scene instances (`DustCrawler_Plaza`, `MedinaGuard_Street`), instantiates `DustCrawler_InnerGate` (behind the gate) and `DustCrawler_LowerStreet` from prefabs
11. Saves the scene

### Phase 3 test checklist (Play mode)

Setup: run the Phase 1, 2, and 3 tools in order (each logs green COMPLETE).

**Level & camera**
- [ ] Region visible: sandstone floor, darker background walls, side walls; old test box and demo checkpoint gone
- [ ] Camera confined: walk/jump/climb everywhere — screen never shows black voids beyond the region; smooth at edges
- [ ] Teaching pit: fall in, jump out (2-deep)
- [ ] No unjumpable obstacles: every step ≤ 2 tiles up, gaps crossable (run+jump, or dash)

**Progression loop**
- [ ] Medina Gate (dark red) blocks the elevated street; enemies can't cross either
- [ ] Stepping stone (x 38) → hop onto the alcove → gold key bobs; walk into it → Console logs the pickup + gate unlocks and fades
- [ ] After the gate: inner street enemy, descending steps
- [ ] Sealed Passage on the plateau stays solid (no key exists)

**Benches**
- [ ] Prompt "E  Rest at Forgotten Bench" near all three benches
- [ ] Rest: hearts refill instantly, Console logs the rest, respawn point moves (die afterwards → respawn at that bench, full health)

**Death pits**
- [ ] Fall into either dash gap → fade → respawn at the last checkpoint — even right after taking a hit (i-frames don't protect pits)
- [ ] No invisible floor stops the fall (the camera-bounds polygon must not catch you)

**Enemies in the region**
- [ ] DustCrawler_Plaza patrols between the pit and the step; chases + lunges
- [ ] MedinaGuard_Street holds the elevated street; walks under the key alcove without getting stuck
- [ ] DustCrawler_InnerGate only reachable after opening the gate
- [ ] DustCrawler_LowerStreet patrols between the two gaps without walking off
- [ ] All enemies die to the 3-hit combo; killed enemies can be restored by re-running the Phase 3 tool or dragging from `Assets/Prefabs/Enemies`

**Regression (Phase 1+2 re-check)**: hearts, respawn at plaza spawn, combo + hit-stop/shake, NPC dialogue near spawn.

### Known issues / deferred (Phase 4+)

- All visuals are placeholders (squares/tinted tiles) — real art in Phase 6.
- Key/gate state is per-session (no persistence yet) — Phase 4 adds the save system (bench rest = save point).
- The Sealed Passage is intentionally unopenable — becomes the Phase 4 region transition.
- Background is a flat tinted wall — Moroccan pattern/parallax layers come with real art.
- Enemy placements can be tuned by moving the instances; re-running the Phase 3 tool resets them to the designed spots.

---

## Phase 4 — Persistence & Progression: "Upper Medina"

### Scope delivered

- [x] **JSON save system** — keys, opened gates, health, respawn point and player position persist between sessions
- [x] **Auto-save** on key pickup, gate unlock, NPC reward and bench rest (bench = manual save point)
- [x] **Save restore** at scene start (doors/keys check the save in `Awake`; `SaveApplier` restores the player)
- [x] **Pause menu** on Esc / gamepad Start (freezes time; Resume / Quit)
- [x] **Quest groundwork** — the Old Cartographer grants the **Cartographer's Seal** when his intro dialogue ends; the Sealed Passage opens; later talks use a shorter follow-up dialogue
- [x] **District 2 "Upper Medina"** beyond the city wall — the region is now **134 × 22 tiles**
- [x] **Unique door persistence keys** — `doorName` is set from the GameObject name when placed and synced for pre-existing doors

### Save design

- `SaveManager` is a lazy singleton (`EnsureInstance()` + `DontDestroyOnLoad`) mirroring state to `Application.persistentDataPath/soledar_save.json` (`C:/Users/<user>/AppData/LocalLow/DefaultCompany/Soledar/soledar_save.json`).
- File shape (human-readable, `JsonUtility`):

```json
{
  "playerX": 5.0, "playerY": 3.6,
  "health": 5, "maxHealth": 5,
  "collectedKeys": ["Medina Gate Key"],
  "openedDoors": ["MedinaGate"]
}
```

- Write points: `CollectibleKey` pickup → auto-save; `Door.Open` → auto-save; NPC reward → `Door.Unlock` → auto-save; `ForgottenBench` rest → `SaveGame`.
- Restore points: `Door.Awake` / `CollectibleKey.Awake` query the loaded state; `SaveApplier` (on the Player) restores health and position at scene start.
- `PlayerRespawn` gained a position-based override (`SetRespawnPosition`) so a **saved** position beats the default spawn.
- Debug: `Tools > Soledar > Delete Save File (Debug)`.

### District 2 layout (x 94–133, every hop ≤ 2 tiles up)

- **Corridor** (x 94–99, top y10): continues the city-wall plateau through the old boundary
- **Drop shaft** (x 100–102): drop to street level; return path p1 (top y5) → p2 (x102, top y7) → p3 (top y9) → corridor
- **Street** (x 102–125, top y3): death gap at x 110–111; `DustCrawler_District2` and `MedinaGuard_District2` patrol; **Aisha the Rug Merchant** at x 119
- **Climb** (x 116–117 top y5 → x 119–120 top y7)
- **Shrine ledge** (x 122–131, top y9): `Bench_UpperMedina` (x 124); **Palace Gate** (x 128.5, sealed 2×4) — Phase 5 teaser
- **Right wall** closed at x 132–133; camera bounds extended to x 133, death pit widened to 136

### New scripts / changes

| Script | Change |
|--------|--------|
| `Core/SaveManager.cs` (new) | Lazy singleton; load/mirror JSON; `IsKeyCollected` / `IsDoorOpen` / `MarkKeyCollected` / `MarkDoorOpen` / `SaveGame` / `ApplyToScene` / `DeleteSave`; `HasSave` |
| `Core/SaveApplier.cs` (new) | On Player Start: if a save exists, restore health/position (saved position wins over the default spawn) |
| `UI/PauseMenu.cs` (new) | Esc/Start toggle; stores previous `Time.timeScale`, freezes to 0; Resume/Quit buttons; restores timeScale if destroyed while paused |
| `World/Door.cs` | `Awake` re-opens saved doors; `Open()` auto-saves `doorName`; exposes `DoorName` |
| `World/CollectibleKey.cs` | `Awake` destroys already-collected keys; pickup auto-saves before unlocking |
| `World/ForgottenBench.cs` | Rest now calls `SaveManager.SaveGame` |
| `Dialogue/NPCInteractable.cs` | New `rewardDoor` + `repeatDialogue`; unlocks the door when the dialogue closes (once); later talks use the repeat line |
| `Player/PlayerHealth.cs` | New `SetHealth(int)` (clamped), used by save restore |
| `Player/PlayerRespawn.cs` | New position-based respawn override (`SetRespawnPosition`, `CurrentRespawnPosition`) |
| `Player/PlayerInputReader.cs` | New `PauseWasPressed` (Player/Pause action) |
| `InputSystem_Actions.inputactions` | `Player/Pause`: Esc + gamepad Start |
| `Editor/SoledarSetupUtility.cs` | Phase 4 menu item, width-aware painting (`paintWidth`), District 2, camera bounds/death pit resize, `SaveApplier`, pause UI + EventSystem, Cartographer reward wiring, Aisha, District 2 actors, `SyncDoorNames`, Delete Save File |

### Setup tool — Phase 4 (idempotent; requires Phases 1–3)

1. Repaints the region at **134** tiles wide (Phase 3 painting is now width-aware; its old right wall is skipped past x96).
2. Extends `CameraBounds` (x 1..133) and the `DeathPit` strip (136 × 2 at y −2.5).
3. Adds `SaveApplier` to the Player.
4. Builds `PausePanel` (full-screen dark overlay, inactive) with `PauseMenu`, Resume and Quit buttons; adds an EventSystem (`InputSystemUIInputModule`) if missing.
5. Creates `CartographerRepeat.asset` and wires the reward (`rewardDoor = SealedPassage`).
6. Creates `AishaIntro.asset` + Aisha NPC; places `DustCrawler_District2`, `MedinaGuard_District2`, `Bench_UpperMedina`, `PalaceGate`.
7. Syncs every `Door.doorName` to its GameObject name (persistence keys).
8. Warns if a save file already exists (stale saves may pre-open doors — delete via the debug menu).

### Verified in-editor (Unity MCP, 2026-11-12)

- **Compile:** `Assembly-CSharp` + `Assembly-CSharp-Editor` rebuild clean; Console error count 0.
- **Latent Phase 1–3 fixes found during verification** (Phases 1–3 had never been compiled before): Cinemachine 3 namespace is `Unity.Cinemachine`; CM3 serialized fields are `Target.TrackingTarget`, `Lens.OrthographicSize`, `Damping`, `Lookahead.Time`, `BoundingShape2D`; `EditorSceneManager.GetLoadedScenes` → `SceneManager.sceneCount/GetSceneAt`; `Rigidbody2D.interpolate` → `.interpolation`; `AssetDatabase.RenameAsset(Object,…)` → path overload; missing `using UnityEngine.Tilemaps;`; Phase 3's boundary wall would have blocked the new corridor (now painted only at ≤96 width).
- **Scene structure:** tilemaps 134 × 22; camera bounds (1,0)–(133,20) wired to the confiner; `DeathPit` (66.5, −2.5) 136 × 2; doors `MedinaGate` / `SealedPassage` / `PalaceGate` with unique `doorName`s; key `Medina Gate Key`; 4 benches; 6 enemies incl. both District 2 actors; Cartographer `rewardDoor = SealedPassage`, `repeatDialogue = CartographerRepeat`; `PauseMenu` refs wired (panel hidden); EventSystem present.
- **Tile audit:** 23/23 assertions pass (corridor open above y10, shaft platforms, street gap, climb, ledge, right wall, plateau).
- **Play-mode smoke test:** player spawns at (5, 3.6) with 5/5 HP; `Pause()` → `timeScale 0` + panel visible, `Resume()` → `timeScale 1` + panel hidden; `SaveGame` writes the file.
- **Persistence round-trip:** opened `MedinaGate` + recorded the key + saved → exited and re-entered Play mode → gate restored open (collider disabled), key gone, `SealedPassage`/`PalaceGate` still closed, player position/health restored from the file. Test save deleted afterwards.

### Phase 4 test checklist (manual — hands-on pass still recommended)

**Pause**
- [ ] Esc during play → PAUSED overlay, game frozen; Esc again → resumes at normal speed
- [ ] Gamepad Start does the same; Resume/Quit buttons respond to mouse
- [ ] Pause mid-air / mid-lunge → resume continues cleanly (no stuck timeScale)

**Save / restore**
- [ ] Pick the key → Console logs pickup; quit Play, re-enter → key gone, Medina Gate open
- [ ] Open the Sealed Passage via the NPC → quit/re-enter → stays open
- [ ] Rest at a bench → quit/re-enter → dying afterwards respawns at that bench
- [ ] Die without having saved → respawns at the last checkpoint, health resets

**Quest**
- [ ] Talk to the Old Cartographer through his intro → after the last line the Sealed Passage opens (Console: reward granted)
- [ ] Talk to him again → shorter follow-up dialogue
- [ ] Aisha has her own dialogue on the Upper Medina street

**District 2**
- [ ] Drop down the shaft → street; climb back p1 → p2 → p3 → corridor
- [ ] Death gap at x 110–111 drops to the pit; street enemies don't walk into it
- [ ] Climb to the shrine ledge: bench rests/saves; Palace Gate is solid
- [ ] Camera stays inside bounds up to the right wall; no black voids

**Regression:** Phases 1–3 checks (movement/combat/dialogue/benches/gates).

### Known issues / deferred

- Single scene only — no scene transitions yet; multi-scene architecture deferred (the Palace Gate is a Phase 5 teaser).
- No settings/options menu yet (pause has Resume/Quit only).
- Save file has no version field or migration path — deleting it is the reset.
- Stale-save edge: a save written before Phase 4 wiring can pre-open doors; the setup tool warns and the debug menu deletes it.
- NPC reward grants on dialogue close even if the player dies mid-dialogue — accepted for now.
- Dialogue typing continues during pause (unscaled time) — cosmetic; revisit when the UI gets its own pause handling.
- Hit-stop vs pause timeScale: if a hit-stop ends during pause it restores 1 instead of the paused 0 — practically impossible timing, revisit if reported.
- Balance/quest data assets reset if setup tools are re-run (edit in the Inspector afterwards).

---

## Phase 5 — First Boss: "The Gatekeeper" & Descent of Ash

### Scope delivered

- [x] **The Gatekeeper** — the first major boss, guarding the District 2 shrine courtyard in front of the Palace Gate
- [x] **Descent of Ash** — reward ability: airborne down-strike + pogo bounce, persisted in the save
- [x] **Boss arena orchestration** — entrance seal, boss bounds, health bar, retry on death, re-arm on re-entry
- [x] **Boss defeat persistence** — the Gatekeeper stays dead across play sessions (`defeatedBosses` in the save)
- [x] **Sunken Medina vertical slice** (`Assets/Scenes/SunkenMedina.unity`) — cloned from TestScene with discovery rooms, Ancient Dirham currency, the Silent Merchant, Lost Soul, Medina Warden and a completion trigger
- [x] **Ziri visual install** — the protagonist's placeholder art lives on a `ZiriVisual` child (idle bob; gameplay untouched)

### The Gatekeeper (design + numbers)

State machine `Dormant → Choosing → WindUp → Slam / Sweep / Volley → Recover` (`Enemies/GatekeeperBoss.cs`):

| Attack | Behaviour | Tuning |
|--------|-----------|--------|
| Slam | leaps at the player, lands with an AoE shockwave | slamJump 12, chase 7, AoE radius 1.7 + camera shake |
| Sweep | dashes across the courtyard floor | speed 8 (phase 1) → 10 (phase 2) |
| Volley | lobs arcing projectiles | 3 shots (phase 1) → 5 shots (phase 2), vx ±2.5, vy 7 |

- Telegraph 0.55 s, recover 1.1 s, choose delay 0.7 s; walk 1.2; HP 30 via the shared `EnemyHealth` (knockbackForce 0 so it never staggers).
- **Phase 2 at ≤ 50 % HP:** angry tint + faster sweep + bigger volleys.
- Projectiles are `BossProjectile` (arcing, ground-layer collision, despawn on impact).
- The boss is clamped inside the arena by `BossBounds`; teleports use `rb.position` + `Physics2D.SyncTransforms()` (Unity 6 keeps `autoSyncTransforms` off).

### Descent of Ash (player kit)

- Airborne + holding Down + `HasAbility("descentOfAsh")` → dedicated down-strike (`PlayerAttack`): own range (1), doesn't advance the ground combo, 0.25 s recovery.
- Striking an enemy or a `PogoSurface` below bounces the player up (`PogoBounce`, force 11) — the street gap at x 110–111 is crossable with a pogo pad placed over it.

### BossArena orchestration (`World/BossArena.cs`)

- Lives on the `GatekeeperArena` trigger at the courtyard entrance; on entry it seals the way back (child `Seal`), wakes the boss, shows `BossHealthUI` ("THE GATEKEEPER").
- Victory: grants Descent of Ash, opens the Palace Gate, shows the reward banner (6 s), records `defeatedBosses: ["gatekeeper"]` — auto-saved.
- Death: unseals, resets boss (HP 30, dormant) and hides the UI; the bench inside the courtyard is the retry point; the arena re-arms when the player re-enters.
- On later sessions with the boss already defeated, the arena unlocks the gate and deactivates the boss object.

### Sunken Medina vertical slice

`Tools > Soledar > Create Sunken Medina Vertical Slice` clones TestScene → `Assets/Scenes/SunkenMedina.unity` (TestScene stays the regression scene). Demo content (stable IDs preserved):

| Object | Room ID / item | Placement (verified against the tilemap) |
|--------|----------------|------------------------------------------|
| Room_Start / Street / Rooftops / Courtyard | `medina_start`, `medina_street`, `medina_rooftops`, `broken_riad` | trigger centres (3,0) (20,0) (38,10) (57,5) |
| Dirham_Street_01 / Rooftop_01 / Courtyard_01 | 10 dirhams each | (11,3.5) (27,6.5) (45,5.5) |
| SilentMerchant | "Wind of the Atlas" for 25 dirhams (grants `windOfAtlas`) | (20,1.5) in the street trench |
| LostSoul | floating ranged enemy (HP 3) | (34,6) over the y5 ledge |
| MedinaWarden | mini-boss guard (HP 12, charge AI) | (55,3.5) inside the broken riad |
| DemoCompletion + DemoCompletePanel | inert until `gatekeeper` is defeated | (65,3) past the riad |

Demo flow: collect 3 × 10 dirhams → buy the Wind of the Atlas (30 available, 5 left over) → cross Medina Gate → defeat the Gatekeeper deep in District 2 → walk back to the completion trigger to see the end panel.

### Defects found & fixed during verification

| # | Severity | Finding | Fix |
|---|----------|---------|-----|
| 1 | Critical | `GroundTiles` carried a **Dynamic** Rigidbody2D baked into the Phase 4 commit — the level fell/tumbled at play start | Set to Static + saved; the Phase 5 setup tool now asserts it (regression guard in step 0) |
| 2 | Critical | Unity 6 keeps `Physics2D.autoSyncTransforms` off — transform-only teleports left the physics body behind (respawn visibly desynced) | `PlayerRespawn.RespawnRoutine` and `GatekeeperBoss.ResetBoss` now also set `rb.position` + `Physics2D.SyncTransforms()` |
| 3 | High | Boss health bar and reward banner never showed — `BossArena.healthUI` / `rewardBanner` were wired before those UI objects existed in the setup order | Setup tool completes the arena wiring after UI creation; all six arena fields + `bossId` verified |
| 4 | High | `SunkenMedinaBootstrap.CreateMarker` NRE'd: Unity 6.3 refuses to add a `SpriteRenderer` while the quad's `MeshRenderer` exists (`AddComponent` returns null) | Markers keep the quad mesh as the placeholder visual; the tint goes through a material instance |
| 5 | Medium | Demo markers were placed as if positions were corners/ground-level guesses — several buried under tiles (dirhams at y2 under a y3/y5 surface) or floating over pits (Warden at (41,2) under an y11 column) | Raycasted a full ground-height map of the tilemap and repositioned every marker onto solid, reachable ground |
| 6 | Medium | Room discovery never fired at spawn — the bootstrap treated room positions as corners, but `BoxCollider2D` sizes are centred (Room_Start spanned x−7..3, missing the spawn at x5) | Re-centred all four room triggers; spawn discovery verified |
| 7 | Medium | `DemoEndTrigger.completionPanel` was never authored — completion would have been invisible | Bootstrap now creates `DemoCompletePanel` (canvas text, hidden) and wires it |
| 8 | Medium | **Ground-check seam bug (latent since Phase 1):** the ground check was a single centre ray; when the player stood exactly on a tilemap cell seam (the spawn is one), the ray grazed the seam between composite polygons and missed → grounded never became true, first jump dead | `PlayerMovement.IsGrounded()` now probes a short `OverlapBox` (0.8 wide) across the feet |

### Verification (play mode, this phase)

- World static at play start (regression guard passes); Ziri visual renders and the player stands on the courtyard ledge next to the bench.
- Fight start: seal on, bar visible ("THE GATEKEEPER", Filled 1.00), boss awake at 30/30.
- Phase 2: angry tint + 5-projectile volley observed live.
- Retry: player death → respawn at spawn with **body and transform in sync** (fix #2 verified), boss reset to 30/30 dormant, seal off, bar hidden; re-entry re-arms the full fight.
- Victory: Descent of Ash granted, Palace Gate opened, `defeatedBosses: ["gatekeeper"]` + ability persisted to `soledar_save.json`.
- Demo scene: all demo objects present with correct components; build settings contain TestScene + SunkenMedina; spawn room discovery fires; ground check grounded at spawn after fix #8.

### Phase 5 test checklist (manual — hands-on pass recommended)

**Boss fight (District 2 shrine courtyard)**
- [ ] Walk into the courtyard — entrance seals behind you, boss bar appears
- [ ] Learn his tells: slam shockwave, floor sweep, arcing volleys; phase 2 at half health (angry tint, 5 shots)
- [ ] Die → respawn at the courtyard bench, boss fully reset, seal gone; walk back in → fight re-arms
- [ ] Win → banner, Descent of Ash acquired, Palace Gate opens; quit/re-enter — boss stays dead, gate stays open

**Descent of Ash**
- [ ] In the air, hold Down + Attack → down-strike; pogo off the PogoPad over the x110–111 gap
- [ ] Pogo off enemies without taking contact damage

**Sunken Medina slice (open `Assets/Scenes/SunkenMedina.unity`, set as first scene or play from there)**
- [ ] Rooms log as discovered as you pass through them (start room fires at spawn)
- [ ] Collect all 3 Ancient Dirhams (30 total); buy "Wind of the Atlas" from the Silent Merchant (E) — verify the deny message with < 25, then the purchase
- [ ] Lost Soul hovers and lobs shots; Medina Warden telegraphs and charges in the broken riad
- [ ] After beating the Gatekeeper, walk back to x65 — the completion panel appears

### Known issues / deferred

- Reward banner and completion panel were verified by wiring/force-activation; their 6 s / trigger windows are easy to miss from remote throttled-editor checks — confirm live.
- Boss/enemy/projectile visuals are placeholder primitives (see `Docs/AssetRegister.md`); Ziri art is AI-generated temporary source awaiting the commissioned sheet.
- Resting mid-fight at the courtyard bench heals while the fight continues — accepted for now; revisit with arena-wide "no rest during fight" rule.
- `EnemyHealth` velocity-zero on damage interrupts boss attacks mid-animation — acceptable, revisit if it looks wrong live.
- Demo merchants/gates grant `windOfAtlas` but no `AbilityGate` blockers are placed yet — the charm is cosmetic until District 1 gets ability-gated secrets.
- Remote (bridge-driven) play testing runs at ~4–7 fps while the editor is unfocused — timing-sensitive checks (banner windows, trigger races) are only reliable hands-on.

---

## Phase 6 — The Silent Palace & scene transitions

### Scope delivered

- [x] **Scene transitions** — walk-through doorway, scene loader, per-scene named spawn points, arrival hand-off
- [x] **Persistent scene state** — save v3 records the current scene + spawn point; bench saves are scene-tagged; continue restores the player at the last checkpoint
- [x] **The Silent Palace** — a new self-contained region behind the Palace Gate (`Assets/Scenes/SilentPalace.unity`, 76 × 22 tiles)
- [x] **Shadow Passage** — the second merchant-chain ability, bought from the Lantern Keeper (40 dirhams, requires Wind of the Atlas)
- [x] **Ability-gated traversal** — Shadow Passage barriers block the palace corridor; a Wind of the Atlas barrier hides an optional dirham stash
- [x] **Merchant chain** — `MerchantInteractable.requiresAbility` refuses trade until the earlier ability is owned
- [x] **Interactor freeze fix** — non-dialogue interactions (merchants, benches) no longer leave the player frozen

### Scene flow design

```
SunkenMedina ── PalaceDoorway (behind the Gatekeeper-opened Palace Gate)
     │                 x 130.6, y 10.5 → target SilentPalace / "palace_entry"
     │
     └── MedinaReturn  x 126, y 9.6  ← arrival point for the palace return doorway

SilentPalace ── PalaceDoorway (left edge of the entry hall)
                       x 2.2, y 5 → target SunkenMedina / "medina_return"
```

- `SceneLoader.BeginTransition(scene, spawnId)` is the only scene-loading entry point. It
  1. writes a scene checkpoint to the save file,
  2. hands the spawn id to `SceneFlow` (keyed by target scene name),
  3. loads the scene, and clears its in-flight flag on `sceneLoaded`.
- `SaveApplier.Start()` in the arriving scene consumes the pending spawn, teleports the player
  (transform + Rigidbody2D + `Physics2D.SyncTransforms`) and makes that spawn point the respawn point.
- `SceneTransition` is trigger-only, ignores a dead player, and has a 1.5 s re-arm window so a
  spawn point next to a return doorway cannot bounce the player straight back.

**Save v3 shape** (old saves load fine; missing fields default to empty):

```json
{
  "version": 3,
  "playerX": 9.0, "playerY": 3.35,
  "health": 5, "maxHealth": 5,
  "unlockedAbilities": ["windOfAtlas", "shadowPassage"],
  "discoveredRooms": ["palace_entry_hall"],
  "ancientDirhams": 5,
  "currentScene": "SilentPalace",
  "spawnPointId": ""
}
```

- `SaveGame()` tags every save with the active scene and clears `spawnPointId` (position is authoritative).
- `SetSceneCheckpoint()` snapshots the departing scene first, then points the save at the destination.
- `ApplyToScene()` only restores coordinates when the recorded scene is the active one; after a
  transition the named spawn point wins over raw coordinates.

### The Silent Palace (76 × 22 tiles)

| Area | x range | Notes |
|---|---|---|
| Entry hall (floor top y3) | 2–26 | arrival at x4, bench x9, 3 × 15 dirhams, Lantern Keeper x16.5 |
| Shadow barrier | x 20.5 | 1 × 3 solid barrier, requires `shadowPassage` |
| Death gap | x 27–28 | 2 wide, jump/dash across, death pit below |
| Corridor + stair | 29–38 | Dust Crawler x24; platforms top y5 → y7 → y9 (each hop 2 tiles) |
| Upper hall (floor top y9) | 39–72 | Medina Guard x44, Dust Crawler x63, bench x57 |
| Wind alcove secret | x 49.5 / 52 | `windOfAtlas` barrier, 20 dirhams behind it |
| Palace Seal | x 71.5 | permanently locked door — next-region teaser |

- Camera bounds (1,0)–(74,20) wired to the existing confiner; death pit 78 × 2 at y −2.5.
- Tilemap tinted cool ivory/indigo so the region reads differently from the Medina sandstone.
- Map rooms: `palace_entry_hall`, `palace_gallery`, `palace_upper_hall`.

### New scripts / changes

| Script | Change |
|--------|--------|
| `Core/SceneFlow.cs` (new) | Static, scene-keyed spawn request; cleared on play start |
| `Core/SceneLoader.cs` (new) | `BeginTransition(scene, spawnId)`; build-settings guard; in-flight flag reset on `sceneLoaded` |
| `World/SceneTransition.cs` (new) | Trigger doorway with destination + spawn id + re-entry cooldown |
| `World/SceneSpawnPoint.cs` (new) | Named arrival anchor + `Find(id)` |
| `Core/SaveManager.cs` | Save v3 (`currentScene`, `spawnPointId`), scene-tagged saves, scene-aware position restore, `SavedHealth`, static `AbilityUnlocked` event |
| `Core/SaveApplier.cs` | Consumes the pending spawn on arrival; restores saved health; otherwise applies the save |
| `Player/PlayerRespawn.cs` | New `TeleportTo(Vector3)` (transform + body + sync + `ResetState`) |
| `Core/AbilityGate.cs` | Live refresh: subscribes to `SaveManager.AbilityUnlocked`, opens instantly on purchase; blocker + Veil visual wiring |
| `Dialogue/MerchantInteractable.cs` | Optional `requiresAbility` chain gate + locked prompt; purchase logs the granted ability |
| `Interaction/PlayerInteractor.cs` | **Bug fix:** only dialogues keep the controls frozen — merchants/benches hand movement back |
| `Editor/SilentPalaceBootstrap.cs` (new) | Phase 6 scene builder + Medina doorway tool |

### Setup tool — Phase 6 (idempotent)

**`Tools > Soledar > Setup Phase 6 Silent Palace`**
1. Copies `TestScene.unity` → `Assets/Scenes/SilentPalace.unity` (only when absent).
2. Removes every Medina-only object, **including inactive roots** (old prototype `Ground`, demo `Checkpoint`) and demo additions.
3. Repaints the 76 × 22 palace layout, tints the tilemaps, forces the ground tilemap static (Phase 5 regression guard).
4. Sets camera bounds + confiner, death pit, player/spawn/camera at the entry.
5. Places: `PalaceEntry` spawn, return `PalaceDoorway`, 3 map rooms, 2 benches, Lantern Keeper, 3 + 1 dirhams, shadow + wind barriers, 3 enemies, `PalaceSeal`.
6. Adds TestScene / SunkenMedina / SilentPalace to Build Settings.

**`Tools > Soledar > Setup Phase 6 Scene Flow (active scene)`** (run on `SunkenMedina`)
- Adds `MedinaReturn` spawn (126, 9.6) and the `PalaceDoorway` trigger (130.6, 10.5 → `SilentPalace` / `palace_entry`).
- TestScene is deliberately left untouched (regression scene, as before).

### Defects found & fixed during verification

| # | Severity | Finding | Fix |
|---|----------|---------|-----|
| 1 | Critical | `SceneLoader.IsLoading` was only reset on play-mode start, so **only the first transition of a session** worked (second request silently ignored) | Reset on `SceneManager.sceneLoaded` |
| 2 | Critical | Bench/other saves were not scene-tagged — quitting in the palace and reopening the Medina would restore palace coordinates in the Medina | `SaveGame` tags `currentScene` and clears `spawnPointId`; `ApplyToScene` skips position across scenes |
| 3 | High | `ApplyToScene` only set the respawn override; the player was **not actually placed** at the saved checkpoint on load (pre-existing Phase 4 gap) | Restore now calls `PlayerRespawn.TeleportTo` |
| 4 | High | Buying from a merchant froze the player — `PlayerInteractor` disabled movement/attack but only dialogues re-enabled them | Re-enable immediately when no dialogue opened |
| 5 | Medium | Inactive Medina leftovers (`Ground`, `Checkpoint`) survived into the palace clone because `GameObject.Find` ignores inactive objects | Cleanup walks `scene.GetRootGameObjects()` including inactive |
| 6 | Medium | Pending spawn was a bare id — a failed/redirected load could teleport a player using another scene's id | Request keyed by target scene name |

### Verification (Unity 6.3, MCP-driven, 2026-11-19)

- **Compile:** `Assembly-CSharp` + `Assembly-CSharp-Editor` rebuild clean; 0 errors.
- **Scene build:** palace tile surface audit matches the design (entry y3, gap x27–28, platforms y5/y7/y9, upper hall y9); camera polygon (1,0)–(74,20); all actors on solid ground; build settings contain all three scenes.
- **Arrival:** entering play in SilentPalace spawns the player at `palace_entry` (4, 3.6), 5/5 HP, barrier sealed, `palace_entry_hall` discovered.
- **Merchant chain:** locked prompt before Wind of the Atlas; purchase refused without spending; prompt switches to “Buy Shadow Passage (40 dirhams)”; purchase sets `shadowPassage`, 45 → 5 dirhams, post-purchase prompt.
- **Ability gates:** unlocking `windOfAtlas` opened the wind barrier (collider off, Veil hidden) live via the event; purchasing `shadowPassage` opened the entry barrier the same way.
- **Transition (live trigger):** teleporting into the Medina doorway loaded SilentPalace at `palace_entry`, health carried over, save scene/spawn correct; the palace doorway returned to `medina_return`, proving the multi-transition fix.
- **Continue:** bench rest wrote `currentScene: SilentPalace`, `spawnPointId: ""`, bench coordinates; a fresh play session restored the player at the bench with body/transform in sync.
- **Cleanup:** test save deleted (no pre-existing save was present); editor left in the SunkenMedina scene; no scene left dirty.
- Not run: hands-on gamepad/keyboard pass (bridge-driven input is not reliable), and the boss-gated walk into the doorway as a player (the trigger was driven by teleport).

### Phase 6 test checklist (manual)

**Transitions**
- [ ] Defeat the Gatekeeper, walk through the opened Palace Gate and into the doorway → Silent Palace loads at the entry hall
- [ ] Walk left into the palace entry wall → back in the Medina at the shrine ledge (bench/gate in view), health unchanged
- [ ] Re-cross the doorway twice in a row → both directions still work (the multi-transition fix)

**Save / continue**
- [ ] Rest at the palace bench, quit Play, re-enter → you start at that bench with full health
- [ ] Pick up dirhams / open a barrier in the palace, quit and reopen the Medina → no palace coordinates leak into the Medina

**Merchant chain**
- [ ] Buy Wind of the Atlas from the Silent Merchant in the Medina → Lantern Keeper prompt becomes “Buy Shadow Passage (40 dirhams)”
- [ ] Try to buy before owning the wind → refusal prompt, no dirhams spent
- [ ] Collect the three entry-hall dirhams (45 total) and buy Shadow Passage → 5 dirhams left, prompt “Thank the Lantern Keeper”, movement still works afterwards

**Ability-gated traversal**
- [ ] Shadow barrier is solid before the purchase; jump/dash cannot clear it; it vanishes when bought
- [ ] Optional wind alcove barrier opens if Wind of the Atlas is owned; 20 dirhams are reachable behind it
- [ ] Palace Seal (far right) stays solid — no key exists

**Palace combat & traversal**
- [ ] Death gap at x27–28 kills (death pit), enemies turn at ledges
- [ ] Stair y3 → y5 → y7 → y9 climbable without dash; camera stays inside the palace
- [ ] Both palace rooms log as discovered while passing

**Regression:** Medina quests/gates/benches, boss fight, pause, hearts, combo, dialogue.

### Known issues / deferred

- No main-menu “Continue” yet: opening a different scene than the save's scene restores health but leaves the player at that scene's authored spot. The main menu / bootstrap scene is Phase 7 work.
- Transitions use a hard cut (no fade). A short fade would be a small polish item once the UI pass lands.
- The palace is all placeholder geometry/tints; no parallax or Moroccan wall art yet (art pass).
- The Lantern Keeper is a merchant with no conversation — flavour dialogue belongs to the Phase 7 NPC/dialogue pass.
- `PalaceSeal` is intentionally unopenable (next region teaser).
- The save version field is written but still not validated/migrated; deleting the file remains the reset.

---

## Phase 7 — Main menu (New Game / Continue)

### Scope delivered

- [x] **Title screen** (`Assets/Scenes/MainMenu.unity`) — dark background, gold “SOLEDAR” title, subtitle, New Game / Continue
- [x] **New Game** — deletes the save, starts a clean run in the Sunken Medina
- [x] **Continue** — loads the scene + spawn point recorded in the save (Silent Palace included) and restores health/position
- [x] **Build order** — MainMenu is now build scene 0 (TestScene / SunkenMedina / SilentPalace follow)

### How it works

- `MainMenuUI` lives on the menu Canvas and wires its buttons in `Start` (no persisted onClick lists).
- `Refresh()` enables Continue only when a save exists **and** its scene is in Build Settings; the status line reads
  “Continue — The Silent Palace” or “No journey recorded yet”.
- **New Game:** `SaveManager.DeleteSave()` → `SceneManager.LoadScene("SunkenMedina")`. The first room trigger writes a fresh save, so Continue is available afterwards.
- **Continue:** `SceneLoader.BeginTransition(savedScene, savedSpawnId)` — the same path used by doorways, so health, spawn ids and scene tagging behave identically. A save with no scene (pre-Phase-6) continues in SunkenMedina.
- The menu never touches `Time.timeScale` except to force it back to 1, so a paused/frozen clock can't leak into a new session.

```
MainMenu
  ├─ New Game ──→ delete save ──→ SunkenMedina (authored spawn)
  └─ Continue ──→ SceneLoader.BeginTransition(save.currentScene, save.spawnPointId)
                    └─ SaveApplier restores health + position/spawn in the loaded region
```

### New scripts / wiring

| Script | Role |
|--------|------|
| `UI/MainMenuUI.cs` (new) | New Game / Continue / status line / Continue availability |
| `Editor/MainMenuBootstrap.cs` (new) | Creates MainMenu.unity, title layout, camera, EventSystem, Canvas wiring; makes it build scene 0 |

### Setup tool — `Tools > Soledar > Setup Main Menu` (idempotent)

1. Creates `Assets/Scenes/MainMenu.unity` when missing (empty scene → camera + EventSystem + Canvas).
2. Builds/reuses `MenuCanvas`: dark full-screen background, `SOLEDAR` title (72 pt gold), subtitle, `NewGameButton`, `ContinueButton`, `StatusText`.
3. Adds `MainMenuUI` and wires the two buttons, status text and opening scene.
4. Rewrites Build Settings with MainMenu first, all other scenes preserved and enabled.

### Verification (Unity 6.3, MCP-driven, 2026-11-19)

- **Compile:** 0 errors after adding both scripts.
- **Scene:** roots `Main Camera | EventSystem | MenuCanvas`; hierarchy `MenuCanvas → MenuBackground, Title, Subtitle, NewGameButton(+Label), ContinueButton(+Label), StatusText`; all `MainMenuUI` references wired; build order `MainMenu, TestScene, SunkenMedina, SilentPalace`.
- **No save:** Continue `interactable = false`, status “No journey recorded yet”.
- **New Game:** SunkenMedina loaded at the plaza spawn, 5/5 HP, 0 dirhams, no key, Medina Gate closed — a clean run. The spawn-room auto-save creates a SunkenMedina save.
- **Continue (same region):** save tagged SunkenMedina → Continue enabled and loads the plaza spawn.
- **Continue (palace):** after a palace save, the menu showed “Continue — The Silent Palace”; clicking Continue loaded SilentPalace at the palace entry with 5/5 HP and the room re-discovered on arrival.
- **Cleanup:** test save deleted; editor left on the MainMenu scene.
- Not run: hands-on mouse/gamepad clicking of the buttons (methods invoked directly) and a standalone build launch (no player build was produced).

### Phase 7 test checklist (manual)

- [ ] Press Play in `MainMenu` with no save → Continue greyed out, “No journey recorded yet”
- [ ] Click New Game → Sunken Medina plaza, 5 hearts, Medina Gate closed
- [ ] Rest at a bench / collect something, quit Play, Play again → Continue enabled; click it → same region, saved checkpoint, saved health
- [ ] Travel to the Silent Palace, quit Play, Play again → status “Continue — The Silent Palace”; Continue drops you in the palace entry hall with the same health
- [ ] Delete the save (Tools > Soledar > Delete Save File) → reopen the menu, Continue is disabled again
- [ ] Mouse + gamepad navigation work on both buttons; disabled Continue cannot be clicked

### Known issues / deferred

- No “Quit” / Settings / Credits entries yet — the brief was New Game and Continue only.
- No way back to the menu from the pause menu (continue playing, quitting the app is the exit).
- Continue uses the same hard cut as doorways (no fade); menu UI is placeholder typography, no Moroccan ornament/art yet.
- The menu is a single layout for 16:9 (CanvasScaler scales it, but ultra-wide/portrait framing is untested).
- If the saved scene is renamed/removed, Continue falls back to the Sunken Medina and logs a warning.
- Unity writes a **zero GUID** for the MainMenu build-settings entry when the scene is created in the same editor session; the entry still resolves by path (`SceneUtility.GetBuildIndexByScenePath` → 0) and the GUID is filled in on the next editor launch.

---

---

## Phase 7.5 — UI refinement: hierarchy, contrast, focus (2026-11-19)

Goal: make the title screen and HUD read like one branded product without touching gameplay. Functionality, content and the Moroccan-influenced palette are unchanged; every restyle is a re-use of the same tokens.

### Shared tokens

`UI/SoledarUITheme.cs` is the single source of truth: ink backgrounds (`#0B0910` page, `#141019` panel, `#1D1824` raised), ivory content (`#EFE4CE`), gold reserved for actions and accents (`#E4C27A`), a muted tone for secondary copy (`#A29680`), and danger red (`#C4553D`) only for the boss bar. Type scale is size + weight only: display 84 / title 40 / heading 22 / body 21 / label 20 / small 15.

### Main menu

- Centred symmetry replaced by a **left-aligned reading column** (760×520 at x=160) with one 2 px gold margin rule — the only decoration on the screen.
- Order: title → gold rule (sits directly on the title's baseline box) → subtitle → two-line premise → hairline divider → actions → status line.
- **Content decides the hierarchy:** Continue is the gold primary when a save exists, otherwise New Game is; the other action is a quiet raised-panel secondary. Only the primary action gets a gold fill and ink bold label (12.6:1).
- Disabled Continue keeps a legible plate and a muted label (≈5:1) instead of fading to invisibility; disabled never shows a focus rule.
- **Focus states:** `UI/SelectableFocusVisual.cs` adds a short gold rule in the page margin plus a brighter label on hover/selection, and the primary action receives initial keyboard/gamepad focus. When anything is selected, a hovered sibling shows only its tint — the menu never shows two focus rules at once.
- Fixed a layout bug where the title Text used a bottom pivot and rendered 96 px above its block, which made the gold rule look detached.

### HUD

- **Hearts:** masks are 34 px ivory with a hard ink outline so they hold up over bright sandstone; spent masks stay visible as dimmed slots (max health is readable), instead of disappearing. The layout group only spaces them — it no longer collapses them to the sprite's preferred 10 px size. Renamed the legacy `"HeartsContainer "` (trailing space).
- **Dialogue:** solid ink panel (not translucent — 90 % alpha over bright tiles composites to mid-grey), 2 px gold hairline on top, gold bold speaker, ivory body at 21 px/1.2, small muted “E Continue” hint with a gold key-cap.
- **Interaction prompt:** ink plate + 1 px outline, gold `E` then the verb in ivory body text; the plate is ordered behind the text (idempotently) so re-running the tool cannot cover the label.
- **Pause:** solid ink overlay, gold `PAUSED` at title size, Resume as the gold primary and Quit as a quiet secondary; explicit up/down navigation, focus rules, and `defaultSelection = Resume` so a controller can unpause immediately.
- **Boss bar:** 18 pt bold gold title, 22 px opaque ink track, danger-red fill, 600 px wide.
- Every scene canvas uses `matchWidthOrHeight = 0.5` so the HUD holds proportion on both landscape and portrait.

### Tooling

`Tools > Soledar > Apply HUD Design (active scene)` (`Editor/SoledarUIPolish.cs`) applies the whole HUD pass to the active gameplay scene. It is idempotent and saves the scene; run it on any new region instead of restyling by hand.

### Verification (Unity 6.3, MCP-driven)

- **Compile:** 0 errors after all UI scripts and both tools.
- **Menu captures** at 1920×1080 and at 900×1600 (canvas 1080×1920) rendered through a temporary camera: title block 280–742 px, gold rule flush with the title box, buttons and status aligned to the action column.
- **Hierarchy swap:** with no save → New Game gold, Continue muted and non-interactable, focus on New Game. With a save → Continue gold and focused, status “Continue — The Sunken Medina”; New Game becomes the gold-label secondary.
- **Focus collision test:** hovering New Game while Continue is selected leaves `newGameBar = false`, `continueBar = true` — exactly one rule.
- **Pause:** opening selects Resume, `resumeBar = true`, `quitBar = false`; a real overlay frame is fully opaque and shows no world bleeding through.
- **HUD frames:** hearts 34 px at 32/24 margins with 8 px gaps; dialogue panel 220 px tall with the gold rule, gold speaker and ivory body; prompt plate with gold `E`; boss bar at 62 % fill. Verified in `SunkenMedina`; `SilentPalace` checked by value (opaque panels, 18 pt boss title, Resume default, scaler 0.5).
- **Contrast:** gold/ink 12.6:1, ink/gold 12.6:1, ivory/panel 14.9:1, muted/panel 6.45:1, gold/raised 11:1, disabled label ≈5:1.
- Not run: hands-on mouse/gamepad input (states driven programmatically), a standalone build, and a real portrait device.

### Known issues / deferred

- Typography is still the built-in legacy font; no TMP or custom typeface is installed, so the hierarchy relies on size/weight/colour.
- Scene art is placeholder, so mask silhouettes and the world's brightness are not final; the opaque-plate decision is based on the brightest placeholder tiles.
- The Game view dock in this editor session is 568×473, so captures were rendered through a temporary camera; foreground sprites placed in front of the UI plane can appear above the HUD in those captures (Screen Space overlays always draw last in play).

### Pause: return to main menu (2026-11-19)

- The pause panel now has three actions in reading order: **Resume** (gold primary), **Main Menu** (raised panel, gold label) and **Quit** (raised panel, muted label). Quit is deliberately the quietest of the three.
- Keyboard/gamepad navigation is one explicit vertical chain — Resume ⇄ Main Menu ⇄ Quit — so a controller is never stranded on a button, and `defaultSelection` is still Resume.
- `PauseMenu.ReturnToMainMenu()` clears the pause state and UI selection, checks the scene is in Build Settings, then asks `ScreenFader` to fade into `mainMenuScene` (`MainMenu`). The clock stays frozen while the ink closes; the title screen restores it in `MainMenuUI.Start`. If the fade cannot start, the clock is restored so the run is never left frozen with a hidden panel.
- The save file is left untouched: returning to the title screen behaves like quitting to the menu, and Continue resumes from the last checkpoint. No confirmation prompt — returning is one step, matching the rest of the pause menu.
- `Tools > Soledar > Apply HUD Design (active scene)` now creates the Main Menu button in scenes authored before it existed, wires `PauseMenu.mainMenuButton`, and re-applies tone, focus rule and navigation. Both regions were re-applied and saved.

**Verified:** `SunkenMedina` and `SilentPalace` both show `PauseTitle, ResumeButton, MainMenuButton, QuitButton`; nav chain and per-button focus rules correct (selecting Main Menu moved the rule off Resume). Live run: pause → Return to Main Menu → `MainMenu` active, `Time.timeScale = 1`, `IsPaused = false`, save present, Continue enabled and focused with “Continue — The Sunken Medina”. Test save deleted afterwards; editor left on `MainMenu`.

### Fade to the menu (2026-11-19)

- New `UI/ScreenFader.cs`: one persistent overlay, built on demand and marked `DontDestroyOnLoad`, so it survives the scene load instead of popping between scenes. It fades to brand ink, loads the destination, waits one frame for the new scene to render under the ink, then fades back in (0.22 s out / 0.28 s in by default, overridable per call).
- **Unscaled time** throughout — the pause menu holds `Time.timeScale = 0`, and the fade must still animate.
- The overlay sits at sorting order 32000 (above the HUD and pause panel) and sets `raycastTarget` while in flight, so a stray click cannot reach the screen behind it. `IsFading` guards state, and a single instance is reused for later transitions.
- The pause menu no longer hard-cuts: it validates `MainMenu`, hides the panel, then calls `ScreenFader.FadeToScene`. The world stays frozen behind the closing ink, which avoids a fifth of a second of enemies moving under the fade.

**Verified:** pause → Main Menu starts the fade (`IsFading = true`, `timeScale` still 0, overlay `raycastTarget = true`, sorting order 32000); afterwards the active scene is `MainMenu`, `IsFading = false`, overlay alpha 0 with raycast off, exactly one `ScreenFader` alive, and `timeScale = 1`. A slowed 30 s fade was captured mid-flight at alpha 0.47: the ink layer visibly dims the world above the HUD, confirming the overlay order. Not run: hands-on gamepad input during the fade (state driven programmatically).

Scope note: New Game and Continue still hard-cut; the fade is currently wired only to the pause menu return. Reusing `ScreenFader.FadeToScene` there is a one-line change when wanted.

### HUD: Ancient Dirham readout (2026-11-19)

Currency existed only in the save file: the merchant charged dirhams and pickups added them, but the player never saw a balance. That is a usability defect, not just a missing flourish.

- `SaveManager` gained `DirhamsChanged` (static, carries the new total) raised from `AddDirhams`, `TrySpendDirhams`, `DeleteSave` and `LoadFromFile`. One event keeps every source in sync without the HUD polling.
- New `UI/CurrencyUI.cs`: writes the balance into a legacy `Text` and pulses the gold coin mark on **gain only** — spending updates quietly, so the motion still means "you picked something up". Unscaled time, and the authored scale is cached in `Awake` so a scene unloaded mid-pulse cannot leave the mark stretched.
- `SoledarUIPolish` now builds the row idempotently on both gameplay scenes: a 12 px gold rhombus (zellige geometry, no glyph dependency) plus the count, in the left column under the masks. It sits left rather than top-right because the 620 px boss bar runs past the right edge at the 568 px game view.

**Verified (play mode):** fresh run shows the saved balance (`4`, then `10` after a gain); gain pulse observed mid-animation (`markScale 1.02`); `TrySpendDirhams` moved `7 → 4` with the label following; a direct scene load into `SilentPalace` initialised the HUD from the save and updated on gain; wiring (text/mark refs, `EFE4CE` label, `E4C27A` mark, 45° rotation, 8 px mark-to-count gap) read back from the scene. Visual check at 1920×1080 shows the diamond and count tight under the ivory masks. Not run: a real merchant purchase through the interaction prompt (the spend path was called directly).

**Defect caught during the pass:** `EnsureOutline(countGO, …)` referenced a variable scoped to the create branch, so the project assembly failed while the command assembly reported success — a reminder to read `GetConsoleLogs` after script edits.

### Echo Energy and Lantern healing (2026-11-22)

The combat loop had no reason to keep pressure up: masks healed for free at benches and fights cost nothing but masks. Echo Energy closes the loop — you earn the resource by landing hits, and you spend it standing still to heal, so a hard fight is survivable but not free.

**Controls:** hold `F` (keyboard) or `LB` (gamepad) for 0.6 s while grounded. One mask per focus, 33 energy each, out of 100; a hit grants 9 energy per enemy struck. Bar goes dim gold below 33, so "can I heal?" is readable without text.

- `Player/PlayerEnergy.cs`: capacity 100, gain from `PlayerAttack.AttackConnected` (raised with the number of enemies struck, so only real connections pay), `TrySpend`, `Refill`. Not persisted — a fresh entry starts at 0, a bench rest refills it.
- `Player/PlayerHealing.cs`: owns the focus state machine. Starts only when grounded, not dashing, missing a mask and holding enough energy; roots the player through `PlayerMovement.SetMovementLocked`; cancels on release, damage, stun, attack or death **before** energy is charged, so a broken focus never costs anything. Holding the button chains heals (Hollow Knight style).
- `PlayerInputReader` + `InputSystem_Actions.inputactions`: new `Heal` action (F / gamepad LB), added as a required action so a stale asset fails loudly instead of silently.
- `PlayerMovement`: `MovementLocked` gates horizontal input, jump buffer and dash (gravity and knockback still apply). Cleared by `ResetState` so a death mid-focus cannot leave the player frozen.
- `PlayerHealth.Heal(int)` returns the masks actually restored; `CanHeal` guards the whole path.
- `UI/EnergyUI.cs` + `SoledarUIPolish.StyleEnergy`: 140×8 track in the left stack (masks → energy → dirhams, currency moved to y −86). Event-driven, colours from the theme tokens.
- `ForgottenBench`: a rest refills energy alongside health.

The Lantern is part of Ziri's kit for now rather than a merchant item; the upgrade line (bigger heals, faster focus) stays open for the inventory phase.

**Verified (play mode):** bar starts empty and dim at 0 energy; `AddEnergy(100)` filled it and switched to gold; holding F with 3/5 masks restored two masks, spending exactly 66 energy; a damage event 1 s into a focus broke it and charged nothing (3 completed heals totalled 99 energy, matching a 100 start); a real swing on a dummy crawler hit for 3→2 HP and paid +9 energy into the meter; a bench rest took 70 → 100 energy and 3 → 5 masks, then saved; both gameplay scenes carry the bar, `EnergyUI`, `PlayerEnergy` and `PlayerHealing` (bar 140×8 at (32,−70), Filled/Horizontal, currency at −86); a 1920×1080 capture shows the three-row stack reading cleanly. Console clean, save deleted, editor left on `MainMenu` not playing, `runInBackground` reverted.

**Limitations:** the swing that granted energy was dispatched through the component (`SendMessage`) because synthetic attack input could not produce a reliable press edge from the editor bridge — the input path itself is the pre-existing, previously verified Jump/Attack one, but the final confirmation of "press J → energy" was not hands-on. Gamepad was not tested on hardware. Energy is intentionally not in the save file.

*Next: pause-menu Abilities panel, then the single-region map.*

### Pause menu: Abilities page (2026-11-22)

The pause menu only had Resume / Main Menu / Quit, so unlocked abilities were invisible outside a shop or a gate. This adds the first of the blueprint's pause submenus, using the panel, type scale and focus language already in place, and no new save data.

- `Core/AbilityCatalog.cs`: one source for display names and one-line descriptions. Dash, Wall Jump and the Lantern are always owned; Wind of the Atlas, Shadow Passage and Descent of Ash appear only when `SaveManager.HasAbility` says so. Unknown saved ids are ignored instead of shown raw.
- `UI/AbilitiesPanel.cs`: a full-screen page inside `PausePanel`. It rebuilds its rows on every open — name in heading size/ivory, description in small/muted, a 1 px rule between entries — so a new unlock needs no scene work. Focus starts on Back, whose navigation is `None` so a controller cannot wander into the pause buttons hidden underneath. A `Closed` event hands focus back to the Abilities button.
- `UI/PauseMenu.cs`: gains `abilitiesButton` / `abilitiesPanel`; opens the page, closes it on pause, resume and return-to-menu, and restores focus.
- `SoledarUIPolish`: adds the Abilities button between Resume and Main Menu (one explicit up/down chain), builds and wires the page idempotently on both regions, and moves `PausePanel` to the top of the Canvas so the HUD no longer draws over the pause surface.

**Verified (play mode, SunkenMedina and SilentPalace):** both regions start with the panel and page hidden; pause freezes `timeScale` at 0; a fresh kit shows 3 rows (Dash, Wall Jump, Lantern), and a save carrying `windOfAtlas` + `shadowPassage` shows 5, in catalog order; reopening does not duplicate rows (the first pass appended a second set because `Destroy` is deferred — rows are now detached before destruction); Back closes the page and returns focus to Abilities; resuming with the page still open restores `timeScale = 1` and hides both; the canvas ends with `PausePanel` as its last child in both scenes. A 1578×1314 UI-only capture shows the title, five rows and Back with the hearts/energy/currency fully covered.

**Limitations:** the empty-state line cannot trigger while the base kit supplies three entries, so it was only code-reviewed, not seen on screen. Gamepad focus/navigation was read from the EventSystem, not tested on hardware. The offscreen capture needed a UI-layer-only culling mask — rendering the world through the temporary camera otherwise drew scene geometry behind the page, which briefly looked like a UI artifact.

*Next: single-region room map driven by `discoveredRooms`.*

