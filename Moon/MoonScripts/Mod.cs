using System.Collections.Generic;
using Modding;
using Modding.Blocks;
using Modding.Levels;
using Modding.Modules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoonMod
{
    /// <summary>
    /// Entry point. Registers the two block modules, owns the shared list of live
    /// gravity sources, and installs the console commands.
    /// </summary>
    public class Mod : ModEntryPoint
    {
        /// <summary>Whether altitude thins gravity, drag and light. Set by the <c>atmosphere</c> command.</summary>
        public static bool atmoEffects = false;

        /// <summary>Altitude at which the atmosphere starts to thin.</summary>
        public static float minAltitude = 750f;

        /// <summary>Altitude at which gravity has fallen to nothing.</summary>
        public static float maxAltitude = 1000f;

        /// <summary>
        /// Every live gravity source, keyed by its GameObject's instance id. One
        /// shared list is what lets a single attraction loop serve both the gravity
        /// gun's fired spheres and the moon blocks.
        /// </summary>
        public static Dictionary<string, GS_mapping> GravSpheres = new Dictionary<string, GS_mapping>();

        // Gravity and ambient lighting belong to the level, not to any one body, so
        // the atmosphere's edits are captured and undone here rather than by each
        // Moon. See AGENTS.md: the 2018 build never undid them at all.
        private static bool atmoCaptured;
        private static Vector3 baseGravity = new Vector3(0f, -32.81f, 0f);
        private static Color baseAmbientLight;
        private static float baseAmbientIntensity;

        /// <summary>The level's own gravity, which the atmosphere scales down from.</summary>
        public static Vector3 BaseGravity
        {
            get { return baseGravity; }
        }

        public override void OnLoad()
        {
            CustomModules.AddBlockModule<GravityGun, GravityGunBehaviour>("GravityGun", false);
            CustomModules.AddBlockModule<MoonBlock, MoonBlockBehaviour>("MoonBlock", false);

            // A body only feels the attraction if it carries a Moon, and bodies
            // arrive by four routes. The immediate call covers the scene already up
            // when the mod loads.
            Events.OnBlockInit += BlockPlacedHandler;
            Events.OnEntityPlaced += EntityPlacedHandler;
            Events.OnLevelLoaded += LevelLoadedHandler;
            SceneManager.sceneLoaded += SceneLoadedHandler;
            AddMoonToEveryBody();

            ModConsole.RegisterCommand("atmosphere", CH_active, "Activate or deactivate the effects of the atmosphere. ");
            ModConsole.RegisterCommand("minAltitude", CH_minAltitude, "Altitude at which the gravity starts to decrease. ");
            ModConsole.RegisterCommand("maxAltitude", CH_maxAltitude, "Altitude at which the gravity is 0. ");
        }

        /// <summary>Publishes one attractor. Replaces any entry under the same key rather than throwing.</summary>
        public static void Register(GameObject go, float force, float minRadius, float maxRadius)
        {
            GS_mapping mapping = new GS_mapping();
            mapping.gameObject = go;
            mapping.force = force;
            mapping.minRadius = minRadius;
            mapping.maxRadius = maxRadius;
            GravSpheres[go.GetInstanceID().ToString()] = mapping;
        }

        public static void Unregister(GameObject go)
        {
            GravSpheres.Remove(go.GetInstanceID().ToString());
        }

        /// <summary>The component of type T on <paramref name="go"/>, adding one if it has none.</summary>
        public static T Ensure<T>(GameObject go) where T : Component
        {
            // Compared through Component so Unity's == overload is used; a bare type
            // parameter would get a plain reference comparison instead.
            T component = go.GetComponent<T>();
            if ((Component)component == null)
            {
                component = go.AddComponent<T>();
            }
            return component;
        }

        /// <summary>Writes both shader colour slots: the additive shader reads _TintColor, the standard one _Color.</summary>
        public static void SetTint(Renderer renderer, Color color)
        {
            renderer.material.SetColor("_Color", color);
            renderer.material.SetColor("_TintColor", color);
        }

        /// <summary>Remembers the level's gravity and lighting, once, before the atmosphere first touches them.</summary>
        public static void CaptureAtmosphere()
        {
            if (atmoCaptured)
            {
                return;
            }
            baseGravity = Physics.gravity;
            baseAmbientLight = RenderSettings.ambientLight;
            baseAmbientIntensity = RenderSettings.ambientIntensity;
            atmoCaptured = true;
        }

        /// <summary>Puts them back at simulation stop, from whichever behaviour notices first.</summary>
        public static void RestoreAtmosphere()
        {
            if (!atmoCaptured)
            {
                return;
            }
            // Same carve-out as when it was applied: gravity the player turned off
            // themselves is not ours to write.
            if (!StatMaster.GodTools.GravityDisabled)
            {
                Physics.gravity = baseGravity;
            }
            RenderSettings.ambientLight = baseAmbientLight;
            RenderSettings.ambientIntensity = baseAmbientIntensity;
            atmoCaptured = false;
        }

        private static void AddMoon(GameObject go)
        {
            if (go.GetComponent<Moon>() == null)
            {
                go.AddComponent<Moon>();
            }
        }

        private static void AddMoonToEveryBody()
        {
            Rigidbody[] bodies = Object.FindObjectsOfType<Rigidbody>();
            foreach (Rigidbody body in bodies)
            {
                AddMoon(body.gameObject);
            }
        }

        private void BlockPlacedHandler(Block block)
        {
            AddMoon(block.GameObject);
        }

        private void EntityPlacedHandler(Entity entity)
        {
            AddMoon(entity.GameObject);
        }

        // Two handlers rather than one because they are two delegate types.
        private void LevelLoadedHandler(Level level)
        {
            AddMoonToEveryBody();
        }

        private void SceneLoadedHandler(Scene scene, LoadSceneMode mode)
        {
            AddMoonToEveryBody();
        }

        private void CH_active(string[] values)
        {
            atmoEffects = values[0] == "true";
            ModConsole.Log("Moon effects: " + (atmoEffects ? "ON" : "OFF"));
        }

        private void CH_minAltitude(string[] values)
        {
            minAltitude = float.Parse(values[0]);
            ModConsole.Log("Min altitude set to: " + minAltitude.ToString());
        }

        private void CH_maxAltitude(string[] values)
        {
            maxAltitude = float.Parse(values[0]);
            ModConsole.Log("Max altitude set to: " + maxAltitude.ToString());
        }
    }
}
