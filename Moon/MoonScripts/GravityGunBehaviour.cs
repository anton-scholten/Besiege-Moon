using System.Collections;
using Modding;
using Modding.Modules;
using UnityEngine;

namespace MoonMod
{
    /// <summary>
    /// The Gravity Gun block: fires a glowing sphere that becomes a gravity source
    /// for as long as it lives, then fades out and is removed.
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

        private Shader AdditiveShader;
        private Texture SphereText;
        private Mesh SphereMesh;
        private Rigidbody RigidB;
        private MeshFilter MeshF;
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

            AdditiveShader = GameMaterials.Shaders.Entities.VertexLit;
            SphereText = ModResource.GetTexture("ICO_tex");
            SphereMesh = ModResource.GetMesh("ICO1");

            GravitySphere = new GameObject();
            GravitySphere.transform.name = "GravitySphereMain";
            GravitySphere.transform.parent = gameObject.transform;
            GravitySphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            GravitySphere.SetActive(false);

            RigidB = GravitySphere.GetComponent<Rigidbody>();
            if (RigidB == null)
            {
                RigidB = GravitySphere.AddComponent<Rigidbody>();
            }
            RigidB.useGravity = false;
            RigidB.mass = 0.01f;
            RigidB.interpolation = RigidbodyInterpolation.Interpolate;

            MeshF = GravitySphere.GetComponent<MeshFilter>();
            if (MeshF == null)
            {
                MeshF = GravitySphere.AddComponent<MeshFilter>();
            }
            MeshF.mesh = SphereMesh;

            MeshR = GravitySphere.GetComponent<MeshRenderer>();
            if (MeshR == null)
            {
                MeshR = GravitySphere.AddComponent<MeshRenderer>();
            }
            MeshR.material.mainTexture = SphereText;
            MeshR.material.shader = AdditiveShader;

            SphereColor.ValueChanged += SphereColor_ValueChanged;
        }

        /// <summary>Recolours the template, at full alpha; the fades write alpha themselves.</summary>
        private void SphereColor_ValueChanged(Color col)
        {
            MeshR.material.SetColor("_Color", new Color(col.r, col.g, col.b, 1f));
            MeshR.material.SetColor("_TintColor", new Color(col.r, col.g, col.b, 1f));
        }

        public override void OnSimulateStart()
        {
            // Besiege keeps this behaviour between runs, so the settle counter has
            // to be wound back or the second run reads the mapper on frame one.
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
            // The mapper's values are not settled at the first simulated frame, so
            // shooting is held off for three of them.
            if (!hasStarted)
            {
                if (startFrames == 3)
                {
                    hasStarted = true;
                }
                else
                {
                    startFrames++;
                    return;
                }
            }

            if (ShootKey.IsPressed)
            {
                GameObject shot = (GameObject)Instantiate(GravitySphere, gameObject.transform);
                shot.transform.localPosition = Vector3.forward;
                shot.transform.name = "GravitySphereClone";
                shot.SetActive(true);
                Rigidbody shotRB = shot.GetComponent<Rigidbody>();
                shotRB.AddForce(SphereSpeed.Value / 10f * gameObject.transform.forward, ForceMode.Impulse);
                StartCoroutine(DeleteThis(shot, SphereLife.Value, SphereActivationDelay.Value, 0f));
            }
        }

        /// <summary>
        /// The whole life of one shot: fade in over <paramref name="delay"/>, act as
        /// a gravity source for <paramref name="time"/>, fade out over the last
        /// quarter of that, then unregister and destroy itself.
        /// </summary>
        private IEnumerator DeleteThis(GameObject GO, float time, float delay, float index)
        {
            MeshRenderer MR = GO.GetComponent<MeshRenderer>();
            Color col = MR.material.GetColor("_TintColor");
            Color supercolor = col;

            // Fade in. The sphere pulls nothing while this runs, which is what the
            // Activation delay slider buys: time to get the shot clear of the machine.
            while (index < 1f)
            {
                supercolor.a = index;
                MR.material.SetColor("_Color", supercolor);
                MR.material.SetColor("_TintColor", supercolor);
                index += Time.deltaTime / delay;
                yield return null;
            }

            GS_mapping Stdby = new GS_mapping
            {
                gameObject = GO,
                force = SphereForce.Value,
                minRadius = SphereMinRadius.Value,
                maxRadius = SphereMaxRadius.Value
            };
            // Indexer, not Add: a duplicate key would throw out of the
            // coroutine and leave the shot registered but never cleaned up.
            Mod.GravSpheres[GO.GetInstanceID().ToString()] = Stdby;

            index = 0f;
            while (index < 1f)
            {
                if (index > 0.75f)
                {
                    supercolor = Color.Lerp(col, new Color(col.r, col.g, col.b, 0f), (index - 0.75f) * 4f);
                    MR.material.SetColor("_Color", supercolor);
                    MR.material.SetColor("_TintColor", supercolor);
                }
                index += Time.deltaTime / time;
                yield return null;
            }

            Mod.GravSpheres.Remove(GO.GetInstanceID().ToString());
            Destroy(GO);
        }
    }
}
