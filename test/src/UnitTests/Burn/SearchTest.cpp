//-------------------------------------------------------------------------------------------------
// <copyright file="SearchTest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Search unit tests for Burn.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


static INSTALLSTATE WINAPI MsiComponentSearchTest_MsiGetComponentPathW(
    __in LPCWSTR szProduct,
    __in LPCWSTR szComponent,
    __out_ecount_opt(*pcchBuf) LPWSTR lpPathBuf,
    __inout_opt LPDWORD pcchBuf
    );
static INSTALLSTATE WINAPI MsiComponentSearchTest_MsiLocateComponentW(
    __in LPCWSTR szComponent,
    __out_ecount_opt(*pcchBuf) LPWSTR lpPathBuf,
    __inout_opt LPDWORD pcchBuf
    );
static UINT WINAPI MsiProductSearchTest_MsiGetProductInfoW(
    __in LPCWSTR szProductCode,
    __in LPCWSTR szProperty,
    __out_ecount_opt(*pcchValue) LPWSTR szValue,
    __inout_opt LPDWORD pcchValue
    );
static UINT WINAPI MsiProductSearchTest_MsiGetProductInfoExW(
    __in LPCWSTR szProductCode,
    __in_opt LPCWSTR szUserSid,
    __in MSIINSTALLCONTEXT dwContext,
    __in LPCWSTR szProperty,
    __out_ecount_opt(*pcchValue) LPWSTR szValue,
    __inout_opt LPDWORD pcchValue
    );


using namespace System;
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
    public ref class SearchTest : BurnUnitTest
    {
    public:
        [TestMethod]
        void DirectorySearchTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            BURN_VARIABLES variables = { };
            BURN_SEARCHES searches = { };
            try
            {
                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                pin_ptr<const WCHAR> wzDirectory1 = PtrToStringChars(this->TestContext->TestDir);
                pin_ptr<const WCHAR> wzDirectory2 = PtrToStringChars(System::IO::Path::Combine(this->TestContext->TestDir, gcnew String(L"none")));

                VariableSetStringHelper(&variables, L"Directory1", wzDirectory1);
                VariableSetStringHelper(&variables, L"Directory2", wzDirectory2);

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <DirectorySearch Id='Search1' Type='exists' Path='[Directory1]' Variable='Variable1' />"
                    L"    <DirectorySearch Id='Search2' Type='exists' Path='[Directory2]' Variable='Variable2' />"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = SearchesParseFromXml(&searches, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse searches from XML.");

                // execute searches
                hr = SearchesExecute(&searches, &variables);
                TestThrowOnFailure(hr, L"Failed to execute searches.");

                // check variable values
                Assert::AreEqual(1ll, VariableGetNumericHelper(&variables, L"Variable1"));
                Assert::AreEqual(0ll, VariableGetNumericHelper(&variables, L"Variable2"));
            }
            finally
            {
                ReleaseObject(pixeBundle);
                VariablesUninitialize(&variables);
                SearchesUninitialize(&searches);
            }
        }

        [TestMethod]
        void FileSearchTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            BURN_VARIABLES variables = { };
            BURN_SEARCHES searches = { };
            ULARGE_INTEGER uliVersion = { };
            try
            {
                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                pin_ptr<const WCHAR> wzFile1 = PtrToStringChars(System::IO::Path::Combine(this->TestContext->TestDir, gcnew String(L"none.txt")));
                pin_ptr<const WCHAR> wzFile2 = PtrToStringChars(System::Reflection::Assembly::GetExecutingAssembly()->Location);

                hr = FileVersion(wzFile2, &uliVersion.HighPart, &uliVersion.LowPart);
                TestThrowOnFailure(hr, L"Failed to get DLL version.");

                VariableSetStringHelper(&variables, L"File1", wzFile1);
                VariableSetStringHelper(&variables, L"File2", wzFile2);

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <FileSearch Id='Search1' Type='exists' Path='[File1]' Variable='Variable1' />"
                    L"    <FileSearch Id='Search2' Type='exists' Path='[File2]' Variable='Variable2' />"
                    L"    <FileSearch Id='Search3' Type='version' Path='[File2]' Variable='Variable3' />"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = SearchesParseFromXml(&searches, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse searches from XML.");

                // execute searches
                hr = SearchesExecute(&searches, &variables);
                TestThrowOnFailure(hr, L"Failed to execute searches.");

                // check variable values
                Assert::AreEqual(0ll, VariableGetNumericHelper(&variables, L"Variable1"));
                Assert::AreEqual(1ll, VariableGetNumericHelper(&variables, L"Variable2"));
                Assert::AreEqual(uliVersion.QuadPart, VariableGetVersionHelper(&variables, L"Variable3"));
            }
            finally
            {
                ReleaseObject(pixeBundle);
                VariablesUninitialize(&variables);
                SearchesUninitialize(&searches);
            }
        }

        [TestMethod]
        void RegistrySearchTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            BURN_VARIABLES variables = { };
            BURN_SEARCHES searches = { };
            HKEY hkey32 = NULL;
            HKEY hkey64 = NULL;
            BOOL f64bitMachine = (nullptr != Environment::GetEnvironmentVariable("ProgramFiles(x86)"));

            try
            {
                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                Registry::SetValue(gcnew String(L"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value"), gcnew String(L"String"), gcnew String(L"String1 %TEMP%"), RegistryValueKind::String);
                Registry::SetValue(gcnew String(L"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value"), gcnew String(L"StringExpand"), gcnew String(L"String1 %TEMP%"), RegistryValueKind::ExpandString);
                Registry::SetValue(gcnew String(L"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value"), gcnew String(L"DWord"), 1, RegistryValueKind::DWord);
                Registry::SetValue(gcnew String(L"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value"), gcnew String(L"QWord"), 1ll, RegistryValueKind::QWord);
                Registry::SetValue(gcnew String(L"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value"), gcnew String(L"VersionString"), gcnew String(L"1.1.1.1"), RegistryValueKind::String);
                Registry::SetValue(gcnew String(L"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value"), gcnew String(L"VersionQWord"), MAKEQWORDVERSION(1,1,1,1), RegistryValueKind::QWord);
                Registry::SetValue(gcnew String(L"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\String"), nullptr, gcnew String(L"String1"), RegistryValueKind::String);
                Registry::SetValue(gcnew String(L"HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Numeric"), nullptr, 1ll, RegistryValueKind::DWord);

                if (f64bitMachine)
                {
                    hr = RegCreate(HKEY_CURRENT_USER, L"SOFTWARE\\Classes\\CLSID\\WiX_Burn_UnitTest\\Bitness\\", KEY_WRITE | KEY_WOW64_32KEY, &hkey32);
                    Assert::IsTrue(SUCCEEDED(hr));

                    hr = RegCreate(HKEY_CURRENT_USER, L"SOFTWARE\\Classes\\CLSID\\WiX_Burn_UnitTest\\Bitness\\", KEY_WRITE | KEY_WOW64_64KEY, &hkey64);
                    Assert::IsTrue(SUCCEEDED(hr));

                    hr = RegWriteString(hkey64, L"TestStringSpecificToBitness", L"64-bit");
                    Assert::IsTrue(SUCCEEDED(hr));

                    hr = RegWriteString(hkey32, L"TestStringSpecificToBitness", L"32-bit");
                    Assert::IsTrue(SUCCEEDED(hr));
                }

                VariableSetStringHelper(&variables, L"MyKey", L"SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value");
                VariableSetStringHelper(&variables, L"MyValue", L"String");

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <RegistrySearch Id='Search1' Type='exists' Root='HKLM' Key='SOFTWARE\\Microsoft' Variable='Variable1' />"
                    L"    <RegistrySearch Id='Search2' Type='exists' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\None' Variable='Variable2' />"
                    L"    <RegistrySearch Id='Search3' Type='exists' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value' Value='None' Variable='Variable3' />"
                    L"    <RegistrySearch Id='Search4' Type='exists' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value' Value='String' Variable='Variable4' />"
                    L"    <RegistrySearch Id='Search5' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value' Value='String' Variable='Variable5' VariableType='string' />"
                    L"    <RegistrySearch Id='Search6' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value' Value='String' Variable='Variable6' VariableType='string' ExpandEnvironment='no' />"
                    L"    <RegistrySearch Id='Search7' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value' Value='String' Variable='Variable7' VariableType='string' ExpandEnvironment='yes' />"
                    L"    <RegistrySearch Id='Search8' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value' Value='StringExpand' Variable='Variable8' VariableType='string' />"
                    L"    <RegistrySearch Id='Search9' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value' Value='StringExpand' Variable='Variable9' VariableType='string' ExpandEnvironment='no' />"
                    L"    <RegistrySearch Id='Search10' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value' Value='StringExpand' Variable='Variable10' VariableType='string' ExpandEnvironment='yes' />"
                    L"    <RegistrySearch Id='Search11' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value' Value='DWord' Variable='Variable11' VariableType='numeric' />"
                    L"    <RegistrySearch Id='Search12' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value' Value='QWord' Variable='Variable12' VariableType='numeric' />"
                    L"    <RegistrySearch Id='Search13' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value' Value='VersionString' Variable='Variable13' VariableType='version' />"
                    L"    <RegistrySearch Id='Search14' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value' Value='VersionQWord' Variable='Variable14' VariableType='version' />"
                    L"    <RegistrySearch Id='Search15' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\String' Variable='Variable15' VariableType='string' />"
                    L"    <RegistrySearch Id='Search16' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Numeric' Variable='Variable16' VariableType='numeric' />"
                    L"    <RegistrySearch Id='Search17' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\None' Variable='Variable17' VariableType='numeric' />"
                    L"    <RegistrySearch Id='Search18' Type='value' Root='HKCU' Key='SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Numeric' Value='None' Variable='Variable18' VariableType='numeric' />"
                    L"    <RegistrySearch Id='Search19' Type='exists' Root='HKCU' Key='[MyKey]' Value='[MyValue]' Variable='Variable19' />"
                    L"    <RegistrySearch Id='Search20' Type='value' Root='HKCU' Key='[MyKey]' Value='[MyValue]' Variable='Variable20' VariableType='string' />"
                    L"    <RegistrySearch Id='Search21' Type='value' Root='HKCU' Key='SOFTWARE\\Classes\\CLSID\\WiX_Burn_UnitTest\\Bitness' Value='TestStringSpecificToBitness' Variable='Variable21' VariableType='string' Win64='no' />"
                    L"    <RegistrySearch Id='Search22' Type='value' Root='HKCU' Key='SOFTWARE\\Classes\\CLSID\\WiX_Burn_UnitTest\\Bitness' Value='TestStringSpecificToBitness' Variable='Variable22' VariableType='string' Win64='yes' />"
                    L"    <RegistrySearch Id='Search23' Type='exists' Root='HKU' Key='.DEFAULT\\System\\CurrentControlSet' Variable='Variable23' />"
                    L"    <RegistrySearch Id='Search23' Type='exists' Root='HKU' Key='.DEFAULT\\System\\NetworkServiceSidSubkeyDoesNotExist' Variable='Variable24' />"
                    L"    <RegistrySearch Id='Search24' Type='value' Root='HKCR' Key='.msi' Variable='Variable25' VariableType='string' />"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = SearchesParseFromXml(&searches, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse searches from XML.");

                // execute searches
                hr = SearchesExecute(&searches, &variables);
                TestThrowOnFailure(hr, L"Failed to execute searches.");

                // check variable values
                Assert::AreEqual(1ll, VariableGetNumericHelper(&variables, L"Variable1"));
                Assert::AreEqual(0ll, VariableGetNumericHelper(&variables, L"Variable2"));
                Assert::AreEqual(0ll, VariableGetNumericHelper(&variables, L"Variable3"));
                Assert::AreEqual(1ll, VariableGetNumericHelper(&variables, L"Variable4"));
                Assert::AreEqual(gcnew String(L"String1 %TEMP%"), VariableGetStringHelper(&variables, L"Variable5"));
                Assert::AreEqual(gcnew String(L"String1 %TEMP%"), VariableGetStringHelper(&variables, L"Variable6"));
                Assert::AreEqual(gcnew String(L"String1 %TEMP%"), VariableGetStringHelper(&variables, L"Variable7"));
                Assert::AreEqual(gcnew String(L"String1 %TEMP%"), VariableGetStringHelper(&variables, L"Variable8"));
                Assert::AreEqual(gcnew String(L"String1 %TEMP%"), VariableGetStringHelper(&variables, L"Variable9"));
                Assert::AreNotEqual(gcnew String(L"String1 %TEMP%"), VariableGetStringHelper(&variables, L"Variable10"));
                Assert::AreEqual(1ll, VariableGetNumericHelper(&variables, L"Variable11"));
                Assert::AreEqual(1ll, VariableGetNumericHelper(&variables, L"Variable12"));
                Assert::AreEqual(MAKEQWORDVERSION(1,1,1,1), VariableGetVersionHelper(&variables, L"Variable13"));
                Assert::AreEqual(MAKEQWORDVERSION(1,1,1,1), VariableGetVersionHelper(&variables, L"Variable14"));
                Assert::AreEqual(gcnew String(L"String1"), VariableGetStringHelper(&variables, L"Variable15"));
                Assert::AreEqual(1ll, VariableGetNumericHelper(&variables, L"Variable16"));
                Assert::IsFalse(VariableExistsHelper(&variables, L"Variable17"));
                Assert::IsFalse(VariableExistsHelper(&variables, L"Variable18"));
                Assert::AreEqual(1ll, VariableGetNumericHelper(&variables, L"Variable19"));
                Assert::AreEqual(gcnew String(L"String1 %TEMP%"), VariableGetStringHelper(&variables, L"Variable20"));
                if (f64bitMachine)
                {
                    Assert::AreEqual(gcnew String(L"32-bit"), VariableGetStringHelper(&variables, L"Variable21"));
                    Assert::AreEqual(gcnew String(L"64-bit"), VariableGetStringHelper(&variables, L"Variable22"));
                }

                Assert::AreEqual(1ll, VariableGetNumericHelper(&variables, L"Variable23"));
                Assert::AreEqual(0ll, VariableGetNumericHelper(&variables, L"Variable24"));
                Assert::AreEqual(gcnew String(L"Msi.Package"), VariableGetStringHelper(&variables, L"Variable25"));
            }
            finally
            {
                ReleaseRegKey(hkey32);
                ReleaseRegKey(hkey64);
                ReleaseObject(pixeBundle);
                VariablesUninitialize(&variables);
                SearchesUninitialize(&searches);

                Registry::CurrentUser->DeleteSubKeyTree(gcnew String(L"SOFTWARE\\Microsoft\\WiX_Burn_UnitTest"));
                if (f64bitMachine)
                {
                    RegDelete(HKEY_CURRENT_USER, L"SOFTWARE\\Classes\\CLSID\\WiX_Burn_UnitTest\\Bitness", REG_KEY_32BIT, FALSE);
                    RegDelete(HKEY_CURRENT_USER, L"SOFTWARE\\Classes\\CLSID\\WiX_Burn_UnitTest", REG_KEY_32BIT, FALSE);
                    RegDelete(HKEY_CURRENT_USER, L"SOFTWARE\\Classes\\CLSID\\WiX_Burn_UnitTest\\Bitness", REG_KEY_64BIT, FALSE);
                    RegDelete(HKEY_CURRENT_USER, L"SOFTWARE\\Classes\\CLSID\\WiX_Burn_UnitTest", REG_KEY_64BIT, FALSE);
                }
            }
        }

        [TestMethod]
        void MsiComponentSearchTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            BURN_VARIABLES variables = { };
            BURN_SEARCHES searches = { };
            try
            {
                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                // set mock API's
                WiuFunctionOverride(NULL, MsiComponentSearchTest_MsiGetComponentPathW, MsiComponentSearchTest_MsiLocateComponentW, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <MsiComponentSearch Id='Search1' Type='state' ComponentId='{BAD00000-1000-0000-0000-000000000000}' Variable='Variable1' />"
                    L"    <MsiComponentSearch Id='Search2' Type='state' ProductCode='{BAD00000-0000-0000-0000-000000000000}' ComponentId='{BAD00000-1000-0000-0000-000000000000}' Variable='Variable2' />"
                    L"    <MsiComponentSearch Id='Search3' Type='state' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{BAD00000-1000-0000-0000-000000000000}' Variable='Variable3' />"
                    L"    <MsiComponentSearch Id='Search4' Type='keyPath' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-1000-0000-000000000000}' Variable='Variable4' />"
                    L"    <MsiComponentSearch Id='Search5' Type='keyPath' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-2000-0000-000000000000}' Variable='Variable5' />"
                    L"    <MsiComponentSearch Id='Search6' Type='keyPath' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-3000-0000-000000000000}' Variable='Variable6' />"
                    L"    <MsiComponentSearch Id='Search7' Type='keyPath' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-4000-0000-000000000000}' Variable='Variable7' />"
                    L"    <MsiComponentSearch Id='Search8' Type='keyPath' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-5000-0000-000000000000}' Variable='Variable8' />"
                    L"    <MsiComponentSearch Id='Search9' Type='keyPath' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-6000-0000-000000000000}' Variable='Variable9' />" // todo: value key path
                    L"    <MsiComponentSearch Id='Search10' Type='state' ComponentId='{600D0000-1000-1000-0000-000000000000}' Variable='Variable10' />"
                    L"    <MsiComponentSearch Id='Search11' Type='state' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-1000-0000-000000000000}' Variable='Variable11' />"
                    L"    <MsiComponentSearch Id='Search12' Type='state' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-2000-0000-000000000000}' Variable='Variable12' />"
                    L"    <MsiComponentSearch Id='Search13' Type='state' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-3000-0000-000000000000}' Variable='Variable13' />"
                    L"    <MsiComponentSearch Id='Search14' Type='state' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-4000-0000-000000000000}' Variable='Variable14' />"
                    L"    <MsiComponentSearch Id='Search15' Type='directory' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-1000-0000-000000000000}' Variable='Variable15' />"
                    L"    <MsiComponentSearch Id='Search16' Type='directory' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-2000-0000-000000000000}' Variable='Variable16' />"
                    L"    <MsiComponentSearch Id='Search17' Type='directory' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-3000-0000-000000000000}' Variable='Variable17' />"
                    L"    <MsiComponentSearch Id='Search18' Type='directory' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-4000-0000-000000000000}' Variable='Variable18' />"
                    L"    <MsiComponentSearch Id='Search19' Type='keyPath' ProductCode='{600D0000-0000-0000-0000-000000000000}' ComponentId='{600D0000-1000-7000-0000-000000000000}' Variable='Variable19' />"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = SearchesParseFromXml(&searches, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse searches from XML.");

                // execute searches
                hr = SearchesExecute(&searches, &variables);
                TestThrowOnFailure(hr, L"Failed to execute searches.");

                // check variable values
                Assert::AreEqual(2ll, VariableGetNumericHelper(&variables, L"Variable1"));
                Assert::AreEqual(2ll, VariableGetNumericHelper(&variables, L"Variable2"));
                Assert::AreEqual(2ll, VariableGetNumericHelper(&variables, L"Variable3"));
                Assert::AreEqual(gcnew String(L"C:\\directory\\file1.txt"), VariableGetStringHelper(&variables, L"Variable4"));
                Assert::AreEqual(gcnew String(L"C:\\directory\\file2.txt"), VariableGetStringHelper(&variables, L"Variable5"));
                Assert::AreEqual(gcnew String(L"C:\\directory\\file3.txt"), VariableGetStringHelper(&variables, L"Variable6"));
                Assert::AreEqual(gcnew String(L"C:\\directory\\file4.txt"), VariableGetStringHelper(&variables, L"Variable7"));
                Assert::AreEqual(gcnew String(L"02:\\SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\"), VariableGetStringHelper(&variables, L"Variable8"));
                Assert::AreEqual(gcnew String(L"02:\\SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value"), VariableGetStringHelper(&variables, L"Variable9"));
                Assert::AreEqual(3ll, VariableGetNumericHelper(&variables, L"Variable10"));
                Assert::AreEqual(3ll, VariableGetNumericHelper(&variables, L"Variable11"));
                Assert::AreEqual(4ll, VariableGetNumericHelper(&variables, L"Variable12"));
                Assert::AreEqual(4ll, VariableGetNumericHelper(&variables, L"Variable13"));
                Assert::AreEqual(2ll, VariableGetNumericHelper(&variables, L"Variable14"));
                Assert::AreEqual(gcnew String(L"C:\\directory\\"), VariableGetStringHelper(&variables, L"Variable15"));
                Assert::AreEqual(gcnew String(L"C:\\directory\\"), VariableGetStringHelper(&variables, L"Variable16"));
                Assert::AreEqual(gcnew String(L"C:\\directory\\"), VariableGetStringHelper(&variables, L"Variable17"));
                Assert::AreEqual(gcnew String(L"C:\\directory\\"), VariableGetStringHelper(&variables, L"Variable18"));
                Assert::AreEqual(gcnew String(L"C:\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\file5.txt"), VariableGetStringHelper(&variables, L"Variable19"));
            }
            finally
            {
                ReleaseObject(pixeBundle);
                VariablesUninitialize(&variables);
                SearchesUninitialize(&searches);
            }
        }

        [TestMethod]
        void MsiProductSearchTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            BURN_VARIABLES variables = { };
            BURN_SEARCHES searches = { };
            try
            {
                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                // set mock API's
                WiuFunctionOverride(NULL, NULL, NULL, NULL, MsiProductSearchTest_MsiGetProductInfoW, MsiProductSearchTest_MsiGetProductInfoExW, NULL, NULL, NULL, NULL, NULL, NULL, NULL);

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <MsiProductSearch Id='Search1' Type='state' ProductCode='{BAD00000-0000-0000-0000-000000000000}' Variable='Variable1' />"
                    L"    <MsiProductSearch Id='Search2' Type='version' ProductCode='{600D0000-0000-0000-0000-000000000000}' Variable='Variable2' />"
                    L"    <MsiProductSearch Id='Search3' Type='language' ProductCode='{600D0000-0000-0000-0000-000000000000}' Variable='Variable3' />"
                    L"    <MsiProductSearch Id='Search4' Type='state' ProductCode='{600D0000-0000-0000-0000-000000000000}' Variable='Variable4' />"
                    L"    <MsiProductSearch Id='Search5' Type='assignment' ProductCode='{600D0000-0000-0000-0000-000000000000}' Variable='Variable5' />"
                    L"    <MsiProductSearch Id='Search6' Type='version' ProductCode='{600D0000-1000-0000-0000-000000000000}' Variable='Variable6' />"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = SearchesParseFromXml(&searches, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse searches from XML.");

                // execute searches
                hr = SearchesExecute(&searches, &variables);
                TestThrowOnFailure(hr, L"Failed to execute searches.");

                // check variable values
                Assert::AreEqual(2ll, VariableGetNumericHelper(&variables, L"Variable1"));
                Assert::AreEqual(MAKEQWORDVERSION(1,0,0,0), VariableGetVersionHelper(&variables, L"Variable2"));
                Assert::AreEqual(1033ll, VariableGetNumericHelper(&variables, L"Variable3"));
                Assert::AreEqual(5ll, VariableGetNumericHelper(&variables, L"Variable4"));
                Assert::AreEqual(1ll, VariableGetNumericHelper(&variables, L"Variable5"));
                Assert::AreEqual(MAKEQWORDVERSION(1,0,0,0), VariableGetVersionHelper(&variables, L"Variable6"));
            }
            finally
            {
                ReleaseObject(pixeBundle);
                VariablesUninitialize(&variables);
                SearchesUninitialize(&searches);
            }
        }

        [TestMethod]
        void MsiFeatureSearchTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            BURN_VARIABLES variables = { };
            BURN_SEARCHES searches = { };
            try
            {
                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <MsiFeatureSearch Id='Search1' Type='state' ProductCode='{BAD00000-0000-0000-0000-000000000000}' FeatureId='' Variable='Variable1' />"
                    L"</Bundle>";

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = SearchesParseFromXml(&searches, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse searches from XML.");

                // execute searches
                hr = SearchesExecute(&searches, &variables);
                TestThrowOnFailure(hr, L"Failed to execute searches.");
            }
            finally
            {
                ReleaseObject(pixeBundle);
                VariablesUninitialize(&variables);
                SearchesUninitialize(&searches);
            }
        }

        [TestMethod]
        void ConditionalSearchTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            BURN_VARIABLES variables = { };
            BURN_SEARCHES searches = { };
            try
            {
                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <RegistrySearch Id='Search1' Type='exists' Root='HKLM' Key='SOFTWARE\\Microsoft' Variable='Variable1' Condition='0' />"
                    L"    <RegistrySearch Id='Search2' Type='exists' Root='HKLM' Key='SOFTWARE\\Microsoft' Variable='Variable2' Condition='1' />"
                    L"    <RegistrySearch Id='Search3' Type='exists' Root='HKLM' Key='SOFTWARE\\Microsoft' Variable='Variable3' Condition='=' />"
                    L"</Bundle>";

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = SearchesParseFromXml(&searches, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse searches from XML.");

                // execute searches
                hr = SearchesExecute(&searches, &variables);
                TestThrowOnFailure(hr, L"Failed to execute searches.");

                // check variable values
                Assert::IsFalse(VariableExistsHelper(&variables, L"Variable1"));
                Assert::AreEqual(1ll, VariableGetNumericHelper(&variables, L"Variable2"));
                Assert::IsFalse(VariableExistsHelper(&variables, L"Variable3"));
            }
            finally
            {
                ReleaseObject(pixeBundle);
                VariablesUninitialize(&variables);
                SearchesUninitialize(&searches);
            }
        }
        [TestMethod]
        void NoSearchesTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            BURN_VARIABLES variables = { };
            BURN_SEARCHES searches = { };
            try
            {
                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"</Bundle>";

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = SearchesParseFromXml(&searches, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse searches from XML.");

                // execute searches
                hr = SearchesExecute(&searches, &variables);
                TestThrowOnFailure(hr, L"Failed to execute searches.");
            }
            finally
            {
                ReleaseObject(pixeBundle);
                VariablesUninitialize(&variables);
                SearchesUninitialize(&searches);
            }
        }
    };
}
}
}
}
}


static INSTALLSTATE WINAPI MsiComponentSearchTest_MsiGetComponentPathW(
    __in LPCWSTR szProduct,
    __in LPCWSTR szComponent,
    __out_ecount_opt(*pcchBuf) LPWSTR lpPathBuf,
    __inout_opt LPDWORD pcchBuf
    )
{
    INSTALLSTATE is = INSTALLSTATE_INVALIDARG;
    String^ product = gcnew String(szProduct);

    if (String::Equals(product, gcnew String(L"{BAD00000-0000-0000-0000-000000000000}")))
    {
        is = INSTALLSTATE_UNKNOWN;
    }
    else if (String::Equals(product, gcnew String(L"{600D0000-0000-0000-0000-000000000000}")))
    {
        is = MsiComponentSearchTest_MsiLocateComponentW(szComponent, lpPathBuf, pcchBuf);
    }

    return is;
}

static INSTALLSTATE WINAPI MsiComponentSearchTest_MsiLocateComponentW(
    __in LPCWSTR szComponent,
    __out_ecount_opt(*pcchBuf) LPWSTR lpPathBuf,
    __inout_opt LPDWORD pcchBuf
    )
{
    HRESULT hr = S_OK;
    INSTALLSTATE is = INSTALLSTATE_INVALIDARG;
    String^ component = gcnew String(szComponent);
    LPCWSTR wzValue = NULL;

    if (String::Equals(component, gcnew String(L"{BAD00000-1000-0000-0000-000000000000}")))
    {
        is = INSTALLSTATE_UNKNOWN;
    }
    else if (String::Equals(component, gcnew String(L"{600D0000-1000-1000-0000-000000000000}")))
    {
        wzValue = L"C:\\directory\\file1.txt";
        is = INSTALLSTATE_LOCAL;
    }
    else if (String::Equals(component, gcnew String(L"{600D0000-1000-2000-0000-000000000000}")))
    {
        wzValue = L"C:\\directory\\file2.txt";
        is = INSTALLSTATE_SOURCE;
    }
    else if (String::Equals(component, gcnew String(L"{600D0000-1000-3000-0000-000000000000}")))
    {
        wzValue = L"C:\\directory\\file3.txt";
        is = INSTALLSTATE_SOURCEABSENT;
    }
    else if (String::Equals(component, gcnew String(L"{600D0000-1000-4000-0000-000000000000}")))
    {
        wzValue = L"C:\\directory\\file4.txt";
        is = INSTALLSTATE_ABSENT;
    }
    else if (String::Equals(component, gcnew String(L"{600D0000-1000-5000-0000-000000000000}")))
    {
        wzValue = L"02:\\SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\";
        is = INSTALLSTATE_LOCAL;
    }
    else if (String::Equals(component, gcnew String(L"{600D0000-1000-6000-0000-000000000000}")))
    {
        wzValue = L"02:\\SOFTWARE\\Microsoft\\WiX_Burn_UnitTest\\Value";
        is = INSTALLSTATE_LOCAL;
    }
    else if (String::Equals(component, gcnew String(L"{600D0000-1000-7000-0000-000000000000}")))
    {
        wzValue = L"C:\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\directory\\file5.txt";
        is = INSTALLSTATE_ABSENT;
    }

    if (wzValue && lpPathBuf)
    {
        hr = ::StringCchCopyW(lpPathBuf, *pcchBuf, wzValue);
        if (STRSAFE_E_INSUFFICIENT_BUFFER == hr)
        {
            *pcchBuf = lstrlenW(wzValue);
            is = INSTALLSTATE_MOREDATA;
        }
        else if (FAILED(hr))
        {
            is = INSTALLSTATE_INVALIDARG;
        }
    }

    return is;
}

static UINT WINAPI MsiProductSearchTest_MsiGetProductInfoW(
    __in LPCWSTR szProductCode,
    __in LPCWSTR szProperty,
    __out_ecount_opt(*pcchValue) LPWSTR szValue,
    __inout_opt LPDWORD pcchValue
    )
{
    if (String::Equals(gcnew String(szProductCode), gcnew String(L"{600D0000-0000-0000-0000-000000000000}")) &&
        String::Equals(gcnew String(szProperty), gcnew String(INSTALLPROPERTY_PRODUCTSTATE)))
    {
        // force call to WiuGetProductInfoEx
        return ERROR_UNKNOWN_PROPERTY;
    }

    UINT er = MsiProductSearchTest_MsiGetProductInfoExW(szProductCode, NULL, MSIINSTALLCONTEXT_MACHINE, szProperty, szValue, pcchValue);
    return er;
}

static UINT WINAPI MsiProductSearchTest_MsiGetProductInfoExW(
    __in LPCWSTR szProductCode,
    __in_opt LPCWSTR /*szUserSid*/,
    __in MSIINSTALLCONTEXT dwContext,
    __in LPCWSTR szProperty,
    __out_ecount_opt(*pcchValue) LPWSTR szValue,
    __inout_opt LPDWORD pcchValue
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_FUNCTION_FAILED;
    LPCWSTR wzValue = NULL;

    String^ productCode = gcnew String(szProductCode);
    String^ _property = gcnew String(szProperty);
    switch (dwContext)
    {
    case MSIINSTALLCONTEXT_USERMANAGED:
        er = ERROR_UNKNOWN_PRODUCT;
        break;
    case MSIINSTALLCONTEXT_USERUNMANAGED:
        if (String::Equals(productCode, gcnew String(L"{600D0000-0000-0000-0000-000000000000}")))
        {
            if (String::Equals(_property, gcnew String(INSTALLPROPERTY_PRODUCTSTATE)))
            {
                wzValue = L"5";
            }
        }
        break;
    case MSIINSTALLCONTEXT_MACHINE:
        if (String::Equals(productCode, gcnew String(L"{BAD00000-0000-0000-0000-000000000000}")))
        {
            er = ERROR_UNKNOWN_PRODUCT;
        }
        else if (String::Equals(productCode, gcnew String(L"{600D0000-0000-0000-0000-000000000000}")))
        {
            if (String::Equals(_property, gcnew String(INSTALLPROPERTY_VERSIONSTRING)))
            {
                wzValue = L"1.0.0.0";
            }
            else if (String::Equals(_property, gcnew String(INSTALLPROPERTY_LANGUAGE)))
            {
                wzValue = L"1033";
            }
            else if (String::Equals(_property, gcnew String(INSTALLPROPERTY_ASSIGNMENTTYPE)))
            {
                wzValue = L"1";
            }
            else if (String::Equals(_property, gcnew String(INSTALLPROPERTY_PRODUCTSTATE)))
            {
                // try again in per-user context
                er = ERROR_UNKNOWN_PRODUCT;
            }
        }
        else if (String::Equals(productCode, gcnew String(L"{600D0000-1000-0000-0000-000000000000}")))
        {
            static BOOL fFlipp = FALSE;
            if (fFlipp)
            {
                if (String::Equals(_property, gcnew String(INSTALLPROPERTY_VERSIONSTRING)))
                {
                    wzValue = L"1.0.0.0";
                }
            }
            else
            {
                *pcchValue = MAX_PATH * 2;
                er = ERROR_MORE_DATA;
            }
            fFlipp = !fFlipp;
        }
        break;
    }

    if (wzValue)
    {
        hr = ::StringCchCopyW(szValue, *pcchValue, wzValue);
        if (STRSAFE_E_INSUFFICIENT_BUFFER == hr)
        {
            *pcchValue = lstrlenW(wzValue);
            er = ERROR_MORE_DATA;
        }
        else if (SUCCEEDED(hr))
        {
            er = ERROR_SUCCESS;
        }
    }

    return er;
}
