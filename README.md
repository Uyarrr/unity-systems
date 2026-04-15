# Unity Multiplayer Gameplay Systems

Selected gameplay and networking systems from a multiplayer party game built with Unity and Mirror.

Focus:
- modular minigame architecture
- multiplayer player state & authority
- server-authoritative gameplay

## Project Overview
### Character Selection (Multiplayer Lobby)
![character-selection](https://github.com/user-attachments/assets/157177df-fbc4-472e-bb83-ab61fb74c9ab)
<img width="721" height="405" alt="image" src="https://github.com/user-attachments/assets/635fd879-7ab9-4305-9bcf-5dc4042a7bab" />

### Level Selection (Playlist System)
![level-selection](https://github.com/user-attachments/assets/af7ae210-7ecd-4b9c-9cba-61e654108b05)

### Minigames (AI + Player Interaction)
![castle-fiasco](https://github.com/user-attachments/assets/26d1efa6-6fda-4c2a-9b98-0e707c1b0dd1)
![surge-sphere](https://github.com/user-attachments/assets/ef6607c8-153d-4b51-905e-ebe497d78f8a)
![bomb](https://github.com/user-attachments/assets/cbc498f5-093f-4ba3-b3c0-3556ffe18228)

## Key Systems

- **MinigameBase**  
  Core architecture for all minigames (round flow, player lifecycle, ranking)

- **MinigamePlayerNetwork**  
  Handles player initialization, authority, and input enabling

- **PropertiesBase**  
  Shared networked gameplay state (health, stun, invulnerability, alive state)

- **PlayerState**  
  Persistent multiplayer state (lobby, selection, readiness, scoring)

- **SurgeSphereGameManager**  
  Example minigame implementation (spawning, death flow, round logic)

- **CastleFiascoCPU**  
  Server-side AI using a state machine and decision logic for specific minigame

---
## Tech
- Unity
- C#
- Mirror Networking
---

## Notes
These scripts are extracted from a larger project and shared as a focused systems showcase rather than a full game.
