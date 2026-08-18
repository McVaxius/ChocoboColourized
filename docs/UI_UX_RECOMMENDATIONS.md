# Chocobo Colourized UI/UX Recommendations

**Review date:** 2026-08-18  
**Scope:** UI code review only; no runtime behaviour or implementation changes are included in this document.

## Product goal

Move a user from current colour to target colour, verify fruit requirements, and complete feeding automation without losing their place.

## Reviewed surfaces

- `ChocoboColourized/Windows/MainWindow.cs`
- `ChocoboColourized/Windows/ConfigWindow.cs`

## What is already working

- Searchable current/target colour selectors and swatches reduce colour-name ambiguity.
- The calculated result, inventory requirements, saved plan, timers, and automation are already connected in one workflow.
- The inventory table distinguishes required, owned, and status.

## Prioritized recommendations

| Priority | Recommendation | Rationale and completion signal |
| --- | --- | --- |
| P0 | Present one visible Calculate → Prepare → Feed flow. | Use a step indicator across Calculator, fruit requirements, and Automation so users always know what is complete and what blocks the next step. |
| P0 | Surface automation prerequisites beside Start. | Show OPEN ALL and EXPANDED, stable access, inventory, and saved-plan readiness as a checklist with a direct reason for every disabled action. |
| P0 | Clarify detected versus manually selected colour. | The UI currently presents detection language while also saying auto-detection is future work. Label the source explicitly and never imply live detection when the value is manual or stale. |
| P1 | Make the result visually scannable. | Show current and target swatches together, total fruit count, ordered feeding batches, and the nearest achievable final colour before the detailed table. |
| P1 | Turn shortages into next actions. | Highlight only missing quantities and offer copy shopping list or open/save plan from the same inventory section. |
| P1 | Show resumable automation progress. | Display current batch, fruit just fed, remaining fruit, pause reason, and a safe Resume action after UI-state interruptions. |
| P2 | Fold the one-setting config window into the main UI. | Place `Movable window` under a small Interface section unless more configuration is added. |

## Suggested information hierarchy

1. Colour selection
2. Calculated outcome
3. Fruit readiness
4. Automation progress
5. Timers and saved plans

## Validation checklist

- A new user can identify the primary action and current blocker within five seconds.
- Every disabled control has a nearby plain-language reason and, when possible, a direct corrective action.
- Healthy, warning, error, running, and disabled states remain distinguishable without colour.
- The UI remains usable at narrow window widths and common Dalamud UI scales without clipped labels or unreachable controls.
- Destructive, global, or high-impact actions identify their scope and require confirmation or provide a safe undo.
- Empty, loading, stale-data, success, partial-success, and failure states each provide an appropriate next action.
- Settings clearly identify whether they apply globally, per account, per character, per preset, or only for the current session.
- Advanced diagnostics are still reachable but do not compete with the everyday workflow.

## Recommended implementation order

1. Implement P0 items and validate the primary workflow plus blocker recovery.
2. Implement P1 information-architecture and configuration improvements.
3. Apply P2 polish, then test at multiple UI scales with both fresh and mature configurations.
