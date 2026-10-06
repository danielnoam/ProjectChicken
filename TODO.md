# ProjectChicken — outstanding work

Open work, in the order to do it. Finished items are in [DONE.md](DONE.md).
Last full check against the code: 2026-10-06.

---

## 1. Open it in the editor and check the upgrade

The upgrade to 6000.6.2f1 compiles headless with no errors or warnings, and
every script and prefab reference in the game scenes and prefabs resolves.
These need the editor:

- [ ] **Transitions look the same.** The circle iris in and out (Intro, Level
      Start/End, Credits) and the Level Launch lens distortion were rebuilt for
      the new VFX format, with the icon effects now in the game.
- [ ] **Menu buttons** still animate and play their select and submit sounds.
- [ ] **Inspectors** that used vInspector foldouts and `ShowIf` blocks
      (`SOLevelStage`, `SOWeaponData`, `SOWeaponUpgrade`, `RailPlayerWeaponSystem`,
      `HUDManager`, the main menu elements)
      group and hide fields as before.
- [ ] **Pools.** Tools > DNExtensions > Object Pooling Settings shows the 14
      pools from the old prefab. Projectiles, eggs and chickens spawn.
- [ ] **Chain lightning stun** now uses its real chance (15% by default)
      instead of stunning almost every hit. Raise `baseStunChance` if the old
      feel was intended.
- [ ] Play a full run: intro, level 1 with a store, a checkpoint death and a
      death before the first checkpoint, the outro into level 2, and the
      credits.

## 2. Gameplay

- [ ] **Chain lightning shares state between shots.** Hitscan behaviours are
      the shared assets (`WeaponInstance` copies projectile behaviours but not
      hitscan ones), so firing again mid-chain mixes chains, and chains jump to
      dead chickens (`BehaviorChainOnHit`).
- [ ] **Homing projectiles chase dead targets** and can log "viewing vector is
      zero" (`BehaviorAimLockMovement`).
- [ ] **Passage building disruption can stay on** if the building is destroyed
      inside the zone, since `OnTriggerExit` never fires
      (`PassageBuildingChickenDisruptor`). It also has no restart handling.
- [ ] **Circle formations have one slot more** than `numberOfSlots` (centre
      plus ring, `FormationGenerator`). Decide whether the centre slot is meant.
- [ ] **Formation attack in sequential mode** fires at most one egg per run
      (`FormationShapeAttackSO`). The shipped asset uses simultaneous mode.
- [ ] **`SelectRandomAttackThatIsNot`** always returns the first match instead
      of a weighted random one (`AttackLootTableSO`).
- [ ] Chickens that get a slot while still flying to spawn keep the spawn
      target (`ChickenMovementBehavior.HandleMovement` gates on `!isMoving`).
- [ ] The score and currency counters can count backwards when kills land
      less than 0.5s apart (`HUDManager`).

## 3. Settings

- [ ] **Display settings do nothing.** `DisplayTab.Start` is commented out and
      nothing applies the saved display settings. When it comes back, store
      width, height and refresh rate instead of an index into
      `Screen.resolutions` (index 0 is the lowest resolution).
- [ ] **Saved volumes only apply once the options screen opens**
      (`VolumeTab.Start`), so a muted player still hears the intro. Apply them
      at boot.
- [ ] `SOAudioEvent.PlayAtPoint` creates and destroys a GameObject per sound
      now; the old version used the `OneShotSFX` pool.

## 4. Small things

- [ ] scene-ref-attribute (KBCore) still works on 6.6. Swapping its 36 uses for
      DNExtensions `AutoGet` would drop the last inspector plugin.
- [ ] `Settings/DefaultVolumeProfile.asset` holds editor-test and sample volume
      components with missing scripts. The HDRP sample scenes under `Samples`
      have missing prefabs too.
- [ ] `Tay's Scene` overrode the old pooler's pools; those overrides are gone
      with the prefab.
- [ ] `TextWriter.asset` audio event is not used by anything.
- [ ] The in-game credits still list bl4st, Eflatun SceneReference and the
      vInspector family, which the project no longer uses.
- [ ] The repo is 861 MB because binaries were committed before LFS. Only a
      history rewrite would shrink it.
