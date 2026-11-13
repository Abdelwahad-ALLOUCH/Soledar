# Sunken Medina vertical-slice setup

1. Let Unity finish compiling, then run **Tools > Soledar > Create Sunken Medina Vertical Slice**.
2. Open `Assets/Scenes/SunkenMedina.unity`; the command clones the existing test route and adds discovery volumes, three currency pickups, the Silent Merchant, Lost Soul, Medina Warden, and completion trigger.
3. In the cloned scene, position generated markers to match the final tilemap route. Keep their stable room/item IDs unchanged once testing begins.
4. Verify the Player remains wired to `InputSystem_Actions`; the existing keyboard/gamepad bindings remain authoritative.

## Required project configuration

| Layer | Use |
| --- | --- |
| Ground | Tilemap, gates, walls, and raycast geometry |
| Enemy | Player attack overlap target; enemies do not collide with one another |
| Interactable | NPCs, benches, and room triggers |
| UI | Canvas elements |

`TestScene` is a regression scene. Do not run the bootstrap against it or replace it in build settings.
