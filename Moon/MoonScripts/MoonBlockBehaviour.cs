using System.Collections.Generic;
using Modding;
using Modding.Modules;
using UnityEngine;

namespace MoonMod
{
    /// <summary>
    /// A large sphere placed somewhere in the level that acts as a standing gravity
    /// source. The block itself disappears at simulation start; only the moon remains.
    /// </summary>
    public class MoonBlockBehaviour : BlockModuleBehaviour<MoonBlock>
    {
        /// <summary>Per-frame tumble, before the timescale and 1/100 factor.</summary>
        private static readonly Vector3 SpinRate = new Vector3(1f, 0.75f, 0.8f);

        private MToggle rotationToggle;
        private MSlider force;
        private MSlider minRadius;
        private MSlider maxRadius;
        private MColourSlider color;

        /// <summary>Picks which of the four slider groups the mapper shows.</summary>
        private MMenu optionMenu;

        private MSlider posX;
        private MSlider posY;
        private MSlider posZ;
        private MSlider rotX;
        private MSlider rotY;
        private MSlider rotZ;
        private MSlider scaX;
        private MSlider scaY;
        private MSlider scaZ;

        /// <summary>The moon itself. It lives in the scene, not under the block.</summary>
        public GameObject moonGO;

        private MeshRenderer moonMR;
        private bool hasStarted;
        private int startFrames;

        public override void SafeAwake()
        {
            rotationToggle = AddToggle("Auto-rotation", "RotationToggleKey", true);
            force = AddSliderUnclamped("Force", "ForceKey", 0.05f, -1f, 1f);
            minRadius = AddSliderUnclamped("min attractive radius", "minRadiusKey", 100f, 0f, 500f);
            maxRadius = AddSliderUnclamped("max attractive radius", "maxRadiusKey", 1000f, 0f, 10000f);
            color = AddColourSlider("Color", "colorKey", Color.white, false);

            optionMenu = AddMenu("optionMenu", 0,
                new List<string> { "Options", "Position", "Rotation", "Scale" }, false);

            posX = AddSliderUnclamped("X", "posXKey", -300f, -500f, 500f);
            posY = AddSliderUnclamped("Y", "posYKey", 125f, 0f, 1000f);
            posZ = AddSliderUnclamped("Z", "posZKey", 300f, -500f, 500f);
            rotX = AddSliderUnclamped("X", "rotXKey", 0f, -90f, 90f);
            rotY = AddSliderUnclamped("Y", "rotYKey", 0f, -90f, 90f);
            rotZ = AddSliderUnclamped("Z", "rotZKey", 0f, -90f, 90f);
            scaX = AddSliderUnclamped("X", "scaXKey", 25f, 0.1f, 50f);
            scaY = AddSliderUnclamped("Y", "scaYKey", 25f, 0.1f, 50f);
            scaZ = AddSliderUnclamped("Z", "scaZKey", 25f, 0.1f, 50f);

            if (moonGO != null)
            {
                return;
            }

            moonGO = new GameObject("Moon");
            ApplyTransform();

            Rigidbody moonRB = Mod.Ensure<Rigidbody>(moonGO);
            moonRB.useGravity = false;
            moonRB.mass = 0.01f;
            moonRB.interpolation = RigidbodyInterpolation.Interpolate;
            // Kinematic: the moon pulls, it is never pulled.
            moonRB.isKinematic = true;

            Mesh mesh = ModResource.GetMesh("Planet_mesh");
            Mod.Ensure<MeshFilter>(moonGO).mesh = mesh;

            MeshCollider moonMC = Mod.Ensure<MeshCollider>(moonGO);
            moonMC.sharedMesh = mesh;
            moonMC.enabled = true;
            moonMC.material.bounceCombine = PhysicMaterialCombine.Minimum;
            moonMC.material.bounciness = 1f;
            moonMC.material.dynamicFriction = 1f;
            moonMC.material.frictionCombine = PhysicMaterialCombine.Maximum;
            moonMC.material.staticFriction = 1f;

            moonMR = Mod.Ensure<MeshRenderer>(moonGO);
            moonMR.material.mainTexture = ModResource.GetTexture("Planet_text");
            Mod.SetTint(moonMR, color.Value);

            color.ValueChanged += Color_ValueChanged;
            optionMenu.ValueChanged += OptionMenu_ValueChanged;
            foreach (MSlider slider in new MSlider[] { posX, posY, posZ, rotX, rotY, rotZ, scaX, scaY, scaZ })
            {
                slider.ValueChanged += MoonProperty_ValueChanged;
            }
        }

        /// <summary>Shows the group the menu selects and hides the other three.</summary>
        private void OptionMenu_ValueChanged(int value)
        {
            SetVisible(value == 0, rotationToggle, force, minRadius, maxRadius, color);
            SetVisible(value == 1, posX, posY, posZ);
            SetVisible(value == 2, rotX, rotY, rotZ);
            SetVisible(value == 3, scaX, scaY, scaZ);
        }

        private static void SetVisible(bool visible, params MapperType[] controls)
        {
            foreach (MapperType control in controls)
            {
                control.DisplayInMapper = visible;
            }
        }

        private void Color_ValueChanged(Color value)
        {
            Mod.SetTint(moonMR, value);
        }

        /// <summary>One handler for all nine transform sliders; it rewrites all three vectors.</summary>
        private void MoonProperty_ValueChanged(float value)
        {
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            moonGO.transform.position = new Vector3(posX.Value, posY.Value, posZ.Value);
            moonGO.transform.rotation = Quaternion.Euler(rotX.Value, rotY.Value, rotZ.Value);
            moonGO.transform.localScale = new Vector3(scaX.Value, scaY.Value, scaZ.Value);
        }

        private void Spin()
        {
            if (rotationToggle.IsActive)
            {
                moonGO.transform.Rotate(SpinRate * Time.timeScale / 100f);
            }
        }

        public override void OnSimulateStart()
        {
            // Besiege keeps this behaviour between runs while OnSimulateStop empties
            // GravSpheres, so without this the moon is registered on the first run
            // only and attracts nothing ever again.
            hasStarted = false;
            startFrames = 0;
        }

        public override void OnSimulateStop()
        {
            Mod.GravSpheres.Clear();
            Mod.RestoreAtmosphere();
            BlockBehaviour.BuildingBlock.gameObject.SetActive(true);
        }

        public void OnDestroy()
        {
            // Only tear the moon down with the block itself, not with a simulation copy.
            if (BlockBehaviour.BuildingBlock == null)
            {
                Destroy(moonGO);
            }
        }

        public override void BuildingUpdate()
        {
            Spin();
        }

        public override void SimulateUpdateAlways()
        {
            if (!hasStarted)
            {
                // The block gets out of the way of its own moon: invisible, no
                // collisions, no mass, so it neither renders nor is attracted.
                VisualController.SetInvisible();
                Rigidbody.detectCollisions = false;
                Rigidbody.mass = 0f;

                if (startFrames != 3)
                {
                    startFrames++;
                    return;
                }
                hasStarted = true;

                moonMR = moonGO.GetComponent<MeshRenderer>();
                moonGO.transform.rotation = Quaternion.Euler(rotX.Value, rotY.Value, rotZ.Value);
                BlockBehaviour.BuildingBlock.gameObject.SetActive(false);
                Mod.Register(moonGO, force.Value, minRadius.Value, maxRadius.Value);
            }

            Spin();
        }
    }
}
