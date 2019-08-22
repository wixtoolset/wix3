// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#undef GetTempPath
#undef GetEnvironmentVariable

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
    using namespace Xunit;

    public ref class VariableTest : BurnUnitTest
    {
    public:
        [NamedFact]
        void VariablesBasicTest()
        {
            HRESULT hr = S_OK;
            BURN_VARIABLES variables = { };
            try
            {
                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                // set variables
                VariableSetStringHelper(&variables, L"PROP1", L"VAL1");
                VariableSetNumericHelper(&variables, L"PROP2", 2);
                VariableSetStringHelper(&variables, L"PROP5", L"VAL5");
                VariableSetStringHelper(&variables, L"PROP3", L"VAL3");
                VariableSetStringHelper(&variables, L"PROP4", L"VAL4");
                VariableSetStringHelper(&variables, L"PROP6", L"VAL6");
                VariableSetStringHelper(&variables, L"PROP7", L"7");
                VariableSetVersionHelper(&variables, L"PROP8", MAKEQWORDVERSION(1,1,0,0));

                // set overwritten variables
                VariableSetStringHelper(&variables, L"OVERWRITTEN_STRING", L"ORIGINAL");
                VariableSetNumericHelper(&variables, L"OVERWRITTEN_STRING", 42);

                VariableSetNumericHelper(&variables, L"OVERWRITTEN_NUMBER", 5);
                VariableSetStringHelper(&variables, L"OVERWRITTEN_NUMBER", L"NEW");

                // get and verify variable values
                Assert::Equal(gcnew String(L"VAL1"), VariableGetStringHelper(&variables, L"PROP1"));
                Assert::Equal(2ll, VariableGetNumericHelper(&variables, L"PROP2"));
                Assert::Equal(gcnew String(L"2"), VariableGetStringHelper(&variables, L"PROP2"));
                Assert::Equal(gcnew String(L"VAL3"), VariableGetStringHelper(&variables, L"PROP3"));
                Assert::Equal(gcnew String(L"VAL4"), VariableGetStringHelper(&variables, L"PROP4"));
                Assert::Equal(gcnew String(L"VAL5"), VariableGetStringHelper(&variables, L"PROP5"));
                Assert::Equal(gcnew String(L"VAL6"), VariableGetStringHelper(&variables, L"PROP6"));
                Assert::Equal(7ll, VariableGetNumericHelper(&variables, L"PROP7"));
                Assert::Equal(MAKEQWORDVERSION(1,1,0,0), VariableGetVersionHelper(&variables, L"PROP8"));
                Assert::Equal(gcnew String(L"1.1.0.0"), VariableGetStringHelper(&variables, L"PROP8"));

                Assert::Equal(42ll, VariableGetNumericHelper(&variables, L"OVERWRITTEN_STRING"));
                Assert::Equal(gcnew String(L"NEW"), VariableGetStringHelper(&variables, L"OVERWRITTEN_NUMBER"));
            }
            finally
            {
                VariablesUninitialize(&variables);
            }
        }

        [NamedFact]
        void VariablesParseXmlTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            BURN_VARIABLES variables = { };
            try
            {
                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <Variable Id='Var1' Type='numeric' Value='1' Hidden='no' Persisted='no' />"
                    L"    <Variable Id='Var2' Type='string' Value='String value.' Hidden='no' Persisted='no' />"
                    L"    <Variable Id='Var3' Type='version' Value='1.2.3.4' Hidden='no' Persisted='no' />"
                    L"    <Variable Id='Var4' Hidden='no' Persisted='no' />"
                    L"    <Variable Id='Var5' Type='string' Value='' Hidden='no' Persisted='no' />"
                    L"</Bundle>";

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = VariablesParseFromXml(&variables, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse searches from XML.");

                // get and verify variable values
                Assert::Equal((int)BURN_VARIANT_TYPE_NUMERIC, VariableGetTypeHelper(&variables, L"Var1"));
                Assert::Equal((int)BURN_VARIANT_TYPE_STRING, VariableGetTypeHelper(&variables, L"Var2"));
                Assert::Equal((int)BURN_VARIANT_TYPE_VERSION, VariableGetTypeHelper(&variables, L"Var3"));
                Assert::Equal((int)BURN_VARIANT_TYPE_NONE, VariableGetTypeHelper(&variables, L"Var4"));

                Assert::Equal(1ll, VariableGetNumericHelper(&variables, L"Var1"));
                Assert::Equal(gcnew String(L"String value."), VariableGetStringHelper(&variables, L"Var2"));
                Assert::Equal(MAKEQWORDVERSION(1,2,3,4), VariableGetVersionHelper(&variables, L"Var3"));
            }
            finally
            {
                ReleaseObject(pixeBundle);
                VariablesUninitialize(&variables);
            }
        }

        [NamedFact]
        void VariablesFormatTest()
        {
            HRESULT hr = S_OK;
            BURN_VARIABLES variables = { };
            LPWSTR scz = NULL;
            DWORD cch = 0;
            try
            {
                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                // set variables
                VariableSetStringHelper(&variables, L"PROP1", L"VAL1");
                VariableSetStringHelper(&variables, L"PROP2", L"VAL2");
                VariableSetNumericHelper(&variables, L"PROP3", 3);

                // test string formatting
                Assert::Equal(gcnew String(L"NOPROP"), VariableFormatStringHelper(&variables, L"NOPROP"));
                Assert::Equal(gcnew String(L"VAL1"), VariableFormatStringHelper(&variables, L"[PROP1]"));
                Assert::Equal(gcnew String(L" VAL1 "), VariableFormatStringHelper(&variables, L" [PROP1] "));
                Assert::Equal(gcnew String(L"PRE VAL1"), VariableFormatStringHelper(&variables, L"PRE [PROP1]"));
                Assert::Equal(gcnew String(L"VAL1 POST"), VariableFormatStringHelper(&variables, L"[PROP1] POST"));
                Assert::Equal(gcnew String(L"PRE VAL1 POST"), VariableFormatStringHelper(&variables, L"PRE [PROP1] POST"));
                Assert::Equal(gcnew String(L"VAL1 MID VAL2"), VariableFormatStringHelper(&variables, L"[PROP1] MID [PROP2]"));
                Assert::Equal(gcnew String(L""), VariableFormatStringHelper(&variables, L"[NONE]"));
                Assert::Equal(gcnew String(L""), VariableFormatStringHelper(&variables, L"[prop1]"));
                Assert::Equal(gcnew String(L"["), VariableFormatStringHelper(&variables, L"[\\[]"));
                Assert::Equal(gcnew String(L"]"), VariableFormatStringHelper(&variables, L"[\\]]"));
                Assert::Equal(gcnew String(L"[]"), VariableFormatStringHelper(&variables, L"[]"));
                Assert::Equal(gcnew String(L"[NONE"), VariableFormatStringHelper(&variables, L"[NONE"));
                Assert::Equal(gcnew String(L"VAL2"), VariableGetFormattedHelper(&variables, L"PROP2"));
                Assert::Equal(gcnew String(L"3"), VariableGetFormattedHelper(&variables, L"PROP3"));

                hr = VariableFormatString(&variables, L"PRE [PROP1] POST", &scz, &cch);
                TestThrowOnFailure(hr, L"Failed to format string");

                Assert::Equal((DWORD)lstrlenW(scz), cch);

                hr = VariableFormatString(&variables, L"PRE [PROP1] POST", NULL, &cch);
                TestThrowOnFailure(hr, L"Failed to format string");

                Assert::Equal((DWORD)lstrlenW(scz), cch);
            }
            finally
            {
                VariablesUninitialize(&variables);
                ReleaseStr(scz);
            }
        }

        [NamedFact]
        void VariablesEscapeTest()
        {
            // test string escaping
            Assert::Equal(gcnew String(L"[\\[]"), VariableEscapeStringHelper(L"["));
            Assert::Equal(gcnew String(L"[\\]]"), VariableEscapeStringHelper(L"]"));
            Assert::Equal(gcnew String(L" [\\[]TEXT[\\]] "), VariableEscapeStringHelper(L" [TEXT] "));
        }

        [NamedFact]
        void VariablesConditionTest()
        {
            HRESULT hr = S_OK;
            BURN_VARIABLES variables = { };
            try
            {
                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                // set variables
                VariableSetStringHelper(&variables, L"PROP1", L"VAL1");
                VariableSetStringHelper(&variables, L"PROP2", L"VAL2");
                VariableSetStringHelper(&variables, L"PROP3", L"VAL3");
                VariableSetStringHelper(&variables, L"PROP4", L"BEGIN MID END");
                VariableSetNumericHelper(&variables, L"PROP5", 5);
                VariableSetNumericHelper(&variables, L"PROP6", 6);
                VariableSetStringHelper(&variables, L"PROP7", L"");
                VariableSetNumericHelper(&variables, L"PROP8", 0);
                VariableSetStringHelper(&variables, L"_PROP9", L"VAL9");
                VariableSetNumericHelper(&variables, L"PROP10", -10);
                VariableSetNumericHelper(&variables, L"PROP11", 9223372036854775807ll);
                VariableSetNumericHelper(&variables, L"PROP12", -9223372036854775808ll);
                VariableSetNumericHelper(&variables, L"PROP13", 0x00010000);
                VariableSetNumericHelper(&variables, L"PROP14", 0x00000001);
                VariableSetNumericHelper(&variables, L"PROP15", 0x00010001);
                VariableSetVersionHelper(&variables, L"PROP16", MAKEQWORDVERSION(0,0,0,0));
                VariableSetVersionHelper(&variables, L"PROP17", MAKEQWORDVERSION(1,0,0,0));
                VariableSetVersionHelper(&variables, L"PROP18", MAKEQWORDVERSION(1,1,0,0));
                VariableSetVersionHelper(&variables, L"PROP19", MAKEQWORDVERSION(1,1,1,0));
                VariableSetVersionHelper(&variables, L"PROP20", MAKEQWORDVERSION(1,1,1,1));
                VariableSetNumericHelper(&variables, L"vPROP21", 1);
                VariableSetVersionHelper(&variables, L"PROP22", MAKEQWORDVERSION(65535,65535,65535,65535));
                VariableSetStringHelper(&variables, L"PROP23", L"1.1.1");

                // test conditions
                Assert::True(EvaluateConditionHelper(&variables, L"PROP1"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP5"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP7"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP8"));
                Assert::True(EvaluateConditionHelper(&variables, L"_PROP9"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP16"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP17"));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP1 = \"VAL1\""));
                Assert::False(EvaluateConditionHelper(&variables, L"NONE = \"NOT\""));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP1 <> \"VAL1\""));
                Assert::True(EvaluateConditionHelper(&variables, L"NONE <> \"NOT\""));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP1 ~= \"val1\""));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP1 = \"val1\""));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP1 ~<> \"val1\""));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP1 <> \"val1\""));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP5 = 5"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP5 = 0"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP5 <> 5"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP5 <> 0"));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP10 = -10"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP10 <> -10"));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP17 = v1"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP17 = v0"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP17 <> v1"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP17 <> v0"));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP16 = v0"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP17 = v1"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP18 = v1.1"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP19 = v1.1.1"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP20 = v1.1.1.1"));
                Assert::True(EvaluateFailureConditionHelper(&variables, L"PROP20 = v1.1.1.1.0"));
                Assert::True(EvaluateFailureConditionHelper(&variables, L"PROP20 = v1.1.1.1.1"));
                Assert::True(EvaluateConditionHelper(&variables, L"vPROP21 = 1"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP23 = v1.1.1"));
                Assert::True(EvaluateConditionHelper(&variables, L"v1.1.1 = PROP23"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP1 <> v1.1.1"));
                Assert::True(EvaluateConditionHelper(&variables, L"v1.1.1 <> PROP1"));

                Assert::False(EvaluateConditionHelper(&variables, L"PROP11 = 9223372036854775806"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP11 = 9223372036854775807"));
                Assert::True(EvaluateFailureConditionHelper(&variables, L"PROP11 = 9223372036854775808"));
                Assert::True(EvaluateFailureConditionHelper(&variables, L"PROP11 = 92233720368547758070000"));

                Assert::False(EvaluateConditionHelper(&variables, L"PROP12 = -9223372036854775807"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP12 = -9223372036854775808"));
                Assert::True(EvaluateFailureConditionHelper(&variables, L"PROP12 = -9223372036854775809"));
                Assert::True(EvaluateFailureConditionHelper(&variables, L"PROP12 = -92233720368547758080000"));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP22 = v65535.65535.65535.65535"));
                Assert::True(EvaluateFailureConditionHelper(&variables, L"PROP22 = v65536.65535.65535.65535"));
                Assert::True(EvaluateFailureConditionHelper(&variables, L"PROP22 = v65535.655350000.65535.65535"));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP5 < 6"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP5 < 5"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP5 > 4"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP5 > 5"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP5 <= 6"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP5 <= 5"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP5 <= 4"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP5 >= 4"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP5 >= 5"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP5 >= 6"));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP4 << \"BEGIN\""));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP4 << \"END\""));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP4 >> \"END\""));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP4 >> \"BEGIN\""));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP4 >< \"MID\""));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP4 >< \"NONE\""));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP16 < v1.1"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP16 < v0"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP17 > v0.12"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP17 > v1"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP18 >= v1.0"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP18 >= v1.1"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP18 >= v2.1"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP19 <= v1.1234.1"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP19 <= v1.1.1"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP19 <= v1.0.123"));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP6 = \"6\""));
                Assert::True(EvaluateConditionHelper(&variables, L"\"6\" = PROP6"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP6 = \"ABC\""));
                Assert::False(EvaluateConditionHelper(&variables, L"\"ABC\" = PROP6"));
                Assert::False(EvaluateConditionHelper(&variables, L"\"ABC\" = PROP6"));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP13 << 1"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP13 << 0"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP14 >> 1"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP14 >> 0"));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP15 >< 65537"));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP15 >< 0"));

                Assert::False(EvaluateConditionHelper(&variables, L"NOT PROP1"));
                Assert::True(EvaluateConditionHelper(&variables, L"NOT (PROP1 <> \"VAL1\")"));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP1 = \"VAL1\" AND PROP2 = \"VAL2\""));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP1 = \"VAL1\" AND PROP2 = \"NOT\""));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP1 = \"NOT\" AND PROP2 = \"VAL2\""));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP1 = \"NOT\" AND PROP2 = \"NOT\""));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP1 = \"VAL1\" OR PROP2 = \"VAL2\""));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP1 = \"VAL1\" OR PROP2 = \"NOT\""));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP1 = \"NOT\" OR PROP2 = \"VAL2\""));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP1 = \"NOT\" OR PROP2 = \"NOT\""));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP1 = \"VAL1\" AND PROP2 = \"VAL2\" OR PROP3 = \"NOT\""));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP1 = \"VAL1\" AND PROP2 = \"NOT\" OR PROP3 = \"VAL3\""));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP1 = \"VAL1\" AND PROP2 = \"NOT\" OR PROP3 = \"NOT\""));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP1 = \"VAL1\" AND (PROP2 = \"NOT\" OR PROP3 = \"VAL3\")"));
                Assert::True(EvaluateConditionHelper(&variables, L"(PROP1 = \"VAL1\" AND PROP2 = \"VAL2\") OR PROP3 = \"NOT\""));

                Assert::True(EvaluateConditionHelper(&variables, L"PROP3 = \"NOT\" OR PROP1 = \"VAL1\" AND PROP2 = \"VAL2\""));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP3 = \"VAL3\" OR PROP1 = \"VAL1\" AND PROP2 = \"NOT\""));
                Assert::False(EvaluateConditionHelper(&variables, L"PROP3 = \"NOT\" OR PROP1 = \"VAL1\" AND PROP2 = \"NOT\""));
                Assert::True(EvaluateConditionHelper(&variables, L"(PROP3 = \"NOT\" OR PROP1 = \"VAL1\") AND PROP2 = \"VAL2\""));
                Assert::True(EvaluateConditionHelper(&variables, L"PROP3 = \"NOT\" OR (PROP1 = \"VAL1\" AND PROP2 = \"VAL2\")"));

                Assert::True(EvaluateFailureConditionHelper(&variables, L"="));
                Assert::True(EvaluateFailureConditionHelper(&variables, L"(PROP1"));
                Assert::True(EvaluateFailureConditionHelper(&variables, L"(PROP1 = \""));
                Assert::True(EvaluateFailureConditionHelper(&variables, L"1A"));
                Assert::True(EvaluateFailureConditionHelper(&variables, L"*"));

                Assert::True(EvaluateFailureConditionHelper(&variables, L"1 == 1"));
            }
            finally
            {
                VariablesUninitialize(&variables);
            }
        }

        [NamedFact]
        void VariablesSerializationTest()
        {
            HRESULT hr = S_OK;
            BYTE* pbBuffer = NULL;
            SIZE_T cbBuffer = 0;
            SIZE_T iBuffer = 0;
            BURN_VARIABLES variables1 = { };
            BURN_VARIABLES variables2 = { };
            try
            {
                // serialize
                hr = VariableInitialize(&variables1);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                VariableSetStringHelper(&variables1, L"PROP1", L"VAL1");
                VariableSetNumericHelper(&variables1, L"PROP2", 2);
                VariableSetVersionHelper(&variables1, L"PROP3", MAKEQWORDVERSION(1,1,1,1));
                VariableSetStringHelper(&variables1, L"PROP4", L"VAL4");

                hr = VariableSerialize(&variables1, FALSE, &pbBuffer, &cbBuffer);
                TestThrowOnFailure(hr, L"Failed to serialize variables.");

                // deserialize
                hr = VariableInitialize(&variables2);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = VariableDeserialize(&variables2, FALSE, pbBuffer, cbBuffer, &iBuffer);
                TestThrowOnFailure(hr, L"Failed to deserialize variables.");

                Assert::Equal(gcnew String(L"VAL1"), VariableGetStringHelper(&variables2, L"PROP1"));
                Assert::Equal(2ll, VariableGetNumericHelper(&variables2, L"PROP2"));
                Assert::Equal(MAKEQWORDVERSION(1,1,1,1), VariableGetVersionHelper(&variables2, L"PROP3"));
                Assert::Equal(gcnew String(L"VAL4"), VariableGetStringHelper(&variables2, L"PROP4"));
            }
            finally
            {
                ReleaseBuffer(pbBuffer);
                VariablesUninitialize(&variables1);
                VariablesUninitialize(&variables2);
            }
        }

        [NamedFact]
        void VariablesBuiltInTest()
        {
            HRESULT hr = S_OK;
            BURN_VARIABLES variables = { };
            try
            {
                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                // VersionMsi
                Assert::True(EvaluateConditionHelper(&variables, L"VersionMsi >= v1.1"));

                // VersionNT
                Assert::True(EvaluateConditionHelper(&variables, L"VersionNT <> v0.0.0.0"));

                // VersionNT64
                if (nullptr == Environment::GetEnvironmentVariable("ProgramFiles(x86)"))
                {
                    Assert::False(EvaluateConditionHelper(&variables, L"VersionNT64"));
                }
                else
                {
                    Assert::True(EvaluateConditionHelper(&variables, L"VersionNT64"));
                }

                // attempt to set a built-in property
                hr = VariableSetString(&variables, L"VersionNT", L"VAL", FALSE);
                Assert::Equal(E_INVALIDARG, hr);
                Assert::False(EvaluateConditionHelper(&variables, L"VersionNT = \"VAL\""));

                VariableGetNumericHelper(&variables, L"NTProductType");
                VariableGetNumericHelper(&variables, L"NTSuiteBackOffice");
                VariableGetNumericHelper(&variables, L"NTSuiteDataCenter");
                VariableGetNumericHelper(&variables, L"NTSuiteEnterprise");
                VariableGetNumericHelper(&variables, L"NTSuitePersonal");
                VariableGetNumericHelper(&variables, L"NTSuiteSmallBusiness");
                VariableGetNumericHelper(&variables, L"NTSuiteSmallBusinessRestricted");
                VariableGetNumericHelper(&variables, L"NTSuiteWebServer");
                VariableGetNumericHelper(&variables, L"CompatibilityMode");
                VariableGetNumericHelper(&variables, L"Privileged");
                VariableGetNumericHelper(&variables, L"SystemLanguageID");
                VariableGetNumericHelper(&variables, L"TerminalServer");
                VariableGetNumericHelper(&variables, L"UserUILanguageID");
                VariableGetNumericHelper(&variables, L"UserLanguageID");

                // known folders
                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::ApplicationData) + "\\", VariableGetStringHelper(&variables, L"AppDataFolder"));
                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::CommonApplicationData) + "\\", VariableGetStringHelper(&variables, L"CommonAppDataFolder"));

                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::ProgramFiles) + "\\", VariableGetStringHelper(&variables, L"ProgramFilesFolder"));
                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::DesktopDirectory) + "\\", VariableGetStringHelper(&variables, L"DesktopFolder"));
                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::Favorites) + "\\", VariableGetStringHelper(&variables, L"FavoritesFolder"));
                VariableGetStringHelper(&variables, L"FontsFolder");
                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData) + "\\", VariableGetStringHelper(&variables, L"LocalAppDataFolder"));
                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::Personal) + "\\", VariableGetStringHelper(&variables, L"PersonalFolder"));
                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::Programs) + "\\", VariableGetStringHelper(&variables, L"ProgramMenuFolder"));
                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::SendTo) + "\\", VariableGetStringHelper(&variables, L"SendToFolder"));
                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::StartMenu) + "\\", VariableGetStringHelper(&variables, L"StartMenuFolder"));
                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::Startup) + "\\", VariableGetStringHelper(&variables, L"StartupFolder"));
                VariableGetStringHelper(&variables, L"SystemFolder");
                VariableGetStringHelper(&variables, L"WindowsFolder");
                VariableGetStringHelper(&variables, L"WindowsVolume");

                Assert::Equal(System::IO::Path::GetTempPath(), System::IO::Path::GetFullPath(VariableGetStringHelper(&variables, L"TempFolder")));

                VariableGetStringHelper(&variables, L"AdminToolsFolder");
                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::CommonProgramFiles) + "\\", VariableGetStringHelper(&variables, L"CommonFilesFolder"));
                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::MyPictures) + "\\", VariableGetStringHelper(&variables, L"MyPicturesFolder"));
                Assert::Equal(Environment::GetFolderPath(Environment::SpecialFolder::Templates) + "\\", VariableGetStringHelper(&variables, L"TemplateFolder"));

                if (Environment::Is64BitOperatingSystem)
                {
                    VariableGetStringHelper(&variables, L"ProgramFiles64Folder");
                    VariableGetStringHelper(&variables, L"CommonFiles64Folder");
                    VariableGetStringHelper(&variables, L"System64Folder");
                }
            }
            finally
            {
                VariablesUninitialize(&variables);
            }
        }
    };
}
}
}
}
}
