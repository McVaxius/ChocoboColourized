# Chocobo Colourized - FFXIV Plugin Project Plan

## Project Overview

**Plugin Name:** Chocobo Colourized  
**Purpose:** Calculate and display the optimal feeding path to change your chocobo companion's color from its current state to a user-selected target color using various fruits/foods.  
**Platform:** FFXIV via Dalamud/XIVLauncher  
**Language:** C# (.NET 8)  
**Development Approach:** Incremental rollout with testing at each phase

---

## Core Concept

The plugin will implement an optimized chocobo color calculation algorithm that:
- Takes the current chocobo RGB color values as input
- Takes the desired target RGB color values as input
- Calculates the optimal sequence of fruits to feed
- Displays the feeding path in an easy-to-understand UI
- Minimizes the number of fruits needed while accounting for RGB clamping mechanics

---

## Applications & Tools Required

### Development Environment
- **Visual Studio 2022** or **JetBrains Rider** - Primary C# IDE
- **.NET 8 SDK** - Required for Dalamud plugin development
- **Git** - Version control and backup management

### FFXIV Specific
- **XIVLauncher** - Custom game launcher
- **Dalamud** - Plugin framework for FFXIV
- **FINAL FANTASY XIV** - The game itself (must be run with Dalamud at least once)

### Reference Materials
- **SamplePlugin Repository** - Template and structure reference
- **Dalamud Developer Docs** - API documentation
- **OptimizedChocoboColoring** - Algorithm reference implementation
- **ffxiv.pf-n.co/chocobo-color** - Algorithm theory and mathematics

---

## Technical Breakdown

### 1. Plugin Architecture

#### Core Components
```
ChocoboColourized/
├── Plugin.cs                    # Main plugin entry point
├── Configuration.cs             # Settings persistence
├── PluginUI.cs                  # Main UI window
├── Windows/
│   ├── MainWindow.cs           # Primary color calculator interface
│   ├── ConfigWindow.cs         # Settings/configuration window
│   └── HelpWindow.cs           # Tutorial and information
├── Core/
│   ├── ColorCalculator.cs      # Core algorithm implementation
│   ├── ChocoboColor.cs         # Color data structure (RGB values)
│   ├── FruitData.cs            # Fruit definitions and RGB modifiers
│   └── PathFinder.cs           # Lookahead pathfinding algorithm
├── Data/
│   └── FruitDatabase.json      # Fruit names, effects, and metadata
└── ChocoboColourized.json      # Plugin manifest
```

#### Key Classes & Responsibilities

**Plugin.cs**
- Initialize Dalamud services
- Register commands (e.g., `/chococolor`)
- Manage plugin lifecycle
- Handle UI window toggling

**ColorCalculator.cs**
- Implement greedy algorithm with lookahead
- Handle RGB clamping (0-255 bounds)
- Calculate distance metrics between colors
- Generate optimal fruit feeding sequence

**ChocoboColor.cs**
- Store RGB values (Red, Green, Blue)
- Implement color distance calculation (Euclidean distance)
- Handle color addition with clamping
- Provide color name lookup (e.g., "Snow White", "Soot Black")

**FruitData.cs**
- Define fruit types and their RGB modifiers
- Map fruit names to in-game items
- Store fruit costs/availability information

**PathFinder.cs**
- Implement lookahead algorithm (depth 3 recommended)
- Generate all possible fruit combinations
- Sort paths by distance to target
- Handle early termination conditions

**MainWindow.cs**
- Current color input (RGB sliders or color picker)
- Target color selection (dropdown or RGB input)
- "Calculate" button
- Results display (fruit list with quantities)
- Copy to clipboard functionality

---

### 2. Algorithm Implementation

#### Algorithm Choice: Greedy with Lookahead (Depth 3)

**Why This Algorithm:**
- **Optimal Results:** Lookahead depth of 3 guarantees optimal solution (closest possible color)
- **Performance:** Computationally feasible for real-time calculation
- **Handles Edge Cases:** Can navigate situations where immediate greedy choice fails
- **Clamping Aware:** Can utilize RGB clamping mechanics when beneficial

#### Algorithm Steps:
1. Start with current chocobo color
2. Compute all possible fruit combinations up to depth 3
3. Find the path that gets closest to target color
4. If no path improves distance, terminate
5. Otherwise, add first fruit from best path to solution
6. Update current color and repeat

#### Data Collection Requirements

**Fruit Database:**
- 6 fruit types with RGB modifiers:
  - **Xelphatol Apple:** +5 Red, -5 Green, -5 Blue
  - **Mamook Pear:** -5 Red, +5 Green, -5 Blue
  - **O'Ghomoro Berries:** -5 Red, -5 Green, +5 Blue
  - **Doman Plum:** -5 Red, +5 Green, +5 Blue
  - **Valfruit:** +5 Red, -5 Green, +5 Blue
  - **Cieldalaes Pineapple:** +5 Red, +5 Green, -5 Blue

**Color Database:**
- 85 possible chocobo colors with RGB values
- Color names (e.g., "Desert Yellow", "Ash Grey", "Grape Purple")
- Organized by hue families for easier selection

**Why We Need This Data:**
- Fruit modifiers are fixed game mechanics
- Color names improve UX (users think in color names, not RGB)
- Pre-calculated color database enables dropdown selection
- Validation against impossible colors

---

### 3. User Interface Design

#### Main Window Features
- **Current Color Section:**
  - RGB sliders (0-255 for each channel)
  - OR dropdown with named colors
  - Visual preview of current color
  
- **Target Color Section:**
  - Same input options as current color
  - Visual preview of target color
  
- **Calculate Button:**
  - Large, prominent button
  - Shows "Calculating..." state during processing
  
- **Results Panel:**
  - Ordered list of fruits to feed
  - Quantity of each fruit type needed
  - Total fruit count
  - Final color preview (may differ slightly from target)
  - Distance metric (how close we got)
  - "Copy to Clipboard" button

#### Configuration Window
- Algorithm settings (lookahead depth - advanced users)
- UI preferences (theme, window size)
- Reset to defaults button

#### Help Window
- Quick tutorial on how to use the plugin
- Explanation of chocobo color mechanics
- Link to algorithm source/credits

---

### 4. Backup & Version Control Strategy

#### Automated Backup System
Every file edit will trigger:
1. **Pre-Edit Backup:**
   - Copy original file to `backups/YYYYMMDD_HHMMSS_filename.ext`
   - Timestamp format: `20260227_140530_Plugin.cs`
   - Preserves directory structure within backups folder

2. **Backup Folder Structure:**
   ```
   backups/
   ├── 20260227_140530_Plugin.cs
   ├── 20260227_141205_ColorCalculator.cs
   └── 20260227_142018_MainWindow.cs
   ```

#### Changelog Management
- **File:** `CHANGELOG.md`
- **Format:** Markdown with timestamps
- **Content per entry:**
  - Date and time of change
  - Files modified
  - Description of what was implemented/changed
  - Reason for the change
  - Testing notes

**Example Entry:**
```markdown
## 2026-02-27 14:05:30

### Files Modified
- `Plugin.cs`
- `ChocoboColourized.json`

### Changes
- Initialized basic plugin structure
- Added command registration for `/chococolor`
- Configured plugin manifest with metadata

### Reason
- Establishing foundation for plugin functionality
- Enabling in-game command interface

### Testing Required
- Load plugin in Dalamud dev tools
- Verify `/chococolor` command is recognized
- Check plugin appears in plugin list
```

---

### 5. Quality Assurance Process

#### Pre-Release Checks (Automated Where Possible)

**Syntax Validation:**
- Build solution in Release mode
- Verify no compiler errors or warnings
- Check for unused using statements

**Memory Management:**
- Review IDisposable implementations
- Verify event handler cleanup in Dispose()
- Check for circular references
- Monitor for resource leaks during testing

**Functionality Verification:**
- Plugin loads without errors
- UI windows open and close properly
- Commands execute correctly
- Configuration saves and loads
- Algorithm produces expected results

**Testing Checklist Per Update:**
- [ ] Plugin compiles successfully
- [ ] No runtime exceptions on load
- [ ] UI renders correctly
- [ ] Commands respond as expected
- [ ] Configuration persists across restarts
- [ ] Algorithm accuracy verified with known test cases
- [ ] Memory usage remains stable

---

## Phase Rollout Plan

### Phase 0: Foundation & Setup ✓ (Current Phase)
**Goal:** Establish project structure and verify basic plugin loading

**Tasks:**
- [x] Create project plan document
- [ ] Create "How to Import Plugins" guide
- [ ] Set up Git repository
- [ ] Clone SamplePlugin template
- [ ] Rename all references to "ChocoboColourized"
- [ ] Update plugin manifest (ChocoboColourized.json)
- [ ] Build solution
- [ ] Load plugin in Dalamud dev tools

**Success Criteria:**
- Plugin appears in dev plugin list
- Plugin can be enabled without errors
- Basic command (`/chococolor`) is recognized

**User Testing Required:**
- Confirm plugin loads in-game
- Verify command shows in chat
- Check for any error messages in Dalamud log

---

### Phase 1: Core Data Structures
**Goal:** Implement fundamental data types without UI

**Tasks:**
- [ ] Create `ChocoboColor.cs` class
  - RGB properties (byte values 0-255)
  - Distance calculation method
  - Color addition with clamping
  - Equality comparison
- [ ] Create `FruitData.cs` class
  - Enum for fruit types
  - RGB modifier properties
  - Fruit name mapping
- [ ] Create `FruitDatabase.json`
  - All 6 fruit types with modifiers
- [ ] Create `ColorDatabase.json`
  - All 85 chocobo colors with RGB values
- [ ] Write unit tests for color math

**Success Criteria:**
- Color distance calculations are accurate
- Color addition respects RGB clamping
- Fruit data loads correctly

**User Testing Required:**
- No user testing (internal data structures)
- Automated tests pass

---

### Phase 2: Algorithm Implementation
**Goal:** Build the core calculation engine

**Tasks:**
- [ ] Implement `ColorCalculator.cs`
  - Greedy algorithm (no lookahead first)
  - Distance-based fruit selection
  - Termination conditions
- [ ] Implement `PathFinder.cs`
  - Lookahead depth 1 first
  - Path generation and evaluation
  - Incremental lookahead (depth 2, then 3)
- [ ] Create test cases with known solutions
- [ ] Optimize algorithm performance
- [ ] Add progress callbacks for long calculations

**Success Criteria:**
- Algorithm finds optimal path for test cases
- Calculation completes in <1 second for typical cases
- Results match reference implementation

**User Testing Required:**
- No user testing yet (no UI)
- Verify test cases pass

---

### Phase 3: Basic UI Implementation
**Goal:** Create functional user interface

**Tasks:**
- [ ] Implement `MainWindow.cs`
  - Current color RGB sliders
  - Target color RGB sliders
  - Calculate button
  - Results text display
- [ ] Implement `ConfigWindow.cs`
  - Basic settings structure
- [ ] Wire up UI to algorithm
- [ ] Add visual color previews
- [ ] Implement result formatting

**Success Criteria:**
- UI opens via command
- Sliders update color values
- Calculate button triggers algorithm
- Results display correctly

**User Testing Required:**
- Open UI with `/chococolor` command
- Input test colors (current and target)
- Click calculate button
- Verify fruit list appears
- Test with multiple color combinations
- Check for UI responsiveness

---

### Phase 4: Enhanced UI Features
**Goal:** Improve user experience and usability

**Tasks:**
- [ ] Add color name dropdown (all 85 colors)
- [ ] Implement color picker/preview boxes
- [ ] Add "Copy to Clipboard" functionality
- [ ] Display total fruit count
- [ ] Show final color vs target color comparison
- [ ] Add distance metric display
- [ ] Implement Help window
- [ ] Add tooltips and instructions

**Success Criteria:**
- Users can select colors by name
- Visual feedback is clear and helpful
- Copy function works correctly
- Help documentation is accessible

**User Testing Required:**
- Test dropdown selection
- Verify color previews are accurate
- Copy results and paste elsewhere
- Read through help documentation
- Check tooltip clarity

---

### Phase 5: Polish & Optimization
**Goal:** Refine performance and add quality-of-life features

**Tasks:**
- [ ] Optimize algorithm for edge cases
- [ ] Add caching for common calculations
- [ ] Implement result history
- [ ] Add "Reverse Calculate" (find colors reachable from current)
- [ ] Performance profiling and optimization
- [ ] Add error handling and user feedback
- [ ] Implement configuration persistence
- [ ] Add localization support (if needed)

**Success Criteria:**
- Plugin feels responsive
- No crashes or errors during normal use
- Configuration saves correctly
- Additional features work as expected

**User Testing Required:**
- Test performance with various inputs
- Verify configuration saves/loads
- Try edge cases (same color, impossible colors)
- Test history functionality
- Check error messages are helpful

---

### Phase 6: Final Testing & Documentation
**Goal:** Prepare for release

**Tasks:**
- [ ] Comprehensive testing suite
- [ ] Update README.md
- [ ] Create user guide
- [ ] Add screenshots to documentation
- [ ] Code cleanup and commenting
- [ ] Final security review
- [ ] Prepare for plugin repository submission
- [ ] Create release notes

**Success Criteria:**
- All tests pass
- Documentation is complete
- Code is clean and maintainable
- Ready for public release

**User Testing Required:**
- Full end-to-end testing
- Test on fresh install
- Verify documentation accuracy
- Final approval before release

---

## Data Collection Strategy

### Fruit Data
**Source:** FFXIV game mechanics (well-documented)  
**Collection Method:** Manual entry from game wikis and official sources  
**Validation:** Cross-reference multiple sources, test in-game

### Color Data
**Source:** Community databases and datamining  
**Collection Method:** Import from existing color calculators  
**Validation:** Verify RGB values produce correct in-game colors

### Algorithm Validation
**Source:** ffxiv.pf-n.co/chocobo-color calculator  
**Collection Method:** Generate test cases with known solutions  
**Validation:** Compare our results against reference implementation

---

## Why This Approach

### Algorithm Choice Justification
- **Greedy with Lookahead:** Proven optimal for this problem domain
- **Depth 3:** Mathematically guaranteed to find best solution
- **Performance:** Fast enough for real-time calculation
- **User Trust:** Matches community-trusted calculators

### UI Design Rationale
- **Dual Input Methods:** RGB sliders for precision, dropdowns for convenience
- **Visual Previews:** Users think visually about colors
- **Copy Function:** Easy to share results or reference while feeding
- **Help Integration:** Reduces support burden

### Phase Approach Benefits
- **Incremental Testing:** Catch issues early
- **User Feedback:** Adjust based on real usage
- **Manageable Scope:** Each phase is completable
- **Clear Milestones:** Easy to track progress

### Backup Strategy Reasoning
- **Timestamp-based:** Never overwrite backups
- **Pre-edit:** Can always revert changes
- **Flat Structure:** Easy to find and restore files
- **Automated:** No manual backup steps to forget

---

## Risk Mitigation

### Potential Issues & Solutions

**Issue:** Algorithm too slow for complex calculations  
**Solution:** Implement caching, optimize lookahead, add progress indicator

**Issue:** Plugin conflicts with other Dalamud plugins  
**Solution:** Namespace isolation, minimal global state, thorough testing

**Issue:** Game updates break plugin  
**Solution:** Version locking, update monitoring, quick patch releases

**Issue:** User confusion about color mechanics  
**Solution:** Comprehensive help documentation, tooltips, examples

**Issue:** Memory leaks from UI windows  
**Solution:** Proper disposal patterns, event cleanup, testing

---

## Success Metrics

### Phase 0 Success
- Plugin loads without errors
- Command is recognized
- No crashes on enable/disable

### Overall Project Success
- Users can calculate color paths accurately
- Results match reference implementations
- Plugin is stable and performant
- Positive user feedback
- No critical bugs in release

---

## Notes & Considerations

### Technical Constraints
- Must use .NET 8 (Dalamud requirement)
- Cannot access game memory directly (use Dalamud APIs)
- UI must use ImGui (Dalamud's UI framework)
- Plugin size should be minimal (<5MB)

### Future Enhancements (Post-Release)
- Integration with inventory to show available fruits
- Cost calculator (market board prices)
- Multiple chocobo support
- Color palette favorites
- Export/import color schemes
- Mobile companion app (stretch goal)

### Community Engagement
- Open source on GitHub
- Accept community contributions
- Provide support via Discord/GitHub issues
- Regular updates for game patches

---

## Timeline Estimate

**Phase 0:** 1-2 hours (setup and verification)  
**Phase 1:** 2-3 hours (data structures)  
**Phase 2:** 4-6 hours (algorithm implementation)  
**Phase 3:** 3-4 hours (basic UI)  
**Phase 4:** 3-4 hours (enhanced UI)  
**Phase 5:** 2-3 hours (polish)  
**Phase 6:** 2-3 hours (documentation)  

**Total Estimated Time:** 17-25 hours

---

## Conclusion

This project plan provides a comprehensive roadmap for developing the Chocobo Colourized plugin. By following the phased approach, implementing proper backup and testing procedures, and focusing on user experience, we will create a reliable and useful tool for the FFXIV community.

The next step is to verify that the basic plugin structure loads correctly in-game (Phase 0 completion), after which we will proceed with implementing the core functionality.
