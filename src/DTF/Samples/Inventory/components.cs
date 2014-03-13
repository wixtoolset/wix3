using System;
using System.IO;
using System.Data;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Deployment.WindowsInstaller;
using View = Microsoft.Deployment.WindowsInstaller.View;

namespace Microsoft.Deployment.Samples.Inventory
{
	/// <summary>
	/// Provides inventory data about components of products installed on the system.
	/// </summary>
	public class ComponentsInventory : IInventoryDataProvider
	{
        private static object syncRoot = new object();

		public ComponentsInventory()
		{
		}

		public string Description
		{
			get { return "Components of installed products"; }
		}

		private Hashtable componentProductsMap;

		public string[] GetNodes(InventoryDataLoadStatusCallback statusCallback)
		{
			ArrayList nodes = new ArrayList();
			componentProductsMap = new Hashtable();
			foreach(ProductInstallation product in ProductInstallation.AllProducts)
			{
                string productName = MsiUtils.GetProductName(product.ProductCode);
                statusCallback(nodes.Count, String.Format(@"Products\{0}", productName));

				try
				{
					IntPtr hWnd = IntPtr.Zero;
					Installer.SetInternalUI(InstallUIOptions.Silent, ref hWnd);
					lock(syncRoot)  // Only one Installer session can be active at a time
					{
                        using (Session session = Installer.OpenProduct(product.ProductCode))
						{
                            statusCallback(nodes.Count, String.Format(@"Products\{0}\Features", productName));
                            IList<string> features = session.Database.ExecuteStringQuery("SELECT `Feature` FROM `Feature`");
                            string[] featuresArray = new string[features.Count];
                            features.CopyTo(featuresArray, 0);
                            Array.Sort(featuresArray, 0, featuresArray.Length, StringComparer.OrdinalIgnoreCase);
                            foreach (string feature in featuresArray)
							{
								nodes.Add(String.Format(@"Products\{0}\Features\{1}", productName, feature));
							}
                            statusCallback(nodes.Count, String.Format(@"Products\{0}\Components", productName));
							nodes.Add(String.Format(@"Products\{0}\Components", productName));
                            IList<string> components = session.Database.ExecuteStringQuery("SELECT `ComponentId` FROM `Component`");
							for (int i = 0; i < components.Count; i++)
							{
								string component = components[i];
								if (component.Length > 0)
								{
									nodes.Add(String.Format(@"Products\{0}\Components\{1}", productName, component));
									ArrayList sharingProducts = (ArrayList) componentProductsMap[component];
									if (sharingProducts == null)
									{
										sharingProducts = new ArrayList();
										componentProductsMap[component] = sharingProducts;
									}
                                    sharingProducts.Add(product.ProductCode);
								}
								if (i % 100 == 0) statusCallback(nodes.Count, null);
							}
							nodes.Add(String.Format(@"Products\{0}\Files", productName));
							nodes.Add(String.Format(@"Products\{0}\Registry", productName));
							statusCallback(nodes.Count, String.Empty);
						}
					}
				}
				catch(InstallerException) { }
			}
            statusCallback(nodes.Count, @"Products\...\Components\...\Sharing");
            foreach (DictionaryEntry componentProducts in componentProductsMap)
			{
				string component = (string) componentProducts.Key;
				ArrayList products = (ArrayList) componentProducts.Value;
				if(products.Count > 1)
				{
					foreach(string productCode in products)
					{
						nodes.Add(String.Format(@"Products\{0}\Components\{1}\Sharing", MsiUtils.GetProductName(productCode), component));
					}
				}
			}
			statusCallback(nodes.Count, String.Empty);
			return (string[]) nodes.ToArray(typeof(string));
		}

		public bool IsNodeSearchable(string searchRoot, string searchNode)
		{
			string[] rootPath = searchRoot.Split('\\');
			string[] nodePath = searchNode.Split('\\');
			if(rootPath.Length < 3 && nodePath.Length >= 3 && nodePath[0] == "Products" && nodePath[2] == "Components")
			{
				// When searching an entire product, don't search the "Components" subtree --
				// it just has duplicate data from the Files and Registry table.  And if you
				// really want to know about the component, it's only a click away from
				// those other tables.
				return false;
			}
			return true;
		}

		public DataView GetData(string nodePath)
		{
			string[] path = nodePath.Split('\\');

			if(path.Length == 4 && path[0] == "Products" && path[2] == "Features")
			{
				return GetFeatureComponentsData(MsiUtils.GetProductCode(path[1]), path[3]);
			}
			else if(path.Length == 3 && path[0] == "Products" && path[2] == "Components")
			{
				return GetProductComponentsData(MsiUtils.GetProductCode(path[1]));
			}
			else if(path.Length == 4 && path[0] == "Products" && path[2] == "Components")
			{
				return GetComponentData(MsiUtils.GetProductCode(path[1]), path[3]);
			}
			else if(path.Length == 5 && path[0] == "Products" && path[2] == "Components" && path[4] == "Sharing")
			{
				return GetComponentProductsData(path[3]);
			}
			else if(path.Length == 3 && path[0] == "Products" && path[2] == "Files")
			{
				return GetProductFilesData(MsiUtils.GetProductCode(path[1]));
			}
			else if(path.Length == 3 && path[0] == "Products" && path[2] == "Registry")
			{
				return GetProductRegistryData(MsiUtils.GetProductCode(path[1]));
			}
			return null;
		}

		public DataView GetComponentData(string productCode, string componentCode)
		{
			DataTable table = new DataTable("ProductComponentItems");
			table.Locale = CultureInfo.InvariantCulture;
			table.Columns.Add("ProductComponentItemsIsKey", typeof(bool));
			table.Columns.Add("ProductComponentItemsKey", typeof(string));
			table.Columns.Add("ProductComponentItemsPath", typeof(string));
			table.Columns.Add("ProductComponentItemsExists", typeof(bool));
			table.Columns.Add("ProductComponentItemsDbVersion", typeof(string));
			table.Columns.Add("ProductComponentItemsInstalledVersion", typeof(string));
			table.Columns.Add("ProductComponentItemsInstalledMatch", typeof(bool));
			try
			{
				IntPtr hWnd = IntPtr.Zero;
                Installer.SetInternalUI(InstallUIOptions.Silent, ref hWnd);
				lock(syncRoot)  // Only one Installer session can be active at a time
				{
					using(Session session = Installer.OpenProduct(productCode))
					{
						session.DoAction("CostInitialize");
						session.DoAction("FileCost");
						session.DoAction("CostFinalize");

						foreach(object[] row in this.GetComponentFilesRows(productCode, componentCode, session, false))
						{
							table.Rows.Add(row);
						}
						foreach(object[] row in this.GetComponentRegistryRows(productCode, componentCode, session, false))
						{
							table.Rows.Add(row);
						}
					}
				}
				return new DataView(table, "", "ProductComponentItemsPath ASC", DataViewRowState.CurrentRows);
			}
			catch(InstallerException) { }
			return null;
		}

		private object[][] GetComponentFilesRows(string productCode, string componentCode, Session session, bool includeComponent)
		{
			ArrayList rows = new ArrayList();
			string componentPath = new ComponentInstallation(componentCode, productCode).Path;

			string componentKey = (string) session.Database.ExecuteScalar(
				"SELECT `Component` FROM `Component` WHERE `ComponentId` = '{0}'", componentCode);
			if(componentKey == null) return null;
			int attributes = Convert.ToInt32(session.Database.ExecuteScalar(
				"SELECT `Attributes` FROM `Component` WHERE `Component` = '{0}'", componentKey));
			bool registryKeyPath = (attributes & (int) ComponentAttributes.RegistryKeyPath) != 0;
			if(!registryKeyPath && componentPath.Length > 0) componentPath = Path.GetDirectoryName(componentPath);
			string keyPath = (string) session.Database.ExecuteScalar(
				"SELECT `KeyPath` FROM `Component` WHERE `Component` = '{0}'", componentKey);

			using (View	view = session.Database.OpenView("SELECT `File`, `FileName`, `Version`, `Language`, " +
			    "`Attributes` FROM `File` WHERE `Component_` = '{0}'", componentKey))
            {
				view.Execute();
				
                foreach (Record rec in view) using (rec)
				{
					string fileKey = (string) rec["File"];
					bool isKey = !registryKeyPath && keyPath == fileKey;
							
					string dbVersion = (string) rec["Version"];
					bool versionedFile = dbVersion.Length != 0;
					if(versionedFile)
					{
						string language = (string) rec["Language"];
						if(language.Length > 0)
						{
							dbVersion = dbVersion + " (" + language + ")";
						}
					}
					else if(session.Database.Tables.Contains("MsiFileHash"))
					{
						IList<int> hash = session.Database.ExecuteIntegerQuery("SELECT `HashPart1`, `HashPart2`, " + 
							"`HashPart3`, `HashPart4` FROM `MsiFileHash` WHERE `File_` = '{0}'", fileKey);
						if(hash != null && hash.Count == 4)
						{
							dbVersion = this.GetFileHashString(hash);
						}
					}

					string filePath = GetLongFileName((string) rec["FileName"]);
					bool exists = false;
					bool installedMatch = false;
					string installedVersion = "";
					if(!registryKeyPath && componentPath.Length > 0)
					{
						filePath = Path.Combine(componentPath, filePath);

						if(File.Exists(filePath))
						{
							exists = true;
							if(versionedFile)
							{
								installedVersion = Installer.GetFileVersion(filePath);
								string language = Installer.GetFileLanguage(filePath);
								if(language.Length > 0)
								{
									installedVersion = installedVersion + " (" + language + ")";
								}
							}
							else
							{
                                int[] hash = new int[4];
                                Installer.GetFileHash(filePath, hash);
    							installedVersion = this.GetFileHashString(hash);
							}
							installedMatch = installedVersion == dbVersion;
						}
					}

					object[] row;
					if(includeComponent) row = new object[] { isKey, fileKey, filePath, exists, dbVersion, installedVersion, installedMatch, componentCode };
					else row = new object[] { isKey, fileKey, filePath, exists, dbVersion, installedVersion, installedMatch };
					rows.Add(row);
				}
			}

            return (object[][]) rows.ToArray(typeof(object[]));
		}

		private string GetLongFileName(string fileName)
		{
			string[] fileNames = fileName.Split('|');
			return fileNames.Length == 1? fileNames[0] : fileNames[1];
		}

		private string GetFileHashString(IList<int> hash)
		{
			return String.Format("{0:X8}{1:X8}{2:X8}{3:X8}", (uint) hash[0], (uint) hash[1], (uint) hash[2], (uint) hash[3]);
		}

		private object[][] GetComponentRegistryRows(string productCode, string componentCode, Session session, bool includeComponent)
		{
			ArrayList rows = new ArrayList();
            string componentPath = new ComponentInstallation(componentCode, productCode).Path;

			string componentKey = (string) session.Database.ExecuteScalar(
				"SELECT `Component` FROM `Component` WHERE `ComponentId` = '{0}'", componentCode);
			if(componentKey == null) return null;
			int attributes = Convert.ToInt32(session.Database.ExecuteScalar(
				"SELECT `Attributes` FROM `Component` WHERE `Component` = '{0}'", componentKey));
			bool registryKeyPath = (attributes & (int) ComponentAttributes.RegistryKeyPath) != 0;
			if(!registryKeyPath && componentPath.Length > 0) componentPath = Path.GetDirectoryName(componentPath);
			string keyPath = (string) session.Database.ExecuteScalar(
				"SELECT `KeyPath` FROM `Component` WHERE `Component` = '{0}'", componentKey);

			using (View view = session.Database.OpenView("SELECT `Registry`, `Root`, `Key`, `Name`, " +
			    "`Value` FROM `Registry` WHERE `Component_` = '{0}'", componentKey))
            {
				view.Execute();

				foreach (Record rec in view) using (rec)
				{
					string regName = (string) rec["Name"];
					if(regName == "-") continue;  // Don't list deleted keys

					string regTableKey = (string) rec["Registry"];
					bool isKey = registryKeyPath && keyPath == regTableKey;
					string regPath = this.GetRegistryPath(session, (RegistryRoot) Convert.ToInt32(rec["Root"]),
						(string) rec["Key"], (string) rec["Name"]);

					string dbValue;
					using(Record formatRec = new Record(0))
					{
						formatRec[0] = rec["Value"];
						dbValue = session.FormatRecord(formatRec);
					}

					string installedValue = this.GetRegistryValue(regPath);
					bool exists = installedValue != null;
					if(!exists) installedValue = "";
					bool match = installedValue == dbValue;

					object[] row;
					if(includeComponent) row = new object[] { isKey, regTableKey, regPath, exists, dbValue, installedValue, match, componentCode };
					else row = new object[] { isKey, regTableKey, regPath, exists, dbValue, installedValue, match };
					rows.Add(row);
				}
			}

            return (object[][]) rows.ToArray(typeof(object[]));
		}

		private string GetRegistryPath(Session session, RegistryRoot root, string key, string name)
		{
			bool allUsers = session.EvaluateCondition("ALLUSERS = 1", true);
			string rootName = "????";
			switch(root)
			{
				case RegistryRoot.LocalMachine : rootName = "HKLM"; break;
				case RegistryRoot.CurrentUser  : rootName = "HKCU"; break;
				case RegistryRoot.Users        : rootName = "HKU"; break;
				case RegistryRoot.UserOrMachine: rootName = (allUsers ? "HKLM" : "HKCU"); break;
				case RegistryRoot.ClassesRoot  : rootName = (allUsers ? @"HKLM\Software\Classes" : @"HKCU\Software\Classes"); break;
				// TODO: Technically, RegistryRoot.ClassesRoot should be under HKLM on NT4.
			}
			if(name.Length == 0) name = "(Default)";
			if(name == "+" || name == "*") name = "";
			else name = " : " + name;
			using(Record formatRec = new Record(0))
			{
				formatRec[0] = String.Format(@"{0}\{1}{2}", rootName, key, name);
				return session.FormatRecord(formatRec);
			}
		}

		private string GetRegistryValue(string regPath)
		{
			string valueName = null;
			int iColon = regPath.IndexOf(" : ", StringComparison.Ordinal) + 1;
			if(iColon > 0)
			{
				valueName = regPath.Substring(iColon + 2);
				regPath = regPath.Substring(0, iColon - 1);
			}
			if(valueName == "(Default)") valueName = "";

			RegistryKey root;
			if(regPath.StartsWith(@"HKLM\", StringComparison.Ordinal))
			{
				root = Registry.LocalMachine;
				regPath = regPath.Substring(5);
			}
			else if(regPath.StartsWith(@"HKCU\", StringComparison.Ordinal))
			{
				root = Registry.CurrentUser;
				regPath = regPath.Substring(5);
			}
			else if(regPath.StartsWith(@"HKU\", StringComparison.Ordinal))
			{
				root = Registry.Users;
				regPath = regPath.Substring(4);
			}
			else return null;

			using(RegistryKey regKey = root.OpenSubKey(regPath))
			{
				if(regKey != null)
				{
					if(valueName == null)
					{
						// Just checking for the existence of the key.
						return "";
					}
					object value = regKey.GetValue(valueName);
					if(value is string[])
					{
						value = String.Join("[~]", (string[]) value);
					}
					else if(value is int)
					{
						value = "#" + value.ToString();
					}
					else if(value is byte[])
					{
						byte[] valueBytes = (byte[]) value;
						StringBuilder byteString = new StringBuilder("#x");
						for(int i = 0; i < valueBytes.Length; i++)
						{
							byteString.Append(valueBytes[i].ToString("x2"));
						}
						value = byteString.ToString();
					}
					return (value != null ? value.ToString() : null);
				}
			}
			return null;
		}

		public DataView GetProductFilesData(string productCode)
		{
			DataTable table = new DataTable("ProductFiles");
			table.Locale = CultureInfo.InvariantCulture;
			table.Columns.Add("ProductFilesIsKey", typeof(bool));
			table.Columns.Add("ProductFilesKey", typeof(string));
			table.Columns.Add("ProductFilesPath", typeof(string));
			table.Columns.Add("ProductFilesExists", typeof(bool));
			table.Columns.Add("ProductFilesDbVersion", typeof(string));
			table.Columns.Add("ProductFilesInstalledVersion", typeof(string));
			table.Columns.Add("ProductFilesInstalledMatch", typeof(bool));
			table.Columns.Add("ProductFilesComponentID", typeof(string));
			try
			{
				IntPtr hWnd = IntPtr.Zero;
                Installer.SetInternalUI(InstallUIOptions.Silent, ref hWnd);
				lock(syncRoot)  // Only one Installer session can be active at a time
				{
					using(Session session = Installer.OpenProduct(productCode))
					{
						session.DoAction("CostInitialize");
						session.DoAction("FileCost");
						session.DoAction("CostFinalize");

						foreach(string componentCode in session.Database.ExecuteStringQuery("SELECT `ComponentId` FROM `Component`"))
						{
							foreach(object[] row in this.GetComponentFilesRows(productCode, componentCode, session, true))
							{
								table.Rows.Add(row);
							}
						}
					}
				}
				return new DataView(table, "", "ProductFilesPath ASC", DataViewRowState.CurrentRows);
			}
			catch(InstallerException) { }
			return null;
		}

		public DataView GetProductRegistryData(string productCode)
		{
			DataTable table = new DataTable("ProductRegistry");
			table.Locale = CultureInfo.InvariantCulture;
			table.Columns.Add("ProductRegistryIsKey", typeof(bool));
			table.Columns.Add("ProductRegistryKey", typeof(string));
			table.Columns.Add("ProductRegistryPath", typeof(string));
			table.Columns.Add("ProductRegistryExists", typeof(bool));
			table.Columns.Add("ProductRegistryDbVersion", typeof(string));
			table.Columns.Add("ProductRegistryInstalledVersion", typeof(string));
			table.Columns.Add("ProductRegistryInstalledMatch", typeof(bool));
			table.Columns.Add("ProductRegistryComponentID", typeof(string));
			try
			{
				IntPtr hWnd = IntPtr.Zero;
                Installer.SetInternalUI(InstallUIOptions.Silent, ref hWnd);
				lock(syncRoot)  // Only one Installer session can be active at a time
				{
					using(Session session = Installer.OpenProduct(productCode))
					{
						session.DoAction("CostInitialize");
						session.DoAction("FileCost");
						session.DoAction("CostFinalize");

						foreach(string componentCode in session.Database.ExecuteStringQuery("SELECT `ComponentId` FROM `Component`"))
						{
							foreach(object[] row in this.GetComponentRegistryRows(productCode, componentCode, session, true))
							{
								table.Rows.Add(row);
							}
						}
					}
				}
				return new DataView(table, "", "ProductRegistryPath ASC", DataViewRowState.CurrentRows);
			}
			catch(InstallerException) { }
			return null;
		}

		public DataView GetComponentProductsData(string componentCode)
		{
			DataTable table = new DataTable("ComponentProducts");
			table.Locale = CultureInfo.InvariantCulture;
			table.Columns.Add("ComponentProductsProductName", typeof(string));
			table.Columns.Add("ComponentProductsProductCode", typeof(string));
			table.Columns.Add("ComponentProductsComponentPath", typeof(string));

			if(this.componentProductsMap != null)
			{
				ArrayList componentProducts = (ArrayList) this.componentProductsMap[componentCode];
				foreach(string productCode in componentProducts)
				{
					string productName = MsiUtils.GetProductName(productCode);
                    string componentPath = new ComponentInstallation(componentCode, productCode).Path;
					table.Rows.Add(new object[] { productName, productCode, componentPath });
				}
				return new DataView(table, "", "ComponentProductsProductName ASC", DataViewRowState.CurrentRows);
			}
			return null;
		}

		public DataView GetProductComponentsData(string productCode)
		{
			DataTable table = new DataTable("ProductComponents");
			table.Locale = CultureInfo.InvariantCulture;
			table.Columns.Add("ProductComponentsComponentName", typeof(string));
			table.Columns.Add("ProductComponentsComponentID", typeof(string));
			table.Columns.Add("ProductComponentsInstallState", typeof(string));

			try
			{
				IntPtr hWnd = IntPtr.Zero;
                Installer.SetInternalUI(InstallUIOptions.Silent, ref hWnd);
				lock(syncRoot)  // Only one Installer session can be active at a time
				{
					using(Session session = Installer.OpenProduct(productCode))
					{
						session.DoAction("CostInitialize");
						session.DoAction("FileCost");
						session.DoAction("CostFinalize");

						IList<string> componentsAndIds = session.Database.ExecuteStringQuery(
							"SELECT `Component`, `ComponentId` FROM `Component`");

                        for (int i = 0; i < componentsAndIds.Count; i += 2)
						{
							if(componentsAndIds[i+1] == "Temporary Id") continue;
							InstallState compState = session.Components[componentsAndIds[i]].CurrentState;
							table.Rows.Add(new object[] { componentsAndIds[i], componentsAndIds[i+1],
															(compState == InstallState.Advertised ? "Advertised" : compState.ToString())});
						}
					}
				}
				return new DataView(table, "", "ProductComponentsComponentName ASC", DataViewRowState.CurrentRows);
			}
			catch(InstallerException) { }
			return null;
		}

		public DataView GetFeatureComponentsData(string productCode, string feature)
		{
			DataTable table = new DataTable("ProductFeatureComponents");
			table.Locale = CultureInfo.InvariantCulture;
			table.Columns.Add("ProductFeatureComponentsComponentName", typeof(string));
			table.Columns.Add("ProductFeatureComponentsComponentID", typeof(string));

			try
			{
				IntPtr hWnd = IntPtr.Zero;
                Installer.SetInternalUI(InstallUIOptions.Silent, ref hWnd);
				lock(syncRoot)  // Only one Installer session can be active at a time
				{
					using(Session session = Installer.OpenProduct(productCode))
					{
                        IList<string> componentsAndIds = session.Database.ExecuteStringQuery(
                            "SELECT `FeatureComponents`.`Component_`, " +
							"`Component`.`ComponentId` FROM `FeatureComponents`, `Component` " +
							"WHERE `FeatureComponents`.`Component_` = `Component`.`Component` " +
							"AND `FeatureComponents`.`Feature_` = '{0}'", feature);
						for (int i = 0; i < componentsAndIds.Count; i += 2)
						{
							table.Rows.Add(new object[] { componentsAndIds[i], componentsAndIds[i+1] });
						}
					}
				}
				return new DataView(table, "", "ProductFeatureComponentsComponentName ASC", DataViewRowState.CurrentRows);
			}
			catch(InstallerException) { }
			return null;
		}

		public string GetLink(string nodePath, DataRow row)
		{
			string[] path = nodePath.Split('\\');

			if(path.Length == 3 && path[0] == "Products" && path[2] == "Components")
			{
				string component = (string) row["ProductComponentsComponentID"];
				return String.Format(@"Products\{0}\Components\{1}", path[1], component);
			}
			else if(path.Length == 4 && path[0] == "Products" && path[2] == "Features")
			{
				string component = (string) row["ProductFeatureComponentsComponentID"];
				return String.Format(@"Products\{0}\Components\{1}", path[1], component);
			}
			else if(path.Length == 3 && path[0] == "Products" && path[2] == "Files")
			{
				string component = (string) row["ProductFilesComponentID"];
				return String.Format(@"Products\{0}\Components\{1}", path[1], component);
			}
			else if(path.Length == 3 && path[0] == "Products" && path[2] == "Registry")
			{
				string component = (string) row["ProductRegistryComponentID"];
				return String.Format(@"Products\{0}\Components\{1}", path[1], component);
			}
			else if(path.Length == 5 && path[0] == "Products" && path[2] == "Components" && path[4] == "Sharing")
			{
				string product = (string) row["ComponentProductsProductCode"];
				return String.Format(@"Products\{0}\Components\{1}", MsiUtils.GetProductName(product), path[3]);
			}
			return null;
		}
	}
}
