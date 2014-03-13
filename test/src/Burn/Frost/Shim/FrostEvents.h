//-------------------------------------------------------------------------------------------------
// <copyright file="FrostEvents.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Defined events for Frost shim
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#include "shim.h"

namespace Microsoft
{
namespace Tools
{
namespace WindowsInstallerXml
{
namespace Test
{
namespace Frost
{
    using namespace System;
    using namespace System::Runtime::InteropServices;

    public ref class ResultReturnArgs : public EventArgs
    {
    private:
        HRESULTS hr;

    internal:
        ResultReturnArgs() : hr(HRESULTS::HR_S_OK)
        {
        }

    public:
        property HRESULTS ResultToReturn
        {
            HRESULTS get() { return hr; }
            void set(HRESULTS value) { hr = value; }
        }
    };

    public ref class SetupActionArgs : public ResultReturnArgs
    {
    private:
        SETUP_ACTION action;

    internal:
        SetupActionArgs() : action(SETUP_ACTION::SETUP_ACTION_UNKNOWN)
        {
        }

        SetupActionArgs(SETUP_ACTION a) : action(a)
        {
        }

    public:
        property SETUP_ACTION SetupAction
        {
            SETUP_ACTION get() { return action; }
            void set(SETUP_ACTION value) { action = value; }
        }
    };

    public ref class StringEventArgs : public ResultReturnArgs
    {
    private:
        String^ strValue;
    
    internal:
        StringEventArgs() : strValue(nullptr)
        {
        }

    public:
        property String^ StringValue
        {
            String^ get() { return strValue; }
            void set(String^ value) { strValue = value; }
        }
    };

    public ref class LongIntEventArgs : public StringEventArgs
    {
    private:
        Int64 lInt;

    internal:
        LongIntEventArgs() : lInt(0)
        {
        }

    public:
        property Int64 Number
        {
            System::Int64 get() { return lInt; }
            void set(Int64 value) { lInt = value; }
        }
    };

    public ref class PackageCountEventArgs : public ResultReturnArgs
    {
    private:
        UInt32 numPackages;
       
    internal:
        PackageCountEventArgs() : numPackages(0)
        {
        }

    public:
        property UInt32 PackageCount
        {
            UInt32 get() { return numPackages; }
            void set(UInt32 value) { numPackages = value; }
        }
    };

    public ref class StringVariableEventArgs : public StringEventArgs
    {
    private:
        String^ strName;
        UInt32 dwValue;

    internal:
        StringVariableEventArgs()
        {
            dwValue = 0;
            strName = nullptr;
        }

    public:
        property UInt32 DWordValue
        {
            UInt32 get() { return dwValue; }
            void set(UInt32 value) { dwValue = value; }
        }
        
        property String^ StringName
        {
            String^ get() { return strName; }
            void set(String^ value) { strName = value; }
        }
    };

    public ref class ConditionalEventArgs : public StringEventArgs
    {
    private: 
        bool bEvalResult;

    internal:
        ConditionalEventArgs(): bEvalResult(true){}

    public:
        property bool EvalResult
        {
            bool get() { return bEvalResult; }
            void set(bool value) { bEvalResult = value; }
        }
    };

    public ref class LogEventArgs : public StringEventArgs
    {
    private:
        ENGINE_LOG_LEVEL msgLogLevel;

    internal:
        LogEventArgs() : msgLogLevel(ENGINE_LOG_LEVEL::ENGINE_LOG_LEVEL_STANDARD)
        {
        }

    public:
        property ENGINE_LOG_LEVEL MessageLogLevel
        {
            ENGINE_LOG_LEVEL get() { return msgLogLevel; }
            void set(ENGINE_LOG_LEVEL value) { msgLogLevel = value; }
        }
    };

    public ref class FormatStringEventArgs : public ResultReturnArgs
    {
    private:
        String^ strStaticVal;
        String^ strDynVal;

    internal:
        FormatStringEventArgs()
        {
            strStaticVal = nullptr;
            strDynVal = nullptr;
        }

    public:
        property String^ InValue
        {
            String^ get() { return strStaticVal; }
            void set(String^ value) { strStaticVal = value; }
        }

        property String^ InOutValue
        {
            String^ get() { return strDynVal; }
            void set(String^ value) { strDynVal = value; }
        }
    };

    public delegate void DetectEventHandler(Object^ sender, ResultReturnArgs^ e);
    public delegate void PlanEventHandler(Object^ sender, SetupActionArgs^ e);
    public delegate void ApplyEventHandler(Object^ sender, ResultReturnArgs^ e);
    public delegate void SuspendEventHandler(Object^ sender, ResultReturnArgs^ e);
    public delegate void RebootEventHandler(Object^ sender, ResultReturnArgs^e);

    public delegate void GetPackageCountEventHandler(Object^ sender, PackageCountEventArgs^ e);
    public delegate void GetStringEventHandler(Object^ sender, StringEventArgs^ e);
    public delegate void GetVariableNumericEventHandler(Object^ sender, LongIntEventArgs^ e);
    public delegate void GetVariableStringEventHandler(Object^ sender, StringVariableEventArgs^ e);
    public delegate void GetVariableVersionEventHandler(Object^ sender, LongIntEventArgs^ e);
    public delegate void SetVariableNumericEventHandler(Object^ sender, LongIntEventArgs^ e);
    public delegate void SetVariableStringEventHandler(Object^ sender, StringVariableEventArgs^ e);
    public delegate void SetVariableVersionEventHandler(Object^ sender, LongIntEventArgs^ e);
    public delegate void FormatStringEventHandler(Object^ sender, FormatStringEventArgs^ e);
    public delegate void EscapeStringEventHandler(Object^ sender, FormatStringEventArgs^ e);
    public delegate void ElevateEventHandler(Object^ sender, ResultReturnArgs^ e);
    public delegate void EvaluateConditionEventHandler(Object^ sender, ConditionalEventArgs^ e);
    public delegate void LogEventHandler(Object^ sender, LogEventArgs^ e);
    public delegate void SetSourceEventHandler(Object^ sender, StringEventArgs^ e);

}
}
}
}
}
