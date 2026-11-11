# Shift Happens! defensive exchange prototype

This local experimental slice extends the existing Arcade Ice React prototype and uses the selected clean rink asset from `visual-design`. It applies the [Box + 1 movement model](BOX-PLUS-ONE-MODEL.md) to one defensive exchange. It has one controlled blue defenseman, four AI teammates, five AI opponents, two AI goalies, a moving puck, one teaching pause, two draft defensive reads, short consequence feedback, and a freer-practice mode. It is **not** a complete hockey match or the five-decision opening shift.

## Try it locally

From this folder, run:

```powershell
pnpm install
pnpm dev
```

Open the local address printed by Vite. If dependencies are already installed but `pnpm` is unavailable, run `node node_modules/vite/bin/vite.js` instead.

1. Leave **Guided** selected and start the shift. You control **Y**, the blue left defenseman marked by a star with a light gray outline; **RD** is the returning blue right defenseman.
2. Move with WASD, arrow keys, or by tapping and dragging on the rink. Use **1** or **Stick**, **2** or **Pass**, **3** or **Clear**, and **4** or **Tie up**. Tie up works when LD is within stick reach of Red C. It briefly restrains that skater and can deny a net-front pass; moving away breaks the tie and the action has a short cooldown. Space pauses or resumes manual play.
3. At the automatic teaching pause, choose an intent. Play resumes so you can execute it. Stay near net front until RD sets; then cover Red C near the net while RD handles the puck side. Two draft reads appear in Shift review.
4. Select **Freer** to restart the same exchange without the automatic teaching pause. Review the two reads afterward. **Play again** restarts the shift in the selected mode.
5. At shift end, choose **Watch replay**. Scrub the recorded shift or jump to Read 1 and Read 2. The rink shows Y's path and numbered read markers. The replay samples positions about every half second, so it is a review aid rather than continuous video.

Each new shift rolls an opponent seed in both modes. Red can cycle to the point or try a short feed to Red C after the carrier comes out from behind the net. A blocked lane can send Red back to the point. Intercept the feed, or use **Tie up** to deny the reception and **Stick** to pick up the loose puck if it is within reach.

For repeatable practice, append `?seed=0` to the local URL to force the point cycle, or `?seed=1` to favor the net-front feed. The replay shows the seed and records the pass and its result. Reusing the seed reproduces Red's choice under the same play conditions; your actions can still change the result.

On a narrow phone the camera follows Y, RD, the puck, and the defending net. The inset full-rink overview marks the camera area, Y, and the puck. Short-height landscape layout is designed to keep the play area and action buttons in view; desktop shows the full rink. The narrow layouts still need a visual device check.

## Exchange setup

Red starts with RW carrying low on the LD-side circle near the corner, RD supporting at the upper point, C near the blue net, and LW closer to the net and middle on the weak side. Red LD is at the lower point, slightly closer to the middle lane. Blue LW and RW start inside the tops of their respective circles, and blue RD starts by his goal-line corner. The red carrier goes through the corner, crosses behind the blue net, and emerges on the RD side. Blue RD guards the post outside the goalie's space, then steps out to contain the lower-side carrier. Blue C tracks Red LW while controlled LD stays with Red C near the net and may tie up that coverage. The puck-side wing stays high toward the point; the far wing tucks into the high slot. They exchange those jobs as Red comes out on the other side. While Red has the puck, its two other forwards and point defensemen move as connected support around the carrier and keep distinct lanes. After a blue turnover, blue teammates move into outlet lanes and Red enters a [12U 2–1–2 forecheck](RED-12U-FORECHECK.md): F1 angles toward the puck, F2 closes the wall outlet, F3 protects the middle, and both defensemen stay at the points. AI skaters accelerate into their targets and separate when their paths converge. The carrier can cycle to the new puck-side point, and a pass may be intercepted. A point shot can be saved, while a far-side weak-side shot can score if the goalie cannot recover in time. Winning the puck allows an outlet pass or a clear out of the defensive zone. These are bounded prototype behaviors, not a full 5-on-5 tactical AI.

## Status and checks

Run `node --test src/game.test.js` and `node node_modules/vite/bin/vite.js build` to check the game logic and build.

Tactical positions, coverage thresholds, consequence wording, and the simple shot outcome are **draft pending David's coaching review**. Reads use coverage accumulated while Red controls the puck; an early turnover supersedes them, and a missing play window is marked unobserved. Freer mode shows those coaching results only after the shift. The current shift ends after a shot, clear, breakout, or the 30-second limit. It awards no mastery or tokens. Offside, icing, penalties, line changes, contact rules, and full period play are not implemented. The prior S04 review sheet remains available in the app.

The local development server must be running to use the development URL. An HTTPS mobile preview is hosted through Sites; its built files are copied into the separate `Shift Happens Mobile Preview` folder before publication. Check the Site's current access setting before each publication. The hosted version changes only after a new build is published.
