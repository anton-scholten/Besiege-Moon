# Working on this repository

Notes for anyone — human or AI — changing this mod. The [README](README.md) is
for people who just want to use it; nothing here needs repeating there.

How the C# was recovered from the shipped assembly, and how faithful the result
is, are in [docs/RECOVERY.md](docs/RECOVERY.md).

## Layout

The folder Besiege loads is `Moon/`, because that subfolder is the whole of what
gets uploaded to the Workshop. Everything beside it is not part of the mod.

```
Moon/Mod.xml                manifest: assembly, resources, block list
Moon/GravityGun.xml         the gravity gun block: mesh, colliders, module
Moon/MoonBlock.xml          the moon block: mesh, colliders, module
Moon/MoonAssembly.dll       built by tools/build.sh (checked in, the game loads it)
Moon/Resources/             the meshes and textures both blocks use
Moon/MoonScripts/*.cs       mod source; not read by the game
                            Mod.cs also holds the shared statics: the attractor
                            list, the atmosphere capture, and the small helpers
                            (Ensure/Register/SetTint) both behaviours use
tools/build.sh              compiles with Besiege's own compiler
tools/verify-build.sh       the check to run after editing any .cs
tools/install.sh            builds and installs into the game
docs/, Previous_stuff/      notes and working files; not loaded by anything
```

`Moon/MoonAssembly.dll` is committed on purpose. `Mod.xml` names it as an
`<Assembly>`, so a checkout has to carry a built one or the mod does not load.

`MoonScripts/` sits inside `Moon/` so the sources travel with the mod folder, the
way Clippy and Git View do it. Besiege only reads what `Mod.xml` names, so the
`.cs` files there are ignored by the game; `tools/install.sh --copy` strips them
out of the copy it makes.

## Hard rules

**Never change `<ID>` in `Mod.xml`.** The game generated it on first load in
2018, and changing it breaks every saved machine that references the mod. The
same goes for `<ID>1</ID>` in `GravityGun.xml` and `<ID>2</ID>` in
`MoonBlock.xml`, and for the two module names `GravityGun` and `MoonBlock`, each
of which is spelled in three places that must agree: the `[XmlRoot]` on the
module class, the `AddBlockModule` call in `Mod.OnLoad`, and the element inside
`<Modules>` in the block XML.

**Do not rename a mapper key.** The second argument to `AddKey`/`AddSlider*`/
`AddToggle`/`AddColourSlider` and the first to `AddMenu` (`"ShootKey"`,
`"minRadiusKey"`, `"posXKey"`, …) is the key a saved machine stores its setting
under. Renaming one silently resets that setting on every existing machine. The
*first* argument is only the label in the mapper and is free to change.

Two of them are worth pointing at because they read like typos and are not: the
gravity gun's colour slider is registered under `"ColorKey"` and the moon
block's under `"colorKey"`, and the moon block's radius sliders are
`"minRadiusKey"`/`"maxRadiusKey"` in lower case where the gravity gun's are
`"MinRadiusKey"`/`"MaxRadiusKey"`. They are different blocks, so nothing
collides. Leave them alone.

**Run `./tools/verify-build.sh` after editing any `.cs`.** Besiege's compiler is
ancient — write C# 4: no interpolated strings, no `?.`, no `nameof`, no
expression-bodied members, and no `enum` declarations (they segfault it).

**The five adding points in each block XML are the house standard; keep them.**
Top at `(0,0,1.0)`, and the four sides at `z=0.5` with `±0.5` offsets and their
matching `±90` rotations. They are copied verbatim from the sibling mods because
that is what makes a modded block snap onto the same grid as a base-game one.

**Do not reorder the four entries in the moon block's option menu.** A machine
saves its choice as an *index* into that list, so inserting anything but at the
end repoints every saved block at a different slider group.

## Why it is built the way it is

**`System.Xml` is on the mod loader's blacklist and this mod references it
anyway.** That is not an oversight. `InternalModding.Assemblies.AssemblyScanner`
walks field types, method locals and IL operands; it never enumerates custom
attributes. The `[XmlRoot]` markers on `GravityGun` and `MoonBlock` are metadata,
so they pass, and they are the only way to name the elements a block module
deserialises. `tools/build.sh` runs a blacklist check over every build rather
than trusting that reasoning.

**`Moon` is a `SimBehaviour` attached to every rigidbody, not a block
behaviour.** The attraction has to reach the enemy, the scenery, arrows and other
players' machines — not just modded blocks — so `Mod.OnLoad` subscribes to four
different arrival routes (`OnBlockInit`, `OnEntityPlaced`, `OnLevelLoaded`,
`SceneManager.sceneLoaded`) and adds one to each body it finds. The immediate
`SceneLoadedHandlers` call covers the scene that is already up when the mod
loads.

**Attractors publish into one shared `Mod.GravSpheres` rather than being found
by search.** A gravity gun's fired sphere and a moon block are completely
different objects, and this is what lets a single attraction loop in
`Moon.FixedUpdate` serve them both. The key is the GameObject's instance id.

**The two block behaviours defer setup to the third simulated frame, and `Moon`
to the eighth.** `SafeAwake` builds the mapper controls, but the *values* in them
are not settled until the machine has been simulating for a frame — hence the
`hasStarted` / `startFrames` dance at the top of `SimulateUpdateAlways`. `Moon`
waits longer because what it captures is the rigidbody's own drag, which the game
is still writing for several frames.

**The falloff between the two radii is a parabola, not an inverse square.** It is
1 at `minRadius` and 0 at `maxRadius`, so the field ends smoothly instead of
being cut off at the edge, and inside `minRadius` the pull is flat so a body that
falls all the way in is not flung back out. Do not "correct" it to Newtonian
gravity; the block is a toy and the shape is the point.

## What was wrong with it

The 2018 assembly was recovered faithfully, and then five real defects in it were
fixed. Read this before "simplifying" any of it back. Each fix shows up in the
IL comparison as a difference in exactly the method it was made in — see
[docs/RECOVERY.md](docs/RECOVERY.md).

**The moon block only attracted anything on the first simulation run.** Besiege
keeps the machine, and so these behaviours, alive when you stop simulating, but
`OnSimulateStop` emptied `Mod.GravSpheres` while `hasStarted` stayed `true`
forever. From the second run on, the moon was never re-registered and pulled
nothing at all; the only way back was reloading the machine. Both behaviours now
wind `hasStarted`/`startFrames` back in `OnSimulateStart`.

**The atmosphere was never put back.** `Physics.gravity`,
`RenderSettings.ambientLight` and `ambientIntensity` are global and were written
and never restored, so one flight above `maxAltitude` left the whole session at
zero gravity and pitch dark — build area, later runs and later levels included.
`Mod.CaptureAtmosphere`/`RestoreAtmosphere` remember the level's own values before
the first change and put them back at simulation stop, from whichever behaviour
notices first.

That capture also fixed a second thing: the old code scaled gravity from a
hardcoded `new Vector3(0f, -32.81f, 0f)`, so turning the atmosphere on in a level
with any other gravity snapped it to that value at sea level. It now scales from
whatever the level actually had.

**Turning the atmosphere on mid-run killed gravity outright.** The altitude band
boundaries were computed once, at the eighth simulated frame, and only if
`atmoEffects` happened to be true at that moment. Switch it on later — which is
the only way to switch it on, since it is off by default and the command is a
console command — and every boundary was still 0, so `altitude > alt3_u` held
from the first frame and `UpAtmProp(0)` ran. `UpdateBands` now rebuilds them
whenever `minAltitude` or `maxAltitude` has moved since the last pass, which
covers the `minAltitude`/`maxAltitude` commands as well.

**`UpAtmProp` logged a line per band change per body.** It reports a change to
settings that are global, from a component attached to every rigidbody in the
level, so a level with a few hundred bodies wrote a few hundred identical
`Atmo: 0.75` lines to the mod console each time anything crossed a boundary. The
log is gone.

**Neither block declared any adding points, so nothing could be built on them
and whatever you tried landed inside them.** Both had `hasAddingPoint="true"` and
no `<AddingPoints>` list. That combination is worse than it looks:
`BlockPrefabCreator.SetupAddingPoints` gives the base point's implicit adding
point `localPosition = (0, 0, 0.5)` — the block's own centre — and, unlike the
path that handles a declared `<AddingPoint>`, never applies the
`position -= forward * 0.5` correction that path ends with. So the one adding
point either block had sat half a block deeper than a declared one would, facing
-Y, and it was the only one. Both blocks now carry the same five points
(Top/Back/Front/Left/Right) that Sound Blocks, Return 2 Center and Special
Effects all use verbatim, with `hasAddingPoint="false"`.

Setting that attribute false does **not** stop the block attaching to a parent.
The base *connection* is the `TriggerForJoint` child and the `ConfigurableJoint`,
and `SetupAddingPoints` drives those from `BasePoint.Sticky`, which is a separate
element and still `true`.

**`GravSpheres.Add` could throw.** `Dictionary.Add` raises `ArgumentException` on
a duplicate key, and it was reached from inside a coroutine (`DeleteThis`) and
from `SimulateUpdateAlways`, where an exception does not just log — it abandons
the rest of the shot's lifetime, leaving it registered as a gravity source that
is never removed. Both are indexer assignments now, which cannot throw.

## Known, and not this mod's to fix

`Tried to setup button for nonexistent tooltip 1000` in `Player.log` on startup
is the base game. `Besiege.Tooltips.BlockTooltipController.RebuildTooltips`
builds its table by iterating `Enum.GetValues(typeof(BlockType))`, which is the
base game's own enum — a modded block can never be in it, so
`SetupTooltipButton` logs and returns. Every modded block does this. The cost is
one log line and no hover tooltip on the block's toolbar button.

`Mod.LevelLoadedHandler` and `SceneLoadedHandler` are one line each and identical.
They stay separate methods because they are separate delegate types
(`Action<Level>` and `UnityAction<Scene, LoadSceneMode>`); both just call
`AddMoonToEveryBody`.
