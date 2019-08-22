// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

if (2 > WScript.Arguments.Length)
{
  helpAndQuit();
}

var FileSystem = new ActiveXObject('Scripting.FileSystemObject');
var ForReading = 1;
var ForWriting = 2;
var VersionEntryPrefix = "## WixBuild: Version ";

var historyTxtPath = WScript.Arguments(0);
var version = WScript.Arguments(1);

var history = new historyFile(historyTxtPath);

if (history.version != version)
{
  updateFile(historyTxtPath, version, history);
}
else
{
  WScript.Echo("Build version '" + version + "' already present in: " + historyTxtPath);
}

WScript.Quit(0);


function helpAndQuit()
{
  WScript.Echo("syntax: histver.js historyfile version");
  WScript.Quit(1);
}


function historyFile(historyPath)
{
  var fileHandle = FileSystem.OpenTextFile(historyTxtPath, ForReading);
  var fileContents = fileHandle.ReadAll();
  fileHandle.Close();

  var thisBuildIndex = fileContents.indexOf(VersionEntryPrefix);
  this.fileContents = fileContents;
  this.version = getVersion(fileContents, thisBuildIndex);
}

function getVersion(all, index)
{
  var verStartIndex = index + VersionEntryPrefix.length;
  var verEndIndex = all.indexOf("\r\n", verStartIndex);

  return all.substring(verStartIndex, verEndIndex);
}

function updateFile(path, version, history)
{
  WScript.Echo("Adding build version '" + version + "' to history file at: " + path);

  var fileHandle = FileSystem.OpenTextFile(path, ForWriting);
  fileHandle.WriteLine(VersionEntryPrefix + version);
  fileHandle.WriteLine();
  fileHandle.Write(history.fileContents);
  fileHandle.Close();
}
