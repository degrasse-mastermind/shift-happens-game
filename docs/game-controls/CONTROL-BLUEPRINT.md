# Shift Happens! Control Blueprint

Source inventory for the Unity **Original handoff** drill, inspected November 12, 2026 Eastern. The first comparison uses Guided mode, the same variation and the same LD inputs. The existing Original preset is the reference. See [governing game-controls guide](GOVERNING-GAME-CONTROLS.md) and [Practice Tuning v1](practice-tuning-v1.md).

## The read on the ice

1. Blue LD protects the passing lane to the net-front attacker while RD returns. Watch the puck, the net-front threat and RD's inside shoulder position.
2. The handoff becomes available when RD gets goal-side, closes the lane and faces toward the puck and threat for the required settle time. In Guided, the coverage cue tells LD when to close from inside. If RD loses coverage, LD must recover inside.
3. An early departure can open a slot feed. Once RD has coverage and the carrier is in the containment window, LD can close while staying between the carrier and the middle. Review scores Hold and Contain separately from the resulting shot, save, goal or loose puck.

## Control inventory

Positions below use unmirrored simulation coordinates with X increasing right and Y increasing down. Distances use simulation units, speeds use simulation units per second, and times use simulation seconds. The displayed rink uses a piecewise projection. Review annotations and on-ice edits must continue to use that projection and its inverse.

| System and owner | What governs Original | Status and verification |
|---|---|---|
| Rink and movement · `DefensiveZoneView`, `PracticeShift`, `ZoneSkating` | Mirroring maps X to 1262 − X. LD skates toward the input target at 53, wings use a speed of 47, and the puck travels at 150 on a pass and 300 on a shot. RD's return speed is 48. | RD speed is exposed in v1. The other listed motion rates are in code. Check mirrored placement and input, responsive skating, pass cuts and annotation alignment. |
| Hockey state · `PracticeShift` | Tracks Red carry, in-flight, loose, Blue control, goalie control and goal outcome. A stick interception or denied reception makes the puck loose. LD must stay within 30 for 0.3 seconds to recover it. An unresolved loose puck ends the attempt after 3 seconds. Tie-up uses a reach of 75 and lasts up to 2.5 seconds, ending early if separation exceeds 90. | Existing simulation rules. Check that a deflection produces a loose puck, then compare a recovery with a contested puck. |
| Player intelligence · `PracticeShift`, `ZoneSkating` | RD returns toward the threat-relative inside shoulder. Original uses offset (−47.21, −13.25) and zero return delay. Blue C supports the inside during the handoff and contain. Blue LW covers the puck side and RW the weak side, with lane cuts on release. Red can look for a slot pass when the lane has been open for 0.5 seconds and at least 1.5 seconds have elapsed. Original's warning lasts 1.5 seconds and its fallback pass decision occurs at 14 seconds. | The Review export text records the named Blue responsibilities and revised RD return as approved. Numerical behavior here is inherited from code. Check support positions, point-lane cuts, early departure and the wrong-side return. |
| Scenario direction · `DraftShiftSettings`, `RinkPractice`, `RinkPractice.Tuning` | Original starts LD at (185, 360), RD at (130, 600), Blue C at (225, 465), LW at (330, 245) and RW at (330, 550). Red RW carries from (230, 315), C starts net front at (204, 402), LW supports from (260, 500), LD starts at (390, 635), and RD at (390, 150). The configured puck start is (213, 324). The shift limit is 30 seconds. | The preset also initializes the other participants through the simulation and team behavior code. Verify the opening snapshot against the preset and repeat the same variation after applying and restoring a draft. |
| Coaching and evaluation · `PracticeShift` | Original coverage requires local X ≥ 150, at least 15 goal-side of the threat, within 65 of the threat and within 50 of the puck-to-threat lane. For RD, facing alignment must exceed a dot product of 0.5 and the position and facing must hold for 0.6 seconds. Hold requires coverage for at least 70% of the eligible window and fails if an early departure opens the slot feed. With under 0.5 seconds of opportunity it can be unobserved. Contain requires live RD coverage and an eligible Red carrier, then one continuous second within 110 of the carrier and within 50 of the inside lane, with LD's local X no more than 10 beyond the carrier. Guided can pause during the opening carry from 0.8 seconds. | Original Hold and Contain are the existing scoring baseline. The numbers describe the current implementation. Check successful Hold and Contain, early departure, interrupted contain, insufficient opportunity and equal hockey behavior under Guided and Freer with the same simulation-time inputs. Behind-net and delayed-exit use separate draft reads. |
| Presentation and review · `RinkPractice`, `DefensiveZoneView`, `ReviewMode`, `ReviewModel`, `ReviewStorage` | Shows the setup, RD coverage state and teaching cues. Review captures the run and exports preset and settings information alongside positions and annotations. The simulation supplies coverage and possession state. | Inspect exported preset, variation, settings and position mapping for Original and a draft. Role, destination and lane overlays beyond the current tuning and review displays remain proposals. |

## Practice Tuning v1 contract

| Editable value | Inherited Original | Draft range and application | Acceptance check |
|---|---|---|---|
| RD start · Scenario direction | (130, 600) | X 110–410 and Y 190–610. Move the draft marker on the ice. Applies on **Apply & restart**. | Confirm the marker matches the draft's starting snapshot and that Cancel resumes the same active run. |
| RD return speed · Rink and movement | 48 | 24–72 in steps of 4. Applies on **Apply & restart**. | Hold position and vary speed alone. Check RD's approach and handoff time under the same variation and LD inputs. |

The ranges are draft UI limits. Save and reload one named draft on the device, compare it against Original and restore Original. The save slot can be replaced. A draft must leave the Original reference intact. Start position and speed affect when RD can meet the existing coverage conditions.

## Verification sequence

1. Run **Tools → Shift Happens → Validate Draft Practice**. The source currently lists 18 checks, including mirrored handoff, early departure, wrong-side return, retry, loose-puck recovery, interrupted containment and both behind-net variants.
2. In the Unity Game view, run **Watch First Guided Shift → Hold then close in current setup** with Original and with a draft that uses the Original values. Compare the opening positions, cue and handoff time, puck outcome and both read scores. Repeat after restoring Original. Play **Leave early** to check that Hold follows the defensive read independently of the shot outcome.
3. Change RD start and speed separately, then exercise Cancel, Apply & restart, Save draft, Load draft and Restore Original. Export an Original and a draft review. Verify preset name, variation, effective settings and coordinates against the Game view. Leave the selected comparison ready to run again.
4. Have David review the handoff and a first-time player try Guided without verbal help. Record desktop Editor, standalone and physical phone results separately. This blueprint records source inspection. The runtime checks in this sequence are acceptance work.

Keep facing and role editing, broader movement tuning, additional overlays, scenario authoring and 3D integration outside the first comparison. Extend the same contract to other drills after Original is repeatable and reviewable.
