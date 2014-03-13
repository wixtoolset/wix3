//-------------------------------------------------------------------------------------------------
// <copyright file="atomUpdate.js" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

if (3 > WScript.Arguments.length)
{
  WScript.StdErr.WriteLine("Must specify path to history.txt, atom feed, Wix35.msi [Wix36.exe]");
  WScript.Quit(1);
}

var WshShell = new ActiveXObject('WScript.Shell');
var FileSystem = new ActiveXObject('Scripting.FileSystemObject');
var Msi = new ActiveXObject('WindowsInstaller.Installer');
var ForReading = 1;

var historyTxtPath = WScript.Arguments(0);
var feedPath = WScript.Arguments(1);
var msiPath = WScript.Arguments(2);
var exePath = null;
if (3 < WScript.Arguments.length)
{
  exePath = WScript.Arguments(3);
}

var history = new historyInfo(historyTxtPath);
var product = new productInfo(msiPath, exePath);
var atom = new atomInfo(feedPath, history, product);

if (null != atom.xml)
{
  // WScript.Echo(atom.xml.documentElement.xml);
  atom.xml.save(feedPath);
  WScript.Echo("Successfully updated: " + feedPath);
}
else
{
  WScript.Echo("No update needed to: " + feedPath);
}
WScript.Quit(0);


function historyInfo(historyPath)
{
  var fileHandle = FileSystem.OpenTextFile(historyTxtPath, ForReading);
  var fileContents = fileHandle.ReadAll();

  var thisBuildIndex = fileContents.indexOf("WixBuild: Version ")
  var previousBuildIndex = fileContents.indexOf("WixBuild: Version ", thisBuildIndex + 1);

  var historyStartIndex = fileContents.indexOf("\r\n", thisBuildIndex);
  var history = fileContents.substring(historyStartIndex, previousBuildIndex);
  var a = history.split("\r\n\r\n");

  // Remove blank lines
  for (var i = 0; i < a.length; i++)
  {
    if (a[i] == "")
    {
      a.splice(i, 1);
      i--;
    }
  }

  this.version = getVersion(fileContents, thisBuildIndex);
  this.description = "<p>Change list:<ul><li>" + a.reverse().join("</li><li>") + "</li></ul></p>";
}

function getVersion(all, index)
{
  var verStartIndex = index + 18;
  var verEndIndex = all.indexOf("\r\n", verStartIndex);

  return all.substring(verStartIndex, verEndIndex);
}

function productInfo(msiPath, exePath)
{
  var db = Msi.OpenDatabase(msiPath, 0);
  var view = db.OpenView("SELECT `Value` FROM `Property` WHERE `Property`='ProductCode'");
  view.Execute();

  var rec = view.Fetch();
  this.code = rec.StringData(1).substr(1, 36);

  view = db.OpenView("SELECT `Value` FROM `Property` WHERE `Property`='UpgradeCode'");
  view.Execute();

  rec = view.Fetch();
  this.upgrade = rec.StringData(1).substr(1, 36);

  var lastSlashIndex = exePath.lastIndexOf("\\");
  this.fileName = exePath.substring(lastSlashIndex + 1);
  this.mimeType = "application/exe";
  this.size = FileSystem.GetFile(exePath).Size;
}

function atomInfo(feedPath, history, product)
{
  var xml = new ActiveXObject("Msxml2.DOMDocument.3.0");
  xml.async = false;
  xml.load(feedPath);

  if (0 != xml.parseError.errorCode)
  {
    var myErr = xml.parseError;
    WScript.StdErr.WriteLine("ATOM feed has error " + myErr.reason);
  }
  else
  {
    var d = new Date();
    var dateString = AtomDate(d);
    var root = xml.documentElement;

    var entryExists = root.selectSingleNode("/feed/entry/id[.='http://wixtoolset.org/releases/" + history.version + "']");
    if (null != entryExists)
    {
      this.xml = null;
      return;
    }

    var entry = xml.createNode(1, "entry", "http://www.w3.org/2005/Atom");

    var title = xml.createNode(1, "title", "http://www.w3.org/2005/Atom");
    title.text = "Windows Installer Xml toolset v" + history.version;
    entry.appendChild(title);

    var id = xml.createNode(1, "id", "http://www.w3.org/2005/Atom");
    id.text = "http://wixtoolset.org/releases/" + history.version;
    entry.appendChild(id);

    var author = xml.createNode(1, "author", "http://www.w3.org/2005/Atom");
    var authorName = xml.createNode(1, "name", "http://www.w3.org/2005/Atom");
    authorName.text = "wixtoolset";
    author.appendChild(authorName);
    var authorUri = xml.createNode(1, "uri", "http://www.w3.org/2005/Atom");
    authorUri.text = "http://twitter.com/wixtoolset";
    author.appendChild(authorUri);
    var authorEmail = xml.createNode(1, "email", "http://www.w3.org/2005/Atom");
    authorEmail.text = "wix-users@lists.sourceforge.net";
    author.appendChild(authorEmail);
    entry.appendChild(author);

    var link = xml.createNode(1, "link", "http://www.w3.org/2005/Atom");
    link.setAttribute("rel", "alternate");
    link.setAttribute("href", "http://wixtoolset.org/releases/" + history.version);
    entry.appendChild(link);

    var enclosure = xml.createNode(1, "link", "http://www.w3.org/2005/Atom");
    enclosure.setAttribute("rel", "enclosure");
    enclosure.setAttribute("href", "http://wixtoolset.org/releases/" + history.version + "/" + product.fileName);
    enclosure.setAttribute("length", product.size);
    enclosure.setAttribute("type", product.mimeType);
    entry.appendChild(enclosure);

    var content = xml.createNode(1, "content", "http://www.w3.org/2005/Atom");
    content.setAttribute("type", "html");
    content.text = history.description;
    entry.appendChild(content);

    var application = xml.createNode(1, "application", "http://appsyndication.org/2006/appsyn");
    application.setAttribute("type", product.mimeType);
    application.text = product.code;
    entry.appendChild(application);

    var ver = xml.createNode(1, "version", "http://appsyndication.org/2006/appsyn");
    ver.text = history.version;
    entry.appendChild(ver);

    var pubDate = xml.createNode(1, "updated", "http://www.w3.org/2005/Atom");
    pubDate.text = dateString;
    entry.appendChild(pubDate);

    var feedElement = root.selectSingleNode("/feed");
    var entryElement = feedElement.selectSingleNode("entry");
    if (null == entryElement)
    {
      feedElement.appendChild(entry);
    }
    else
    {
      feedElement.insertBefore(entry, entryElement);
    }

    var updatedElement = feedElement.selectSingleNode("updated");
    updatedElement.text = dateString;

    this.xml = xml;
  }
}

function AtomDate(date)
{
  return "" + date.getUTCFullYear() + "-" + TwoDigits(date.getUTCMonth() + 1) + "-" + TwoDigits(date.getUTCDate()) + "T" + TwoDigits(date.getUTCHours()) + ":" + TwoDigits(date.getUTCMinutes()) + ":" + TwoDigits(date.getUTCSeconds()) + "Z";
}

function TwoDigits(s)
{
  return 10 > s ? "0" + s : "" + s;
}
