// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
