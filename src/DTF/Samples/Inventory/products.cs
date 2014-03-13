using System;
using System.Data;
using System.Globalization;
using System.Collections;
using System.Windows.Forms;
using Microsoft.Deployment.WindowsInstaller;

namespace Microsoft.Deployment.Samples.Inventory
{
	/// <summary>
	/// Provides inventory data about products installed or advertised on the system.
	/// </summary>
	public class ProductsInventory : IInventoryDataProvider
	{
		public ProductsInventory()
		{
		}

		public string Description
		{
			get { return "Installed products"; }
		}

		public string[] GetNodes(InventoryDataLoadStatusCallback statusCallback)
		{
            statusCallback(0, "Products");
            ArrayList nodes = new ArrayList();
			nodes.Add("Products");
			foreach(ProductInstallation product in ProductInstallation.AllProducts)
			{
                nodes.Add("Products\\" + MsiUtils.GetProductName(product.ProductCode));
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

			if(path.Length == 1 && path[0] == "Products")
			{
				return this.GetAllProductsData();
			}
			else if(path.Length == 2 && path[0] == "Products")
			{
				return this.GetProductData(MsiUtils.GetProductCode(path[1]));
			}
			return null;
		}

		private DataView GetAllProductsData()
		{
			DataTable table = new DataTable("Products");
			table.Locale = CultureInfo.InvariantCulture;
			table.Columns.Add("ProductsProductName", typeof(string));
			table.Columns.Add("ProductsProductCode", typeof(string));

            foreach (ProductInstallation product in ProductInstallation.AllProducts)
            {
                string productName = MsiUtils.GetProductName(product.ProductCode);
                table.Rows.Add(new object[] { productName, product.ProductCode });
			}
			return new DataView(table, "", "ProductsProductName ASC", DataViewRowState.CurrentRows);
		}

		private DataView GetProductData(string productCode)
		{
			DataTable table = new DataTable("ProductProperties");
			table.Locale = CultureInfo.InvariantCulture;
			table.Columns.Add("ProductPropertiesProperty", typeof(string));
			table.Columns.Add("ProductPropertiesValue", typeof(string));

			// Add a fake "ProductCode" install property, just for display convenience.
			table.Rows.Add(new object[] { "ProductCode", productCode });

            ProductInstallation product = new ProductInstallation(productCode);

			foreach(string property in new string[]
			{
				"AssignmentType",
				"DiskPrompt",
				"HelpLink",
				"HelpTelephone",
				"InstalledProductName",
				"InstallDate",
				"InstallLocation",
				"InstallSource",
				"Language",
				"LastUsedSource",
				"LastUsedType",
				"LocalPackage",
				"MediaPackagePath",
				"PackageCode",
				"PackageName",
				"ProductIcon",
				"ProductID",
				"ProductName",
				"Publisher",
				"RegCompany",
				"RegOwner",
				"State",
				"transforms",
				"Uninstallable",
				"UrlInfoAbout",
				"UrlUpdateInfo",
				"Version",
				"VersionMinor",
				"VersionMajor",
				"VersionString"
			})
			{
				try
				{
                    string value = product[property];
					table.Rows.Add(new object[] { property,  (value != null ? value : "") });
				}
				catch(InstallerException iex)
				{
					table.Rows.Add(new object[] { property, iex.Message });
				}
				catch(ArgumentException) { }
			}
			return new DataView(table, "", "ProductPropertiesProperty ASC", DataViewRowState.CurrentRows);
		}

		public string GetLink(string nodePath, DataRow row)
		{
			string[] path = nodePath.Split('\\');

			if(path.Length == 1 && path[0] == "Products")
			{
				return String.Format(@"Products\{0}", MsiUtils.GetProductName((string) row["ProductsProductCode"]));
			}
			return null;
		}
	}
}
