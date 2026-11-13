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

*Next: Phase 6 — Silent Palace beyond the Palace Gate (scene transitions, ability-gated traversal, second ability from the merchant chain).*

