# Shift Happens! next build

Working plan, November 9, 2026. The current published game is one playable, approximately 30-second S04 defensive exchange. It has one guided pause, two draft reads, a freer mode, a replay, and a stick tie-up. It is not the full opening shift or a complete match.

## Priority 1: make the current exchange react to the player

Build one additional **seeded branch** within the same defensive-zone setup. Red can either cycle the puck to a point, as it does now, or look for Red C at the net front. Blue RD contains the low carrier; Blue C tracks Red LW; controlled Blue LD covers Red C. If LD ties up Red C at the right time, the net-front pass or reception is disrupted. If Red C gets free, Red gains a real shot or rebound chance. Neither branch should guarantee a goal or save.

Use possession, puck location, player separation, and a small seeded choice to trigger the branch. Avoid adding another fixed timestamp sequence. Show the branch and its cause in the replay so the coach can tell whether the puck play and coverage response made sense. Keep the existing rink asset and React/Vite SVG playfield; separate scenario definitions, AI targets, puck outcomes, and coaching reads as the behavior grows.

**Acceptance for this build:**

- Two reproducible branches can be selected for testing, while ordinary play varies between them.
- Red C can actually become a receiving or rebound threat in a played shift; Tie up has a visible consequence in that branch.
- RD stays clear of the goalie while containing the carrier, Blue C tracks Red LW, and LD's second read evaluates Red C at the net front.
- Guided and freer modes run the same puck and skater simulation. A pause freezes everyone; replay shows the action and its result.
- Desktop and 390 px and 320 px phone layouts keep the star, puck, threat, and five controls readable. Simulated browser checks and a real-phone playtest are both recorded distinctly.
- Draft coaching results remain marked as draft. No mastery or token award is added until the actor geometry and accepted outcomes are coach-approved.

## Priority 2: tune hockey feel from recorded play

Review a small set of full-shift replays for skater spacing, movement speed, passing lanes, goalie reactions, and tie-up reach and duration. Adjust one behavior at a time against a reproducible seed. Add a simple in-game branch or seed label to make coach comments easy to map to a specific play. Check landscape phone play as well as the portrait layouts used so far.

## Priority 3: expand the opening defenseman shift

After the two-branch exchange is reviewed, add the S01 read and a second, distinct S04 arrangement. Reconcile the older five-decision plan with the user's corrected RD, C, and LD responsibilities before writing new answer keys. Only then consider a fresh unassisted challenge and the once-per-skill token rule. A separate wing role remains a later slice.

## Current decision

Start with the **Red C net-front pass branch**. It uses the coverage and Tie up action already in the game and provides the clearest test of whether the current teaching read changes a hockey outcome. Keep the current point cycle as the comparison branch.
