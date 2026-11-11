# Shift Happens! project instructions

## Purpose

Build a hockey-learning game that feels like playing real hockey, with controlled teaching moments that improve players’ positioning, awareness and decisions. The initial audience is 12U players.

The long-term foundation is realistic 5-on-5 hockey: five skaters and a goalie per team, a moving puck, reactive teammates and opponents, and meaningful consequences for the player’s choices.

## Product direction

- Unity is the primary game. Keep the browser prototype as a reference and a convenient coaching-review tool. Avoid maintaining two complete gameplay systems.
- Prioritize hockey accuracy, game mechanics and responsive controls before aesthetics.
- Start with local single-player, an elevated rink view and one controlled defenseman. Expand roles and features after the core experience works.
- Default to Guided mode with a few meaningful automatic teaching pauses. Provide a Freer mode without automatic pauses.
- Teaching should support the action rather than turn the game into a frequent multiple-choice quiz.

## Hockey and coaching standards

- Use the supplied Box Plus One Defense video, coaching references and team materials as foundational sources.
- Team-approved 12U Box + 1 responsibilities govern coaching and scoring. Do not assume every coach uses the same interpretation.
- Evaluate responsibilities, spacing, passing lanes, timing, awareness and recovery. Distinguish controlled possession from merely touching a contested puck.
- A goal against does not automatically mean the player made a bad decision; a save does not automatically mean the decision was correct.
- Label proposed positioning, coaching thresholds and feedback as draft until David or an authorized coach validates them. Incorporate Brian’s feedback when available.
- Keep player-facing feedback short, specific, age-appropriate and actionable. Explain the cue, decision and consequence.

## Immediate milestone

Create one complete, repeatable defensive-zone shift before expanding the game. Begin with a one-page blueprint and rink diagram showing:

1. Starting positions and each player’s responsibility.
2. What the controlled defenseman should notice.
3. When the returning partner has established coverage.
4. Why leaving net front too early creates danger.
5. When the defender should move to contain.
6. How both decisions will be assessed and tested.

Then inspect and reuse the existing Unity work. Build the sequence so the player’s actions genuinely affect lanes and outcomes. Script attacking intent where useful, not a guaranteed success or failure.

## Engineering and validation

- Inspect the current implementation and repository instructions before making changes. Preserve existing code, assets, documentation and useful prototypes. Avoid unnecessary rewrites.
- Separate hockey simulation, team behavior, player input, lesson triggers and presentation. Make scenarios and coaching criteria editable where practical.
- Use repeatable setups for testing and retries. Keep pause, resume, restart and replay consistent with actual game state.
- Make each gameplay playtest viewable on David's Unity Game screen. Provide a repeatable live replay of the reported scenario, including the setup and scripted inputs, and leave the run ready to watch again.
- Test puck ownership, passes, loose pucks, shots, role handoffs, teaching triggers and retries. Verify that choices affect outcomes and feedback matches what happened.
- Test mirrored and unfamiliar variations to distinguish learning from memorization.
- Validate desktop and phone separately. Editor testing alone does not establish standalone or mobile readiness.
- Treat local folders, cloud files and published previews as separate copies until their relationship is verified.
- Report what changed, what was tested, what remains unverified and the next recommended step. Do not describe a feature as deployed or coach-approved without evidence.
- Preserve a working version before major changes. Do not publish merely because a local build passes.

## Working style

Make sensible routine decisions and keep progress moving. Ask when a consequential product decision, unclear hockey rule or required permission blocks the next step. Prefer diagrams and playable examples over lengthy explanations. Surface important blockers promptly.

Defer multiplayer, seasons, elaborate rewards, customization and advanced visual polish until the core defensive shift is accurate, understandable and enjoyable.
