# MVP Design Document: Project "Polis-Grid"

## 1. Core Philosophy
- **Visuals**: Minimalist. 2D Grid with heatmap overlays. No avatars, no 3D.
- **Simulation**: Deterministic but chaotic. Systems interact to create emergent behavior.
- **Goal**: Survive political volatility. You don't "win"—you just try to keep the country from collapsing or being voted out.

## 2. The 4-Axis Political Model
To support >2 parties, every entity (Voter Bloc, Party, Policy) exists as a coordinate in 4-dimensional space.

### The Axes (Range: -1.0 to +1.0)
- **Economic**: Collectivism (-1.0) ↔ Market (+1.0)
  - Concerns: Taxes, welfare, privatisation, unions.
- **Societal**: Tradition (-1.0) ↔ Progress (+1.0)
  - Concerns: Religion, culture, minority rights, education content.
- **Authority**: Liberty (-1.0) ↔ Order (+1.0)
  - Concerns: Policing, surveillance, free speech, protest rights.
- **Diplomatic**: Sovereignty (-1.0) ↔ Globalism (+1.0)
  - Concerns: Borders, trade deals, foreign aid, military intervention.

Why this works: A party can be "Economically Left" but "Socially Conservative" (Old Labour), or "Economically Right" but "Authoritarian" (Nationalist). This allows for 5+ viable parties.

## 3. The Grid (World State)
The map is a fixed-size grid (e.g., 30×30).

### The Tile Data Structure
Each tile represents a Constituency or District.

- **Population Density**: (Rural/Urban/Metro).
- **Dominant Industry**: (Agri/Tech/Heavy Industry/Service).
- **Voter Blocs**: A list of population groups residing here.
  - Example: Tile [4,5] contains:
    - 40% Industrial Workers (Econ -0.8, Soc -0.2)
    - 20% Managers (Econ +0.5, Soc +0.4)
    - 40% Retirees (Econ -0.1, Soc -0.8)
- **The "Mood" (Calculated per tick)**:
  - **Stability**: How likely is a riot? (0-100)
  - **Turnout**: How likely are they to vote? (Derived from anger + hope).
  - **Local Issue**: The one axis they care about most right now (e.g., due to a scandal, "Authority" becomes the #1 priority).

## 4. The Actors
### A. The Player (The Government)
You do not play as a party; you play as the State Apparatus.

- You set Policies (Global modifiers).
- You allocate Budget (Local modifiers).
- Constraint: You have a "Political Capital" resource. You cannot change everything at once.

### B. The Parties (AI Controlled)
Parties are dynamic. They are not hard-coded.

- **Spawning**: If a cluster of tiles drifts to coordinate [0.8, -0.8, 0.5, 0.5] and no party represents them, a new "Populist Right" party automatically spawns.
- **Drift**: Parties move their coordinates towards where the votes are (opportunism) OR stay rigid (ideological purity).

## 5. The Gameplay Loop (The MVP Flow)
### Phase 1: The Setup
- Generate Grid (Perlin noise for density/industry).
- Seed Initial Parties (Start with 3: SocDem, Conservative, Liberal).

### Phase 2: The Cycle (Monthly Ticks)
- **Event Card**: A random scenario hits (e.g., "Market Crash").
  - Effect: Shifts voter coordinates globally (e.g., everyone moves -0.2 on Econ axis).
- **Player Action**:
  - Pass/Repeal Law.
  - Adjust Budget Sliders (Health, Military, Infrastructure).
- **Simulation Resolve**:
  - Calculate new Happiness per Bloc.
  - Update Tile Stability.
  - Check for "Snap Events" (Protests, Strikes).

### Phase 3: The Election (Every 48 Ticks)
- **Campaigning**: Parties get temporary buffs in specific regions.
- **Voting**:
  - Every Bloc in every Tile calculates "Distance" to each Party on the 4-axis graph.
  - Vote goes to the closest Party (weighted by Turnout).
- **Result**: Parliament seats allocated.
- **Win Condition**: Your preferred party (or coalition) maintains control.
- **Lose Condition**: A Hostile Party takes power and starts undoing your laws.

## 6. Interface (Minimal GUI)
- **Main View**: The Heatmap Grid.
  - Toggle 1: Economy View (Rich/Poor).
  - Toggle 2: Stability View (Peace/Riot).
  - Toggle 3: Party Stronghold (Colour coded by leading party).
- **Sidebar**:
  - Current Polls (Pie Chart).
  - Budget Balance.
  - Active Policies List.
- **Bottom Bar**:
  - Speed Controls (Pause, Play, Fast Forward).
  - "Next Turn" button.

## 7. MVP Constraints (What is NOT in V1)
- No individual politicians (no portraits/names).
- No foreign relations screen (abstracted to one axis).
- No complex legislative mini-games (laws pass instantly if you have capital).
- No multiplayer.

Why this fits your "Topica" goal: It removes the fluff. The drama comes from watching a "Blue" district slowly turn "Red" because you ignored their economic decline, or seeing a "Green" libertarian party suddenly surge because you made policing too strict.
