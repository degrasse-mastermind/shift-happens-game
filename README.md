# Shift Happens!

This folder is the main local workspace for the Unity game, coaching blueprint and browser reference.

| Folder | Purpose |
| --- | --- |
| `unity` | Active Unity 6.6 project. Open `Assets/Scenes/DefensivePractice.unity`. |
| `docs` | Current one-page blueprint, its editable rendering source and implementation notes. |
| `docs/historical` | Earlier blueprint found beside the Unity project. Its coaching content is historical. |
| `reference/browser` | Preserved browser prototype and original coaching and rink references. |
| `artifacts` | Local checks, screenshots and standalone builds. Excluded from Git. |

The Unity practice uses the supplied rink. Proposed handoff geometry, coverage thresholds and player feedback remain **draft for David's review**.

## Preservation and file locations

Before gameplay changes, source files were archived and SHA-256 verified under `C:\Users\degra\Documents\Shift Happens Backups\2026-11-10-before-handoff`. To restore, extract `unity-working.zip` to its own folder and add that folder in Unity Hub. The `manifest.json` lists source paths and hashes. The consolidated source baseline is local Git commit `8e79a6e`. Unity's generated Library, Logs and Temp folders are rebuildable and are excluded from the recovery ZIP. The existing Library and UserSettings moved with the active project.

The active Unity project moved from `C:\Users\degra\Shift Happens!` to `unity`. Browser source and the existing local browser build moved from the OneDrive `Youth Hockey Coach\Shift Happens!` folder to `reference/browser`. Browser dependency links were rebuilt from the unchanged lockfile. The nested Unity starter copy moved into the preservation folder as `unity-template-original`. The earlier blueprint moved to `docs/historical`. Both former source locations contain a `PROJECT-MOVED.txt` note.

The separately published browser preview repository remains at `C:\Users\degra\OneDrive\Documents\ChatGPT\Youth Hockey Coach\Shift Happens Mobile Preview`. Its hosted status has not been reverified. A Unity source change or a local browser build does not update that preview.

## Open and check locally

Add the `unity` folder as an existing project in Unity Hub using Editor `6000.6.8f1`, or use `Open Unity.cmd`. The game scene is `DefensivePractice`.

In practice, use WASD, arrow keys, or drag on the rink to steer **Y**. Start the shift, resume after the Guided pause, and watch RD come in from the bottom corner. After RD has established coverage, move toward the carrier from inside the lane. The **Guided / Freer** button restarts the seed in the chosen mode. **Point look** and **Slot look** select repeatable attacking preferences. The defenders' coverage can change which pass is actually attempted.

Browser coaching reference commands run from `reference/browser` with `pnpm dev`, `pnpm build`, or `node --test src/game.test.js`.

Implementation and validation details are recorded in `docs/implementation-plan.md` and `docs/handoff-validation.md`.
