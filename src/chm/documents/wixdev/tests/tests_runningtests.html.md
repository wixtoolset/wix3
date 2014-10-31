---
title: Running WiX Tests
layout: documentation
---

# Running WiX Tests

There is a suite of tests that are included with WiX. They can be used to verify that changes to the toolset do not regress existing functionality.

## Building the Tests

The tests will build as part of the normal WiX build. They have a dependency on Microsoft.VisualStudio.QualityTools.UnitTestFramework 9.0.0.0 assembly that ships with the following editions of Visual Studio:

* Visual Studio 2008 Professional Edition
* Visual Studio Team System 2008 Database Edition
* Visual Studio Team System 2008 Development Edition
* Visual Studio Team System 2008 Team Suite
* Visual Studio Team System 2008 Test Edition

The build system searches the registry to detect if one of the above mentioned editions is installed on the machine. If the [detection key](http://blogs.msdn.com/b/astebner/archive/2007/10/18/5516901.aspx) cannot be found then the tests will not build from Nant but they can still be built by MSBuild if the required UnitTestFramework assembly exists.  
%nbsp;  
The tests are built into an assembly called wixtests.dll to the same location as the other WiX binaries.

### Building the tests using Nant

Nant must be run from the WiX root directory. To build only the tests, specify the &apos;wixtests&apos; target.

    c:\delivery\dev\wix>nant.exe wixtests

### Building the tests in Visual Studio

Open c:\delivery\dev\wix\test\wixtests.sln from a WiX command window. The solution should build from within Visual Studio.

    devenv.exe c:\delivery\dev\wix\test\wixtests.sln

## Running the tests

The tests can be run from within Visual Studio or from the command line. Before the tests are run, the environment variable &apos;WIX_ROOT&apos; must be set to the WiX root directory. It should be set if you are in a WiX command window, but if it is not:

    set WIX_ROOT=c:\delivery\dev\wix

The WIX_ROOT environment variable requirement is used in many tests to locate test data.

### Running the tests from the command line with MSTest.bat

There is a batch file, test.bat, which can be used to run the tests.

    c:\delivery\dev\wix\test\test.bat [all|smoke|test name]

### Running the tests from the command line with MSTest.exe

Run MSTest with the test binaries.

    mstest.exe c:\delivery\Dev\wix\build\debug\x86\wixtests.dll

### Running the tests from Visual Studio

Open wixtests.sln from a WiX command window.

    devenv.exe c:\delivery\dev\wix\test\wixtests.sln

Run the tests from Visual Studio Test Manager.
