//-------------------------------------------------------------------------------------------------
// <copyright file="RegistrationTest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Unit tests for Burn registration.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


#define ROOT_PATH L"SOFTWARE\\Microsoft\\WiX_Burn_UnitTest"
#define HKLM_PATH L"SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\HKLM"
#define HKCU_PATH L"SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\HKCU"
#define REGISTRY_UNINSTALL_KEY L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
#define REGISTRY_RUN_KEY L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce"

#define TEST_UNINSTALL_KEY L"HKEY_CURRENT_USER\\" HKCU_PATH L"\\" REGISTRY_UNINSTALL_KEY L"\\{D54F896D-1952-43e6-9C67-B5652240618C}"
#define TEST_RUN_KEY L"HKEY_CURRENT_USER\\" HKCU_PATH L"\\" REGISTRY_RUN_KEY


static LSTATUS APIENTRY RegistrationTest_RegCreateKeyExW(
    __in HKEY hKey,
    __in LPCWSTR lpSubKey,
    __reserved DWORD Reserved,
    __in_opt LPWSTR lpClass,
    __in DWORD dwOptions,
    __in REGSAM samDesired,
    __in_opt CONST LPSECURITY_ATTRIBUTES lpSecurityAttributes,
    __out PHKEY phkResult,
    __out_opt LPDWORD lpdwDisposition
    );
static LSTATUS APIENTRY RegistrationTest_RegOpenKeyExW(
    __in HKEY hKey,
    __in_opt LPCWSTR lpSubKey,
    __reserved DWORD ulOptions,
    __in REGSAM samDesired,
    __out PHKEY phkResult
    );
static LSTATUS APIENTRY RegistrationTest_RegDeleteKeyExW(
    __in HKEY hKey,
    __in LPCWSTR lpSubKey,
    __in REGSAM samDesired,
    __reserved DWORD Reserved
    );


using namespace System;
using namespace System::IO;
using namespace Microsoft::VisualStudio::TestTools::UnitTesting;
using namespace Microsoft::Win32;


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
    [TestClass]
    public ref class RegistrationTest : BurnUnitTest
    {
    public:
        [TestMethod]
        void RegisterBasicTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"));
            try
            {
                // set mock API's
                RegFunctionOverride(RegistrationTest_RegCreateKeyExW, RegistrationTest_RegOpenKeyExW, RegistrationTest_RegDeleteKeyExW, NULL, NULL, NULL, NULL, NULL, NULL);

                Registry::CurrentUser->CreateSubKey(gcnew String(HKCU_PATH));

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43e6-9C67-B5652240618C}' UpgradeCode='{D54F896D-1952-43e6-9C67-B5652240618C}' Tag='foo' ProviderKey='foo' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' DisplayName='RegisterBasicTest' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                hr = PlanSetResumeCommand(&registration, BOOTSTRAPPER_ACTION_INSTALL, &command, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, &userExperience, BOOTSTRAPPER_ACTION_INSTALL, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, 0);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was created
                Assert::IsTrue(Directory::Exists(cacheDirectory));
                Assert::IsTrue(File::Exists(Path::Combine(cacheDirectory, gcnew String(L"setup.exe"))));

                Assert::AreEqual(Int32(BURN_RESUME_MODE_ACTIVE), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::AreEqual(String::Concat(L"\"", Path::Combine(cacheDirectory, gcnew String(L"setup.exe")), L"\" /burn.log.append \"BurnUnitTest.txt\" /burn.runonce"), Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // end session
                hr = RegistrationSessionEnd(&registration, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was removed
                Assert::IsFalse(Directory::Exists(cacheDirectory));

                Assert::AreEqual(nullptr, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::AreEqual(nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                Registry::CurrentUser->DeleteSubKeyTree(gcnew String(ROOT_PATH));
                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                RegFunctionOverride(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
            }
        }

        [TestMethod]
        void RegisterArpMinimumTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"));
            try
            {
                // set mock API's
                RegFunctionOverride(RegistrationTest_RegCreateKeyExW, RegistrationTest_RegOpenKeyExW, RegistrationTest_RegDeleteKeyExW, NULL, NULL, NULL, NULL, NULL, NULL);

                Registry::CurrentUser->CreateSubKey(gcnew String(HKCU_PATH));

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43e6-9C67-B5652240618C}' UpgradeCode='{D54F896D-1952-43e6-9C67-B5652240618C}' Tag='foo' ProviderKey='foo' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' DisplayName='Product1' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                hr = PlanSetResumeCommand(&registration, BOOTSTRAPPER_ACTION_INSTALL, &command, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                //
                // install
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, &userExperience, BOOTSTRAPPER_ACTION_INSTALL, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, 0);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was created
                Assert::AreEqual(Int32(BURN_RESUME_MODE_ACTIVE), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::AreEqual(String::Concat(L"\"", Path::Combine(cacheDirectory, gcnew String(L"setup.exe")), L"\" /burn.log.append \"BurnUnitTest.txt\" /burn.runonce"), Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // complete registration
                hr = RegistrationSessionEnd(&registration, BURN_RESUME_MODE_ARP, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was updated
                Assert::AreEqual(Int32(BURN_RESUME_MODE_ARP), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::AreEqual(1, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Installed"), nullptr));
                Assert::AreEqual(nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                //
                // uninstall
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, &userExperience, BOOTSTRAPPER_ACTION_UNINSTALL, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER, 0);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was updated
                Assert::AreEqual(Int32(BURN_RESUME_MODE_ACTIVE), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::AreEqual(1, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Installed"), nullptr));
                Assert::AreEqual(String::Concat(L"\"", Path::Combine(cacheDirectory, gcnew String(L"setup.exe")), L"\" /burn.log.append \"BurnUnitTest.txt\" /burn.runonce"), Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // delete registration
                hr = RegistrationSessionEnd(&registration, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was removed
                Assert::AreEqual(nullptr, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::AreEqual(nullptr, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Installed"), nullptr));
                Assert::AreEqual(nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                Registry::CurrentUser->DeleteSubKeyTree(gcnew String(ROOT_PATH));
                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                RegFunctionOverride(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
            }
        }

        [TestMethod]
        void RegisterArpFullTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"));
            try
            {
                // set mock API's
                RegFunctionOverride(RegistrationTest_RegCreateKeyExW, RegistrationTest_RegOpenKeyExW, RegistrationTest_RegDeleteKeyExW, NULL, NULL, NULL, NULL, NULL, NULL);

                Registry::CurrentUser->CreateSubKey(gcnew String(HKCU_PATH));

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX UxDllPayloadId='ux.dll'>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43e6-9C67-B5652240618C}' UpgradeCode='{D54F896D-1952-43e6-9C67-B5652240618C}' Tag='foo' ProviderKey='foo' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' DisplayName='DisplayName1' DisplayVersion='1.2.3.4' Publisher='Publisher1' HelpLink='http://www.microsoft.com/help'"
                    L"             HelpTelephone='555-555-5555' AboutUrl='http://www.microsoft.com/about' UpdateUrl='http://www.microsoft.com/update'"
                    L"             Comments='Comments1' Contact='Contact1' DisableModify='yes' DisableRemove='yes' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                hr = PlanSetResumeCommand(&registration, BOOTSTRAPPER_ACTION_INSTALL, &command, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                //
                // install
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, &userExperience, BOOTSTRAPPER_ACTION_INSTALL, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, 0);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was created
                Assert::AreEqual(Int32(BURN_RESUME_MODE_ACTIVE), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::AreEqual(String::Concat(L"\"", Path::Combine(cacheDirectory, gcnew String(L"setup.exe")), L"\" /burn.log.append \"BurnUnitTest.txt\" /burn.runonce"), Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // finish registration
                hr = RegistrationSessionEnd(&registration, BURN_RESUME_MODE_ARP, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was updated
                Assert::AreEqual(Int32(BURN_RESUME_MODE_ARP), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::AreEqual(1, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Installed"), nullptr));
                Assert::AreEqual(nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                Assert::AreEqual(gcnew String(L"DisplayName1"), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"DisplayName"), nullptr));
                Assert::AreEqual(gcnew String(L"1.2.3.4"), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"DisplayVersion"), nullptr));
                Assert::AreEqual(gcnew String(L"Publisher1"), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Publisher"), nullptr));
                Assert::AreEqual(gcnew String(L"http://www.microsoft.com/help"), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"HelpLink"), nullptr));
                Assert::AreEqual(gcnew String(L"555-555-5555"), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"HelpTelephone"), nullptr));
                Assert::AreEqual(gcnew String(L"http://www.microsoft.com/about"), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"URLInfoAbout"), nullptr));
                Assert::AreEqual(gcnew String(L"http://www.microsoft.com/update"), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"URLUpdateInfo"), nullptr));
                Assert::AreEqual(gcnew String(L"Comments1"), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Comments"), nullptr));
                Assert::AreEqual(gcnew String(L"Contact1"), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Contact"), nullptr));
                Assert::AreEqual(1, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"NoModify"), nullptr));
                Assert::AreEqual(1, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"NoRemove"), nullptr));

                //
                // uninstall
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, &userExperience, BOOTSTRAPPER_ACTION_UNINSTALL, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER, 0);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was updated
                Assert::AreEqual(Int32(BURN_RESUME_MODE_ACTIVE), Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::AreEqual(String::Concat(L"\"", Path::Combine(cacheDirectory, gcnew String(L"setup.exe")), L"\" /burn.log.append \"BurnUnitTest.txt\" /burn.runonce"), Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // delete registration
                hr = RegistrationSessionEnd(&registration, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was removed
                Assert::AreEqual(nullptr, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Resume"), nullptr));
                Assert::AreEqual(nullptr, Registry::GetValue(gcnew String(TEST_UNINSTALL_KEY), gcnew String(L"Installed"), nullptr));
                Assert::AreEqual(nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                Registry::CurrentUser->DeleteSubKeyTree(gcnew String(ROOT_PATH));
                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }
            
                RegFunctionOverride(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
            }
        }

        [TestMethod]
        void ResumeTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            BYTE rgbData[256] = { };
            BOOTSTRAPPER_RESUME_TYPE resumeType = BOOTSTRAPPER_RESUME_TYPE_NONE;
            BYTE* pbBuffer = NULL;
            DWORD cbBuffer = 0;
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"));
            try
            {
                for (DWORD i = 0; i < 256; ++i)
                {
                    rgbData[i] = (BYTE)i;
                }

                // set mock API's
                RegFunctionOverride(RegistrationTest_RegCreateKeyExW, RegistrationTest_RegOpenKeyExW, RegistrationTest_RegDeleteKeyExW, NULL, NULL, NULL, NULL, NULL, NULL);

                Registry::CurrentUser->CreateSubKey(gcnew String(HKCU_PATH));

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43e6-9C67-B5652240618C}' UpgradeCode='{D54F896D-1952-43e6-9C67-B5652240618C}' Tag='foo' ProviderKey='foo' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' DisplayName='RegisterBasicTest' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                hr = PlanSetResumeCommand(&registration, BOOTSTRAPPER_ACTION_INSTALL, &command, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                // read resume type before session
                hr = RegistrationDetectResumeType(&registration, &resumeType);
                TestThrowOnFailure(hr, L"Failed to read resume type.");

                Assert::AreEqual((int)BOOTSTRAPPER_RESUME_TYPE_NONE, (int)resumeType);

                // begin session
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &variables, &userExperience, BOOTSTRAPPER_ACTION_INSTALL, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER, 0);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                hr = RegistrationSaveState(&registration, rgbData, sizeof(rgbData));
                TestThrowOnFailure(hr, L"Failed to save state.");

                // read interrupted resume type
                hr = RegistrationDetectResumeType(&registration, &resumeType);
                TestThrowOnFailure(hr, L"Failed to read interrupted resume type.");

                Assert::AreEqual((int)BOOTSTRAPPER_RESUME_TYPE_INTERRUPTED, (int)resumeType);

                // suspend session
                hr = RegistrationSessionEnd(&registration, BURN_RESUME_MODE_SUSPEND, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER);
                TestThrowOnFailure(hr, L"Failed to suspend session.");

                // verify that run key was removed
                Assert::AreEqual(nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // read suspend resume type
                hr = RegistrationDetectResumeType(&registration, &resumeType);
                TestThrowOnFailure(hr, L"Failed to read suspend resume type.");

                Assert::AreEqual((int)BOOTSTRAPPER_RESUME_TYPE_SUSPEND, (int)resumeType);

                // read state back
                hr = RegistrationLoadState(&registration, &pbBuffer, &cbBuffer);
                TestThrowOnFailure(hr, L"Failed to load state.");

                Assert::AreEqual(sizeof(rgbData), cbBuffer);
                Assert::IsTrue(0 == memcmp(pbBuffer, rgbData, sizeof(rgbData)));

                // write active resume mode
                hr = RegistrationSessionResume(&registration);
                TestThrowOnFailure(hr, L"Failed to write active resume mode.");

                // verify that run key was put back
                Assert::AreNotEqual(nullptr, Registry::GetValue(gcnew String(TEST_RUN_KEY), gcnew String(L"{D54F896D-1952-43e6-9C67-B5652240618C}"), nullptr));

                // end session
                hr = RegistrationSessionEnd(&registration, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // read resume type after session
                hr = RegistrationDetectResumeType(&registration, &resumeType);
                TestThrowOnFailure(hr, L"Failed to read resume type.");

                Assert::AreEqual((int)BOOTSTRAPPER_RESUME_TYPE_NONE, (int)resumeType);
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                Registry::CurrentUser->DeleteSubKeyTree(gcnew String(ROOT_PATH));
                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                RegFunctionOverride(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
            }
        }

    //BOOTSTRAPPER_RESUME_TYPE_NONE,
    //BOOTSTRAPPER_RESUME_TYPE_INVALID,        // resume information is present but invalid
    //BOOTSTRAPPER_RESUME_TYPE_UNEXPECTED,     // relaunched after an unexpected interruption
    //BOOTSTRAPPER_RESUME_TYPE_REBOOT_PENDING, // reboot has not taken place yet
    //BOOTSTRAPPER_RESUME_TYPE_REBOOT,         // relaunched after reboot
    //BOOTSTRAPPER_RESUME_TYPE_SUSPEND,        // relaunched after suspend
    //BOOTSTRAPPER_RESUME_TYPE_ARP,            // launched from ARP
    };
}
}
}
}
}


static LSTATUS APIENTRY RegistrationTest_RegCreateKeyExW(
    __in HKEY hKey,
    __in LPCWSTR lpSubKey,
    __reserved DWORD Reserved,
    __in_opt LPWSTR lpClass,
    __in DWORD dwOptions,
    __in REGSAM samDesired,
    __in_opt CONST LPSECURITY_ATTRIBUTES lpSecurityAttributes,
    __out PHKEY phkResult,
    __out_opt LPDWORD lpdwDisposition
    )
{
    LSTATUS ls = ERROR_SUCCESS;
    LPCWSTR wzRoot = NULL;
    HKEY hkRoot = NULL;

    if (HKEY_LOCAL_MACHINE == hKey)
    {
        wzRoot = HKLM_PATH;
    }
    else if (HKEY_CURRENT_USER == hKey)
    {
        wzRoot = HKCU_PATH;
    }
    else
    {
        hkRoot = hKey;
    }

    if (wzRoot)
    {
        ls = ::RegOpenKeyExW(HKEY_CURRENT_USER, wzRoot, 0, KEY_WRITE, &hkRoot);
        if (ERROR_SUCCESS != ls)
        {
            ExitFunction();
        }
    }

    ls = ::RegCreateKeyExW(hkRoot, lpSubKey, Reserved, lpClass, dwOptions, samDesired, lpSecurityAttributes, phkResult, lpdwDisposition);

LExit:
    ReleaseRegKey(hkRoot);

    return ls;
}

static LSTATUS APIENTRY RegistrationTest_RegOpenKeyExW(
    __in HKEY hKey,
    __in_opt LPCWSTR lpSubKey,
    __reserved DWORD ulOptions,
    __in REGSAM samDesired,
    __out PHKEY phkResult
    )
{
    LSTATUS ls = ERROR_SUCCESS;
    LPCWSTR wzRoot = NULL;
    HKEY hkRoot = NULL;

    if (HKEY_LOCAL_MACHINE == hKey)
    {
        wzRoot = HKLM_PATH;
    }
    else if (HKEY_CURRENT_USER == hKey)
    {
        wzRoot = HKCU_PATH;
    }
    else
    {
        hkRoot = hKey;
    }

    if (wzRoot)
    {
        ls = ::RegOpenKeyExW(HKEY_CURRENT_USER, wzRoot, 0, KEY_WRITE, &hkRoot);
        if (ERROR_SUCCESS != ls)
        {
            ExitFunction();
        }
    }

    ls = ::RegOpenKeyExW(hkRoot, lpSubKey, ulOptions, samDesired, phkResult);

LExit:
    ReleaseRegKey(hkRoot);

    return ls;
}

static LSTATUS APIENTRY RegistrationTest_RegDeleteKeyExW(
    __in HKEY hKey,
    __in LPCWSTR lpSubKey,
    __in REGSAM samDesired,
    __reserved DWORD Reserved
    )
{
    LSTATUS ls = ERROR_SUCCESS;
    LPCWSTR wzRoot = NULL;
    HKEY hkRoot = NULL;

    if (HKEY_LOCAL_MACHINE == hKey)
    {
        wzRoot = HKLM_PATH;
    }
    else if (HKEY_CURRENT_USER == hKey)
    {
        wzRoot = HKCU_PATH;
    }
    else
    {
        hkRoot = hKey;
    }

    if (wzRoot)
    {
        ls = ::RegOpenKeyExW(HKEY_CURRENT_USER, wzRoot, 0, KEY_WRITE | samDesired, &hkRoot);
        if (ERROR_SUCCESS != ls)
        {
            ExitFunction();
        }
    }

    ls = ::RegDeleteKeyExW(hkRoot, lpSubKey, samDesired, Reserved);

LExit:
    ReleaseRegKey(hkRoot);

    return ls;
}
