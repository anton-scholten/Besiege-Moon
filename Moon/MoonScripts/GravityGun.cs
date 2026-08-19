using System.Xml.Serialization;
using Modding.Modules;

namespace MoonMod
{
    /// <summary>The block module GravityGun.xml deserialises into.</summary>
    /// <remarks>This name, the AddBlockModule call in <see cref="Mod.OnLoad"/> and the
    /// element in GravityGun.xml must agree. See AGENTS.md.</remarks>
    [XmlRoot("GravityGun")]
    public class GravityGun : BlockModule
    {
        public string Text { get; set; }
    }
}
