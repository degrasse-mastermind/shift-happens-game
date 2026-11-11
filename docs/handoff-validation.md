# Draft handoff review

Open `unity/Assets/Scenes/DefensivePractice.unity` in Unity 6000.6.8f1. The `Rink Practice` object's **Draft Coaching** settings expose RD's starting position, return speed and delay, coverage distance, goal-side margin, time to establish coverage, lane reach, containment distance, and mirrored rink option. A new run takes a copy of these settings so the seed stays repeatable.

In the proposed same-side setup, the carrier stays on LD's side. RD skates in from the bottom corner and takes a goal-side position near Red C. The simulation checks stick reach to the pass lane and the direction RD faces, then requires a short period of continuous coverage. LD's second read opens while coverage is established and Red still controls the puck on LD's side. LD can steer freely throughout live play. Red may feed an open slot, cycle to the point when the slot is covered, or cycle early when LD contains from inside.

Draft hold and containment results are assessed before the pass outcome. An early pass can leave containment unobserved. A deflected pass or denied reception produces a loose puck, with a short sustained pickup window before Blue has control. The scripted shot and goalie tracking remain simplified. Other skater markers are in starting positions for this slice, and the display still uses the overhead rink.

## Automated checks

The Unity Editor verification entry point is `ShiftHappens.Editor.PracticeValidation.Run`. The `BuildWindows` entry point runs the same checks and builds the configured practice scene under `artifacts/windows`.

1. Hold through the bottom-corner return, then contain from inside and force an earlier point cycle on the default rink.
2. Repeat on the mirrored rink.
3. Leave early with RD delayed and verify Red can feed the slot before the handoff.
4. Send RD to the wrong side and verify arrival alone gives no coverage cue.
5. Check the Guided pause, manual pause, resume and same-seed retry.
6. Deny a reception with a tie-up and distinguish a loose puck from sustained Blue control.
7. Hold through the entire shift and verify the containment read is missed.

All seven checks passed in Unity. The consolidated browser reference passed its 37 existing tests and production compilation. These checks establish behavior for the tested scenarios. David's coaching review and physical phone testing remain pending.

## Local play and build status

Handoff revision `5f2d946` built successfully for Windows. The executable is at `artifacts/windows/Shift Happens.exe`. Windows Application Control refused to launch it on this PC, so standalone runtime behavior is unverified.

In Play Mode, the isolated copy at `artifacts/handoff-check` opened the `DefensivePractice` scene. The game displayed the draft label, RD at the bottom corner, RD skating toward net front, and the Guided pause at 0.8 seconds after Start. The rest of the interactive playthrough was interrupted by a shared desktop automation conflict. The Unity simulation checks exercised resume, handoff, containment, repeatability and outcomes through the end of the shift.

Separate review-mode edits appeared in the shared workspace during verification. Those edits are outside the handoff commit. The isolated copy uses the handoff revision for review while the main `unity` folder remains the active source project.
