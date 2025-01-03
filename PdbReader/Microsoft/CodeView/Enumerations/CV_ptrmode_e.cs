namespace PdbReader.Microsoft.CodeView.Enumerations
{
    internal enum CV_ptrmode_e : byte
    {
        NormalPointer = 0x00, // "normal" pointer
        OldReference = 0x01, // "old" reference
        // YES this is intended OldReference and LeftValueReference are homonyms.
        LeftValueReference = 0x01, // l-value reference
        PointerToMember = 0x02, // pointer to data member
        PointerToMemberFunction = 0x03, // pointer to member function
        RightValueReference = 0x04, // r-value reference
        FirstUnusedPointerMode = 0x05  // first unused pointer mode
    }
}
