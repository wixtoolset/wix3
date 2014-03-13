//-------------------------------------------------------------------------------------------------
// <copyright file="IniUtilTest.cpp" company="Outercurve Foundation">
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

typedef HRESULT (__clrcall *IniFormatParameters)(
    INI_HANDLE
    );

namespace CfgTests
{
    public ref class IniUtil
    {
    public:
        [Fact]
        void IniUtilTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczTempIniFilePath = NULL;
            LPWSTR sczTempIniFileDir = NULL;
            LPWSTR wzIniContents = L"           PlainValue             =       \t      Blah               \r\n;CommentHere\r\n[Section1]\r\n     ;Another Comment With = Equal Sign\r\nSection1ValueA=Foo\r\n\r\nSection1ValueB=Bar\r\n[Section2]\r\nSection2ValueA=Cha\r\n\r\n";
            LPWSTR wzScriptContents = L"setf ~PlainValue Blah\r\n;CommentHere\r\n\r\nsetf ~Section1\\Section1ValueA Foo\r\n\r\nsetf ~Section1\\Section1ValueB Bar\r\nsetf ~Section2\\Section2ValueA Cha\r\n\r\n";

            hr = PathExpand(&sczTempIniFilePath, L"%TEMP%\\IniUtilTest\\Test.ini", PATH_EXPAND_ENVIRONMENT);
            ExitOnFailure(hr, "Failed to get path to temp INI file");

            hr = PathGetDirectory(sczTempIniFilePath, &sczTempIniFileDir);
            ExitOnFailure(hr, "Failed to get directory to temp INI file");

            hr = DirEnsureDelete(sczTempIniFileDir, TRUE, TRUE);
            if (E_PATHNOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to delete IniUtilTest directory");

            hr = DirEnsureExists(sczTempIniFileDir, NULL);
            ExitOnFailure(hr, "Failed to ensure temp directory exists");

            // Tests parsing, then modifying a regular INI file
            TestReadThenWrite(sczTempIniFilePath, StandardIniFormat, wzIniContents);

            // Tests programmatically creating from scratch, then parsing an INI file
            TestWriteThenRead(sczTempIniFilePath, StandardIniFormat);
            
            // Tests parsing, then modifying a regular INI file
            TestReadThenWrite(sczTempIniFilePath, ScriptFormat, wzScriptContents);

            // Tests programmatically creating from scratch, then parsing an INI file
            TestWriteThenRead(sczTempIniFilePath, ScriptFormat);
            
        LExit:
            ReleaseStr(sczTempIniFilePath);
            ReleaseStr(sczTempIniFileDir);

            return;
        }

    private:
        void AssertValue(INI_HANDLE iniHandle, LPCWSTR wzValueName, LPCWSTR wzValue)
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;

            hr = IniGetValue(iniHandle, wzValueName, &sczValue);
            ExitOnFailure1(hr, "Failed to get ini value: %ls", wzValueName);

            if (0 != wcscmp(sczValue, wzValue))
            {
                hr = E_FAIL;
                ExitOnFailure3(hr, "Expected to find value in INI: '%ls'='%ls' - but found value '%ls' instead", wzValueName, wzValue, sczValue);
            }

        LExit:
            ReleaseStr(sczValue);
        }

        void AssertNoValue(INI_HANDLE iniHandle, LPCWSTR wzValueName)
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;

            hr = IniGetValue(iniHandle, wzValueName, &sczValue);
            if (E_NOTFOUND != hr)
            {
                if (SUCCEEDED(hr))
                {
                    hr = E_FAIL;
                }
                ExitOnFailure1(hr, "INI value shouldn't have been found: %ls", wzValueName);
            }
        LExit:
            ReleaseStr(sczValue);
        }

        static HRESULT StandardIniFormat(__inout INI_HANDLE iniHandle)
        {
            HRESULT hr = S_OK;

            hr = IniSetOpenTag(iniHandle, L"[", L"]");
            ExitOnFailure(hr, "Failed to set open tag settings on ini handle");

            hr = IniSetValueStyle(iniHandle, NULL, L"=");
            ExitOnFailure(hr, "Failed to set value separator setting on ini handle");

            hr = IniSetCommentStyle(iniHandle, L";");
            ExitOnFailure(hr, "Failed to set comment style setting on ini handle");

        LExit:
            return hr;
        }

        static HRESULT ScriptFormat(__inout INI_HANDLE iniHandle)
        {
            HRESULT hr = S_OK;

            hr = IniSetValueStyle(iniHandle, L"setf ~", L" ");
            ExitOnFailure(hr, "Failed to set value separator setting on ini handle");

        LExit:
            return hr;
        }

        void TestReadThenWrite(LPWSTR wzIniFilePath, IniFormatParameters SetFormat, LPCWSTR wzContents)
        {
            HRESULT hr = S_OK;
            INI_HANDLE iniHandle = NULL;
            INI_HANDLE iniHandle2 = NULL;
            INI_VALUE *rgValues = NULL;
            DWORD cValues = 0;

            hr = FileWrite(wzIniFilePath, 0, reinterpret_cast<LPCBYTE>(wzContents), lstrlenW(wzContents) * sizeof(WCHAR), NULL);
            ExitOnFailure(hr, "Failed to write out INI file");

            hr = IniInitialize(&iniHandle);
            ExitOnFailure(hr, "Failed to initialize INI object");

            hr = SetFormat(iniHandle);
            ExitOnFailure(hr, "Failed to set parameters for INI file");

            hr = IniParse(iniHandle, wzIniFilePath, NULL);
            ExitOnFailure(hr, "Failed to parse INI file");

            hr = IniGetValueList(iniHandle, &rgValues, &cValues);
            ExitOnFailure(hr, "Failed to get list of values in INI");

            if (cValues != 4)
            {
                hr = E_FAIL;
                ExitOnFailure1(hr, "Expected to find 4 values in INI file, but found %u instead!", cValues);
            }

            AssertValue(iniHandle, L"PlainValue", L"Blah");
            AssertNoValue(iniHandle, L"PlainValue2");
            AssertValue(iniHandle, L"Section1\\Section1ValueA", L"Foo");
            AssertValue(iniHandle, L"Section1\\Section1ValueB", L"Bar");
            AssertValue(iniHandle, L"Section2\\Section2ValueA", L"Cha");
            AssertNoValue(iniHandle, L"Section1\\ValueDoesntExist");

            hr = IniSetValue(iniHandle, L"PlainValue2", L"Blah2");
            ExitOnFailure(hr, "Failed to set value in INI");

            hr = IniSetValue(iniHandle, L"Section1\\CreatedValue", L"Woo");
            ExitOnFailure(hr, "Failed to set value in INI");

            hr = IniGetValueList(iniHandle, &rgValues, &cValues);
            ExitOnFailure(hr, "Failed to get list of values in INI");

            if (cValues != 6)
            {
                hr = E_FAIL;
                ExitOnFailure1(hr, "Expected to find 4 values in INI file, but found %u instead!", cValues);
            }

            AssertValue(iniHandle, L"PlainValue", L"Blah");
            AssertValue(iniHandle, L"PlainValue2", L"Blah2");
            AssertValue(iniHandle, L"Section1\\Section1ValueA", L"Foo");
            AssertValue(iniHandle, L"Section1\\Section1ValueB", L"Bar");
            AssertValue(iniHandle, L"Section2\\Section2ValueA", L"Cha");
            AssertNoValue(iniHandle, L"Section1\\ValueDoesntExist");
            AssertValue(iniHandle, L"Section1\\CreatedValue", L"Woo");

            // Try deleting a value as well
            hr = IniSetValue(iniHandle, L"Section1\\Section1ValueB", NULL);
            ExitOnFailure(hr, "Failed to kill value in INI");

            hr = IniWriteFile(iniHandle, NULL, FILE_ENCODING_UNSPECIFIED);
            ExitOnFailure(hr, "Failed to write ini file back out to disk");

            ReleaseNullIni(iniHandle);
            // Now re-parse the INI we just wrote and make sure it matches the values we expect
            hr = IniInitialize(&iniHandle2);
            ExitOnFailure(hr, "Failed to initialize INI object");

            hr = SetFormat(iniHandle2);
            ExitOnFailure(hr, "Failed to set parameters for INI file");

            hr = IniParse(iniHandle2, wzIniFilePath, NULL);
            ExitOnFailure(hr, "Failed to parse INI file");

            hr = IniGetValueList(iniHandle2, &rgValues, &cValues);
            ExitOnFailure(hr, "Failed to get list of values in INI");

            if (cValues != 5)
            {
                hr = E_FAIL;
                ExitOnFailure1(hr, "Expected to find 5 values in INI file, but found %u instead!", cValues);
            }

            AssertValue(iniHandle2, L"PlainValue", L"Blah");
            AssertValue(iniHandle2, L"PlainValue2", L"Blah2");
            AssertValue(iniHandle2, L"Section1\\Section1ValueA", L"Foo");
            AssertNoValue(iniHandle2, L"Section1\\Section1ValueB");
            AssertValue(iniHandle2, L"Section2\\Section2ValueA", L"Cha");
            AssertNoValue(iniHandle2, L"Section1\\ValueDoesntExist");
            AssertValue(iniHandle2, L"Section1\\CreatedValue", L"Woo");

        LExit:
            ReleaseIni(iniHandle);
            ReleaseIni(iniHandle2);
        }

        void TestWriteThenRead(LPWSTR wzIniFilePath, IniFormatParameters SetFormat)
        {
            HRESULT hr = S_OK;
            INI_HANDLE iniHandle = NULL;
            INI_HANDLE iniHandle2 = NULL;
            INI_VALUE *rgValues = NULL;
            DWORD cValues = 0;

            hr = FileEnsureDelete(wzIniFilePath);
            ExitOnFailure(hr, "Failed to ensure file is deleted");

            hr = IniInitialize(&iniHandle);
            ExitOnFailure(hr, "Failed to initialize INI object");

            hr = SetFormat(iniHandle);
            ExitOnFailure(hr, "Failed to set parameters for INI file");

            hr = IniGetValueList(iniHandle, &rgValues, &cValues);
            ExitOnFailure(hr, "Failed to get list of values in INI");

            if (cValues != 0)
            {
                hr = E_FAIL;
                ExitOnFailure1(hr, "Expected to find 0 values in INI file, but found %u instead!", cValues);
            }

            hr = IniSetValue(iniHandle, L"Value1", L"BlahTypo");
            ExitOnFailure(hr, "Failed to set value in INI");

            hr = IniSetValue(iniHandle, L"Value2", L"Blah2");
            ExitOnFailure(hr, "Failed to set value in INI");

            hr = IniSetValue(iniHandle, L"Section1\\Value1", L"Section1Value1");
            ExitOnFailure(hr, "Failed to set value in INI");

            hr = IniSetValue(iniHandle, L"Section1\\Value2", L"Section1Value2");
            ExitOnFailure(hr, "Failed to set value in INI");

            hr = IniSetValue(iniHandle, L"Section2\\Value1", L"Section2Value1");
            ExitOnFailure(hr, "Failed to set value in INI");

            hr = IniSetValue(iniHandle, L"Value3", L"Blah3");
            ExitOnFailure(hr, "Failed to set value in INI");

            hr = IniSetValue(iniHandle, L"Value4", L"Blah4");
            ExitOnFailure(hr, "Failed to set value in INI");

            hr = IniSetValue(iniHandle, L"Value4", NULL);
            ExitOnFailure(hr, "Failed to set value in INI");

            hr = IniSetValue(iniHandle, L"Value1", L"Blah1");
            ExitOnFailure(hr, "Failed to set value in INI");

            hr = IniGetValueList(iniHandle, &rgValues, &cValues);
            ExitOnFailure(hr, "Failed to get list of values in INI");

            if (cValues != 7)
            {
                hr = E_FAIL;
                ExitOnFailure1(hr, "Expected to find 6 values in INI file, but found %u instead!", cValues);
            }

            AssertValue(iniHandle, L"Value1", L"Blah1");
            AssertValue(iniHandle, L"Value2", L"Blah2");
            AssertValue(iniHandle, L"Value3", L"Blah3");
            AssertNoValue(iniHandle, L"Value4");
            AssertValue(iniHandle, L"Section1\\Value1", L"Section1Value1");
            AssertValue(iniHandle, L"Section1\\Value2", L"Section1Value2");
            AssertValue(iniHandle, L"Section2\\Value1", L"Section2Value1");

            hr = IniWriteFile(iniHandle, wzIniFilePath, FILE_ENCODING_UNSPECIFIED);
            ExitOnFailure(hr, "Failed to write ini file back out to disk");

            ReleaseNullIni(iniHandle);
            // Now re-parse the INI we just wrote and make sure it matches the values we expect
            hr = IniInitialize(&iniHandle2);
            ExitOnFailure(hr, "Failed to initialize INI object");

            hr = SetFormat(iniHandle2);
            ExitOnFailure(hr, "Failed to set parameters for INI file");

            hr = IniParse(iniHandle2, wzIniFilePath, NULL);
            ExitOnFailure(hr, "Failed to parse INI file");

            hr = IniGetValueList(iniHandle2, &rgValues, &cValues);
            ExitOnFailure(hr, "Failed to get list of values in INI");

            if (cValues != 6)
            {
                hr = E_FAIL;
                ExitOnFailure1(hr, "Expected to find 4 values in INI file, but found %u instead!", cValues);
            }

            AssertValue(iniHandle2, L"Value1", L"Blah1");
            AssertValue(iniHandle2, L"Value2", L"Blah2");
            AssertValue(iniHandle2, L"Value3", L"Blah3");
            AssertNoValue(iniHandle2, L"Value4");
            AssertValue(iniHandle2, L"Section1\\Value1", L"Section1Value1");
            AssertValue(iniHandle2, L"Section1\\Value2", L"Section1Value2");
            AssertValue(iniHandle2, L"Section2\\Value1", L"Section2Value1");

        LExit:
            ReleaseIni(iniHandle);
            ReleaseIni(iniHandle2);
        }
    };
}
