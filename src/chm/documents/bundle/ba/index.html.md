---
title: Building Bootstrapper Application
layout: documentation
after: ../wixstdba/
---
# Building Bootstrapper Application

Burn is a bootstrapper, downloader, and chainer engine. As a bootstrapper, Burn is responsible for getting the installation process started with the fewest dependencies possible. As a downloader, Burn is responsible for robustly caching files from source media (such as CD), a standalone download or the Internet. As a chainer, Burn is responsible for installing multiple installation packages in a consistent transaction. As an engine, Burn provides all of this functionality via interfaces to the hosted bootstrapper application.

The bootstrapper application (BA) is a DLL loaded by the Burn engine. The engine provides the BA an interface to control the engine called IBootstrapperEngine. The engine expects the BA to provide an interface called [IBootstrapperApplication](bootstrapper_application_interface.html) so the engine can provide progress.

The engine retrieves the IBootstrapperApplication interface by calling the BootstrapperApplicationCreate function that must be exported by the BA DLL. This function looks like this:

    extern "C" HRESULT WINAPI BootstrapperApplicationCreate(
        __in IBootstrapperEngine* pEngine,
        __in const BOOTSTRAPPER_COMMAND* pCommand,
        __out IBootstrapperApplication** ppApplication
        )

The BOOTSTRAPPER_COMMAND structure is provided by the engine and contains information read from the command-line. On success, the BA returns its IBootstrapperApplication interface. The BA DLL is provided the IBootstrapperEngine interface when the engine calls IBootstrapperApplication::OnStartup.

The BA DLL can optionally provide an exported function named BootstrapperApplicationDestroy that the engine will call just before unloading the BA DLL. Most cleanup operations should take place in IBootstrapperApplication::OnShutdown but sometimes there are resources created during BootstrapperApplicationCreate that need to be cleaned up in BootstrapperApplicationDestroy. The entry point looks like this:

    extern "C" void WINAPI BootstrapperApplicationDestroy()
