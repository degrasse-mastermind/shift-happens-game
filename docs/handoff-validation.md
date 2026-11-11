# Handoff validation

David marked the player-facing coaching cues **final** for both Unity presets on November 11, 2026. This covers holding net front while RD returns or arrives on the wrong side, and containing from inside once RD has coverage.

On November 11, 2026, David also approved **Hold and Contain scoring** in both **Original handoff** and **Marked upper corner**. This approval includes the marked preset's passing-lane Hold rule and the one-continuous-second Contain rule in both presets. The scored conditions and thresholds carry forward from the playtests below. Earlier entries use the labels displayed during those runs.

### Blue RW weak-side approval

On November 11, 2026, after watching the live marked late-C replay, David approved **Blue RW's weak-side positioning and lane cut** in both **Original handoff** and **Marked upper corner**. The approval covers RW's tighter high-slot tuck and the immediate cut into the weak-side passing lane on release while staying on the net side of Red LD.

### Blue LW puck-side approval

On November 11, 2026, after the on-screen puck-side replays, David approved **Blue LW's puck-side positioning and lane cut** in both **Original handoff** and **Marked upper corner**. The approval covers shading Red RD's point lane from the inside shoulder and cutting into the passing lane as Red releases to that point.

### Blue C support approval

On November 11, 2026, after reviewing both on-screen replays, David approved **Blue C's support positioning and inside-shoulder assignment** in both **Original handoff** and **Marked upper corner**. C supports from inside the puck-side circle as LD moves out to contain and stays on the net side of Red's support forward.

### Blue RD return and net-front approval

On November 11, 2026, David approved **Blue RD's revised return and net-front positioning** in both **Original handoff** and **Marked upper corner**. RD returns to the inside shoulder between the net-front threat and Blue's net, taking Red C in Original and Red LW in Marked. This approval applies to the revised target from David's saved review. All remaining unapproved positioning stays draft.

Open `unity/Assets/Scenes/DefensivePractice.unity` in Unity 6000.6.8f1. The `Rink Practice` object's **Draft Coaching** settings expose RD's starting position, return speed and delay, coverage distance, goal-side margin, time to establish coverage, lane reach, containment distance, and mirrored rink option. A new run takes a copy of these settings so the seed stays repeatable.

In the proposed same-side setup, the carrier stays on LD's side. RD skates in from the bottom corner and takes a goal-side position near Red C. The simulation checks stick reach to the pass lane and the direction RD faces, then requires a short period of continuous coverage. LD's second read opens while coverage is established and Red still controls the puck on LD's side. LD can steer freely throughout live play. Red may feed an open slot, cycle to the point when the slot is covered, or cycle early when LD contains from inside.

Draft hold and containment results are assessed before the pass outcome. An early pass can leave containment unobserved. A deflected pass or denied reception produces a loose puck, with a short sustained pickup window before Blue has control. The scripted shot and goalie tracking remain simplified. Other skater markers are in starting positions for this slice, and the display still uses the overhead rink.

## Automated checks

The Unity Editor verification entry point is `ShiftHappens.Editor.PracticeValidation.Run`. The `BuildWindows` entry point runs the same checks and builds the configured practice scene under `artifacts/windows`.

1. Hold through the bottom-corner return, then contain from inside and force an earlier point cycle on the default rink.
2. Repeat on the mirrored rink.
3. Leave early with RD delayed and verify Red can feed the slot before the handoff.
4. Send RD to the wrong side on the mirrored rink, hold until RD arrives, then leave. Verify the slot feed, missed hold, and unobserved containment.
5. Check the Guided pause, manual pause, resume and same-seed retry.
6. Deny a reception with a tie-up and distinguish a loose puck from sustained Blue control.
7. Hold through the entire shift and verify the containment read is missed.

All seven checks passed in Unity. The consolidated browser reference passed its 37 existing tests and production compilation. These checks establish behavior for the tested scenarios. David's coaching review and physical phone testing remain pending.

## Local play and build status

Handoff revision `5f2d946` built successfully for Windows. The executable is at `artifacts/windows/Shift Happens.exe`. Windows Application Control refused to launch it on this PC, so standalone runtime behavior is unverified.

The interactive runs below used `DefensivePractice` in the isolated Unity Editor copy at `artifacts/handoff-check`. The scene displayed **Draft Coaching for David** throughout. Times are the on-screen shift clock.

### Default rink, Guided, seed 1

| LD's choice | Lane and puck outcome | Draft reads |
| --- | --- | --- |
| Leave net front before RD is set | The slot lane opened. Red fed Red C at net front, and Blue's goalie saved and covered the shot at about 4.5 seconds. | Hold missed. Contain unobserved. |
| Wait for RD, then contain from inside | RD took net-front coverage before LD moved out. The slot stayed covered, and Red cycled to the point. Blue's goalie saved and covered the point shot at about 13.9 seconds. | Hold met. Contain met. |

### Mirrored rink, Guided, seed 0

Blue defended the right net. Both runs used **Point look · 0** and the same draft settings. RD's return delay was zero.

| LD's choice | Lane and puck outcome | Draft reads |
| --- | --- | --- |
| Leave net front before RD is set | The slot lane opened. Red chose the feed to Red C even with the point-look seed. The pass was in flight by the 3.5-second observation. Blue's goalie saved the slot shot and covered the puck at about 4.4 seconds. | Hold missed. Contain unobserved. |
| Wait for RD, then contain from inside | RD's coverage cue was visible by the 5.0-second observation. LD closed from inside while RD kept the slot covered. Red cycled to the point and shot. Blue's goalie saved and covered the puck at about 13.9 seconds. | Hold met. Contain met. |

The lane choice differed in both comparisons. Leaving early gave Red a slot shot. Waiting for RD and then containing produced a point shot. Blue's goalie covered all four shots. These are observations from the draft scenario in Unity Play Mode.

### RD arrives on the wrong side, mirrored rink, Guided, seed 0

Blue defended the right net. In the isolated Editor session, the draft Partner Coverage Offset was set to X = 25 and Y = 20, putting RD on the side of Red C farther from Blue's goal. The return delay stayed at zero.

- At the 5.9-second observation, RD had reached the wrong side of Red C while LD held net front. LD received no contain cue. The draft banner still said RD was returning and told LD to protect net front.
- After LD left, the slot opened and Red attempted the feed to Red C. The pass was in flight at the 9.5-second observation. It became a loose puck, and Blue established control at about 9.9 seconds. The play ended without a shot. The live observations did not identify which Blue skater broke up the pass.
- Before the cue and scoring revision, the final draft reads were **Hold met** and **Contain unobserved**. The earlier coverage-time calculation allowed the hold read to pass despite the late slot opening.

### Wrong-side replay after the draft cue and hold-read revision

The replay used the same mirrored Guided setup, seed 0, offset X = 25 and Y = 20, and zero return delay. At the 6.2-second observation the draft banner read **RD is on the wrong side of Red C. Hold net front.** No contain cue appeared.

LD left after that cue. The slot opened and triggered Red's look to Red C. The play ended with Blue controlling the loose puck at about 9.5 seconds and no shot. A repeat also ended with Blue controlling the loose puck. The final draft read in both runs showed **Hold missed (left net front and opened slot)** and **Contain unobserved**.

The hold read now records the slot opening when it lasts long enough to trigger Red's slot look before the first handoff. Earlier coverage time and later puck recovery do not clear the miss. The cue and criteria remain draft for David's review.

All seven Unity Editor checks passed after the revision. The revised wrong-side check verified that Red released the pass toward Red C and that the draft hold read was missed with containment unobserved.

### Starting-position review

David's Starting setup export from November 10 contains rink marks for the default left-defending Guided setup at seed 1. The note field is blank. The last mark for each player or puck is its proposed starting position.

- Blue LD moves closer to the goal line and toward the upper side. Blue RD starts behind the goal line on the lower side. Blue C and RW move up the rink diagram, with a small adjustment to LW.
- Red C and the puck move to the upper corner area. Red RW moves into the upper circle. Red LW moves closer to the net, and both Red defensemen shift along the blue-line area.
- The diagram puts the puck away from Red RW, who owns it in the current scenario, and moves Red C away from the current net-front target. The proposed setup leaves the initial puck carrier and slot threat to be confirmed. The wrong-side comparison uses the original mirrored seed 0 setup so LD's decision is the change being tested.

### Wrong-side replay with LD holding net front

In Unity Play Mode, Blue defended the right net in Guided mode at seed 0. The draft offset remained X = 25 and Y = 20 with zero return delay. At 5.8 seconds RD had arrived on the wrong side of Red C. The banner told LD to hold net front. No contain cue appeared.

LD stayed at the original net-front position and kept the slot covered while Red carried. Red cycled the puck to the point by the 14.1-second observation. Blue's goalie saved and covered the point shot at about 16.9 seconds. The final draft reads were **Hold met** and **Contain unobserved**.

In the earlier replay with the same settings, leaving opened a feed to Red C and scored Hold missed. With LD holding, Red used the point and the hold read scored Met. The starting-position proposals and coaching criteria remain draft.

### Historical playtest of the separate marked scene

This playtest used the former separate marked scene. The **Marked upper corner** preset in `unity/Assets/Scenes/DefensivePractice.unity` now loads the same starting positions from the November 10 review. Blue defends the left net. Red C starts with the puck at the upper corner and Red LW threatens net front. The historical run used Red RD as its point outlet. The puck uses the marked starting offset from Red C and moves to the normal carry offset as C skates out of the corner.

LD's marked start is goal-side and near the passing lane, farther from LW than the original net-front station. In this draft, LD can cover the feed from that position. RD still has to get goal-side and within coverage reach of LW. The marked scene uses a draft RD return speed of 36 and a containment distance of 80 in rink coordinates. These values give LD a separate approach to the carrier after the handoff.

| Guided seed 0, Unity Play Mode | Passing lane and puck outcome | Draft reads |
| --- | --- | --- |
| LD leaves toward the outside before RD takes coverage | The lane opened and Red C released the pass to LW. The pass was visible at 4.1 seconds. RD recovered in time to deny the reception, and the puck remained loose and contested at the end. Red got no shot in this run. | Hold missed, slot opened before coverage. Contain unobserved. |
| LD holds, waits for RD, then moves inside the carrier | RD's coverage cue was visible at 4.5 seconds. LD moved out from inside while RD covered LW. Red C cycled to Red RD at the point. Blue's goalie saved and covered the point shot at 10.1 seconds. | Hold met. Contain met. |

All eight Unity Editor checks passed in the isolated copy, including the seven prior checks and the marked setup comparison. The automated early exit also ended with a contested loose puck. Both the marked geometry and the coaching criteria remain draft for David's review.

Separate review-mode edits appeared in the shared workspace during verification. Those edits are outside the handoff commit. The isolated copy uses the handoff revision for review while the main `unity` folder remains the active source project.

### One practice scene with responsive skating — draft for David

Open `unity/Assets/Scenes/DefensivePractice.unity`. Select **Setup** above the rink to switch between **Original handoff** and **Marked upper corner**. Switching returns the shift to its starting positions and keeps the seed and Guided or Freer mode. To author another starting setup, add a named entry to **Presets** on the **Rink Practice** object in Unity. The entry holds the starting positions, Red forward roles, orientation, carry route choice and draft coaching settings.

Red's net-front forward changes the passing angle, the support forward moves away from pressure, and both points slide to show an outlet. Blue's center supports inside, the puck-side wing shades the point lane, and the weak-side wing folds toward the high slot. RD tracks the live net-front threat. Red compares point lanes at release, and moving Blue skaters can break up a pass. The seed varies the routes while keeping a retry repeatable. The initial puck carrier still follows a practice route.

| Unity Editor simulation | Lane and puck | Draft reads |
| --- | --- | --- |
| Original, seed 0, LD leaves early with RD's return delayed for the comparison | Red fed C through the open slot. C shot and Blue's goalie covered at 4.26 s. | Hold missed. Contain unobserved. |
| Original, seed 1, LD waits then contains | RD established coverage at 4.06 s. Red tried RD at the point. Blue broke up the pass and the puck stayed contested at 9.54 s. The mirrored run matched. | Hold met. Contain met. |
| Marked, seed 0, LD leaves early | The slot opened before the handoff. Red fed LW, but the play ended with a contested loose puck at 8.52 s. | Hold missed. Contain unobserved. |
| Marked, seed 0, LD waits then contains | RD took LW and kept the slot covered. Red used LD at the point. Blue's goalie covered the point shot at 8.74 s. | Hold met. Contain met. |

In the main project's **Unity Play Mode**, the Setup control switched to the marked start in the same scene, and Blue and Red skaters visibly changed positions as play advanced. With mouse input, the original seed 1 hold-and-contain run ended with a goalie cover at 9.2 s, and the marked seed 0 hold-and-contain run ended with a cover at 9.7 s. Both reads were Met in both runs. A separate live marked early exit opened the slot and scored Hold Missed and Contain Unobserved. That run ended with a goalie cover at 7.4 s. The live steering differed from the automated steering above.

All 10 Editor checks passed via **Tools → Shift Happens → Validate Draft Practice**. They include the wrong-side RD case, puck recovery, pause and retry, mirrored movement, and how changing LD's route or the seed changes off-puck movement in both presets.

The setups, Box + 1 behavior and coaching criteria remain draft. Standalone and phone play are unverified.

### Continuous Contain scoring — draft for David

Contain now requires one uninterrupted second in a valid contain position while RD covers and Red still carries. Breaking any of those conditions before the second is earned resets the timer. A completed second counts for the shift. The marked preset still awards Hold from the passing lane to Red LW using the same geometry and timing.

| Preset | Sustained contain in Unity Play Mode | Interrupted contain in the Unity Editor check |
| --- | --- | --- |
| Original handoff | Seed 1. Hold Met, Contain Met. Blue's goalie covered at 9.3 s. | Seed 0. Two attempts totaled 1.32 s, with 0.66 s the longest. Hold Met, Contain Missed. Red held the puck until the regular pass decision, then used RD at the point. The puck ended loose and contested. |
| Marked upper corner | Seed 0. Hold Met, Contain Met. Blue's goalie covered at 9.7 s. | Seed 0. Two attempts totaled 1.36 s, with 0.68 s the longest. Hold Met, Contain Missed. Red held the puck until the regular pass decision, then used LD at the point. Blue's goalie covered. |

All 12 Editor checks passed, including the existing early-exit and wrong-side RD checks. The end-of-shift screen displayed **Draft Score** in both live runs.

### Box + 1 positioning review

Review the three supporting skaters around the approved LD–RD handoff first. The current Unity targets and the proposed checks apply to both presets. The Original puck carrier is Red RW. In Marked, Red C starts in the upper corner and Red LW threatens net front.

| Role | Current Unity positioning | Proposed review check |
| --- | --- | --- |
| Blue C | Shades toward the puck from inside and moves away from nearby LD or RD. The same puck-following target applies through the handoff. | Freeze just before RD takes net front and again as LD steps out. Check whether C is close enough to support LD while keeping inside support available and giving RD room at net front. |
| Puck-side wing | Shades the line between the puck and the Red point on that side. | At both freezes, check whether the wing can take away the point pass while staying inside the Red point player. |
| Weak-side wing | Folds toward a high-slot target on its side. The two wing targets blend as the puck moves across the zone. | At both freezes, check whether the weak-side wing takes away the high-slot seam and can recover to its point if the puck changes sides. |

The first decision is Blue C's assignment once RD takes net front and LD closes on the carrier. The proposed starting point is for C to support from inside the puck-side circle while staying available to protect the middle. The browser coaching reference calls for C to hold the slot when the puck goes behind the net. Neither current Unity carry route crosses behind the net, so that rotation needs its own review setup.

At the 5.0 s pause in **Marked upper corner**, seed 0, RD had coverage and LD had the contain cue. Blue C was near the lower edge of the puck-side circle, Blue LW was between the carrier and the upper point, and Blue RW was on the lower side of the high slot. This is a starting frame for reviewing the three support assignments. All 12 Editor checks passed after the scoring approval labels were applied.

### Blue C supports from inside the puck-side circle — draft positioning

Once RD has net front and LD closes from inside, Blue C skates inside the puck-side circle on the half nearest the slot. C also gives the defensemen room. The wings keep their existing point-lane and weak-side assignments. The scored Hold and Contain conditions are the approved versions.

The table uses left-defending rink coordinates. The before frame is the last sampled step before RD established coverage. The after frame is 0.7 s into LD's contain, with RD still covering and Red still carrying. The local diagram is at `artifacts/validation/box-plus-one-center-support.png`.

| Preset and frame | Blue C | Blue LW, puck side | Blue RW, weak side |
| --- | --- | --- | --- |
| Original, seed 1, before at 4.04 s | (237.9, 338.4) | (331.4, 196.1) | (306.0, 469.1) |
| Original, seed 1, after at 5.04 s | (239.6, 307.1) | (327.8, 190.3) | (306.0, 469.0) |
| Marked, seed 0, before at 3.92 s | (220.0, 320.6) | (304.5, 168.5) | (306.0, 469.0) |
| Marked, seed 0, after at 5.16 s | (236.8, 291.1) | (307.6, 167.1) | (306.0, 469.0) |

Both automated handoffs put Blue C inside the upper circle during containment and produced Hold Met and Contain Met. All 14 Editor checks passed. In the live Unity Play Mode runs, both scores also read Met. The puck stayed loose and contested at the end of each live run. The automated Original run also ended contested, while the automated Marked run went to Red RD at the upper point and ended with Blue's goalie covering. Positioning remains draft for David.

### Weak-side wing and inside shoulder — draft positioning

The weak-side wing now tucks 24 rink units closer to the middle. Blue C's circle support carries forward. Review Mode reports a draft inside-shoulder check for each Blue skater. The check uses the direction from the assigned Red opponent toward Blue's net. LD checks the slot threat while holding and the carrier once containment is available. RD checks the slot threat, C checks Red's support forward, and the wings check their same-side points. The goalie protects the crease. The approved Hold and Contain scoring rules are unchanged.

| Preset and frame | Blue LW, puck side | Blue RW, weak side |
| --- | --- | --- |
| Original, seed 1, before at 4.04 s | (331.4, 196.1) | (306.1, 445.2) |
| Original, seed 1, after at 5.04 s | (327.8, 190.3) | (306.0, 445.0) |
| Marked, seed 0, before at 3.92 s | (304.5, 168.5) | (306.0, 445.0) |
| Marked, seed 0, after at 5.16 s | (307.6, 167.1) | (306.0, 445.0) |

The before frame is the last sampled step before RD establishes coverage. The after frame is 0.7 s into LD's contain. Both wings and all five Blue skaters were on the net side of their assigned opponents at these frames. The mirrored wrong-side check flagged RD outside. All 14 Editor checks passed. The comparison diagram is at `artifacts/validation/box-plus-one-wing-positioning.png`.

Both live Unity Play Mode runs scored Hold Met and Contain Met. The goalie covered in Original at 9.9 s. The puck stayed loose and contested in Marked at 10.6 s. A separate Marked run was frozen in Review Mode at 5.39 s with all five skaters shown inside.

### Weak-side point cycle — draft positioning playtest

In Unity, **Tools → Shift Happens → Playtest Weak-Side Cycle** runs the current simulation with repeatable LD inputs. It tracks passes to Red LD on the lower side after a successful contain while the puck starts on the upper side. Red chooses the outlet normally. It records RW's net-side position throughout the pass and the actual breakup or shot. A positive inside-shoulder distance means RW stayed on the net side of Red LD.

| Setup | Weak-side cycles observed | Blue RW's recovery |
| --- | --- | --- |
| Original handoff, 64 standard trials | Red chose the weak-side point 23 times. RW broke up all 23 passes and stayed inside throughout. | Seed 4. Pass released at 5.44 s. RW skated from (306.0, 445.0) to (322.7, 505.0) and broke up the pass after 2.06 s. The puck stayed loose. |
| Marked upper corner, 64 standard trials | Red chose the weak-side point 58 times. Blue C cut off all 58 passes before they reached RW. | Seed 0. C broke up the pass after 0.74 s. RW stayed tucked at (306.0, 445.0) and inside. These runs left RW's ability to stop a completed cycle unobserved. |
| Marked upper corner, 768 trials with varied LD routes and delays | Red chose the weak-side point 210 times. C broke up 131 passes and LD broke up 79. | RW stayed inside while teammates broke up the passes upstream. |
| Marked upper corner, 32 stress trials with only Blue C's start moved to (380, 620) in a test copy | C broke up 10 passes. Red LD received and shot in 22. RW stayed inside throughout all 32. | Seed 0. RW moved from (306.0, 445.0) to (342.6, 458.2). Red LD received after 2.32 s and shot with RW still 56.4 rink units away. The closest RW got to the moving puck was 40.8 units. Blue's goalie covered. |

RW reached the lane in time in Original. The marked stress test exposed a recovery gap while RW was still on the inside shoulder. The current wing rotation follows the puck's side as the pass travels. A follow-up can compare that timing with a wing that starts to recover as soon as Red releases to the weak-side point. Positioning and the live preset files remain as they were for this playtest. The diagram is at `artifacts/validation/weak-side-cycle-recovery.png`.

### RW recovers on weak-side pass release — draft positioning

RW now takes the point-lane assignment on the first simulation step after Red releases to the lower point. The tighter high-slot starting target, recovery lane geometry, and net-side positioning rule carry forward. The same Unity Editor trials were replayed with the same seeds, LD inputs, and late-C test setup.

| Setup | Replay result |
| --- | --- |
| Original handoff | RW broke up all 23 weak-side cycles. At seed 4, RW began moving toward the point on the first step and broke up the pass after 2.22 s. The puck stayed loose. In the previous seed-4 run the breakup was 2.06 s into flight, farther up the passing lane. |
| Marked upper corner | RW started on release. C still broke up all 58 standard cycles. In seed 0, RW moved from (306.0, 445.0) to (325.5, 459.2) before C broke up the pass at 0.74 s. The varied-route runs again produced 131 C breakups and 79 LD breakups. |
| Marked late-C stress test | C broke up 10 passes and Red LD received and shot in 22 of 32, matching the earlier split. At seed 0, RW was 34.0 units from Red LD at reception, down from 56.4. Its closest approach to the moving puck improved from 40.8 to 27.1 units, outside the 16-unit wing breakup range. Blue's goalie covered. |

Across the observed cycles, RW moved toward the receiver on the first measured 0.02 s step after release and stayed on the inside shoulder for the full pass. The smallest net-side margin was 17.4 units in the marked late-C trials. All 14 existing Editor checks passed. The seed-0 stress comparison is at `artifacts/validation/weak-side-release-recovery.png`. Positioning remains draft for David's review.

### RW cuts into the weak-side passing lane — draft positioning

At release to the lower point, RW looks along the live puck path for a spot it can reach before the puck arrives. The target must be on the net side of Red LD. RW starts skating on the next simulation step and uses the regular point-lane recovery if no reachable cut is available. The tighter high-slot position and approved scoring carry forward.

| Unity Editor replay | Result |
| --- | --- |
| Original handoff | RW broke up all 23 weak-side passes. At seed 4, the breakup came 1.42 s after release, compared with 2.22 s when RW skated toward the point. The puck stayed loose. |
| Marked upper corner | C still broke up all 58 standard weak-side passes before the puck reached RW. The varied approaches again produced 131 C breakups and 79 LD breakups. |
| Marked late-C stress test | RW broke up 22 of 32 passes and C broke up the other 10. The previous replay produced 22 Red point shots. At seed 0, RW moved from (306.0, 445.0) to (330.9, 401.8) and broke up the pass 1.62 s after release. The puck stayed loose and Red LD got no shot. |

RW started moving on the first measured 0.02 s step and stayed on the net side of Red LD throughout every observed weak-side pass. The smallest net-side margin in the late-C trials was 67.7 rink units. All 15 Editor checks passed, including the new seed-0 late-C check for an immediate cut, inside positioning, and a breakup. The same-seed comparison is at `artifacts/validation/weak-side-lane-cut.png`.

To watch the exact seed-0 late-C run in Unity, open **Defensive Practice**, enter Play Mode, and select **Tools → Shift Happens → Watch Weak-Side Lane Cut**. Teal identifies RW and gold traces the remaining passing lane. The live run freezes at RW's breakup at 7.04 s. Select **Replay** to watch again. This Editor replay now marks RW's approved weak-side positioning as final.

### Blue LW puck-side point coverage — draft positioning playtest

**Tools → Shift Happens → Playtest Puck-Side Point** replays 64 seeds per preset with the same LD hold-and-contain input. Red chooses the outlet normally. The comparison counts passes to Red RD at the upper point and samples LW's position relative to the line from Red RD to Blue's net throughout the pass.

| Preset and example | LW just before RD took coverage | LW 0.7 s into LD's contain | Pass outcome |
| --- | --- | --- | --- |
| Original handoff, seed 1 | (331.4, 196.1) at 4.04 s | (327.8, 190.3) at 5.04 s | LW broke up the pass at (357.0, 170.4), 1.22 s after release. The puck stayed loose. |
| Marked upper corner, seed 2 | (301.1, 167.1) at 3.98 s | (312.6, 175.7) at 5.20 s | LW broke up the pass at (357.5, 180.3), 1.30 s after release. The puck stayed loose. |

Original produced 41 puck-side passes. LW broke up 21 and Red RD received and shot on 20. In seed 0, LW stayed inside but came within only 17.4 rink units of the puck, outside the 16-unit breakup range. Blue's goalie covered that shot. Marked produced six puck-side passes, and LW broke up all six. LW stayed on the inside shoulder throughout every observed pass in both presets. The minimum net-side margin was 37.0 rink units in Original and 36.9 in Marked.

All 15 Editor checks passed. The comparison diagram is at `artifacts/validation/blue-lw-puck-side-point.png`. LW positioning remains draft and this playtest left the movement and scoring rules as they were.

### Blue LW cuts into the puck-side passing lane — draft positioning

LW now starts cutting on release to the upper point, using the same reachable, net-side interception rule as RW. The same 64 seeds per preset, LD input and natural Red outlet choice were replayed.

| Preset | Previous puck-side passes | With the lane cut |
| --- | --- | --- |
| Original handoff | 21 LW breakups and 20 Red RD shots out of 41 passes. | 41 LW breakups and zero point shots. In seed 0, LW broke up the pass 0.84 s after release and the puck stayed loose. |
| Marked upper corner | Six LW breakups out of six passes. | Six LW breakups out of six. In seed 2, LW broke up the pass 0.92 s after release, compared with 1.30 s previously. |

LW stayed inside for every measured puck-side pass. The minimum net-side margin was 68.3 rink units in Original and 71.4 in Marked. All 16 Editor checks passed, and the RW weak-side replays matched their previous outcomes. The Original seed-0 comparison is at `artifacts/validation/blue-lw-puck-side-lane-cut.png`.

Both examples played live in Unity and froze at the breakup, at 6.22 s for Original and 6.38 s for Marked. While in Play Mode, use **Tools → Shift Happens → Watch Puck-Side Cut** and select the preset. **Replay** repeats the displayed run. Teal marks LW and gold shows the passing lane. Future gameplay playtests use the on-screen replay workflow recorded in the project instructions.

### Unity playtest layout and Blue C support review

The local **Shift Happens Playtest** Editor layout uses a fitted QHD (2560 × 1440, 16:9) Game view, **Play Maximized**, and a docked Console in Edit Mode. The project reloads the scene and scripting state when entering Play Mode for a clean test start.

| Preset | C just before RD took coverage | C 0.7 s into LD's contain | Smallest net-side margin from the before frame to pass release |
| --- | --- | --- | --- |
| Original handoff, seed 1 | (237.9, 338.4) at 4.04 s, near the slot-side edge of the puck-side circle. | (239.6, 307.1) at 5.04 s, inside the circle. | 69.6 rink units on the net side of Red LW. |
| Marked upper corner, seed 0 | (220.0, 320.6) at 3.92 s, near the slot-side edge of the puck-side circle. | (236.8, 291.1) at 5.16 s, farther inside the circle. | 59.7 rink units on the net side of Red RW. |

C stayed inside Red's support forward while LD moved out and contained in both sampled shifts. The before-and-after diagram is at `artifacts/validation/blue-c-inside-support.png`. All 16 Editor checks passed. These playtests preceded David's approval of C's assignment above.

Both examples were played live on the Unity Game screen and frozen at the after frame. In Play Mode, use **Tools → Shift Happens → Watch Center Support** to choose either preset. The replay highlights Blue C in teal and **Replay** repeats the current run.

### Blue RD return and net-front coverage — initial draft playtests

Both live Unity replays start with RD on the net side of the assigned attacker by the inside-shoulder check. RD still has to return into reach and satisfy the coverage criteria before LD can contain.

| Preset | Slot threat | Inside shoulder and within 65 rink units | Coverage established |
| --- | --- | --- | --- |
| Original handoff, seed 1 | Red C | 3.14 s | 4.06 s at (177.35, 440.57) |
| Marked upper corner, seed 0 | Red LW | 2.88 s | 3.94 s at (157.86, 464.89) |

Established coverage requires the full position, lane and facing checks to hold for 0.6 continuous seconds. Both examples played live and froze at coverage. The revised, approved positioning was tested in the runs below.

### Playtests of the revised RD positioning

In the Original handoff review at 4.48 s, David moved RD from (177.06, 440.32) to (169.85, 402.07), between Red C and the net. The approved return target uses that offset relative to the moving threat in both presets. The Marked preset uses Red LW as the threat and respects the existing net-front coverage boundary.

| Live replay | Inside and within reach | Coverage established |
| --- | --- | --- |
| Original handoff, seed 1 | 3.38 s | 4.54 s at (169.40, 402.58) |
| Marked upper corner, seed 0 | 3.12 s | 4.54 s at (150.00, 423.16) |

Both runs froze at the revised handoff in Unity. All 16 Editor checks passed. These runs preceded David's approval of RD's revised positioning above.

### Blue-only nameplates in the crowded handoff

In `DefensivePractice` Play Mode, choose **Tools → Shift Happens → Watch Crowded Handoff**. This replay uses seed 1 and starts RD below the net and Blue C near the slot to bring LD, RD, Blue C, and Red C together. LD holds the starting position. The Guided teaching pause resumes automatically, then the replay freezes when LD can contain. Select **Replay** to repeat from 0.00 s.

In the desktop Unity Game view, all six Blue labels (LD, RD, C, LW, RW, G) remained readable during the teaching pause and at the handoff. LD kept the active ring, RD stayed blue, and Red skaters and goalie had no on-ice position labels. The handoff cue appeared below the rink.

The teaching pause occurred at 0.82 s while the slot was covered and containment was unavailable. RD established coverage at 1.98 s, with the slot still covered. Replay reproduced both timestamps and all four logged skater positions. The 16 Editor practice checks passed. A standalone build and physical-phone readability were not tested.

### Youth equipment and defensive-zone view

The new gameplay and Review Mode views crop the supplied rink to Blue's defending end, with roughly 3.5 ft of ice beyond the blue line for point play. The view calibrates the schematic to a 75 × 85 ft end zone using the [NHL rink diagram](https://media.d3.nhle.com/image/private/t_document/prd/slwjuaqwmuvj5bkplixo.pdf). The original diagram's markings remain schematic. Simulation and saved annotation coordinates retain their existing units.

The youth sprites use roughly 1.5 m standing stature as a reference from the [CDC growth charts](https://www.cdc.gov/growthcharts/cdc-charts.htm), with an elevated-view art box of 4.5 ft including equipment. The puck uses a shaded black disc with the [USA Hockey 3-inch diameter and 1-inch thickness ratio](https://cdn3.sportngin.com/attachments/document/a554-1700645/2025-29_USAH_Playing_Rules.pdf) and is drawn at four times physical size for tracking.

In the desktop Editor, both presets rendered the defending end. Mouse steering and a Review Mode drag worked in Marked upper corner. **Watch Crowded Handoff** and **Watch Mirrored Crowded Handoff** showed the correct end, retained readable Blue labels, and paused at 0.82 s and 1.98 s on each side. The standard replay reproduced both checkpoints on restart. All 16 practice checks passed. Physical phones and standalone builds remain unverified.

### Dimensioned rink, larger players and phone test

The new practice rink uses a single scale in both directions. Each end-zone circle is 30 ft across. The dots are 20 ft ahead of the goal line and 22 ft either side of the center axis, 44 ft apart. A red six-foot-wide goal with white mesh sits behind the goal line, with the youth crease in front. Source dimensions are in [USA Hockey Rules 101–105](https://cdn3.sportngin.com/attachments/document/a554-1700645/2025-29_USAH_Playing_Rules.pdf). The [rink drawing script](../unity/Tools/DrawPracticeZone.ps1) creates the local Unity texture. The simulation still uses its original coordinates, mapped through the end boards, goal line, face-off dots and blue line.

Player art was increased by 36% linearly, with youth body proportions and a 6.1 ft on-screen box that includes the stick and equipment. That size is a visibility choice. The puck remains a black disc at five times physical size for tracking. Blue-only labels are larger. The portrait layout moves controls below the ice so the rink fills the phone width.

Both face-off circles measured 483 pixels across horizontally and vertically in the texture, including the line. Both presets reached their teaching pause in the Unity Editor's 390 × 844 Game view, with the net, puck and all six Blue labels visible. All 16 Unity practice checks passed. Physical-device findings are separate from these Editor observations.

The phone preview uses the Unity Web build on the local Wi-Fi. David approved retaining the **Shift Happens local phone preview** Windows firewall rule, scoped to TCP 4173 on Wi-Fi and local-subnet clients. The PC's Wi-Fi address changed while preparing the build. Safari on David's iPhone reached the replacement address and fetched the completed build.

The Unity Web build completed successfully. In Chrome's 390 × 844 touch emulation, both presets accepted Start and Resume read, and the same simulated drag moved LD to the intended spot in each preset. All six Blue labels were readable in the captured frames. The black puck stood out in open ice during the Marked point pass and had less separation against dark gear during a carry. This emulation did not measure physical-phone response time.

David reported that the iPhone Safari preview “worked pretty much as intended” overall and that he lost sight of the puck when a Red player carried it. He did not identify a specific preset for that observation.

The follow-up Unity Web build adds a small, high-contrast outline around the puck during Red possession. All 16 practice checks passed. In Chrome's 390 × 844 touch emulation, the outline appeared during Red's carry in both Original handoff and Marked upper corner. It was absent after the pass release in the captured Marked run. The local preview was served at `/puck-outline/index.html`.

On the physical iPhone retest, David reported that the puck was more visible during Red possession and that the outline disappeared when Red released a pass. The preset and how easy the puck was to track in flight were not reported.

David asked to keep the outline visible through the pass. The next Unity Web preview at `/puck-pass/index.html` keeps the outline on during Red possession and throughout the pass. It turns off when the pass ends, including on a breakup. All 16 practice checks passed. Chrome's 390 × 844 touch emulation showed the outline after release in both presets. The Marked upper corner puck-side cut replay in Unity showed the outline on every sampled pass frame and off as soon as Blue LW broke up the pass at 6.96 s. In Play Mode, choose **Tools → Shift Happens → Watch Puck-Side Cut → Marked upper corner** and select **Replay** to repeat the same setup and scripted inputs.

David confirmed on his physical iPhone that the outline stayed on through the pass and he could follow the puck after release. The preset and what happened to the outline after the pass ended were not reported.

### Blue LD hold and contain — approved positioning

On November 11, 2026, after watching both Unity replays, David approved **Blue LD's hold position and route to contain** in **Original handoff** and **Marked upper corner**. The approval covers holding net front in Original, holding the passing lane to net front in Marked, and closing from the inside once RD takes coverage in both presets.

In `DefensivePractice` Play Mode, choose **Tools → Shift Happens → Watch LD Positioning**, then select **Original handoff** or **Marked upper corner**. Follow LD's active ring. The replay holds LD at the preset starting position until RD takes coverage, pauses there for four seconds, and then steers LD toward the inside of the moving carrier. It freezes after 0.7 continuous seconds in the contain position while Red still has the puck. Select **Replay** to repeat the same setup and steering.

| Preset | Hold at RD's handoff | Contain freeze |
| --- | --- | --- |
| Original handoff, seed 1 | 4.54 s at (185.00, 360.00), net front | 5.66 s at (176.27, 301.36) |
| Marked upper corner, seed 0 | 4.54 s at (143.73, 306.30), in the passing lane to net front | 5.68 s at (159.90, 272.17) |

The slot lane was covered at both checkpoints in both replays. RD had coverage when LD moved out. All 16 Unity practice checks passed before the approval.

### Isolated Stylized Pro visual proof

On November 11, 2026, after watching **Original handoff** and **Marked upper corner**, David approved the **lower perspective camera angle, larger skater and goalie models, and revised stick poses** for the isolated Stylized Pro proof scene.

After watching both animated replays on November 11, 2026, David approved **Blue LD's skating animation and the goalie's ready stance and lateral movement** for the isolated proof scene.

After watching **Original handoff** and **Marked right corner** on November 11, 2026, David approved **Blue LD as the active player, the right-side formation, and the puck staying against the Red carrier's stick blade during possession** for the isolated Stylized Pro proof scene.

In Unity Edit Mode, use **Tools → Shift Happens → Watch Stylized Pro Proof**. The separate scene shows five animated skaters per team and the defending Blue goalie. Blue LD wears the active ring. Select **Original handoff**, **Marked right corner**, or **Replay** in the Game view. The proof mirrors the formation across the middle of the zone, with the Marked carrier starting in the right corner and the Original carrier starting on the right at the original depth. The simulation uses the approved replay coordinates. During Red possession, the rendered stick blade meets the puck at its edge. The proof pauses at RD's handoff and at LD's contain checkpoint, then resumes automatically through the pass and freezes at the outcome. The outline follows the puck during the pass and switches off when the pass ends. The practice scene and build scene list retain their existing files.

Both proof replays matched the approved practice runs at the handoff and contain checkpoints, including LD, RD, and carrier positions and the recorded coverage flags. A repeated Original run matched again. All 16 Unity practice checks passed. The 1600 × 900 and 390 × 844 Game view captures show the blue line, crease, and full defending zone with glass confined to the perimeter. The 390 × 844 capture is an Editor layout check. The proof has no standalone or physical-phone build validation.

In the extended replays, Red released at 5.92 s in **Original handoff** and 5.94 s in **Marked right corner**. Both plays ended with the puck loose and contested, at 9.84 s and 9.60 s respectively. Hold and Contain both scored Met in both presets. The Unity Game view check observed the outline throughout both passes and verified that each outcome matched the same replay inputs run directly in the practice simulation.

On November 11, 2026, after watching both extended replays, David approved **Original handoff** and **Marked right corner** through the end of play for the isolated Stylized Pro proof scene. The approval covers the puck outline staying visible through the pass and the contested loose-puck ending in both presets.
