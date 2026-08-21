# Changelog

## 0.1.0

Everything below is on top of 0.0.3, the last released version. Machines built
with that version load and behave as they did, apart from the fixes.

**Fixed**

- The Moon block only attracted anything on the first simulation run. From the
  second run on it was silently inert until the machine was reloaded.
- Flying above the ceiling left gravity at zero and the level pitch dark for the
  rest of the session — the build area, later runs and later levels included.
  Everything the atmosphere changes is now put back when you stop simulating.
- Turning the atmosphere on with `atmosphere true` during a run went straight to
  zero gravity instead of applying the altitude bands. The bands are now rebuilt
  whenever they, or `minAltitude`/`maxAltitude`, have changed.
- The atmosphere scaled gravity from a hardcoded value, so switching it on in a
  level with different gravity snapped to that value at ground level. It now
  scales from whatever the level actually has.
- Crossing an altitude boundary wrote one console line per rigidbody in the
  level. The log is gone.
- The gravity gun could only be fired from the keyboard. Its **Shoot** key now
  responds to Besiege's variables the same way the base-game cannon's does.
- Neither block could be built on: they declared no adding points, so anything
  attached to one landed inside it and intersected. Both now carry the standard
  five (top and four sides).
- A duplicate registration could throw out of a gravity sphere's coroutine and
  leave the sphere pulling forever.

**Changed**

- Rebuilt against current Besiege.

The source was recovered from the shipped assembly — the original was lost. See
`docs/RECOVERY.md`.

## 0.0.3

The last released Workshop version, built in 2018.
