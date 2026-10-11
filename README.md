# 101okey-lite

A single-player Okey portfolio project built with Unity 6000.3.15f1 and URP 2D.

The project is developed in small, working steps. Multiplayer is outside the scope of this project. The boundaries allow another developer to introduce networked input and synchronization later; networking is not implemented.

## Current foundation

`Assets/Scripts/Core` contains the Unity-independent `Okey.Core` assembly. It cannot reference Unity engine APIs. Stone definitions are immutable and contain their type, color and number. Number ranges, scoring and move legality belong to the rule system rather than the stone constructor.

A real joker is a role determined by game rules, rather than a separate physical stone type. A fake joker is a physical stone type. `StoneColor.None` supports stones without a printed color.

`Assets/Scripts/Presentation` contains the `Okey.Presentation` assembly. It references Core and Unity, owns face visibility and displays stone definitions. Multiple views can display the same definition with different visibility.

`Assets/Settings/Stones/DefaultStoneVisualSettings.asset` provides the palette and fake joker text. Edit these in the Inspector or create another asset through Create > Okey > Stone Visual Settings.

`Assets/Prefabs/Stone.prefab` contains the wired Stone and StoneVisual components. Its text, symbol, visible face objects and visual settings are serialized references. The text reference accepts both world-space TextMeshPro and Canvas text. Initializing a stone preserves the view's selected face visibility.

`Assets/Scripts/Configuration` contains Unity configuration adapters in `Okey.Configuration`, which references Core. `DeckSettings` converts Inspector data into an immutable `DeckDefinition`; later edits to an asset do not change an existing definition.

`Assets/Settings/Decks/StandardOkeyDeck.asset` defines the standard 106 stones as configurable sets: type, color, inclusive number range and copies per number. Create other deck assets through Create > Okey > Deck Settings. DeckFactory has no standard Okey counts or number ranges built into it.

`DeckFactory.Create` generates an ordered, read-only collection of `StoneInstance` objects. Each physical stone has an ID unique within that deck. Recreating the same definition in the same order produces the same IDs; a new game needs its own session identity if IDs are persisted or sent over a network. Copies share immutable StoneData, while their physical identities remain distinct. Stone views bind a StoneInstance so later interactions can identify the selected physical stone.

Deck generation does not shuffle, distribute, instantiate visuals or decide the joker. These remain separate responsibilities.

`IStoneShuffler` is the replaceable ordering strategy. SeededStoneShuffler applies Fisher-Yates using a private System.Random per shuffle, preserving the source collection and physical stone identities. Replaying a seed assumes the same deck definition, shuffle implementation and runtime; persist the ordered IDs for replay across different runtime versions.

`DrawPile` owns a snapshot of ordered stones and consumes them with a cursor. TryDraw returns false when exhausted. Batch Draw validates availability before consuming any stones. Duplicate physical IDs and null entries are rejected.

`OpeningDealFactory` combines generation, ordering and initial distribution. OpeningDeal contains the seed, an immutable snapshot of the initial hand and the remaining draw pile; it is not the live hand or turn state.

`Assets/Settings/Dealing/StandardOpeningDeal.asset` controls the initial hand size and fixed/random seed selection. The standard configuration deals 21 stones and leaves 85 in the draw pile. These counts are asset data. GameInstaller provides one OpeningDeal per scene through Zenject.

`Assets/Prefabs/Istaka.prefab` contains RackView with 28 slot transforms in two rows. Adjacent slots align the stone edges so stones can sit together. Players can leave empty slots between groups to create gaps. Edit the slots, stone scale, deal origin and interval in the Inspector. RackView converts slot positions into the stone root's local space and delegates movement to StoneMotion. The slot order determines the initial visual arrangement; it does not define a live hand model or game rules.

HandPresenter uses Zenject's initialization lifecycle to create and animate the initial hand once. Refresh adds or removes views as membership changes, retaining existing views and their arrangement. It validates rack capacity before creating views and owns their cleanup when the scene is disposed. Each view retains its physical identity.

`RackArrangement` owns a Unity-independent snapshot of physical stones in configurable slots. Moving to an empty slot leaves a gap. Inserting into an occupied slot shifts its contiguous neighbors toward the nearest empty slot after lifting the dragged stone; equal distances prefer the right side. Other groups and gaps remain in place. Moves preserve membership and identity and return the affected stones so presentation only animates changed positions. Its read-only slot collection reflects the current arrangement. This is a local rack layout, not turn state or move legality.

Drag a stone in SampleScene to rearrange the rack. StoneDragHandler receives EventSystem pointer events through the camera's Physics2DRaycaster and the stone's BoxCollider2D. It projects the pointer onto the rack plane, preserves the click offset and raises the whole stone through its SortingGroup. RackInteractionController coordinates the arrangement and view movement. A drop outside the rack returns the stone to its current slot unless a legal discard or layoff target accepts it; disabling the handler or losing application focus cancels the drag. One pointer can rearrange the rack at a time.

RackView's drop radius is measured in the stone root's local space and is editable in the Inspector. Slot positions, input collider size and drag sorting order are prefab data. Repositioning uses the same StoneMotion settings as the opening deal, without replaying its origin or staggered delay.

## Playing the 101 table

SampleScene starts a local 101 table with you and three bots. The first seat receives 22 stones and starts by discarding; the other seats receive 21. A regular indicator is set aside and the remaining 20 stones form the closed pile. The indicator determines the real joker; fake jokers represent that fixed color and value.

Click the face-down pile near the center to draw during your turn. Drop a stone into the bottom-right outlined area to discard. The four corner areas display each seat's latest discard. Opponent hands are represented by face-down covers and counts; their physical stones never receive face-up scene views. Rearranging your rack remains available during other seats' turns.

The header shows the maximum opening series value, pair count, thresholds, remaining pile and your penalty or final score. `SERI AC` finds disjoint runs and sets; `CIFT AC` finds identical pairs. Both reserve a stone for the required final discard. An initial series opening needs 101 points; a pair opening needs five pairs. Invalid attempts apply the configured penalty without changing the hand. Once opened, additional melds must use the same opening mode. Drag a stone onto an open meld to extend it. Joker assignments remain fixed after opening. Discarding the final tile finishes the hand and calculates scores.

`Assets/Settings/Dealing/Standard101Table.asset` controls player count, hand size, number range, opening thresholds, penalties, scoring and bot action delay. `StandardOpeningDeal.asset` supplies the shuffle seed; its practice-hand size and phase fields are not used by the table. `SinglePlayerSession` remains an isolated draw/discard practice model.

`TableSession` owns hands, turn legality, public melds and discard histories. `IRoundScoring` supplies replaceable scoring, with `Okey101Scoring` as the default. `IMeldRules` supplies replaceable meld validation; the baseline `MeldRules` validates runs, sets and pairs; `MeldPlanner` searches compatible candidates for disjoint melds. `IPlayerStrategy` receives an immutable snapshot of its own hand and public melds, without opponents' hands or the draw pile. The initial strategy protects completed melds, opens when eligible, extends public melds and discards weak tiles. Bot actions pass through the same table methods as local input.

`Assets/Prefabs/GameControls.prefab` owns the draw surface, four discard areas, opening buttons, public meld root, opponent covers, labels, text formats, scales and spacing. These are editable prefab references. Zenject binds the table, strategy and controllers; DOTween animates rack movement, discard placement and public meld positioning.

The baseline supports drawing from the closed pile. Taking the previous player's discard, special immediate-finish exceptions and multi-hand match progression are not implemented yet. The rule reference is [Pagat's 101 Okey rules](https://www.pagat.com/rummy/okey101.html); this implementation uses the configurable baseline described above rather than every published variation.

`Assets/Tests/EditMode` verifies deck generation, Inspector configuration, the model and actual prefab wiring. Run the EditMode suite from Unity's Test Runner.

## Dependency injection and animation

Zenject 9.2.0 and DOTween 1.3.030 are vendored from their official distributions under `Assets/Plugins`. Zenject's sample and test folders are excluded. Their license files are included. No additional download is needed after cloning this repository.

Sources: [Zenject release](https://github.com/modesttree/Zenject/releases/tag/9.2.0), [DOTween release](https://dotween.demigiant.com/download.php).

`Okey.Composition` is the Unity composition assembly. The GameContext object in SampleScene contains a Zenject SceneContext and GameInstaller. Assign the deck and opening deal assets, stone prefab and scene rack on the installer in the Inspector. It connects generation, dealing and presentation. StoneViewFactory instantiates and injects the assigned prefab, then binds its physical stone identity. Core has no dependency on Zenject or DOTween.

The stone prefab contains StoneMotion, which uses DOTween for local movement. `Assets/Settings/Animations/DefaultStoneMotionSettings.asset` configures duration, easing and time scaling. MoveTo accepts an optional delay and replaces the previous move; SnapTo cancels movement and places the stone immediately. Disabling a stone cancels its movement so a later reuse cannot resume an old tween.

PlayMode tests load the real scene and verify dependency resolution, prefab creation and movement lifecycle. EditMode tests cover definitions, generation and presentation configuration.

## Development conventions

- Keep game state and rules independent of Unity, UI and future transport code.
- Keep visuals and prefab references editable through the Inspector.
- Put adjustable gameplay values in configuration when their system is introduced.
- Introduce abstractions and dependency injection at actual dependency boundaries.
- Use descriptive names and focused methods without code comments.
- Commit each verified step with a plain English message, without type prefixes.

## Next steps

Add discard pickup with immediate melding, special finish variations and multi-hand match progression. Improve bot planning through IPlayerStrategy. Persistence and network synchronization can build on the existing table boundary.
