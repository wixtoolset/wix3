using System;
using System.IO;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Deployment.WindowsInstaller;

namespace Microsoft.Deployment.Samples.Inventory
{
	/// <summary>
	/// Provides inventory data about features of products installed on the system.
	/// </summary>
	public class FeaturesInventory : IInventoryDataProvider
	{
        private static object syncRoot = new object();

		public FeaturesInventory()
		{
		}

		public string Description
		{
			get { return "Features of installed products"; }
		}

		public string[] GetNodes(InventoryDataLoadStatusCallback statusCallback)
		{
            statusCallback(0, @"Products\...\Features");
            ArrayList nodes = new ArrayList();
			foreach (ProductInstallation product in ProductInstallation.AllProducts)
			{
                nodes.Add(String.Format(@"Products\{0}\Features", MsiUtils.GetProductName(product.ProductCode)));
			}
			statusCallback(nodes.Count, String.Empty);
			return (string[]) nodes.ToArray(typeof(string));
		}

		public bool IsNodeSearchable(string searchRoot, string searchNode)
		{
			return true;
		}

		public DataView GetData(string nodePath)
		{
			string[] path = nodePath.Split('\\');

			if(path.Length == 3 && path[0] == "Products" && path[2] == "Features")
			{
				return GetProductFeaturesData(MsiUtils.GetProductCode(path[1]));
			}
			return null;
		}

		public DataView GetProductFeaturesData(string productCode)
		{
			DataTable table = new DataTable("ProductFeatures");
			table.Locale = CultureInfo.InvariantCulture;
			table.Columns.Add("ProductFeaturesFeatureTitle", typeof(string));
			table.Columns.Add("ProductFeaturesFeatureName", typeof(string));
			table.Columns.Add("ProductFeaturesInstallState", typeof(string));

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

						IList<string> featuresAndTitles = session.Database.ExecuteStringQuery(
							"SELECT `Title`, `Feature` FROM `Feature`");

						for(int i = 0; i < featuresAndTitles.Count; i += 2)
						{
                            InstallState featureState = session.Features[featuresAndTitles[i + 1]].CurrentState;
							table.Rows.Add(new object[] { featuresAndTitles[i], featuresAndTitles[i+1],
															(featureState == InstallState.Advertised ? "Advertised" : featureState.ToString()) });
						}
					}
				}
				return new DataView(table, "", "ProductFeaturesFeatureTitle ASC", DataViewRowState.CurrentRows);
			}
			catch(InstallerException) { }
			catch(IOException) { }
			return null;
		}

		public string GetLink(string nodePath, DataRow row)
		{
			string[] path = nodePath.Split('\\');

			if(path.Length == 3 && path[0] == "Products" && path[2] == "Features")
			{
				return String.Format(@"Products\{0}\Features\{1}", path[1], row["ProductFeaturesFeatureName"]);
			}
			return null;
		}
	}
}
