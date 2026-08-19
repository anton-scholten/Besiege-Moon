using System.Collections;
using Modding;
using Modding.Modules;
using UnityEngine;

namespace MoonMod
{
    /// <summary>
    /// Fires a glowing sphere that becomes a gravity source for its lifetime, then
    /// fades out and is removed.
    /// </summary>
    public class GravityGunBehaviour : BlockModuleBehaviour<GravityGun>
    {
        private MKey ShootKey;
        private MColourSlider SphereColor;
        private MSlider SphereSpeed;
        private MSlider SphereForce;
        private MSlider SphereMinRadius;
        private MSlider SphereMaxRadius;
        private MSlider SphereLife;
        private MSlider SphereActivationDelay;

        /// <summary>The inactive template every shot is instantiated from.</summary>
        private GameObject GravitySphere;
        private MeshRenderer MeshR;

        private bool hasStarted;
        private int startFrames;

        public override void SafeAwake()
        {
            ShootKey = AddKey("Shoot", "ShootKey", KeyCode.G);
            SphereColor = AddColourSlider("Color", "ColorKey", Color.white, false);
            SphereSpeed = AddSliderUnclamped("Speed", "SpeedKey", 0.5f, 0f, 1f);
            SphereForce = AddSliderUnclamped("Force", "ForceKey", 1f, -5f, 5f);
            SphereMinRadius = AddSliderUnclamped("Min Radius", "MinRadiusKey", 5f, 0f, 100f);
            SphereMaxRadius = AddSliderUnclamped("Max Radius", "MaxRadiusKey", 50f, 0f, 100f);
            SphereLife = AddSliderUnclamped("Lifetime", "LifetimeKey", 2f, 0f, 10f);
            SphereActivationDelay = AddSliderUnclamped("Activation delay", "ActivationDelayKey", 0.5f, 0f, 5f);

            GravitySphere = new GameObject("GravitySphereMain");
            GravitySphere.transform.parent = gameObject.transform;
            GravitySphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            GravitySphere.SetActive(false);

            Rigidbody rb = Mod.Ensure<Rigidbody>(GravitySphere);
            rb.useGravity = false;
            rb.mass = 0.01f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            Mod.Ensure<MeshFilter>(GravitySphere).mesh = ModResource.GetMesh("ICO1");

            MeshR = Mod.Ensure<MeshRenderer>(GravitySphere);
            MeshR.material.mainTexture = ModResource.GetTexture("ICO_tex");
            MeshR.material.shader = GameMaterials.Shaders.Entities.VertexLit;

            SphereColor.ValueChanged += SphereColor_ValueChanged;
        }

        /// <summary>Recolours the template at full alpha; the fades write alpha themselves.</summary>
        private void SphereColor_ValueChanged(Color col)
        {
            Mod.SetTint(MeshR, new Color(col.r, col.g, col.b, 1f));
        }

        public override void OnSimulateStart()
        {
            // Besiege keeps this behaviour between runs; without this the second run
            // reads the mapper on frame one.
            hasStarted = false;
            startFrames = 0;
        }

        public override void OnSimulateStop()
        {
            Mod.GravSpheres.Clear();
            Mod.RestoreAtmosphere();
        }

        public override void SimulateUpdateAlways()
        {
            // The mapper's values are not settled at the first simulated frame.
            if (!hasStarted)
            {
                if (startFrames != 3)
                {
                    startFrames++;
                    return;
                }
                hasStarted = true;
            }

            if (ShootKey.IsPressed)
            {
                GameObject shot = (GameObject)Instantiate(GravitySphere, gameObject.transform);
                shot.transform.localPosition = Vector3.forward;
                shot.transform.name = "GravitySphereClone";
                shot.SetActive(true);
                shot.GetComponent<Rigidbody>().AddForce(
                    SphereSpeed.Value / 10f * gameObject.transform.forward, ForceMode.Impulse);
                StartCoroutine(DeleteThis(shot, SphereLife.Value, SphereActivationDelay.Value, 0f));
            }
        }

        /// <summary>
        /// One shot's whole life: fade in over <paramref name="delay"/> pulling
        /// nothing, act as a gravity source for <paramref name="time"/>, fade out
        /// over its last quarter, then unregister and destroy itself.
        /// </summary>
        private IEnumerator DeleteThis(GameObject GO, float time, float delay, float index)
        {
            MeshRenderer MR = GO.GetComponent<MeshRenderer>();
            Color col = MR.material.GetColor("_TintColor");
            Color faded = col;

            while (index < 1f)
            {
                faded.a = index;
                Mod.SetTint(MR, faded);
                index += Time.deltaTime / delay;
                yield return null;
            }

            Mod.Register(GO, SphereForce.Value, SphereMinRadius.Value, SphereMaxRadius.Value);

            index = 0f;
            while (index < 1f)
            {
                if (index > 0.75f)
                {
                    Mod.SetTint(MR, Color.Lerp(col, new Color(col.r, col.g, col.b, 0f),
                        (index - 0.75f) * 4f));
                }
                index += Time.deltaTime / time;
                yield return null;
            }

            Mod.Unregister(GO);
            Destroy(GO);
        }
    }
}
