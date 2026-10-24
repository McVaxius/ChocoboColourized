# ChocoboColourized LCARS image prompts

## 01-main-regular-compact.png

```text
Create a source-faithful Chocobo Colourized mockup sheet with REGULAR and COMPACT specimens of the same Calculator window. Use equally readable body text in both; reduce spacing and reflow controls in compact without scaling the whole window down. Dark near-black backgrounds and panel interiors; restrained orange/lilac/periwinkle LCARS elbows and segmented rails; blue control accents, dark outlined buttons. No white cards, gradients or glow. Native titlebars retain product actions and visible globe/language, monitor/Compact mode, droplet/colour, Krangle privacy and muted speaker shortcuts. Krangle obscures identifying text, never opens hidden settings. Anonymous fixture data only.

Retain exact tabs Calculator, Timers, Automation; Calculator selected. This is a chocobo plumage calculator, not a race/breeding planner. Show the three actual top groups Current colour, Target colour and Calculate Feeding Path. Each colour group has an editable search box reading “Search colours...”, a colour dropdown, a swatch and read-only RGB values. Use different ordinary game colour names, such as Desert Yellow and Snow White, so the Calculate Feeding Path action is enabled. The product-colour swatches remain domain data, independent of the LCARS theme palette.

Use the native uncalculated state, avoiding invented feeding mathematics: the result area remains empty, and Feeding order displays “Your feeding order will appear here.” The subtitle is “Feed the following fruits to your chocobo in order.” Retain a read-only instruction/result region and a real scroll region. At the bottom show Save Plan & Switch to Automation and Copy Results to Clipboard with save/copy icons; keep them disabled until there is a computed result. Do not show Start feeding on Calculator. The real calculation action can later populate results; this static sheet is no proof of a correct fruit plan or performed feeding.

Preserve native titlebar appearance actions and the existing support link. No new food shop, dungeon queue, race rank or breeding controls. Regular/compact keep identical selected tab, fixture and action gates. Source evidence, not literal screen text: ChocoboColourized/Windows/MainWindow.cs:108 tabs; :163 calculator layout; :524 colour fields; :621 action; :410 and :421 result footer. Dark content panels framed by restrained LCARS rails, not oversized solid pastel cards.
```

## 02-settings-mini-and-auxiliary.png

```text
Create a Chocobo Colourized supporting-surface sheet with a native Automation specimen, a Settings specimen and a small Timers reference. There is no separate Mini in this source inventory; never rename compact main as Mini. Dark near-black backgrounds and panel interiors; restrained orange/lilac/periwinkle LCARS elbows and segmented rails; blue control accents, dark outlined buttons. No white cards, gradients or glow. Native titlebars retain product actions and visible globe/language, monitor/Compact mode, droplet/colour, Krangle privacy and muted speaker shortcuts. Krangle obscures identifying text, never opens hidden settings. Anonymous fixture data only.

Automation preserves tabs Calculator, Timers, Automation with Automation selected. Use an idle fixture with no saved feeding plan, showing the native no-plan guidance instead of invented progress or fruit inventory. Add a separate labelled state crop outside the primary specimen for an existing saved plan: checkbox Check stable condition before feeding, disabled action Start Automated Feeding (Insufficient Fruits), and separate Clear Plan action. Stop Automation belongs only to a running state, and OK/Dismiss belong to completed/error states; do not collect all buttons into one active toolbar. No live automation is implied.

Settings retains exactly Settings and Window appearance tabs. In Settings show the existing Movable Config Window checkbox and support-log export area. In a separate Window appearance crop show ordered radios Auto / Picker / LCARS, original language/compact/opacity preferences and proposed UI tones off, bound to the muted native titlebar speaker. No volume slider, voice package or fabricated stable/breeding configuration. The settings window remains separate from main automation.

The Timers reference retains Colour Change Timers and a native table with Character, Chocobo, Status, Remaining headings. Use one anonymous fabricated character and bird, read-only Changing... and a sample countdown clearly presented as fixture data. Do not include real worlds, player identities or CIDs, and do not add a Pause timer control. Existing saved plan clearing and completion/error messages keep their original confirms and gates rather than new wizards.

Source contract, not rendered UI: ChocoboColourized/Windows/ConfigWindow.cs:52 and :69; MainWindow.cs:822 timers, :915 automation, :999 running stop, :1034 stable check and :1063 fruit gate. Keep dark panel interiors and readable semantic disabled states.
```
