# Queenless

The colony shared one mind. The queen died, and the mind tore.

Queenless is a top-down puzzle about a hive that lost its queen. You are one mind shared across several drones. You can inhabit only one body at a time. The others keep your last order — walk this way, or stay. Silence unmakes a mind but not an empty body. Mites hunt the glow and ignore everyone else.

Nine chambers. About ten minutes. Unity 6, 2D (URP).

## Play

Open this folder in **Unity 6** (this project was made in `6000.3.9f1`). Let the editor import, then press Play. The title, how to play, chamber select, pause, and ending all live in one scene.

From the title you can enter the hive, read how to play, jump to any chamber, or open the credits. Cleared chambers keep a small honey mark on this machine. Mute is remembered too.

## How to play

You inhabit one drone. The bright spark is you.

- Move with **WASD** or the arrow keys.
- **Tab** or **E** moves your mind into the next body. **Q** moves it into the previous one. You can also click a body.
- A body you leave remembers your last direction. **F** tells every remembered body to take one step.
- **Space** cancels that memory. The body stays.
- Gold seals open gold doors. Teal seals open teal doors. A door stays open only while every seal of its color is held.
- Purple silence unmakes a mind. An empty body can walk through it.
- Red mites hunt the glow. They do not see an empty body. **W** waits one beat.
- **Z** undoes. **R** restarts the chamber. **Esc** pauses.

Get your mind to the gold cell. In the last chamber, stand every body on a resonance pad at the same time.

From chamber 7 on, **W** waits and does not also move. Move up with the **up arrow**. The hint on What Hunts says so until you wait once.

Click a drone to inhabit it. Click a neighbouring open cell to step there. While a card is open, gameplay keys do nothing. **Space** dismisses the card. It does not also issue Stay.

Verbs the mind has not learned yet are visible and dim. Clicking one says: “The mind has not remembered this yet.”

## Controls

| Action | Keys | On-screen button |
|---|---|---|
| Move | WASD, arrows | — |
| Previous body | Q | — |
| Next body | Tab, E | Mind |
| Step the others | F | Follow |
| Cancel the order | Space | Stay |
| Wait one beat | W | Wait |
| Undo | Z, Backspace | Undo |
| Restart | R | Restart |
| Pause | Esc | — |
| Dismiss a card | Enter, click | — |

## Chambers

All nine are available from the title.

| | Name | What it teaches |
|---|---|---|
| 1 | First Light | Move |
| 2 | Left Behind | Switch bodies |
| 3 | The Seal | Gold seals and doors |
| 4 | Silence | Stay, Follow, and empty bodies |
| 5 | Every Body | A second seal color |
| 6 | Procession | Two orders at once |
| 7 | What Hunts | Wait, and a mite |
| 8 | Empty Things | A mite that cannot see an empty body |
| 9 | Chorus | Every body on a resonance pad |

## Check the rules

The chamber maps live in `Assets/Levels`. Author solutions are checked by a small program that does not need the Unity editor:

```bash
dotnet run --project Tools/RuleTests/RuleTests.csproj
```

Inside Unity, the same check is under **Queenless → Run Author Tests**.

## Build

With the project open in Unity: **Queenless → Build Windows** writes `Builds/Windows/Queenless.exe`. **Queenless → Build WebGL** writes `Builds/WebGL` when the WebGL module is installed. Company and product name are Queenless.

`Library`, `Temp`, `Logs`, and `UserSettings` are not in the repository. Unity recreates them on import.

## Credits

| | |
|---|---|
| Game | Queenless |
| Design and code | From the Queenless design document |
| Art | Original, made in Unity |
| Font | TextMeshPro, using Unity’s built-in typeface |
| Sound | Original, synthesized in Unity |
| Engine | Unity |
