//-------------------------------------------------------------------------------------------------
// <copyright file="UtilExtension.PropertyTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Util Extension Property tests</summary>
//-------------------------------------------------------------------------------------------------

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;
   
    /// <summary>
    /// Util extension Property element tests
    /// </summary>
    [TestClass]
    public class PropertyTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\PropertyTests");

        [TestMethod]
        [Description("Verify that the predefined properties have the expected values.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void Property_Install()
        {
            string sourceFile = Path.Combine(PropertyTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            string logFile = MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Special Folder properties
            VerifyPropery(logFile, "WIX_DIR_ADMINTOOLS", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.AdminTools));
            VerifyPropery(logFile, "WIX_DIR_ALTSTARTUP", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.Startup));
            VerifyPropery(logFile, "WIX_DIR_CDBURN_AREA", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.CDBurning));
            VerifyPropery(logFile, "WIX_DIR_COMMON_ADMINTOOLS", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.CommonAdminTools));
            VerifyPropery(logFile, "WIX_DIR_COMMON_ALTSTARTUP", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.CommonStartup));
            VerifyPropery(logFile, "WIX_DIR_COMMON_DOCUMENTS", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.PublicDocuments));
            VerifyPropery(logFile, "WIX_DIR_COMMON_FAVORITES", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.Favorites));
            VerifyPropery(logFile, "WIX_DIR_COMMON_MUSIC", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.PublicMusic));
            VerifyPropery(logFile, "WIX_DIR_COMMON_PICTURES", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.PublicPictures));
            VerifyPropery(logFile, "WIX_DIR_COMMON_VIDEO", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.PublicVideos));
            VerifyPropery(logFile, "WIX_DIR_COOKIES", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.Cookies));
            VerifyPropery(logFile, "WIX_DIR_DESKTOP", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.Desktop));
            VerifyPropery(logFile, "WIX_DIR_HISTORY", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.History));
            VerifyPropery(logFile, "WIX_DIR_INTERNET_CACHE", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.InternetCache));
            VerifyPropery(logFile, "WIX_DIR_MYMUSIC", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.Music));
            VerifyPropery(logFile, "WIX_DIR_MYPICTURES", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.Pictures));
            VerifyPropery(logFile, "WIX_DIR_MYVIDEO", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.Videos));
            VerifyPropery(logFile, "WIX_DIR_NETHOOD", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.NetHood));
            VerifyPropery(logFile, "WIX_DIR_PERSONAL", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.UsersFiles));
            VerifyPropery(logFile, "WIX_DIR_PRINTHOOD", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.PrintHood));
            VerifyPropery(logFile, "WIX_DIR_PROFILE", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.Profile));
            VerifyPropery(logFile, "WIX_DIR_RECENT", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.Recent));
            VerifyPropery(logFile, "WIX_DIR_RESOURCES", SpecialFoldersVerifier.GetFolderPath(SpecialFoldersVerifier.KnownFolder.ResourceDir));
            
            // Verify SID properties
            VerifyPropery(logFile,"WIX_ACCOUNT_LOCALSYSTEM", UserVerifier.GetLocalUserNameFromSID(UserVerifier.SIDStrings.NT_AUTHORITY_SYSTEM));
            VerifyPropery(logFile, "WIX_ACCOUNT_LOCALSERVICE", UserVerifier.GetLocalUserNameFromSID(UserVerifier.SIDStrings.NT_AUTHORITY_LOCAL_SERVICE));
            VerifyPropery(logFile, "WIX_ACCOUNT_NETWORKSERVICE", UserVerifier.GetLocalUserNameFromSID(UserVerifier.SIDStrings.NT_AUTHORITY_NETWORK_SERVICE));
            VerifyPropery(logFile, "WIX_ACCOUNT_ADMINISTRATORS", UserVerifier.GetLocalUserNameFromSID(UserVerifier.SIDStrings.BUILTIN_ADMINISTRATORS));
            VerifyPropery(logFile, "WIX_ACCOUNT_USERS", UserVerifier.GetLocalUserNameFromSID(UserVerifier.SIDStrings.BUILTIN_USERS));
            VerifyPropery(logFile, "WIX_ACCOUNT_GUESTS", UserVerifier.GetLocalUserNameFromSID(UserVerifier.SIDStrings.BUILTIN_GUESTS));

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }


        #region Helper Methods
        /// <summary>
        /// Verify a property using the logfile
        /// </summary>
        /// <param name="logFileName">Log file name</param>
        /// <param name="propertyName">The property to look for</param>
        /// <param name="propertyValue">The expected value of the property</param>
        private static void VerifyPropery(string logFileName, string propertyName, string propertyValue)
        {
            Assert.IsTrue(
                LogVerifier.MessageInLogFile(logFileName, string.Format("Property(S): {0} = {1}", propertyName, propertyValue)), 
               "Property '{0}' with expected value '{1}' was not found in the log file: '{2}'", propertyName, propertyValue, logFileName);
        }
        #endregion
    }
}
