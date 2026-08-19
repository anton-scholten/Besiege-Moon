using System.Collections.Generic;
using UnityEngine;

namespace MoonMod
{
    /// <summary>
    /// Rides every rigidbody in the level and pulls it towards each entry in
    /// <see cref="Mod.GravSpheres"/>. Also applies the optional atmosphere, which
    /// thins gravity, drag and ambient light with altitude.
    /// </summary>
    /// <remarks>
    /// Attached by <see cref="Mod"/> rather than by a block XML, because it has to
    /// reach base-game bodies and level entities too, not just modded blocks.
    /// </remarks>
    public class Moon : SimBehaviour
    {
        private float differenceAltitude;

        // The two interior band boundaries, and a dead zone either side of all
        // four. No band matches inside a dead zone, so the last one applied simply
        // stays -- which is what keeps a body hovering on a boundary from flapping
        // between two settings.
        private float alt1;
        private float alt2;
        private float alt0_l;
        private float alt0_u;
        private float alt1_l;
        private float alt1_u;
        private float alt2_l;
        private float alt2_u;
        private float alt3_l;
        private float alt3_u;

        /// <summary>Which altitude band was last applied; 0 means "none yet".</summary>
        private int updateProp;

        /// <summary>
        /// The altitudes the bands above were computed from. Both console-settable,
        /// and the atmosphere can be switched on long after this body started, so
        /// the bands are rebuilt whenever these have moved rather than once at
        /// startup. NaN forces the first pass to compute them.
        /// </summary>
        private float bandMin = float.NaN;
        private float bandMax = float.NaN;

        private GameObject GO;
        private Rigidbody RB;
        private Vector3 ForceDir;

        private bool hasStarted;
        private int startFrames;

        private Vector3 pos1;
        private Vector3 pos2;
        private float altitude;

        private float oldDrag;
        private float oldAngularDrag;

        public void FixedUpdate()
        {
            if (!isSimulating)
            {
                // Besiege keeps this behaviour alive between runs, so whatever the
                // last one left behind has to be put back here; nothing else will.
                if (hasStarted)
                {
                    hasStarted = false;
                    startFrames = 0;
                    updateProp = 0;
                    Mod.RestoreAtmosphere();
                }
                return;
            }

            // Setup waits eight simulated frames: the rigidbody's own drag values
            // are what get scaled by the atmosphere, and they are not settled at
            // the first frame of a run.
            if (!hasStarted)
            {
                if (startFrames == 8)
                {
                    hasStarted = true;
                    startFrames = 0;
                    GO = transform.gameObject;
                    RB = GO.GetComponent<Rigidbody>();
                    if (RB != null)
                    {
                        oldDrag = RB.drag;
                        oldAngularDrag = RB.angularDrag;
                    }
                }
                else
                {
                    startFrames++;
                    return;
                }
            }

            // Massless bodies are the game's own bookkeeping objects, and the moon
            // block zeroes its own mass to bow out of its own attraction.
            if (RB == null || RB.mass < 0.01)
            {
                return;
            }

            pos1 = transform.position;
            foreach (KeyValuePair<string, GS_mapping> sphere in Mod.GravSpheres)
            {
                GS_mapping gs = sphere.Value;
                pos2 = gs.gameObject.transform.position;
                ForceDir = pos2 - pos1;
                float distance = ForceDir.magnitude;
                if (distance < gs.maxRadius)
                {
                    if (distance < gs.minRadius)
                    {
                        // Inside the core the pull is flat, so a body that falls in
                        // does not get an unbounded kick out of a 1/r^2 term.
                        ForceDir = ForceDir.normalized;
                        RB.AddForce(ForceDir * gs.force, ForceMode.Impulse);
                    }
                    else
                    {
                        // Between the radii the pull falls off as a parabola that is
                        // 1 at minRadius and 0 at maxRadius, so it reaches the edge
                        // of the field smoothly instead of cutting out.
                        float span = Mathf.Pow(gs.minRadius - gs.maxRadius, 2f);
                        float shaped = -Mathf.Pow(distance, 2f) + Mathf.Pow(gs.maxRadius, 2f)
                            + 2f * gs.minRadius * (distance - gs.maxRadius);
                        ForceDir = ForceDir.normalized * (shaped / span);
                        RB.AddForce(ForceDir * gs.force, ForceMode.Impulse);
                    }
                }
            }

            if (Mod.atmoEffects)
            {
                UpdateBands();
                altitude = gameObject.transform.position.y;
                if (altitude < alt0_l)
                {
                    if (updateProp != 1)
                    {
                        UpAtmProp(1f);
                        updateProp = 1;
                    }
                }
                else if (altitude > alt3_u)
                {
                    if (updateProp != 2)
                    {
                        UpAtmProp(0f);
                        updateProp = 2;
                    }
                }
                else if (alt0_u < altitude & altitude < alt1_l)
                {
                    if (updateProp != 3)
                    {
                        UpAtmProp(0.75f);
                        updateProp = 3;
                    }
                }
                else if (alt1_u < altitude & altitude < alt2_l)
                {
                    if (updateProp != 4)
                    {
                        UpAtmProp(0.5f);
                        updateProp = 4;
                    }
                }
                else if (alt2_u < altitude & altitude < alt3_l)
                {
                    if (updateProp != 5)
                    {
                        UpAtmProp(0.25f);
                        updateProp = 5;
                    }
                }
            }
        }

        /// <summary>
        /// Recomputes the altitude bands if the altitudes they came from have
        /// changed since the last pass.
        /// </summary>
        /// <remarks>
        /// The 2018 build did this once, at the eighth simulated frame, and only if
        /// the atmosphere happened to be on at that moment. Switching it on later
        /// left every boundary at zero, so <c>altitude &gt; alt3_u</c> was true from
        /// the first frame and gravity went straight to nothing.
        /// </remarks>
        private void UpdateBands()
        {
            if (bandMin == Mod.minAltitude && bandMax == Mod.maxAltitude)
            {
                return;
            }
            bandMin = Mod.minAltitude;
            bandMax = Mod.maxAltitude;

            differenceAltitude = Mod.maxAltitude - Mod.minAltitude;
            alt1 = differenceAltitude / 3f + Mod.minAltitude;
            alt2 = differenceAltitude * 2f / 3f + Mod.minAltitude;
            alt0_l = Mod.minAltitude - (float)(differenceAltitude * 0.05);
            alt0_u = Mod.minAltitude + (float)(differenceAltitude * 0.05);
            alt1_l = alt1 - (float)(differenceAltitude * 0.05);
            alt1_u = alt1 + (float)(differenceAltitude * 0.05);
            alt2_l = alt2 - (float)(differenceAltitude * 0.05);
            alt2_u = alt2 + (float)(differenceAltitude * 0.05);
            alt3_l = Mod.maxAltitude - (float)(differenceAltitude * 0.05);
            alt3_u = Mod.maxAltitude + (float)(differenceAltitude * 0.05);

            // The boundaries moved, so whichever band we are in now has to be
            // applied again even if it carries the same number as before.
            updateProp = 0;
        }

        /// <summary>Applies one atmosphere step, where <paramref name="scale"/> is 1 at sea level and 0 in vacuum.</summary>
        private void UpAtmProp(float scale)
        {
            Mod.CaptureAtmosphere();
            RenderSettings.ambientLight = new Color(1f, 1f, 1f, scale);
            RenderSettings.ambientIntensity = scale;
            // Leave gravity alone when the player has turned it off themselves.
            if (!StatMaster.GodTools.GravityDisabled)
            {
                Physics.gravity = Mod.BaseGravity * scale;
            }
            RB.drag = oldDrag * scale;
            RB.angularDrag = oldAngularDrag * scale;
        }
    }
}
