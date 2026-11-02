# Echoes of Kur — Game Technical Specification

> **Company:** Aeterna Draconis Studios
> **Document Version:** 1.0
> **Generated:** 2026-10-20
> **Engine:** Unity 6000.6.3f1 (Unity 6)
> **Render Pipeline:** Universal Render Pipeline (URP) 17.6.0

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Project Architecture](#2-project-architecture)
3. [Folder Hierarchy](#3-folder-hierarchy)
4. [Package Dependencies](#4-package-dependencies)
5. [Networking Architecture](#5-networking-architecture)
6. [Controls & Input System](#6-controls--input-system)
7. [Player Systems](#7-player-systems)
8. [Camera System](#8-camera-system)
9. [UI System](#9-ui-system)
10. [Audio System](#10-audio-system)
11. [Class Hierarchy & Relationships](#11-class-hierarchy--relationships)
12. [Core C# Signatures](#12-core-c-signatures)
13. [Prefab Specifications](#13-prefab-specifications)
14. [Build & PlayMode Configuration](#14-build--playmode-configuration)

---

## 1. Project Overview

**Echoes of Kur** is a multiplayer game built in **Unity 6** using a **server-authoritative networking** model powered by **Netcode for GameObjects** and **Unity Gaming Services (UGS)**. The game features an **isometric camera perspective**, online sessions via **Relay**, and a lobby system with ready-up mechanics. The codebase is in early/mid development, with core movement, networking, and lobby UI infrastructure in place.

### Key Technical Pillars

| Pillar | Technology |
|---|---|
| Engine | Unity 6 (6000.6.3f1) |
| Rendering | URP 17.6.0 (PC + Mobile profiles) |
| Networking | Netcode for GameObjects 2.13.3 |
| Multiplayer Services | UGS Multiplayer 2.3.3 (Relay) |
| Input | Unity Input System 1.20.0 |
| Audio | FMOD Studio (plugin) |
| UI Framework | UI Toolkit (`PanelRenderer`, `VisualElement`) |
| Navigation | AI Navigation 2.0.14 |
| AI Inference | Unity AI Inference 2.6.1 |

---

## 2. Project Architecture

### 2.1 Design Patterns

| Pattern | Usage |
|---|---|
| **Singleton** | `NetworkSessionManager.Instance`, `UGSInitializer.Instance` — persistent across scenes via `DontDestroyOnLoad` |
| **Server-Authoritative Movement** | Client writes intent to `NetworkVariable`; server reads it and executes `CharacterController.Move()` |
| **Component Composition** | Player prefab composes `NetworkObject`, `NetworkTransform`, `NetworkPlayerController`, `PlayerInputHandler`, `PlayerMovement`, `CharacterController`, `PlayerInput`, `NetworkPlayerDebug` |
| **Event-Driven UI** | `NetworkSessionManager` fires `LobbyChanged` / `SessionEnded` events; `LobbyUI` subscribes and refreshes |
| **Connection Approval** | `NetworkPlayerSpawner` hooks into `NetworkManager.ConnectionApprovalCallback` for spawn-point assignment |

### 2.2 Networking Model

```
┌──────────────┐         NetworkVariable<Vector3>         ┌──────────────┐
│  Owner Client │ ─── writes movementDirection ──────────▶ │    Server    │
│  (Input)      │                                          │  (Authority) │
│               │ ◀── NetworkTransform replication ─────── │  .Move()     │
└──────────────┘                                           └──────────────┘
```

- **Authority model:** Server-authoritative (`AuthorityMode: 0` on `NetworkTransform`).
- **Transport:** Unity Relay (via `SessionOptions.WithRelayNetwork()`).
- **Connection flow:** UGS Authentication (anonymous) → Session Create/Join → Connection Approval → Player Spawn.

### 2.3 Execution Flow

1. **`UGSInitializer.Awake()`** — Initializes Unity Services, signs in anonymously.
2. **`NetworkSessionManager`** — Manages session lifecycle (create / join / leave / ready).
3. **`LobbyUI`** — Presents Main Menu → Lobby transition; drives UI Toolkit panels.
4. **`NetworkPlayerSpawner.ApprovalCheck()`** — Approves connections, assigns spawn points.
5. **`NetworkPlayerController.OnNetworkSpawn()`** — Enables input (owner) or `CharacterController` (server).
6. **`IsometricCameraController`** — Finds local player's `NetworkObject` and follows it.

---

## 3. Folder Hierarchy

```
Assets/
├── Fonts/                          # TMP font assets (Aktura-Regular SDF)
├── Plugins/
│   └── FMOD/                       # FMOD Studio audio plugin
│       ├── addons/                  #   Haptics, ResonanceAudio
│       ├── platforms/               #   Per-platform native bindings
│       ├── src/                     #   Runtime + Editor scripts
│       ├── Cache/                   #   Editor bank cache
│       └── Resources/              #   FMODStudioSettings
├── Scenes/
│   └── GameStart.unity             # Main/only scene
├── Scripts/
│   ├── Camera/
│   │   └── IsometricCameraController.cs
│   ├── Network/
│   │   ├── Debug/
│   │   │   └── NetworkSessionDebugUI.cs
│   │   ├── NetworkPlayerController.cs
│   │   ├── NetworkPlayerSpawner.cs
│   │   ├── NetworkSessionManager.cs
│   │   └── UGSInitializer.cs
│   ├── Player/
│   │   ├── NetworkPlayerDebug.cs
│   │   ├── Player.prefab
│   │   ├── PlayerInputHandler.cs
│   │   └── PlayerMovement.cs
│   └── UI/
│       ├── LobbyPlayerSlotUI.cs
│       └── LobbyUI.cs
├── Settings/
│   ├── Build Profiles/             # Windows-Test build profile
│   ├── PlayMode/                   # 2 Players, 4 Players multiplayer test
│   ├── DefaultVolumeProfile.asset
│   ├── Mobile_Renderer.asset       # URP Mobile renderer
│   ├── Mobile_RPAsset.asset        # URP Mobile pipeline asset
│   ├── PC_Renderer.asset           # URP PC renderer
│   ├── PC_RPAsset.asset            # URP PC pipeline asset
│   └── SampleSceneProfile.asset
├── TextMesh Pro/                   # TMP resources
├── UI/
│   └── Common/
│       └── LobbyPanelSettings.asset
├── VFXs/
│   └── Fireball/
│       └── Fireball.prefab
├── DefaultNetworkPrefabs.asset     # Netcode prefab registry
├── GameControls.inputactions       # Input Action asset
├── GameControlSettings.asset
└── Readme.asset
```

---

## 4. Package Dependencies

### Core Packages

| Package | Version | Purpose |
|---|---|---|
| `com.unity.netcode.gameobjects` | 2.13.3 | Netcode for GameObjects (NGO) |
| `com.unity.services.multiplayer` | 2.3.3 | UGS Multiplayer (Session, Relay) |
| `com.unity.inputsystem` | 1.20.0 | New Input System |
| `com.unity.render-pipelines.universal` | 17.6.0 | URP |
| `com.unity.multiplayer.playmode` | 3.0.0 | Editor multiplayer testing (MPPM) |
| `com.unity.ai.navigation` | 2.0.14 | NavMesh baking & agents |
| `com.unity.ai.inference` | 2.6.1 | Unity AI Inference runtime |
| `com.unity.timeline` | 6.6.0 | Timeline sequencing |
| `com.unity.ugui` | 2.6.0 | Legacy uGUI (likely for TMP compatibility) |
| `com.unity.visualscripting` | 1.9.12 | Visual Scripting |

### Third-Party Plugins

| Plugin | Purpose |
|---|---|
| **FMOD Studio** | Adaptive audio engine (with Haptics + ResonanceAudio addons) |

---

## 5. Networking Architecture

### 5.1 Service Initialization (`UGSInitializer`)

| Property | Type | Description |
|---|---|---|
| `Instance` | `static UGSInitializer` | Singleton; survives scene loads |
| `IsReady` | `bool` | `true` after UGS init + anonymous sign-in |
| `PlayerId` | `string` | UGS Authentication player ID |
| `CurrentPlayerName` | `string` | Authentication player name |

```csharp
// Core initialization flow
public async Task InitializeAsync();
public async Task<bool> SetPlayerNameAsync(string requestedName);
```

**Player name normalization:** spaces → `_`, max 50 characters, fallback `"Player"`.

### 5.2 Session Management (`NetworkSessionManager`)

| Property | Type | Description |
|---|---|---|
| `Instance` | `static NetworkSessionManager` | Singleton |
| `CurrentSession` | `ISession` | Active UGS session reference |
| `IsInSession` | `bool` | Whether currently in a session |
| `IsHost` | `bool` | Whether this client is the host |
| `JoinCode` | `string` | Session join code for sharing |
| `MaxPlayers` | `int` | Configured max (default: **4**) |
| `PlayerCount` | `int` | Current player count |

**Session Settings (Serialized Defaults):**

| Setting | Default Value |
|---|---|
| `maxPlayers` | 4 |
| `isPrivate` | `false` |
| `isLocked` | `false` |
| `sessionName` | `"OnlineGame"` |

**Events:**

| Event | Signature | Fired When |
|---|---|---|
| `LobbyChanged` | `Action` | Player joins/leaves/changes properties |
| `SessionEnded` | `Action` | Session is left or player is removed |

```csharp
public async Task<bool> CreateSessionAsync(string playerName);
public async Task<bool> JoinSessionAsync(string joinCode, string playerName);
public async Task LeaveSessionAsync();
public async Task<bool> SetReadyAsync(bool ready);
public List<LobbyPlayerData> GetLobbyPlayers();
public bool IsPlayerReady(IReadOnlyPlayer player);
```

**Player Properties (per-session):**

| Key | Type | Visibility | Description |
|---|---|---|---|
| `"playerName"` | `string` | `Member` | Display name in lobby |
| `"ready"` | `string` (`"true"`/`"false"`) | `Member` | Ready state |

### 5.3 Connection Approval (`NetworkPlayerSpawner`)

```csharp
private void ApprovalCheck(
    NetworkManager.ConnectionApprovalRequest request,
    NetworkManager.ConnectionApprovalResponse response);
```

- Always approves (`response.Approved = true`).
- Creates player object (`response.CreatePlayerObject = true`).
- Assigns spawn point by `clientId` index into a `Transform[]` array.
- Falls back to `Vector3.zero` if no spawn point is available.

### 5.4 Data Model

```csharp
[Serializable]
public class LobbyPlayerData
{
    public string PlayerId;
    public string DisplayName;
    public bool IsHost;
    public bool IsLocalPlayer;
    public bool IsReady;
}
```

---

## 6. Controls & Input System

### 6.1 Input Action Asset: `GameControls.inputactions`

The project uses Unity's **New Input System** (1.20.0) with a single asset defining two action maps.

#### Action Map: `Player`

| Action | Type | Control Type | Bindings |
|---|---|---|---|
| **Move** | Value | `Vector2` | WASD (Dpad composite), Arrow Keys, Gamepad Left Stick, XR Primary2DAxis, Joystick Stick |
| **Attack** | Button | — | Mouse Left Button, Gamepad West (X/□), Joystick Trigger, XR Primary Action |
| **Jump** | Button | — | Space, Gamepad South (A/✕), XR Secondary Button |
| **Interact** | Button (Hold) | — | E, Gamepad North (Y/△) |
| **Sprint** | Button | — | Left Shift, Gamepad Left Stick Press, XR Trigger |
| **Previous** | Button | — | 1, Gamepad D-Pad Left |
| **Next** | Button | — | 2, Gamepad D-Pad Right |

#### Action Map: `UI`

| Action | Type | Control Type |
|---|---|---|
| Navigate | PassThrough | `Vector2` |
| Submit | Button | `Button` |
| Cancel | Button | `Button` |
| Point | PassThrough | `Vector2` |
| Click | PassThrough | `Button` |
| RightClick | PassThrough | `Button` |
| MiddleClick | PassThrough | `Button` |
| ScrollWheel | PassThrough | `Vector2` |
| TrackedDevicePosition | PassThrough | `Vector3` |
| TrackedDeviceOrientation | PassThrough | `Quaternion` |

#### Control Schemes

| Scheme | Required Devices |
|---|---|
| Keyboard&Mouse | `<Keyboard>`, `<Mouse>` |
| Gamepad | `<Gamepad>` |
| Touch | `<Touchscreen>` |
| Joystick | `<Joystick>` |
| XR | `<XRController>` |

### 6.2 Input Processing Pipeline

```
PlayerInput (Unity Component)
    │
    │  Player/Move action fires
    ▼
PlayerInputHandler.OnMove(InputAction.CallbackContext)
    │
    │  Reads Vector2, forwards to controller
    ▼
NetworkPlayerController.SetMovementInput(Vector2)
    │
    │  Converts to camera-relative world direction
    │  Writes to NetworkVariable<Vector3> (owner-write)
    ▼
NetworkVariable<Vector3> movementDirection
    │
    │  Server reads in Update()
    ▼
PlayerMovement.MoveCharacter(Vector3)
    │
    │  CharacterController.Move() — server-authoritative
    ▼
NetworkTransform replicates position to all clients
```

**Camera-relative conversion** (in `NetworkPlayerController.SetMovementInput`):
```csharp
Vector3 cameraForward = playerCamera.transform.forward;
Vector3 cameraRight   = playerCamera.transform.right;
cameraForward.y = 0f;  // flatten to XZ plane
cameraRight.y   = 0f;
cameraForward.Normalize();
cameraRight.Normalize();
worldDirection = cameraForward * input.y + cameraRight * input.x;
```

The `PlayerInput` component on the Player prefab uses **Unity Events** notification behavior (`m_NotificationBehavior: 2`) and wires `Player/Move` directly to `PlayerInputHandler.OnMove`.

---

## 7. Player Systems

### 7.1 Player Movement (`PlayerMovement`)

| Parameter | Type | Default (Prefab) | Description |
|---|---|---|---|
| `moveSpeed` | `float` | **5** | Units per second |
| `rotationSpeed` | `float` | **5** | Slerp interpolation factor |
| `gravity` | `float` | **−9.81** | Downward acceleration |
| `groundedForce` | `float` | **−2** | Force applied when grounded |

```csharp
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public void MoveCharacter(Vector3 moveDirection);
}
```

**Movement logic:**
1. Clamp direction to unit magnitude.
2. Slerp rotation toward movement direction.
3. Apply gravity; snap `verticalVelocity` to `groundedForce` when grounded.
4. Final velocity = `(direction * moveSpeed) + (0, verticalVelocity, 0)`.
5. `CharacterController.Move(velocity * Time.deltaTime)`.

### 7.2 Network Player Controller (`NetworkPlayerController`)

```csharp
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterController))]
public class NetworkPlayerController : NetworkBehaviour
{
    // Owner-writable network variable for movement intent
    private NetworkVariable<Vector3> movementDirection;

    public void SetMovementInput(Vector2 input);
}
```

**Spawn behavior:**

| Condition | `PlayerInput` | `CharacterController` |
|---|---|---|
| Pre-spawn | ❌ Disabled | ❌ Disabled |
| Owner | ✅ Enabled | ❌ Disabled |
| Server (non-owner) | ❌ Disabled | ✅ Enabled |
| Server + Owner (Host) | ✅ Enabled | ✅ Enabled |
| Remote Client | ❌ Disabled | ❌ Disabled |

### 7.3 Network Player Debug (`NetworkPlayerDebug`)

Diagnostic `NetworkBehaviour` that logs spawn/despawn events with `NetworkObjectId`, `OwnerClientId`, ownership, and authority flags.

---

## 8. Camera System

### 8.1 Isometric Camera Controller

```csharp
public class IsometricCameraController : MonoBehaviour
{
    [SerializeField] private Vector3 followOffset = new Vector3(10f, 10f, -10f);
    [SerializeField] private float followSmoothTime = 0.12f;
}
```

| Parameter | Default | Description |
|---|---|---|
| `followOffset` | `(10, 10, −10)` | Camera position relative to target |
| `followSmoothTime` | `0.12` | `SmoothDamp` response time (seconds) |

**Behavior:**
1. **On Start:** Computes isometric rotation via `Quaternion.LookRotation(-followOffset.normalized, Vector3.up)`.
2. **On LateUpdate:** Uses `Vector3.SmoothDamp` to follow the local player's transform.
3. **Target acquisition:** Queries `NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()` to find the local player.
4. **Reconnection:** If the target becomes inactive, resets `target = null` to allow re-acquisition.

**Isometric angle** (from default offset `(10, 10, -10)`):
- Elevation: ~35.26° (true isometric ≈ 35.264°)
- Azimuth: 315° (looking from top-right toward bottom-left)

---

## 9. UI System

### 9.1 Architecture

The lobby UI uses **UI Toolkit** (not legacy uGUI). `LobbyUI` is a `MonoBehaviour` attached to a `GameObject` with a `PanelRenderer` component. The UI document is loaded and cached via `PanelRenderer.RegisterUIReloadCallback`.

### 9.2 Lobby UI (`LobbyUI`)

**Panels:**

| Panel | VisualElement Name | Description |
|---|---|---|
| Main Menu | `MainMenuPanel` | Player name input, join code input, Create/Join buttons |
| Lobby | `LobbyPanel` | Session info, player slots, Ready/Leave buttons |

**Named UI Elements:**

| Element | Type | Panel |
|---|---|---|
| `PlayerNameInput` | `TextField` | Main Menu |
| `JoinCodeInput` | `TextField` | Main Menu |
| `CreateButton` | `Button` | Main Menu |
| `JoinButton` | `Button` | Main Menu |
| `MainStatus` | `Label` | Main Menu |
| `SessionName` | `Label` | Lobby |
| `JoinCode` | `Label` | Lobby |
| `PlayerCount` | `Label` | Lobby |
| `LobbyStatus` | `Label` | Lobby |
| `ReadyButton` | `Button` | Lobby |
| `LeaveButton` | `Button` | Lobby |
| `PlayerSlot_1` … `PlayerSlot_4` | `VisualElement` | Lobby |

### 9.3 Lobby Player Slot (`LobbyPlayerSlotUI`)

A pure C# class (not a `MonoBehaviour`) that wraps a `VisualElement` subtree.

**Slot element hierarchy:**

| Element | Type | Description |
|---|---|---|
| `Avatar` | `VisualElement` | Player avatar image |
| `PlayerInfo` | `VisualElement` | Info container |
| `PlayerName` | `Label` | Player display name (bold if local player) |
| `PlayerRole` | `Label` | `"HOST"` or `"JOGADOR"` |
| `ReadyIndicator` | `VisualElement` | Ready status container |
| `ReadyActive` | `VisualElement` | Shown when player is ready |
| `ReadyInactive` | `VisualElement` | Shown when player is not ready |

```csharp
public sealed class LobbyPlayerSlotUI
{
    public LobbyPlayerSlotUI(VisualElement rootElement);
    public void SetPlayer(LobbyPlayerData player);
    public void SetEmpty();
}
```

### 9.4 Debug UI (`NetworkSessionDebugUI`)

An IMGUI-based debug overlay (`OnGUI`) for quick testing of session create/join/leave without the full UI Toolkit interface.

---

## 10. Audio System

The project uses **FMOD Studio** as its audio middleware. The FMOD plugin includes:

| Module | Description |
|---|---|
| `FMOD/src/RuntimeManager.cs` | Core FMOD runtime management |
| `FMOD/src/StudioEventEmitter.cs` | Per-object sound emitter |
| `FMOD/src/StudioListener.cs` | Audio listener replacement |
| `FMOD/addons/Haptics/` | Haptic feedback integration |
| `FMOD/addons/ResonanceAudio/` | Google Resonance spatial audio |

Platform-specific bindings exist for: Windows, macOS, Linux, iOS, tvOS, visionOS, Android, and WebGL.

---

## 11. Class Hierarchy & Relationships

### 11.1 Inheritance Hierarchy

| Class | Base Class | Assembly |
|---|---|---|
| `IsometricCameraController` | `MonoBehaviour` | Assembly-CSharp |
| `NetworkPlayerController` | `NetworkBehaviour` | Assembly-CSharp |
| `NetworkPlayerDebug` | `NetworkBehaviour` | Assembly-CSharp |
| `NetworkPlayerSpawner` | `MonoBehaviour` | Assembly-CSharp |
| `NetworkSessionManager` | `MonoBehaviour` | Assembly-CSharp |
| `UGSInitializer` | `MonoBehaviour` | Assembly-CSharp |
| `PlayerInputHandler` | `MonoBehaviour` | Assembly-CSharp |
| `PlayerMovement` | `MonoBehaviour` | Assembly-CSharp |
| `LobbyUI` | `MonoBehaviour` | Assembly-CSharp |
| `LobbyPlayerSlotUI` | `object` (plain class) | Assembly-CSharp |
| `NetworkSessionDebugUI` | `MonoBehaviour` | Assembly-CSharp |
| `LobbyPlayerData` | `object` (serializable) | Assembly-CSharp |

### 11.2 Component Dependencies

```
Player (GameObject, Tag: "Player")
├── Transform
├── MeshFilter            ← Capsule mesh (built-in 10208)
├── MeshRenderer
├── CapsuleCollider       ← radius 0.5, height 2
├── NetworkObject         ← Netcode identity, ownership mode 1
├── NetworkTransform      ← Server-authoritative, syncs Pos XYZ + Rot Y
├── NetworkPlayerController ← [RequireComponent] all below
├── PlayerInputHandler    ← [RequireComponent(NetworkPlayerController)]
├── PlayerInput           ← Unity Input System, events mode
├── PlayerMovement        ← [RequireComponent(CharacterController)]
├── CharacterController   ← height 2, radius 0.5, slope 45°, step 0.3
└── NetworkPlayerDebug    ← Diagnostic logging

    └── Direction Pointer (Child GameObject)
        ├── Transform         ← localPos (0, 0.5, 0.6), localScale (0.2, 0.1, 0.6)
        ├── MeshFilter        ← Cube mesh (built-in 10202)
        ├── MeshRenderer
        └── BoxCollider
```

### 11.3 Dependency Graph

```
UGSInitializer ◄─── NetworkSessionManager ◄─── LobbyUI
                                                  │
                                                  ├─── LobbyPlayerSlotUI (×4)
                                                  │
                                                  └─── LobbyPlayerData

NetworkManager ◄─── NetworkPlayerSpawner
       │
       └──── Player Prefab
                ├── NetworkPlayerController ──▶ PlayerMovement
                ├── PlayerInputHandler ──▶ NetworkPlayerController
                └── IsometricCameraController (scene camera, follows local player)
```

---

## 12. Core C# Signatures

### UGSInitializer

```csharp
public class UGSInitializer : MonoBehaviour
{
    public static UGSInitializer Instance { get; }
    public bool IsReady { get; }
    public string PlayerId { get; }
    public string CurrentPlayerName { get; }

    public async Task InitializeAsync();
    public async Task<bool> SetPlayerNameAsync(string requestedName);
}
```

### NetworkSessionManager

```csharp
public class NetworkSessionManager : MonoBehaviour
{
    public static NetworkSessionManager Instance { get; }
    public ISession CurrentSession { get; }
    public bool IsInSession { get; }
    public bool IsHost { get; }
    public string JoinCode { get; }
    public string SessionName { get; }
    public int MaxPlayers { get; }
    public int PlayerCount { get; }
    public string LastErrorMessage { get; }

    public event Action LobbyChanged;
    public event Action SessionEnded;

    public Task<bool> CreateSessionAsync();
    public async Task<bool> CreateSessionAsync(string playerName);
    public Task<bool> JoinSessionAsync(string joinCode);
    public async Task<bool> JoinSessionAsync(string joinCode, string playerName);
    public async Task LeaveSessionAsync();
    public async Task<bool> SetReadyAsync(bool ready);
    public List<LobbyPlayerData> GetLobbyPlayers();
    public bool IsPlayerReady(IReadOnlyPlayer player);
}
```

### NetworkPlayerController

```csharp
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterController))]
public class NetworkPlayerController : NetworkBehaviour
{
    public void SetMovementInput(Vector2 input);
    public override void OnNetworkSpawn();
    public override void OnNetworkDespawn();
}
```

### PlayerMovement

```csharp
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed;
    public float rotationSpeed;
    public float gravity;
    public float groundedForce;

    public void MoveCharacter(Vector3 moveDirection);
}
```

### PlayerInputHandler

```csharp
[RequireComponent(typeof(NetworkPlayerController))]
public class PlayerInputHandler : MonoBehaviour
{
    public void OnMove(InputAction.CallbackContext context);
}
```

### IsometricCameraController

```csharp
public class IsometricCameraController : MonoBehaviour
{
    // Serialized: followOffset (Vector3), followSmoothTime (float)
    // Auto-configures isometric rotation on Start
    // SmoothDamp follows local NetworkObject on LateUpdate
}
```

### NetworkPlayerSpawner

```csharp
public class NetworkPlayerSpawner : MonoBehaviour
{
    // Serialized: spawnPoints (Transform[])
    // Sets ConnectionApprovalCallback on Awake
}
```

### LobbyUI

```csharp
[RequireComponent(typeof(PanelRenderer))]
public class LobbyUI : MonoBehaviour
{
    // Serialized: sessionManager (NetworkSessionManager)
    // Manages MainMenuPanel ↔ LobbyPanel transitions
    // 4 × LobbyPlayerSlotUI instances
}
```

### LobbyPlayerSlotUI

```csharp
public sealed class LobbyPlayerSlotUI
{
    public LobbyPlayerSlotUI(VisualElement rootElement);
    public void SetPlayer(LobbyPlayerData player);
    public void SetEmpty();
}
```

### LobbyPlayerData

```csharp
[Serializable]
public class LobbyPlayerData
{
    public string PlayerId;
    public string DisplayName;
    public bool IsHost;
    public bool IsLocalPlayer;
    public bool IsReady;
}
```

---

## 13. Prefab Specifications

### 13.1 Player Prefab

**Location:** `Assets/Scripts/Player/Player.prefab`

| Property | Value |
|---|---|
| Tag | `Player` |
| Layer | 0 (Default) |
| Mesh | Built-in Capsule |
| Collider | `CapsuleCollider` (radius: 0.5, height: 2) |

**CharacterController Settings:**

| Setting | Value |
|---|---|
| Height | 2 |
| Radius | 0.5 |
| Slope Limit | 45° |
| Step Offset | 0.3 |
| Skin Width | 0.08 |
| Min Move Distance | 0.001 |

**NetworkObject Settings:**

| Setting | Value |
|---|---|
| Ownership | 1 (Owner) |
| SynchronizeTransform | `true` |
| SpawnWithObservers | `true` |
| DontDestroyWithOwner | `false` |
| AutoObjectParentSync | `true` |

**NetworkTransform Settings:**

| Setting | Value |
|---|---|
| AuthorityMode | 0 (Server) |
| Sync Position | X ✅ Y ✅ Z ✅ |
| Sync Rotation | X ❌ Y ✅ Z ❌ |
| Sync Scale | X ❌ Y ❌ Z ❌ |
| Interpolate | `true` |
| Position Threshold | 0.001 |
| Rotation Threshold | 0.01 |
| Position Max Interp Time | 0.1s |
| Rotation Max Interp Time | 0.1s |

**Prefab serialized values for `PlayerMovement`:**

| Field | Serialized Value |
|---|---|
| `moveSpeed` | 5 |
| `rotationSpeed` | 5 |
| `gravity` | −9.81 |
| `groundedForce` | −2 |

**Child: Direction Pointer**

| Property | Value |
|---|---|
| Mesh | Built-in Cube |
| Local Position | (0, 0.5, 0.6) |
| Local Scale | (0.2, 0.1, 0.6) |
| Collider | `BoxCollider` |

### 13.2 Fireball Prefab

**Location:** `Assets/VFXs/Fireball/Fireball.prefab`
*Currently exists as an asset; implementation details are stored in the VFX prefab (likely a particle/VFX Graph effect).*

---

## 14. Build & PlayMode Configuration

### 14.1 Build Profiles

| Profile | Platform | Location |
|---|---|---|
| Windows-Test | Windows | `Assets/Settings/Build Profiles/Windows-Test.asset` |

Build output directory: `Build/Debug/`

### 14.2 Multiplayer PlayMode (MPPM)

| Configuration | File |
|---|---|
| 2 Players | `Assets/Settings/PlayMode/2 Players.asset` |
| 4 Players | `Assets/Settings/PlayMode/4 Players.asset` |

These enable in-editor multiplayer testing with virtual clients via `com.unity.multiplayer.playmode` 3.0.0.

### 14.3 Rendering Profiles

| Profile | Renderer Asset | Pipeline Asset |
|---|---|---|
| PC | `PC_Renderer.asset` | `PC_RPAsset.asset` |
| Mobile | `Mobile_Renderer.asset` | `Mobile_RPAsset.asset` |

### 14.4 Scenes in Build

| Scene | Path |
|---|---|
| GameStart | `Assets/Scenes/GameStart.unity` |

---

> **Note:** This specification was reverse-engineered from the codebase as of 2026-10-20. Actions defined in the Input System (Attack, Jump, Interact, Sprint, Previous, Next) have bindings configured but are **not yet wired** to gameplay handlers — only `Move` is currently implemented end-to-end.
