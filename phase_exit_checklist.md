# PolisGrid Phase Exit Checklist

Use this checklist as the gate before moving to the next phase.

---

## Phase 1: The Living World (Simulation Foundation)

Goal: A self-running ant farm where the simulation works mathematically without player intervention.

### Exit Requirements (Must Have)

- [ ] Grid Generation: The 30x30 grid generates with coherent Perlin noise for Population Density and Industry (not just random noise).
- [ ] Voter Logic: Blocs (for example Farmers) exist in tiles and correctly calculate Happiness based on their Ideology vs. the Government.
- [ ] Dynamic Parties: Parties automatically spawn when a voter cluster is unrepresented and drift their ideology over time to chase votes.
- [ ] Election Loop: The game can run a full election cycle, allocate seats based on votes, and declare a winner without crashing.
- [ ] Visuals: The Heatmap correctly updates colors for Stability, Turnout, and Party Control in real-time.

### Dos

- Do rely heavily on GD.Print or a debug console to verify the math (for example: Why did the Socialists win?). If logs do not make sense, the game will not either.
- Do keep frame rate high. The simulation steps around 900 tiles per tick, so loops must stay efficient.
- Do use extreme colors for debugging (for example: bright magenta for error or 0 Stability) so issues are instantly visible.

### Donts

- Do not start building large Policy UI first. If the underlying simulation is not fun to watch, it will not be fun to play.
- Do not simulate individual citizens. Simulate blocs only.
- Do not hardcode party names permanently. Let spawner logic create them dynamically.

---

## Phase 2: The Hand of State (Player Agency)

Goal: The player can interact with the world and influence the simulation.

### Exit Requirements (Must Have)

- [ ] Policy Engine: A system to activate/deactivate Policy objects that applies modifiers to Voter Blocs or Tile stats.
- [ ] Budget Model: A global Treasury that tracks Income versus Expenses. Running out of money must have consequences (for example reduced Stability).
- [ ] Political Capital: A resource mechanic that limits how many actions a player can take per turn.
- [ ] The Lever: At least one mechanism (Budget slider or Law) that turns an unstable tile stable over time.

### Dos

- Do create trade-offs. Every policy must hurt someone.
- Do implement lag. Law effects should ramp over roughly 3 to 6 months, not apply instantly.
- Do visualize costs clearly (for example: Free Healthcare can bankrupt Treasury in N turns).

### Donts

- Do not allow player to change everything at once. Restrict actions via Capital.
- Do not make Budget only a score. It should function like a survival timer.

---

## Phase 3: The Microscope (Data and UI)

Goal: Provide information needed to understand simulation feedback.

### Exit Requirements (Must Have)

- [ ] Tile Inspector: Clicking any tile opens a window detailing who lives there and why they are angry or happy.
- [ ] National Dashboard: A centralized screen showing trends (line graphs) for Stability, GDP, and Polling over the last 50 turns.
- [ ] Fog of War (Polling): UI shows projected votes with margin of error, not exact future outcomes.
- [ ] Tooltip System: Hovering over variables explains what they do.

### Dos

- Do focus on the why. Inspector should show contribution sources, not only final percentages.
- Do keep color semantics consistent (green good/high, red bad/low).

### Donts

- Do not overwhelm the main map. Keep complex numbers inside inspector/dashboard layers.
- Do not distort past data. Only future predictions should include uncertainty.

---

## Phase 4: The Chaos (Emergent Systems)

Goal: Introduce external pressures and chain reactions.

### Exit Requirements (Must Have)

- [ ] Media System: Tiles have Media Exposure values that affect drift speed and scandal penetration.
- [ ] Crisis Chains: Events become multi-stage scenarios (for example Market Crash -> Unemployment -> Crime Spike).
- [ ] Gerrymandering (optional but recommended): Mechanic to redraw district lines or influence seat distribution.
- [ ] Fail States: Clear game-over triggers (for example Revolution, Coup, Bankruptcy).

### Dos

- Do allow death spirals if crises are ignored.
- Do give high-cost bailout options.

### Donts

- Do not make game-over random. Defeat should trace to player decisions.
- Do not over-spawn crises. Preserve pacing with quiet recovery years.

---

## Phase 5: The Polish and Meta

Goal: Make the game distributable, moddable, and replayable.

### Exit Requirements (Must Have)

- [ ] Save and Load: Entire DistrictTile grid and simulation state serialize to JSON and reload correctly.
- [ ] External Config: Policies, Events, and Parties load from json, not hardcoded C#.
- [ ] Scenarios: At least three starting setups (for example Depression, Boom, Divided Nation).
- [ ] UX Polish: Readable fonts, hover states, and responsive UI scaling.

### Dos

- Do expose as much balance data as possible to json.
- Do run long hands-off playtests (for example 100 years on high speed) and verify no simulation collapse into uniformity.

### Donts

- Do not ship with debug cheats enabled.
- Do not postpone save-system hardening to the very end.

---

## Current Snapshot (as of now)

- Phase 1: Complete foundation. Core simulation, elections, dynamic parties, diffusion, and overlays are implemented.
- Phase 2: Substantially complete. Policy engine, ramping, treasury/capital constraints, and policy trade-offs are in place.
- Phase 3: Mostly complete. Tile inspector, dashboard trends, polling fog with margin of error, and tooltips are implemented.
- Phase 4: Early hooks present. Event system is active, and fail states (Revolution/Bankruptcy/Election loss) now trigger game-over.
- Phase 5: Not started at full scope (save/load, external config, scenarios pending).
