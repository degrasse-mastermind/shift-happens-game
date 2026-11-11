# Desktop Review Mode (first implementation)

Click **Review / Export** in the game header. The simulation and gameplay controls freeze independently of Unity time scale. Proposed edits never move live players.

1. Choose **This moment** or **Starting setup**. Starting setup shows the current seed/settings at the initial state, not a rewind.
2. **Move**: drag a skater, goalie, or puck. A faint original stays visible. **Arrow** and **Circle** annotate the rink. **Undo mark** removes the latest mark.
3. Optionally type a note. Windows **Win+H** may dictate into the selected note field if supported by the OS; this feature has not been verified. There is no in-game microphone recording or external transcription.
4. **Save note & resume** captures the annotated game view and stores a durable note. **Discard draft & resume** changes nothing in gameplay.
5. Open Review again and **Export saved reviews**. Use **Open reviews folder** to find `Exports`. Attach the ZIP manually to the project Work chat. Export does not send anything automatically.

The local queue survives app restarts. An unsaved draft does not survive app termination. Scope changes are blocked while marks exist so starting positions cannot be silently relabeled as in-play movement. Save/discard before changing scope.

## Package

ZIP includes `review.md`, `review.json`, and one annotated PNG per note. JSON records stable IDs for 12 players and the puck, original/proposed coordinates, seed, mode, scene, build, game time, phase, possession, pause state, orientation and draft settings. The screenshot captures the game review overlay only, not other desktop windows.

Notes cover arbitrary game changes through free text. This version draws marks on the rink only, not on arbitrary gameplay UI widgets. Arrows can also describe facing direction; no separate facing tool yet. No audio recording, replay/rewind, mobile validation, automatic chat delivery or automatic gameplay modifications.

## Validation

Unity menu: **Shift Happens > Validate Review Mode**. Runs the existing gameplay handoff checks, then guided/freer review freeze/resume, copied proposals/undo, disk save/load and portable ZIP checks. Success logs `REVIEW_CHECKS_PASSED`. Full interactive drag/save/export testing and Windows dictation must be performed on the desktop build before calling this release verified.

## Verification receipt (2026-11-11)

- Runtime and editor C# compilation succeeded using the installed Unity 6000.6.8f1 compiler and generated project references. Repeated after final rendering fix.
- Unity menu validation displayed `REVIEW_CHECKS_PASSED`: existing gameplay handoff cases, guided/freer freeze/resume, copy/undo, durable save/load, ZIP PNG/Markdown/JSON.
- In Unity Play Mode: opened Review, selected Starting setup, dragged LD, saw original ghost, drew an arrow, typed a note, saved and resumed. Live LD remained unchanged at 0.0s. Disk PNG and JSON verified, with stable blue.ld ID, Starting setup scope and exact mark coordinates.
- Found scaled-arrow display offset during interactive verification; changed line drawing to compose its transform with the existing GUI matrix. Final code compiled, but final visual retest was blocked by a separate active Unity handoff-check window. Do not call rendering fully verified yet.
- Remaining: visual retest arrow/circle at multiple window sizes, click Export and open produced package, test interactive Undo/discard, confirm queue after full restart, validate Windows dictation, produce and test standalone Windows build. No new standalone build was produced in this pass.
- A clearly labeled TEST REVIEW exists in the local queue; it is verification data, not David's requested gameplay edit.

### Follow-up desktop verification (2026-11-11)

- Reopened the consolidated Unity project after the previous editor session closed. Added its existing folder to Unity Hub; retained the stale old entry without deleting anything.
- Confirmed the prior saved TEST REVIEW appeared in Review queue / 1 after full editor process restart.
- Visually verified the corrected arrow endpoints and circle annotation at both docked Game view (0.31x) and maximized Game view (0.58x), using the QHD game resolution.
- Clicked Undo mark: circle disappeared while the earlier arrow remained. Clicked Discard draft & resume: returned to unchanged ready gameplay; reopening Review showed no draft marks and still one saved review.
- Clicked Export saved reviews and saw the success message. Opened the resulting ZIP read-only: review.json, review.md and the saved annotated PNG are present; JSON and Markdown identify the existing test review correctly.
- Left Unity in Play Mode at the initial ready screen, maximized Game view, with no unsaved review draft. No additional review notes were saved.
- Remaining limitations: Windows dictation needs a manual Win+H test. Standalone Windows runtime remains unverified because the earlier executable was blocked by Windows Application Control; no protection was changed or bypassed and no new standalone build was produced in this follow-up.

### David's playtest feedback (2026-11-11)

- David reported: "audio didnt work but thats ok, review exported save notes" (message Sentinel_e129a5faacc08191be731c19a6d02b42).
- Review export succeeded in David's own test. This records his reported result; the contents of his exported review were not opened or evaluated for this update.
- The attempted audio/dictation input did not work. The exact steps, symptoms and cause are unknown. This is not evidence of an in-game audio-recording failure: this version has no native recording feature and only offers optional OS dictation into the note field.
- David said the audio limitation was acceptable for now. Keep it as a deferred issue to investigate; do not block use of the working annotation and export workflow.
- No gameplay edits, coaching approval or standalone-runtime verification are implied by this feedback.
