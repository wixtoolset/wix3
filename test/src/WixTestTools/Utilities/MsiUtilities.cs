//-----------------------------------------------------------------------
// <copyright file="MsiUtilities.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Helper function get windows installer data for msi and msp</summary>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;
using System.Xml;
using System.Diagnostics;

//namespace WixTest.Utilities
namespace WixTest.Utilities
{
    public class MsiUtils
    {
        public enum MsiProductSearchType
        {
            version,
            language,
            state,
            assignment
        }

        public enum MsiComponentSearchType
        {
            keyPath,
            state,
            directory
        }

        public static string GetMSIProductCode(string msiPath)
        {
            return GetProperty(msiPath, "ProductCode");
        }

        public static string GetMSIProductVersion(string msiPath)
        {
            return GetProperty(msiPath, "ProductVersion");
        }

        public static string GetProperty(string msiPath, string property)
        {
            string propertyValue = null;
            if (!String.IsNullOrEmpty(msiPath))
            {
                msiPath = System.Environment.ExpandEnvironmentVariables(msiPath);
                Database db = new Database(msiPath);
                propertyValue = MsiUtils.GetProperty(db, property);
                db.Close();  // be sure to close the db or future attempts to open it will fail (i.e. to install/uninstall it)
            }
            return propertyValue;
        }

        public static string GetProperty(Database db, string property)
        {
            string retval = "";
            using (View view = db.OpenView("SELECT Property, Value FROM Property WHERE Property = '{0}'", property))
            {
                view.Execute();
                Record rec;
                if ((rec = view.Fetch()) != null)
                {
                    retval = (string)rec["Value"];
                }
                else
                {
                    throw new Exception("Property '" + property + "' was not found in: " + db.FilePath);
                }
            }
            return retval;
        }

        /// <summary>
        /// adds or updates a property in an msi file.
        /// </summary>
        /// <param name="msiFile">full path to the msi file to update</param>
        /// <param name="propertyName">name of property to add or update</param>
        /// <param name="propertyValue">value of property</param>
        public static void SetPropertyInMsi(string msiFile, string propertyName, string propertyValue)
        {
            Database db = new Database(msiFile, DatabaseOpenMode.Direct);

            string sql;
            try
            {
                // GetProperty will throw if the propertyName does not exist in the property table
                GetProperty(db, propertyName);
                sql = string.Format("UPDATE `Property` SET `Value` = '{0}' WHERE `Property` = '{1}'", propertyValue, propertyName);
            }
            catch
            {
                sql = string.Format("INSERT INTO `Property` (`Property`, `Value`) VALUES ('{0}', '{1}')", propertyName, propertyValue);
            }
            db.Execute(sql);
            db.Commit();
            db.Close();
        }

        /// <summary>
        /// Installs specified MSI
        /// </summary>
        /// <param name="srcFile">Path to MSI to be installed</param>
        public static void InstallMSI(string srcFile)
        {
            InstallMSI(srcFile, "");
        }

        /// <summary>
        /// Installs specified MSI
        /// </summary>
        /// <param name="srcFile">Path to MSI to be installed</param>
        /// <param name="commandLine">command line property settings</param>
        public static void InstallMSI(string srcFile, string commandLine)
        {
            if (!System.IO.File.Exists(srcFile))
                throw new Exception(String.Format("\"{0}\" - NOT Found", srcFile));

            Installer.InstallProduct(srcFile, commandLine);
        }

        /// <summary>
        /// UnInstall specified MSI
        /// </summary>
        /// <param name="srcFile">Path to MSI to be removed</param>
        public static void RemoveMSI(string srcFile)
        {
            if (!System.IO.File.Exists(srcFile))
                throw new Exception(String.Format("\"{0}\" - NOT Found", srcFile));

            try
            {
                if (IsProductInstalled(GetMSIProductCode(srcFile)))
                {
                    // This doesn't work:
                    //Installer.InstallProduct(srcFile, "REMOVE=ALL");
                    // The code above will only remove per-machine products and 
                    // per-user products installed under the current user.  
                    // per-user installs for other users will not be removed.
                    // Instead, do this:
                    // run a "MsiExec.exe /qn /x productcode" process under each user 
                    // account that was used to install the product to remove it.

                    foreach (ProductInstallation product in GetProducts(GetMSIProductCode(srcFile)))
                    {

                        Process proc = new Process();
                        proc.StartInfo.Arguments = "/qn /x " + GetMSIProductCode(srcFile);
                        proc.StartInfo.FileName = "msiexec.exe";

                        // BUGBUG: this needs to be passed in and not hard coded...  It is also in StartBurnstub.cs
                        string UserName = "NonAdminTestAcct";
                        string Password = "Password!123";
                        string currentUserSid = WixTest.Verifiers.Extensions.UserVerifier.GetSIDFromUserName(
                            System.Environment.ExpandEnvironmentVariables("%USERDOMAIN%"),
                            System.Environment.ExpandEnvironmentVariables("%USERNAME%"));
                        if (product.UserSid == null || 
                            product.UserSid == "s-1-1-0" ||
                            product.UserSid == currentUserSid)
                        {
                            // per-machine install or
                            // per-user install for the current user.
                            // no need to set special user/password to run the process in.
                        }
                        else
                        {
                            // per-user install for the test user.
                            proc.StartInfo.UserName = UserName;
                            proc.StartInfo.Password = new System.Security.SecureString();
                            proc.StartInfo.Password.Clear(); // Make sure there is no junk
                            foreach (char c in Password.ToCharArray())
                            {
                                proc.StartInfo.Password.AppendChar(c);
                            }
                            proc.StartInfo.Domain = Environment.MachineName.ToString();
                            proc.StartInfo.UseShellExecute = false;
                            proc.StartInfo.LoadUserProfile = true;
                        }

                        proc.Start();
                        proc.WaitForExit();
                    }
                }
            }
            catch
            {
                // don't fail if the MSI being removed doesn't exist or fails to uninstall
            }
        }


        /// <summary>
        /// Installs MSP on a Product MSI
        /// </summary>
        /// <param name="srcFile">Path to package</param>
        /// <param name="prodCode">Product code for MSI to apply patch on.</param>
        public static void InstallMSPOnProduct(string srcFile, string prodCode)
        {
            InstallMSPOnProduct(srcFile, prodCode, "");
        }

        /// <summary>
        /// Installs MSP on a Product MSI
        /// </summary>
        /// <param name="srcFile">Path to package</param>
        /// <param name="prodCode">Product code for MSI to apply patch on.</param>
        /// <param name="commandLine">commandLine used to install MSP (i.e. set properties during the patch install)</param>
        public static void InstallMSPOnProduct(string srcFile, string prodCode, string commandLine)
        {
            if (!System.IO.File.Exists(srcFile))
                throw new Exception(String.Format("\"{0}\" - NOT Found", srcFile));

            Installer.ApplyPatch(srcFile, prodCode, InstallType.SingleInstance, commandLine);
        }

        /// <summary>
        /// Removes provided package from and MSI
        /// </summary>
        /// <param name="srcFile">Patch to package</param>
        /// <param name="prodCode">Product Code for MSI to remove patch from.</param>
        public static void RemovePatchFromProduct(string srcFile, string prodCode)
        {
            if (!System.IO.File.Exists(srcFile))
                throw new Exception(String.Format("\"{0}\" - NOT Found", srcFile));

            // If patch is installed remove it otherwise do nothing.
            // TODO: Write logic to check patch is installed before calling Remove.
            string[] srcArray = { srcFile, null };
            Installer.RemovePatches(srcArray, prodCode, "");
        }

        /// <summary>
        /// Return true if it finds the given productcode in system otherwise it returns false
        /// </summary>
        /// <param name="prodCode"></param>
        /// <returns></returns>
        public static bool IsProductInstalled(string prodCode)
        {
            //look in all user's products (both per-machine and per-user)
            foreach (ProductInstallation product in ProductInstallation.GetProducts(null, "s-1-1-0", UserContexts.All))
            {
                if (product.ProductCode == prodCode)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Return a list of ProductInstallation objects.  
        /// For per-machine installs, the list will only contain 1 object.
        /// For per-user installs, the list will contain an object for each user that installed it.
        /// </summary>
        /// <param name="prodCode"></param>
        /// <returns></returns>
        public static List<ProductInstallation> GetProducts(string prodCode)
        {
            List<ProductInstallation> products = new List<ProductInstallation>();

            //look in all user's products (both per-machine and per-user)
            foreach (ProductInstallation product in ProductInstallation.GetProducts(null, "s-1-1-0", UserContexts.All))
            {
                if (product.ProductCode == prodCode)
                {
                    products.Add(product);
                }
            }
            return products;
        }

        public static string GetProductInfo(string productode, MsiProductSearchType type)
        {
            string productInfo = string.Empty;

            ProductInstallation product = new ProductInstallation(productode, "s-1-1-0", UserContexts.All);

            switch(type)
            {
                case MsiProductSearchType.assignment:
                    productInfo = product.Context.ToString(); //TODO
                    break;
                case MsiProductSearchType.language:
                    productInfo = product["ProductLanguage"].ToString();
                    break;
                case MsiProductSearchType.state:
                    productInfo = product.Context.ToString();
                    break;
                case MsiProductSearchType.version:
                    productInfo = product.ProductVersion.ToString();
                    break;
            }

            return productInfo;
        }

        public static string GetComponentInfo(string prodCode, string compId, MsiComponentSearchType type)
        {
            string msiComponent = string.Empty;
            ComponentInstallation ci = null;

            if (string.IsNullOrEmpty(prodCode))
                ci = new ComponentInstallation(compId);
            else
                ci = new ComponentInstallation(compId, prodCode);

            switch(type)
            {
                case MsiComponentSearchType.directory:
                    msiComponent = ci.Path; // TODO
                    break;
                case MsiComponentSearchType.keyPath:
                    msiComponent = ci.Path;
                    break;
                case MsiComponentSearchType.state:
                    msiComponent = ci.State.ToString();
                    break;
            }

            return msiComponent;
        }

        /// <summary>
        /// To get the patch code from a msp file
        /// </summary>
        /// <param name="mspPath"></param>
        /// <returns></returns>
        public static string GetPatchCode(string mspPath)
        {
            string patchInfo = Installer.ExtractPatchXmlData(mspPath);
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(patchInfo);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("p", "http://www.microsoft.com/msi/patch_applicability.xsd");

            // Parse the xml to the patch code
            return xmldoc.SelectSingleNode("./p:MsiPatch", nsmgr).Attributes["PatchGUID"].Value;
        }

        public static string GetPatchXmlBlob(string mspPath)
        {
            return Installer.ExtractPatchXmlData(mspPath);
        }

        /// <summary>
        /// It returns a array containing all the target productcodes for a given patch
        /// </summary>
        /// <param name="mspPath"></param>
        /// <returns></returns>
        public static string[] GetTargetProductCodes(string mspPath)
        {
            string patchInfo = Installer.ExtractPatchXmlData(mspPath);
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(patchInfo);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmldoc.NameTable);
            nsmgr.AddNamespace("p", "http://www.microsoft.com/msi/patch_applicability.xsd");

            XmlNodeList nodes = xmldoc.SelectNodes("./p:*/p:TargetProductCode", nsmgr);
            string[] targetProductCodes = new string[nodes.Count];
            int index = 0;

            foreach (XmlNode node in nodes)
            {
                targetProductCodes[index++] = node.InnerText;
            }
            return targetProductCodes;
        }

        /// <summary>
        /// Returns true if given patchcode is found in system otherwise it returns false.
        /// </summary>
        /// <param name="patchCode"></param>
        /// <returns></returns>
        public static bool IsPatchInstalled(string patchCode, string[] targetProdCodes)
        {
            bool found = false;
            foreach (string targetProdCode in targetProdCodes)
            {
                // Exclude the patch verification for those products which are not installed
                if (!MsiUtils.IsProductInstalled(targetProdCode))
                    continue;

                foreach (PatchInstallation patch in PatchInstallation.GetPatches(patchCode, targetProdCode, null, UserContexts.All, PatchStates.All))
                {
                    if (patch.PatchCode == patchCode)
                        found = true;
                }

                // Return after first occurance of not found. That is it will return false if patch is not applied to any of the target products.
                if (!found)
                    return found;
            }
            return found;
        }

        public static bool IsPatchInstalled(string mspPath)
        {
            string[] targetProductCode = GetTargetProductCodes(mspPath);
            string patchCode = GetPatchCode(mspPath);

            return IsPatchInstalled(patchCode, targetProductCode);
        }

        /// <summary>
        /// Returns true if given patch is uninstalled from all the target products
        /// </summary>
        /// <param name="patchCode"></param>
        /// <param name="targetProdCodes"></param>
        /// <returns></returns>
        public static bool IsPatchUninstalled(string patchCode, string[] targetProdCodes)
        {
            bool found = true;
            foreach (string targetProdCode in targetProdCodes)
            {
                // Exclude the patch verification for those products which are not installed
                if (!MsiUtils.IsProductInstalled(targetProdCode))
                    continue;

                foreach (PatchInstallation patch in PatchInstallation.GetPatches(patchCode, targetProdCode, null, UserContexts.All, PatchStates.All))
                {
                    if (patch.PatchCode == patchCode)
                        found = false;
                }

                // Return after first occurance of found. That is it will return false if patch is applied to any of the target products.
                if (!found)
                    return found;
            }
            return found;
        }

        /// <summary>
        /// Removes the patch from all target products
        /// </summary>
        /// <param name="patchCode"></param>
        /// <param name="targetProductCodes"></param>
        public static void RemovePatchFromProducts(string patchCode, string[] targetProductCodes)
        {
            string[] patchCodes = { patchCode };
            foreach (string targetProductCode in targetProductCodes)
            {
                try
                {
                    Installer.RemovePatches(patchCodes, targetProductCode, null);
                }
                catch
                {
                    // don't crash if we try to remove a patch from a product that has already been removed
                }
            }
        }

        public static bool IsFeatureAdvertised(string msiPath, string featureName)
        {
            string productCode = GetMSIProductCode(msiPath);

            FeatureInstallation feature = new FeatureInstallation(featureName, productCode);
            return feature.State == InstallState.Advertised;
        }
    }
}
