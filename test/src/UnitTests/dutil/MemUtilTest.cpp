//-------------------------------------------------------------------------------------------------
// <copyright file="MemUtilTest.cpp" company="Outercurve Foundation">
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

namespace CfgTests
{
    struct ArrayValue
    {
        DWORD dwNum;
        void *pvNull1;
        LPWSTR sczString;
        void *pvNull2;
    };

    public ref class MemUtil
    {
    public:
        [Fact]
        void MemUtilTest()
        {
            TestAppend();

            TestInsert();
        }

    private:
        void TestAppend()
        {
            HRESULT hr = S_OK;
            DWORD dwSize;
            ArrayValue *rgValues = NULL;
            DWORD cValues = 0;

            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to grow array size to 1");
            ++cValues;
            SetItem(rgValues + 0, 0);

            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to grow array size to 2");
            ++cValues;
            SetItem(rgValues + 1, 1);

            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to grow array size to 3");
            ++cValues;
            SetItem(rgValues + 2, 2);

            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to grow array size to 4");
            ++cValues;
            SetItem(rgValues + 3, 3);

            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to grow array size to 5");
            ++cValues;
            SetItem(rgValues + 4, 4);

            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to grow array size to 6");
            ++cValues;
            SetItem(rgValues + 5, 5);

            // OK, we used growth size 5, so let's try ensuring we have space for 6 (5 + first item) items
            // and make sure it doesn't grow since we already have enough space
            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to ensure array size matches what it should already be");
            dwSize = MemSize(rgValues);
            if (dwSize != 6 * sizeof(ArrayValue))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "MemEnsureArraySize is growing an array that is already big enough!");
            }

            for (DWORD i = 0; i < cValues; ++i)
            {
                CheckItem(rgValues + i, i);
            }

            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to grow array size to 7");
            ++cValues;
            SetItem(rgValues + 6, 6);

            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to grow array size to 7");
            ++cValues;
            SetItem(rgValues + 7, 7);

            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to grow array size to 7");
            ++cValues;
            SetItem(rgValues + 8, 8);

            for (DWORD i = 0; i < cValues; ++i)
            {
                CheckItem(rgValues + i, i);
            }

        LExit:
            return;
        }

        void TestInsert()
        {
            HRESULT hr = S_OK;
            ArrayValue *rgValues = NULL;
            DWORD cValues = 0;

            hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 0, 1, cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to insert into beginning of empty array");
            ++cValues;
            CheckNullItem(rgValues + 0);
            SetItem(rgValues + 0, 5);

            hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 1, 1, cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to insert at end of array");
            ++cValues;
            CheckNullItem(rgValues + 1);
            SetItem(rgValues + 1, 6);

            hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 0, 1, cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to insert into beginning of array");
            ++cValues;
            CheckNullItem(rgValues + 0);
            SetItem(rgValues + 0, 4);

            hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 0, 1, cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to insert into beginning of array");
            ++cValues;
            CheckNullItem(rgValues + 0);
            SetItem(rgValues + 0, 3);

            hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 0, 1, cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to insert into beginning of array");
            ++cValues;
            CheckNullItem(rgValues + 0);
            SetItem(rgValues + 0, 1);

            hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 1, 1, cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to insert into beginning of array");
            ++cValues;
            CheckNullItem(rgValues + 1);
            SetItem(rgValues + 1, 2);

            hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 0, 1, cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to insert into beginning of array");
            ++cValues;
            CheckNullItem(rgValues + 0);
            SetItem(rgValues + 0, 0);

            for (DWORD i = 0; i < cValues; ++i)
            {
                CheckItem(rgValues + i, i);
            }

            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to grow array size to 7");
            ++cValues;
            CheckNullItem(rgValues + 7);
            SetItem(rgValues + 7, 7);

            hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 8, 1, cValues + 1, sizeof(ArrayValue), 5);
            ExitOnFailure(hr, "Failed to insert into beginning of array");
            ++cValues;
            CheckNullItem(rgValues + 8);
            SetItem(rgValues + 8, 8);

            for (DWORD i = 0; i < cValues; ++i)
            {
                CheckItem(rgValues + i, i);
            }

        LExit:
            return;
        }

        void SetItem(ArrayValue *pValue, DWORD dwValue)
        {
            HRESULT hr = S_OK;
            pValue->dwNum = dwValue;

            hr = StrAllocFormatted(&pValue->sczString, L"%u", dwValue);
            ExitOnFailure(hr, "Failed to allocate string");

        LExit:
            return;
        }

        void CheckItem(ArrayValue *pValue, DWORD dwValue)
        {
            HRESULT hr = S_OK;
            LPWSTR sczTemp = NULL;

            if (pValue->dwNum != dwValue)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Arrayvalue doesn't match expected DWORD value!");
            }

            hr = StrAllocFormatted(&sczTemp, L"%u", dwValue);
            ExitOnFailure(hr, "Failed to allocate temp string");

            if (0 != _wcsicmp(sczTemp, pValue->sczString))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Item found doesn't match!");
            }

            if (NULL != pValue->pvNull1 || NULL != pValue->pvNull2)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "One of the expected NULL values wasn't NULL!");
            }

        LExit:
            ReleaseStr(sczTemp);

            return;
        }

        void CheckNullItem(ArrayValue *pValue)
        {
            HRESULT hr = S_OK;

            if (pValue->dwNum != 0)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Arrayvalue doesn't match expected 0 value!");
            }

            if (NULL != pValue->sczString)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Item found isn't NULL!");
            }

            if (NULL != pValue->pvNull1 || NULL != pValue->pvNull2)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "One of the expected NULL values wasn't NULL!");
            }

        LExit:
            return;
        }
    };
}
