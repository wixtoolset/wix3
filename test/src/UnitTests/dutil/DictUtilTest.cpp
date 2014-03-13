//-------------------------------------------------------------------------------------------------
// <copyright file="DictUtilTest.cpp" company="Outercurve Foundation">
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

const DWORD numIterations = 100000;

namespace CfgTests
{
    struct Value
    {
        DWORD dwNum;
        LPWSTR sczKey;
    };

    public ref class DictUtil
    {
    public:
        [Fact]
        void DictUtilTest()
        {
            EmbeddedKeyTestHelper(DICT_FLAG_NONE, numIterations);

            EmbeddedKeyTestHelper(DICT_FLAG_CASEINSENSITIVE, numIterations);

            StringListTestHelper(DICT_FLAG_NONE, numIterations);

            StringListTestHelper(DICT_FLAG_CASEINSENSITIVE, numIterations);
        }

    private:
        void EmbeddedKeyTestHelper(DICT_FLAG dfFlags, DWORD dwNumIterations)
        {
            HRESULT hr = S_OK;
            Value *rgValues = NULL;
            Value *valueFound = NULL;
            DWORD cValues = 0;
            LPWSTR sczExpectedKey = NULL;
            STRINGDICT_HANDLE sdValues = NULL;

            hr = DictCreateWithEmbeddedKey(&sdValues, 0, (void **)&rgValues, offsetof(Value, sczKey), dfFlags);
            ExitOnFailure(hr, "Failed to create dictionary of values");

            for (DWORD i = 0; i < dwNumIterations; ++i)
            {
                cValues++;

                hr = MemEnsureArraySize((void **)&rgValues, cValues, sizeof(Value), 5);
                ExitOnFailure(hr, "Failed to grow value array");

                hr = StrAllocFormatted(&rgValues[i].sczKey, L"%u_a_%u", i, i);
                ExitOnFailure2(hr, "Failed to allocate key for value %u", i, i);

                hr = DictAddValue(sdValues, rgValues + i);
                ExitOnFailure(hr, "Failed to add item to dict");
            }

            for (DWORD i = 0; i < dwNumIterations; ++i)
            {
                hr = StrAllocFormatted(&sczExpectedKey, L"%u_a_%u", i, i);
                ExitOnFailure(hr, "Failed to alloc expected key");

                hr = DictGetValue(sdValues, sczExpectedKey, (void **)&valueFound);
                ExitOnFailure1(hr, "Failed to find value %ls", sczExpectedKey);

                if (0 != wcscmp(sczExpectedKey, valueFound->sczKey))
                {
                    ExitOnFailure(hr, "Item found doesn't match!");
                }

                hr = StrAllocFormatted(&sczExpectedKey, L"%u_A_%u", i, i);
                ExitOnFailure(hr, "Failed to alloc expected key");

                hr = DictGetValue(sdValues, sczExpectedKey, (void **)&valueFound);

                if (dfFlags & DICT_FLAG_CASEINSENSITIVE)
                {
                    ExitOnFailure1(hr, "Failed to find value %ls", sczExpectedKey);

                    if (0 != _wcsicmp(sczExpectedKey, valueFound->sczKey))
                    {
                        hr = E_FAIL;
                        ExitOnFailure(hr, "Item found doesn't match!");
                    }
                }
                else
                {
                    if (E_NOTFOUND != hr)
                    {
                        hr = E_FAIL;
                        ExitOnFailure1(hr, "This embedded key is case sensitive, but it seemed to have found something case using case insensitivity!: %ls", sczExpectedKey);
                    }
                }

                hr = StrAllocFormatted(&sczExpectedKey, L"%u_b_%u", i, i);
                ExitOnFailure(hr, "Failed to alloc expected key");

                hr = DictGetValue(sdValues, sczExpectedKey, (void **)&valueFound);
                if (E_NOTFOUND != hr)
                {
                    hr = E_FAIL;
                    ExitOnFailure1(hr, "Item shouldn't have been found in dictionary: %ls", sczExpectedKey);
                }
            }

        LExit:
            for (DWORD i = 0; i < cValues; ++i)
            {
                ReleaseStr(rgValues[i].sczKey);
            }
            ReleaseMem(rgValues);
            ReleaseStr(sczExpectedKey);
            ReleaseDict(sdValues);
        }

        void StringListTestHelper(DICT_FLAG dfFlags, DWORD dwNumIterations)
        {
            HRESULT hr = S_OK;
            LPWSTR sczKey = NULL;
            LPWSTR sczExpectedKey = NULL;
            STRINGDICT_HANDLE sdValues = NULL;

            hr = DictCreateStringList(&sdValues, 0, dfFlags);
            ExitOnFailure(hr, "Failed to create dictionary of keys");

            for (DWORD i = 0; i < dwNumIterations; ++i)
            {
                hr = StrAllocFormatted(&sczKey, L"%u_a_%u", i, i);
                ExitOnFailure2(hr, "Failed to allocate key for value %u", i, i);

                hr = DictAddKey(sdValues, sczKey);
                ExitOnFailure(hr, "Failed to add key to dict");
            }

            for (DWORD i = 0; i < dwNumIterations; ++i)
            {
                hr = StrAllocFormatted(&sczExpectedKey, L"%u_a_%u", i, i);
                ExitOnFailure(hr, "Failed to alloc expected key");

                hr = DictKeyExists(sdValues, sczExpectedKey);
                ExitOnFailure1(hr, "Failed to find value %ls", sczExpectedKey);

                hr = StrAllocFormatted(&sczExpectedKey, L"%u_A_%u", i, i);
                ExitOnFailure(hr, "Failed to alloc expected key");

                hr = DictKeyExists(sdValues, sczExpectedKey);
                if (dfFlags & DICT_FLAG_CASEINSENSITIVE)
                {
                    ExitOnFailure1(hr, "Failed to find value %ls", sczExpectedKey);
                }
                else
                {
                    if (E_NOTFOUND != hr)
                    {
                        hr = E_FAIL;
                        ExitOnFailure1(hr, "This stringlist dict is case sensitive, but it seemed to have found something case using case insensitivity!: %ls", sczExpectedKey);
                    }
                }

                hr = StrAllocFormatted(&sczExpectedKey, L"%u_b_%u", i, i);
                ExitOnFailure(hr, "Failed to alloc expected key");

                hr = DictKeyExists(sdValues, sczExpectedKey);
                if (E_NOTFOUND != hr)
                {
                    hr = E_FAIL;
                    ExitOnFailure1(hr, "Item shouldn't have been found in dictionary: %ls", sczExpectedKey);
                }
            }

        LExit:
            ReleaseStr(sczKey);
            ReleaseStr(sczExpectedKey);
            ReleaseDict(sdValues);
        }
    };
}
