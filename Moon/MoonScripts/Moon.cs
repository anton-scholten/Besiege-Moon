using System.Collections.Generic;
using UnityEngine;

namespace MoonMod
{
    /// <summary>
    /// Rides every rigidbody in the level and pulls it towards each entry in
    /// <see cref="Mod.GravSpheres"/>, and applies the optional atmosphere.
    /// </summary>
    /// <remarks>
    /// A SimBehaviour added by <see cref="Mod"/>, not a block module, because the
    /// attraction has to reach base-game bodies and level entities too.
    /// </remarks>
    public class Moon : SimBehaviour
    {
        /// <summary>How far above and below each edge nothing is applied, so a body sitting on one does not flap between two zones.</summary>
        private const float GuardFraction = 0.05f;

        /// <summary>Atmosphere strength in each of the five zones, ground upwards.</summary>
        private static readonly float[] ZoneScale = new float[] { 1f, 0.75f, 0.5f, 0.25f, 0f };

        /// <summary>The four zone boundaries: minAltitude, two thirds between, maxAltitude.</summary>
        private readonly float[] edges = new float[4];
        private float guard;

        /// <summary>Zone last applied, or -1 for none yet.</summary>
        private int zoneApplied = -1;

        /// <summary>The altitudes <see cref="edges"/> was built from; NaN forces the first build.</summary>
        private float bandMin = float.NaN;
        private float bandMax = float.NaN;

        private Rigidbody RB;
        private bool hasStarted;
        private int startFrames;
        private float oldDrag;
        private float oldAngularDrag;

        public void FixedUpdate()
        {
            if (!isSimulating)
            {
                // Besiege keeps this behaviour between runs, so the last run's state
                // has to be undone here; nothing else will.
                if (hasStarted)
                {
                    hasStarted = false;
                    startFrames = 0;
                    zoneApplied = -1;
                    Mod.RestoreAtmosphere();
                }
                return;
            }

            // Setup waits eight frames: what it captures is the body's own drag,
            // which the game is still writing for the first few.
            if (!hasStarted)
            {
                if (startFrames != 8)
                {
                    startFrames++;
                    return;
                }
                hasStarted = true;
                startFrames = 0;
                RB = GetComponent<Rigidbody>();
                if (RB != null)
                {
                    oldDrag = RB.drag;
                    oldAngularDrag = RB.angularDrag;
                }
            }

            // Massless bodies are the game's own bookkeeping objects, and a moon
            // block zeroes its own mass to bow out of its own attraction.
            if (RB == null || RB.mass < 0.01)
            {
                return;
            }

            Attract();

            if (Mod.atmoEffects)
            {
                ApplyAtmosphere();
            }
        }

        private void Attract()
        {
            Vector3 here = transform.position;
            foreach (KeyValuePair<string, GS_mapping> entry in Mod.GravSpheres)
            {
                GS_mapping gs = entry.Value;
                Vector3 toSphere = gs.gameObject.transform.position - here;
                float distance = toSphere.magnitude;
                if (distance >= gs.maxRadius)
                {
                    continue;
                }

                // Flat inside minRadius so a body that falls all the way in is not
                // flung back out; outside it, a parabola falling to 0 at maxRadius.
                float strength = 1f;
                if (distance >= gs.minRadius)
                {
                    float span = Mathf.Pow(gs.minRadius - gs.maxRadius, 2f);
                    strength = (-Mathf.Pow(distance, 2f) + Mathf.Pow(gs.maxRadius, 2f)
                        + 2f * gs.minRadius * (distance - gs.maxRadius)) / span;
                }
                RB.AddForce(toSphere.normalized * strength * gs.force, ForceMode.Impulse);
            }
        }

        private void ApplyAtmosphere()
        {
            UpdateBands();

            float altitude = transform.position.y;
            int zone = -1;
            if (altitude < edges[0] - guard)
            {
                zone = 0;
            }
            else if (altitude > edges[3] + guard)
            {
                zone = 4;
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    if (edges[i] + guard < altitude && altitude < edges[i + 1] - guard)
                    {
                        zone = i + 1;
                        break;
                    }
                }
            }

            if (zone >= 0 && zone != zoneApplied)
            {
                UpAtmProp(ZoneScale[zone]);
                zoneApplied = zone;
            }
        }

        /// <summary>
        /// Rebuilds the zone boundaries when the altitudes they came from have moved.
        /// Both are console-settable and the atmosphere can be switched on long after
        /// this body started, so this cannot be done once at startup.
        /// </summary>
        private void UpdateBands()
        {
            if (bandMin == Mod.minAltitude && bandMax == Mod.maxAltitude)
            {
                return;
            }
            bandMin = Mod.minAltitude;
            bandMax = Mod.maxAltitude;

            float span = Mod.maxAltitude - Mod.minAltitude;
            guard = span * GuardFraction;
            edges[0] = Mod.minAltitude;
            edges[1] = Mod.minAltitude + span / 3f;
            edges[2] = Mod.minAltitude + span * 2f / 3f;
            edges[3] = Mod.maxAltitude;

            // The boundaries moved, so re-apply whichever zone we are in now.
            zoneApplied = -1;
        }

        /// <summary>Applies one atmosphere step: <paramref name="scale"/> is 1 at ground level and 0 in vacuum.</summary>
        private void UpAtmProp(float scale)
        {
            Mod.CaptureAtmosphere();
            RenderSettings.ambientLight = new Color(1f, 1f, 1f, scale);
            RenderSettings.ambientIntensity = scale;
            // Gravity the player turned off themselves is not ours to write.
            if (!StatMaster.GodTools.GravityDisabled)
            {
                Physics.gravity = Mod.BaseGravity * scale;
            }
            RB.drag = oldDrag * scale;
            RB.angularDrag = oldAngularDrag * scale;
        }
    }
}
