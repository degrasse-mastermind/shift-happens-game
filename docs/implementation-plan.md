# RD return and draft handoff implementation plan

The approved task is to preserve and consolidate the existing Unity practice, implement RD's bottom-corner return, and let LD contain after net-front coverage is established. The one-page blueprint remains draft coaching material.

- [x] Preserve source archives and verify file hashes before changing gameplay.
- [x] Consolidate the active Unity project into `unity`, browser source into `reference/browser`, and coaching documents into `docs`. Keep the published preview separate and record its location.
- [x] Extend the existing `PracticeShift` state model with an editable draft scenario, moving RD, coverage-based handoff, two observed decisions, and player-dependent passing lanes. Reuse the display, input, pause and retry entry points.
- [x] Verify delayed and wrong-side RD, early departure, timely containment, pause/resume, retries, branch and tie-up behavior, and a mirrored variation in Unity. Compile the Windows standalone build.
- [x] Open the isolated revision in Play Mode and verify RD's starting position, movement and the Guided pause. See `handoff-validation.md` for the limits of the interactive, standalone and phone checks.

`PracticeShift` owns simulation state. `RinkPractice` reads input and renders that state. Scenario settings carry draft geometry and thresholds. Checks evaluate behavior through the simulation's public entry points. Generated builds and verification results stay outside source control.
