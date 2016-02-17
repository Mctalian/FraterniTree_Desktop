using System;

namespace FraterniTree.Enums
{
    [Flags]
    public enum FieldEdit
    {
        None = 0x0,
        FirstName = 0x1,
        LastName = 0x2,
        Big = 0x4,
        Littles = 0x8,
        IniMonth = 0x10,
        IniYear = 0x11,
        Active = 0x12,
        AllMask = 0xFF
    }
}
