using System.Xml.Serialization;
using Modding.Modules;

namespace MoonMod
{
    /// <summary>The block module <c>GravityGun.xml</c> deserialises into.</summary>
    /// <remarks>
    /// The element name here, the <c>AddBlockModule</c> call in
    /// <see cref="Mod.OnLoad"/> and the <c>&lt;GravityGun&gt;</c> element in
    /// GravityGun.xml all have to agree. See AGENTS.md.
    /// </remarks>
    [XmlRoot("GravityGun")]
    public class GravityGun : BlockModule
    {
        public string Text { get; set; }
    }
}
