//-------------------------------------------------------------------------------------------------
// <copyright file="balcondition.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Bootstrapper Application Layer condition utility.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// prototypes


DAPI_(HRESULT) BalConditionsParseFromXml(
    __in BAL_CONDITIONS* pConditions,
    __in IXMLDOMDocument* pixdManifest,
    __in_opt WIX_LOCALIZATION* pWixLoc
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pNodeList = NULL;
    IXMLDOMNode* pNode = NULL;
    BAL_CONDITION* prgConditions = NULL;
    DWORD cConditions = 0;

    hr = XmlSelectNodes(pixdManifest, L"/BootstrapperApplicationData/WixBalCondition", &pNodeList);
    ExitOnFailure(hr, "Failed to select all conditions.");

    hr = pNodeList->get_length(reinterpret_cast<long*>(&cConditions));
    ExitOnFailure(hr, "Failed to get the condition count.");

    if (!cConditions)
    {
        ExitFunction();
    }

    prgConditions = static_cast<BAL_CONDITION*>(MemAlloc(sizeof(BAL_CONDITION) * cConditions, TRUE));
    ExitOnNull(prgConditions, hr, E_OUTOFMEMORY, "Failed to allocate memory for conditions.");

    DWORD iCondition = 0;
    while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, NULL)))
    {
        hr = XmlGetAttributeEx(pNode, L"Condition", &prgConditions[iCondition].sczCondition);
        ExitOnFailure(hr, "Failed to get condition for condition.");

        hr = XmlGetAttributeEx(pNode, L"Message", &prgConditions[iCondition].sczMessage);
        ExitOnFailure(hr, "Failed to get message for condition.");

        if (pWixLoc && prgConditions[iCondition].sczMessage && *prgConditions[iCondition].sczMessage)
        {
            hr = LocLocalizeString(pWixLoc, &prgConditions[iCondition].sczMessage);
            ExitOnFailure(hr, "Failed to localize condition message.");
        }

        ++iCondition;
        ReleaseNullObject(pNode);
    }
    ExitOnFailure(hr, "Failed to parse all condition elements.");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

    pConditions->cConditions = cConditions;
    pConditions->rgConditions = prgConditions;
    prgConditions = NULL;

LExit:
    ReleaseMem(prgConditions);
    ReleaseObject(pNode);
    ReleaseObject(pNodeList);

    return hr;
}


//the contents of psczMessage may be sensitive, should keep encrypted and SecureZeroFree
DAPI_(HRESULT) BalConditionEvaluate(
    __in BAL_CONDITION* pCondition,
    __in IBootstrapperEngine* pEngine,
    __out BOOL* pfResult,
    __out_z_opt LPWSTR* psczMessage
    )
{
    HRESULT hr = S_OK;
    DWORD_PTR cchMessage = 0;

    hr = pEngine->EvaluateCondition(pCondition->sczCondition, pfResult);
    ExitOnFailure(hr, "Failed to evaluate condition with bootstrapper engine.");

    if (psczMessage)
    {
        if (*psczMessage)
        {
            hr = StrMaxLength(*psczMessage, &cchMessage);
            ExitOnFailure(hr, "Failed to get length of message.");
        }

        hr = pEngine->FormatString(pCondition->sczMessage, *psczMessage, reinterpret_cast<DWORD*>(&cchMessage));
        if (E_MOREDATA == hr)
        {
            ++cchMessage;

            hr = StrAllocate(psczMessage, cchMessage, TRUE);
            ExitOnFailure(hr, "Failed to allocate string for condition's formatted message.");

            hr = pEngine->FormatString(pCondition->sczMessage, *psczMessage, reinterpret_cast<DWORD*>(&cchMessage));
        }
        ExitOnFailure(hr, "Failed to format condition's message.");
    }

LExit:
    return hr;
}


DAPI_(void) BalConditionsUninitialize(
    __in BAL_CONDITIONS* pConditions
    )
{
    for (DWORD i = 0; i < pConditions->cConditions; ++i)
    {
        ReleaseStr(pConditions->rgConditions[i].sczMessage);
        ReleaseStr(pConditions->rgConditions[i].sczCondition);
    }

    ReleaseMem(pConditions->rgConditions);
    memset(pConditions, 0, sizeof(BAL_CONDITIONS));
}
