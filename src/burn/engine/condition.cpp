// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


//
// parse rules
//
// value                variable | literal | integer | version
// comparison-operator  < | > | <= | >= | = | <> | >< | << | >>
// term                 value | value comparison-operator value | ( expression )
// boolean-factor       term | NOT term
// boolean-term         boolean-factor | boolean-factor AND boolean-term
// expression           boolean-term | boolean-term OR expression
//


// constants

#define COMPARISON  0x00010000
#define INSENSITIVE 0x00020000

enum BURN_SYMBOL_TYPE
{
    // terminals
    BURN_SYMBOL_TYPE_NONE       =  0,
    BURN_SYMBOL_TYPE_END        =  1,
    BURN_SYMBOL_TYPE_OR         =  2,                               // OR
    BURN_SYMBOL_TYPE_AND        =  3,                               // AND
    BURN_SYMBOL_TYPE_NOT        =  4,                               // NOT
    BURN_SYMBOL_TYPE_LT         =  5 | COMPARISON,                  // <
    BURN_SYMBOL_TYPE_GT         =  6 | COMPARISON,                  // >
    BURN_SYMBOL_TYPE_LE         =  7 | COMPARISON,                  // <=
    BURN_SYMBOL_TYPE_GE         =  8 | COMPARISON,                  // >=
    BURN_SYMBOL_TYPE_EQ         =  9 | COMPARISON,                  // =
    BURN_SYMBOL_TYPE_NE         = 10 | COMPARISON,                  // <>
    BURN_SYMBOL_TYPE_BAND       = 11 | COMPARISON,                  // ><
    BURN_SYMBOL_TYPE_HIEQ       = 12 | COMPARISON,                  // <<
    BURN_SYMBOL_TYPE_LOEQ       = 13 | COMPARISON,                  // >>
    BURN_SYMBOL_TYPE_LT_I       =  5 | COMPARISON | INSENSITIVE,    // ~<
    BURN_SYMBOL_TYPE_GT_I       =  6 | COMPARISON | INSENSITIVE,    // ~>
    BURN_SYMBOL_TYPE_LE_I       =  7 | COMPARISON | INSENSITIVE,    // ~<=
    BURN_SYMBOL_TYPE_GE_I       =  8 | COMPARISON | INSENSITIVE,    // ~>=
    BURN_SYMBOL_TYPE_EQ_I       =  9 | COMPARISON | INSENSITIVE,    // ~=
    BURN_SYMBOL_TYPE_NE_I       = 10 | COMPARISON | INSENSITIVE,    // ~<>
    BURN_SYMBOL_TYPE_BAND_I     = 11 | COMPARISON | INSENSITIVE,    // ~><
    BURN_SYMBOL_TYPE_HIEQ_I     = 12 | COMPARISON | INSENSITIVE,    // ~<<
    BURN_SYMBOL_TYPE_LOEQ_I     = 13 | COMPARISON | INSENSITIVE,    // ~>>
    BURN_SYMBOL_TYPE_LPAREN     = 14,                               // (
    BURN_SYMBOL_TYPE_RPAREN     = 15,                               // )
    BURN_SYMBOL_TYPE_NUMBER     = 16,
    BURN_SYMBOL_TYPE_IDENTIFIER = 17,
    BURN_SYMBOL_TYPE_LITERAL    = 18,
    BURN_SYMBOL_TYPE_VERSION    = 19,
};


// structs

struct BURN_SYMBOL
{
    BURN_SYMBOL_TYPE Type;
    DWORD iPosition;
    BURN_VARIANT Value;
};

struct BURN_CONDITION_PARSE_CONTEXT
{
    BURN_VARIABLES* pVariables;
    LPCWSTR wzCondition;
    LPCWSTR wzRead;
    BURN_SYMBOL NextSymbol;
    BOOL fError;
};


// internal function declarations

static HRESULT ParseExpression(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    );
static HRESULT ParseBooleanTerm(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    );
static HRESULT ParseBooleanFactor(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    );
static HRESULT ParseTerm(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    );
static HRESULT ParseValue(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext,
    __out BURN_VARIANT* pValue
    );
static HRESULT Expect(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext,
    __in BURN_SYMBOL_TYPE symbolType
    );
static HRESULT NextSymbol(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext
    );
static HRESULT CompareValues(
    __in BURN_SYMBOL_TYPE comparison,
    __in BURN_VARIANT leftOperand,
    __in BURN_VARIANT rightOperand,
    __out BOOL* pfResult
    );
static HRESULT CompareStringValues(
    __in BURN_SYMBOL_TYPE comparison,
    __in_z LPCWSTR wzLeftOperand,
    __in_z LPCWSTR wzRightOperand,
    __out BOOL* pfResult
    );
static HRESULT CompareIntegerValues(
    __in BURN_SYMBOL_TYPE comparison,
    __in LONGLONG llLeftOperand,
    __in LONGLONG llRightOperand,
    __out BOOL* pfResult
    );
static HRESULT CompareVersionValues(
    __in BURN_SYMBOL_TYPE comparison,
    __in DWORD64 qwLeftOperand,
    __in DWORD64 qwRightOperand,
    __out BOOL* pfResult
    );


// function definitions

extern "C" HRESULT ConditionEvaluate(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf
    )
{
    HRESULT hr = S_OK;
    BURN_CONDITION_PARSE_CONTEXT context = { };
    BOOL f = FALSE;

    context.pVariables = pVariables;
    context.wzCondition = wzCondition;
    context.wzRead = wzCondition;

    hr = NextSymbol(&context);
    ExitOnFailure(hr, "Failed to read next symbol.");

    hr = ParseExpression(&context, &f);
    ExitOnFailure(hr, "Failed to parse expression.");

    hr = Expect(&context, BURN_SYMBOL_TYPE_END);
    ExitOnFailure(hr, "Failed to expect end symbol.");

    LogId(REPORT_VERBOSE, MSG_CONDITION_RESULT, wzCondition, LoggingTrueFalseToString(f));

    *pf = f;
    hr = S_OK;

LExit:
    if (context.fError)
    {
        Assert(FAILED(hr));
        LogErrorId(hr, MSG_FAILED_PARSE_CONDITION, wzCondition, NULL, NULL);
    }

    return hr;
}

extern "C" HRESULT ConditionGlobalCheck(
    __in BURN_VARIABLES* pVariables,
    __in BURN_CONDITION* pCondition,
    __in BOOTSTRAPPER_DISPLAY display,
    __in_z LPCWSTR wzBundleName,
    __out DWORD *pdwExitCode,
    __out BOOL *pfContinueExecution
    )
{
    HRESULT hr = S_OK;
    BOOL fSuccess = TRUE;
    HRESULT hrError = HRESULT_FROM_WIN32(ERROR_OLD_WIN_VERSION);
    OS_VERSION osv = OS_VERSION_UNKNOWN;
    DWORD dwServicePack = 0;

    OsGetVersion(&osv, &dwServicePack);

    // Always error on Windows 2000 or lower
    if (OS_VERSION_WIN2000 >= osv)
    {
        fSuccess = FALSE;
    }
    else
    {
        if (NULL != pCondition->sczConditionString)
        {
            hr = ConditionEvaluate(pVariables, pCondition->sczConditionString, &fSuccess);
            ExitOnFailure1(hr, "Failed to evaluate condition: %ls", pCondition->sczConditionString);
        }
    }

    if (!fSuccess)
    {
        // Display the error messagebox, as long as we're in an appropriate display mode
        hr = SplashScreenDisplayError(display, wzBundleName, hrError);
        ExitOnFailure(hr, "Failed to display error dialog");

        *pdwExitCode = static_cast<DWORD>(hrError);
        *pfContinueExecution = FALSE;
    }

LExit:
    return hr;
}

HRESULT ConditionGlobalParseFromXml(
    __in BURN_CONDITION* pCondition,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pixnNode = NULL;
    BSTR bstrExpression = NULL;

    // select variable nodes
    hr = XmlSelectSingleNode(pixnBundle, L"Condition", &pixnNode);
    if (S_FALSE == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to select condition node.");

    // @Condition
    hr = XmlGetText(pixnNode, &bstrExpression);
    ExitOnFailure(hr, "Failed to get Condition inner text.");

    hr = StrAllocString(&pCondition->sczConditionString, bstrExpression, 0);
    ExitOnFailure(hr, "Failed to copy condition string from BSTR");

LExit:
    ReleaseBSTR(bstrExpression);
    ReleaseObject(pixnNode);

    return hr;
}


// internal function definitions

static HRESULT ParseExpression(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    )
{
    HRESULT hr = S_OK;
    BOOL fFirst = FALSE;
    BOOL fSecond = FALSE;

    hr = ParseBooleanTerm(pContext, &fFirst);
    ExitOnFailure(hr, "Failed to parse boolean-term.");

    if (BURN_SYMBOL_TYPE_OR == pContext->NextSymbol.Type)
    {
        hr = NextSymbol(pContext);
        ExitOnFailure(hr, "Failed to read next symbol.");

        hr = ParseExpression(pContext, &fSecond);
        ExitOnFailure(hr, "Failed to parse expression.");

        *pf = fFirst || fSecond;
    }
    else
    {
        *pf = fFirst;
    }

LExit:
    return hr;
}

static HRESULT ParseBooleanTerm(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    )
{
    HRESULT hr = S_OK;
    BOOL fFirst = FALSE;
    BOOL fSecond = FALSE;

    hr = ParseBooleanFactor(pContext, &fFirst);
    ExitOnFailure(hr, "Failed to parse boolean-factor.");

    if (BURN_SYMBOL_TYPE_AND == pContext->NextSymbol.Type)
    {
        hr = NextSymbol(pContext);
        ExitOnFailure(hr, "Failed to read next symbol.");

        hr = ParseBooleanTerm(pContext, &fSecond);
        ExitOnFailure(hr, "Failed to parse boolean-term.");

        *pf = fFirst && fSecond;
    }
    else
    {
        *pf = fFirst;
    }

LExit:
    return hr;
}

static HRESULT ParseBooleanFactor(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    )
{
    HRESULT hr = S_OK;
    BOOL fNot = FALSE;
    BOOL f = FALSE;

    if (BURN_SYMBOL_TYPE_NOT == pContext->NextSymbol.Type)
    {
        hr = NextSymbol(pContext);
        ExitOnFailure(hr, "Failed to read next symbol.");

        fNot = TRUE;
    }

    hr = ParseTerm(pContext, &f);
    ExitOnFailure(hr, "Failed to parse term.");

    *pf = fNot ? !f : f;

LExit:
    return hr;
}

static HRESULT ParseTerm(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    )
{
    HRESULT hr = S_OK;
    BURN_VARIANT firstValue = { };
    BURN_VARIANT secondValue = { };

    if (BURN_SYMBOL_TYPE_LPAREN == pContext->NextSymbol.Type)
    {
        hr = NextSymbol(pContext);
        ExitOnFailure(hr, "Failed to read next symbol.");

        hr = ParseExpression(pContext, pf);
        ExitOnFailure(hr, "Failed to parse expression.");

        hr = Expect(pContext, BURN_SYMBOL_TYPE_RPAREN);
        ExitOnFailure(hr, "Failed to expect right parenthesis.");

        ExitFunction1(hr = S_OK);
    }

    hr = ParseValue(pContext, &firstValue);
    ExitOnFailure(hr, "Failed to parse value.");

    if (COMPARISON & pContext->NextSymbol.Type)
    {
        BURN_SYMBOL_TYPE comparison = pContext->NextSymbol.Type;

        hr = NextSymbol(pContext);
        ExitOnFailure(hr, "Failed to read next symbol.");

        hr = ParseValue(pContext, &secondValue);
        ExitOnFailure(hr, "Failed to parse value.");

        hr = CompareValues(comparison, firstValue, secondValue, pf);
        ExitOnFailure(hr, "Failed to compare value.");
    }
    else
    {
        LONGLONG llValue = 0;
        LPWSTR sczValue = NULL;
        DWORD64 qwValue = 0;
        switch (firstValue.Type)
        {
        case BURN_VARIANT_TYPE_NONE:
            *pf = FALSE;
            break;
        case BURN_VARIANT_TYPE_STRING:
            hr = BVariantGetString(&firstValue, &sczValue);
            if (SUCCEEDED(hr))
            {
                *pf = sczValue && *sczValue;
            }
            StrSecureZeroFreeString(sczValue);
            break;
        case BURN_VARIANT_TYPE_NUMERIC:
            hr = BVariantGetNumeric(&firstValue, &llValue);
            if (SUCCEEDED(hr))
            {
                *pf = 0 != llValue;
            }
            SecureZeroMemory(&llValue, sizeof(llValue));
            break;
        case BURN_VARIANT_TYPE_VERSION:
            hr = BVariantGetVersion(&firstValue, &qwValue);
            if (SUCCEEDED(hr))
            {
                *pf = 0 != qwValue;
            }
            SecureZeroMemory(&llValue, sizeof(qwValue));
            break;
        default:
            ExitFunction1(hr = E_UNEXPECTED);
        }
    }

LExit:
    BVariantUninitialize(&firstValue);
    BVariantUninitialize(&secondValue);
    return hr;
}

static HRESULT ParseValue(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext,
    __out BURN_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;

    // Symbols don't encrypt their value, so can access the value directly.
    switch (pContext->NextSymbol.Type)
    {
    case BURN_SYMBOL_TYPE_IDENTIFIER:
        Assert(BURN_VARIANT_TYPE_STRING == pContext->NextSymbol.Value.Type);

        // find variable
        hr = VariableGetVariant(pContext->pVariables, pContext->NextSymbol.Value.sczValue, pValue);
        if (E_NOTFOUND != hr)
        {
            ExitOnRootFailure(hr, "Failed to find variable.");
        }
        break;

    case BURN_SYMBOL_TYPE_NUMBER: __fallthrough;
    case BURN_SYMBOL_TYPE_LITERAL: __fallthrough;
    case BURN_SYMBOL_TYPE_VERSION:
        // steal value of symbol
        memcpy_s(pValue, sizeof(BURN_VARIANT), &pContext->NextSymbol.Value, sizeof(BURN_VARIANT));
        memset(&pContext->NextSymbol.Value, 0, sizeof(BURN_VARIANT));
        break;

    default:
        pContext->fError = TRUE;
        hr = E_INVALIDDATA;
        ExitOnRootFailure2(hr, "Failed to parse condition '%ls' at position: %u", pContext->wzCondition, pContext->NextSymbol.iPosition);
    }

    // get next symbol
    hr = NextSymbol(pContext);
    ExitOnFailure(hr, "Failed to read next symbol.");

LExit:
    return hr;
}

//
// Expect - expects a symbol.
//
static HRESULT Expect(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext,
    __in BURN_SYMBOL_TYPE symbolType
    )
{
    HRESULT hr = S_OK;

    if (pContext->NextSymbol.Type != symbolType)
    {
        pContext->fError = TRUE;
        hr = E_INVALIDDATA;
        ExitOnRootFailure2(hr, "Failed to parse condition '%ls' at position: %u", pContext->wzCondition, pContext->NextSymbol.iPosition);
    }

    hr = NextSymbol(pContext);
    ExitOnFailure(hr, "Failed to read next symbol.");

LExit:
    return hr;
}

//
// NextSymbol - finds the next symbol in an expression string.
//
static HRESULT NextSymbol(
    __in BURN_CONDITION_PARSE_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;
    WORD charType = 0;
    DWORD iPosition = 0;
    DWORD n = 0;

    // free existing symbol
    BVariantUninitialize(&pContext->NextSymbol.Value);
    memset(&pContext->NextSymbol, 0, sizeof(BURN_SYMBOL));

    // skip past blanks
    while (L'\0' != pContext->wzRead[0])
    {
        ::GetStringTypeW(CT_CTYPE1, pContext->wzRead, 1, &charType);
        if (0 == (C1_BLANK & charType))
        {
            break; // no blank, done
        }
        ++pContext->wzRead;
    }
    iPosition = (DWORD)(pContext->wzRead - pContext->wzCondition);

    // read depending on first character type
    switch (pContext->wzRead[0])
    {
    case L'\0':
        pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_END;
        break;
    case L'~':
        switch (pContext->wzRead[1])
        {
        case L'=':
            pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_EQ_I;
            n = 2;
            break;
        case L'>':
            switch (pContext->wzRead[2])
            {
            case '=':
                pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_GE_I;
                n = 3;
                break;
            case L'>':
                pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_LOEQ_I;
                n = 3;
                break;
            case L'<':
                pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_BAND_I;
                n = 3;
                break;
            default:
                pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_GT_I;
                n = 2;
            }
            break;
        case L'<':
            switch (pContext->wzRead[2])
            {
            case '=':
                pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_LE_I;
                n = 3;
                break;
            case L'<':
                pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_HIEQ_I;
                n = 3;
                break;
            case '>':
                pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_NE_I;
                n = 3;
                break;
            default:
                pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_LT_I;
                n = 2;
            }
            break;
        default:
            // error
            pContext->fError = TRUE;
            hr = E_INVALIDDATA;
            ExitOnRootFailure2(hr, "Failed to parse condition \"%ls\". Unexpected '~' operator at position %d.", pContext->wzCondition, iPosition);
        }
        break;
    case L'>':
        switch (pContext->wzRead[1])
        {
        case L'=':
            pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_GE;
            n = 2;
            break;
        case L'>':
            pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_LOEQ;
            n = 2;
            break;
        case L'<':
            pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_BAND;
            n = 2;
            break;
        default:
            pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_GT;
            n = 1;
        }
        break;
    case L'<':
        switch (pContext->wzRead[1])
        {
        case L'=':
            pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_LE;
            n = 2;
            break;
        case L'<':
            pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_HIEQ;
            n = 2;
            break;
        case L'>':
            pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_NE;
            n = 2;
            break;
        default:
            pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_LT;
            n = 1;
        }
        break;
    case L'=':
        pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_EQ;
        n = 1;
        break;
    case L'(':
        pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_LPAREN;
        n = 1;
        break;
    case L')':
        pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_RPAREN;
        n = 1;
        break;
    case L'"': // literal
        do
        {
            ++n;
            if (L'\0' == pContext->wzRead[n])
            {
                // error
                pContext->fError = TRUE;
                hr = E_INVALIDDATA;
                ExitOnRootFailure2(hr, "Failed to parse condition \"%ls\". Unterminated literal at position %d.", pContext->wzCondition, iPosition);
            }
        } while (L'"' != pContext->wzRead[n]);
        ++n; // terminating '"'

        pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_LITERAL;
        hr = BVariantSetString(&pContext->NextSymbol.Value, &pContext->wzRead[1], n - 2);
        ExitOnFailure(hr, "Failed to set symbol value.");
        break;
    default:
        if (C1_DIGIT & charType || L'-' == pContext->wzRead[0])
        {
            do
            {
                ++n;
                ::GetStringTypeW(CT_CTYPE1, &pContext->wzRead[n], 1, &charType);
                if (C1_ALPHA & charType || L'_' == pContext->wzRead[n])
                {
                    // error, identifier cannot start with a digit
                    pContext->fError = TRUE;
                    hr = E_INVALIDDATA;
                    ExitOnRootFailure2(hr, "Failed to parse condition \"%ls\". Identifier cannot start at a digit, at position %d.", pContext->wzCondition, iPosition);
                }
            } while (C1_DIGIT & charType);

            // number
            pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_NUMBER;

            LONGLONG ll = 0;
            hr = StrStringToInt64(pContext->wzRead, n, &ll);
            if (FAILED(hr))
            {
                pContext->fError = TRUE;
                hr = E_INVALIDDATA;
                ExitOnRootFailure2(hr, "Failed to parse condition \"%ls\". Constant too big, at position %d.", pContext->wzCondition, iPosition);
            }

            hr = BVariantSetNumeric(&pContext->NextSymbol.Value, ll);
            ExitOnFailure(hr, "Failed to set symbol value.");
        }
        else if (C1_ALPHA & charType || L'_' == pContext->wzRead[0])
        {
            ::GetStringTypeW(CT_CTYPE1, &pContext->wzRead[1], 1, &charType);
            if (L'v' == pContext->wzRead[0] && C1_DIGIT & charType)
            {
                // version
                DWORD cParts = 1;
                for (;;)
                {
                    ++n;
                    if (L'.' == pContext->wzRead[n])
                    {
                        ++cParts;
                        if (4 < cParts)
                        {
                            // error, too many parts in version
                            pContext->fError = TRUE;
                            hr = E_INVALIDDATA;
                            ExitOnRootFailure2(hr, "Failed to parse condition \"%ls\". Version can have a maximum of 4 parts, at position %d.", pContext->wzCondition, iPosition);
                        }
                    }
                    else
                    {
                        ::GetStringTypeW(CT_CTYPE1, &pContext->wzRead[n], 1, &charType);
                        if (C1_DIGIT != (C1_DIGIT & charType))
                        {
                            break;
                        }
                    }
                }

                // Symbols don't encrypt their value, so can access the value directly.
                hr = FileVersionFromStringEx(&pContext->wzRead[1], n - 1, &pContext->NextSymbol.Value.qwValue);
                if (FAILED(hr))
                {
                    pContext->fError = TRUE;
                    hr = E_INVALIDDATA;
                    ExitOnRootFailure2(hr, "Failed to parse condition \"%ls\". Invalid version format, at position %d.", pContext->wzCondition, iPosition);
                }

                pContext->NextSymbol.Value.Type = BURN_VARIANT_TYPE_VERSION;
                pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_VERSION;
            }
            else
            {
                do
                {
                    ++n;
                    ::GetStringTypeW(CT_CTYPE1, &pContext->wzRead[n], 1, &charType);
                } while (C1_ALPHA & charType || C1_DIGIT & charType || L'_' == pContext->wzRead[n]);

                if (2 == n && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pContext->wzRead, 2, L"OR", 2))
                {
                    // OR
                    pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_OR;
                }
                else if (3 == n && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pContext->wzRead, 3, L"AND", 3))
                {
                    // AND
                    pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_AND;
                }
                else if (3 == n && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pContext->wzRead, 3, L"NOT", 3))
                {
                    // NOT
                    pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_NOT;
                }
                else
                {
                    // identifier
                    pContext->NextSymbol.Type = BURN_SYMBOL_TYPE_IDENTIFIER;
                    hr = BVariantSetString(&pContext->NextSymbol.Value, pContext->wzRead, n);
                    ExitOnFailure(hr, "Failed to set symbol value.");
                }
            }
        }
        else
        {
            // error, unexpected character
            pContext->fError = TRUE;
            hr = E_INVALIDDATA;
            ExitOnRootFailure2(hr, "Failed to parse condition \"%ls\". Unexpected character at position %d.", pContext->wzCondition, iPosition);
        }
    }
    pContext->NextSymbol.iPosition = iPosition;
    pContext->wzRead += n;

LExit:
    return hr;
}

//
// CompareValues - compares two variant values using a given comparison.
//
static HRESULT CompareValues(
    __in BURN_SYMBOL_TYPE comparison,
    __in BURN_VARIANT leftOperand,
    __in BURN_VARIANT rightOperand,
    __out BOOL* pfResult
    )
{
    HRESULT hr = S_OK;
    LONGLONG llLeft = 0;
    DWORD64 qwLeft = 0;
    LPWSTR sczLeft = NULL;
    LONGLONG llRight = 0;
    DWORD64 qwRight = 0;
    LPWSTR sczRight = NULL;

    // get values to compare based on type
    if (BURN_VARIANT_TYPE_STRING == leftOperand.Type && BURN_VARIANT_TYPE_STRING == rightOperand.Type)
    {
        hr = BVariantGetString(&leftOperand, &sczLeft);
        ExitOnFailure(hr, "Failed to get the left string");
        hr = BVariantGetString(&rightOperand, &sczRight);
        ExitOnFailure(hr, "Failed to get the right string");
        hr = CompareStringValues(comparison, sczLeft, sczRight, pfResult);
    }
    else if (BURN_VARIANT_TYPE_NUMERIC == leftOperand.Type && BURN_VARIANT_TYPE_NUMERIC == rightOperand.Type)
    {
        hr = BVariantGetNumeric(&leftOperand, &llLeft);
        ExitOnFailure(hr, "Failed to get the left numeric");
        hr = BVariantGetNumeric(&rightOperand, &llRight);
        ExitOnFailure(hr, "Failed to get the right numeric");
        hr = CompareIntegerValues(comparison, llLeft, llRight, pfResult);
    }
    else if (BURN_VARIANT_TYPE_VERSION == leftOperand.Type && BURN_VARIANT_TYPE_VERSION == rightOperand.Type)
    {
        hr = BVariantGetVersion(&leftOperand, &qwLeft);
        ExitOnFailure(hr, "Failed to get the left version");
        hr = BVariantGetVersion(&rightOperand, &qwRight);
        ExitOnFailure(hr, "Failed to get the right version");
        hr = CompareVersionValues(comparison, qwLeft, qwRight, pfResult);
    }
    else if (BURN_VARIANT_TYPE_VERSION == leftOperand.Type && BURN_VARIANT_TYPE_STRING == rightOperand.Type)
    {
        hr = BVariantGetVersion(&leftOperand, &qwLeft);
        ExitOnFailure(hr, "Failed to get the left version");
        hr = BVariantGetVersion(&rightOperand, &qwRight);
        if (FAILED(hr))
        {
            if (DISP_E_TYPEMISMATCH != hr)
            {
                ExitOnFailure(hr, "Failed to get the right version");
            }
            *pfResult = (BURN_SYMBOL_TYPE_NE == comparison);
            hr = S_OK;
        }
        else
        {
            hr = CompareVersionValues(comparison, qwLeft, qwRight, pfResult);
        }
    }
    else if (BURN_VARIANT_TYPE_STRING == leftOperand.Type && BURN_VARIANT_TYPE_VERSION == rightOperand.Type)
    {
        hr = BVariantGetVersion(&rightOperand, &qwRight);
        ExitOnFailure(hr, "Failed to get the right version");
        hr = BVariantGetVersion(&leftOperand, &qwLeft);
        if (FAILED(hr))
        {
            if (DISP_E_TYPEMISMATCH != hr)
            {
                ExitOnFailure(hr, "Failed to get the left version");
            }
            *pfResult = (BURN_SYMBOL_TYPE_NE == comparison);
            hr = S_OK;
        }
        else
        {
            hr = CompareVersionValues(comparison, qwLeft, qwRight, pfResult);
        }
    }
    else if (BURN_VARIANT_TYPE_NUMERIC == leftOperand.Type && BURN_VARIANT_TYPE_STRING == rightOperand.Type)
    {
        hr = BVariantGetNumeric(&leftOperand, &llLeft);
        ExitOnFailure(hr, "Failed to get the left numeric");
        hr = BVariantGetNumeric(&rightOperand, &llRight);
        if (FAILED(hr))
        {
            if (DISP_E_TYPEMISMATCH != hr)
            {
                ExitOnFailure(hr, "Failed to get the right numeric");
            }
            *pfResult = (BURN_SYMBOL_TYPE_NE == comparison);
            hr = S_OK;
        }
        else
        {
            hr = CompareIntegerValues(comparison, llLeft, llRight, pfResult);
        }
    }
    else if (BURN_VARIANT_TYPE_STRING == leftOperand.Type && BURN_VARIANT_TYPE_NUMERIC == rightOperand.Type)
    {
        hr = BVariantGetNumeric(&rightOperand, &llRight);
        ExitOnFailure(hr, "Failed to get the right numeric");
        hr = BVariantGetNumeric(&leftOperand, &llLeft);
        if (FAILED(hr))
        {
            if (DISP_E_TYPEMISMATCH != hr)
            {
                ExitOnFailure(hr, "Failed to get the left numeric");
            }
            *pfResult = (BURN_SYMBOL_TYPE_NE == comparison);
            hr = S_OK;
        }
        else
        {
            hr = CompareIntegerValues(comparison, llLeft, llRight, pfResult);
        }
    }
    else
    {
        // not a combination that can be compared
        *pfResult = (BURN_SYMBOL_TYPE_NE == comparison);
    }

LExit:
    SecureZeroMemory(&qwLeft, sizeof(DWORD64));
    SecureZeroMemory(&llLeft, sizeof(LONGLONG));
    StrSecureZeroFreeString(sczLeft);
    SecureZeroMemory(&qwRight, sizeof(DWORD64));
    SecureZeroMemory(&llRight, sizeof(LONGLONG));
    StrSecureZeroFreeString(sczRight);

    return hr;
}

//
// CompareStringValues - compares two string values using a given comparison.
//
static HRESULT CompareStringValues(
    __in BURN_SYMBOL_TYPE comparison,
    __in_z LPCWSTR wzLeftOperand,
    __in_z LPCWSTR wzRightOperand,
    __out BOOL* pfResult
    )
{
    HRESULT hr = S_OK;
    DWORD dwCompareString = (comparison & INSENSITIVE) ? NORM_IGNORECASE : 0;
    int cchLeft = lstrlenW(wzLeftOperand);
    int cchRight = lstrlenW(wzRightOperand);

    switch (comparison)
    {
    case BURN_SYMBOL_TYPE_LT:
    case BURN_SYMBOL_TYPE_GT:
    case BURN_SYMBOL_TYPE_LE:
    case BURN_SYMBOL_TYPE_GE:
    case BURN_SYMBOL_TYPE_EQ:
    case BURN_SYMBOL_TYPE_NE:
    case BURN_SYMBOL_TYPE_LT_I:
    case BURN_SYMBOL_TYPE_GT_I:
    case BURN_SYMBOL_TYPE_LE_I:
    case BURN_SYMBOL_TYPE_GE_I:
    case BURN_SYMBOL_TYPE_EQ_I:
    case BURN_SYMBOL_TYPE_NE_I:
        {
            int i = ::CompareStringW(LOCALE_INVARIANT, dwCompareString, wzLeftOperand, cchLeft, wzRightOperand, cchRight);
            hr = CompareIntegerValues(comparison, i, CSTR_EQUAL, pfResult);
        }
        break;
    case BURN_SYMBOL_TYPE_BAND:
    case BURN_SYMBOL_TYPE_BAND_I:
        // test if left string contains right string
        for (int i = 0; (i + cchRight) <= cchLeft; ++i)
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, dwCompareString, wzLeftOperand + i, cchRight, wzRightOperand, cchRight))
            {
                *pfResult = TRUE;
                ExitFunction();
            }
        }
        *pfResult = FALSE;
        break;
    case BURN_SYMBOL_TYPE_HIEQ:
    case BURN_SYMBOL_TYPE_HIEQ_I:
        // test if left string starts with right string
        *pfResult = cchLeft >= cchRight && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, dwCompareString, wzLeftOperand, cchRight, wzRightOperand, cchRight);
        break;
    case BURN_SYMBOL_TYPE_LOEQ:
    case BURN_SYMBOL_TYPE_LOEQ_I:
        // test if left string ends with right string
        *pfResult = cchLeft >= cchRight && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, dwCompareString, wzLeftOperand + (cchLeft - cchRight), cchRight, wzRightOperand, cchRight);
        break;
    default:
        ExitFunction1(hr = E_INVALIDARG);
    }

LExit:
    return hr;
}

//
// CompareIntegerValues - compares two integer values using a given comparison.
//
static HRESULT CompareIntegerValues(
    __in BURN_SYMBOL_TYPE comparison,
    __in LONGLONG llLeftOperand,
    __in LONGLONG llRightOperand,
    __out BOOL* pfResult
    )
{
    HRESULT hr = S_OK;

    switch (comparison)
    {
    case BURN_SYMBOL_TYPE_LT: case BURN_SYMBOL_TYPE_LT_I: *pfResult = llLeftOperand <  llRightOperand; break;
    case BURN_SYMBOL_TYPE_GT: case BURN_SYMBOL_TYPE_GT_I: *pfResult = llLeftOperand >  llRightOperand; break;
    case BURN_SYMBOL_TYPE_LE: case BURN_SYMBOL_TYPE_LE_I: *pfResult = llLeftOperand <= llRightOperand; break;
    case BURN_SYMBOL_TYPE_GE: case BURN_SYMBOL_TYPE_GE_I: *pfResult = llLeftOperand >= llRightOperand; break;
    case BURN_SYMBOL_TYPE_EQ: case BURN_SYMBOL_TYPE_EQ_I: *pfResult = llLeftOperand == llRightOperand; break;
    case BURN_SYMBOL_TYPE_NE: case BURN_SYMBOL_TYPE_NE_I: *pfResult = llLeftOperand != llRightOperand; break;
    case BURN_SYMBOL_TYPE_BAND: case BURN_SYMBOL_TYPE_BAND_I: *pfResult = (llLeftOperand & llRightOperand) ? TRUE : FALSE; break;
    case BURN_SYMBOL_TYPE_HIEQ: case BURN_SYMBOL_TYPE_HIEQ_I: *pfResult = ((llLeftOperand >> 16) & 0xFFFF) == llRightOperand; break;
    case BURN_SYMBOL_TYPE_LOEQ: case BURN_SYMBOL_TYPE_LOEQ_I: *pfResult = (llLeftOperand & 0xFFFF) == llRightOperand; break;
    default:
        ExitFunction1(hr = E_INVALIDARG);
    }

LExit:
    return hr;
}

//
// CompareVersionValues - compares two quad-word version values using a given comparison.
//
static HRESULT CompareVersionValues(
    __in BURN_SYMBOL_TYPE comparison,
    __in DWORD64 qwLeftOperand,
    __in DWORD64 qwRightOperand,
    __out BOOL* pfResult
    )
{
    HRESULT hr = S_OK;

    switch (comparison)
    {
    case BURN_SYMBOL_TYPE_LT: *pfResult = qwLeftOperand <  qwRightOperand; break;
    case BURN_SYMBOL_TYPE_GT: *pfResult = qwLeftOperand >  qwRightOperand; break;
    case BURN_SYMBOL_TYPE_LE: *pfResult = qwLeftOperand <= qwRightOperand; break;
    case BURN_SYMBOL_TYPE_GE: *pfResult = qwLeftOperand >= qwRightOperand; break;
    case BURN_SYMBOL_TYPE_EQ: *pfResult = qwLeftOperand == qwRightOperand; break;
    case BURN_SYMBOL_TYPE_NE: *pfResult = qwLeftOperand != qwRightOperand; break;
    case BURN_SYMBOL_TYPE_BAND: *pfResult = (qwLeftOperand & qwRightOperand) ? TRUE : FALSE; break;
    case BURN_SYMBOL_TYPE_HIEQ: *pfResult = ((qwLeftOperand >> 16) & 0xFFFF) == qwRightOperand; break;
    case BURN_SYMBOL_TYPE_LOEQ: *pfResult = (qwLeftOperand & 0xFFFF) == qwRightOperand; break;
    default:
        ExitFunction1(hr = E_INVALIDARG);
    }

LExit:
    return hr;
}
