---
title: Bootstrapper Application Interface
layout: documentation
---
# Bootstrapper Application Interface

The engine communicates with the bootstrapper application through callbacks to the IBootstrapperApplication interface. The first call from the engine is IBootstrapperApplication::OnStartup():

    // IBootstrapperApplication::OnStartup
    STDMETHOD(OnStartup)() = 0;

This call provides the engine&apos;s interface, IBootstrapperApplication, as well as how to display the BA&apos;s window (if there is one) and whether this instance launched directly, started from Add/Remove Programs or resumed after reboot. Typically, the BA uses this callback to start a new thread and display a user interface. After the BA returns from OnStartup, the engine enters its idle loop and waits for commands from the BA via IBootstrapperEngine.

The first action the BA should take is detection. The BA does this by a call to IBootstrapperEngine::Detect:

    // IBootstrapperEngine::Detect
    STDMETHOD(Detect)() = 0;

After detection, the BA should determine what operation the user wants to take. Historically this happens as a wizard sequence, prompting the user for installation location, feature selection, etc. When the decisions are made the BA plans the operation by calling IBootstrapperEngine::Plan:

    // IBootstrapperEngine::Plan
    STDMETHOD(Plan)(
         __in BOOTSTRAPPER_ACTION action
         ) = 0;

The BOOTSTRAPPER_ACTION is an enumeration that specifies the overall action of install, uninstall or repair. After the plan is complete, the BA can apply the changes by calling IBootstrapperEngine::Apply:

    // IBootstrapperEngine::Apply
    STDMETHOD(Apply)(
         __in_opt HWND hwndParent
         ) = 0;

The BA should provide a window handle to ensure that the elevation prompt, if one is required, is active and displayed above other windows. The bulk of the BA time will be spent handling callbacks from the Apply action.

When the BA is done, it should notify the engine by calling IBootstrapperEngine::Shutdown:

    // IBootstrapperEngine::Shutdown
    STDMETHOD(Shutdown)(
         __in DWORD dwExitCode,
         __in BOOL fRestart
         ) = 0;

The engine will then call the BA one last time via IBurnUserExperience::OnShutdown:

    // IBurnUserExperience::OnShutdown
    STDMETHOD_(void, OnShutdown)() = 0;
