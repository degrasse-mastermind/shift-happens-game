# Shift Happens! governing game-controls guide

Adopted as project-work guidance at David's request on November 12, 2026 UTC (November 11 Eastern). This guide synthesizes the five archived assistant responses in `source-responses/`. It governs future planning and implementation; it does not claim that proposed controls have been built or that draft hockey rules have coach approval.

## 1. Scope, authority and preservation

- Extend the existing Unity game. Do not build a replacement engine or maintain a second full browser gameplay system.
- Read the root AGENTS.md and this guide before relevant work. Preserve existing code, useful prototypes, assets, settings, review exports and checkpoints.
- Treat the source-response files as verbatim historical records. This guide is the actionable synthesis. Time-relative statements in the records describe that conversation, not the current build.
- Preserve established project decisions. Follow later explicit user direction. Do not turn a proposed numerical threshold or illustrative scenario into an approved coaching rule.
- Distinguish implemented, tested, coach-approved, draft and proposed. A file, test result or visual proof alone does not establish all of these.
- These instructions do not authorize publishing, pushing, deleting work or bypassing security protections. Respect the current no-push direction until David changes it.

## 2. Product and architecture principles

Unity supplies the engine. Shift Happens supplies the hockey simulation and coaching system on top of it.

Organize work around six responsibilities:
1. Rink and movement: geometry, skating, turning, puck movement and contact.
2. Hockey state: positions, possession, loose puck and coverage ownership.
3. Player intelligence: perception, responsibility and action selection.
4. Scenario direction: starts, roles, routes, variation and reset.
5. Coaching and evaluation: cues, teaching triggers and assessment.
6. Presentation: camera, characters, animation, interface and Review Mode.

The simulation is the authoritative game state. Presentation reads that state. Animation or a visual asset must not secretly determine possession or the correctness of a read. Separate coupled responsibilities gradually as actual features require it, not through a disruptive rewrite.

Keep three categories explicit:
- Rules define correct hockey.
- Settings tune behavior and scenario parameters.
- Live state changes during play.

A slider tunes a behavior; it does not replace the rule governing that behavior. Changing speed, visuals or hint visibility must not silently redefine the player's hockey responsibility.

## 3. Hockey and evaluation governance

- Use the team's approved Box + 1 interpretation and the supplied foundational coaching references.
- Evaluate the cue, decision and consequence separately from the shot outcome. A save does not excuse an abandoned assignment; a goal does not automatically invalidate a sound read.
- Distinguish a deflection or contested touch from controlled possession.
- Define coverage establishment explicitly, including the relevant position, orientation, responsibility and time conditions for that scenario.
- Keep the behind-net and delayed-exit responsibilities and scoring labeled draft until David or an authorized coach validates them.
- The archived generic example about a partner establishing coverage is not a universal behind-net rotation rule. Preserve scenario-specific responsibilities. Do not make LD chase or contain merely because the puck crosses behind the goal.
- Keep feedback short, specific, actionable and appropriate for 12U.

## 4. Configuration and control standards

Each adjustable setting must document:
- Plain-English purpose and owning system.
- Units and coordinate system.
- Current default, permitted range and relationships to other settings.
- Whether it is inherited from current behavior, proposed or coach-approved.
- Whether changing it requires restarting the scenario.
- How it will be inspected and tested.

Preserve the existing coordinate system and Review Mode mapping in the first tuning pass. Current simulation coordinates have a non-uniform conversion to the dimensioned displayed rink. Do not label simulation distances as feet or meters or apply one global conversion. Any future physical-coordinate migration requires its own compatibility and regression plan.

Use named scenarios and presets, working copies, reversible changes and explicit reset. Do not silently edit a running drill's rules or overwrite an approved baseline. Prefer visual position editing and a small number of understandable controls to a spreadsheet of hundreds of numbers. Keep advanced settings secondary.

Longer-term controls may cover setup, facing, roles, movement, puck behavior, coverage, support, recognition, cues, overlays, pause/step, comparison and export. Their presence in this guide does not mean they are all in the first release.

## 5. Existing foundation to reuse

The November 12 source inspection found:
- DraftShiftSettings: scenario inputs, positions, roles and several coaching thresholds.
- PracticeShift: live simulation, possession, movement, coverage, teaching and scoring.
- ZoneSkating and BehindNetRotation: team movement and scenario route behavior.
- RinkPractice: preset selection, controls, display and Review Mode integration.
- DefensiveZoneView: coordinate projection.
- ReviewModel, ReviewMode and ReviewStorage: capture, annotation and export.
- Editor/PracticeValidation: an existing 18-check gameplay validation entry point.

The existing presets are Original, Marked, Behind net and Behind net delayed exit. Each PracticeShift copies its selected settings. Some movement, timing and route values remain hard-coded. Reinspect before implementation: this is a dated inventory, not a guarantee that files or behavior are unchanged.

## 6. First implementation slice: Practice Tuning v1

Goal: make the current Original drill understandable and tunable before building a broad scenario editor.

Required flow:
1. Open the existing Original drill and inspect its starting setup.
2. Change the partner's starting position and return speed in a working copy.
3. Apply and restart the same drill/seed.
4. See an overlay explaining when and why coverage becomes established.
5. Save as a named draft preset, compare with Original and restore Original.
6. Export feedback identifying the preset/configuration used.

Safeguards:
- Original remains the baseline.
- Apply changes on explicit restart, not silently mid-play.
- Do not let a draft overwrite the baseline.
- Preserve the current gameplay behavior at unchanged settings.
- Preserve Review Mode capture and mapping.
- Broader AI improvements, full scenario authoring and playable 3D integration are separate stages.
- Do not describe this panel as built until it exists and its required flow is verified.

## 7. Build sequence

1. Map existing positions, movement values, responsibilities and scoring; preserve a working checkpoint.
2. Establish a named, editable configuration for the existing Guided Original shift, reusing the current settings.
3. Add optional role, coverage and destination overlays so behavior can be judged.
4. Build the first small tuning controls with save, reset and comparison.
5. Verify baseline equivalence and hockey behavior; obtain coach review and an unaided first-player playtest.
6. Extend the proven structure to behind-net and center-cover situations.
7. Connect the approved 3D presentation without replacing the underlying simulation.

This is part of the established development plan, not a declaration that its earlier phase-acceptance conditions are complete.

## 8. Verification and delivery gates

Use the existing gameplay checks and add targeted tests for:
- Baseline behavior preserved with unchanged settings.
- Working-copy edits cannot mutate an approved preset or active run.
- Reset reproduces the intended setup and selected seed.
- Mirrored scenarios mirror relevant positions and assignments consistently.
- Hint visibility does not change hockey rules.
- Deflection does not automatically grant possession.
- Feedback follows observed responsibilities rather than goal/save alone.
- Review exports identify their preset/configuration and retain usable coordinates.

Repeatable tests support debugging; do not claim cross-device deterministic physics solely because a seed is present. Record relevant configuration/build versions and inputs where needed.

Run coach review and first-time player tests in addition to technical checks. Verify desktop, standalone and phone separately. Editor success does not certify a new mobile build.

For each implementation delivery, report what changed, what was tested, evidence, remaining draft/unverified items and next step. Leave a repeatable live playtest ready in Unity as required by the root project instructions.
