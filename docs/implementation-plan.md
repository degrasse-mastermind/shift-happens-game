# RD return and draft handoff implementation plan

The approved task is to preserve and consolidate the existing Unity practice, implement RD's bottom-corner return, and let LD contain after net-front coverage is established. The one-page blueprint remains draft coaching material.

- [ ] Preserve source archives and verify file hashes before changing gameplay.
- [ ] Consolidate the active Unity project into `unity`, browser source into `reference/browser`, and coaching documents into `docs`. Keep the published preview separate and record its location.
- [ ] Extend the existing `PracticeShift` state model with an editable draft scenario, moving RD, coverage-based handoff, two observed decisions, and player-dependent passing lanes. Reuse the display, input, pause and retry entry points.
- [ ] Verify delayed and wrong-side RD, early departure, timely containment, pause/resume, retries, existing branch and tie-up behavior, and a mirrored variation. Run Unity compilation, game-view checks and a Windows standalone build. Record phone readiness separately.

`PracticeShift` owns simulation state. `RinkPractice` reads input and renders that state. Scenario settings carry draft geometry and thresholds. Checks evaluate behavior through the simulation's public entry points. Generated builds and verification results stay outside source control.
