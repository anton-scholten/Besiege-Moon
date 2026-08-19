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
    /// Mod entry point: registers the two block modules, owns the list of live
    /// gravity sources, and installs the console commands.
    /// </summary>
    public class Mod : ModEntryPoint
    {
        /// <summary>
        /// Whether altitude thins gravity, drag and ambient light. Off by default;
        /// toggled with the <c>atmosphere</c> console command.
        /// </summary>
        public static bool atmoEffects = false;

        /// <summary>Altitude at which the atmosphere starts to thin.</summary>
        public static float minAltitude = 750f;

        /// <summary>Altitude at which gravity has fallen to nothing.</summary>
        public static float maxAltitude = 1000f;

        /// <summary>
        /// Every live gravity source, keyed by its GameObject's instance id.
        /// Written by the gravity guns and the moon blocks, read by every
        /// <see cref="Moon"/> riding a rigidbody. One shared list is what lets a
        /// single attraction loop serve both kinds of attractor.
        /// </summary>
        public static Dictionary<string, GS_mapping> GravSpheres = new Dictionary<string, GS_mapping>();

        // What the atmosphere overwrites is global -- gravity and the ambient
        // lighting belong to the level, not to any one body -- so it is captured
        // and restored here rather than by each Moon. The 2018 build never put any
        // of it back: one flight above maxAltitude left the whole session at zero
        // gravity and pitch dark, build area and later levels included.
        private static bool atmoCaptured;
        private static Vector3 baseGravity = new Vector3(0f, -32.81f, 0f);
        private static Color baseAmbientLight;
        private static float baseAmbientIntensity;

        /// <summary>The level's own gravity, which the atmosphere scales down from.</summary>
        public static Vector3 BaseGravity
        {
            get { return baseGravity; }
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

        /// <summary>Puts them back, at simulation stop, from whichever behaviour notices first.</summary>
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

        public override void OnLoad()
        {
            CustomModules.AddBlockModule<GravityGun, GravityGunBehaviour>("GravityGun", false);
            CustomModules.AddBlockModule<MoonBlock, MoonBlockBehaviour>("MoonBlock", false);

            // A rigidbody only feels the attraction if it carries a Moon, and they
            // arrive by four routes: a block placed in the build area, a level
            // entity, a level being loaded, and a scene change. The immediate call
            // covers the scene that is already up when the mod loads.
            Events.OnBlockInit += BlockPlacedHandler;
            Events.OnEntityPlaced += EntityPlacedHandler;
            Events.OnLevelLoaded += LevelLoadedHandlers;
            SceneManager.sceneLoaded += SceneLoadedHandlers;
            SceneLoadedHandlers(SceneManager.GetActiveScene(), LoadSceneMode.Additive);

            ModConsole.RegisterCommand("atmosphere", CH_active, "Activate or deactivate the effects of the atmosphere. ");
            ModConsole.RegisterCommand("minAltitude", CH_minAltitude, "Altitude at which the gravity starts to decrease. ");
            ModConsole.RegisterCommand("maxAltitude", CH_maxAltitude, "Altitude at which the gravity is 0. ");
        }

        private void BlockPlacedHandler(Block block)
        {
            if (block.GameObject.GetComponent<Moon>() == null)
            {
                block.GameObject.AddComponent<Moon>();
            }
        }

        private void EntityPlacedHandler(Entity entity)
        {
            if (entity.GameObject.GetComponent<Moon>() == null)
            {
                entity.GameObject.AddComponent<Moon>();
            }
        }

        private void LevelLoadedHandlers(Level level)
        {
            Rigidbody[] bodies = Object.FindObjectsOfType<Rigidbody>();
            foreach (Rigidbody body in bodies)
            {
                if (body.gameObject.GetComponent<Moon>() == null)
                {
                    body.gameObject.AddComponent<Moon>();
                }
            }
        }

        private void SceneLoadedHandlers(Scene scene, LoadSceneMode mode)
        {
            Rigidbody[] bodies = Object.FindObjectsOfType<Rigidbody>();
            foreach (Rigidbody body in bodies)
            {
                if (body.gameObject.GetComponent<Moon>() == null)
                {
                    body.gameObject.AddComponent<Moon>();
                }
            }
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
