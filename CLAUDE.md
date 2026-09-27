# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

God Tower is a Unity 6 (6000.3.2f1, URP, new Input System, DOTween) **portrait Android prototype**: a martial-arts climber scales a tower across five levels, with boosts/hazards and a required boxing-glove event triggered by an external HTTP request. The spec is `Docs/God_Tower_Game_Brief.pdf`; `Docs/ref.mp4` is the visual reference. Key requirements from the brief:

- `GET` or `POST http://localhost:56789/bump` while a level is running must start the glove burst (4–6 gloves, flash, shake, punch sound), in both the Editor and the Android build. It is nonlethal and must always return the climber to playable climbing.
- Repeated bumps, pause, retry and scene changes must never leave stuck states or leaked effects.
- Exactly five finishable levels; the final level's goal is 5,000 height units. Space (also Up/W) acts as the Climb button in the Editor.
- Only free/licensed or AI-generated assets; the README must list the Unity version, assumptions, asset licenses and tested webhook steps.

## Commands

Unity Editor is at `C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe`. There are no custom build scripts, CI or tests in the repo. Batch-mode test run (Edit Mode), if tests are added:

```
"C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe" -batchmode -projectPath "F:\Unity\God Tower" -runTests -testPlatform EditMode -testResults results.xml [-testFilter <FullTestName>]
```

Quick compile check without the Editor (uses Unity's generated project; regenerate it from the Editor if scripts were added or removed): `dotnet build Assembly-CSharp.csproj -nologo -v q -o "$TEMP/gtbuild"`.

Trigger the webhook while playing: `curl http://localhost:56789/bump` or `curl -X POST http://localhost:56789/bump`. Response is JSON with `"triggered": true|false` (false when no level is in the Playing state). On an Android device over USB: `adb forward tcp:56789 tcp:56789`, then curl localhost.

## Architecture

All gameplay code is in `Assets/Scripts`, namespaced `GodTower.<Folder>` (Core, Level, Player, Effects, Cameras, UI, Audio, Net). It currently compiles into `Assembly-CSharp` (no asmdefs); the `GodTower.*.csproj` files in the root are stale leftovers referring to a removed `Assets/_Project/Editor` folder.

**Session vs. level.** `Core/GameSession` is a `DontDestroyOnLoad` singleton created by `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` — it is never placed in a scene. It owns the level catalog, selected level index, `MainThreadDispatcher` and the `BumpServer`, and does scene loading (`SceneNames.MainMenu` / `SceneNames.Game`). `Core/LevelController` lives in the game scene and owns one level's flow: `Intro → Playing ⇄ Paused → Won | Lost`. It builds the level and wires climber/camera/effects via serialized references and events; all in-level UI (HUD, flash, notifications, banner, pause/result screens) sits behind one optional `UI/GameHudController`, which forwards button presses back as events. Its `Cleanup()` (kill coroutines, clear effects/camera/notifications, `DOTween.KillAll()`, reset `timeScale`) must run on every exit path. `SetFlow` is the single place that toggles climber control and `BumpServer.AcceptingBumps`.

**Webhook threading.** `Net/BumpServer` is a raw `TcpListener` (works under IL2CPP on Android) with request parsing in `HttpRequestParser`. `BumpAccepted` fires on a worker thread; `GameSession` marshals it through `MainThreadDispatcher.Enqueue` and re-raises `BumpReceived` on the main thread. Never touch Unity objects from the listener thread. `LevelController.OnBump` re-checks `Flow == Playing` before triggering `EffectType.GloveBurst`.

**Levels are data.** `Level/LevelConfig` (ScriptableObject) holds goal height, units-per-meter, safe zone, climb speed, knockback tuning and a height-sorted list of `Encounter { height, EffectType }`. Heights are in *display units* (what the HUD shows); convert with `ToMeters`/`ToUnits`. `LevelCatalog` is loaded from `Resources/LevelCatalog`. `EncounterRunner` fires each encounter once when height first passes it (knockback never re-fires). `LevelBuilder` stacks a segment prefab up to the goal and places an optional summit prefab (child `StandPoint` marks the landing spot).

**Effects.** Every event (webhook gloves, jetpack/phoenix boosts, axe/explosion hazards) is an `EffectBase` subclass with `Type`, `Play()` (must be safe to call repeatedly) and `Clear()` (return everything to its `PrefabPool`). `EffectManager` maps `EffectType → EffectBase`, is the single trigger entry point, and raises `EffectTriggered(type, source)` which drives the notification feed (gifts left/blue, attacks right/red via `EffectTypeExtensions.IsGift`). Effects receive shared references through `EffectContext` and act on the climber only via `ClimberController.ApplyHit` / `ApplyBoost`. Adding an effect = new enum value + subclass + register it in the scene's `EffectManager.effects` array.

**Climber.** `Player/ClimberController` is kinematic with explicit states (Idle, Climb, Hit, Fall, Win, Lose); height is tracked at the hands. It reads input through `IClimbInput` (`ClimbInput` merges the on-screen button and keyboard) and raises `SummitReached` / `FellBelowBase` that end the level. `ClimberView` / `ClimberAudio` are presentation listeners.

**Audio.** `Audio/AudioHandler` is a scene component (static `Instance`, persists across scene loads; a duplicate in a later scene destroys itself) that owns all audio: start music followed by the background loop (scheduled on `AudioSettings.dspTime` for a gapless handover) and every sound effect, assigned per `SfxId`, with `SfxSynth` placeholders for unassigned ids. Play sounds with `AudioHandler.TryPlay(SfxId.X)`, which is a no-op when no handler exists. It does not depend on `GameSession`, so it works in standalone mode.

**Persistence.** `Core/SaveData` wraps PlayerPrefs (`gt.*` keys) for level completion, volumes and the reduced-shake setting (read by `CameraRig`).

## Current state / gotchas

The project is mid-restructure; code and assets are out of sync:

- Code expects scenes named `MainMenu` and `Game`, but only `Assets/Scenes/Gameplay.unity` exists, and Build Settings still lists the deleted `SampleScene.unity`.
- `Resources/LevelCatalog` and `Resources/SfxLibrary` do not exist yet (only `Resources/DOTweenSettings.asset`); `GameSession` logs an error referencing a `Tools/God Tower/Build Project` menu from the removed editor tooling. Only one `LevelConfig` exists (`Assets/ScriptableObjects/Levels/Level.asset`).
- `LevelController.standalone` (Inspector checkbox) is a test mode: `GameSession.Bootstrap` finds it before the scene wakes and skips creating the session (no webhook, catalog or menu flow), the level plays `fallbackConfig`, and retry/exit reload the current scene. The effects manager and `GameHudController` (and every UI piece inside it) are optional in every mode, so keep new calls on them null-guarded; with no result screen the level logs the outcome and restarts.
- `Assets/Plugins/Demigiant` (DOTween/Pro) and `Assets/CustomPackages/Simple Water Shader` are third-party; don't modify them.
