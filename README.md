# Match-3 

This document describes the refactored architecture and the implemented features of the Match‑3 technical test.  
The goal was not only to meet the assignment requirements, but also to reorganize the project into a cleaner, more maintainable structure that feels closer to real production code.

---

## 🎯 High‑Level Summary

The original monolithic logic has been restructured into a **service‑based architecture**, where each gameplay responsibility is isolated into its own component.  
This makes the project easier to understand, modify, and extend in the future.

New services introduced:

- **InputService** — handles swipe detection, move validation, and swap logic  
- **BoardRefillService** — cascades, gravity, refill, staggered spawning  
- **ScoreService** — unified scoring API  
- **MatchResolver** — match resolution, bomb creation, destruction workflow  
- **BombService** — bomb queueing, explosion rules, delayed detonations  

Each service has a clear responsibility, significantly improving the readability and SOLID compliance of the project.

---

## 🧩 Implemented Features (from assignment requirements)

### ✔ Cascading Logic  
Gems fall individually with natural timing using `cascadeStepDelay`.  
The falling mechanic now looks smooth and predictable.

### ✔ Prevention of Accidental Matches  
Refill logic ensures that newly spawned gems do **not** create instant matches unless absolutely unavoidable.

### ✔ Object Pooling  
A dedicated `SC_GemPool` replaces Instantiate/Destroy during gameplay.  
Supports multiple gem prefabs and eliminates allocation spikes during cascades.

### ✔ Bomb Piece  
- Bombs are created **only** from matches of 4 or more gems  
- Bombs inherit the correct color (`baseType`)  
- Explosion affects:  
  - all 8 neighbors (orthogonal + diagonal)  
  - cross cells at distance 2  
- Explosion sequence follows the required timing:
  1. Destroy neighbors  
  2. Destroy bomb  
  3. Trigger cascade

### ✔ Staggered Gem Refill  
New gems spawn with a stagger using `spawnStaggerDelay`, creating a clean chain‑drop animation similar to commercial Match‑3 games.

---

## 🏗 Architecture Overview

### **SC_GameLogic (Coordinator)**
No longer contains gameplay logic.  
Now acts as a centralized organizer that initializes services and routes key events.

### **InputService**
Extracted from `SC_Gem`, now responsible for:
- detecting swipe direction  
- selecting neighboring gem  
- performing swap  
- validating moves  
- rolling back invalid swaps  

`SC_Gem` became a simple visual/state object with no gameplay logic inside.

### **MatchResolver**
Handles:
- match resolution  
- bomb creation  
- delegating explosion logic to BombService  
- coordinating post-destruction cascades

### **BombService**
Responsible for:
- collecting bombs during match resolution  
- applying the correct explosion pattern  
- executing delayed chain‑explosions

### **BoardRefillService**
Handles the entire board update cycle:
- gravity  
- cascade  
- refill  
- anti‑match spawn logic  
- cleanup of misplaced gem objects

### **ScoreService**
Provides a single place for all scoring updates, simplifying future expansion (combo chains, multipliers, streak bonuses).

---

## 🧹 Additional Improvements
- Added null‑safe match detection  
- Removed or replaced legacy code paths  
- Unified gem spawning through a single API  
- Cleaned up cascade logic for readability and predictability  
- Prepared the project for further extensibility

---

## ✅ Final Result

The project now behaves smoothly and predictably, meets all assignment requirements, and uses a clean, modular architecture.  
This refactored version is easier to work with, easier to debug, and far more suitable for real production feature development.

