---
title: WixGamingExtension
layout: documentation
after: using_standard_customactions
---
# WixGamingExtension

The [WixGamingExtension](~/xsd/gaming/index.html) lets you register your application as a game in Windows Vista and later, in three main categories:

* Game Explorer integration with game definition file
* Game Explorer tasks
* Rich saved-game preview

## Game Explorer integration

For an overview of Game Explorer, see <a href="http://msdn2.microsoft.com/library/bb173446.aspx" target="_blank">Getting Started With Game Explorer</a>. Game Explorer relies on an embedded file (game definition file or GDF) to control the data displayed about the game. For details about GDFs, see <a href="http://msdn2.microsoft.com/library/bb173445.aspx" target="_blank">The Game-Definition-File (GDF) Schema</a> and <a href="http://msdn2.microsoft.com/library/bb173443.aspx" target="_blank">GDF Delivery and Localization</a>. Using WixGamingExtension, you register a game with Game Explorer using the Game element as a child of your game executable&apos;s File element:

    <File Id="MyGameExeFile" Name="passenger_simulator.exe" KeyPath="yes">
        <gaming:Game Id="985D5FD3-FC40-4CE9-9EE5-F2AAAB959230">
        ...
    </File>

The Game/@Id attribute is used as the InstanceID attribute discussed <a href="http://msdn2.microsoft.com/library/bb173446.aspx#Step_4_Call_IGameExplorer_AddGame" target="_blank">here</a>, rather than generating new GUIDs at install time, which would require persisting the generated GUID and loading it for uninstall and maintenance mode.

<span class="signature">Implementation note: Using the Game element adds a row to a custom table in your .msi package and schedules the Gaming custom action; at install time, that custom action adds/updates/removes the game in Game Explorer and for operating system upgrades. (See <a href="http://msdn2.microsoft.com/library/bb173449.aspx" target="_blank">Supporting an Upgrade from Windows XP to Windows Vista</a> for details.)</span>

## Game Explorer tasks

In Game Explorer, a game&apos;s context menu includes custom *tasks*:

* *Play tasks* start the game with optional arguments.
* *Support tasks* start the user&apos;s default browser to go to a specific URL.

For details, see <a href="http://msdn2.microsoft.com/library/bb173450.aspx" target="_blank">Game Explorer Tasks</a>. In WixGameExtension, PlayTask and SupportTask are child elements of the Game element:

    <File Id="MyGameExeFile" Name="passenger_simulator.exe" KeyPath="yes">
        <gaming:Game Id="985D5FD3-FC40-4CE9-9EE5-F2AAAB959230">
            <gaming:PlayTask Name="Play" Arguments="-go" />
            <gaming:SupportTask Name="Help!" Address="http://example.com" />
            ...
        ...
    </File>

For details, see the [Gaming schema documentation](~/xsd/gaming/index.html).

<span class="signature">Implementation note: Game Explorer tasks are shortcuts, so the Gaming compiler extension translates the PlayTask into rows in [Shortcuts](~/xsd/wix/shortcut.html) and SupportTask into WixUtilExtension [InternetShortcuts](~/xsd/util/internetshortcut.html). It also creates directories to hold the shortcuts and custom actions to set the directories.</span>

## Rich saved-game preview

Windows Vista includes a shell handler that lets games expose metadata in their saved-game files. For details, see <a href="http://msdn2.microsoft.com/library/bb173448.aspx" target="_blank">Rich Saved Games</a>. If your game supports rich saved games, you can register it for the rich saved-game preview using the WixGamingExtension IsRichSavedGame attribute on the [Extension element](~/xsd/wix/extension.html):

    <ProgId Id="MyGameProgId">
        <Extension Id="MyGameSave" gaming:IsRichSavedGame="yes" />
    </ProgId>

<span class="signature">Implementation note: The Gaming compiler extension translates the IsRichSavedGame attribute to rows in the MSI <a href="http://msdn2.microsoft.com/library/aa371168.aspx" target="_blank">Registry</a> table.</span>
