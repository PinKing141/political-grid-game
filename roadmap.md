# Master Roadmap: PolisGrid

Working inspiration: simulation-first political sandbox with strong emergent behavior (Tropico-style pressure loops, but focused on parliamentary ideology dynamics).

---

## Phase 1: The Ant Farm (Simulation Foundation)

Goal: The world exists, breathes, and runs without player input. It is a valid simulation of demographic shifts.

- [x] Core Data Structures: `PoliticalCompass`, `VoterBloc`, `DistrictTile`
- [x] Grid Visualisation: 30x30 heatmap with color lerping
- [x] Time System: turn loop with Pause / Play / Fast controls
- [ ] Data Overlays (Lens):
  - Stability (Green-Red)
  - Wealth (Gold-Grey)
  - Population Density (White-Black)
  - Dominant Ideology (4-color blend based on compass)
- [ ] Drift Mechanic (Neighbor Diffusion):
  - [x] Tiles influence neighbors each tick
  - Milestone: run 5 minutes and observe organic migration patterns

---

## Phase 2: The Leviathan (Player Tools)

Goal: The player can poke the simulation and see it react.

- [ ] Global Variables:
  - Treasury
  - PoliticalCapital
  - NationalUnrest
- [ ] Policy Engine:
  - `Policy` class (Name, Cost, Effects)
  - flexible modifier dictionary
  - example: Wealth Tax -> WealthIndex -0.1 (rich blocs), Treasury +500M
- [ ] Budget Sliders:
  - Services / Military / Infrastructure
  - high Military -> high Authority drift, lower Wealth
- [ ] Click Interaction:
  - select a tile and view local pop + bloc breakdown

---

## Phase 3: The Arena (Parties & Elections)

Goal: You are no longer god; you have opponents.

- [ ] Party Spawning Logic:
  - scan every X ticks
  - K-means clustering for unrepresented ideological clumps
  - spawn dynamic parties
- [ ] Voting Algorithm:
  - Euclidean ideological distance
  - turnout weighting behavior
- [ ] Election Cycle (48 months):
  - campaign phase buffs
  - convert tile votes to parliament seats
  - win/loss or coalition mode

---

## Phase 4: The Chaos (Emergent Gameplay)

Goal: Unintended consequences and cascading systems.

- [ ] Event System:
  - random events (earthquake, scandal)
  - conditional events (e.g., unrest > 80% -> general strike)
- [ ] Media Layer:
  - per-tile MediaReach
  - high media = faster drift; low media = echo chambers
- [ ] Protest Logic:
  - 0 stability -> riot object
  - riots damage economy + spread unrest to neighbors
  - player response via police deployment (budget tradeoff)

---

## Phase 5: To The Max (Deep Sim Features)

Goal: Hardcore simulation depth and replayability.

- [ ] Save/Load:
  - serialize entire grid state to JSON
- [ ] Modding Support:
  - policies/events from external `.json`
- [ ] Polling Fog of War:
  - [x] estimates by default with margin-of-error projection
  - accuracy boosted by census spending (pending)
- [ ] Gerrymandering:
  - redraw district borders
  - abuse raises unrest and harms legitimacy

---

## Technical Architecture Roadmap (Godot)

1. Project setup (folders, source control)
2. Core library (pure C# logic where possible)
3. Simulation controller (Godot node stepping logic)
4. UI controller (visualization and HUD)
5. Input controller (clicks, drags, speed controls)

---

## Current Snapshot

Phase 1 progress is approximately 90% complete:

- [x] Core data structures
- [x] Grid generation (Perlin-driven density/industry)
- [x] Heatmap visualisation (Stability / Turnout / Party)
- [x] Time flow controls (Pause/Play/Fast + Next Turn)
- [x] Dynamic party drift/spawn and election resolution
- [ ] Neighbor diffusion (cellular bleed)
- [x] Neighbor diffusion (cellular bleed)

---

## Immediate Next Steps (Vertical Slice)

Priority is Phase 2 (Policies) to transition from simulation viewer to game loop.

1. Define `Policy` class
2. Add a policy UI panel with 3 toggles:
   - Universal Healthcare
   - Strict Policing
   - Open Borders
3. Apply policy effects inside tile simulation (`DistrictTile.ProcessTurn` inputs via manager)

Success condition: toggling policy visibly shifts stability/turnout/party balance within a few turns.
