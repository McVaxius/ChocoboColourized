# Chocobo Colourized - Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

### 2026-02-27 17:48:00

#### Fixed - Talk addon visibility bug blocking stable menu detection

**Problem:** Automation stuck on `WaitingForStableMenu` even though `HousingMyChocobo` was present (pointer 0x16F50AEA9F0). Logs showed Talk addon also existed (0x1706A399E00) but wasn't visible.

**Root cause:** Code checked `AddonExists("Talk")` (non-null pointer), then called `DismissTalkAddon()` which checked `IsVisible` before dismissing. If Talk existed but wasn't visible, the function returned early and **never checked for HousingMyChocobo**. Talk addon pointer can linger after dismissal.

**Fixes:**
1. **Only return early if Talk is VISIBLE** — Changed `if (AddonExists("Talk"))` to `if (IsTalkAddonVisible())`. If Talk exists but isn't visible, continue to check for stable menu.
2. **Use AtkComponentList API** — Added list component detection in `ClickSelectString`. For `HousingMyChocobo`, menu items are in an `AtkComponentList` (component ID 3). Now logs item count and validates index before firing callback.

**How discovered:** User provided xldata screenshots showing the list component structure and logs showing both Talk and HousingMyChocobo pointers present simultaneously.

#### Files Modified
- `ChocoboColourized/Services/FeedingAutomationService.cs` — Fixed Talk visibility check, added AtkComponentList interaction

#### Files Backed Up
- `backups/20260227_174747_FeedingAutomationService.cs`
- `backups/20260227_174835_CHANGELOG.md`
- `backups/20260227_174835_KNOWLEDGEBASE.md`

#### Build Status
- Build succeeded: 0 errors, 0 warnings

#### Testing Requirements
- Reload plugin, start automation
- Check logs for "List component found: X items, clicking index 1"
- Automation should now progress past WaitingForStableMenu when HousingMyChocobo appears
- If Talk dialog appears, it should be dismissed without blocking stable menu detection

---

### 2026-02-27 17:40:00

#### Fixed - Comprehensive stable menu detection and callback fix (from YesAlready/ECommons research)

**Problem:** After feeding a fruit, the automation was stuck on `WaitingForStableMenu` because:
1. Only checking one addon name — but the stable menu might use `SelectString` (separate list addon) OR `HousingMyChocobo` (the container window), or both
2. `FireCallback` was missing the `updateState: true` parameter — ECommons' `Callback.Fire(addon, true, index)` passes `updateState=true` as the third parameter to `FireCallback`. Without this, the game may not process the callback.

**Research:** Studied YesAlready (`SelectString.cs`) and ECommons (`Callback.cs`, `AddonMaster.SelectString`):
- YesAlready hooks `AddonEvent.PostSetup` on `"SelectString"` addon
- Uses `AddonMaster.SelectString(atk).Entries[index].Select()` to click
- `Select()` calls `Callback.Fire((AtkUnitBase*)Addon, true, Index)`
- `Callback.Fire` translates to `addon->FireCallback(valueCount, values, updateState: true)`

**Fixes:**
1. **Dual detection**: Now checks BOTH `SelectString` and `HousingMyChocobo` — whichever appears first triggers the state transition
2. **`updateState: true`**: All `FireCallback` calls (ClickSelectString, ClickContextMenu, DismissTalkAddon) now pass `true` as the third parameter, matching ECommons pattern
3. **Prefer SelectString for clicking**: When clicking "Feed" from menu, tries SelectString first (proven by YesAlready), falls back to HousingMyChocobo
4. **Better logging**: Logs both addon pointers every ~0.5s instead of ~1s, shows which addon was used for clicking

#### Files Modified
- `ChocoboColourized/Services/FeedingAutomationService.cs` — Major rewrite of detection + clicking logic

#### Files Backed Up
- `backups/20260227_173910_FeedingAutomationService.cs`
- `backups/20260227_174029_CHANGELOG.md`
- `backups/20260227_174029_KNOWLEDGEBASE.md`

#### Build Status
- Build succeeded: 0 errors, 0 warnings

#### Testing Requirements
- Reload plugin, start automation from stable Feed inventory
- Check Dalamud log for `WaitForMenu:` lines — should show pointer values for both addons
- Watch which addon gets detected and used for clicking
- If still stuck, share the `WaitForMenu:` log lines to identify which addons have non-zero pointers

---

### 2026-02-27 17:27:00

#### Fixed - Wrong addon name for stable menu

**Problem:** Automation stuck on `WaitingForStableMenu` because the addon name was `"SelectString"` — but the actual addon name (from `/xldata`) is `"HousingMyChocobo"`. In-game title shows "Personal Chocobo".

**Fix:** Changed `StableMenuAddon` from `"SelectString"` to `"HousingMyChocobo"`.

**How discovered:** User inspected the game UI with `/xldata` and found the backend addon name.

#### Files Modified
- `ChocoboColourized/Services/FeedingAutomationService.cs` — One-line constant change

#### Files Backed Up
- `backups/20260227_172634_FeedingAutomationService.cs`

#### Build Status
- Build succeeded: 0 errors, 0 warnings

---

### 2026-02-27 17:22:00

#### Simplified - Removed confirmation dialog states (no SelectYesno exists)

**Problem:** The state machine had `WaitingForConfirmation`, `ConfirmingFeed`, and `WaitingForFeedComplete` states expecting a SelectYesno dialog after clicking Feed. In reality, **there is no confirmation dialog** — the fruit feeds immediately, an animation plays, then the stable SelectString menu reappears.

**Simplified flow:**
```
FindingFruitSlot → OpeningContextMenu → WaitingForContextMenu → SelectingFeed
  → WaitingForStableMenu (animation plays, menu appears = feed confirmed)
  → RecordFeedSuccess + SelectingFeedFromMenu → WaitingForFeedInventory
  → FindingFruitSlot (next fruit)
```

**Removed:**
- `WaitingForConfirmation` state and handler
- `ConfirmingFeed` state and handler
- `WaitingForFeedComplete` state and handler
- `ConfirmAddon` ("SelectYesno") constant
- `ClickSelectYesNo()` method
- `PostFeedDelaySec` constant

**Key change:** `HandleWaitingForStableMenu` now both confirms the feed (records success) AND branches to the next action (complete or click Feed again). SelectString appearing = feed animation done.

#### Files Modified
- `ChocoboColourized/Services/FeedingAutomationService.cs` — Removed 3 states, 3 handlers, simplified flow

#### Files Backed Up
- `backups/20260227_171910_FeedingAutomationService.cs`

#### Build Status
- Build succeeded: 0 errors, 0 warnings

#### Testing Required
- [ ] First fruit feeds from context menu
- [ ] Stable menu detected after animation
- [ ] Feed recorded, "Feed" clicked in menu
- [ ] Inventory reopens, next fruit found
- [ ] Full cycle through all planned fruits

---

### 2026-02-27 17:15:00

#### Fixed - SelectString detection failure after feeding

**Problem:** After the first successful feed, the automation waited 15 seconds for the SelectString menu but timed out — even though the menu was visibly on screen. Two root causes:

**Root Cause 1: IsVisible check too strict**
- `GetAddonByName` returns `AtkUnitBasePtr` (Dalamud wrapper), not raw `nint`
- The addon can exist (non-null pointer) but have `IsVisible = false` during transitions
- SND's `AddonWrapper` separates `Exists` (non-null) from `Ready` (visible + loaded)
- **Fix:** Added `AddonExists()` method that checks pointer non-null WITHOUT requiring IsVisible
- `HandleWaitingForStableMenu` now uses `AddonExists` for detection
- `HandleSelectingFeedFromMenu` falls back to raw pointer if `GetAddon` (IsVisible check) fails

**Root Cause 2: Intermediate Talk dialog blocking**
- After feeding, FFXIV may show a Talk dialog ("You feed your chocobo...")
- TextAdvance was paused (by our IPC call), so it couldn't auto-advance this dialog
- **Fix:** Added `DismissTalkAddon()` that detects and dismisses Talk dialogs during `WaitingForStableMenu`

**Additional improvements:**
- `HandleWaitingForConfirmation` now also checks if SelectString appeared directly (feed completed without SelectYesno)
- Debug logging every ~1 second in `WaitingForStableMenu` showing addon pointer values
- 1.0s delay in `SelectingFeedFromMenu` (up from 0.5s) for menu render time
- All `GetAddonByName` calls now use explicit `nint` conversion (not `var`) to avoid `AtkUnitBasePtr` cast issues

#### Files Modified
- `ChocoboColourized/Services/FeedingAutomationService.cs` — Fixed detection + Talk handling + debug logging

#### Files Backed Up
- `backups/20260227_171227_FeedingAutomationService.cs`

#### Build Status
- Build succeeded: 0 errors, 0 warnings

#### Testing Required
- [ ] First fruit feeds successfully
- [ ] Check Dalamud log for debug messages: "WaitForMenu: elapsed=..." showing pointer values
- [ ] If Talk dialog appears after feed, it should be auto-dismissed
- [ ] SelectString menu detected and "Feed" clicked automatically
- [ ] Full feeding cycle completes through all fruits

#### Key Lesson
- `GetAddonByName` returns `AtkUnitBasePtr` in current Dalamud, NOT `nint` — must use explicit `nint` conversion
- Addon existence (non-null pointer) ≠ addon visibility (`IsVisible` flag)
- When pausing TextAdvance, you must handle any Talk dialogs yourself

---

### 2026-02-27 17:05:00

#### Fixed - Feeding automation loop (stable menu re-navigation)

**Problem:** After successfully feeding the first fruit, the game returns to the stable SelectString menu (Train/Feed/Change Name/Fetch/View Details/Quit). The automation was immediately trying to find the next fruit in inventory without re-selecting "Feed" from this menu, causing it to spam item searches.

**Actual in-game loop per fruit:**
1. Feed item from inventory (context menu → Feed → confirm)
2. Game returns to stable SelectString menu (can take **5+ seconds**)
3. Select "Feed" (index 1) from the menu
4. Wait for inventory to reopen with feedable items
5. Repeat from 1

**New states added:**
- `WaitingForStableMenu` — waits for SelectString to reappear (15s timeout)
- `SelectingFeedFromMenu` — clicks "Feed" (index 1) with 0.5s render delay
- `WaitingForFeedInventory` — waits for any inventory addon to become visible

**Constants:**
- `MenuTimeoutSec = 15f` (longer timeout since menu can take 5+ seconds)
- `StableMenuFeedIndex = 1` (Train=0, Feed=1, Change Name=2, Fetch=3, View Details=4, Quit=5)

#### Files Modified
- `ChocoboColourized/Services/FeedingAutomationService.cs` — Added 3 new states + handlers for stable menu loop

#### Files Backed Up
- `backups/20260227_170010_FeedingAutomationService.cs`

#### Build Status
- Build succeeded: 0 errors, 0 warnings

#### Testing Required
- [ ] First fruit feeds successfully (same as before)
- [ ] After first feed, automation waits for stable menu to reappear
- [ ] Automation clicks "Feed" from the menu automatically
- [ ] Inventory reopens with feedable items
- [ ] Second fruit feeds successfully, loop continues
- [ ] All fruits fed → plan completes

---

### 2026-02-27 16:50:00

#### Rewritten - Feeding Automation (inventory-based context menu approach)

**Problem:** The old FeedingAutomationService assumed a dialog-based flow (SelectString menus). The actual FFXIV stable feeding UI shows the player's inventory with feedable items highlighted. You right-click a fruit and select "Feed" from the context menu.

**New approach:**
1. Find the target fruit in player's inventory (InventoryType.Inventory1-4, slot index)
2. Use `AgentInventoryContext.Instance()->OpenForItemSlot()` to open context menu on the fruit
3. Wait for `ContextMenu` addon to appear
4. Fire callback to select first option ("Feed") — pattern from Dropbox plugin
5. Wait for `SelectYesno` confirmation dialog
6. Fire callback to confirm (Yes)
7. Wait for feed processing delay, then repeat for next fruit

**New state machine:**
`FindingFruitSlot → OpeningContextMenu → WaitingForContextMenu → SelectingFeed → WaitingForConfirmation → ConfirmingFeed → WaitingForFeedComplete → (loop or Completed)`

**Key technical details:**
- `AgentInventoryContext.OpenForItemSlot(InventoryType, int slot, int a4, uint addonId)` from FFXIVClientStructs
- Searches multiple inventory addon names for different layouts (Normal, Expanded, Large)
- ContextMenu callback: `FireCallback(5, [0, index, 0u, 0, 0])` — from Dropbox plugin pattern
- SelectYesno callback: `FireCallback(1, [0])` for Yes
- Retries up to 3 times on context menu failures
- 0.3s pre-delay before opening context menu, 2.0s post-feed delay

#### Added - Stable Condition Check
- New `CheckStableCondition` setting in Configuration (default: true)
- Checkbox in Automation tab with tooltip explaining the feature
- Shows Magicked Stable Broom (ID: 8168) inventory count
- If stable condition is Poor or Fair, user should clean with broom before feeding

#### Added - GameDataService.FindItemSlot()
- New method to locate a specific item by ID, returning `(InventoryType, int slot)?`
- Searches Inventory1-4 containers, returns first match
- Used by FeedingAutomationService to find fruits for context menu interaction

#### Added - GameDataService.GetItemCount(uint itemId)
- Generic item count method (not fruit-specific)
- Used for checking Magicked Stable Broom availability

#### Files Modified
- `ChocoboColourized/Services/FeedingAutomationService.cs` — Complete rewrite (dialog → inventory context menu)
- `ChocoboColourized/Services/GameDataService.cs` — Added `FindItemSlot()` and `GetItemCount()`
- `ChocoboColourized/Configuration.cs` — Added `CheckStableCondition` property
- `ChocoboColourized/Windows/MainWindow.cs` — Updated automation tab with stable condition UI + new instructions

#### Files Backed Up
- `backups/20260227_164442_FeedingAutomationService.cs`
- `backups/20260227_164442_GameDataService.cs`
- `backups/20260227_164442_Configuration.cs`
- `backups/20260227_164442_MainWindow.cs`

#### Build Status
- Build succeeded: 0 errors, 0 warnings

#### Testing Required
- [ ] Reload plugin in Dalamud
- [ ] Navigate to chocobo stable → select Feed → inventory opens with feedable items highlighted
- [ ] Click Start Automated Feeding in plugin
- [ ] Verify context menu opens on first fruit
- [ ] Verify "Feed" is selected from context menu
- [ ] Verify confirmation dialog is handled
- [ ] Verify fruit count decreases after each feed
- [ ] Verify automation continues through all planned fruits
- [ ] Verify stable condition checkbox works (check/uncheck persists)
- [ ] Verify Magicked Stable Broom count shows correctly

#### References
- AgentInventoryContext API: https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/UI/Agent/AgentInventoryContext.cs
- Dropbox plugin context menu callback pattern: https://github.com/Limiana/Dropbox
- Magicked Stable Broom item ID: 8168 (from xivapi/ffxiv-datamining Item.csv)

---

### 2026-02-27 16:12:00

#### Fixed - Inventory counts always showing 0

**Root Cause 1: Wrong item IDs (4 of 6 were incorrect)**
- Old mapping was guessed and scrambled:
  - Mamook Pear was 8158 → actually **Doman Plum**
  - O'Ghomoro Berries was 8159 → actually **Mamook Pear**
  - Doman Plum was 8160 → actually **Valfruit**
  - Valfruit was 8161 → actually **O'Ghomoro Berries**
- Corrected from official `xivapi/ffxiv-datamining` Item.csv:
  - 8157 = Xelphatol Apple ✓ (was correct)
  - 8158 = Doman Plum (was Mamook Pear)
  - 8159 = Mamook Pear (was O'Ghomoro Berries)
  - 8160 = Valfruit (was Doman Plum)
  - 8161 = O'Ghomoro Berries (was Valfruit)
  - 8162 = Cieldalaes Pineapple ✓ (was correct)

**Root Cause 2: Manual inventory iteration was unreliable**
- Old code manually iterated Inventory1-4 containers and checked slots
- Replaced with `InventoryManager.Instance()->GetInventoryItemCount(itemId)` 
- This is the proven pattern from SND (SomethingNeedDoing) InventoryModule
- The built-in method searches all relevant inventory containers automatically

#### Files Modified
- `ChocoboColourized/Core/FruitData.cs` — Fixed all 6 item IDs to match datamining CSV
- `ChocoboColourized/Services/GameDataService.cs` — Replaced manual iteration with `GetInventoryItemCount()`

#### Files Backed Up
- `backups/20260227_161051_FruitData.cs`
- `backups/20260227_161051_GameDataService.cs`

#### Build Status
- Build succeeded: 0 errors, 0 warnings

#### Testing Required
- [ ] Reload plugin in Dalamud
- [ ] Have at least 1 of any chocobo fruit in inventory
- [ ] Calculate a feeding path — verify "Owned" column shows correct non-zero counts
- [ ] Verify all 6 fruit types show correct counts
- [ ] If all fruits are present and sufficient, verify Start button becomes enabled

#### Reference
- Item IDs source: https://raw.githubusercontent.com/xivapi/ffxiv-datamining/refs/heads/master/csv/en/Item.csv
- Inventory pattern source: https://github.com/Jaksuhn/SomethingNeedDoing/blob/0f05b6c73add5ef1709a2c3b710d1391d96fd441/SomethingNeedDoing/LuaMacro/Modules/InventoryModule.cs#L24

---

### 2026-02-27 15:34:00

#### Added - Five Major Features (Phase 4)

**Feature 1: Automated Feeding (Start Button)**
- State machine-based automation service (`FeedingAutomationService.cs`)
- Walks through stable dialog flow: menu → fruit selection → confirmation → feed complete
- Progress display with current fruit, step count, and state
- Error handling with timeouts per state (10s default)
- Stop button to halt automation at any time
- Pauses TextAdvance and YesAlready via IPC before starting, resumes after completion

**Feature 2: Six-Hour Timer List (Per Character)**
- Dedicated "Timers" tab showing all characters with active timers or plans
- Displays: character name, world, chocobo name, status, countdown
- Supports multiple characters (alts on the same account)
- Countdown updates in real-time
- Data persists via JSON in plugin config directory

**Feature 3: Inventory Requirement Display**
- Fruit summary table now shows 4 columns: Fruit, Required, Owned, Status
- Colour-coded status: **Red** (0 owned), **Yellow** (owned > 0 but < required), **Green** (owned ≥ required)
- Reads inventory via FFXIVClientStructs `InventoryManager`
- Graceful fallback when not logged in (shows "?" instead)

**Feature 4: Start Button Gating + JSON Persistence**
- Start button disabled unless player has enough fruit for the full remaining plan
- Plans stored per-character in JSON (`chocobo_plans.json` in plugin config dir)
- Partial feeding decrements `NextFruitIndex` in the plan
- On plan completion: active plan deleted, 6-hour timer stored
- Chocobo name input persisted per character
- Clear Plan button available at all times

**Feature 5: Fetch Chocobo Colour In-Game**
- Auto-detection attempted via `GameDataService.TryGetChocoboColor()`
- Currently returns null (struct offset TBD for current game version)
- Hover tooltip `(?)` explains how to manually find your chocobo's colour:
  - Open Companion window → Appearance tab → Check current colour

#### Files Created
- `ChocoboColourized/Models/CharacterPlanData.cs` — Data models: `CharacterPlanData`, `FeedingPlan`, `PluginPersistentData`
- `ChocoboColourized/Services/PlanStorageService.cs` — JSON persistence for plans and timers
- `ChocoboColourized/Services/GameDataService.cs` — Inventory counts, character info, chocobo colour detection
- `ChocoboColourized/Services/IpcService.cs` — TextAdvance/YesAlready pause/resume via Dalamud IPC
- `ChocoboColourized/Services/FeedingAutomationService.cs` — State machine for automated stable feeding

#### Files Modified
- `ChocoboColourized/Plugin.cs` — Added service injections (IClientState, IGameGui, IFramework, IObjectTable), service initialization and disposal
- `ChocoboColourized/Windows/MainWindow.cs` — Complete rewrite with 3 tabs (Calculator, Timers, Automation), all 5 features integrated
- `ChocoboColourized/Core/FruitData.cs` — Added FFXIV item IDs for inventory lookups

#### Files Backed Up
- `backups/20260227_153454_Plugin.cs`
- `backups/20260227_153454_Configuration.cs`
- `backups/20260227_153454_MainWindow.cs`
- `backups/20260227_153454_ConfigWindow.cs`
- `backups/20260227_153454_ChocoboColor.cs`
- `backups/20260227_153454_ColorCalculator.cs`
- `backups/20260227_153454_FruitData.cs`
- `backups/20260227_153454_ColorDatabase.cs`

#### Architecture
```
Plugin.cs (entry point)
├── Services/
│   ├── GameDataService      — inventory, character info, chocobo colour
│   ├── PlanStorageService   — JSON persistence (chocobo_plans.json)
│   ├── IpcService           — TextAdvance/YesAlready IPC
│   └── FeedingAutomationService — state machine for auto-feeding
├── Models/
│   ├── CharacterPlanData    — per-character plan + timer data
│   ├── FeedingPlan          — ordered fruit list with progress tracking
│   └── PluginPersistentData — root JSON object
├── Core/
│   ├── ChocoboColor         — RGB struct with fruit application
│   ├── ColorCalculator      — greedy lookahead algorithm
│   ├── ColorDatabase        — 84 named colours
│   └── FruitData            — fruit types, modifiers, item IDs
└── Windows/
    ├── MainWindow           — 3 tabs: Calculator, Timers, Automation
    └── ConfigWindow         — settings
```

#### Build Status
- Build succeeded: 0 errors, 0 warnings

#### Testing Required
- [ ] Reload plugin in Dalamud (disable → re-enable)
- [ ] Open with `/chococolor` — verify 3 tabs appear
- [ ] **Calculator tab:** Calculate Desert Yellow → Soot Black, verify 74 fruits
- [ ] **Calculator tab:** Verify inventory columns show owned counts (or "?" if not logged in)
- [ ] **Calculator tab:** Verify colour coding (red/yellow/green) on inventory status
- [ ] **Calculator tab:** Enter chocobo name, save plan — verify plan saves
- [ ] **Timers tab:** After saving a plan, verify it appears in the timer list
- [ ] **Automation tab:** Verify plan summary and progress bar display
- [ ] **Automation tab:** At stable, click Start — verify TextAdvance/YesAlready pause
- [ ] **Automation tab:** Verify feeding progresses through correct fruit sequence
- [ ] **Automation tab:** Verify Stop button halts automation and resumes external plugins
- [ ] **Automation tab:** After full feeding, verify 6-hour timer appears
- [ ] **Timers tab:** Verify countdown timer decrements correctly
- [ ] **Calculator tab:** Hover (?) tooltip — verify manual colour lookup instructions
- [ ] Switch characters — verify separate plan/timer data per character
- [ ] Close and reopen game — verify JSON data persists

---

### 2026-02-27 15:06:00

#### Fixed - Algorithm producing 1000 steps instead of ~74

#### Root Causes (Two bugs)

**Bug 1: Wrong fruit RGB modifiers**
- Old: `(+5, -2, -2)` pattern → trio sum `(+1,+1,+1)` → hundreds of fruits needed
- New: `(+5, -5, -5)` pattern → trio sum `(-5,-5,-5)` → matches reference calculator
- Verified mathematically: 19 Apple + 23 Pear + 32 Berry from Desert Yellow gives (39,40,37), distance 4.58 to Soot Black

**Bug 2: Greedy oscillation near target**
- When two paths (e.g., `Berry` depth-1 and `Apple+Berry+Plum` depth-3) reach the same distance, the longer path won because it was found first in the search loop
- Algorithm picked Apple → went wrong direction → Plum to fix → infinite oscillation
- Fix: prefer shorter paths on distance ties (`distance == bestDistance && pathLen < bestPath.Count`)

#### Files Modified
- `ChocoboColourized/Core/FruitData.cs` — Fixed all 6 fruit RGB modifiers
- `ChocoboColourized/Core/ColorCalculator.cs` — Added shorter-path-on-tie preference
- `PROJECT_PLAN.md` — Updated fruit modifier documentation

#### Files Created
- `KNOWLEDGEBASE.md` — Full documentation of mechanics, bugs, fixes, and verified test cases
- `TestAlgorithm/` — Standalone test project for algorithm verification

#### Files Backed Up
- `backups/20260227_145643_FruitData.cs`
- `backups/20260227_145643_ColorCalculator.cs`
- `backups/20260227_145643_MainWindow.cs`

#### Verified Test Results
| Route | Fruits | Result |
|-------|--------|--------|
| Desert Yellow → Soot Black | 74 (Apple×19, Pear×23, Berry×32) | **Exact match with reference** |
| Desert Yellow → Snow White | 34 (Plum×16, Valfruit×13, Pineapple×5) | ✓ |
| Desert Yellow → Blood Red | 49 (Apple×19, Pear×11, Berry×19) | ✓ |
| Desert Yellow → Ink Blue | 50 (Apple×8, Pear×14, Berry×28) | ✓ |
| Desert Yellow → Hunter Green | 54 (Apple×9, Pear×22, Berry×23) | ✓ |

#### Build Status
- Build succeeded: 0 errors, 0 warnings

#### Testing Required
- [ ] Reload plugin in Dalamud (disable → re-enable)
- [ ] Open with `/chococolor`
- [ ] Desert Yellow → Soot Black: should show 74 fruits (Apple×19, Pear×23, Berry×32)
- [ ] Try Snow White, Blood Red, other targets — all should be under 100 fruits
- [ ] Verify feeding order matches the reference website
- [ ] No more 1000-step results

---

### 2026-02-27 14:30:00

#### Added - Phases 1-3: Core Data, Algorithm, and Calculator UI

#### Files Created
- `ChocoboColourized/Core/ChocoboColor.cs` - Color struct with RGB, distance calculation, clamping, fruit application
- `ChocoboColourized/Core/FruitData.cs` - FruitType enum, FruitModifier struct, display names, RGB modifiers
- `ChocoboColourized/Core/ColorDatabase.cs` - All 84 named chocobo colours with RGB values
- `ChocoboColourized/Core/ColorCalculator.cs` - Greedy+lookahead algorithm (depth 3), CalculationResult class

#### Files Modified
- `ChocoboColourized/Windows/MainWindow.cs` - Replaced Phase 0 placeholder with full calculator UI

#### Files Backed Up
- `backups/20260227_142555_Plugin.cs`
- `backups/20260227_142555_Configuration.cs`
- `backups/20260227_142555_MainWindow.cs`
- `backups/20260227_142555_ConfigWindow.cs`

#### Details
**Phase 1 - Core Data Structures:**
- `ChocoboColor` struct: RGB properties with 0-255 clamping, Euclidean distance calculation, fruit application with clamping, path application, equality/hashcode
- `FruitData`: 6 fruit types with RGB modifiers (+5/-2/-2 pattern), display name mapping
- `ColorDatabase`: 84 named colours organized by hue family (whites, greys, pinks, reds, oranges, browns, yellows, greens, blues, purples), lookup by name/index, closest color finder

**Phase 2 - Algorithm:**
- Greedy algorithm with configurable lookahead depth (default 3)
- Explores all fruit combinations up to depth (6^3 = 216 paths per iteration)
- Safety limit of 1000 iterations to prevent infinite loops
- Returns CalculationResult with: fruit list, start/target/final colors, distance, closest named color, fruit counts

**Phase 3 - Calculator UI:**
- Current/target color selection via dropdown combos (all 84 colors)
- Color preview squares with RGB values
- "Calculate Feeding Path" button
- Results display: final color preview, closest named color, distance metric
- Fruit summary table (name + quantity)
- Scrollable feeding order list (recommended sequence)
- "Copy Results to Clipboard" button with formatted output
- Same-color warning when current == target

#### Build Status
- Build succeeded: 0 errors, 0 warnings
- Output: `ChocoboColourized/bin/x64/Debug/ChocoboColourized.dll`

#### Reason
- Implementing core functionality to make the plugin useful
- Providing complete color calculation workflow in a single update

#### Testing Required
- [ ] Rebuild plugin (`dotnet build` or via Visual Studio)
- [ ] Reload plugin in Dalamud (disable → re-enable)
- [ ] Open with `/chococolor`
- [ ] Select "Desert Yellow" as current colour (default chocobo)
- [ ] Select "Soot Black" as target colour
- [ ] Click "Calculate Feeding Path" - should show fruit list
- [ ] Verify fruit summary table appears with counts
- [ ] Verify feeding order list is scrollable
- [ ] Click "Copy Results to Clipboard" and paste somewhere to verify
- [ ] Try several different colour combinations
- [ ] Select same colour for both → should show yellow warning
- [ ] Verify colour preview squares match selected colours
- [ ] Check `/xllog` for any runtime errors

---

### 2026-02-27 14:14:00

#### Added - Phase 0 Implementation
- Created `ChocoboColourized.sln` - Solution file
- Created `ChocoboColourized/ChocoboColourized.csproj` - Project file (Dalamud.NET.Sdk 14.0.2, .NET 8)
- Created `ChocoboColourized/ChocoboColourized.json` - Plugin manifest
- Created `ChocoboColourized/Plugin.cs` - Main plugin entry point with `/chococolor` command
- Created `ChocoboColourized/Configuration.cs` - Plugin configuration class
- Created `ChocoboColourized/Windows/MainWindow.cs` - Main UI window (placeholder)
- Created `ChocoboColourized/Windows/ConfigWindow.cs` - Settings window
- Created `.gitignore` - Git ignore rules (includes backups/)

#### Details
- Based on goatcorp/SamplePlugin template, fully renamed to ChocoboColourized
- Stripped out SamplePlugin-specific features (goat image, player state display)
- MainWindow shows a simple confirmation message to verify plugin loads
- ConfigWindow has a basic "movable window" toggle
- Command `/chococolor` toggles the main window
- Plugin registers with Dalamud's UiBuilder for Draw, OpenConfigUi, and OpenMainUi
- Proper Dispose() cleanup: unregisters events, removes windows, removes command handler

#### Reason
- Establish minimal working plugin that can load in Dalamud
- Verify build toolchain and dependencies work
- Provide foundation for Phase 1+ feature development

#### Testing Required
- [ ] Open `ChocoboColourized.sln` in Visual Studio 2022
- [ ] Build the solution (Debug|x64)
- [ ] Locate `ChocoboColourized/bin/x64/Debug/ChocoboColourized.dll`
- [ ] Add DLL path to Dalamud dev plugin locations (`/xlsettings` > Experimental)
- [ ] Enable plugin in Plugin Installer (`/xlplugins` > Dev Tools)
- [ ] Type `/chococolor` in chat - main window should appear
- [ ] Verify "Open Settings" button opens config window
- [ ] Verify no errors in Dalamud log (`/xllog`)
- [ ] Disable and re-enable plugin without crashes

---

### 2026-02-27 14:06:00

#### Added
- Created `PROJECT_PLAN.md` - Comprehensive project plan and roadmap
- Created `HOW_TO_IMPORT_PLUGINS.md` - User guide for loading Dalamud plugins
- Created `CHANGELOG.md` - This changelog file for tracking all project changes

#### Details
**PROJECT_PLAN.md:**
- Defined complete project structure and architecture
- Documented all 6 phases of development rollout
- Specified algorithm implementation (Greedy with Lookahead depth 3)
- Listed all required tools and applications
- Defined backup strategy (timestamped backups before every edit)
- Created quality assurance checklist
- Estimated timeline: 17-25 hours total development time

**HOW_TO_IMPORT_PLUGINS.md:**
- Step-by-step guide for loading dev plugins in Dalamud
- Troubleshooting section for common issues
- Command reference table
- Environment variable setup instructions
- Update and uninstall procedures

**CHANGELOG.md:**
- Established changelog format and structure
- Ready to track all future changes

#### Reason
- Establishing project foundation and documentation before any code is written
- Ensuring clear roadmap and expectations
- Providing user with import instructions for testing Phase 0
- Creating systematic change tracking from project start

#### Testing Required
**User Action:**
- Review `PROJECT_PLAN.md` to confirm project scope and approach
- Review `HOW_TO_IMPORT_PLUGINS.md` to understand plugin loading process
- Confirm readiness to proceed with Phase 0 implementation

**Next Steps:**
- Wait for user approval of project plan
- Once approved, begin Phase 0: Clone SamplePlugin template and set up basic plugin structure
- Build and test plugin loading in-game per `HOW_TO_IMPORT_PLUGINS.md`

---

## Template for Future Entries

```markdown
### YYYY-MM-DD HH:MM:SS

#### Added / Changed / Fixed / Removed
- List of changes

#### Files Modified
- `path/to/file1.cs`
- `path/to/file2.json`

#### Details
Detailed description of what was implemented and why.

#### Reason
Explanation of why these changes were necessary.

#### Testing Required
- [ ] Specific test 1
- [ ] Specific test 2
- [ ] User verification needed
```

---

## Notes

- All timestamps are in local time (UTC-5)
- Backups are created automatically before any file edits
- Each phase completion will be marked with a version tag
- Breaking changes will be clearly marked
