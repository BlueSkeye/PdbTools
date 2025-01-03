namespace PdbReader.Microsoft.CodeView.Enumerations
{
    [Flags()]
    internal enum CV_prop_t : ushort
    {
        Packed = 0x0001, // true if structure is packed
        HasConstructor = 0x0002, // true if constructors or destructors present
        HasOverloadedOperator = 0x0004, // true if overloaded operators present
        IsNested = 0x0008, // true if this is a nested class
        HasNestedType = 0x0010, // true if this class contains nested types
        HasOverloadedAssignment = 0x0020, // true if overloaded assignment (=)
        HasCastingMethod = 0x0040, // true if casting methods
        HasForwardReference = 0x0080, // true if forward reference (incomplete defn)
        IsScopedDefinition = 0x0100, // scoped definition
        HasUniqueName = 0x0200, // true if there is a decorated name following the regular name
        IsSealed = 0x0400, // true if class cannot be used as a base class
        HFA = 0x1800, // CV_HFA_e
        IsIntrinsic = 0x2000, // true if class is an intrinsic type (e.g. __m128d)
        MOCOMUDT = 0xC000 // CV_MOCOM_UDT_e
    }
}
