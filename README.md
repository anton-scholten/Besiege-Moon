# Moon

A Besiege mod that adds two ways to bend gravity: a **Gravity Gun** that fires
spheres which pull everything near them, and a **Moon** — a planet you place in
the sky and can then orbit, land on, or crash into.

Everything in the level is affected, not just your machine: the enemy, the
scenery, arrows, debris, and other people's machines in multiplayer.

## The blocks

**Gravity Gun.** Aim it, press the key, and it fires a glowing sphere. The sphere
fades in, becomes a gravity source for its lifetime, then fades out and vanishes.

| Setting | What it does |
| --- | --- |
| Shoot | The key that fires. |
| Color | The sphere's colour. |
| Speed | How fast the sphere is launched. |
| Force | Pull strength. **Negative values push instead.** |
| Min Radius | Inside this radius the pull is flat, so nothing gets flung. |
| Max Radius | Outside this radius the sphere does nothing. |
| Lifetime | How long the sphere lasts once it is active. |
| Activation delay | Fade-in time, during which it pulls nothing — long enough to get the shot clear of your own machine. |

**Moon.** A sphere placed somewhere in the level. The block itself disappears
when you start simulating; only the moon is left.

| Setting | What it does |
| --- | --- |
| Auto-rotation | Spins the moon slowly. |
| Force | Pull strength; negative pushes. |
| min / max attractive radius | The same two radii as above, at planet scale. |
| Color | The moon's colour. |
| Position / Rotation / Scale | Nine sliders, one group at a time, chosen from the menu at the top. |

Between the two radii the pull falls off smoothly to nothing, so the edge of a
field is not a cliff.

## The atmosphere

Optional, and **off by default**. With it on, gravity, air drag and the ambient
light all thin out with altitude in four steps: full at ground level, gone above
the ceiling. Fly high enough and you are in vacuum and darkness.

It is driven from the mod console:

| Command | Effect |
| --- | --- |
| `atmosphere true` / `atmosphere false` | Turn it on or off. |
| `minAltitude <n>` | Where the air starts to thin. Default 750. |
| `maxAltitude <n>` | Where gravity reaches zero. Default 1000. |

Everything it changes is put back when you stop simulating.

## Installing

Subscribe on the Steam Workshop, or build it yourself:

```
./tools/install.sh
```

That compiles the mod and links it into your Besiege install; restart the game
and enable **Moon** in the mods menu. No C# toolchain is needed — the build uses
Besiege's own compiler. See [AGENTS.md](AGENTS.md) if you intend to change
anything.

The C# for this mod was lost and has been recovered from the shipped assembly;
[docs/RECOVERY.md](docs/RECOVERY.md) is the record of how, and of how far the
result can be trusted.

## Credits

Mod by **wizz6rd**. Licensed under the terms in [LICENSE](LICENSE).
