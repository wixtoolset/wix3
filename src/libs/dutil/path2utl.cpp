//-------------------------------------------------------------------------------------------------
// <copyright file="path2utl.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Path helper functions that require shlwapi.lib.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


DAPI_(HRESULT) PathCanonicalizePath(
    __in_z LPCWSTR wzPath,
    __deref_out_z LPWSTR* psczCanonicalized
    )
{
    HRESULT hr = S_OK;
    int cch = MAX_PATH + 1;

    hr = StrAlloc(psczCanonicalized, cch);
    ExitOnFailure(hr, "Failed to allocate string for the canonicalized path.");

    if (::PathCanonicalizeW(*psczCanonicalized, wzPath))
    {
        hr = S_OK;
    }
    else
    {
        ExitFunctionWithLastError(hr);
    }

LExit:
    return hr;
}

DAPI_(HRESULT) PathDirectoryContainsPath(
    __in_z LPCWSTR wzDirectory,
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;
    LPWSTR sczDirectory = NULL;
    LPWSTR sczOriginalPath = NULL;
    LPWSTR sczOriginalDirectory = NULL;

    hr = PathCanonicalizePath(wzPath, &sczOriginalPath);
    ExitOnFailure(hr, "Failed to canonicalize the path.");

    hr = PathCanonicalizePath(wzDirectory, &sczOriginalDirectory);
    ExitOnFailure(hr, "Failed to canonicalize the directory.");

    if (!sczOriginalPath || !*sczOriginalPath)
    {
        ExitFunction1(hr = S_FALSE);
    }
    if (!sczOriginalDirectory || !*sczOriginalDirectory)
    {
        ExitFunction1(hr = S_FALSE);
    }

    sczPath = sczOriginalPath;
    sczDirectory = sczOriginalDirectory;

    for (; *sczDirectory;)
    {
        if (!*sczPath)
        {
            ExitFunction1(hr = S_FALSE);
        }

        if (CSTR_EQUAL != ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, sczDirectory, 1, sczPath, 1))
        {
            ExitFunction1(hr = S_FALSE);
        }

        ++sczDirectory;
        ++sczPath;
    }

    --sczDirectory;
    if (('\\' == *sczDirectory && *sczPath) || '\\' == *sczPath)
    {
        hr = S_OK;
    }
    else
    {
        hr = S_FALSE;
    }

LExit:
    ReleaseStr(sczOriginalPath);
    ReleaseStr(sczOriginalDirectory);
    return hr;
}
