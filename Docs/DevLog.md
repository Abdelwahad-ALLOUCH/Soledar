# SOLEDAR — Development Log

*Project: SOLEDAR — Echoes of a Forgotten Kingdom (Unity 6 LTS 6000.3.4f1, 2D Metroidvania)*

---

## Phase 1 — Foundation (in progress)

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

- Hit during dash: knockback is overwritten by dash velocity (prototype behaviour preserved). Fix in Phase 2 combat polish (block/cancel actions on damage).
- Enemy has no AI (idle/patrol/chase/attack) — Phase 2 enemy framework.
- Single-ray ground check can miss ledge edges — refine with 2–3 raycast foot check in Phase 2.
- Camera has no room bounds/confiner — added with the first real region (Phase 3).
- Non-uniform player scale (0.6, 1.2) in TestScene — clean up when real sprites arrive (keep uniform scale + collider size).
- Interact action exists and is bound but has no consumer yet — wired to NPCs/dialogue in Phase 2.
- Scene file still at `Assets/TestScene.unity` (relocate to `Assets/Scenes/` when the editor is closed).

---

*Next: Phase 2 — combat depth (3-hit combo, hit-stop, camera shake), enemy AI framework (idle/patrol/detect/chase/attack/death), damage reactions, dialogue groundwork.*
