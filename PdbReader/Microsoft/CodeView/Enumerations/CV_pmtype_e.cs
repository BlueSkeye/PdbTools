namespace PdbReader.Microsoft.CodeView.Enumerations
{
    internal enum CV_pmtype_e : ushort
    {
        Undefined = 0x00, // not specified (pre VC8)
        MemberDataSingleInheritance = 0x01, // member data, single inheritance
        MemberDataMultipleInheritance = 0x02, // member data, multiple inheritance
        MemberDataVirtual = 0x03, // member data, virtual inheritance
        MemberDataGeneral = 0x04, // member data, most general
        MemberFunctionSingleInheritance = 0x05, // member function, single inheritance
        MemberFunctionMultipleInheritanc = 0x06, // member function, multiple inheritance
        MemberFunctionVirtual = 0x07, // member function, virtual inheritance
        MemberFunctionGeneral = 0x08, // member function, most general
    }
}
