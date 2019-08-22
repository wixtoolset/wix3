// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Ole32
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Specifies the access mode to use when opening, creating, or deleting a storage object.
    /// </summary>
    internal enum StorageMode
    {
        /// <summary>
        /// Indicates that the object is read-only, meaning that modifications cannot be made.
        /// </summary>
        Read = 0x0,

        /// <summary>
        /// Enables you to save changes to the object, but does not permit access to its data.
        /// </summary>
        Write = 0x1,

        /// <summary>
        /// Enables access and modification of object data.
        /// </summary>
        ReadWrite = 0x2,

        /// <summary>
        /// Specifies that subsequent openings of the object are not denied read or write access.
        /// </summary>
        ShareDenyNone = 0x40,

        /// <summary>
        /// Prevents others from subsequently opening the object in Read mode.
        /// </summary>
        ShareDenyRead = 0x30,

        /// <summary>
        /// Prevents others from subsequently opening the object for Write or ReadWrite access.
        /// </summary>
        ShareDenyWrite = 0x20,

        /// <summary>
        /// Prevents others from subsequently opening the object in any mode.
        /// </summary>
        ShareExclusive = 0x10,

        /// <summary>
        /// Opens the storage object with exclusive access to the most recently committed version.
        /// </summary>
        Priority = 0x40000,

        /// <summary>
        /// Indicates that an existing storage object or stream should be removed before the new object replaces it.
        /// </summary>
        Create = 0x1000,
    }

    /// <summary>
    /// Wrapper for the compound storage file APIs.
    /// </summary>
    internal sealed class Storage : IDisposable
    {
        private bool disposed;
        private IStorage storage;

        /// <summary>
        /// Instantiate a new Storage.
        /// </summary>
        /// <param name="storage">The native storage interface.</param>
        private Storage(IStorage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// Storage destructor.
        /// </summary>
        ~Storage()
        {
            this.Dispose();
        }

        /// <summary>
        /// The IEnumSTATSTG interface enumerates an array of STATSTG structures.
        /// </summary>
        [ComImport, Guid("0000000d-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumSTATSTG
        {
            /// <summary>
            /// Gets a specified number of STATSTG structures.
            /// </summary>
            /// <param name="celt">The number of STATSTG structures requested.</param>
            /// <param name="rgelt">An array of STATSTG structures returned.</param>
            /// <param name="pceltFetched">The number of STATSTG structures retrieved in the rgelt parameter.</param>
            /// <returns>The error code.</returns>
            [PreserveSig]
            uint Next(uint celt, [MarshalAs(UnmanagedType.LPArray), Out] STATSTG[] rgelt, out uint pceltFetched);

            /// <summary>
            /// Skips a specified number of STATSTG structures in the enumeration sequence.
            /// </summary>
            /// <param name="celt">The number of STATSTG structures to skip.</param>
            void Skip(uint celt);

            /// <summary>
            /// Resets the enumeration sequence to the beginning of the STATSTG structure array.
            /// </summary>
            void Reset();

            /// <summary>
            /// Creates a new enumerator that contains the same enumeration state as the current STATSTG structure enumerator.
            /// </summary>
            /// <returns>The cloned IEnumSTATSTG interface.</returns>
            [return: MarshalAs(UnmanagedType.Interface)]
            IEnumSTATSTG Clone();
        }

        /// <summary>
        /// The IStorage interface supports the creation and management of structured storage objects.
        /// </summary>
        [ComImport, Guid("0000000b-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IStorage
        {
            /// <summary>
            /// Creates and opens a stream object with the specified name contained in this storage object.
            /// </summary>
            /// <param name="pwcsName">The name of the newly created stream.</param>
            /// <param name="grfMode">Specifies the access mode to use when opening the newly created stream.</param>
            /// <param name="reserved1">Reserved for future use; must be zero.</param>
            /// <param name="reserved2">Reserved for future use; must be zero.</param>
            /// <param name="ppstm">On return, pointer to the location of the new IStream interface pointer.</param>
            void CreateStream(string pwcsName, uint grfMode, uint reserved1, uint reserved2, out IStream ppstm);

            /// <summary>
            /// Opens an existing stream object within this storage object using the specified access permissions in grfMode.
            /// </summary>
            /// <param name="pwcsName">The name of the stream to open.</param>
            /// <param name="reserved1">Reserved for future use; must be NULL.</param>
            /// <param name="grfMode">Specifies the access mode to be assigned to the open stream.</param>
            /// <param name="reserved2">Reserved for future use; must be zero.</param>
            /// <param name="ppstm">A pointer to IStream pointer variable that receives the interface pointer to the newly opened stream object.</param>
            void OpenStream(string pwcsName, IntPtr reserved1, uint grfMode, uint reserved2, out IStream ppstm);

            /// <summary>
            /// Creates and opens a new storage object nested within this storage object with the specified name in the specified access mode.
            /// </summary>
            /// <param name="pwcsName">The name of the newly created storage object.</param>
            /// <param name="grfMode">A value that specifies the access mode to use when opening the newly created storage object.</param>
            /// <param name="reserved1">Reserved for future use; must be zero.</param>
            /// <param name="reserved2">Reserved for future use; must be zero.</param>
            /// <param name="ppstg">A pointer, when successful, to the location of the IStorage pointer to the newly created storage object.</param>
            void CreateStorage(string pwcsName, uint grfMode, uint reserved1, uint reserved2, out IStorage ppstg);

            /// <summary>
            /// Opens an existing storage object with the specified name in the specified access mode.
            /// </summary>
            /// <param name="pwcsName">The name of the storage object to open.</param>
            /// <param name="pstgPriority">Must be NULL.</param>
            /// <param name="grfMode">Specifies the access mode to use when opening the storage object.</param>
            /// <param name="snbExclude">Must be NULL.</param>
            /// <param name="reserved">Reserved for future use; must be zero.</param>
            /// <param name="ppstg">When successful, pointer to the location of an IStorage pointer to the opened storage object.</param>
            void OpenStorage(string pwcsName, IStorage pstgPriority, uint grfMode, IntPtr snbExclude, uint reserved, out IStorage ppstg);

            /// <summary>
            /// Copies the entire contents of an open storage object to another storage object.
            /// </summary>
            /// <param name="ciidExclude">The number of elements in the array pointed to by rgiidExclude.</param>
            /// <param name="rgiidExclude">An array of interface identifiers (IIDs) that either the caller knows about and does not want
            /// copied or that the storage object does not support, but whose state the caller will later explicitly copy.</param>
            /// <param name="snbExclude">A string name block (refer to SNB) that specifies a block of storage or stream objects that are not to be copied to the destination.</param>
            /// <param name="pstgDest">A pointer to the open storage object into which this storage object is to be copied.</param>
            void CopyTo(uint ciidExclude, IntPtr rgiidExclude, IntPtr snbExclude, IStorage pstgDest);

            /// <summary>
            /// Copies or moves a substorage or stream from this storage object to another storage object.
            /// </summary>
            /// <param name="pwcsName">The name of the element in this storage object to be moved or copied.</param>
            /// <param name="pstgDest">IStorage pointer to the destination storage object.</param>
            /// <param name="pwcsNewName">The new name for the element in its new storage object.</param>
            /// <param name="grfFlags">Specifies whether the operation should be a move (STGMOVE_MOVE) or a copy (STGMOVE_COPY).</param>
            void MoveElementTo(string pwcsName, IStorage pstgDest, string pwcsNewName, uint grfFlags);

            /// <summary>
            /// Reflects changes for a transacted storage object to the parent level.
            /// </summary>
            /// <param name="grfCommitFlags">Controls how the changes are committed to the storage object.</param>
            void Commit(uint grfCommitFlags);

            /// <summary>
            /// Discards all changes that have been made to the storage object since the last commit operation.
            /// </summary>
            void Revert();

            /// <summary>
            /// Returns an enumerator object that can be used to enumerate the storage and stream objects contained within this storage object.
            /// </summary>
            /// <param name="reserved1">Reserved for future use; must be zero.</param>
            /// <param name="reserved2">Reserved for future use; must be NULL.</param>
            /// <param name="reserved3">Reserved for future use; must be zero.</param>
            /// <param name="ppenum">Pointer to IEnumSTATSTG* pointer variable that receives the interface pointer to the new enumerator object.</param>
            void EnumElements(uint reserved1, IntPtr reserved2, uint reserved3, out IEnumSTATSTG ppenum);

            /// <summary>
            /// Removes the specified storage or stream from this storage object.
            /// </summary>
            /// <param name="pwcsName">The name of the storage or stream to be removed.</param>
            void DestroyElement(string pwcsName);

            /// <summary>
            /// Renames the specified storage or stream in this storage object.
            /// </summary>
            /// <param name="pwcsOldName">The name of the substorage or stream to be changed.</param>
            /// <param name="pwcsNewName">The new name for the specified substorage or stream.</param>
            void RenameElement(string pwcsOldName, string pwcsNewName);

            /// <summary>
            /// Sets the modification, access, and creation times of the indicated storage element, if supported by the underlying file system.
            /// </summary>
            /// <param name="pwcsName">The name of the storage object element whose times are to be modified.</param>
            /// <param name="pctime">Either the new creation time for the element or NULL if the creation time is not to be modified.</param>
            /// <param name="patime">Either the new access time for the element or NULL if the access time is not to be modified.</param>
            /// <param name="pmtime">Either the new modification time for the element or NULL if the modification time is not to be modified.</param>
            void SetElementTimes(string pwcsName, FILETIME pctime, FILETIME patime, FILETIME pmtime);

            /// <summary>
            /// Assigns the specified CLSID to this storage object.
            /// </summary>
            /// <param name="clsid">The CLSID that is to be associated with the storage object.</param>
            void SetClass(Guid clsid);

            /// <summary>
            /// Stores up to 32 bits of state information in this storage object.
            /// </summary>
            /// <param name="grfStateBits">Specifies the new values of the bits to set.</param>
            /// <param name="grfMask">A binary mask indicating which bits in grfStateBits are significant in this call.</param>
            void SetStateBits(uint grfStateBits, uint grfMask);

            /// <summary>
            /// Returns the STATSTG structure for this open storage object.
            /// </summary>
            /// <param name="pstatstg">On return, pointer to a STATSTG structure where this method places information about the open storage object.</param>
            /// <param name="grfStatFlag">Specifies that some of the members in the STATSTG structure are not returned, thus saving a memory allocation operation.</param>
            void Stat(out STATSTG pstatstg, uint grfStatFlag);
        }

        /// <summary>
        /// The IStream interface lets you read and write data to stream objects.
        /// </summary>
        [ComImport, Guid("0000000c-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IStream
        {
            /// <summary>
            /// Reads a specified number of bytes from the stream object into memory starting at the current seek pointer.
            /// </summary>
            /// <param name="pv">A pointer to the buffer which the stream data is read into.</param>
            /// <param name="cb">The number of bytes of data to read from the stream object.</param>
            /// <param name="pcbRead">A pointer to a ULONG variable that receives the actual number of bytes read from the stream object.</param>
            void Read([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] pv, int cb, IntPtr pcbRead);

            /// <summary>
            /// Writes a specified number of bytes into the stream object starting at the current seek pointer.
            /// </summary>
            /// <param name="pv">A pointer to the buffer that contains the data that is to be written to the stream.</param>
            /// <param name="cb">The number of bytes of data to attempt to write into the stream.</param>
            /// <param name="pcbWritten">A pointer to a ULONG variable where this method writes the actual number of bytes written to the stream object.</param>
            void Write([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] pv, int cb, IntPtr pcbWritten);

            /// <summary>
            /// Changes the seek pointer to a new location relative to the beginning of the stream, the end of the stream, or the current seek pointer.
            /// </summary>
            /// <param name="dlibMove">The displacement to be added to the location indicated by the dwOrigin parameter.</param>
            /// <param name="dwOrigin">The origin for the displacement specified in dlibMove.</param>
            /// <param name="plibNewPosition">A pointer to the location where this method writes the value of the new seek pointer from the beginning of the stream.</param>
            void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition);

            /// <summary>
            /// Changes the size of the stream object.
            /// </summary>
            /// <param name="libNewSize">Specifies the new size of the stream as a number of bytes.</param>
            void SetSize(long libNewSize);

            /// <summary>
            /// Copies a specified number of bytes from the current seek pointer in the stream to the current seek pointer in another stream.
            /// </summary>
            /// <param name="pstm">A pointer to the destination stream.</param>
            /// <param name="cb">The number of bytes to copy from the source stream.</param>
            /// <param name="pcbRead">A pointer to the location where this method writes the actual number of bytes read from the source.</param>
            /// <param name="pcbWritten">A pointer to the location where this method writes the actual number of bytes written to the destination.</param>
            void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten);

            /// <summary>
            /// Ensures that any changes made to a stream object open in transacted mode are reflected in the parent storage object.
            /// </summary>
            /// <param name="grfCommitFlags">Controls how the changes for the stream object are committed.</param>
            void Commit(int grfCommitFlags);

            /// <summary>
            /// Discards all changes that have been made to a transacted stream since the last call to IStream::Commit.
            /// </summary>
            void Revert();

            /// <summary>
            /// Restricts access to a specified range of bytes in the stream.
            /// </summary>
            /// <param name="libOffset">Integer that specifies the byte offset for the beginning of the range.</param>
            /// <param name="cb">Integer that specifies the length of the range, in bytes, to be restricted.</param>
            /// <param name="dwLockType">Specifies the restrictions being requested on accessing the range.</param>
            void LockRegion(long libOffset, long cb, int dwLockType);

            /// <summary>
            /// Removes the access restriction on a range of bytes previously restricted with IStream::LockRegion.
            /// </summary>
            /// <param name="libOffset">Specifies the byte offset for the beginning of the range.</param>
            /// <param name="cb">Specifies, in bytes, the length of the range to be restricted.</param>
            /// <param name="dwLockType">Specifies the access restrictions previously placed on the range.</param>
            void UnlockRegion(long libOffset, long cb, int dwLockType);

            /// <summary>
            /// Retrieves the STATSTG structure for this stream.
            /// </summary>
            /// <param name="pstatstg">Pointer to a STATSTG structure where this method places information about this stream object.</param>
            /// <param name="grfStatFlag">Specifies that this method does not return some of the members in the STATSTG structure, thus saving a memory allocation operation.</param>
            void Stat(out STATSTG pstatstg, int grfStatFlag);

            /// <summary>
            /// Creates a new stream object that references the same bytes as the original stream but provides a separate seek pointer to those bytes.
            /// </summary>
            /// <param name="ppstm">When successful, pointer to the location of an IStream pointer to the new stream object.</param>
            void Clone(out IStream ppstm);
        }

        /// <summary>
        /// Creates a new compound file storage object.
        /// </summary>
        /// <param name="storageFile">The compound file being created.</param>
        /// <param name="mode">Specifies the access mode to use when opening the new storage object.</param>
        /// <returns>The created Storage object.</returns>
        public static Storage CreateDocFile(string storageFile, StorageMode mode)
        {
            IStorage storage = NativeMethods.StgCreateDocfile(storageFile, (uint)mode, 0);

            return new Storage(storage);
        }

        /// <summary>
        /// Opens an existing root storage object in the file system.
        /// </summary>
        /// <param name="storageFile">The file that contains the storage object to open.</param>
        /// <param name="mode">Specifies the access mode to use to open the storage object.</param>
        /// <returns>The created Storage object.</returns>
        public static Storage Open(string storageFile, StorageMode mode)
        {
            IStorage storage = NativeMethods.StgOpenStorage(storageFile, IntPtr.Zero, (uint)mode, IntPtr.Zero, 0);

            return new Storage(storage);
        }

        /// <summary>
        /// Copies the entire contents of this open storage object into another Storage object.
        /// </summary>
        /// <param name="destinationStorage">The destination Storage object.</param>
        public void CopyTo(Storage destinationStorage)
        {
            this.storage.CopyTo(0, IntPtr.Zero, IntPtr.Zero, destinationStorage.storage);
        }

        /// <summary>
        /// Opens an existing Storage object with the specified name according to the specified access mode.
        /// </summary>
        /// <param name="name">The name of the Storage object.</param>
        /// <returns>The opened Storage object.</returns>
        public Storage OpenStorage(string name)
        {
            IStorage subStorage;

            this.storage.OpenStorage(name, null, (uint)(StorageMode.Read | StorageMode.ShareExclusive), IntPtr.Zero, 0, out subStorage);

            return new Storage(subStorage);
        }

        /// <summary>
        /// Disposes the managed and unmanaged objects in this object.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                Marshal.ReleaseComObject(this.storage);

                this.disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The native methods.
        /// </summary>
        private sealed class NativeMethods
        {
            /// <summary>
            /// Protect the constructor since this class only contains static methods.
            /// </summary>
            private NativeMethods()
            {
            }

            /// <summary>
            /// Creates a new compound file storage object.
            /// </summary>
            /// <param name="pwcsName">The name for the compound file being created.</param>
            /// <param name="grfMode">Specifies the access mode to use when opening the new storage object.</param>
            /// <param name="reserved">Reserved for future use; must be zero.</param>
            /// <returns>A pointer to the location of the IStorage pointer to the new storage object.</returns>
            [DllImport("ole32.dll", PreserveSig = false)]
            [return: MarshalAs(UnmanagedType.Interface)]
            internal static extern IStorage StgCreateDocfile([MarshalAs(UnmanagedType.LPWStr)] string pwcsName, uint grfMode, uint reserved);

            /// <summary>
            /// Opens an existing root storage object in the file system.
            /// </summary>
            /// <param name="pwcsName">The file that contains the storage object to open.</param>
            /// <param name="pstgPriority">Most often NULL.</param>
            /// <param name="grfMode">Specifies the access mode to use to open the storage object.</param>
            /// <param name="snbExclude">If not NULL, pointer to a block of elements in the storage to be excluded as the storage object is opened.</param>
            /// <param name="reserved">Indicates reserved for future use; must be zero.</param>
            /// <returns>A pointer to a IStorage* pointer variable that receives the interface pointer to the opened storage.</returns>
            [DllImport("ole32.dll", PreserveSig = false)]
            [return: MarshalAs(UnmanagedType.Interface)]
            internal static extern IStorage StgOpenStorage([MarshalAs(UnmanagedType.LPWStr)] string pwcsName, IntPtr pstgPriority, uint grfMode, IntPtr snbExclude, uint reserved);
        }
    }
}
