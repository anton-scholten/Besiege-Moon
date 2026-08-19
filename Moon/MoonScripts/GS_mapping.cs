using UnityEngine;

namespace MoonMod
{
    /// <summary>
    /// One gravity source as the pullers publish it and <see cref="Moon"/> reads it
    /// back: where it is, how hard it pulls, and the two radii shaping the falloff.
    /// </summary>
    public class GS_mapping
    {
        public GameObject gameObject { get; set; }
        public float force { get; set; }
        public float minRadius { get; set; }
        public float maxRadius { get; set; }
    }
}
