using System;
using System.IO;
using System.Security;
using System.Data;
using System.Collections;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Deployment.WindowsInstaller;

namespace Microsoft.Deployment.Samples.Inventory
{
	/// <summary>
	/// Provides inventory data about patches installed on the system.
	/// </summary>
	public class PatchesInventory : IInventoryDataProvider
	{
		public PatchesInventory()
		{
		}

		public string Description
		{
			get { return "Installed patches"; }
		}

		public string[] GetNodes(InventoryDataLoadStatusCallback statusCallback)
		{
			ArrayList nodes = new ArrayList();
            statusCallback(nodes.Count, @"Products\...\Patches");
            foreach (ProductInstallation product in ProductInstallation.AllProducts)
			{
				string productName = MsiUtils.GetProductName(product.ProductCode);

                bool addedRoot = false;
				foreach (PatchInstallation productPatch in PatchInstallation.GetPatches(null, product.ProductCode, null, UserContexts.All, PatchStates.Applied))
				{
                    if (!addedRoot) nodes.Add(String.Format(@"Products\{0}\Patches", productName));
    				nodes.Add(String.Format(@"Products\{0}\Patches\{1}", productName, productPatch.PatchCode));
				}
			}
			
            statusCallback(nodes.Count, "Patches");

            string[] allPatches = GetAllPatchesList();
			if(allPatches.Length > 0)
			{
				nodes.Add("Patches");
				foreach(string patchCode in allPatches)
				{
					nodes.Add(String.Format(@"Patches\{0}", patchCode));
					nodes.Add(String.Format(@"Patches\{0}\Patched Products", patchCode));
				}
				statusCallback(nodes.Count, String.Empty);
			}
			return (string[]) nodes.ToArray(typeof(string));
		}

		public bool IsNodeSearchable(string searchRoot, string searchNode)
		{
			return true;
		}

		public DataView GetData(string nodePath)
		{
			string[] path = nodePath.Split('\\');

			if(path.Length == 3 && path[0] == "Products" && path[2] == "Patches")
			{
				return this.GetProductPatchData(path[1]);
			}
			else if(path.Length == 4 && path[0] == "Products" && path[2] == "Patches")
			{
				return this.GetPatchData(path[3]);
			}
			else if(path.Length == 1 && path[0] == "Patches")
			{
				return this.GetAllPatchesData();
			}
			else if(path.Length == 2 && path[0] == "Patches")
			{
				return this.GetPatchData(path[1]);
			}
			else if(path.Length == 3 && path[0] == "Patches" && path[2] == "Patched Products")
			{
				return this.GetPatchTargetData(path[1]);
			}
			return null;
		}

		private string[] GetAllPatchesList()
		{
			ArrayList patchList = new ArrayList();
			foreach(PatchInstallation patch in PatchInstallation.AllPatches)
			{
				if(!patchList.Contains(patch.PatchCode))
				{
					patchList.Add(patch.PatchCode);
				}
			}
			string[] patchArray = (string[]) patchList.ToArray(typeof(string));
			Array.Sort(patchArray, 0, patchArray.Length, StringComparer.Ordinal);
			return patchArray;
		}

		private DataView GetAllPatchesData()
		{
			DataTable table = new DataTable("Patches");
			table.Locale = CultureInfo.InvariantCulture;
			table.Columns.Add("PatchesPatchCode", typeof(string));

			foreach(string patchCode in GetAllPatchesList())
			{
				table.Rows.Add(new object[] { patchCode });
			}
			return new DataView(table, "", "PatchesPatchCode ASC", DataViewRowState.CurrentRows);
		}

		private DataView GetProductPatchData(string productCode)
		{
			DataTable table = new DataTable("ProductPatches");
			table.Locale = CultureInfo.InvariantCulture;
			table.Columns.Add("ProductPatchesPatchCode", typeof(string));

			foreach(PatchInstallation patch in PatchInstallation.GetPatches(null, productCode, null, UserContexts.All, PatchStates.Applied))
			{
				table.Rows.Add(new object[] { patch.PatchCode });
			}
			return new DataView(table, "", "ProductPatchesPatchCode ASC", DataViewRowState.CurrentRows);
		}

		private DataView GetPatchData(string patchCode)
		{
			DataTable table = new DataTable("PatchProperties");
			table.Locale = CultureInfo.InvariantCulture;
			table.Columns.Add("PatchPropertiesProperty", typeof(string));
			table.Columns.Add("PatchPropertiesValue", typeof(string));

			table.Rows.Add(new object[] { "PatchCode", patchCode });

            PatchInstallation patch = new PatchInstallation(patchCode, null);

			string localPackage = null;
			foreach(string property in new string[]
			{
				"InstallDate",
				"LocalPackage",
				"State",
				"Transforms",
				"Uninstallable",
			})
			{
				try
				{
					string value = patch[property];
					table.Rows.Add(new object[] { property,  (value != null ? value : "") });
					if(property == "LocalPackage") localPackage = value;
				}
				catch(InstallerException iex)
				{
					table.Rows.Add(new object[] { property, iex.Message });
				}
				catch(ArgumentException) { }
			}

			if(localPackage != null)
			{
				try
				{
					using(SummaryInfo patchSummaryInfo = new SummaryInfo(localPackage, false))
					{
						table.Rows.Add(new object[] { "Title", patchSummaryInfo.Title });
						table.Rows.Add(new object[] { "Subject", patchSummaryInfo.Subject });
						table.Rows.Add(new object[] { "Author", patchSummaryInfo.Author });
						table.Rows.Add(new object[] { "Comments", patchSummaryInfo.Comments });
						table.Rows.Add(new object[] { "TargetProductCodes", patchSummaryInfo.Template });
						string obsoletedPatchCodes = patchSummaryInfo.RevisionNumber.Substring(patchSummaryInfo.RevisionNumber.IndexOf('}') + 1);
						table.Rows.Add(new object[] { "ObsoletedPatchCodes", obsoletedPatchCodes });
						table.Rows.Add(new object[] { "TransformNames", patchSummaryInfo.LastSavedBy });
					}
				}
				catch(InstallerException) { }
				catch(IOException) { }
				catch(SecurityException) { }
			}
			return new DataView(table, "", "PatchPropertiesProperty ASC", DataViewRowState.CurrentRows);
		}

		private DataView GetPatchTargetData(string patchCode)
		{
			DataTable table = new DataTable("PatchTargets");
			table.Locale = CultureInfo.InvariantCulture;
			table.Columns.Add("PatchTargetsProductName", typeof(string));
			table.Columns.Add("PatchTargetsProductCode", typeof(string));

            foreach (PatchInstallation patch in PatchInstallation.GetPatches(patchCode, null, null, UserContexts.All, PatchStates.Applied))
			{
				if(patch.PatchCode == patchCode)
				{
					string productName = MsiUtils.GetProductName(patch.ProductCode);
					table.Rows.Add(new object[] { productName, patch.ProductCode });
				}
			}
			return new DataView(table, "", "PatchTargetsProductName ASC", DataViewRowState.CurrentRows);
		}

		public string GetLink(string nodePath, DataRow row)
		{
			string[] path = nodePath.Split('\\');

			if(path.Length == 3 && path[0] == "Products" && path[2] == "Patches")
			{
				return String.Format(@"Patches\{0}", row["ProductPatchesPatchCode"]);
			}
			else if(path.Length == 1 && path[0] == "Patches")
			{
				return String.Format(@"Patches\{0}", row["PatchesPatchCode"]);
			}
			else if(path.Length == 3 && path[0] == "Patches" && path[2] == "Patched Products")
			{
				return String.Format(@"Products\{0}", MsiUtils.GetProductCode((string) row["PatchTargetsProductCode"]));
			}
			return null;
		}
	}
}
