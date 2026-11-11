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

*Next: Phase 3 — first real region (tilemap level, camera confiner, benches/checkpoints as real interactables, doors/keys groundwork).*
