//-------------------------------------------------------------------------------------------------
// <copyright file="CLRInterop.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Interop class for mscorwks.dll assembly name APIs.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.CLR.Interop
{
    using System;
    using System.Text;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Interop class for mscorwks.dll assembly name APIs.
    /// </summary>
    internal sealed class CLRInterop
    {
        private static readonly Guid referenceIdentityGuid = new Guid("6eaf5ace-7917-4f3c-b129-e046a9704766");

        /// <summary>
        /// Protect the constructor.
        /// </summary>
        private CLRInterop()
        {
        }

        /// <summary>
        /// Represents a reference to the unique signature of a code object.
        /// </summary>
        [ComImport]
            [Guid("6eaf5ace-7917-4f3c-b129-e046a9704766")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface IReferenceIdentity
        {
            /// <summary>
            /// Get an assembly attribute.
            /// </summary>
            /// <param name="attributeNamespace">Attribute namespace.</param>
            /// <param name="attributeName">Attribute name.</param>
            /// <returns>The assembly attribute.</returns>
            [return: MarshalAs(UnmanagedType.LPWStr)]
            string GetAttribute(
                [In, MarshalAs(UnmanagedType.LPWStr)] string attributeNamespace,
                [In, MarshalAs(UnmanagedType.LPWStr)] string attributeName);

            /// <summary>
            /// Set an assembly attribute.
            /// </summary>
            /// <param name="attributeNamespace">Attribute namespace.</param>
            /// <param name="attributeName">Attribute name.</param>
            /// <param name="attributeValue">Attribute value.</param>
            void SetAttribute(
                [In, MarshalAs(UnmanagedType.LPWStr)] string attributeNamespace,
                [In, MarshalAs(UnmanagedType.LPWStr)] string attributeName,
                [In, MarshalAs(UnmanagedType.LPWStr)] string attributeValue);

            /// <summary>
            /// Get an iterator for the assembly's attributes.
            /// </summary>
            /// <returns>Assembly attribute enumerator.</returns>
            IEnumIDENTITY_ATTRIBUTE EnumAttributes();

            /// <summary>
            /// Clone an IReferenceIdentity.
            /// </summary>
            /// <param name="countOfDeltas">Count of deltas.</param>
            /// <param name="deltas">The deltas.</param>
            /// <returns>Cloned IReferenceIdentity.</returns>
            IReferenceIdentity Clone(
                [In] IntPtr /*SIZE_T*/ countOfDeltas,
                [In, MarshalAs(UnmanagedType.LPArray)] IDENTITY_ATTRIBUTE[] deltas);
        }

        /// <summary>
        /// IEnumIDENTITY_ATTRIBUTE interface.
        /// </summary>
        [ComImport]
            [Guid("9cdaae75-246e-4b00-a26d-b9aec137a3eb")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface IEnumIDENTITY_ATTRIBUTE
        {
            /// <summary>
            /// Gets the next attributes.
            /// </summary>
            /// <param name="celt">Count of elements.</param>
            /// <param name="attributes">Array of attributes being returned.</param>
            /// <returns>The next attribute.</returns>
            uint Next(
                [In] uint celt,
                [Out, MarshalAs(UnmanagedType.LPArray)] IDENTITY_ATTRIBUTE[] attributes);

            /// <summary>
            /// Copy the current attribute into a buffer.
            /// </summary>
            /// <param name="available">Number of available bytes.</param>
            /// <param name="data">Buffer into which attribute should be written.</param>
            /// <returns>Pointer to buffer containing the attribute.</returns>
            IntPtr CurrentIntoBuffer(
                [In] IntPtr /*SIZE_T*/ available,
                [Out, MarshalAs(UnmanagedType.LPArray)] byte[] data);

            /// <summary>
            /// Skip past a number of elements.
            /// </summary>
            /// <param name="celt">Count of elements to skip.</param>
            void Skip([In] uint celt);

            /// <summary>
            /// Reset the enumeration to the beginning.
            /// </summary>
            void Reset();

            /// <summary>
            /// Clone this attribute enumeration.
            /// </summary>
            /// <returns>Clone of a IEnumIDENTITY_ATTRIBUTE.</returns>
            IEnumIDENTITY_ATTRIBUTE Clone();
        }

        /// <summary>
        /// Gets the guid.
        /// </summary>
        public static Guid ReferenceIdentityGuid
        {
            get { return referenceIdentityGuid; }
        }

        /// <summary>
        /// Gets an interface pointer to an object with the specified IID, in the assembly at the specified file path.
        /// </summary>
        /// <param name="wszAssemblyPath">A valid path to the requested assembly.</param>
        /// <param name="riid">The IID of the interface to return.</param>
        /// <param name="i">The returned interface pointer.</param>
        /// <returns>The error code.</returns>
        [DllImport("mscorwks.dll", CharSet = CharSet.Unicode, EntryPoint = "GetAssemblyIdentityFromFile")]
        internal static extern uint GetAssemblyIdentityFromFile(System.String wszAssemblyPath, ref Guid riid, out IReferenceIdentity i);

        /// <summary>
        /// Assembly attributes. Contains data about an IReferenceIdentity.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct IDENTITY_ATTRIBUTE
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string AttributeNamespace;
            [MarshalAs(UnmanagedType.LPWStr)] public string AttributeName;
            [MarshalAs(UnmanagedType.LPWStr)] public string AttributeValue;
        }
    }
}
