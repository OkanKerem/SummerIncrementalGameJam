# Parallel Universes — Idle Clicker

A jam-scoped idle/clicker game built in Unity 6 (2D URP). Create universes, harvest stardust, grow stars and planets, manage entropy, and collapse into black holes to earn Universe DNA for permanent upgrades.

## How to Play

1. Open the project in **Unity 6000.0.57f1** (or compatible Unity 6).
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press **Play**.
4. Click **Big Bang** to start your universe.
5. **Click stars** to collect stardust (ages stars faster — risk/reward).
6. **Create Star** / **Create Planet** to grow production.
7. Watch **Entropy** rise; slow it with stardust or let it reach 100% for collapse.
8. **Collapse Universe** manually for DNA (early collapse = penalty).
9. Spend **Universe DNA** on upgrades, then pick a **parallel universe variant** for the next run.

### Controls

- **Left-click** star: harvest stardust
- **Right-drag**: pan camera
- **Scroll wheel**: zoom

## Optional Editor Setup

For persistent ScriptableObject assets and prefabs in the project (instead of runtime generation):

**Universes → Setup Game (Full)** in the Unity menu bar.

This creates `Assets/Scenes/Game.unity`, balance assets, upgrade definitions, variants, and prefabs.

## Architecture

- **Run state** (resets each collapse): stardust, stars, planets, entropy, run stats
- **Prestige state** (persists): Universe DNA, upgrade levels, chosen variant
- **Save file**: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/universes/universes_save.json`

## Core Loop

```
Big Bang → Click/Harvest → Grow Stars & Planets → Manage Entropy → Collapse → DNA → Upgrades → Pick Variant → Repeat
```

*"Every universe dies, but its best traits live on in the next one."*
