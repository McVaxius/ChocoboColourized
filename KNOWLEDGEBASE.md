# Chocobo Colourized - Knowledgebase

This document captures everything learned during development about FFXIV's chocobo colour mechanics.

---

## Bug Fix: Fruit RGB Modifiers Were Wrong (2026-02-27)

### The Problem
The algorithm was producing ~1000 step feeding paths (hitting the safety limit) instead of the expected ~74 steps. For example, Desert Yellow → Soot Black was giving Doman Plum ×447, Xelphatol Apple ×319 etc., instead of the reference answer of Apple ×19, Pear ×23, Berries ×32 (74 total).

### Root Cause
The fruit RGB modifiers were set to **(+5, -2, -2)** pattern (primary channel ±5, secondary channels ∓2). This is **incorrect**. The correct pattern is **(+5, -5, -5)** (primary channel ±5, secondary channels ∓5).

### Mathematical Proof
Using the reference test case: Desert Yellow (219, 180, 87) → Soot Black (43, 41, 35), delta needed = (-176, -139, -52).

**With WRONG modifiers (+5, -2, -2):**
- Apple + Pear + Berry = (+5-2-2, -2+5-2, -2-2+5) = **(+1, +1, +1)** per trio
- Net change per trio is only ±1 per channel → needs hundreds of fruits
- The trio SUM is too small, leading to extremely slow convergence

**With CORRECT modifiers (+5, -5, -5):**
- Apple + Pear + Berry = (+5-5-5, -5+5-5, -5-5+5) = **(-5, -5, -5)** per trio
- Net change per trio is ±5 per channel → matches the about page quote: "eating 3 fruits to increase/reduce all values by 5"

**Verification with reference answer (19 Apple, 23 Pear, 32 Berry):**
```
R: 19*(+5) + 23*(-5) + 32*(-5) = 95 - 115 - 160 = -180
G: 19*(-5) + 23*(+5) + 32*(-5) = -95 + 115 - 160 = -140
B: 19*(-5) + 23*(-5) + 32*(+5) = -95 - 115 + 160 = -50

Start:  (219, 180, 87)
Result: (219-180, 180-140, 87-50) = (39, 40, 37)
Target: (43, 41, 35)
Error:  (4, 1, -2) → distance ≈ 4.58

This is the closest achievable with integer fruit counts! ✓
```

**Exact fractional solution:** Apple=19.1, Pear=22.8, Berry=31.5 → rounds to 19, 23, 32. ✓

### Why the Community Wikis Were Misleading
Several community wikis describe the fruit modifiers as "+5 primary, -2 secondary". This is either:
1. An older/incorrect understanding of the mechanic
2. A misinterpretation of data
3. A simplified explanation

The actual mechanic is **+5 primary, -5 secondary** per channel. This is confirmed by:
- Mathematical verification against the reference calculator (ffxiv.pf-n.co)
- Matching the about page's statement that 3 fruits can "reduce all values by 5"
- Producing results consistent with multiple online chocobo color calculators

---

## Fruit Modifier Reference (CORRECTED)

### Positive Fruits (shift colour TOWARDS named hue)
| Fruit | R | G | B | Description |
|-------|-----|-----|-----|-------------|
| Xelphatol Apple | **+5** | -5 | -5 | Shifts towards Red |
| Mamook Pear | -5 | **+5** | -5 | Shifts towards Green |
| O'Ghomoro Berries | -5 | -5 | **+5** | Shifts towards Blue |

### Negative Fruits (shift colour AWAY FROM named hue)
| Fruit | R | G | B | Description |
|-------|-----|-----|-----|-------------|
| Doman Plum | **-5** | +5 | +5 | Shifts away from Red |
| Valfruit | +5 | **-5** | +5 | Shifts away from Green |
| Cieldalaes Pineapple | +5 | +5 | **-5** | Shifts away from Blue |

### Key Combinations
| Combination | Net Effect | Use Case |
|-------------|-----------|----------|
| Apple + Pear + Berry | (-5, -5, -5) | Darken all channels |
| Plum + Valfruit + Pineapple | (+5, +5, +5) | Lighten all channels |
| Apple + Plum | (0, 0, 0) | Cancel out (no change) |
| Pear + Valfruit | (0, 0, 0) | Cancel out (no change) |
| Berry + Pineapple | (0, 0, 0) | Cancel out (no change) |

---

## Algorithm Details

### Greedy with Lookahead (Depth 3)

**Source:** https://ffxiv.pf-n.co/chocobo-color/about

The algorithm works as follows:
1. Start at current colour
2. Generate all possible fruit combinations up to depth 3 (6^3 = 216 paths)
3. Find the path that gets closest to target colour (Euclidean distance)
4. If no path improves current distance, STOP
5. Otherwise, add the FIRST fruit of the best path to the solution
6. Update current colour and repeat from step 2

### Why Lookahead 3?
- Depth 3 is mathematically **proven optimal** (the algorithm returns the closest achievable colour)
- Depth 3 specifically enables the "3 fruits to shift all values by ±5" strategy
- With +5/-5 modifiers, feeding 2 fruits can adjust any individual RGB value by ±10 while adjusting others
- Depth 3 allows the algorithm to temporarily move AWAY from target to get closer later
- 6^3 = 216 combinations is trivial to compute in real-time

### Optimality Guarantee
Per the reference: "a lookahead of 3 is enough to guarantee that the algorithm terminates with a color as close to the target color as possible (ignoring clamping)."

### Clamping
- RGB values are clamped to 0-255 range
- The algorithm applies clamping after each fruit application
- Clamping makes fruit order matter (fruits don't fully commute when values hit bounds)
- The algorithm accounts for clamping through its greedy evaluation

---

## Chocobo Colour Mechanics

### How It Works In-Game
1. Stable your chocobo at a house/apartment/FC
2. Feed it fruits through the stable interface
3. After feeding, wait 6 hours (Earth time) for the colour to change
4. The colour result is based on the cumulative RGB modifications from all fruits fed
5. Use a Han Lemon to reset to Desert Yellow (219, 180, 87) at any time

### Colour Determination
- The game stores internal RGB values for each chocobo
- After processing all fed fruits, the game finds the closest named colour to the resulting RGB
- There are 84+ named chocobo colours
- Default starting colour is Desert Yellow (219, 180, 87)

### Important Notes
- Feeding order matters due to clamping at RGB boundaries
- The calculator provides a recommended order to minimize clamping issues
- If results are wrong in-game, use Han Lemon and retry
- The game rounds/snaps to the nearest named colour after feeding

---

## Test Cases (Verified Against Reference Calculator)

### Test 1: Desert Yellow → Soot Black
- **Start:** Desert Yellow (219, 180, 87)
- **Target:** Soot Black (43, 41, 35)
- **Expected:** ~74 fruits (Apple ×19, Pear ×23, Berry ×32)
- **Expected final RGB:** approximately (39, 40, 37)

### Test 2: Desert Yellow → Snow White ✓
- **Start:** Desert Yellow (219, 180, 87)
- **Target:** Snow White (228, 223, 208)
- **Result:** 34 fruits — Plum ×16, Valfruit ×13, Pineapple ×5
- **Final RGB:** (229, 220, 207), distance 3.32

### Test 3: Desert Yellow → Blood Red ✓
- **Start:** Desert Yellow (219, 180, 87)
- **Target:** Blood Red (165, 48, 34)
- **Result:** 49 fruits — Apple ×19, Pear ×11, Berry ×19
- **Final RGB:** (164, 45, 32), distance 3.74

### Test 4: Desert Yellow → Ink Blue ✓
- **Start:** Desert Yellow (219, 180, 87)
- **Target:** Ink Blue (44, 70, 116)
- **Result:** 50 fruits — Apple ×8, Pear ×14, Berry ×28
- **Final RGB:** (49, 70, 117), distance 5.10

### Test 5: Desert Yellow → Hunter Green ✓
- **Start:** Desert Yellow (219, 180, 87)
- **Target:** Hunter Green (40, 127, 47)
- **Result:** 54 fruits — Apple ×9, Pear ×22, Berry ×23
- **Final RGB:** (39, 130, 47), distance 3.16

---

## Bug Fix: Greedy Oscillation Near Target (2026-02-27)

### The Problem
Even after fixing the fruit modifiers, the algorithm still hit 1000 steps. The first ~72 steps matched the reference perfectly, but the algorithm oscillated at the end between opposing fruits (e.g., Apple → Plum → Apple → Plum forever).

### Root Cause
When two paths achieve the **exact same distance**, the algorithm preferred whichever was found first in the search loop. Because the nested loop checks depth-3 paths from Apple (index 0) before checking depth-1 Berry (index 2), the longer path won ties.

**Example at the oscillation point:**
- From `(44,45,32)`, target `(43,41,35)`:
  - `Apple+Berry+Plum` (depth 3) → `(39,40,37)`, distance = **4.58** (found first, Apple is index 0)
  - `Berry` (depth 1) → `(39,40,37)`, distance = **4.58** (found second, Berry is index 2)
- Both reach the same color! But `Apple+Berry+Plum` was found first and set `bestDist=4.58`
- Berry at depth 1 was **rejected** because `4.58 < 4.58` is `false`
- Algorithm picks Apple → goes wrong direction → needs Plum to fix → oscillation

### Fix
Added "prefer shorter paths on distance ties" to match the reference's "stable sort with empty path prioritized":
```csharp
// Before (bug):
if (distance < bestDistance)

// After (fix):
if (distance < bestDistance || (distance == bestDistance && pathLen < bestPath.Count))
```

This ensures that when a depth-1 fruit reaches the same color as a depth-3 path, the depth-1 path wins. The algorithm then applies Berry directly, reaching (39,40,37), and terminates cleanly.

### Result
Desert Yellow → Soot Black: **74 fruits** (exact match with reference), no oscillation.

---

## Phase 4: Plugin Architecture (2026-02-27)

### Service Layer
The plugin uses a service-oriented architecture. All services are created in `Plugin.cs` and injected into the UI.

| Service | Purpose | Dalamud APIs Used |
|---------|---------|-------------------|
| `GameDataService` | Inventory counts, character info, chocobo colour | `IClientState`, `IObjectTable`, `FFXIVClientStructs.InventoryManager` |
| `PlanStorageService` | JSON persistence for plans + timers | `IDalamudPluginInterface.GetPluginConfigDirectory()` |
| `IpcService` | Pause/resume TextAdvance & YesAlready | `IDalamudPluginInterface.GetIpcSubscriber<>()` |
| `FeedingAutomationService` | State machine for auto-feeding | `IGameGui`, `IFramework`, `AtkUnitBase` callbacks |

### JSON Persistence Structure
File: `{pluginConfigDir}/chocobo_plans.json`
```json
{
  "Characters": {
    "Firstname Lastname@WorldName": {
      "CharacterName": "Firstname Lastname",
      "WorldName": "WorldName",
      "ChocoboName": "Boco",
      "ActivePlan": {
        "StartColorName": "Desert Yellow",
        "TargetColorName": "Soot Black",
        "FruitOrder": ["Xelphatol Apple", "Xelphatol Apple", ...],
        "NextFruitIndex": 0,
        "CreatedUtc": "2026-02-27T20:00:00Z"
      },
      "TimerExpiresUtc": null
    }
  }
}
```

### Feeding Automation State Machine (Inventory-Based)
The actual FFXIV stable feeding UI shows the player's inventory with feedable items highlighted (non-feedable faded). Right-clicking a fruit shows a context menu with "Feed" as the first option.

**Full loop (per fruit after the first):**
```
[First feed: user starts in feed inventory]
FindingFruitSlot → OpeningContextMenu → WaitingForContextMenu → SelectingFeed
  → WaitingForStableMenu (animation plays, menu appears = feed confirmed)
  → RecordFeedSuccess + SelectingFeedFromMenu → WaitingForFeedInventory
  → (loop back to FindingFruitSlot)
  → Completed
```

**There is NO confirmation dialog** — fruit feeds immediately, animation plays, then stable menu reappears.

After each feed, the game returns to the stable menu (in-game title: "Personal Chocobo"):
- **Container addon**: `HousingMyChocobo` (found via `/xldata`)
- **List addon**: May also appear as a separate `SelectString` addon
- Menu options: Train (0), **Feed (1)**, Change Name (2), Fetch (3), View Details (4), Quit (5)
- The menu can take **5+ seconds** to appear — `MenuTimeoutSec = 15f`
- After clicking Feed (index 1), the inventory reopens with feedable items
- **Detection**: Check BOTH `SelectString` and `HousingMyChocobo` — whichever appears first
- **Clicking**: Prefer `SelectString` if found (proven by YesAlready), fall back to `HousingMyChocobo`

**Key interaction flow:**
1. `FindItemSlot()` locates fruit in Inventory1-4 by item ID
2. `AgentInventoryContext.Instance()->OpenForItemSlot(container, slot, 0, addonId)` opens context menu
3. `ContextMenu` addon callback `FireCallback(5, [0, 0, 0u, 0, 0], true)` selects "Feed" (index 0)
4. Wait for stable menu to appear — check BOTH SelectString and HousingMyChocobo (confirms feed completed)
5. Record feed success
6. Click Feed (index 1) via `ClickSelectString` on whichever addon is found
7. Wait for inventory to reopen → repeat from step 1

**Critical: `updateState` parameter** — All `FireCallback` calls must pass `true` as the third parameter:
`addon->FireCallback(valueCount, values, true)`. This matches ECommons' `Callback.Fire(addon, true, args)` pattern. Without it, the game may not process the callback.

**Inventory addon detection:**
Searches these addon names (different inventory layouts): `InventoryExpansion`, `InventoryLarge`, `InventoryGrid0E`-`3E`, `InventoryGrid0`-`3`, `InventoryBuddy`, `InventoryBuddy2`

- Each state has a 10-second timeout (menu states use 15s), 3 retries on context menu failures
- 0.5s pre-delay before opening context menu or clicking menu options
- On error → resumes external plugins and stops
- On completion → deletes plan, starts 6-hour timer

### Stable Condition Check
- Magicked Stable Broom item ID: 8168
- Checkbox in Configuration (default: true)
- If stable condition is Poor or Fair, user should clean before feeding
- Broom count shown in UI for convenience

### IPC Protocol
TextAdvance and YesAlready use a simple enable/disable pattern:
- `TextAdvance.IsEnabled` → `bool` (check state)
- `TextAdvance.SetEnabled` → `(bool, object?)` (set state)
- Same pattern for `YesAlready.*`
- Previous state is saved before pausing, restored after automation completes
- Safety: `Dispose()` always resumes if paused

### Inventory Access
Uses `FFXIVClientStructs.FFXIV.Client.Game.InventoryManager`:
- **Method:** `InventoryManager.Instance()->GetInventoryItemCount(itemId)` (built-in, searches all containers)
- Pattern from SND (SomethingNeedDoing) `InventoryModule.GetItemCount()`
- Previous manual Inventory1-4 iteration was unreliable — replaced with the built-in method
- Returns 0 on any error (graceful fallback)

### FFXIV Fruit Item IDs (verified from xivapi/ffxiv-datamining Item.csv)
| Fruit | Item ID | Notes |
|-------|---------|-------|
| Xelphatol Apple | 8157 | R+5, G-5, B-5 |
| Doman Plum | 8158 | R-5, G+5, B+5 |
| Mamook Pear | 8159 | R-5, G+5, B-5 |
| Valfruit | 8160 | R+5, G-5, B+5 |
| O'Ghomoro Berries | 8161 | R-5, G-5, B+5 |
| Cieldalaes Pineapple | 8162 | R+5, G+5, B-5 |

**WARNING:** The item IDs are NOT in the same order as common wiki listings. The datamining CSV is the authoritative source. Previous incorrect IDs (guessed from wiki order) caused inventory counts to always show 0.

### Chocobo Colour Auto-Detection
Currently returns null. The colour is stored in the companion module but the exact struct offset in FFXIVClientStructs varies by game version. A hover tooltip explains manual lookup as a fallback.

### Dalamud API Compatibility Notes
- `IClientState.LocalPlayer` is obsolete → use `IObjectTable.LocalPlayer`
- `IGameGui.GetAddonByName()` returns `AtkUnitBasePtr` wrapper → cast via `nint` then to `AtkUnitBase*`
- `ValueType` is ambiguous between `System.ValueType` and `FFXIVClientStructs.FFXIV.Component.GUI.ValueType` → fully qualify the latter

---

## Lessons Learned

1. **Don't trust wiki values blindly** — Always verify game mechanics mathematically against known-good calculators
2. **The trio test is key** — If 3 complementary fruits don't produce a ±5 shift on all channels, the modifiers are wrong
3. **Always have a reference test case** — Desert Yellow → Soot Black with known fruit counts is an excellent sanity check
4. **Small modifier errors cause huge algorithm divergence** — The difference between ±2 and ±5 secondary modifiers caused 74 vs 1000+ step results
5. **Shorter paths must win ties** — Without this, the greedy lookahead oscillates at convergence because longer paths are found first in the search loop
6. **Dalamud API evolves rapidly** — Always check for obsolete warnings and wrapper types; cast through `nint` when needed
7. **Always pause automation plugins** — TextAdvance and YesAlready will interfere with dialog-based automation if not paused via IPC
8. **Never guess FFXIV item IDs** — Always verify from the official datamining CSV (`xivapi/ffxiv-datamining`). Wiki ordering does not match item ID ordering. 4 of 6 fruit IDs were wrong when guessed.
9. **Use built-in InventoryManager methods** — `GetInventoryItemCount(itemId)` is simpler and more reliable than manual container iteration. Reference: SND InventoryModule pattern.
10. **FFXIV stable feeding is inventory-based, not dialog-based** — The feed screen shows the player's inventory with feedable items highlighted. You right-click a fruit → context menu → "Feed" (first option). Use `AgentInventoryContext.OpenForItemSlot()` to open context menus programmatically. Reference: Dropbox plugin by Limiana.
11. **Context menu callback pattern** — `FireCallback(5, [0, index, 0u, 0, 0])` to select an option. `SelectYesno` uses `FireCallback(1, [0])` for Yes. Pattern from Dropbox plugin (ECommons `Callback.Fire`).
12. **Stable feeding is NOT a single inventory session** — After each fruit feed, the game returns to the stable SelectString menu (Train/Feed/Change Name/Fetch/View Details/Quit). You must re-select "Feed" (index 1) each time. The menu can take 5+ seconds to appear. Use a 15s timeout.
13. **GetAddonByName returns AtkUnitBasePtr, NOT nint** — In current Dalamud, `IGameGui.GetAddonByName()` returns `AtkUnitBasePtr` (a wrapper type). You MUST use explicit `nint` conversion: `nint ptr = _gameGui.GetAddonByName(name);` — using `var` gives you the wrapper type which cannot be directly cast to `AtkUnitBase*`.
14. **Addon existence ≠ addon visibility** — An addon can have a non-null pointer but `IsVisible = false` during transitions. Use `AddonExists()` (non-null check) for detection, then check `IsVisible` before interaction. SND's `AddonWrapper` models this as `Exists` vs `Ready`.
15. **When pausing TextAdvance, handle Talk dialogs yourself** — After feeding, FFXIV may show a Talk dialog ("You feed your chocobo..."). If TextAdvance is paused, it can't auto-advance this. Check for and dismiss Talk addons manually with `FireCallback(1, [0])`.
16. **Always verify addon names with /xldata** — The stable chocobo menu shows "Personal Chocobo" in-game but the actual addon name is `"HousingMyChocobo"` (NOT `"SelectString"`). Never guess addon names — use `/xldata` in-game to inspect the real name. Generic-looking list menus can be custom addons.
17. **ECommons `Callback.Fire` requires `updateState: true`** — The correct callback pattern from ECommons is `addon->FireCallback(valueCount, values, true)`. The third `bool` parameter (`updateState`) is critical — without it (defaults to `false`), the game may silently ignore the callback. Discovered by reading ECommons `Callback.cs` and `AddonMaster.SelectString` source code. YesAlready uses this pattern for all SelectString interactions.
18. **Dual addon detection for complex UI** — Some FFXIV UI panels use multiple addons simultaneously (e.g., a container window + a list addon). When waiting for a menu to appear, check all possible addon names. The stable chocobo menu may use `HousingMyChocobo` (container), `SelectString` (list), or both. Log pointer values for all candidates to diagnose detection failures.
19. **Addon pointers can linger after dismissal** — An addon's pointer can remain non-null even after the UI element is dismissed or hidden. Always check `IsVisible` in addition to pointer existence. Critical bug: checking `AddonExists("Talk")` then returning early blocked stable menu detection because Talk pointer lingered (0x1706A399E00) even though it wasn't visible. Fix: only return early if `IsVisible == true`.
20. **Use AtkComponentList for menu interactions** — List-based menus (like HousingMyChocobo's Train/Feed/Change Name list) store items in an `AtkComponentList` component. Use `addon->GetComponentListById(3)` to get the list, then `GetItemCount()` to validate the index before firing callbacks. Component ID 3 is common for list menus. This provides better error messages and validates the UI state before interaction.
