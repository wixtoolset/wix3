#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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

        virtual void TestInitialize() override
        {
            HRESULT hr = S_OK;

            LogInitialize(::GetModuleHandleW(NULL));

            hr = LogOpen(NULL, L"BurnUnitTest", NULL, L"txt", FALSE, FALSE, NULL);
            TestThrowOnFailure(hr, L"Failed to open log.");

            PlatformInitialize();
        }

        virtual void TestUninitialize() override
        {
            LogUninitialize(FALSE);
        }
    }; 
}
}
}
}
}
