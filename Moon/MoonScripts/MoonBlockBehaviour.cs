using System.Collections.Generic;
using Modding;
using Modding.Modules;
using UnityEngine;

namespace MoonMod
{
    /// <summary>
    /// The Moon block: a large sphere placed somewhere in the level that acts as a
    /// standing gravity source. The block itself disappears at simulation start;
    /// only the moon remains.
    /// </summary>
    public class MoonBlockBehaviour : BlockModuleBehaviour<MoonBlock>
    {
        private MToggle rotationToggle;
        private MSlider force;
        private MSlider minRadius;
        private MSlider maxRadius;
        private MColourSlider color;

        /// <summary>Picks which of the four slider groups below the mapper shows.</summary>
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

        private Rigidbody moonRB;
        private MeshFilter moonMF;
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

            optionMenu = AddMenu("optionMenu", 0, new List<string> { "Options", "Position", "Rotation", "Scale" }, false);

            posX = AddSliderUnclamped("X", "posXKey", -300f, -500f, 500f);
            posY = AddSliderUnclamped("Y", "posYKey", 125f, 0f, 1000f);
            posZ = AddSliderUnclamped("Z", "posZKey", 300f, -500f, 500f);
            rotX = AddSliderUnclamped("X", "rotXKey", 0f, -90f, 90f);
            rotY = AddSliderUnclamped("Y", "rotYKey", 0f, -90f, 90f);
            rotZ = AddSliderUnclamped("Z", "rotZKey", 0f, -90f, 90f);
            scaX = AddSliderUnclamped("X", "scaXKey", 25f, 0.1f, 50f);
            scaY = AddSliderUnclamped("Y", "scaYKey", 25f, 0.1f, 50f);
            scaZ = AddSliderUnclamped("Z", "scaZKey", 25f, 0.1f, 50f);

            if (moonGO == null)
            {
                moonGO = new GameObject("Moon");
                moonGO.transform.position = new Vector3(-300f, 125f, 300f);
                moonGO.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                moonGO.transform.localScale = new Vector3(25f, 25f, 25f);

                moonRB = moonGO.GetComponent<Rigidbody>();
                if (moonRB == null)
                {
                    moonRB = moonGO.AddComponent<Rigidbody>();
                }
                moonRB.useGravity = false;
                moonRB.mass = 0.01f;
                moonRB.interpolation = RigidbodyInterpolation.Interpolate;
                // Kinematic: the moon pulls, it is never pulled.
                moonRB.isKinematic = true;

                moonMF = moonGO.GetComponent<MeshFilter>();
                if (moonMF == null)
                {
                    moonMF = moonGO.AddComponent<MeshFilter>();
                }
                moonMF.mesh = ModResource.GetMesh("Planet_mesh");

                MeshCollider moonMC = moonGO.GetComponent<MeshCollider>();
                if (moonMC == null)
                {
                    moonMC = moonGO.AddComponent<MeshCollider>();
                }
                moonMC.sharedMesh = ModResource.GetMesh("Planet_mesh");
                moonMC.enabled = true;
                moonMC.material.bounceCombine = PhysicMaterialCombine.Minimum;
                moonMC.material.bounciness = 1f;
                moonMC.material.dynamicFriction = 1f;
                moonMC.material.frictionCombine = PhysicMaterialCombine.Maximum;
                moonMC.material.staticFriction = 1f;

                moonMR = moonGO.GetComponent<MeshRenderer>();
                if (moonMR == null)
                {
                    moonMR = moonGO.AddComponent<MeshRenderer>();
                }
                moonMR.material.mainTexture = ModResource.GetTexture("Planet_text");
                moonMR.material.SetColor("_Color", color.Value);
                moonMR.material.SetColor("_TintColor", color.Value);

                color.ValueChanged += Color_ValueChanged;
                optionMenu.ValueChanged += OptionMenu_ValueChanged;
                posX.ValueChanged += MoonProperty_ValueChanged;
                posY.ValueChanged += MoonProperty_ValueChanged;
                posZ.ValueChanged += MoonProperty_ValueChanged;
                rotX.ValueChanged += MoonProperty_ValueChanged;
                rotY.ValueChanged += MoonProperty_ValueChanged;
                rotZ.ValueChanged += MoonProperty_ValueChanged;
                scaX.ValueChanged += MoonProperty_ValueChanged;
                scaY.ValueChanged += MoonProperty_ValueChanged;
                scaZ.ValueChanged += MoonProperty_ValueChanged;
            }
        }

        /// <summary>Shows the slider group the menu selects and hides the other three.</summary>
        private void OptionMenu_ValueChanged(int value)
        {
            if (value == 0)
            {
                rotationToggle.DisplayInMapper = true;
                force.DisplayInMapper = true;
                minRadius.DisplayInMapper = true;
                maxRadius.DisplayInMapper = true;
                color.DisplayInMapper = true;
            }
            else
            {
                rotationToggle.DisplayInMapper = false;
                force.DisplayInMapper = false;
                minRadius.DisplayInMapper = false;
                maxRadius.DisplayInMapper = false;
                color.DisplayInMapper = false;
            }

            if (value == 1)
            {
                posX.DisplayInMapper = true;
                posY.DisplayInMapper = true;
                posZ.DisplayInMapper = true;
            }
            else
            {
                posX.DisplayInMapper = false;
                posY.DisplayInMapper = false;
                posZ.DisplayInMapper = false;
            }

            if (value == 2)
            {
                rotX.DisplayInMapper = true;
                rotY.DisplayInMapper = true;
                rotZ.DisplayInMapper = true;
            }
            else
            {
                rotX.DisplayInMapper = false;
                rotY.DisplayInMapper = false;
                rotZ.DisplayInMapper = false;
            }

            if (value == 3)
            {
                scaX.DisplayInMapper = true;
                scaY.DisplayInMapper = true;
                scaZ.DisplayInMapper = true;
            }
            else
            {
                scaX.DisplayInMapper = false;
                scaY.DisplayInMapper = false;
                scaZ.DisplayInMapper = false;
            }
        }

        private void Color_ValueChanged(Color value)
        {
            moonMR.material.SetColor("_Color", value);
            moonMR.material.SetColor("_TintColor", value);
        }

        /// <summary>One handler for all nine transform sliders; it just writes all three vectors.</summary>
        private void MoonProperty_ValueChanged(float value)
        {
            moonGO.transform.position = new Vector3(posX.Value, posY.Value, posZ.Value);
            moonGO.transform.rotation = Quaternion.Euler(rotX.Value, rotY.Value, rotZ.Value);
            moonGO.transform.localScale = new Vector3(scaX.Value, scaY.Value, scaZ.Value);
        }

        public override void OnSimulateStart()
        {
            // Besiege keeps this behaviour between runs while OnSimulateStop empties
            // GravSpheres, so without winding these back the moon is registered on
            // the first run only and attracts nothing ever again.
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
            // Only tear the moon down when the block itself is gone, not when the
            // simulation copy of it is being cleaned up.
            if (BlockBehaviour.BuildingBlock == null)
            {
                Destroy(moonGO);
            }
        }

        public override void BuildingUpdate()
        {
            if (rotationToggle.IsActive)
            {
                moonGO.transform.Rotate(new Vector3(1f, 0.75f, 0.8f) * Time.timeScale / 100f);
            }
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

                if (startFrames == 3)
                {
                    hasStarted = true;
                    moonMR = moonGO.GetComponent<MeshRenderer>();
                    moonGO.transform.rotation = Quaternion.Euler(rotX.Value, rotY.Value, rotZ.Value);
                    BlockBehaviour.BuildingBlock.gameObject.SetActive(false);

                    GS_mapping mapping = new GS_mapping
                    {
                        gameObject = moonGO,
                        force = force.Value,
                        minRadius = minRadius.Value,
                        maxRadius = maxRadius.Value
                    };
                    // Indexer, not Add, so re-registering a moon can never throw.
                    Mod.GravSpheres[moonGO.GetInstanceID().ToString()] = mapping;
                }
                else
                {
                    startFrames++;
                    return;
                }
            }

            if (rotationToggle.IsActive)
            {
                moonGO.transform.Rotate(new Vector3(1f, 0.75f, 0.8f) * Time.timeScale / 100f);
            }
        }
    }
}
