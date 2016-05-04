// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

FixHelpLinks();

function GetHelpCode(apiName)
{
    switch (apiName.toLowerCase())
    {
        case "msiadvertiseproduct":           return 370056;
        case "msiadvertiseproductex":         return 370057;
        case "msiapplymultiplepatches":       return 370059;
        case "msiapplypatch":                 return 370060;
        case "msibegintransaction":           return 736312;
        case "msiclosehandle":                return 370067;
        case "msicollectuserinfo":            return 370068;
        case "msiconfigurefeature":           return 370069;
        case "msiconfigureproduct":           return 370070;
        case "msiconfigureproductex":         return 370071;
        case "msicreaterecord":               return 370072;
        case "msicreatetransformsummaryinfo": return 370073;
        case "msidatabaseapplytransform":     return 370074;
        case "msidatabasecommit":             return 370075;
        case "msidatabaseexport":             return 370076;
        case "msidatabasegeneratetransform":  return 370077;
        case "msidatabasegetprimarykeys":     return 370078;
        case "msidatabaseimport":             return 370079;
        case "msidatabaseistablepersistent":  return 370080;
        case "msidatabasemerge":              return 370081;
        case "msidatabaseopenview":           return 370082;
        case "msidetermineapplicablepatches": return 370084;
        case "msideterminepatchsequence":     return 370085;
        case "msidoaction":                   return 370090;
        case "msienablelog":                  return 370091;
        case "msiendtransaction":             return 736318;
        case "msienumclients":                return 370094;
        case "msienumcomponentcosts":         return 370095;
        case "msienumcomponentqualifiers":    return 370096;
        case "msienumcomponents":             return 370097;
        case "msienumfeatures":               return 370098;
        case "msienumpatches":                return 370099;
        case "msienumpatchesex":              return 370100;
        case "msienumproducts":               return 370101;
        case "msienumproductsex":             return 370102;
        case "msienumrelatedproducts":        return 370103;
        case "msievaluatecondition":          return 370104;
        case "msiextractpatchxmldata":        return 370105;
        case "msiformatrecord":               return 370109;
        case "msigetactivedatabase":          return 370110;
        case "msigetcomponentpath":           return 370112;
        case "msigetcomponentstate":          return 370113;
        case "msigetdatabasestate":           return 370114;
        case "msigetfeaturecost":             return 370115;
        case "msigetfeatureinfo":             return 370116;
        case "msigetfeaturestate":            return 370117;
        case "msigetfeatureusage":            return 370118;
        case "msigetfeaturevalidstates":      return 370119;
        case "msigetfilehash":                return 370120;
        case "msigetfileversion":             return 370122;
        case "msigetlanguage":                return 370123;
        case "msigetlasterrorrecord":         return 370124;
        case "msigetmode":                    return 370125;
        case "msigetpatchfilelist":           return 370126;
        case "msigetpatchinfo":               return 370127;
        case "msigetpatchinfoex":             return 370128;
        case "msigetproductcode":             return 370129;
        case "msigetproductinfo":             return 370130;
        case "msigetproductinfoex":           return 370131;
        case "msigetproductinfofromscript":   return 370132;
        case "msigetproductproperty":         return 370133;
        case "msigetproperty":                return 370134;
        case "msigetshortcuttarget":          return 370299;
        case "msigetsourcepath":              return 370300;
        case "msigetsummaryinformation":      return 370301;
        case "msigettargetpath":              return 370303;
        case "msiinstallmissingcomponent":    return 370311;
        case "msiinstallmissingfile":         return 370313;
        case "msiinstallproduct":             return 370315;
        case "msijointransaction":            return 736319;
        case "msilocatecomponent":            return 370320;
        case "msinotifysidchange":            return 370328;
        case "msiopendatabase":               return 370338;
        case "msiopenpackage":                return 370339;
        case "msiopenpackageex":              return 370340;
        case "msiopenproduct":                return 370341;
        case "msiprocessadvertisescript":     return 370353;
        case "msiprocessmessage":             return 370354;
        case "msiprovideassembly":            return 370355;
        case "msiprovidecomponent":           return 370356;
        case "msiprovidequalifiedcomponent":  return 370357;
        case "msiprovidequalifiedcomponentex":return 370358;
        case "msiquerycomponnetstate":        return 370360;
        case "msiqueryfeaturestate":          return 370361;
        case "msiqueryfeaturestateex":        return 370362;
        case "msiqueryproductstate":          return 370363;
        case "msirecordcleardata":            return 370364;
        case "msirecorddatasize":             return 370365;
        case "msirecordgetfieldcount":        return 370366;
        case "msirecordgetinteger":           return 370367;
        case "msirecordgetstring":            return 370368;
        case "msirecordisnull":               return 370369;
        case "msirecordreadstream":           return 370370;
        case "msirecordsetinteger":           return 370371;
        case "msirecordsetstream":            return 370372;
        case "msirecordsetstring":            return 370373;
        case "msireinstallfeature":           return 370374;
        case "msireinstallproduct":           return 370375;
        case "msiremovepatches":              return 370376;
        case "msisequence":                   return 370382;
        case "msisetcomponentstate":          return 370383;
        case "msisetexternalui":              return 370384;
        case "msisetexternaluirecord":        return 370385;
        case "msisetfeatureattributes":       return 370386;
        case "msisetfeaturestate":            return 370387;
        case "msisetinstalllevel":            return 370388;
        case "msisetinternalui":              return 370389;
        case "msisetmode":                    return 370390;
        case "msisetproperty":                return 370391;
        case "msisettargetpath":              return 370392;
        case "msisourcelistaddmediadisk":     return 370394;
        case "msisourcelistaddsource":        return 370395;
        case "msisourcelistaddsourceex":      return 370396;
        case "msisourcelistclearall":         return 370397;
        case "msisourcelistclearallex":       return 370398;
        case "msisourcelistclearmediadisk":   return 370399;
        case "msisourcelistclearsource":      return 370401;
        case "msisourcelistenummediadisks":   return 370402;
        case "msisourcelistenumsources":      return 370403;
        case "msisourcelistforceresolution":  return 370404;
        case "msisourcelistforceresolutionex":return 370405;
        case "msisourcelistgetinfo":          return 370406;
        case "msisourcelistsetinfo":          return 370407;
        case "msisummaryinfogetproperty":     return 370409;
        case "msisummaryinfopersist":         return 370490;
        case "msisummaryinfosetproperty":     return 370491;
        case "msiusefeature":                 return 370502;
        case "msiusefeatureex":               return 370503;
        case "msiverifydiskspace":            return 370506;
        case "msiverifypackage":              return 370508;
        case "msiviewexecute":                return 370513;
        case "msiviewfetch":                  return 370514;
        case "msiviewgetcolumninfo":          return 370516;
        case "msiviewgeterror":               return 370518;
        case "msiviewmodify":                 return 370519;
        case "productid":                     return 370855;
        default:
            return 0;
    }
}

function GetHelpLink(apiName)
{
    var helpCode = GetHelpCode(apiName);
    if (helpCode != 0)
    {
        // Found a direct link!
        var prefix = (helpCode < 500000 ? "aa" : "bb");
        return "http://msdn2.microsoft.com/en-us/library/" + prefix + helpCode + ".aspx";
    }
    else
    {
        // This link works, but goes through an annoying 5-sec redirect page.
        return "http://msdn.microsoft.com/library/en-us/msi/setup/" + apiName.toLowerCase() + ".asp";
    }
}

// Change any MSI API help links from indirect MSDN references to direct references.
function FixHelpLinks()
{
    var msiLinkRegex = /msdn\.microsoft\.com\/library\/en-us\/msi\/setup\/([a-z]+)\.asp/i;
    var links = document.body.all.tags("a");
    var i;
    for (i = 0; i < links.length; i++)
    {
        var linkElem = links(i);
        var match = msiLinkRegex.exec(linkElem.href);
        if (match)
        {
            var apiName = match[1];
            linkElem.href = GetHelpLink(apiName);
            linkElem.target = "_blank";
            linkElem.title = "MSDN Library";
        }
    }
}
