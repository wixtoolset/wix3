---
title: Performance Counter Custom Action
layout: documentation
after: using_standard_customactions
---
# Performance Counter Custom Action

The PerfCounter element (part of WiXUtilExtension) allows you to register your performance counters with the Windows API. There are several pieces that all work together to successfully register:

* Your performance DLL - The DLL must export Open, Collect, and Close methods. See MSDN for more detail.
* Performance registry values - The registry must contain keys pointing to your DLL and its Open, Collect, and Close methods. These are created using the Registry element.
* Perfmon INI and H text files - These contain the text descriptions to display in the UI. See MSDN for lodctr documentation. <a href='http://msdn.microsoft.com/library/aa371878.aspx' target="_blank">This MSDN documentation</a> is a good place to start. See below for samples re-purposed from MSDN.
* The RegisterPerfmon custom action - You can link with the WiXUtilExtension.dll to ensure that the custom actions are included in your final MSI. See [Using Standard Custom Actions](using_standard_customactions.html). The custom action calls (Un)LoadPerfCounterTextStrings to register your counters with Windows&#65533; Perfmon API. To invoke the custom action, you create a PerfCounter element nested within the File element for the Perfmon.INI file. The PerfCounter element contains a single attribute: Name. The Name attribute should match the name in the Registry and in the .INI file. See below for sample WIX usage of the &lt;PerfCounter&gt; element.

## Sample WIX source fragment and PerfCounter.ini
    <?xml version="1.0"?>
    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
    <Fragment>
      <DirectoryRef Id="BinDir">
        <Component Id="SharedNative" DiskId="1">
    
          <Registry Id="Shared_r1" Root="HKLM" Key="SYSTEM\CurrentControlSet\Services\MyApplication\Performance" Name="Open" Value="OpenPerformanceData" Type="string" />
          <Registry Id="Shared_r2" Root="HKLM" Key="SYSTEM\CurrentControlSet\Services\MyApplication\Performance" Name="Collect" Value="CollectPerformanceData" Type="string" />
          <Registry Id="Shared_r3" Root="HKLM" Key="SYSTEM\CurrentControlSet\Services\MyApplication\Performance" Name="Close" Value="ClosePerformanceData" Type="string" />
          <Registry Id="Shared_r4" Root="HKLM" Key="SYSTEM\CurrentControlSet\Services\MyApplication\Performance" Name="Library" Value="[!PERFDLL.DLL]" Type="string" />
    
         <File Id="PERFDLL.DLL" Name="MyPerfDll.dll" Source="x86\debug\0\myperfdll.dll" />
    
         <File Id="PERFCOUNTERS.H" Name="PerfCounters.h" Source="x86\debug\0\perfcounters.h" />
         <File Id="PERFCOUNTERS.INI" Name="PerfCounters.ini" Source="x86\debug\0\perfcounters.ini" >
            <PerfCounter Name="MyApplication" />
         </File>
    
        </Component>
      </DirectoryRef>
    </Fragment>
    </Wix>

&nbsp;  

    Sample PerfCounters.ini:
    [info]
    drivername=MyApplication
    symbolfile=PerfCounters.h
    
    [languages] 
    009=English
    004=Chinese
    
    [objects]
    PERF_OBJECT_1_009_NAME=Performance object name
    PERF_OBJECT_1_004_NAME=Performance object name in Chinese
    
    [text]
    OBJECT_1_009_NAME=Name of the device
    OBJECT_1_009_HELP=Displays performance statistics of the device
    OBJECT_1_004_NAME=Name of the device in Chinese
    OBJECT_1_004_HELP=Displays performance statistics of the device in Chinese
    
    DEVICE_COUNTER_1_009_NAME=Name of first counter
    DEVICE_COUNTER_1_009_HELP=Displays the current value of the first counter
    DEVICE_COUNTER_1_004_NAME=Name of the first counter in Chinese
    DEVICE_COUNTER_1_004_HELP=Displays the value of the first counter in Chinese
    
    DEVICE_COUNTER_2_009_NAME=Name of the second counter
    DEVICE_COUNTER_2_009_HELP=Displays the current rate of the second counter
    DEVICE_COUNTER_2_004_NAME=Name of the second counter in Chinese
    DEVICE_COUNTER_2_004_HELP=Displays the rate of the second counter in Chinese
    
    PERF_OBJECT_1_009_NAME=Name of the third counter
    PERF_OBJECT_1_009_HELP=Displays the current rate of the third counter
    PERF_OBJECT_1_004_NAME=Name of the third counter in Chinese
    PERF_OBJECT_1_004_HELP=Displays the rate of the third counter in Chinese
    Sample PerfCounters.h:
    #define OBJECT_1    0
    #define DEVICE_COUNTER_1    2
    #define DEVICE_COUNTER_2    4
    #define PERF_OBJECT_1    8
