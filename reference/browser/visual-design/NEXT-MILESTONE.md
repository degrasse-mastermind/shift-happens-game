# Shift Happens! next design milestone

**Historical handoff:** This file records the earlier storyboard plan. The current playable exchange and next build are documented in [../BOX-PLUS-ONE-MODEL.md](../BOX-PLUS-ONE-MODEL.md) and [../NEXT-STEPS.md](../NEXT-STEPS.md). In the current exchange, RD contains the carrier while LD covers Red C near the net; the older LD-containment wording below is superseded.

## Current product direction (updated November 9, 2026)

David approved a hybrid 5-on-5 game: real-time control of one skater, with automatic pauses at a few meaningful teaching moments by default, short consequence-based feedback, and play resuming. A freer practice mode will turn automatic pauses off and review moments afterward. The initial pacing target is two or three pauses per guided shift, subject to playtesting. A **pause event is not a scored decision**: the opening defenseman shift still requires five scored decisions across S01 and two S04 exchanges unless David changes that requirement.

The local **live S04 exchange prototype** is now in `src`: full 5-on-5 actor counts, puck movement, one automatic teaching pause, two distinct draft defensive reads, resumed play, consequence feedback, and a no-automatic-pause practice toggle. It is an experimental slice for testing the core loop before expansion to the five-decision opening shift. See [HYBRID-GAME-DESIGN.md](../HYBRID-GAME-DESIGN.md) and [README.md](../README.md) for the proposal and play instructions. The visual-design handoff below records earlier storyboard work and remains useful for coaching review; it is no longer the full game milestone. Coach-approved scoring and full-match rules are not claimed.

Prepared November 9, 2026 from the existing Finalize Shift IQ design chat and the verified local rink assets. This is a working handoff, not a verbatim restoration of the missing design brief or approval of tactical coordinates.

## Current state

- The approved visual direction is Arcade Ice. The user accepted it for the phone mockups in the existing chat.
- The chat records completed 390 px and 320 px first-pause design mockups. Those files and the earlier game design brief are absent from their expected project paths in the current workspace. Do not claim they are currently delivered or reconstruct them as exact copies.
- The current visual-design folder contains the source rink image, clean raster reference, editable rink-base-clean.svg, rendered preview, and RINK-ASSET-DECISION.md. The rink is horizontal, with the defending zone on the left.
- The user's latest rink instruction supersedes Figma: use the supplied diagram for game development and remove its text. Preserve the supplied source and clean base. Tactical actors and teaching cues remain separate layers.
- No game implementation or deployment was found here. This next milestone remains visual design.

## Next deliverable: S04 two-pause mobile storyboard

Create one coherent review sheet at both 390 px and 320 px widths using the approved rink asset and Arcade Ice direction. Show:

1. First pause: the partner is still returning; the player chooses to hold net-front coverage. The player-facing label is Defenseman practice / Protect net front.
2. Selected state: the choice is identifiable through a persistent outline and text or shape, not color alone. Show a distinct Confirm control.
3. Feedback state: explain the protected space with a separate overlay. Keep factual feedback contingent on coach-reviewed scene geometry.
4. Second pause: the partner is visibly set; ask for a new player decision to take containment. Do not award credit merely for animation shown in feedback.
5. Retry and fresh unassisted variation states: clearly distinguish guided practice from mastery evidence.

Use controls at least 44 px high, readable text, clear controlled-player identity, consistent non-color selection cues, and a restrained legend. If the full rink is too small to read on a phone, retain its orientation and geometry while testing a clearly labeled defending-zone crop plus full-rink context. Do not silently alter the rink source.

## Decisions to preserve

- One role per shift. Opening defenseman practice uses S01 and two S04 arrangements, totaling five scored decisions. S05 belongs in separate wing practice with a visible role-change screen.
- Each S04 exchange includes two actual choices: hold net-front coverage until the partner is visibly set, then take containment. Both must be correct in a fresh unassisted variant for exchange mastery.
- Guided retries are free and do not remove earned progress.
- Five tokens are available once per skill for a fresh unassisted challenge after practice. No prior mistake is required; extra mistakes cannot increase the reward.
- Introductory practice is a pilot slice, not proof of complete stage 1 or stage 2 campaign mastery.
- Coaching-rule approval was confirmed and its date corrected to November 8, 2026 in the source chat. Exact actor placements, safe reach, scoring geometry and answer keys still require coach review. Do not confuse rule approval with geometric validation.

## Review gate

Before implementing gameplay, review the storyboard for legibility at both widths and ask the coach to validate the pictured handoff, actor locations and accepted answers. Keep all tactical placements labeled draft until that review. Render and inspect every delivered screen; report checks as passed, failed or not run. Keep the original rink assets unchanged.

## Source and verification

Existing native Codex chat: Finalize Shift IQ design, thread 01a1be32-51a8-7091-b1da-f2116e0a8781, in Youth Hockey Coach on Jimothy. Relevant user decisions appear in turns 01a1be3d-87e8-7cb2-b519-91a921b12414, 01a1be73-d3cd-7fa3-9a7c-1eb3d8434bae, and 01a1be99-f237-7c63-a42a-86255a9abf19.

Checked for this handoff: read the source chat and current asset note; inspected the clean preview image and SVG markup; enumerated current project files. No storyboard, gameplay, interactive accessibility test or coach geometry review was completed by this handoff.

## Storyboard delivered in this pass

Four editable SVGs and matching PNGs are now saved as s04-storyboard-{320|390}-pause-{1|2}. They show selected states for both pauses, with an explicit Confirm choice and a partner position change. The actor locations are illustrative placeholders, not validated hockey instruction. Opponents, puck, accepted answer regions, detailed feedback overlays and initial/unselected screens are not yet drawn. This is a focused handoff-sequence storyboard, not a complete tactical scene or playable prototype.

Checks completed: rendered all four SVGs with CairoSVG and inspected all four final PNGs. Replaced an unsupported checkmark glyph with explicit Selected text after inspection. Answer cards are 54 px high and Confirm is 52 px high. Both sizes retain the same player responsibility sequence. The existing rink file was read and embedded, not edited. Interactive accessibility, actual touch behavior and coach validation were not run. At 320 px, answer labels use 12 px text to fit; increase type and allow wrapped cards in the next usability pass before implementation.

## Proposed S04 v2 continuation
The v2 source sequence now includes puck, opponents, both handoff decisions, feedback overlays, guided retry and both fresh-variation pauses at 320/390 px. See S04-V2-REVIEW.md for the exact review gate and verification status. These editable sources are not coach-approved. PNG rendering and visual inspection remain pending in the parent task; do not confuse source completion with visual verification.

