using UnityEngine;

namespace MoonMod
{
    /// <summary>
    /// One gravity source, as the pullers publish it and <see cref="Moon"/> reads
    /// it back: where it is, how hard it pulls, and the two radii that shape the
    /// falloff.
    /// </summary>
    /// <remarks>
    /// Both a gravity gun's fired sphere and a moon block register one of these in
    /// <see cref="Mod.GravSpheres"/>, which is what lets a single attraction loop
    /// serve them both.
    /// </remarks>
    public class GS_mapping
    {
        public GameObject gameObject { get; set; }
        public float force { get; set; }
        public float minRadius { get; set; }
        public float maxRadius { get; set; }
    }
}
