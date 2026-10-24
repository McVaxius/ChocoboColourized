# Chocobo Colourized LCARS concepts

Approved LCARS design concepts. Implementation is selected for the repositories in Z:\myrepos.txt, with the rollout tracked in Z:\!todo\2026-10-22GOATFUCK0002.md. These images are design references, not runtime or test evidence.

- [Main: regular and compact](01-main-regular-compact.png)
- [Settings, Mini and supporting surfaces](02-settings-mini-and-auxiliary.png)

The second sheet depicts selected existing supporting surfaces. Other source windows retain their established layout and actions under the proposed shared appearance; this is not a claim that every scroll position or conditional state has a separate image. Browser companions retain browser controls. Main/Mini windows are distinct from density variants. Generated text and examples are design material, not runtime evidence.

## Source scope

Source commit: `8876034f7529d64170b6d16abd27753d3715092c`. Existing dirty paths were inspected by count (0); none are included or changed by this mockup task.

## Source-verified control contract

- `ChocoboColourized/Windows/MainWindow.cs:108` defines Calculator, Timers, Automation. Calculator is selected for both densities; main density is distinct from its other tabs.
- `MainWindow.cs:524` uses Current colour and Target colour, each with Search colours... and a real colour combo/RGB display. `:621` defines Calculate Feeding Path and disables it for equal colours. `:410` / `:421` define Save Plan & Switch to Automation / Copy Results to Clipboard.
- `MainWindow.cs:822` renders Colour Change Timers with Character / Chocobo / Status / Remaining. `:915` owns Automation; `:1034` is Check stable condition before feeding; `:1063` gates Start Automated Feeding by fruit readiness, while Stop Automation, OK and Dismiss belong to different execution states.
- `ChocoboColourized/Windows/ConfigWindow.cs:52` has Settings and Window appearance tabs; Settings includes support-log export and Movable Config Window. Do not invent breeding, stable-management or shopping preferences.

Selected supporting surfaces are Automation (idle/no saved plan), native Settings/Appearance and Timers. Result expansion, saved-plan clearing and completed/error messages keep existing layouts and conditional actions. No separate Mini exists. Generated plan fruit counts are not treated as validated calculations.

## Existing UI source inventory

- `ChocoboColourized/Windows/ConfigWindow.cs`
- `ChocoboColourized/Windows/MainWindow.cs`

## Shared appearance contract

Auto and Picker retain their order and values; LCARS is the third option. Preserve native IDs, original actions, saved scope and semantic status colours. The proposed LCARS UI tones default off; the speaker and Settings preference are the same value, with no added volume or pack selector. Krangle obfuscates identifying text. Actual adoption waits for the FrenRider pilot.

Exact generation prompts are in [PROMPTS.md](PROMPTS.md).




