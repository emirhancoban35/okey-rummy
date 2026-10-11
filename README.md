# 101okey-lite

A single-player Okey portfolio project built with Unity 6000.3.15f1 and URP 2D.

The project is developed in small, working steps. Multiplayer is outside the scope of this project. The boundaries allow another developer to introduce networked input and synchronization later; networking is not implemented.

## Current foundation

`Assets/Scripts/Core` contains the Unity-independent `Okey.Core` assembly. It cannot reference Unity engine APIs. Stone definitions are immutable and contain their type, color and number. Number ranges, scoring and move legality belong to the future rule system, rather than the stone constructor.

A real joker is a role determined by game rules, rather than a separate physical stone type. A fake joker is a physical stone type. `StoneColor.None` supports stones without a printed color.

`Assets/Scripts/Presentation` contains the `Okey.Presentation` assembly. It references Core and Unity, owns face visibility and displays stone definitions. Multiple views can display the same definition with different visibility.

`Assets/Settings/Stones/DefaultStoneVisualSettings.asset` provides the palette and fake joker text. Edit these in the Inspector or create another asset through Create > Okey > Stone Visual Settings.

`Assets/Prefabs/Stone.prefab` contains the wired Stone and StoneVisual components. Its text, symbol, visible face objects and visual settings are serialized references. The text reference accepts both world-space TextMeshPro and Canvas text. Initializing a stone preserves the view's selected face visibility.

`Assets/Tests/EditMode` verifies the model and actual prefab wiring. Run the EditMode suite from Unity's Test Runner.

## Development conventions

- Keep game state and rules independent of Unity, UI and future transport code.
- Keep visuals and prefab references editable through the Inspector.
- Put adjustable gameplay values in configuration when their system is introduced.
- Introduce abstractions and dependency injection at actual dependency boundaries.
- Use descriptive names and focused methods without code comments.
- Commit each verified step with a plain English message, without type prefixes.

## Next steps

Introduce deck configuration and generation, then hand and rack interactions, game flow, and interchangeable rules and penalties. Persistence, bot decisions and network synchronization can build on these boundaries when needed.
