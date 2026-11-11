# S04 v2 proposed coaching-review storyboard

**Historical storyboard:** The current playable exchange uses the corrected assignment in [../BOX-PLUS-ONE-MODEL.md](../BOX-PLUS-ONE-MODEL.md): RD contains the carrier and LD covers Red C near the net. The older second-read containment proposal below is not the current gameplay rule.

## Deliverable

18 editable phone SVGs: nine states at both 320 px and 390 px. Two review sheets collect the full sequence. All tactical positions and suggested feedback are PROPOSED, NOT APPROVED, for David's coaching review.

Sequence: first-pause read, first-pause selected, first feedback, second-pause read, second-pause selected, second feedback, guided retry, fresh first pause, fresh second pause. The two fresh screens form one unassisted variation, not two separately rewarded attempts.

The source rink is unchanged. Its SHA-256 remains 71325E65B0AE38F14310463540E756ABB66ACA15AAD53A3AA3257C2C0481AC7A. The generated scenes embed its geometry and add separate player, partner, opponent, puck and protected-space layers. Left remains the defending end. Both a full-rink context strip and a labeled zone detail appear on each screen.

Typography: 17 px choices, 16–17 px body text, 25 px headings. Answer cards are 66 px tall and Confirm is 56 px. Selection uses an outline and explicit SELECTED text. Confirm is visibly disabled before selection. The controlled player has a ring and Y label; opponents are squares; partner is P; puck is black with a white edge.

## Review gate

David must confirm actor positions, safe reach, net-front responsibility, the point when P is set, the containment handoff, and accepted answers. Proposed feedback is deliberately not presented as validated coaching instruction. Mastery requires both decisions correct in a fresh unassisted exchange. Guided retries preserve progress. Five tokens are available only once per skill.

## Verification status

- Passed: generator ran; all 18 frame SVGs and two review sheets were created; source rink hash unchanged.
- Not yet run: successful PNG render and visual inspection. Parent task is arranging cloud rendering of the uploaded source ZIP.
- Not run: coach geometry validation, live touch behavior, interactive accessibility, gameplay tests. This is static visual design, not a game or deployment.
- Local rendering limits: Windows python alias failed; bundled Python lacks CairoSVG; a renderer-package download was denied by network policy and was not retried. Available browser entry points reported unavailable; headless attempts did not create PNGs.

## Files

Generator: create-s04-v2.cjs
Frames: s04-v2-{320|390}-{p1-read|p1-selected|p1-feedback|p2-read|p2-selected|p2-feedback|guided-retry|fresh-challenge|fresh-handoff}.svg
Sheets: s04-v2-{320|390}-review-sheet.svg
Transfer: s04-v2-review-sources.zip, Library identity libfile_174142bd3800819199888bbd4a51448f, version 0. Library upload succeeded; Windows extended-attribute persistence was unavailable.
