//-------------------------------------------------------------------------------------------------
// <copyright file="DirUtilTests.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

using namespace System;
using namespace System::Text;
using namespace System::Collections::Generic;
using namespace Xunit;

namespace DutilTests
{
    public ref class DirUtil
    {
    public:
        [Fact]
        void DirUtilTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczCurrentDir = NULL;
            LPWSTR sczGuid = NULL;
            LPWSTR sczFolder = NULL;
            LPWSTR sczSubFolder = NULL;

            try
            {
                CreateGuid(&sczGuid);

                hr = DirGetCurrent(&sczCurrentDir);
                ExitOnFailure(hr, "Failed to get current directory.");

                hr = PathConcat(sczCurrentDir, sczGuid, &sczFolder);
                ExitOnFailure2(hr, "Failed to combine current directory: '%ls' with Guid: '%ls'", sczCurrentDir, sczGuid);

                BOOL fExists = DirExists(sczFolder, NULL);
                Assert::False(fExists);

                hr = PathConcat(sczFolder, L"foo", &sczSubFolder);
                ExitOnFailure1(hr, "Failed to combine folder: '%ls' with subfolder: 'foo'", sczFolder);

                hr = DirEnsureExists(sczSubFolder, NULL);
                ExitOnFailure1(hr, "Failed to create multiple directories: %ls", sczSubFolder);

                // Test failure to delete non-empty folder.
                hr = DirEnsureDelete(sczFolder, FALSE, FALSE);
                Assert::Equal<HRESULT>(0x80070091, hr);

                hr = DirEnsureDelete(sczSubFolder, FALSE, FALSE);
                ExitOnFailure1(hr, "Failed to delete single directory: %ls", sczSubFolder);

                // Put the directory back and we'll test deleting tree.
                hr = DirEnsureExists(sczSubFolder, NULL);
                ExitOnFailure1(hr, "Failed to create single directory: %ls", sczSubFolder);

                hr = DirEnsureDelete(sczFolder, FALSE, TRUE);
                ExitOnFailure1(hr, "Failed to delete directory tree: %ls", sczFolder);

                // Finally, try to create "C:\" which would normally fail, but we want success
                hr = DirEnsureExists(L"C:\\", NULL);
                ExitOnFailure(hr, "Failed to create C:\\");
            }
            finally
            {
                ReleaseStr(sczSubFolder);
                ReleaseStr(sczFolder);
                ReleaseStr(sczGuid);
                ReleaseStr(sczCurrentDir);
            }

        LExit:
            return;
        }

    private:
        void CreateGuid(
            __out LPWSTR* psczGuid
            )
        {
            HRESULT hr = S_OK;
            RPC_STATUS rs = RPC_S_OK;
            UUID guid = { };
            WCHAR wzGuid[39];

            rs = ::UuidCreate(&guid);
            hr = HRESULT_FROM_RPC(rs);
            ExitOnFailure(hr, "Failed to create pipe guid.");

            if (!::StringFromGUID2(guid, wzGuid, countof(wzGuid)))
            {
                hr = E_OUTOFMEMORY;
                ExitOnRootFailure(hr, "Failed to convert pipe guid into string.");
            }

            hr = StrAllocString(psczGuid, wzGuid, 0);
            ExitOnFailure(hr, "Failed to copy guid.");

        LExit:
            return;
        }
    };
}
