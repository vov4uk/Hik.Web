using System.ComponentModel;

namespace Hik.DataAccess.Enums
{
    public enum HdAttributes : byte
    {
        [Description("Default")]
        Default = 0,
        [Description("Redundancy (back up important data)")]
        Redundancy = 1,
        [Description("Read only")]
        ReadOnly = 2,
        [Description("Archiving")]
        Archiving = 3,
        [Description("Cannot be read/read")]
        CannotRead = 4
    }
}
