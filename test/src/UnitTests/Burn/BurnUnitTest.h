//-------------------------------------------------------------------------------------------------
// <copyright file="BurnUnitTest.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Base class for Burn Unit tests.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


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
    using namespace WixTest;
    using namespace Xunit;

    public ref class BurnUnitTest :
        public WixTestBase,
        public IDisposable
    {
    public: 
        // Run code before running the first test in the class
        BurnUnitTest()
        {
            HRESULT hr = XmlInitialize();
            TestThrowOnFailure(hr, L"Failed to initialize XML support.");

            hr = RegInitialize();
            TestThrowOnFailure(hr, L"Failed to initialize Regutil.");
        }

        // Run code after all tests in a class have run
        ~BurnUnitTest()
        {
            XmlUninitialize();
            RegUninitialize();
        }

        void TestInitialize() override
        {
            HRESULT hr = S_OK;

            LogInitialize(::GetModuleHandleW(NULL));

            hr = LogOpen(NULL, L"BurnUnitTest", NULL, L"txt", FALSE, FALSE, NULL);
            TestThrowOnFailure(hr, L"Failed to open log.");

            PlatformInitialize();
        }

        void TestUninitialize() override
        {
            LogUninitialize(FALSE);
        }
    }; 
}
}
}
}
}
