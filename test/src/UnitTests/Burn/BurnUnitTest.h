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
    using namespace Microsoft::VisualStudio::TestTools::UnitTesting;

    [TestClass]
    public ref class BurnUnitTest
    {
    private:
        TestContext^ testContextInstance;

    public: 
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        property Microsoft::VisualStudio::TestTools::UnitTesting::TestContext^ TestContext
        {
            Microsoft::VisualStudio::TestTools::UnitTesting::TestContext^ get()
            {
                return testContextInstance;
            }
            System::Void set(Microsoft::VisualStudio::TestTools::UnitTesting::TestContext^ value)
            {
                testContextInstance = value;
            }
        };

        //Use ClassInitialize to run code before running the first test in the class
        BurnUnitTest ()
        {
            HRESULT hr = XmlInitialize();
            TestThrowOnFailure(hr, L"Failed to initialize XML support.");

            hr = RegInitialize();
            TestThrowOnFailure(hr, L"Failed to initialize Regutil.");
        }

        //Use ClassCleanup to run code after all tests in a class have run
        ~BurnUnitTest()
        {
            XmlUninitialize();
            RegUninitialize();
        }

        //Use TestInitialize to run code before running each test
        [TestInitialize()]
        void TestInitialize()
        {
            HRESULT hr = S_OK;

            LogInitialize(::GetModuleHandleW(NULL));

            hr = LogOpen(NULL, L"BurnUnitTest", NULL, L"txt", FALSE, FALSE, NULL);
            TestThrowOnFailure(hr, L"Failed to open log.");

            PlatformInitialize();
        }

        //Use TestCleanup to run code after each test has run
        [TestCleanup()]
        void TestCleanup() 
        {
            LogUninitialize(FALSE);
        }
    }; 
}
}
}
}
}
