//-------------------------------------------------------------------------------------------------
// <copyright file="perfutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Performance helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

static BOOL vfHighPerformanceCounter = TRUE;   // assume the system has a high performance counter
static double vdFrequency = 1;


/********************************************************************
 PerfInitialize - initializes internal static variables

********************************************************************/
extern "C" void DAPI PerfInitialize(
    )
{
    LARGE_INTEGER liFrequency = { };

    //
    // check for high perf counter
    //
    if (!::QueryPerformanceFrequency(&liFrequency))
    {
        vfHighPerformanceCounter = FALSE;
        vdFrequency = 1000;  // ticks are measured in milliseconds
    }
    else
        vdFrequency = static_cast<double>(liFrequency.QuadPart);
}


/********************************************************************
 PerfClickTime - resets the clicker, or returns elapsed time since last call

 NOTE: if pliElapsed is NULL, resets the elapsed time
       if pliElapsed is not NULL, returns perf number since last call to PerfClickTime()
********************************************************************/
extern "C" void DAPI PerfClickTime(
    __out_opt LARGE_INTEGER* pliElapsed
    )
{
    static LARGE_INTEGER liStart = { };
    LARGE_INTEGER* pli = pliElapsed;

    if (!pli)  // if elapsed time time was not requested, reset the start time
        pli = &liStart;

    if (vfHighPerformanceCounter)
        ::QueryPerformanceCounter(pli);
    else
        pli->QuadPart = ::GetTickCount();

    if (pliElapsed)
        pliElapsed->QuadPart -= liStart.QuadPart;
}


/********************************************************************
 PerfConvertToSeconds - converts perf number to seconds

********************************************************************/
extern "C" double DAPI PerfConvertToSeconds(
    __in const LARGE_INTEGER* pli
    )
{
    Assert(0 < vdFrequency);
    return pli->QuadPart / vdFrequency;
}
