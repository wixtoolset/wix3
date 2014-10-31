//-------------------------------------------------------------------------------------------------
// <copyright file="CacheTest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Cache unit tests for Burn.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


namespace Microsoft
{
namespace Tools
{
namespace WindowsInstallerXml
{
namespace Test
{
namespace Bootstrapper
{
    using namespace System;
    using namespace System::IO;
    using namespace WixTest;
    using namespace Xunit;

    public ref class CacheTest : BurnUnitTest
    {
    public:
        [NamedFact]
        void CacheSignatureTest()
        {
            HRESULT hr = S_OK;
            BURN_PACKAGE package = { };
            BURN_PAYLOAD payload = { };
            LPWSTR sczPayloadPath = NULL;
            BYTE* pb = NULL;
            DWORD cb = NULL;

            try
            {
                hr = PathExpand(&sczPayloadPath, L"%WIX_ROOT%\\src\\Votive\\SDK\\Redist\\ProjectAggregator2.msi", PATH_EXPAND_ENVIRONMENT);
                Assert::True(S_OK == hr, "Failed to get path to project aggregator MSI.");

                hr = StrAllocHexDecode(L"4A5C7522AA46BFA4089D39974EBDB4A360F7A01D", &pb, &cb);
                Assert::Equal(S_OK, hr);

                package.fPerMachine = FALSE;
                package.sczCacheId = L"Bootstrapper.CacheTest.CacheSignatureTest";
                payload.sczKey = L"CacheSignatureTest.PayloadKey";
                payload.sczFilePath = L"CacheSignatureTest.File";
                payload.pbCertificateRootPublicKeyIdentifier = pb;
                payload.cbCertificateRootPublicKeyIdentifier = cb;

                hr = CacheCompletePayload(package.fPerMachine, &payload, package.sczCacheId, sczPayloadPath, FALSE);
                Assert::True(S_OK == hr, "Failed while verifying path.");
            }
            finally
            {
                ReleaseMem(pb);
                ReleaseStr(sczPayloadPath);

                String^ filePath = Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), "Package Cache\\Bootstrapper.CacheTest.CacheSignatureTest\\CacheSignatureTest.File");
                if (File::Exists(filePath))
                {
                    File::SetAttributes(filePath, FileAttributes::Normal);
                    File::Delete(filePath);
                }
            }
        }
    };
}
}
}
}
}
