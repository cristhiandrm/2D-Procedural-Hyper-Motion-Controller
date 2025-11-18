# 2D Procedural Hyper-Motion Controller

![Unity](https://img.shields.io/badge/Unity-2022.3%2B-000000?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp)
![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)

**A technical demonstration of procedural character animation and advanced "game feel" mechanics in Unity 2D.** This project abandons traditional sprite-sheet animation in favor of code-driven movement, allowing for fluid, responsive, and physics-reactive character behaviors.

> **[INSERT GIF OF WALKING AND JUMPING HERE]**

## 🌟 Key Features

### 🦾 Procedural Animation Engine
Instead of pre-rendered frames, the character's limbs are animated in real-time using trigonometric functions and dampening algorithms.
- **Puppet System:** Decouples the physics collider (`Rigidbody2D`) from the visual representation, allowing for drag, overshoot, and follow-through effects.
- **Elliptical Walk Cycle:** Legs and arms move using calculated Sine/Cosine waves for organic motion.
- **Dynamic Articulation:** Multi-jointed arms (Upper/Lower) with simulated elbow flexion based on movement velocity.

### 💥 "Juicy" Physics Feedback
- **Squash & Stretch:** The character compresses upon impact and stretches during high-velocity vertical movement.
- **Dynamic Sorting:** Limb rendering order automatically updates based on the character's facing direction (Left/Right) to maintain depth perspective.
- **Procedural Combat:** Attack animations are generated via Coroutines using `Mathf.LerpAngle` to create snappy "Anticipation -> Strike -> Recovery" curves without animation clips.

### 🧣 Verlet Integration Cape
A custom physics solution for the character's cape, avoiding Unity's heavy Cloth system.
- **Verlet Integration:** Simulates inertia and gravity using point-based history.
- **Collision Handling:** Custom algorithm detects environment colliders and pushes cape segments out of geometry in real-time.
- **Wind Simulation:** Reacts to player velocity to simulate drag and air resistance.

## 🛠️ Technical Implementation

### The "Marionette" Pattern
The core logic resides in `ProceduralAnimator.cs`. The visual body parts are children of the physics root but move independently using `Vector3.SmoothDamp`. This creates a natural "lag" where limbs trail behind the body's movement.

### Limb Logic
```csharp
// Example of the organic arm rotation logic
float lowerArmSmoothTime = isAttacking ? (followSmoothTime * 0.5f) : followSmoothTime;
currentLowerArmAngle = Mathf.SmoothDampAngle(current, target, ref velocity, lowerArmSmoothTime);
