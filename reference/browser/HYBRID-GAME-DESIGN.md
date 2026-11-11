# Shift Happens! hybrid game design

Working proposal, November 9, 2026. This describes the first playable slice of a larger 5-on-5 hockey game. It is not a coach-approved tactical model or a complete game specification.

**Current exchange correction, November 9:** The earlier proposal below says the controlled LD takes containment after RD becomes set. Playtest corrections supersede that assignment for the playable exchange: Blue RD contains the lower-side puck carrier, Blue C tracks Red LW, and the controlled Blue LD covers Red C near the net, with a stick tie-up available. See [BOX-PLUS-ONE-MODEL.md](BOX-PLUS-ONE-MODEL.md) and [NEXT-STEPS.md](NEXT-STEPS.md). The five-decision opening-shift target remains a proposed product structure; its later reads need coaching review before implementation.

## What David decided

- The game should replicate actual 5-on-5 ice hockey games.
- Control should feel more like a hockey video game, but in a controlled manner: a combination of active play and teaching moments.
- Guided play runs in real time, automatically pauses at a few meaningful teaching moments by default, gives short consequence-based feedback, and then resumes. A freer practice mode should turn automatic pauses off and review key moments afterward.
- The supplied text-free, full-rink diagram is the rink asset. Arcade Ice remains the player-facing interface direction.

## Recommended experience

The player controls one skater for one shift. All other skaters, both goalies, and the puck keep moving. The player skates, faces the play, and uses hockey actions in real time. In guided play, the game automatically freezes at selected reads, asks a short question, and lets the player commit an intent. Play resumes for the player to act; a brief consequence cue then shows what changed before the shift continues. The game judges the action and its timing in play; selecting an answer alone is not successful hockey execution.

One shift has five skaters and one goalie per team: the controlled defenseman plus four AI teammates, five AI opponents, and two AI goalies. A clock, score, possession changes, passes, shots, saves or goals, whistles, and restarts make the slice feel like hockey. The full rink remains available as context while the camera follows the active area. The rink artwork is schematic; game coordinates and rule boundaries must be defined separately.

## First playable defenseman shift

**Opening-shift target:** a repeatable 5-on-5 defenseman shift, initially aiming for 60-90 seconds of game time if the sequence remains believable. Start after a legal offensive-zone entry by the opposing team, with both teams in believable positions. The opponent carries or moves the puck inside the zone. The defense partner is returning while the controlled defenseman protects the inside. The partner then becomes visibly set, creating the opportunity to take containment. The puck can be passed, turned over, shot, saved, cleared, or scored; the play should not be a fixed animation with one guaranteed outcome. A whistle or shift-end event closes the attempt and shows a short replay and coaching summary.

The earlier S04 storyboard supplies two candidate teaching reads: hold net-front coverage while the partner returns, then take containment once the partner is set. The first read can be prompted at a pause; the second should be a new decision when the partner is visibly set. Both become **observed actions** when play resumes, rather than credit for tapping an answer card. Proposed scoring inputs are the player's position relative to the net and inside threat, the partner's readiness, the player's timing, and whether the team protects the high-danger space. David must approve the tactical geometry and accepted responses before any scored claim is shown.

Use a small set of seeded variations so the puck side, approach lane, and timing differ on a fresh attempt. The same simulation and controls run in guided and unassisted attempts. Preserve the agreed opening-shift structure of **five scored decisions**: S01 contributes one, and each of two S04 arrangements contributes two. A decision is a distinct player read and response; a pause is a teaching event. The initial test target is **two or three automatic pauses per guided shift**, not a hard rule and not a reduction in scored decisions. One proposed mapping is one pause before S01 and one before each S04 exchange; the second S04 decision in each exchange occurs after play resumes. This makes three pause events and five scored decisions. Test whether that sequence feels like a plausible continuous shift rather than forcing events to hit a count.

## Control proposal

| Action | Phone, landscape play | Desktop |
| --- | --- | --- |
| Skate | Left virtual stick | WASD or arrow keys |
| Face or aim | Right drag area, with assisted facing when released | Mouse pointer, with assisted facing when idle |
| Pass | Right-side Pass button; aim at a teammate | Space; aim with pointer |
| Shoot or clear | Right-side Shoot/Clear button; context changes label | Left mouse button |
| Defend | Right-side Stick button for a controlled poke or block attempt | E |
| Inspect play | Pause/coach button, separate from hockey actions | P |

The first slice should use forgiving pickup, passing, and facing assistance, with visual cues for the controlled player and current puck carrier. Defensive success should primarily come from skating into position and choosing when to engage. Do not make a body-check control a prerequisite for this youth coaching slice. Keep touch targets at least 44 px, permit remapping later, and test both touch and keyboard input. The existing 320/390 px portrait storyboards remain useful for coaching review and menus; active full-rink play should be designed and tested in phone landscape as well.

## Puck, AI, and rules

- Model puck location, velocity, possession, last touch, and transitions through passes, loose pucks, shots, rebounds, saves, and goals. The puck must not teleport between players.
- Teammate AI supports the puck, covers lanes, rotates within its role, and reacts to a turnover. Opponent AI carries, passes, attacks space, and shoots. Goalies track the puck, attempt saves, and cover rebounds. Use bounded, readable behaviors for the first slice rather than attempting a full professional-level simulation.
- Keep all ten skaters on the ice and make movement continuous when play is live. The player controls one skater; AI handles the other nine and both goalies. If play is paused for instruction, every actor, puck, and game clock freezes together.
- For this first defensive-zone slice, support goals, saves, puck out of play, whistles, clock, score, and a restart or end-of-shift transition. Define offside, icing, penalties, faceoffs, line changes, and period structure as part of the full-game rules plan. Do not claim full-match realism while those systems are absent. Choose the age/league rule set before implementing or evaluating those rules.

## Coaching without losing game feel

1. **Guided practice, default:** play live until a meaningful read. Automatically freeze all players, goalies, puck, and clock. Highlight only the actors needed to understand the situation, ask one concise question, and let the player commit an intent. Resume a short live action window so the player can execute the decision; the prompt alone does not earn credit.
2. **Consequence and feedback:** show the near-term result in the action, such as protected space remaining covered or a lane opening, then give a short explanation and continue the shift. Keep text off the puck and active controls. At the whistle or shift end, offer a replay with the player's path and the relevant spacing or timing. Tactical feedback stays marked proposed until David validates it.
3. **Guided retry:** keep progress, allow another try, and distinguish it from mastery evidence.
4. **Freer practice:** turn automatic pauses and in-play tactical highlights off. Keep ordinary user-invoked pause. After the shift, review key moments and consequences. This mode should use the same hockey simulation and controls.
5. **Fresh unassisted challenge:** no automatic teaching pauses or hints; score the five decisions from play. Both decisions in each S04 exchange must be correct in the same fresh variation for that exchange's mastery, subject to coach-approved scoring. Preserve the existing once-per-skill five-token reward rule.

Two or three automatic pauses per guided shift is a starting usability target. If testing shows that a pause interrupts the play or an important read is missed, move or remove that pause while retaining the underlying decision.

## Scope boundary for the first build

Build toward one playable opening shift, one controlled role, one rink, opponent behavior with variations, five scored decisions, and enough game systems to make passes, shots, saves, goals, and a stoppage meaningful. Do not present a smaller prototype as the complete opening shift, match, or stage campaign. A later milestone can add attacking and wing roles, line changes, penalties, full offside and icing behavior, periods, and broader opponent tactics. Keep the simulation, rules, coaching prompts, and rendering separate so those systems can grow without replacing the first playable slice.

## Next actionable prototype milestone

Create a **single S04 exchange prototype within a live 5-on-5 defensive-zone shift**. It should have one controlled defenseman, nine moving AI skaters, two moving AI goalies, a moving puck, clock and score, a plausible possession-to-shot/save/goal or turnover sequence, and one automatic teaching pause before the first handoff read. The player commits an intent, resumes control, and makes two distinct defensive decisions as the partner returns and then becomes set. Show one short consequence-based cue and a post-shift review. Add a Freer practice toggle that runs the same exchange without automatic pauses.

This prototype proves the core loop and two-decision exchange; it is **not** the complete five-decision opening shift. Expand it to S01 plus two S04 arrangements only after the live loop, pacing, and coaching geometry have been reviewed. Use temporary, clearly labeled scoring diagnostics until David approves the hockey positions and answer rules.

## Verification before calling the slice playable

- Roster checks: exactly five skaters and one goalie per side during live 5-on-5 play; the controlled skater is unambiguous.
- Simulation checks: puck movement and possession remain continuous; passes, turnovers, shots, saves, goals, whistles, and clock transitions produce consistent states; seeded variations reproduce failures.
- Input checks: phone touch and desktop keys control the same actions; controls remain reachable and readable; play responds promptly; pause freezes and resumes every actor and the clock.
- Game-flow checks: a complete attempt can start, be played, end, show feedback, and restart without a broken or stuck state.
- Coaching-flow checks: an automatic pause occurs at the intended game state, freezes every actor and the clock, accepts one response, resumes exactly once, and shows a short consequence; Freer practice suppresses the automatic pause but preserves later review.
- Decision-count checks: the S04 prototype records two distinct decisions; the eventual opening shift records five decisions across S01 and two S04 arrangements, regardless of whether guided play uses two or three pauses.
- Presentation checks: the active area and full-rink context are readable on phone landscape and desktop; portrait review screens and accessibility cues still work; selection or status never relies on color alone.
- Coaching gate: David validates the handoff geometry, actor behaviors, accepted actions, feedback wording, and scoring thresholds using recorded play before mastery or rewards are enabled.

## Decisions still needed from David

1. Which age group and rule set should the game represent?
2. How much control assistance feels right: arcade-like forgiving movement and passing, or more demanding stick and puck control?
3. Is one defenseman shift the right first playable example, and should it start in the defensive zone as proposed?
4. What should the camera show on a phone during live play: full rink continuously, or a follow camera with a persistent full-rink mini-map?
5. At an automatic pause, should the player choose an intent from short options, point to a target area, or use another interaction? The decision is scored from subsequent play, not the prompt alone.

## Relationship to existing material

The S04 v2 phone storyboards and their review note remain unchanged. They are proposals for teaching and feedback states, not a specification for live 5-on-5 play. The existing rink source is unchanged.

## Experimental prototype status

A local React/Vite S04 exchange slice now exists in `src`. It implements the two-read guided and freer-practice loop described above using draft positions and simplified AI and puck movement. Its executable checks are in `src/game.test.js`; see `README.md` for play instructions. It does not complete the five-decision opening shift, validate coaching geometry, or implement full-match rules. No deployment was made.
