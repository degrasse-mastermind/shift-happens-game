## What we should build

I’d organize Shift Happens into six connected systems:

1. **Rink and movement:** ice dimensions, skating, turning, puck movement and contact.
2. **Hockey state:** who has the puck, where everyone is, who owns net-front coverage and whether the puck is loose.
3. **Player intelligence:** what each skater recognizes, their responsibility and the action they choose.
4. **Scenario director:** starting positions, puck location, routes, variations and reset.
5. **Coaching and evaluation:** what the player should notice, when hints appear and how the decision is assessed.
6. **Presentation:** camera, characters, animations, interface and Review Mode.

The simulation should be the source of truth. The visuals show what the simulation decided; a character animation shouldn’t secretly determine possession or whether a read was correct.

## The controls you should eventually have

I’d start with a **visual coaching control panel**, not a spreadsheet of hundreds of numbers:

- **Set the scene:** drag players and puck into starting positions; choose their facing, roles and which side of the rink the play begins on.
- **Adjust movement:** skating speed, acceleration, stopping and turning; puck and pass speed.
- **Shape the hockey:** coverage areas, support distances, recognition delay and the conditions for holding position or moving to contain.
- **Control instruction:** hints on or off, when they appear, and what the review explains.
- **See the thinking:** toggle player roles, intended destinations, coverage areas and passing lanes.
- **Test safely:** pause, step through, reset the same setup, compare versions and revert to a saved preset.

The important distinction: **a slider tunes a behavior; it doesn’t define the hockey rule.** For example, a coverage-distance setting can help determine whether a teammate is in position, but we still need an explicit, coach-reviewed rule for *when coverage is established*.

## Example: the current defensive shift

Suppose the puck moves behind the net. The game tracks the puck, your defense partner’s position and who is responsible for net front. When the partner has established coverage, your player can move to contain. The review checks that decision separately from the shot result: a goalie save shouldn’t make an abandoned assignment a good read, and a goal shouldn’t automatically make a sound decision wrong.

That principle is already reflected in the current practice design. The newer behind-net rules and scoring are still **draft and need coaching review**. The full control panel described above is a proposal, not something I’ve built or verified in the game.

## How developers build this reliably

We’ll keep three things distinct: **rules** that define correct hockey, **settings** we can tune, and **live state** that changes moment by moment. Each adjustable setting should have a plain-English description, units, a safe range and a documented default. Values start as testable hypotheses, not claimed hockey truths.

We’ll also save named scenarios and presets, make changes reversible, and use repeatable tests. For example: a deflection must not automatically count as possession; mirroring a scenario must also mirror the assignments; switching off hints must not change the hockey itself. Coach review and player playtests then tell us whether technically correct behavior actually feels and teaches right.