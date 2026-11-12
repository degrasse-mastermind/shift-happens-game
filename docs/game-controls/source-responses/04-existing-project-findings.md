I’ve now checked the actual Unity project, and there’s good news: **we already have the beginnings of this control system.** We should extend it, not start over.

The game already stores starting positions, player roles, mirrored setups, partner return speed and delay, coverage thresholds, containment criteria and shift duration. It has four practice presets: Original, Marked, Behind net and Behind net delayed exit. Each run gets its own copy of the settings, which helps protect the preset while you play.

Other behavior is still embedded in the code, including the controlled defenseman’s movement speed, pass and shot speeds, tie-up timing and parts of the behind-net route. The simulation, coaching evaluation and visual controls are also partly combined. That’s normal at this prototype stage, but we should separate them gradually as the game grows.

**One important technical guardrail:** the current simulation uses its own rink coordinates, with a non-uniform conversion to the displayed rink. We must not label its distances as feet or assume a single conversion. I’ll keep the current behavior and Review Mode mapping intact while the controls are developed.