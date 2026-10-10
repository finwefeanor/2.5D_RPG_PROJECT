# 2.5D RPG Project - Developer Guide

A top-down 3D action RPG for Android, built in **Unity 6.5 (6000.5.8f1)** with **KayKit** skeleton
models and animations. Branch: `upgrade/unity6`.

This document explains how the code is organised, why it is organised that way, and how each
system works. Read sections 1-4 once; after that, use it as a reference.

> Written against commit `3d8180a` + the class-select / HUD patch + the local NavMesh enemy work.
> When the code and this guide disagree, the code wins - fix the guide in the same commit.

---

## Contents

1. [Quick start](#1-quick-start)
2. [Folder layout](#2-folder-layout)
3. [The five architecture rules](#3-the-five-architecture-rules)
4. [Startup order - what runs when](#4-startup-order---what-runs-when)
5. [The Player](#5-the-player)
6. [Classes (Mage / Barbarian / Rogue)](#6-classes-mage--barbarian--rogue)
7. [Combat - abilities, combos, animation events](#7-combat---abilities-combos-animation-events)
8. [Enemies](#8-enemies)
9. [Items, inventory, equipment, shop, saving](#9-items-inventory-equipment-shop-saving)
10. [UI](#10-ui)
11. [Input, camera, mobile controls](#11-input-camera-mobile-controls)
12. [Data assets (current values)](#12-data-assets-current-values)
13. [Editor tools](#13-editor-tools)
14. [How do I... (recipes)](#14-how-do-i-recipes)
15. [Working with patches](#15-working-with-patches)
16. [Known issues and legacy code](#16-known-issues-and-legacy-code)
17. [Roadmap](#17-roadmap)
18. [Script index (A-Z)](#18-script-index-a-z)

---

## 1. Quick start

| | |
|---|---|
| Unity | 6.5 - `6000.5.8f1` |
| Render pipeline | URP (GlobalVolume prefab) |
| Main test scene | `Assets/Scenes/newTestingScene.unity` |
| Target | Android (also builds for Linux / Windows Mono for testing) |
| Input | Old Input Manager (`Input.GetKeyDown`, `Input.GetAxisRaw`) + on-screen joystick / buttons |

**Controls (keyboard)**

| Key | Action | Handled by |
|---|---|---|
| WASD / arrows | Move | `PlayerController` |
| Space | Attack (press again mid-swing = combo) | `PlayerAttack` |
| E | Open / close shop (near the merchant) | `ShopKeeperInteraction` |
| Tab | Open / close inventory | `InventoryUIController` |
| Esc | Pause menu (Save / New Game / Restart / Quit) | `PauseMenu` |
| C | Re-open the class picker (editor + development builds only) | `ClassSelectMenu` |

**Controls (mobile)**: `VirtualJoystick` (move), Attack button, Interact button - all on `HUDCanvas`.

**Building an empty scene from scratch**: `RPG Scene Builder -> Build MVP Scene` spawns all core
prefabs. The ground needs a `NavMeshSurface` and a baked NavMesh for enemies to move.

---

## 2. Folder layout

```
Assets/
  Scripts/            runtime code (all MonoBehaviours, ScriptableObjects, static classes)
  Editor/             editor-only tools (menu items, builders, AttackEventTool)
  Data_Abilities/     AbilityDefinition assets   (Unarmed_Punch, Attack_1H_Slice, ...)
  Data_Classes/       CharacterClassDefinition assets (Class_Mage, Class_Barbarian, Class_Rogue)
  Data_Items/         ItemData assets            (Axe, Blade, Shield, Hat_For_Mage, ...)
  Animators/          MageAnimationController (shared by ALL player visuals),
                      EnemyAnimationController
  Prefabs/
    Core/             everything Build MVP Scene spawns:
                      GameManager, Player, Merchant, Enemy_0_with_sword (+ Boss_Variant),
                      ShopCanvas, PlayerHealthBarCanvas, PauseMenuCanvas, HUDCanvas, GlobalVolume
    *PlayerVisual     MagePlayerVisual, BarbarianPlayerVisual, RoguePlayerVisual
                      (spawned at runtime by PlayerClass - NOT placed in scenes)
    ItemButtonPrefab, InventoryRowPrefab, CubeCollectible (gold pickup)
  Scenes/             newTestingScene (current), SampleScene (+ backup), scene2
```

---

## 3. The five architecture rules

Every design decision in this project follows from these. If a change breaks one of them, it is
probably the wrong change.

### Rule 1 - Prefabs never reference other prefabs

Unity cannot save a reference from one prefab to an object inside another prefab (for example,
`Merchant.prefab -> ShopCanvas/GoldText`). It works in the scene and silently becomes `None` when you
spawn the prefab somewhere else. So **no Inspector drag-links cross a prefab boundary**. Instead,
scripts find each other at runtime in one of three ways:

| Need | Use | Example |
|---|---|---|
| "Give me the player" | `GameManager.Instance.Player` (a `PlayerRefs`) | `Enemy.ResolvePlayer()` |
| "Something happened, whoever cares should react" | `GameEvents` static events | gold changed -> HUD, shop |
| "Give me a UI object inside canvas X" | A `...Refs` component on that canvas root with a static `Instance` | `ShopUIRefs.Instance.goldText` |

References **inside** one prefab are fine and are drag-linked normally (that is what the `Refs`
components hold).

### Rule 2 - One place decides each thing

Each question has exactly one owner. Everyone else asks the owner.

| Question | The one place |
|---|---|
| Which attack does the player do right now? | `EquipmentManager.CurrentAttack` |
| May this player use this item? | `EquipmentManager.CanUse(item)` -> `ItemData.CanBeUsedBy(class)` |
| Which model / animator does the player have? | `PlayerClass.BindVisual()` |
| What is shown on the model (hat, cloak, weapon)? | `CharacterVisualController.RefreshVisuals()` |
| What are the player's base stats? | `PlayerClass.ApplyStats()` from the `CharacterClassDefinition` |
| What does an attack look / sound / hit like? | Its `AbilityDefinition` asset |

### Rule 3 - The animation is the clock

Gameplay code never contains clip lengths or "wait 0.4 seconds then deal damage" timers.

- Code only **requests** an action (`animator.SetTrigger("Attack")`).
- The **Animator** reports when the action really starts / ends, through `StateMachineBehaviour`s
  (`PlayerAttackState`, `EnemyAttackState`) on the state nodes.
- The **contact frame** inside the clip fires Animation Events (`PlayAttackSound`, `DealAttackDamage`)
  that do the actual damage and sound.

Result: change a clip's speed, swap a clip, or add a 3-second attack, and the timing stays correct
with zero code changes.

### Rule 4 - Data lives in ScriptableObjects, not in `if` statements

There is no `if (class == Mage)` or `if (weapon == Axe)` anywhere. A class, an attack and an item are
each an asset. New content = new asset, not new code.

- `CharacterClassDefinition` - a class
- `AbilityDefinition` - an attack
- `ItemData` - an item

### Rule 5 - UI listens, it never polls

UI scripts subscribe to `GameEvents` in `OnEnable` and unsubscribe in `OnDisable`. They also do one
**pull** in `Start` (read the current value directly) because an event may have fired before they
subscribed. This "subscribe + initial pull" pattern is used by `PlayerStatsHUD`, `PlayerHealthUI`,
`ShopManager` and `InventoryUIController`.

---

## 4. Startup order - what runs when

Unity runs all `Awake`s, then all `OnEnable`s, then all `Start`s. The order *between* scripts is not
guaranteed unless forced, so the code is written to not depend on it - with one deliberate exception:
`PlayerClass` has `[DefaultExecutionOrder(-100)]` so it always runs first.

```
AWAKE
  PlayerClass (-100)       spawn class visual -> BindVisual -> ApplyStats
                           (old visual is deactivated first, so nobody finds it)
  GameManager              becomes the singleton
  PlayerRefs               caches components, GameManager.RegisterPlayer(this)
  EquipmentManager         caches PlayerClass
  InventoryManager         caches EquipmentManager
  CharacterVisualController subscribes to OnEquipmentChanged
  ...Refs components       set their static Instance
  PlayerStatsHUD           builds its Text label

ONENABLE
  UI scripts               subscribe to GameEvents

START
  PlayerClass              GameEvents.PlayerClassChanged(class)      -> HUD shows class name
  InventoryManager         SaveSystem.Load(...) then GoldChanged(gold) -> HUD / shop show gold
  PlayerHealth             GameEvents.PlayerHealthChanged(...)       -> health bar
  Enemy                    ResolvePlayer() via GameManager.Instance.Player
  ShopManager / InventoryUIController / PauseMenu  resolve their UI via ...Refs.Instance
  ClassSelectMenu          builds its canvas, opens (Time.timeScale = 0)
  PlayerStatsHUD           initial pull of gold + class
```

`GameManager.Instance` has a lazy getter (`FindAnyObjectByType` if not set yet), so
`PlayerRefs.Awake` can register even if `GameManager.Awake` has not run yet.

---

## 5. The Player

### 5.1 Structure

```
Player  (prefab: Prefabs/Core/Player)          <- all LOGIC lives here
  Rigidbody, CapsuleCollider, tag "Player"
  PlayerClass             which class, spawns the visual, applies stats
  PlayerRefs              cache of the components below + registers with GameManager
  PlayerController        movement
  PlayerAttack            attacks + combos
  PlayerHealth            health, damage, death
  EquipmentManager        what is equipped, current attack, class restrictions
  InventoryManager        owned items + gold
  CharacterVisualController  shows equipment on the model
  InventoryUIController   inventory panel rows
  ItemDatabase            name -> ItemData lookup (for loading saves)
  AttackPoint (child)     centre of the attack hit sphere

  <ClassName>PlayerVisual  (spawned at runtime by PlayerClass)  <- all VISUALS live here
    Animator  (MageAnimationController - shared by every class)
    CharacterVisual        refs into this model: animator, hand sockets, head/chest pieces
    PlayerAnimationEventRelay  forwards Animation Events up to the Player root
    CharacterMotor         (legacy root-motion helper, see section 16)
    KayKit skeleton mesh + bones (handslot.r / handslot.l)
```

**Why the split?** Animation Events can only call methods on components that sit on the **same
GameObject as the Animator**. Logic lives on the root so swapping the visual (class change) doesn't
destroy any game state. The relay bridges the two.

### 5.2 `PlayerRefs` - the player's address book

Sits on the Player root. In `Awake` it caches `Health`, `Attack`, `Controller`, `Inventory`,
`Equipment`, `Class`, `Animator` and registers itself: `GameManager.Instance.RegisterPlayer(this)`.

Any script anywhere gets the player with:

```csharp
PlayerRefs player = GameManager.Instance != null ? GameManager.Instance.Player : null;
if (player != null) player.Inventory.AddGold(5);
```

`SetAnimator(Animator)` is called by `PlayerClass` when the visual is swapped at runtime, so
`PlayerRefs.Animator` never points at a destroyed animator.

**Adding a new player component:** add a property here and one `GetComponent` line in `Awake`. This
is the only file that changes.

### 5.3 `PlayerController` - movement

- `Update`: reads `Horizontal` / `Vertical` axes; if both are ~0, reads the `VirtualJoystick` instead.
  Sets the Animator bool `isMoving`.
- `FixedUpdate`: rotates toward the move direction (`turnSpeed` deg/s) and sets
  `rb.linearVelocity = direction * moveSpeed` (keeps gravity's Y).
- `rootMotionActive` makes it skip `FixedUpdate` (only used by the legacy `CharacterMotor`).
- `moveSpeed` is overwritten by the class (`ApplyStats`).

Movement is **not** blocked while attacking. Attack clips use *Bake Into Pose* so the clip itself
doesn't move the character; the Rigidbody does.

### 5.4 `PlayerHealth` - health, damage, death

- `TakeDamage(damage)`:
  `mitigated = max(1, damage - EquipmentManager.GetTotalDefense())` - armour can never reduce a hit
  below 1. Fires `GameEvents.PlayerHealthChanged`, sets the Animator `Hit` trigger.
- The hit **sound** is not played here - it comes from the Animation Event `PlayHitSound` on the
  `Hit_A` clip, so it matches the visible flinch.
- `Die()`: disables `PlayerController` and `PlayerAttack`, sets `isDead` + `Death` on the Animator,
  fires `GameEvents.PlayerDied` (enemies stop attacking), shows the death panel from
  `PlayerUIRefs`, waits for the death sound, then **reloads the scene**.
- `maxHealth` / `health` are overwritten by the class at startup and on a class switch (full heal).

---

## 6. Classes (Mage / Barbarian / Rogue)

### 6.1 The pieces

| Piece | Type | Job |
|---|---|---|
| `CharacterClassDefinition` | ScriptableObject (`Create -> RPG -> Character Class`) | Describes one class: name, description, icon, visual prefab, level-1 stats, unarmed attack |
| `PlayerClass` | MonoBehaviour on Player root | Applies a class: spawns visual, binds it, applies stats. `SetClass()` for runtime switches |
| `CharacterVisual` | MonoBehaviour on each visual prefab root | Holds refs **inside** the model: `animator`, `rightHandSocket`, `leftHandSocket`, `headPiece`, `chestPiece` |
| `ClassSelectMenu` | MonoBehaviour on GameManager prefab | "Choose your class" screen |

`CharacterClassDefinition` fields:

```
className, description, icon
visualPrefab      CharacterVisual prefab spawned under the Player (null = keep the prefab's own visual)
maxHealth, baseAttackDamage
attackSpeed       multiplies every ability's animationSpeed (Rogue 1.25 = 25% faster swings)
moveSpeed
unarmedAttack     AbilityDefinition used when the right hand is empty
```

### 6.2 What `PlayerClass` does at startup (`Awake`, order -100)

1. **SpawnVisual** - finds the existing `CharacterVisual` child (the Mage visual baked into the
   Player prefab), remembers its local position / rotation / scale, **deactivates** it (Destroy only
   happens at the end of the frame - deactivating hides it from `GetComponentInChildren` this frame),
   destroys it and instantiates `classDefinition.visualPrefab` in its place.
2. **BindVisual** - the *one place* that connects the visual to the root scripts:
   `PlayerController.animator`, `PlayerAttack.SetAnimator`, `PlayerHealth.SetAnimator`,
   `PlayerRefs.SetAnimator`, `CharacterVisualController.BindVisual(v)` (sockets + head/chest pieces).
3. **ApplyStats** - health (full), base damage, attack speed, move speed, unarmed attack.

In `Start` it fires `GameEvents.PlayerClassChanged` so the HUD can show the class name.

### 6.3 Switching class at runtime - `PlayerClass.SetClass(newClass)`

Called by `ClassSelectMenu`. Order matters:

| # | Step | Why |
|---|---|---|
| 1 | `PlayerAttack.ResetAttackState()` | The old Animator is about to be destroyed mid-swing; its `OnStateExit` will never arrive, so the attack counters would stay stuck at "attacking" forever |
| 2 | `SpawnVisual` | New model |
| 3 | `BindVisual` | Everyone now points at the new Animator / sockets / pieces |
| 4 | `ApplyStats` | New stats, **full heal** |
| 5 | `EquipmentManager.UnequipUnusable()` | Mage hat on a Barbarian -> unequipped (still owned) |
| 6 | `CharacterVisualController.Refresh()` | Re-spawn weapons / show pieces on the **new** model |
| 7 | `GameEvents.PlayerHealthChanged` | Health bar shows the new max |
| 8 | `GameEvents.PlayerClassChanged` | HUD name, shop labels ("Mage only"), inventory labels |

### 6.4 Visual prefabs

| Prefab | Source model | Head piece | Chest piece |
|---|---|---|---|
| `MagePlayerVisual` | KayKit Skeleton_Mage | `Skeleton_Mage_Hat` | mage outfit object |
| `BarbarianPlayerVisual` | KayKit Skeleton_Warrior (Humanoid rig) | `Skeleton_Warrior_Helmet` | `Skeleton_Warrior_Cloak` |
| `RoguePlayerVisual` | KayKit Skeleton_Rogue (Humanoid rig) | `Skeleton_Rogue_Hood` | `Skeleton_Rogue_Cape` |

Every visual root needs: `Animator` (MageAnimationController), `CharacterVisual`,
`PlayerAnimationEventRelay`, `CharacterMotor`. Adding `CharacterVisual` (or right-click -> Reset)
auto-fills the Animator and the `handslot.r` / `handslot.l` sockets.

The head / chest pieces are **parts of the model** that start hidden and are switched on when an item
in that slot is equipped. The chest piece gets its **own material copy** (in
`CharacterVisualController.BindVisual`) so tinting it with the item's `outfitColor` doesn't recolour
every skeleton sharing the KayKit material.

### 6.5 Class select screen - `ClassSelectMenu`

- Lives on `GameManager.prefab`. Inspector: `classes` (array), `showOnStart`, `reopenKey` (C).
- Builds its **own** UI in code (Canvas, sortingOrder 50, scaler 1920x1080): a dim full-screen panel,
  a title, and one clickable card per class showing name, description and stats. Nothing to wire.
- `Open()` saves and sets `Time.timeScale = 0`; `Choose(c)` calls
  `GameManager.Instance.Player.Class.SetClass(c)` then `Close()` restores the time scale.
- The reopen key should only work in the editor / development builds (wrap the `Update` body in
  `#if UNITY_EDITOR || DEVELOPMENT_BUILD`). In a release build a mid-game switch would be a free full
  heal and would break per-class progression.

---

## 7. Combat - abilities, combos, animation events

### 7.1 `AbilityDefinition` - one attack as data

`Create -> RPG -> Ability`. Fields:

| Field | Meaning |
|---|---|
| `animatorIndex` | Written to Animator int `AttackIndex`; picks the clip inside `Attacks_SubState`. 0-9 = one-handed, 10-19 = unarmed |
| `animationSpeed` | Clip playback speed. Multiplied by the class's `attackSpeed` and written to Animator float `AttackSpeed` |
| `damageMultiplier` | x `PlayerAttack.baseAttackDamage` |
| `range` | Radius of the hit sphere around `AttackPoint` |
| `nextInCombo` | Attack that follows if the player presses again mid-swing (null = no combo). Must use a different clip |
| `comboInputOpens` | Normalized time (0-1) from which a press is remembered. Earlier presses are ignored, so mashing doesn't chain |
| `comboChainAt` | Normalized time at which a remembered press starts the next attack. Keep it **after** the contact frame |
| `swingSounds[]` | Random one plays every swing, hit or miss |
| `hitSounds[]` | Random one plays only when something was hit (empty = the AudioSource's own clip) |

**Where the player's attack comes from**: `EquipmentManager.CurrentAttack` = the right-hand item's
`attack` if it has one, otherwise the class's `unarmedAttack`.

### 7.2 Player Animator (`MageAnimationController`, shared by all classes)

Parameters: `isMoving` (bool), `Attack` (trigger), `AttackIndex` (int), `AttackSpeed` (float),
`ComboNext` (trigger), `Hit` (trigger), `Death` (trigger), `isDead` (bool).

```
Base Layer
  Idle / Walking / Running  (isMoving)
  Hit_A, Death_A ...
  AnyState --Attack-------> Attacks_SubState
  AnyState --ComboNext----> Attacks_SubState
  Attacks_SubState   (has the PlayerAttackState behaviour on the sub-state node)
     Entry --AttackIndex==0--> Melee_1H_Attack_Slice_Horizontal
     Entry --AttackIndex==1--> Melee_1H_Attack_Jump_Chop
     Entry --AttackIndex==10-> Melee_Unarmed_Attack_Punch_A
     Entry --AttackIndex==11-> Melee_Unarmed_Attack_Kick
     every state: Speed multiplier = AttackSpeed parameter
```

Clip import settings for attacks: root transform rotation / position Y / position XZ all
**Bake Into Pose**, *Based Upon* = Original. The Animator has *Animate Physics* ticked
(Update Mode stays Normal).

### 7.3 One attack, start to finish

```
Space / Attack button
  -> PlayerAttack.TryAttack()
       already attacking?  -> QueueComboInput()  (see 7.4)
       still in recovery?  -> ignore
       attack = EquipmentManager.CurrentAttack
  -> StartAttack(attack, "Attack")
       activeAttack = attack                        (snapshot - equipping mid-swing can't change it)
       Animator: AttackSpeed = attack.animationSpeed * attackSpeed
                 AttackIndex = attack.animatorIndex
                 SetTrigger(Attack)
       NO damage, NO sound, NO timer here

Animator enters the attack state
  -> PlayerAttackState.OnStateEnter  -> PlayerAttack.OnAttackAnimationStart(stateHash)
  -> PlayerAttackState.OnStateUpdate -> PlayerAttack.OnAttackAnimationUpdate(hash, normalizedTime)  (every frame)

Contact frame (Animation Events on the clip)
  -> PlayerAnimationEventRelay.PlayAttackSound  -> PlayerAttack.PlayAttackSound   (swing sound, random pitch)
  -> PlayerAnimationEventRelay.DealAttackDamage -> PlayerAttack.DealAttackDamage
       damage = round(baseAttackDamage * activeAttack.damageMultiplier) + sum(item.damageBonus)
       Physics.OverlapSphere(AttackPoint, activeAttack.range, enemyLayers)
       Enemy.TakeDamage(damage) on each hit
       hit anything? -> one hit sound (once per swing, not per enemy)

Animator leaves the attack state
  -> PlayerAttackState.OnStateExit -> PlayerAttack.OnAttackAnimationEnd()
       nextAttackTime = now + attackRecovery     (recovery starts when the LAST swing ends)
```

### 7.4 Combos

Example: `Unarmed_Punch -> Unarmed_Kick -> Unarmed_Punch -> ...` (each points at the other).

1. During a swing, a press calls `QueueComboInput()`. It is ignored if
   `swingTime < comboInputOpens` (too early = mashing) or there is no `nextInCombo`.
2. Otherwise `comboQueued = true`. Every frame, `TryChainCombo()` checks: has the swing reached
   `comboChainAt`? If yes, `StartAttack(nextInCombo, "ComboNext")`.
3. `ComboNext` is an **AnyState** transition into the sub-state machine, so the next clip crossfades
   in from the middle of the current one.
4. If the player swapped weapons mid-combo (`CurrentAttack != comboRoot`), the queued press is dropped.

**The crossfade problem and `attackStatesActive`**: during a crossfade Unity *enters* the new state
before it *exits* the old one. A simple `isAttacking` bool would be set true by the new state and then
false by the old one's exit - in the middle of the new swing. So `PlayerAttack` counts:
`attackStatesActive++` on enter, `--` on exit, and only treats the chain as over when it reaches 0.
`OnAttackAnimationUpdate` also ignores the older state (`stateHash != currentSwingHash`) so only the
newest swing's time drives the combo window.

When the chain ends (finished, or interrupted by `Hit_A` / death), the `ComboNext` trigger is reset so
it can't fire later by surprise.

### 7.5 Animation Events and relays

Animation Events call methods by name on components **on the Animator's GameObject**. Logic lives on
the parent, so each character has a relay on the visual:

| Relay (on the visual) | Forwards to | Methods |
|---|---|---|
| `PlayerAnimationEventRelay` | `PlayerAttack`, `PlayerHealth` | `PlayAttackSound`, `DealAttackDamage`, `PlayHitSound` |
| `AnimationEventRelay` (enemies) | `Enemy` | `PlayAttackSound`, `DealAttackDamage`, `PlayHitSound` |

`Hit_A` is shared by player and enemies, which is why both relays have `PlayHitSound`.

Events are stored in the FBX `.meta` (clip import settings) as normalized time, so they scale with
playback speed. Use **Tools -> RPG -> Attack Events** to place them (section 13).

### 7.6 StateMachineBehaviours

| Behaviour | On | Calls |
|---|---|---|
| `PlayerAttackState` | `Attacks_SubState` node in MageAnimationController | `PlayerAttack.OnAttackAnimationStart / Update / End` |
| `EnemyAttackState` | Attack state in EnemyAnimationController | `Enemy.OnAttackAnimationStart / End` |

Both find their target with `animator.GetComponentInParent<...>()` and cache it.

---

## 8. Enemies

### 8.1 Structure

```
Enemy_0_with_sword  (Prefabs/Core)
  Enemy, NavMeshAgent, CapsuleCollider, AudioSources
  EnemyHealthBar (world-space canvas child)
  Skeleton_Minion (visual)
    Animator (EnemyAnimationController, EnemyAttackState on the Attack state)
    AnimationEventRelay
Enemy_0_with_sword_Boss_Variant   prefab variant: bigger, more HP, neverFlinch, 360 degree swing
```

### 8.2 Behaviour (`Enemy.Update`)

```
dead / player dead / no player / mid-swing  -> do nothing
distance = EdgeDistanceToPlayer()           (centre distance - own radius - player radius)
distance <= attackRange   -> TickAttack()
distance <= detectRange   -> TickChase()
otherwise                 -> idle
```

- **Edge distance**: ranges are measured between the capsules' surfaces, so a scaled-up boss doesn't
  need a bigger `attackRange` just because its body is wider. Radii include `lossyScale`.
- **TickChase**: `NavMeshAgent.SetDestination(player)`. The agent paths around obstacles (boxes,
  walls). `stoppingDistance = max(attackRange, 0.05) + bodyRadius + playerRadius` so the agent stops
  at the edge of attack range instead of pushing into the player.
- **Stopping**: when attacking / idle the agent is stopped and its `velocity` is zeroed, so it doesn't
  slide into the player with leftover momentum. `isOnNavMesh` is checked before every agent call.
- **TickAttack**: stop, `FacePlayer()` (`turnSpeed` deg/s). Swing only when recovery is over **and** the
  player is inside the `attackAngle` cone in front. Sets `AttackIndex` + `Attack` trigger - nothing else.
- **DealAttackDamage** (contact-frame event): the swing is committed but can still miss:
  - player stepped back beyond `attackRange + dodgeMargin` -> miss
  - player outside the `hitAngle` arc -> miss (360 = full circle, used by the boss)
  - otherwise `PlayerHealth.TakeDamage(attackDamage)`
- **Flinch**: `TakeDamage` always removes health, but plays `Hit_A` at most once per `flinchImmunity`
  seconds - so a combo can't stun-lock an enemy forever. `neverFlinch` disables it (bosses).
- **Death**: drops a `CubeCollectible` gold pickup worth `goldReward`, plays the death animation,
  destroys itself after 2.5 s.
- **Player death**: listens to `GameEvents.OnPlayerDied` and stops attacking the corpse.

The ground needs a `NavMeshSurface` (AI Navigation package). **Re-bake** it after moving obstacles.

### 8.3 Inspector values that matter

| Field | Effect |
|---|---|
| `attackRange` | Edge-to-edge distance at which it starts swinging |
| `detectRange` | Distance at which it starts chasing |
| `attackRecovery` | Pause after a swing ends before the next can start |
| `attackAngle` | Must face the player within this cone to *start* a swing |
| `hitAngle` | Player must be inside this arc at the contact frame to *get hit* |
| `dodgeMargin` | How far past `attackRange` a committed swing still connects |
| `flinchImmunity` / `neverFlinch` | Stun-lock protection |
| `moveSpeed`, `turnSpeed` | Chase speed / facing speed |
| `goldReward`, `goldPickupPrefab` | Loot |

### 8.4 `EnemyHealthBar`

World-space slider above the enemy. Subscribes to the enemy's own `OnHealthChanged` (per-enemy C#
event, not `GameEvents`) and billboards toward the camera in `LateUpdate`.

---

## 9. Items, inventory, equipment, shop, saving

### 9.1 `ItemData` (ScriptableObject, `Create -> RPG -> Item`)

| Field | Meaning |
|---|---|
| `itemName`, `icon`, `description` | Identity. **`itemName` is the save key** - renaming breaks old saves |
| `price` | Shop price |
| `slot` | `None, Head, Chest, RightHand, LeftHand` |
| `defenseBonus` | Subtracted from incoming damage (min 1 damage) |
| `damageBonus` | Added to every player hit |
| `attack` | `AbilityDefinition` this item grants in the RightHand slot |
| `allowedClasses` | Empty = every class. Otherwise only these classes may buy / equip |
| `equipPrefab`, `equipPositionOffset`, `equipRotationOffset` | Weapon / shield model spawned at the hand socket |
| `outfitColor` | Tint for the chest piece and the shop swatch |

`CanBeUsedBy(class)` and `AllowedClassNames()` ("Mage, Rogue" / "Any") live on `ItemData`.

### 9.2 Ownership vs equipment

| Script | Owns |
|---|---|
| `InventoryManager` | The list of **owned** items and **gold**. `AddGold`, `SpendGold`, `AddItem` (auto-equips into its slot), `RemoveItem`, `EquipOwnedItem`, `OwnsItem` |
| `EquipmentManager` | A `Dictionary<EquipSlot, ItemData>` of **equipped** items. `Equip` (refuses unusable items), `Unequip`, `UnequipUnusable`, `GetTotalDefense`, `GetTotalDamageBonus`, `CurrentAttack`, `CanUse`. Fires `OnEquipmentChanged` |
| `CharacterVisualController` | Listens to `OnEquipmentChanged`. Shows / hides head + chest pieces, tints the chest, spawns weapon / shield prefabs at the hand sockets |

An item can be owned but not equipped (bought a second hat, or switched to a class that can't use it).

### 9.3 Shop

```
Merchant (prefab)
  ShopKeeperInteraction   trigger collider; E / Interact button toggles the shop while in range,
                          auto-closes when the player walks away
  ShopUIController        opens / closes ShopUIRefs.shopPanel
  ShopManager             itemsForSale list -> one button per item

ShopCanvas (prefab)
  ShopUIRefs              shopPanel, goldText, itemButtonContainer, itemButtonPrefab, close button,
                          inventoryPanel, inventoryRowContainer, inventoryRowPrefab, inventory close button
```

`ShopManager`:
- Builds one button per `itemsForSale` entry. Label 2 = `"Price: 10"` or `"Barbarian only"`.
- `TryBuyItem`: refuses if the class can't use it, already owned, or not enough gold; otherwise
  `SpendGold` + `AddItem` (which equips).
- Button is interactable only if not owned, affordable, and usable.
- Rebuilds on `OnPlayerClassChanged`, refreshes states on `OnGoldChanged`.

Both panels sit inside a vertical ScrollRect (see `ScrollablePanelLayout`, section 13), so any
number of items fits.

### 9.4 Inventory panel - `InventoryUIController`

On the Player. Tab or the close button toggles it. One row per owned item: name + `"Equipped"` /
`"Mage only"` / blank. Clicking a row equips or unequips it. Redraws on `OnInventoryChanged`,
`OnEquipmentChanged` and `OnPlayerClassChanged`.

### 9.5 Gold

- Enemies drop a `GoldPickup` (`CubeCollectible` prefab: spins, bobs). On trigger with the `Player`
  tag -> `InventoryManager.AddGold(amount)`.
- Every gold change fires `GameEvents.GoldChanged(gold)` -> HUD + shop.

### 9.6 Saving - `SaveSystem` (static, PlayerPrefs)

| Key | Value |
|---|---|
| `SaveGold` | int |
| `SaveOwnedItems` | comma-separated `itemName`s |
| `SaveEquipped_<Slot>` | `itemName` or empty, one per slot |

- **Save**: only from the pause menu's Save button.
- **Load**: `InventoryManager.Start`. Resolves names via `ItemDatabase.GetByName`, re-adds owned items,
  then re-applies the saved equipment exactly.
- **Clear**: pause menu New Game, or `RPG Scene Builder -> Clear Save Data`.
- Not saved yet: class, health, level, position (Phase B adds class + level + XP).
- `ItemDatabase` (on the Player) must list every `ItemData` - use `RPG Scene Builder -> Refresh Item
  Database` after creating items.

---

## 10. UI

| Canvas prefab | Scripts | Contents |
|---|---|---|
| `PlayerHealthBarCanvas` | `PlayerHealthUI`, `PlayerStatsHUD`, `PlayerUIRefs` | Health slider, class + gold text under it, death panel |
| `ShopCanvas` | `ShopUIRefs` | Shop panel, inventory panel |
| `PauseMenuCanvas` | `PauseMenu`, `PauseUIRefs` | Pause panel with Save / New Game / Restart / Quit |
| `HUDCanvas` | `VirtualJoystick`, `AttackButtonRelay`, `InteractButtonRelay` | Mobile controls |
| (built in code) `ClassSelectCanvas` | `ClassSelectMenu` on GameManager | Class picker |

- **`PlayerHealthUI`**: slider driven by `GameEvents.OnPlayerHealthChanged`.
- **`PlayerStatsHUD`**: builds a two-line Text (`Mage` / gold-coloured `Gold: 120`) anchored under the
  `Slider` child's bottom-left corner. Listens to `OnGoldChanged` and `OnPlayerClassChanged`. Phase B
  adds the level here.
- **`PauseMenu`**: Esc toggles; `Time.timeScale = 0` while paused. Restart / New Game reset the time
  scale before reloading (otherwise the new scene loads frozen).

### `GameEvents` - the full list

| Event | Fired by | Listened to by |
|---|---|---|
| `OnPlayerHealthChanged(current, max)` | `PlayerHealth`, `PlayerClass.SetClass` | `PlayerHealthUI` |
| `OnGoldChanged(gold)` | `InventoryManager` | `PlayerStatsHUD`, `ShopManager` |
| `OnInventoryChanged()` | `InventoryManager` | `InventoryUIController` |
| `OnPlayerClassChanged(class)` | `PlayerClass` | `PlayerStatsHUD`, `ShopManager`, `InventoryUIController` |
| `OnPlayerDied()` | `PlayerHealth` | `Enemy` |
| `OnAttackPressed()` | `AttackButtonRelay` (UI button) | `PlayerAttack` |
| `OnInteractPressed()` | `InteractButtonRelay` (UI button) | `ShopKeeperInteraction` |

Pattern for a new event: add `public static event Action<T> OnX;` and
`public static void X(T v) => OnX?.Invoke(v);`. Always unsubscribe in `OnDisable` - static events
outlive scene reloads, so a missing `-=` keeps a destroyed object subscribed.

---

## 11. Input, camera, mobile controls

- **Keyboard**: each script reads its own key (table in section 1).
- **Mobile buttons**: a UI Button's `OnClick` calls a relay on the same canvas (`AttackButtonRelay`,
  `InteractButtonRelay`), which fires a `GameEvents` event. The canvas therefore never references the
  Player prefab (rule 1).
- **`VirtualJoystick`**: drag handler on a UI image; exposes `Horizontal` / `Vertical` and a static
  `Instance`. `PlayerController` reads it when the keyboard axes are idle.
- **`TopDownCameraController`** (on Main Camera, in the scene): follows
  `GameManager.Instance.Player` with `offset (0, 22, -14)`, `pitchAngle 58`, smooth lerp in
  `LateUpdate`. Snaps to the player on the first frame.

---

## 12. Data assets (current values)

### Classes

| Class | HP | Base dmg | Attack speed | Move speed | Unarmed attack | Visual |
|---|---|---|---|---|---|---|
| Mage | 30 | 10 | 1.0 | 5.0 | Unarmed_Punch | MagePlayerVisual |
| Barbarian | 50 | 14 | 0.9 | 4.6 | Unarmed_Punch | BarbarianPlayerVisual |
| Rogue | 25 | 9 | 1.25 | 5.6 | Unarmed_Punch | RoguePlayerVisual |

### Abilities

| Ability | AttackIndex | Anim speed | Dmg mult | Range | Combo |
|---|---|---|---|---|---|
| Unarmed_Punch | 10 | 1.15 | 0.5 | 1.2 | -> Unarmed_Kick (at 0.5) |
| Unarmed_Kick | 11 | 1.0 | 0.7 | 1.4 | -> Unarmed_Punch (at 0.55) |
| Attack_1H_Slice | 0 | 1.3 | 1.0 | 1.5 | none |
| Attack_1H_JumpChop | 1 | 1.0 | 1.5 | 1.6 | none |

### Items (all price 10)

| Item | Slot | Def | Dmg | Grants attack | Classes |
|---|---|---|---|---|---|
| Blade | RightHand | 0 | 0 | Attack_1H_Slice | any |
| Axe | RightHand | 0 | 25 | Attack_1H_JumpChop | any |
| Shield | LeftHand | 0 | 0 | - | any |
| Hat_For_Mage | Head | 5 | 0 | - | Mage |
| ChestItem_For_Mage | Chest | 2 | 3 | - | Mage |
| Helmet_For_Warrior | Head | 0 | 0 | - | Barbarian |
| Cloak_For_Warrior | Chest | 5 | 0 | - | Barbarian |
| Hood_For_Rogue | Head | 0 | 0 | - | Rogue |
| Cape_For_Rogue | Chest | 0 | 0 | - | Rogue |

> Balance note: the Axe's `damageBonus 25` is huge compared to base damage 9-14. Expect it to be
> retuned when the progression phase adds stat growth.

---

## 13. Editor tools

| Menu | Script | What it does |
|---|---|---|
| `RPG Scene Builder -> Build MVP Scene` | `MvpSceneBuilder` | Spawns every core prefab into the open scene. Only spawns - never wires (rule 1). The boss variant is not in the list yet |
| `RPG Scene Builder -> Build Shop UI` / `Clear Shop UI` | `ShopUIBuilder` | One-time generator for a fresh ShopCanvas. **Destroys** the existing one - re-create the prefab afterwards |
| `RPG Scene Builder -> Make Shop + Inventory Scrollable` | `ScrollablePanelLayout` | Edits `ShopCanvas.prefab`: taller panels, item lists inside a ScrollRect. Safe to run twice |
| `RPG Scene Builder -> Refresh Item Database` | `ItemDatabaseAutoFill` | Fills `ItemDatabase.allItems` with every `ItemData` asset in the project |
| `RPG Scene Builder -> Clear Save Data` | `SaveMenu` | Deletes the PlayerPrefs save |
| `RPG Scene Builder -> Build / Clear / Rebuild Scene` | `RPGSceneBuilder` | Old greybox village generator (primitives) |
| `Tools -> RPG -> Attack Events` | `AttackEventTool` | Pick an FBX + clip, type the contact frame; writes `PlayAttackSound` + `DealAttackDamage` events at that normalized time into the FBX import settings |

---

## 14. How do I... (recipes)

### Add a new class

1. Make the visual: drag the KayKit FBX into a scene, set its rig to **Humanoid**, add `Animator`
   (MageAnimationController), `CharacterVisual` (Reset to auto-fill), `PlayerAnimationEventRelay`,
   `CharacterMotor`. Assign `headPiece` / `chestPiece` if the model has them. Save as
   `Prefabs/<Name>PlayerVisual`.
2. `Create -> RPG -> Character Class` in `Data_Classes`. Fill stats, `visualPrefab`, `unarmedAttack`.
3. Add it to `ClassSelectMenu.classes` on the GameManager prefab.
4. Optional: class-only items -> add the class to their `allowedClasses`.

No code changes.

### Add a new attack / combo step

1. Add the clip as a state inside `Attacks_SubState`, with an Entry transition `AttackIndex == N`
   and Speed multiplier parameter `AttackSpeed`. Bake Into Pose on the clip.
2. `Tools -> RPG -> Attack Events` -> set the contact frame on that clip.
3. `Create -> RPG -> Ability`, set `animatorIndex = N`, damage, range, sounds.
4. Use it: as a weapon's `attack`, a class's `unarmedAttack`, or another ability's `nextInCombo`.
   Keep `comboChainAt` **after** the contact frame.

### Add a new item

1. `Create -> RPG -> Item` in `Data_Items`. Pick a slot. Weapons: set `attack` + `equipPrefab` (+ offsets).
2. `RPG Scene Builder -> Refresh Item Database` (otherwise it won't load from a save).
3. Add it to `ShopManager.itemsForSale` on the Merchant prefab.

### Add a new enemy type

Make a prefab variant of `Enemy_0_with_sword` and change Inspector values (section 8.3). For a new
attack clip, add a state + `AttackIndex` transition in EnemyAnimationController, set the variant's
`attackIndex`, and place its events with the Attack Events tool.

### React to something in UI

Subscribe to the matching `GameEvents` event in `OnEnable`, unsubscribe in `OnDisable`, do one
initial pull in `Start`. If no event exists, add one (section 10).

### Need a UI object from another prefab

Add the field to that canvas's `...Refs` component, drag-link it **inside** that prefab, read it at
runtime through `...Refs.Instance`.

---

## 15. Working with patches

Changes from Claude arrive as `.patch` files (git diffs), sometimes with a few **full files** next to
them. Patches are faster than copy-pasting, but you review **after** applying instead of while typing.
This section is the workflow for doing that safely.

### 15.1 Applying

```bash
# 1. Close Unity. It can rewrite prefabs / .meta files while you apply.
# 2. Start clean - commit or stash your own work first:
git status

# 3. See what the patch touches, then check it applies:
git apply --stat  ~/Downloads/some-change.patch
git apply --check ~/Downloads/some-change.patch

# 4. Apply:
git apply ~/Downloads/some-change.patch

# 5. Full files (if any were sent): open the existing file, select all, paste, save.
#    Same result as copying the file over it.
```

**Why some files come as full files instead of in the patch:** a patch only applies if the
surrounding lines match your file exactly. Some older files contain non-ASCII characters
(em dashes and box-drawing characters) in their header comments; on this machine those made
`git apply` fail. Those files are sent whole. New code is kept ASCII-only to avoid this - please do the
same in your own comments.

The `trailing whitespace` warnings during `git apply` come from Unity's YAML (`m_Name: `) and are
harmless.

### 15.2 Reviewing after applying

```bash
git status                     # changed (M) and new (??) files
git add -N .                   # make NEW files show up in git diff (intent-to-add, not staged)
git diff --stat                # how big each change is
git diff -- Assets/Scripts     # code only, skips prefab YAML noise
git diff -- Assets/Prefabs     # prefab changes: usually "component added" + field values
```

In VS Code the **Source Control** panel shows the same diffs side by side - click a file to review it.

What to check in each patch:
- Does every changed method do what the explanation said? (Explanations come as before/after pairs.)
- New `GameEvents` subscription -> is there a matching unsubscribe in `OnDisable`?
- New cross-object reference -> does it go through `GameManager` / `GameEvents` / `...Refs`
  (rule 1), not an Inspector link across prefabs?
- Prefab diffs: only the components / fields the explanation mentioned.
- New `.cs` files always come with a `.meta` (fixed GUID) - keep both, or prefab links break.

### 15.3 Undoing

```bash
git apply -R ~/Downloads/some-change.patch   # reverse exactly that patch
# or throw away ALL uncommitted changes (careful):
git checkout -- . && git clean -fd Assets/
```

### 15.4 After testing

Open Unity, let it import, test, then commit with a message describing the feature, and push.
Claude reviews by cloning the branch - it never pushes to the repo.

---

## 16. Known issues and legacy code

**Legacy / unused (safe to delete after a check):**

| File | Status |
|---|---|
| `PlayerInventory.cs` | Pre-EquipmentManager outfit system. Not on any prefab |
| `Interactions.cs` (`NPCInteraction`, `ShopkeeperInteraction`) | 2D-era port. Not used; `NPCInteraction` still calls `PlayerInventory`. Note the near-duplicate name of the real `ShopKeeperInteraction` |
| `DebugInventoryTester.cs` | Temporary I/U/G/H test keys. Not on any prefab |
| `AttackStateLogger.cs` | Debug behaviour, replaced by `EnemyAttackState`. Not on any animator |
| `UsesRootMotion.cs` + `CharacterMotor.cs` | Root-motion experiment. `UsesRootMotion` is on no state, so `CharacterMotor` never activates. Attacks use Bake Into Pose instead |
| `MusicManager.cs` | Not in any scene |
| `Prefabs/Enemy_0.prefab`, `Enemy_0_backup.prefab`, `SampleScene_backup.unity` | Old copies |

**Known issues:**

- `PauseMenu` is on **both** `GameManager.prefab` and `PauseMenuCanvas.prefab`. Esc runs both; it works
  only because pausing twice gives the same result. Remove the one on GameManager.
- `PauseMenu.Resume` sets `Time.timeScale = 1` directly. Pausing while the class picker is open and
  then resuming un-freezes the game behind the picker. A shared "pause stack" would fix both menus.
- Many `Debug.Log` calls in hot paths (`PlayerHealth.TakeDamage`, `SaveSystem`, shop) - remove or wrap
  before release.
- `PlayerRefs.Start` has a temporary verification log.
- `2.5D_RPG_PROJECT.slnx` is committed; it is generated by Unity and belongs in `.gitignore` with
  `*.sln` / `*.csproj`.
- The old `com.unity.ide.vscode` package conflicts with `com.unity.ide.visualstudio` - remove it from
  `Packages/manifest.json`.
- Death always reloads the scene; there is no respawn point or game-over menu.
- The class is not saved; the picker appears on every start.
- The directional hit cone for player attacks is written but commented out in `PlayerAttack`.

---

## 17. Roadmap

| Phase | Content | Status |
|---|---|---|
| Combat core | AbilityDefinition, animation-driven hits, combos, sounds, attack speed, enemy flinch / arcs / dodge | Done |
| Classes | CharacterClassDefinition, PlayerClass, 3 class visuals, class-restricted wearables | Done |
| Phase A | Wearables for every class, scrollable shop + inventory | Done (`3d8180a`) |
| NavMesh enemies | NavMeshAgent chase, obstacle avoidance, no pushing | Done locally |
| Class select + HUD | Class picker, class + gold under the health bar | Done |
| **B - Progression** | `CharacterProgression` (level, XP, `GainXP`, `OnLevelUp`), `XPCurve` asset, per-class stat growth, `Enemy.xpReward`, level + XP bar on HUD, save class / level / XP | Next |
| C - Skills | Ability loadout / skill bar, `requiredLevel`, resources (mana / rage / energy) | |
| D - Ranged | Projectile system: crossbow, mage staff, VFX | |
| E - Enemy variety | Enemies using `AbilityDefinition`, archers, summoners | |

---

## 18. Script index (A-Z)

| Script | Lives on | One-line job |
|---|---|---|
| `AbilityDefinition` | asset | One attack: clip index, speed, damage, range, combo, sounds |
| `AnimationEventRelay` | enemy visual | Forwards Animation Events to `Enemy` |
| `AttackButtonRelay` | HUDCanvas | Attack button -> `GameEvents.AttackPressed` |
| `AttackStateLogger` | - | Legacy debug behaviour |
| `CharacterClassDefinition` | asset | One class: visual, stats, unarmed attack |
| `CharacterMotor` | player visuals | Legacy root-motion helper |
| `CharacterVisual` | player visual root | Refs inside the model: animator, sockets, head / chest pieces |
| `CharacterVisualController` | Player | Shows equipment on the current model |
| `ClassSelectMenu` | GameManager | Class picker screen (built in code) |
| `DebugInventoryTester` | - | Legacy test keys |
| `Enemy` | enemy root | AI: chase (NavMesh), attack, flinch, die, drop gold |
| `EnemyAttackState` | enemy Animator | Reports attack start / end to `Enemy` |
| `EnemyHealthBar` | enemy child canvas | World-space HP bar |
| `EquipmentManager` | Player | Equipped items, current attack, class restrictions |
| `GameEvents` | static | Global event hub |
| `GameManager` | GameManager | Singleton; holds `Player` (`PlayerRefs`) |
| `GoldPickup` | CubeCollectible | Spinning gold, adds gold on touch |
| `InteractButtonRelay` | HUDCanvas | Interact button -> `GameEvents.InteractPressed` |
| `Interactions` | - | Legacy 2D-era NPC / shopkeeper |
| `InventoryManager` | Player | Owned items + gold; loads the save |
| `InventoryUIController` | Player | Inventory panel rows, equip / unequip |
| `ItemData` | asset | One item |
| `ItemDatabase` | Player | Name -> item lookup for saves |
| `MusicManager` | - | Unused background music |
| `PauseMenu` | PauseMenuCanvas (+ GameManager, duplicate) | Pause, save, restart, quit |
| `PauseUIRefs` | PauseMenuCanvas | Pause panel ref |
| `PlayerAnimationEventRelay` | player visuals | Forwards Animation Events to `PlayerAttack` / `PlayerHealth` |
| `PlayerAttack` | Player | Attacks, combos, damage, sounds |
| `PlayerAttackState` | `Attacks_SubState` | Reports swing start / progress / end to `PlayerAttack` |
| `PlayerClass` | Player | Applies / switches class |
| `PlayerController` | Player | Movement |
| `PlayerHealth` | Player | HP, damage after armour, death |
| `PlayerHealthUI` | PlayerHealthBarCanvas | Health slider |
| `PlayerInventory` | - | Legacy outfit system |
| `PlayerRefs` | Player | Component cache, registers with GameManager |
| `PlayerStatsHUD` | PlayerHealthBarCanvas | Class name + gold under the health bar |
| `PlayerUIRefs` | PlayerHealthBarCanvas | Death panel ref |
| `SaveSystem` | static | PlayerPrefs save / load / clear |
| `SceneBuilderNote` | various | Inspector note text |
| `ShopKeeperInteraction` | Merchant | Range trigger, opens / closes the shop |
| `ShopManager` | Merchant | Shop buttons, buying |
| `ShopUIController` | Merchant | Shows / hides the shop panel |
| `ShopUIRefs` | ShopCanvas | Refs inside the shop + inventory canvas |
| `TopDownCameraController` | Main Camera | Follows the player |
| `UsesRootMotion` | - | Legacy root-motion behaviour |
| `VirtualJoystick` | HUDCanvas | On-screen joystick |
