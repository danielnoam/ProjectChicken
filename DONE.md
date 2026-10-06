# ProjectChicken — done

Finished work moved out of [TODO.md](TODO.md), oldest first. The notes are
kept because they explain why things are the way they are.

## Upgrade, DNExtensions, cleanup, LFS — 2026-10-06

### Unity 6000.2.9f1 → 6000.6.2f1

Same package set as ElectroGrid: URP and VFX Graph 17.6, Input System 1.20,
Cinemachine and Timeline 6.6, uGUI 2.6, Test Framework 1.8, Recorder 5.1.7 and
Splines 2.9. The Unity Analytics and VR modules are gone. TMPEffects is pinned to
0.3.2 instead of following its main branch. The embedded Hot Reload package is
1.13.24 (1.13.11 no longer compiled on 6.6). It compiles headless with no errors
and no warnings.

Active Input Handling is the Input System only. The legacy `Input` calls were
the debug keys: F1, F2, F3 and F5 in `RailPlayerWeaponSystem` (weapon switch and
upgrade) and F12 in `LevelManager` (next stage). They use the Input System now
and only work in the editor and development builds, so players can't trigger
them in a release build.

The deprecated `FindFirstObjectByType` and sorted `FindObjectsByType` calls are
`FindAnyObjectByType` and unsorted `FindObjectsByType`.

### DNExtensions

Installed as the `utilities`, `components`, `systems` and `helpfuleditor` git
packages, pinned to tag `v1.1.0`. `Assets/Plugins/DNExtensions` is deleted. A
scripted pass over all 11 scenes and every prefab and ScriptableObject found no
missing scripts, prefabs or SerializeReference types in game assets.

- **Shared GUIDs.** The game's `InputManager`, `InputReaderBase`,
  `InputManagerBindingFormatter` and the dither shader folder
  (`5_Resources/Daniel/DitherShader`: material, two graphs, `DitheringObject`)
  had the same GUIDs as files in the package, so Unity would have kept one and
  dropped the other. The game's copies have new GUIDs and every reference was
  rewritten.
- **Menu button animators.** The package's `SelectableAnimator` no longer plays
  select and submit sounds, and `SelectableGraphicAnimator` and
  `InteractableGraphicAnimator` changed or are gone. The game's versions moved to
  `2_Scripts/GeneralUI/MenuAnimators` under a `MenuSystem` namespace, so the
  buttons, sliders, toggles and dropdowns keep their sounds and animation.
- **VFX sequences.** `SOVFEffectsSequence` is now `EffectSequence`. All nine
  sequences (intro, level and credits start/end/blackscreen, level launch) were
  rewritten into the new format with the same timings, eases and values. The
  package dropped its icon effects, which the circle iris transitions use, so
  `SetIconImage` and `SetIconScale` live in the game now
  (`2_Scripts/Effects/VFX`). `Circle.png` comes from the package with the same
  GUID. `9Sliced.png` is used by the HUD, so it moved to `5_Resources/Texturs`.
- **Object pooling.** `ObjectPooler.prefab` became
  `Resources/ObjectPoolingSettings.asset` with the same 14 pools, and the
  prefab was removed from the 7 scenes that had it. `IPooledObject` is
  `IPoolable`.
- **Renames.** `ControllerVibration*` is `ControllerRumble*` (`Vibrate` is
  `Rumble`), `CinemachineRotationOffsetExtension` is `CinemachineRotationOffset`,
  `VFXManager.PlayVFX` is `PlaySequence`, `ChanceList.GetIndex` is `IndexOf`, and
  the old `DNExtensions` namespace is split into `DNExtensions.Utilities`,
  `.Utilities.CustomFields`, `.Systems.Scriptables`, `.Systems.VFXManager`,
  `.Systems.ObjectPooling` and `.Systems.ControllerRumble`.

### Plugins

Removed: vInspector, vFolders, vHierarchy, bl4st, SerializeInterfaces (only
unused `using AYellowpaper;` lines), EditableAssetAttribute (`[CreateEditableAsset]`
is DNExtensions `[Inline]`) and `com.eflatun.scenereference` with its OpenUPM
registry (nothing used it). vInspector's `[Foldout]`, `[ShowIf]`, `[EndIf]` and
`[Button]` map onto the DNExtensions v1.1.0 attributes with the same block
behaviour. Three trailing `[EndIf]`s that sat on properties were dropped.

Kept: scene-ref-attribute (KBCore, 36 files; it still compiles on 6.6),
PrimeTween, QuickOutline, SineVFX, TMPEffects and `Assets/Samples` (11 materials
and shaders use its textures and subgraphs).

### Git

- `.gitattributes` from ElectroGrid plus `exr`, `hdr`, `dds`, `flac`, `pdf` and
  `mp4`. The 624 binaries in the repo are LFS pointers now. Only those paths
  were renormalized, so YAML and `.meta` files have no line-ending churn.
- `UserSettings`, `.idea` and the stray `UIElementsSchema` folder are no longer
  tracked. History was not rewritten, so the clone is still 861 MB.

### Bug fixes

- **Checkpoints lost or doubled upgrades.** Save points and run progress kept
  a reference to the player's live upgrade dictionary, so dying cleared every
  upgrade, upgrades bought after the checkpoint leaked into it, and loading the
  next level added one to every stack. They store copies now, and restoring
  applies each upgrade once per stack.
- **Dying before the first checkpoint** reset the ship to base health with no
  currency or upgrades, even on level 2 and later. It restores what the player
  carried into the level.
- **A pending stage change fired after death**, during the 5 second wait or
  right after the restore. Restarting stops it.
- **Leaving a level from the pause menu marked it completed** and saved the
  score, which unlocked the next level. Completion is saved when the outro
  starts.
- **A crash or force-quit left a run progress file** that loaded back as a ship
  with 0 health and no upgrades. Run progress only lives for the session now. An
  empty settings or progress file falls back to defaults.
- Skip worked while paused, and threw when a skippable stage had no events. A
  missing stage in the list stalled the level.
- The "accumulate score" task completed on the first score event.
- The upgrade store handled each stage change twice, and could open on stages
  that aren't store stages.
- A resource caught by the magnet during its despawn animation survived.
- Any collider leaving a passthrough obstacle counted as the player passing
  through. Obstacles also repeated, because the no-repeat check compared a
  prefab with an instance.
- Enemies that died during intro, outro and delay stages were never untracked,
  so stage events stopped spawning and reused chickens fired death events twice.
- Pooled chickens kept their old health, invulnerability and flight path when
  reused. Pooled eggs drew a trail from their last position.
- The large grid attack reported itself as a formation attack, and could freeze
  the game if no chicken was able to shoot.
- Chain lightning stun was compared on the wrong scale and landed about 99.9%
  of the time. It uses the configured chance now (15% by default).
- Holding fire or move while pausing kept firing or drifting after resuming.
  Key prompts stayed drawn as pressed after pausing.
- Upgrading the equipped weapon left the old model visible.
- Shield regen could run twice, and never sent the final full-shield update.
- The ship model froze if the stage changed during a look-at.
- The health HUD flashed red on heals and not on damage. The heat text never
  reached full alpha. The outro camera only cycled when intro cycling was on.
  The stage bar froze after restarting from a checkpoint.
- Pausing or stopping music during a crossfade left the old track playing.
- A skip during the cinematic's end fade restarted the transition.
- The first menu input skipped element 0. Generic HID controllers were treated
  as keyboard and mouse.
- Offset-mode menu buttons jumped to the world origin when their panel closed.
- The formation creator ran its type-change handler on every stage change, which
  could permanently shrink formations.
- Three runtime scripts had `using UnityEditor` outside `#if UNITY_EDITOR`,
  which breaks player builds. The music manager's duplicate subscribed to scene
  events before it was destroyed.
