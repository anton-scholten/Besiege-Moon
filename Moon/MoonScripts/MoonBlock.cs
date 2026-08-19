using System.Xml.Serialization;
using Modding.Modules;

namespace MoonMod
{
    /// <summary>The block module <c>MoonBlock.xml</c> deserialises into.</summary>
    /// <remarks>
    /// The element name here, the <c>AddBlockModule</c> call in
    /// <see cref="Mod.OnLoad"/> and the <c>&lt;MoonBlock&gt;</c> element in
    /// MoonBlock.xml all have to agree. See AGENTS.md.
    /// </remarks>
    [XmlRoot("MoonBlock")]
    public class MoonBlock : BlockModule
    {
        public string Text { get; set; }
    }
}
