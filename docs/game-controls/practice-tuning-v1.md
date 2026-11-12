# Practice Tuning v1

In **Defensive Practice**, select **Original handoff**, then choose **Tune RD** under the drill controls. The shift pauses while the tuning panel is open. Tap or drag on the displayed ice to move the draft RD marker, or use − and + to adjust return speed.

| Setting | Original default | Draft range | What it controls |
|---|---|---|---|
| RD starting position | (130, 600) | X 110–410, Y 190–610 | Where RD begins in the defending zone. These are unmirrored simulation coordinates with X increasing right and Y increasing down. The rink display converts them using the existing Review Mode mapping. |
| RD return speed | 48 simulation units per second | 24–72, in steps of 4 | RD's movement toward coverage. This speed uses simulation distance, which has no single conversion to feet or meters. |

The defaults come from Original. The edit ranges are proposed limits for this tuning tool. The draft settings change the starting setup and movement speed. The existing coverage and scoring rules still determine the handoff.

- **Apply & restart** returns to Start with the edited setup and the same variation. Press **Start** to play the draft.
- **Save draft** stores one named draft on this device. Saving again replaces that slot. **Load draft** brings it back into the editor for inspection or application.
- **Restore Original** returns to Start with the original settings and the same variation. **Cancel** closes the panel and resumes the current run from the same state.
- In Guided mode, watch for RD's coverage cue and handoff time. Review Mode identifies the preset played and records its variation and applied settings in the export.

For a repeatable comparison, hold the same variation and replay the same LD inputs in Original and the draft. Check that RD's starting spot and handoff timing reflect the applied draft, and that restoring Original reproduces the baseline. Use a Review export to compare the starting snapshot and settings.

In Unity Play Mode, **Tools → Shift Happens → Watch First Guided Shift → Hold then close in current setup** runs the scripted Guided comparison using the selected Original or draft setup and its variation.
