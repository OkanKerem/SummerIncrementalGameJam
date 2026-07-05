# Parallel Universes — Prototype

Minimal clicker prototype: click a star, earn Stardust, age the star, trigger a Supernova, create a new star.

## Prototype step 2 — passive + upgrades

- **Passive:** star produces Stardust every second (Yellow +1, Orange +3, Red Giant +8) + upgrade bonus
- Each passive tick also **ages the star** (+1 base, reduced by Star Stability)
- **Upgrades** (persist across new stars): Click Power, Passive Production, Star Stability, Supernova Bonus

### Existing scene

Run **Universes → Add Prototype Step 2 UI To Open Scene** to add the upgrade panel without rebuilding.

### Setup (Editor)

Use the Unity menu — everything is created **in the scene** so you can edit it in the Hierarchy/Inspector:

| Menu | What it does |
|------|----------------|
| **Universes → Create Prototype Star Prefab** | Creates `Star.prefab` once (skipped if it already exists) |

| **Universes → Setup Prototype Scene** | New `PrototypeScene.unity` with all objects wired |
| **Universes → Setup Prototype In Open Scene** | Adds prototype objects to the scene you have open |

Edit **`Assets/Prefabs/Prototype/Star.prefab`** for glow size, sprites, collider, pop settings — runtime code reads those values and does not overwrite them.

Scene setup creates:

- `Main Camera` — orthographic, dark space background
- `WorldRoot` — parent for spawned stars
- `PrototypeGame` — `PrototypeGameController` (star prefab + world root assigned)
- `Canvas` — HUD with stardust, stage, feedback, Create New Star button
- `EventSystem`
- `Assets/Prefabs/Prototype/Star.prefab` — star sprite + collider

After setup, tweak positions, colors, costs, etc. in the Inspector, then press **Play**.

## Play loop

1. Click the star → Stardust + age
2. Star changes color: Yellow → Orange → Red Giant
3. Age 100 → Supernova (+50 bonus), star destroyed
4. **Create New Star** (20 Stardust) → new yellow star

| Age | Stage | Click reward |
|-----|-------|--------------|
| 0–33 | Yellow | +1 |
| 34–66 | Orange | +3 |
| 67–99 | Red Giant | +8 |
| 100 | Supernova | +50 bonus |

## Full game

The full incremental game is under `Assets/Scripts/` — use **Universes → Setup Game (Full)** for that.
