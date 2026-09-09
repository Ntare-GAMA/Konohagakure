# Konohagakure — Interactive Character Experience System

A Unity project built for the **Interactive Character Experience System** assignment: an interactive scene combining animated, interaction-driven characters, a full UI flow, and a checkpoint-based training course.


---

## Overview

The scene simulates a training dojo with three characters and a state-driven UI that walks the player from character/drill select, through a live training session, to a results debrief.

**Core loop:** Dojo Select → pick a drill (Walking / Running / Obstacle) → Training (live HUD) → Debrief (results) → back to Dojo Select.

---

## Characters

| Character | Role | Key script |
|---|---|---|
| **Shinobi** | Player character — walk, run, dodge, strafe | `ShinobiController.cs` |
| **Boxer** | Reactive NPC — punches a bag when the player walks into range, idles otherwise | `BoxerController.cs` |
| **Sensei** | NPC (interaction/instructor role in the scene) | `SenseiController.cs` |
| **X Bot** | Rigged character asset used for one of the above (Mixamo-based) | — |

### Controls (Shinobi)
Based on the in-scene UI labels: **Walk = W**, **Dodge = J**, **Strafe = A / D**. Confirm these still match your current input bindings before recording.

### Boxer interaction logic
- Boxer is **idle by default** and only punches when the player enters a trigger zone near the punching bag — this satisfies an "animation triggered by interaction, not autoplay" requirement.
- `BoxerAnimator` Controller: `Idle` (default) ↔ `Punch`, driven by a **bool** parameter `IsPunching` (not a trigger), so it stays true/false based on proximity rather than firing once.
- Requires a **non-trigger Capsule Collider** on Boxer (so it doesn't sink into the floor) plus either no Rigidbody or a **Kinematic** Rigidbody with rotation frozen on X/Z (prevents falling/toppling).
- A separate **trigger** Box/Sphere Collider defines the "near the bag" zone that flips `IsPunching`.
- The player object must keep the **`Player`** tag for the trigger to detect it.

---

## UI System

Three full-screen panels live under a single `Canvas`, each with its own `Animator` (shared `PanelTransition` controller) and driven centrally by `UIManager.cs`:

- **DojoSelectPanel** — Walking / Running / Obstacle drill buttons, Volume Slider, Mute Toggle
- **TrainingPanel** — Timer text, Checkpoint text, Status text (live HUD)
- **DebriefPanel** — Result Time text, Result Checkpoints text, Back button

All text elements use **TextMeshPro (`TMP_Text` / `TextMeshProUGUI`)**, not the legacy `UnityEngine.UI.Text` — `UIManager.cs` declares its text fields as `TMP_Text`, so legacy Text components won't fit the Inspector slots. Make sure **TMP Essential Resources** are imported (`Window → TextMeshPro → Import TMP Essential Resources`).

### Panel transition logic
Each panel's Animator has four states:
- `Hidden` / `Visible` — static resting poses (single-keyframe clips holding `CanvasGroup` alpha, `Interactable`, and `BlocksRaycasts` at a fixed 0 or 1, plus a slight scale pop)
- `Showing` / `Hiding` — the animated fade + scale transitions (0.25s in / 0.2s out, ease in/out) that play between the resting states

This two-tier setup exists because a panel built manually (not via the editor tool below) starts with whatever `CanvasGroup` values were last saved in the Editor — usually **Alpha 1 / Interactable / Blocks Raycasts**, i.e. fully visible. Giving `Hidden`/`Visible` real static poses means entering either state (including the very first frame at scene start) snaps the panel to the correct alpha/interactivity, instead of only reacting when a `Show`/`Hide` trigger fires.

### Editor tooling (Konohagakure menu)
Two custom Editor scripts automate the UI build so it doesn't have to be wired by hand:

1. **`UIBuilder.cs`** — `Tools → Konohagakure → Build UI Canvas`
   Builds the entire Canvas hierarchy: all 3 panels, TMP texts, buttons, slider, toggle, an `EventSystem` if missing, a shared `PanelTransition` Animator Controller, and a `UIManager` GameObject with every reference and button `onClick` wired up.
   Must live under an `Editor/` folder (e.g. `Assets/Konohagakure/Editor/UIBuilder.cs`) or Unity won't compile it correctly.
   Does **not** handle visual polish (transition animation content, styling/colors/fonts) — that's intentionally left for you.

2. **`UIAnimationBuilder.cs`** — `Tools → Konohagakure → Build UI Animations`
   Run *after* `UIBuilder`. Generates the real `PanelShow` / `PanelHide` fade clips and the `PanelVisiblePose` / `PanelHiddenPose` static pose clips described above, assigns them to the `PanelTransition` controller's states, and adds a `CanvasGroup` + `Animator` to each panel if missing. Safe to re-run — it rebuilds the controller only if it detects the old (posing-less) layout.

---

## State Management

`KonohaManager.cs` owns the overall simulation state (`SimState`) and exposes an `OnStateChanged` event.
- State only changes (and the event only fires) inside `SetState()`, which is called from `StartDrill()`, `EndDrill()`, and `ReturnToDojoSelect()` — there's no automatic state change or double panel-switch on scene load.
- `UIManager.HandleStateChanged` listens for this event and calls `SwitchPanel(...)` to move between Dojo Select / Training / Debrief.

---

## Checkpoints & Course

- `CheckpointCourse.cs` and `CheckpointTrigger.cs` manage the obstacle/checkpoint drill, feeding checkpoint progress into the Training HUD (`checkpointText`) and the Debrief results (`resultCheckpointsText`).

## Audio

- `AudioManager.cs` handles project audio, wired to the Volume Slider and Mute Toggle on `DojoSelectPanel`.

---

## Script Reference

| Script | Purpose |
|---|---|
| `KonohaManager.cs` | Central state machine (`SimState`, `OnStateChanged`) |
| `UIManager.cs` | Panel orchestration, HUD text, button wiring |
| `ShinobiController.cs` | Player movement (walk/run/dodge/strafe) |
| `BoxerController.cs` | Proximity-triggered punch behavior |
| `SenseiController.cs` | Sensei character behavior |
| `CheckpointCourse.cs` / `CheckpointTrigger.cs` | Obstacle course / checkpoint tracking |
| `AudioManager.cs` | Audio + volume/mute controls |
| `UIBuilder.cs` *(Editor)* | Auto-builds the Canvas/UI hierarchy |
| `UIAnimationBuilder.cs` *(Editor)* | Auto-builds panel Show/Hide/pose animations |

---

## Setup

1. Open the project in Unity, make sure **TextMeshPro Essentials** are imported.
2. `Tools → Konohagakure → Build UI Canvas` to generate the Canvas/UI hierarchy (skip if already built).
3. `Tools → Konohagakure → Build UI Animations` to generate/attach the panel transition + pose clips.
4. Confirm each character (Shinobi / Boxer / Sensei / X Bot) has the correct Animator Controller and Avatar assigned, and that Boxer's trigger zone sits near the punching bag.
5. Hit **Play**. You should land on Dojo Select only — Training and Debrief panels should be invisible and non-interactive until selected.

---

## Known Issues / Things to Verify Before Recording

These were open at the end of the last debugging session — check each before you record the demo:

- **X Bot non-uniform scale** — was showing `(2.8, 2.6, 3.8)` instead of `(1, 1, 1)`. Fix: set Transform Scale to `(1,1,1)`.
- **Humanoid rig binding warning** ("generic clip(s) animate transforms... from animation clip 'mixamo.com'") — indicates the Humanoid rig conversion wasn't completed on one or more clips. Fix: for each Walk/Run/Strafe clip, Rig import settings → Humanoid → **Copy From Other Avatar** → the X Bot avatar → Apply.
- **Gliding / floating movement** — reported as still present after the scale/rig fix attempt. Two distinct possible causes, needing different fixes:
  - *(A) Character floats above the ground* — likely still tied to the rig/binding issue above if the warning persists.
  - *(B) Feet slide/skate ("ice skating" look)* while the body moves correctly — caused by `walkMetersPerSec` / `runMetersPerSec` in `ShinobiController.cs` not matching the animation clip's natural stride pace.
    - Quick fix: manually tune `walkMetersPerSec` / `runMetersPerSec` until feet roughly plant on each step.
    - Proper fix: enable **Apply Root Motion** and drive movement from `Animator.deltaPosition` inside `OnAnimatorMove()` instead of a manually-set speed value.
- **Panel visibility on scene start** — should now be fixed by `UIAnimationBuilder.cs`'s pose clips; re-verify Training/Debrief panels start hidden and non-clickable on a fresh Play.

---

## Assignment Context

This project was built for the **Interactive Character Experience System** assessment , evaluating the ability to design and implement an interactive Unity experience combining animated characters, an interaction-driven state machine, and a UI flow.
