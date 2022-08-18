using System.Runtime.InteropServices;

namespace PdbDownloader
{
    [StructLayout(LayoutKind.Explicit)]
    internal class IMAGE_DIRECTORY_ENTRY_SECURITY
    {
        /// <summary>Specifies the length of the attribute certificate entry.
        /// The length includes both the header part and the raw data.</summary>
        [FieldOffset(0)]
        internal uint dwLength;
        /// <summary>Contains the certificate version number.</summary>
        [FieldOffset(4)]
        internal ushort wRevision;
        /// <summary>Specifies the type of content in bCertificate.</summary>
        [FieldOffset(6)]
        internal _CertificateType wCertificateType;
        ///// <summary>Contains a certificate, such as an Authenticode signature.</summary>
        ///// <remarks>Actually this is an array ob bytes which exact length should be
        ///// derived from the <see cref="dwLength"/> member.</remarks>
        //[FieldOffset(8)]
        //internal byte bCertificate;

        internal enum _CertificateType : ushort
        {
            /// <summary>The bCertificate member contains an X.509 certificate.</summary>
            X509 = 0x0001,
            /// <summary>The bCertificate member contains a PKCS SignedData structure.</summary>
            PKCSSignedData = 0x0002,
            /// <summary>The bCertificate member contains PKCS1_MODULE_SIGN fields.</summary>
            PKCS1ModuleSign = 0x0009
        }
    }
}
