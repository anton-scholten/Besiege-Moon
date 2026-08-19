using System.Xml.Serialization;
using Modding.Modules;

namespace MoonMod
{
    /// <summary>The block module MoonBlock.xml deserialises into.</summary>
    /// <remarks>This name, the AddBlockModule call in <see cref="Mod.OnLoad"/> and the
    /// element in MoonBlock.xml must agree. See AGENTS.md.</remarks>
    [XmlRoot("MoonBlock")]
    public class MoonBlock : BlockModule
    {
        public string Text { get; set; }
    }
}
