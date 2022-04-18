---
title: Using Localized Versions of WixUI
layout: documentation
after: wixui_customizations
---
# Using Localized Versions of WixUI

## Using translated UI strings

WixUIExtension includes a set of WiX localization (.wxl) files that contain translated UI text, error and progress text strings for several languages. To specify a UI language for your installer, pass the desired culture value on the command line when calling light. For example:

    light -ext WixUIExtension -cultures:fr-FR Product.wixobj -out Product.msi

WixUIExtension includes translated strings for the following languages:

<table border="1" cellspacing="0" cellpadding="4" id="table1">
  <tr>
    <td><b>Language</b></td>

    <td><b>Location</b></td>

    <td><b>Culture code</b></td>

    <td><b>WXL file</b></td>
  </tr>

  <tr>
    <td>Arabic</td>

    <td>Saudi Arabia</td>

    <td>ar-SA</td>

    <td>WixUI_ar-SA.wxl</td>
  </tr>

  <tr>
    <td>Bulgarian</td>

    <td>Bulgaria</td>

    <td>bg-BG</td>

    <td>WixUI_bg-BG.wxl</td>
  </tr>

  <tr>
    <td>Catalan</td>

    <td>Spain</td>

    <td>ca-ES</td>

    <td>WixUI_ca-ES.wxl</td>
  </tr>

  <tr>
    <td>Croatian</td>

    <td>Croatia</td>

    <td>hr-HR</td>

    <td>WixUI_hr-HR.wxl</td>
  </tr>

  <tr>
    <td>Czech</td>

    <td>Czech Republic</td>

    <td>cs-CZ</td>

    <td>WixUI_cs-CZ.wxl</td>
  </tr>

  <tr>
    <td>Danish</td>

    <td>Denmark</td>

    <td>da-DK</td>

    <td>WixUI_da-DK.wxl</td>
  </tr>

  <tr>
    <td>Dutch</td>

    <td>Netherlands</td>

    <td>nl-NL</td>

    <td>WixUI_nl-NL.wxl</td>
  </tr>

  <tr>
    <td>English</td>

    <td>United States</td>

    <td>en-US</td>

    <td>WixUI_en-US.wxl</td>
  </tr>

  <tr>
    <td>Estonian</td>

    <td>Estonia</td>

    <td>et-EE</td>

    <td>WixUI_et-EE.wxl</td>
  </tr>

  <tr>
    <td>Finnish</td>

    <td>Finland</td>

    <td>fi-FI</td>

    <td>WixUI_fi-FI.wxl</td>
  </tr>

  <tr>
    <td>French</td>

    <td>France</td>

    <td>fr-FR</td>

    <td>WixUI_fr-FR.wxl</td>
  </tr>

  <tr>
    <td>German</td>

    <td>Germany</td>

    <td>de-DE</td>

    <td>WixUI_de-DE.wxl</td>
  </tr>

  <tr>
    <td>Greek</td>

    <td>Greece</td>

    <td>el-GR</td>

    <td>WixUI_el-GR.wxl</td>
  </tr>

  <tr>
    <td>Hebrew</td>

    <td>Israel</td>

    <td>he-IL</td>

    <td>WixUI_he-IL.wxl</td>
  </tr>

  <tr>
    <td>Hindi</td>

    <td>India</td>

    <td>hi-IN</td>

    <td>WixUI_hi-IN.wxl</td>
  </tr>

  <tr>
    <td>Hungarian</td>

    <td>Hungary</td>

    <td>hu-HU</td>

    <td>WixUI_hu-HU.wxl</td>
  </tr>

  <tr>
    <td>Italian</td>

    <td>Italy</td>

    <td>it-IT</td>

    <td>WixUI_it-IT.wxl</td>
  </tr>

  <tr>
    <td>Japanese</td>

    <td>Japan</td>

    <td>ja-JP</td>

    <td>WixUI_ja-JP.wxl</td>
  </tr>

  <tr>
    <td>Kazakh</td>

    <td>Kazakhstan</td>

    <td>kk-KZ</td>

    <td>WixUI_kk-KZ.wxl</td>
  </tr>

  <tr>
    <td>Korean</td>

    <td>Korea</td>

    <td>ko-KR</td>

    <td>WixUI_ko-KR.wxl</td>
  </tr>

  <tr>
    <td>Latvian</td>

    <td>Latvia</td>

    <td>lv-LV</td>

    <td>WixUI_lv-LV.wxl</td>
  </tr>

  <tr>
    <td>Lithuanian</td>

    <td>Lithuania</td>

    <td>lt-LT</td>

    <td>WixUI_lt-LT.wxl</td>
  </tr>

  <tr>
    <td>Norwegian (Bokm&aring;l)</td>

    <td>Norway</td>

    <td>nb-NO</td>

    <td>WixUI_nb-NO.wxl</td>
  </tr>

  <tr>
    <td>Polish</td>

    <td>Poland</td>

    <td>pl-PL</td>

    <td>WixUI_pl-PL.wxl</td>
  </tr>

  <tr>
    <td>Portuguese</td>

    <td>Brazil</td>

    <td>pt-BR</td>

    <td>WixUI_pt-BR.wxl</td>
  </tr>

  <tr>
    <td>Portuguese</td>

    <td>Portugal</td>

    <td>pt-PT</td>

    <td>WixUI_pt-PT.wxl</td>
  </tr>

  <tr>
    <td>Romanian</td>

    <td>Romania</td>

    <td>ro-RO</td>

    <td>WixUI_ro-RO.wxl</td>
  </tr>

  <tr>
    <td>Russian</td>

    <td>Russia</td>

    <td>ru-RU</td>

    <td>WixUI_ru-RU.wxl</td>
  </tr>

  <tr>
    <td>Serbian (Latin)</td>

    <td>Serbia and Montenegro</td>

    <td>sr-Latn-CS</td>

    <td>WixUI_sr-Latn-CS.wxl</td>
  </tr>

  <tr>
    <td>Simplified Chinese</td>

    <td>China</td>

    <td>zh-CN</td>

    <td>WixUI_zh-CN.wxl</td>
  </tr>

  <tr>
    <td>Slovak</td>

    <td>Slovak Republic</td>

    <td>sk-SK</td>

    <td>WixUI_sk-SK.wxl</td>
  </tr>

  <tr>
    <td>Slovenian</td>

    <td>Solvenia</td>

    <td>sl-SI</td>

    <td>WixUI_sl_SI.wxl</td>
  </tr>

  <tr>
    <td>Spanish</td>

    <td>Spain</td>

    <td>es-ES</td>

    <td>WixUI_es-ES.wxl</td>
  </tr>

  <tr>
    <td>Swedish</td>

    <td>Sweden</td>

    <td>sv-SE</td>

    <td>WixUI_sv-SE.wxl</td>
  </tr>

  <tr>
    <td>Thai</td>

    <td>Thailand</td>

    <td>th-TH</td>

    <td>WixUI_th-TH.wxl</td>
  </tr>

  <tr>
    <td>Traditional Chinese</td>

    <td>Hong Kong SAR</td>

    <td>zh-HK</td>

    <td>WixUI_zh-HK.wxl</td>
  </tr>

  <tr>
    <td>Traditional Chinese</td>

    <td>Taiwan</td>

    <td>zh-TW</td>

    <td>WixUI_zh-TW.wxl</td>
  </tr>

  <tr>
    <td>Turkish</td>

    <td>Turkey</td>

    <td>tr-TR</td>

    <td>WixUI_tr-TR.wxl</td>
  </tr>

  <tr>
    <td>Ukrainian</td>

    <td>Ukraine</td>

    <td>uk-UA</td>

    <td>WixUI_uk-UA.wxl</td>
  </tr>
</table>

## Creating multiple setups with different setup UI languages

You can create a series of .msi files that each use different setup UI languages by calling candle once and then calling light multiple times with different culture values. For example:

    candle Product.wxs
    light -ext WixUIExtension -cultures:en-us Product.wixobj -out Product_en-us.msi
    light -ext WixUIExtension -cultures:fr-fr Product.wixobj -out Product_fr-fr.msi
    light -ext WixUIExtension -cultures:de-de Product.wixobj -out Product_de-de.msi
    light -ext WixUIExtension -cultures:it-it Product.wixobj -out Product_it-it.msi
    light -ext WixUIExtension -cultures:ja-jp Product.wixobj -out Product_ja-jp.msi
    light -ext WixUIExtension -cultures:pl-pl Product.wixobj -out Product_pl-pl.msi
    light -ext WixUIExtension -cultures:ru-ru Product.wixobj -out Product_ru-ru.msi
    light -ext WixUIExtension -cultures:es-es Product.wixobj -out Product_es-es.msi

## Using translated error and progress text

By default, WixUI will not include any translated Error or ProgressText elements. You can include them by referencing the WixUI_ErrorProgressText UI element:

    <UIRef Id="WixUI_ErrorProgressText" />
