# Unity 3D Top-Down RPG Scene Builder

Generates a complete top-down RPG scene using **only primitive GameObjects** (no
sprites, no asset store). Run once → instant playable prototype.

---

## File structure

```
UnityRPGSceneBuilder/
├── Editor/
│   └── RPGSceneBuilder.cs        ← Editor-only scene generator
└── Scripts/
    ├── SceneBuilderNote.cs       ← Dev-note component (Inspector helper)
    ├── TopDownCameraController.cs
    └── TopDownPlayerController.cs
```

Copy these into your Unity project:

| File | Destination |
|------|------------|
| `Editor/RPGSceneBuilder.cs` | `Assets/Editor/` (create folder if needed) |
| `Scripts/*.cs` | `Assets/Scripts/` (or any non-Editor folder) |

---

## How to use

1. Open your Unity project (2022 LTS or newer, Built-in or URP).
2. Drop the files into the folders above. Unity will compile automatically.
3. In the top menu bar you'll see **RPG Scene Builder**.
4. Click **RPG Scene Builder → Build Scene**.
5. The scene is generated instantly. Press **Play** to walk around.

> **Rebuild any time:** "RPG Scene Builder → Rebuild Scene" clears and
> regenerates everything in one click.

---

## What gets generated

| Category | Objects |
|----------|---------|
| Ground | 40×40 grass plane |
| Paths | Dirt-coloured planes (crossroads) |
| Water | Semi-transparent pond |
| Buildings | Inn, Smithy, Shop, 2× House (walls + floor + optional roof + door) |
| Trees | 12 trees (trunk cylinder + sphere foliage) |
| Rocks | 4 rock clusters (scaled spheres) |
| Fences | Post-and-rail fence along top & bottom edges |
| Chests | 3 interactable chest cubes |
| Player | Blue capsule + facing indicator |
| Enemies | 4 red capsules |
| NPCs | 3 yellow capsules (Innkeeper, Blacksmith, Merchant) |
| Camera | Angled top-down perspective, auto-positioned |
| Lighting | Warm directional sun + ambient fill |

---

## Customizing the layout

All data is **plain arrays inside the script** — no ScriptableObjects needed
for a prototype. Find the section you want and edit the positions:

```csharp
// ── Add a new building ──────────────────────────────────────
MakeBuilding(folder, "Temple", new Vector3(0, 0, 15), new Vector3(8, 4, 6), true);

// ── Move the player spawn ───────────────────────────────────
var body = MakePrimitive(PrimitiveType.Capsule, "Body", COL_PLAYER, player);
body.transform.position = new Vector3(2, 1, -3);   // ← change this

// ── Add more enemies ────────────────────────────────────────
var enemyPositions = new Vector3[]
{
    new Vector3( 6, 0,  6),
    new Vector3(-6, 0,  7),
    new Vector3( 7, 0, -6),
    new Vector3( 3, 0,  3),   // ← new one
};
```

After any edit, run **Rebuild Scene** to see the result immediately.

---

## Runtime scripts

### TopDownPlayerController
Attach to the **Player** GameObject. Uses `Rigidbody` physics.
- `moveSpeed` – units per second (default 5)
- `rotateSpeed` – degrees per second the model snaps to face direction (default 720)

### TopDownCameraController
Attach to **Main Camera**. Drag the Player transform into `target`.
- `offset` – camera offset from player (default `(0, 22, -14)`)
- `pitchAngle` – how steeply the camera looks down (default 58°)
- `smoothSpeed` – camera lag (default 8)

---

## Tips

- **Colliders** – every `CreatePrimitive` call includes a collider. Remove the
  ones you don't need (e.g. fence rails) in the Inspector after generation.
- **Tags** – add an "Interactable" tag in Project Settings and assign it to
  chests inside `BuildChests()` for easy runtime detection.
- **Materials** – the builder creates one `Standard` material per unique colour
  and shares it. Swap a material in one place to restyle everything of that type.
- **URP** – if you use URP, change `Shader.Find("Standard")` to
  `Shader.Find("Universal Render Pipeline/Lit")` in `GetOrCreateMaterial()`.
- **Scale** – the whole map is 40×40 world units. Change the ground plane scale
  and fence loop bounds together to resize it.
