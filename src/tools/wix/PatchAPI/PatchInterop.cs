//-------------------------------------------------------------------------------------------------
// <copyright file="PatchInterop.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Interop class for the mspatchc.dll. So far, only implements what is needed for delta MSP creation.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.PatchAPI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Interop class for the mspatchc.dll.
    /// </summary>
    internal static class PatchInterop
    {
        // From WinError.h in the Platform SDK
        internal const ushort FACILITY_WIN32 = 7;

        /// <summary>
        /// Parse a number from text in either hex or decimal.
        /// </summary>
        /// <param name="source">Source value. Treated as hex if it starts 0x (or 0X), decimal otherwise.</param>
        /// <returns>Numeric value that source represents.</returns>
        static internal UInt32 ParseHexOrDecimal(string source)
        {
            string value = source.Trim();
            if (String.Equals(value.Substring(0,2), "0x", StringComparison.OrdinalIgnoreCase))
            {
                return UInt32.Parse(value.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture.NumberFormat);
            }
            else
            {
                return UInt32.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
            }
        }

        /// <summary>
        /// Create a binary delta file.
        /// </summary>
        /// <param name="deltaFile">Name of the delta file to create.</param>
        /// <param name="targetFile">Name of updated file.</param>
        /// <param name="targetSymbolPath">Optional paths to updated file's symbols.</param>
        /// <param name="targetRetainOffsets">Optional offsets to the delta retain sections in the updated file.</param>
        /// <param name="basisFiles">Optional array of target files.</param>
        /// <param name="basisSymbolPaths">Optional array of target files' symbol paths (must match basisFiles array).</param>
        /// <param name="basisIgnoreLengths">Optional array of target files' delta ignore section lengths (must match basisFiles array)(each entry must match basisIgnoreOffsets entries).</param>
        /// <param name="basisIgnoreOffsets">Optional array of target files' delta ignore section offsets (must match basisFiles array)(each entry must match basisIgnoreLengths entries).</param>
        /// <param name="basisRetainLengths">Optional array of target files' delta protect section lengths (must match basisFiles array)(each entry must match basisRetainOffsets and targetRetainOffsets entries).</param>
        /// <param name="basisRetainOffsets">Optional array of target files' delta protect section offsets (must match basisFiles array)(each entry must match basisRetainLengths and targetRetainOffsets entries).</param>
        /// <param name="apiPatchingSymbolFlags">ApiPatchingSymbolFlags value.</param>
        /// <param name="optimizePatchSizeForLargeFiles">OptimizePatchSizeForLargeFiles value.</param>
        /// <param name="retainRangesIgnored">Flag to indicate retain ranges were ignored due to mismatch.</param>
        /// <returns>true if delta file was created, false if whole file should be used instead.</returns>
        static public bool CreateDelta(
                string deltaFile,
                string targetFile,
                string targetSymbolPath,
                string targetRetainOffsets,
                string[] basisFiles,
                string[] basisSymbolPaths,
                string[] basisIgnoreLengths,
                string[] basisIgnoreOffsets,
                string[] basisRetainLengths,
                string[] basisRetainOffsets,
                PatchSymbolFlagsType apiPatchingSymbolFlags,
                bool optimizePatchSizeForLargeFiles,
                out bool retainRangesIgnored
                )
        {
            retainRangesIgnored = false;
            if (0 != (apiPatchingSymbolFlags & ~(PatchSymbolFlagsType.PATCH_SYMBOL_NO_IMAGEHLP | PatchSymbolFlagsType.PATCH_SYMBOL_NO_FAILURES | PatchSymbolFlagsType.PATCH_SYMBOL_UNDECORATED_TOO)))
            {
                throw new ArgumentOutOfRangeException("apiPatchingSymbolFlags");
            }

            if (null == deltaFile || 0 == deltaFile.Length)
            {
                throw new ArgumentNullException("deltaFile");
            }

            if (null == targetFile || 0 == targetFile.Length)
            {
                throw new ArgumentNullException("targetFile");
            }

            if (null == basisFiles || 0 == basisFiles.Length)
            {
                return false;
            }
            uint countOldFiles = (uint) basisFiles.Length;

            if (null != basisSymbolPaths)
            {
                if (0 != basisSymbolPaths.Length)
                {
                    if ((uint) basisSymbolPaths.Length != countOldFiles)
                    {
                        throw new ArgumentOutOfRangeException("basisSymbolPaths");
                    }
                }
            }
            // a null basisSymbolPaths is allowed.

            if (null != basisIgnoreLengths)
            {
                if (0 != basisIgnoreLengths.Length)
                {
                    if ((uint) basisIgnoreLengths.Length != countOldFiles)
                    {
                        throw new ArgumentOutOfRangeException("basisIgnoreLengths");
                    }
                }
            }
            else
            {
                basisIgnoreLengths = new string[countOldFiles];
            }

            if (null != basisIgnoreOffsets)
            {
                if (0 != basisIgnoreOffsets.Length)
                {
                    if ((uint) basisIgnoreOffsets.Length != countOldFiles)
                    {
                        throw new ArgumentOutOfRangeException("basisIgnoreOffsets");
                    }
                }
            }
            else
            {
                basisIgnoreOffsets = new string[countOldFiles];
            }

            if (null != basisRetainLengths)
            {
                if (0 != basisRetainLengths.Length)
                {
                    if ((uint) basisRetainLengths.Length != countOldFiles)
                    {
                        throw new ArgumentOutOfRangeException("basisRetainLengths");
                    }
                }
            }
            else
            {
                basisRetainLengths = new string[countOldFiles];
            }

            if (null != basisRetainOffsets)
            {
                if (0 != basisRetainOffsets.Length)
                {
                    if ((uint) basisRetainOffsets.Length != countOldFiles)
                    {
                        throw new ArgumentOutOfRangeException("basisRetainOffsets");
                    }
                }
            }
            else
            {
                basisRetainOffsets = new string[countOldFiles];
            }

            PatchOptionData pod = new PatchOptionData();
            pod.symbolOptionFlags = apiPatchingSymbolFlags;
            pod.newFileSymbolPath = targetSymbolPath;
            pod.oldFileSymbolPathArray = basisSymbolPaths;
            pod.extendedOptionFlags = 0;
            PatchOldFileInfoW[] oldFileInfoArray = new PatchOldFileInfoW[countOldFiles];
            string[] newRetainOffsetArray = ((null == targetRetainOffsets) ? new string[0] : targetRetainOffsets.Split(','));
            for (uint i = 0; i < countOldFiles; ++i)
            {
                PatchOldFileInfoW ofi = new PatchOldFileInfoW();
                ofi.oldFileName = basisFiles[i];
                string[] ignoreLengthArray = ((null == basisIgnoreLengths[i]) ? new string[0] : basisIgnoreLengths[i].Split(','));
                string[] ignoreOffsetArray = ((null == basisIgnoreOffsets[i]) ? new string[0] : basisIgnoreOffsets[i].Split(','));
                string[] retainLengthArray = ((null == basisRetainLengths[i]) ? new string[0] : basisRetainLengths[i].Split(','));
                string[] retainOffsetArray = ((null == basisRetainOffsets[i]) ? new string[0] : basisRetainOffsets[i].Split(','));
                // Validate inputs
                if (ignoreLengthArray.Length != ignoreOffsetArray.Length)
                {
                    throw new ArgumentOutOfRangeException("basisIgnoreLengths");
                }

                if (retainLengthArray.Length != retainOffsetArray.Length)
                {
                    throw new ArgumentOutOfRangeException("basisRetainLengths");
                }

                if (newRetainOffsetArray.Length != retainOffsetArray.Length)
                {
                    // remove all retain range information
                    retainRangesIgnored = true;
                    for (uint j = 0; j < countOldFiles; ++j)
                    {
                        basisRetainLengths[j] = null;
                        basisRetainOffsets[j] = null;
                    }
                    retainLengthArray = new string[0];
                    retainOffsetArray = new string[0];
                    newRetainOffsetArray = new string[0];
                    for (uint j = 0; j < oldFileInfoArray.Length; ++j)
                    {
                        oldFileInfoArray[j].retainRange = null;
                    }
                }

                // Populate IgnoreRange structure
                PatchIgnoreRange[] ignoreArray = null;
                if (0 != ignoreLengthArray.Length)
                {
                    ignoreArray = new PatchIgnoreRange[ignoreLengthArray.Length];
                    for (int j = 0; j < ignoreLengthArray.Length; ++j)
                    {
                        PatchIgnoreRange ignoreRange = new PatchIgnoreRange();
                        ignoreRange.offsetInOldFile = ParseHexOrDecimal(ignoreOffsetArray[j]);
                        ignoreRange.lengthInBytes = ParseHexOrDecimal(ignoreLengthArray[j]);
                        ignoreArray[j] = ignoreRange;
                    }
                    ofi.ignoreRange = ignoreArray;
                }

                PatchRetainRange[] retainArray = null;
                if (0 != newRetainOffsetArray.Length)
                {
                    retainArray = new PatchRetainRange[retainLengthArray.Length];
                    for (int j = 0; j < newRetainOffsetArray.Length; ++j)
                    {
                        PatchRetainRange retainRange = new PatchRetainRange();
                        retainRange.offsetInOldFile = ParseHexOrDecimal(retainOffsetArray[j]);
                        retainRange.lengthInBytes = ParseHexOrDecimal(retainLengthArray[j]);
                        retainRange.offsetInNewFile = ParseHexOrDecimal(newRetainOffsetArray[j]);
                        retainArray[j] = retainRange;
                    }
                    ofi.retainRange = retainArray;
                }
                oldFileInfoArray[i] = ofi;
            }

            if (CreatePatchFileExW(
                    countOldFiles,
                    oldFileInfoArray,
                    targetFile,
                    deltaFile,
                    PatchOptionFlags(optimizePatchSizeForLargeFiles),
                    pod,
                    null,
                    IntPtr.Zero))
            {
                return true;
            }

            // determine if this is an error or a need to use whole file.
            int err = Marshal.GetLastWin32Error();
            switch(err)
            {
            case unchecked((int) ERROR_PATCH_BIGGER_THAN_COMPRESSED):
                break;

            // too late to exclude this file -- should have been caught before
            case unchecked((int) ERROR_PATCH_SAME_FILE):
            default:
                throw new System.ComponentModel.Win32Exception(err);
            }
            return false;
        }

        /// <summary>
        /// Extract the delta header.
        /// </summary>
        /// <param name="delta">Name of delta file.</param>
        /// <param name="deltaHeader">Name of file to create with the delta's header.</param>
        static public void ExtractDeltaHeader(string delta, string deltaHeader)
        {
            if (!ExtractPatchHeaderToFileW(delta, deltaHeader))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        /// Returns the PatchOptionFlags to use.
        /// </summary>
        /// <param name="optimizeForLargeFiles">True if optimizing for large files.</param>
        /// <returns>PATCH_OPTION_FLAG values</returns>
        static private UInt32 PatchOptionFlags(bool optimizeForLargeFiles)
        {
            UInt32 flags = PATCH_OPTION_FAIL_IF_SAME_FILE | PATCH_OPTION_FAIL_IF_BIGGER | PATCH_OPTION_USE_LZX_BEST;
            if (optimizeForLargeFiles)
            {
                flags |= PATCH_OPTION_USE_LZX_LARGE;
            }
            return flags;
        }

        //---------------------------------------------------------------------
        // From PatchApi.h
        //---------------------------------------------------------------------

        //
        // The following contants can be combined and used as the OptionFlags
        // parameter in the patch creation apis.

        internal const uint PATCH_OPTION_USE_BEST          = 0x00000000; // auto choose best (slower)

        internal const uint PATCH_OPTION_USE_LZX_BEST      = 0x00000003; // auto choose best of LXZ A/B (but not large)
        internal const uint PATCH_OPTION_USE_LZX_A         = 0x00000001; // normal
        internal const uint PATCH_OPTION_USE_LXZ_B         = 0x00000002; // better on some x86 binaries
        internal const uint PATCH_OPTION_USE_LZX_LARGE     = 0x00000004; // better support for large files (requires 5.1 or higher applyer)

        internal const uint PATCH_OPTION_NO_BINDFIX        = 0x00010000; // PE bound imports
        internal const uint PATCH_OPTION_NO_LOCKFIX        = 0x00020000; // PE smashed locks
        internal const uint PATCH_OPTION_NO_REBASE         = 0x00040000; // PE rebased image
        internal const uint PATCH_OPTION_FAIL_IF_SAME_FILE = 0x00080000; // don't create if same
        internal const uint PATCH_OPTION_FAIL_IF_BIGGER    = 0x00100000; // fail if patch is larger than simply compressing new file (slower)
        internal const uint PATCH_OPTION_NO_CHECKSUM       = 0x00200000; // PE checksum zero
        internal const uint PATCH_OPTION_NO_RESTIMEFIX     = 0x00400000; // PE resource timestamps
        internal const uint PATCH_OPTION_NO_TIMESTAMP      = 0x00800000; // don't store new file timestamp in patch
        internal const uint PATCH_OPTION_SIGNATURE_MD5     = 0x01000000; // use MD5 instead of CRC (reserved for future support)
        internal const uint PATCH_OPTION_INTERLEAVE_FILES  = 0x40000000; // better support for large files (requires 5.2 or higher applyer)
        internal const uint PATCH_OPTION_RESERVED1         = 0x80000000; // (used internally)

        internal const uint PATCH_OPTION_VALID_FLAGS       = 0xC0FF0007;

        //
        // The following flags are used with PATCH_OPTION_DATA SymbolOptionFlags:
        //

        [Flags]
        public enum PatchSymbolFlagsType :uint
        {
            PATCH_SYMBOL_NO_IMAGEHLP       = 0x00000001, // don't use imagehlp.dll
            PATCH_SYMBOL_NO_FAILURES       = 0x00000002, // don't fail patch due to imagehlp failures
            PATCH_SYMBOL_UNDECORATED_TOO   = 0x00000004, // after matching decorated symbols, try to match remaining by undecorated names
            PATCH_SYMBOL_RESERVED1         = 0x80000000, // (used internally)
            MaxValue = PATCH_SYMBOL_NO_IMAGEHLP | PATCH_SYMBOL_NO_FAILURES | PATCH_SYMBOL_UNDECORATED_TOO
        }

        //
        // The following flags are used with PATCH_OPTION_DATA ExtendedOptionFlags:
        //

        internal const uint PATCH_TRANSFORM_PE_RESOURCE_2  = 0x00000100; // better handling of PE resources (requires 5.2 or higher applyer)
        internal const uint PATCH_TRANSFORM_PE_IRELOC_2    = 0x00000200; // better handling of PE stripped relocs (requires 5.2 or higher applyer)

        //
        // In addition to the standard Win32 error codes, the following error codes may
        // be returned via GetLastError() when one of the patch APIs fails.

        internal const uint ERROR_PATCH_ENCODE_FAILURE         = 0xC00E3101; // create
        internal const uint ERROR_PATCH_INVALID_OPTIONS        = 0xC00E3102; // create
        internal const uint ERROR_PATCH_SAME_FILE              = 0xC00E3103; // create
        internal const uint ERROR_PATCH_RETAIN_RANGES_DIFFER   = 0xC00E3104; // create
        internal const uint ERROR_PATCH_BIGGER_THAN_COMPRESSED = 0xC00E3105; // create
        internal const uint ERROR_PATCH_IMAGEHLP_FALURE        = 0xC00E3106; // create

        /// <summary>
        /// Delegate type that the PatchAPI calls for progress notification.
        /// </summary>
        /// <param name="context">.</param>
        /// <param name="currentPosition">.</param>
        /// <param name="maxPosition">.</param>
        /// <returns>True for success</returns>
        public delegate bool PatchProgressCallback(
                IntPtr context,
                uint currentPosition,
                uint maxPosition
                );

        /// <summary>
        /// Delegate type that the PatchAPI calls for patch symbol load information.
        /// </summary>
        /// <param name="whichFile">.</param>
        /// <param name="symbolFileName">.</param>
        /// <param name="symType">.</param>
        /// <param name="symbolFileCheckSum">.</param>
        /// <param name="symbolFileTimeDate">.</param>
        /// <param name="imageFileCheckSum">.</param>
        /// <param name="imageFileTimeDate">.</param>
        /// <param name="context">.</param>
        /// <returns>???</returns>
        public delegate bool PatchSymloadCallback(
                                                 uint whichFile, // 0 for new file, 1 for first old file, etc
                [MarshalAs(UnmanagedType.LPStr)] string symbolFileName,
                                                 uint symType,   // see SYM_TYPE in imagehlp.h
                                                 uint symbolFileCheckSum,
                                                 uint symbolFileTimeDate,
                                                 uint imageFileCheckSum,
                                                 uint imageFileTimeDate,
                                                 IntPtr context
                                                 );

        /// <summary>
        /// Wraps PATCH_IGNORE_RANGE
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal class PatchIgnoreRange
        {
            public uint offsetInOldFile;
            public uint lengthInBytes;
        }

        /// <summary>
        /// Wraps PATCH_RETAIN_RANGE
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal class PatchRetainRange
        {
            public uint offsetInOldFile;
            public uint lengthInBytes;
            public uint offsetInNewFile;
        }

        /// <summary>
        /// Wraps PATCH_OLD_FILE_INFO (except for the OldFile~ portion)
        /// </summary>
        internal class PatchOldFileInfo
        {
            public PatchIgnoreRange[] ignoreRange;
            public PatchRetainRange[] retainRange;
        }

        /// <summary>
        /// Wraps PATCH_OLD_FILE_INFO_W
        /// </summary>
        internal class PatchOldFileInfoW : PatchOldFileInfo
        {
            public string oldFileName;
        }

        /// <summary>
        /// Wraps each PATCH_INTERLEAVE_MAP Range
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses"), StructLayout(LayoutKind.Sequential)]
        internal class PatchInterleaveMapRange
        {
            public uint oldOffset;
            public uint oldLength;
            public uint newLength;
        }

        /// <summary>
        /// Wraps PATCH_INTERLEAVE_MAP
        /// </summary>
        internal class PatchInterleaveMap
        {
            public PatchInterleaveMapRange[] ranges = null;
        }


        /// <summary>
        /// Wraps PATCH_OPTION_DATA
        /// </summary>
        [BestFitMapping(false, ThrowOnUnmappableChar = true)]
        internal class PatchOptionData
        {
            public PatchSymbolFlagsType symbolOptionFlags;          // PATCH_SYMBOL_xxx flags
            [MarshalAs(UnmanagedType.LPStr)] public string               newFileSymbolPath;          // always ANSI, never Unicode
            [MarshalAs(UnmanagedType.LPStr)] public string[]             oldFileSymbolPathArray;     // array[ OldFileCount ]
            public uint                 extendedOptionFlags;
            public PatchSymloadCallback symLoadCallback = null;
            public IntPtr symLoadContext = IntPtr.Zero;
            public PatchInterleaveMap[] interleaveMapArray = null;  // array[ OldFileCount ] (requires 5.2 or higher applyer)
            public uint maxLzxWindowSize = 0;       // limit memory requirements (requires 5.2 or higher applyer)
        }

        //
        // Note that PATCH_OPTION_DATA contains LPCSTR paths, and no LPCWSTR (Unicode)
        // path argument is available, even when used with one of the Unicode APIs
        // such as CreatePatchFileW. This is because the unlerlying system services
        // for symbol file handling (IMAGEHLP.DLL) only support ANSI file/path names.
        //

        //
        // A note about PATCH_RETAIN_RANGE specifiers with multiple old files:
        //
        // Each old version file must have the same RetainRangeCount, and the same
        // retain range LengthInBytes and OffsetInNewFile values in the same order.
        // Only the OffsetInOldFile values can differ between old foles for retain
        // ranges.
        //

        //
        // The following prototypes are (some of the) interfaces for creating patches from files.
        //

        /// <summary>
        /// Creates a new delta.
        /// </summary>
        /// <param name="oldFileCount">Size of oldFileInfoArray.</param>
        /// <param name="oldFileInfoArray">Target file information.</param>
        /// <param name="newFileName">Name of updated file.</param>
        /// <param name="patchFileName">Name of delta to create.</param>
        /// <param name="optionFlags">PATCH_OPTION_xxx.</param>
        /// <param name="optionData">Optional PATCH_OPTION_DATA structure.</param>
        /// <param name="progressCallback">Delegate for progress callbacks.</param>
        /// <param name="context">Context for progress callback delegate.</param>
        /// <returns>true if successfull, sets Marshal.GetLastWin32Error() if not.</returns>
        [DllImport("mspatchc.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreatePatchFileExW(
                uint oldFileCount,      // maximum 255
                [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(PatchAPIMarshaler), MarshalCookie="PATCH_OLD_FILE_INFO_W")]
                PatchOldFileInfoW[] oldFileInfoArray,
                string newFileName,     // input file  (required)
                string patchFileName,   // output file (required)
                uint optionFlags,
                [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(PatchAPIMarshaler), MarshalCookie="PATCH_OPTION_DATA")]
                PatchOptionData optionData,
                [MarshalAs (UnmanagedType.FunctionPtr)]
                PatchProgressCallback progressCallback,
                IntPtr context
                );

        /// <summary>
        /// Extracts delta header from delta.
        /// </summary>
        /// <param name="patchFileName">Name of delta file.</param>
        /// <param name="patchHeaderFileName">Name of file to create with delta header.</param>
        /// <returns>true if successfull, sets Marshal.GetLastWin32Error() if not.</returns>
        [DllImport("mspatchc.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ExtractPatchHeaderToFileW(
                string patchFileName,      // input file
                string patchHeaderFileName // output file
                );

        // TODO: Add rest of APIs to enable custom binders to perform more exhaustive checks

        /// <summary>
        /// Marshals arguments for the CreatePatch~ APIs
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        internal class PatchAPIMarshaler : ICustomMarshaler
        {
            internal static ICustomMarshaler GetInstance(string cookie)
            {
                return new PatchAPIMarshaler(cookie);
            }

            private enum MarshalType
            {
                PATCH_OPTION_DATA,
                PATCH_OLD_FILE_INFO_W
            };
            private PatchAPIMarshaler.MarshalType marshalType;

            private PatchAPIMarshaler(string cookie)
            {
                this.marshalType = (PatchAPIMarshaler.MarshalType) Enum.Parse(typeof(PatchAPIMarshaler.MarshalType), cookie);
            }

            //
            // Summary:
            //     Returns the size of the native data to be marshaled.
            //
            // Returns:
            //     The size in bytes of the native data.
            public int GetNativeDataSize()
            {
                return Marshal.SizeOf(typeof(IntPtr));
            }

            //
            // Summary:
            //     Performs necessary cleanup of the managed data when it is no longer needed.
            //
            // Parameters:
            //     ManagedObj:
            //       The managed object to be destroyed.
            public void CleanUpManagedData(object ManagedObj)
            {
            }

            //
            // Summary:
            //     Performs necessary cleanup of the unmanaged data when it is no longer needed.
            //
            // Parameters:
            //     pNativeData:
            //       A pointer to the unmanaged data to be destroyed.
            public void CleanUpNativeData(IntPtr pNativeData)
            {
                if (IntPtr.Zero == pNativeData)
                {
                    return;
                }

                switch (this.marshalType)
                {
                case PatchAPIMarshaler.MarshalType.PATCH_OPTION_DATA:
                    this.CleanUpPOD(pNativeData);
                    break;
                default:
                    this.CleanUpPOFI_A(pNativeData);
                    break;
                }
            }

            //
            // Summary:
            //     Converts the managed data to unmanaged data.
            //
            // Parameters:
            //     ManagedObj:
            //       The managed object to be converted.
            //
            // Returns:
            //     Returns the COM view of the managed object.
            public IntPtr MarshalManagedToNative(object ManagedObj)
            {
                if (null == ManagedObj)
                {
                    return IntPtr.Zero;
                }

                switch(this.marshalType)
                {
                case PatchAPIMarshaler.MarshalType.PATCH_OPTION_DATA:
                    return this.MarshalPOD(ManagedObj as PatchOptionData);
                case PatchAPIMarshaler.MarshalType.PATCH_OLD_FILE_INFO_W:
                    return this.MarshalPOFIW_A(ManagedObj as PatchOldFileInfoW[]);
                default:
                    throw new InvalidOperationException();
                }
            }


            //
            // Summary:
            //     Converts the unmanaged data to managed data.
            //
            // Parameters:
            //     pNativeData:
            //       A pointer to the unmanaged data to be wrapped.
            //
            // Returns:
            //     Returns the managed view of the COM data.
            public object MarshalNativeToManaged(IntPtr pNativeData)
            {
                return null;
            }

            // Implementation *************************************************

            // PATCH_OPTION_DATA offsets
            private static readonly int symbolOptionFlagsOffset      =   Marshal.SizeOf(typeof(Int32));
            private static readonly int newFileSymbolPathOffset      = 2*Marshal.SizeOf(typeof(Int32));
            private static readonly int oldFileSymbolPathArrayOffset = 2*Marshal.SizeOf(typeof(Int32)) + Marshal.SizeOf(typeof(IntPtr));
            private static readonly int extendedOptionFlagsOffset    = 2*Marshal.SizeOf(typeof(Int32)) + 2*Marshal.SizeOf(typeof(IntPtr));
            private static readonly int symLoadCallbackOffset        = 3*Marshal.SizeOf(typeof(Int32)) + 2*Marshal.SizeOf(typeof(IntPtr));
            private static readonly int symLoadContextOffset         = 3*Marshal.SizeOf(typeof(Int32)) + 3*Marshal.SizeOf(typeof(IntPtr));
            private static readonly int interleaveMapArrayOffset     = 3*Marshal.SizeOf(typeof(Int32)) + 4*Marshal.SizeOf(typeof(IntPtr));
            private static readonly int maxLzxWindowSizeOffset       = 3*Marshal.SizeOf(typeof(Int32)) + 5*Marshal.SizeOf(typeof(IntPtr));
            private static readonly int patchOptionDataSize          = 4*Marshal.SizeOf(typeof(Int32)) + 5*Marshal.SizeOf(typeof(IntPtr));

            // PATCH_OLD_FILE_INFO offsets
            private static readonly int oldFileOffset          =   Marshal.SizeOf(typeof(Int32));
            private static readonly int ignoreRangeCountOffset =   Marshal.SizeOf(typeof(Int32)) + Marshal.SizeOf(typeof(IntPtr));
            private static readonly int ignoreRangeArrayOffset = 2*Marshal.SizeOf(typeof(Int32)) + Marshal.SizeOf(typeof(IntPtr));
            private static readonly int retainRangeCountOffset = 2*Marshal.SizeOf(typeof(Int32)) + 2*Marshal.SizeOf(typeof(IntPtr));
            private static readonly int retainRangeArrayOffset = 3*Marshal.SizeOf(typeof(Int32)) + 2*Marshal.SizeOf(typeof(IntPtr));
            private static readonly int patchOldFileInfoSize   = 3*Marshal.SizeOf(typeof(Int32)) + 3*Marshal.SizeOf(typeof(IntPtr));

            // Methods and data used to preserve data needed for cleanup

            // This dictionary holds the quantity of items internal to each native structure that will need to be freed (the OldFileCount)
            private static readonly Dictionary<IntPtr, int> OldFileCounts = new Dictionary<IntPtr, int>();
            private static readonly object OldFileCountsLock = new object();

            private IntPtr CreateMainStruct(int oldFileCount)
            {
                int nativeSize;
                switch(this.marshalType)
                {
                case PatchAPIMarshaler.MarshalType.PATCH_OPTION_DATA:
                    nativeSize = patchOptionDataSize;
                    break;
                case PatchAPIMarshaler.MarshalType.PATCH_OLD_FILE_INFO_W:
                    nativeSize = oldFileCount*patchOldFileInfoSize;
                    break;
                default:
                    throw new InvalidOperationException();
                }

                IntPtr native = Marshal.AllocCoTaskMem(nativeSize);

                lock (PatchAPIMarshaler.OldFileCountsLock)
                {
                    PatchAPIMarshaler.OldFileCounts.Add(native, oldFileCount);
                }

                return native;
            }

            private static void ReleaseMainStruct(IntPtr native)
            {
                lock (PatchAPIMarshaler.OldFileCountsLock)
                {
                    PatchAPIMarshaler.OldFileCounts.Remove(native);
                }
                Marshal.FreeCoTaskMem(native);
            }

            private static int GetOldFileCount(IntPtr native)
            {
                lock (PatchAPIMarshaler.OldFileCountsLock)
                {
                    return PatchAPIMarshaler.OldFileCounts[native];
                }
            }

            // Helper methods

            private static IntPtr OptionalAnsiString(string managed)
            {
                return (null == managed) ? IntPtr.Zero : Marshal.StringToCoTaskMemAnsi(managed);
            }

            private static IntPtr OptionalUnicodeString(string managed)
            {
                return (null == managed) ? IntPtr.Zero : Marshal.StringToCoTaskMemUni(managed);
            }

            // string array must be of the same length as the number of old files
            private static IntPtr CreateArrayOfStringA(string[] managed)
            {
                if (null == managed)
                {
                    return IntPtr.Zero;
                }

                int size = managed.Length * Marshal.SizeOf(typeof(IntPtr));
                IntPtr native = Marshal.AllocCoTaskMem(size);

                for (int i = 0; i < managed.Length; ++i)
                {
                    Marshal.WriteIntPtr(native, i*Marshal.SizeOf(typeof(IntPtr)), OptionalAnsiString(managed[i]));
                }

                return native;
            }

            // string array must be of the same length as the number of old files
            private static IntPtr CreateArrayOfStringW(string[] managed)
            {
                if (null == managed)
                {
                    return IntPtr.Zero;
                }

                int size = managed.Length * Marshal.SizeOf(typeof(IntPtr));
                IntPtr native = Marshal.AllocCoTaskMem(size);

                for (int i = 0; i < managed.Length; ++i)
                {
                    Marshal.WriteIntPtr(native, i*Marshal.SizeOf(typeof(IntPtr)), OptionalUnicodeString(managed[i]));
                }

                return native;
            }

            private static IntPtr CreateInterleaveMapRange(PatchInterleaveMap managed)
            {
                if (null == managed)
                {
                    return IntPtr.Zero;
                }

                if (null == managed.ranges)
                {
                    return IntPtr.Zero;
                }

                if (0 == managed.ranges.Length)
                {
                    return IntPtr.Zero;
                }

                IntPtr native = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(UInt32))
                                        + managed.ranges.Length*(Marshal.SizeOf(typeof(PatchInterleaveMap))));
                WriteUInt32(native, (uint) managed.ranges.Length);

                for (int i = 0; i < managed.ranges.Length; ++i)
                {
                    Marshal.StructureToPtr(managed.ranges[i], (IntPtr)((Int64)native + i*Marshal.SizeOf(typeof(PatchInterleaveMap))), false);
                }
                return native;
            }

            private static IntPtr CreateInterleaveMap(PatchInterleaveMap[] managed)
            {
                if (null == managed)
                {
                    return IntPtr.Zero;
                }

                IntPtr native = Marshal.AllocCoTaskMem(managed.Length * Marshal.SizeOf(typeof(IntPtr)));

                for (int i = 0; i < managed.Length; ++i)
                {
                    Marshal.WriteIntPtr(native, i*Marshal.SizeOf(typeof(IntPtr)), CreateInterleaveMapRange(managed[i]));
                }

                return native;
            }

            private static void WriteUInt32(IntPtr native, uint data)
            {
                Marshal.WriteInt32(native, unchecked((int) data));
            }

            private static void WriteUInt32(IntPtr native, int offset, uint data)
            {
                Marshal.WriteInt32(native, offset, unchecked((int) data));
            }

            // Marshal operations

            private IntPtr MarshalPOD(PatchOptionData managed)
            {
                if (null == managed)
                {
                    throw new ArgumentNullException("managed");
                }

                IntPtr native = this.CreateMainStruct(managed.oldFileSymbolPathArray.Length);
                Marshal.WriteInt32(native, patchOptionDataSize); // SizeOfThisStruct
                WriteUInt32(native, symbolOptionFlagsOffset, (uint) managed.symbolOptionFlags);
                Marshal.WriteIntPtr(native, newFileSymbolPathOffset, PatchAPIMarshaler.OptionalAnsiString(managed.newFileSymbolPath));
                Marshal.WriteIntPtr(native, oldFileSymbolPathArrayOffset, PatchAPIMarshaler.CreateArrayOfStringA(managed.oldFileSymbolPathArray));
                WriteUInt32(native, extendedOptionFlagsOffset, managed.extendedOptionFlags);

                // GetFunctionPointerForDelegate() throws an ArgumentNullException if the delegate is null.
                if (null == managed.symLoadCallback)
                {
                    Marshal.WriteIntPtr(native, symLoadCallbackOffset, IntPtr.Zero);
                }
                else
                {
                    Marshal.WriteIntPtr(native, symLoadCallbackOffset, Marshal.GetFunctionPointerForDelegate(managed.symLoadCallback));
                }

                Marshal.WriteIntPtr(native, symLoadContextOffset, managed.symLoadContext);
                Marshal.WriteIntPtr(native, interleaveMapArrayOffset, PatchAPIMarshaler.CreateInterleaveMap(managed.interleaveMapArray));
                WriteUInt32(native, maxLzxWindowSizeOffset, managed.maxLzxWindowSize);
                return native;
            }

            private IntPtr MarshalPOFIW_A(PatchOldFileInfoW[] managed)
            {
                if (null == managed)
                {
                    throw new ArgumentNullException("managed");
                }

                if (0 == managed.Length)
                {
                    return IntPtr.Zero;
                }

                IntPtr native = this.CreateMainStruct(managed.Length);

                for (int i = 0; i < managed.Length; ++i)
                {
                    PatchAPIMarshaler.MarshalPOFIW(managed[i], (IntPtr)((Int64)native + i * patchOldFileInfoSize));
                }

                return native;
            }

            private static void MarshalPOFIW(PatchOldFileInfoW managed, IntPtr native)
            {
                PatchAPIMarshaler.MarshalPOFI(managed, native);
                Marshal.WriteIntPtr(native, oldFileOffset, PatchAPIMarshaler.OptionalUnicodeString(managed.oldFileName)); // OldFileName
            }

            private static void MarshalPOFI(PatchOldFileInfo managed, IntPtr native)
            {
                Marshal.WriteInt32(native, patchOldFileInfoSize); // SizeOfThisStruct
                WriteUInt32(native, ignoreRangeCountOffset,
                                    (null == managed.ignoreRange) ? 0 : (uint) managed.ignoreRange.Length); // IgnoreRangeCount // maximum 255
                Marshal.WriteIntPtr(native, ignoreRangeArrayOffset, MarshalPIRArray(managed.ignoreRange));  // IgnoreRangeArray
                WriteUInt32(native, retainRangeCountOffset,
                                    (null == managed.retainRange) ? 0 : (uint) managed.retainRange.Length); // RetainRangeCount // maximum 255
                Marshal.WriteIntPtr(native, retainRangeArrayOffset, MarshalPRRArray(managed.retainRange));  // RetainRangeArray
            }

            private static IntPtr MarshalPIRArray(PatchIgnoreRange[] array)
            {
                if (null == array)
                {
                    return IntPtr.Zero;
                }

                if (0 == array.Length)
                {
                    return IntPtr.Zero;
                }

                IntPtr native = Marshal.AllocCoTaskMem(array.Length*Marshal.SizeOf(typeof(PatchIgnoreRange)));

                for (int i = 0; i < array.Length; ++i)
                {
                    Marshal.StructureToPtr(array[i], (IntPtr)((Int64)native + (i*Marshal.SizeOf(typeof(PatchIgnoreRange)))), false);
                }

                return native;
            }

            private static IntPtr MarshalPRRArray(PatchRetainRange[] array)
            {
                if (null == array)
                {
                    return IntPtr.Zero;
                }

                if (0 == array.Length)
                {
                    return IntPtr.Zero;
                }

                IntPtr native = Marshal.AllocCoTaskMem(array.Length*Marshal.SizeOf(typeof(PatchRetainRange)));

                for (int i = 0; i < array.Length; ++i)
                {
                    Marshal.StructureToPtr(array[i], (IntPtr)((Int64)native + (i*Marshal.SizeOf(typeof(PatchRetainRange)))), false);
                }

                return native;
            }

            // CleanUp operations

            private void CleanUpPOD(IntPtr native)
            {
                Marshal.FreeCoTaskMem(Marshal.ReadIntPtr(native, newFileSymbolPathOffset));

                if (IntPtr.Zero != Marshal.ReadIntPtr(native, oldFileSymbolPathArrayOffset))
                {
                    for (int i = 0; i < GetOldFileCount(native); ++i)
                    {
                        Marshal.FreeCoTaskMem(
                                Marshal.ReadIntPtr(
                                        Marshal.ReadIntPtr(native, oldFileSymbolPathArrayOffset),
                                        i*Marshal.SizeOf(typeof(IntPtr))));
                    }

                    Marshal.FreeCoTaskMem(Marshal.ReadIntPtr(native, oldFileSymbolPathArrayOffset));
                }

                if (IntPtr.Zero != Marshal.ReadIntPtr(native, interleaveMapArrayOffset))
                {
                    for (int i = 0; i < GetOldFileCount(native); ++i)
                    {
                        Marshal.FreeCoTaskMem(
                                Marshal.ReadIntPtr(
                                        Marshal.ReadIntPtr(native, interleaveMapArrayOffset),
                                        i*Marshal.SizeOf(typeof(IntPtr))));
                    }

                    Marshal.FreeCoTaskMem(Marshal.ReadIntPtr(native, interleaveMapArrayOffset));
                }

                PatchAPIMarshaler.ReleaseMainStruct(native);
            }

            private void CleanUpPOFI_A(IntPtr native)
            {
                for (int i = 0; i < GetOldFileCount(native); ++i)
                {
                    PatchAPIMarshaler.CleanUpPOFI((IntPtr)((Int64)native + i*patchOldFileInfoSize));
                }

                PatchAPIMarshaler.ReleaseMainStruct(native);
            }

            private static void CleanUpPOFI(IntPtr native)
            {
                if (IntPtr.Zero != Marshal.ReadIntPtr(native, oldFileOffset))
                {
                    Marshal.FreeCoTaskMem(Marshal.ReadIntPtr(native, oldFileOffset));
                }

                PatchAPIMarshaler.CleanUpPOFIH(native);
            }

            private static void CleanUpPOFIH(IntPtr native)
            {
                if (IntPtr.Zero != Marshal.ReadIntPtr(native, ignoreRangeArrayOffset))
                {
                    Marshal.FreeCoTaskMem(Marshal.ReadIntPtr(native, ignoreRangeArrayOffset));
                }

                if (IntPtr.Zero != Marshal.ReadIntPtr(native, retainRangeArrayOffset))
                {
                    Marshal.FreeCoTaskMem(Marshal.ReadIntPtr(native, retainRangeArrayOffset));
                }
            }
        }
    }
}
