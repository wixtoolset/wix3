using System;
using System.Collections;
using Microsoft.Deployment.WindowsInstaller;


namespace Microsoft.Deployment.Samples.Inventory
{
	public class MsiUtils
	{
		private static Hashtable productCodesToNames = new Hashtable();
		private static Hashtable productNamesToCodes = new Hashtable();

		public static string GetProductName(string productCode)
		{
			string productName = (string) productCodesToNames[productCode];
			if(productName == null)
			{
                productName = new ProductInstallation(productCode).ProductName;
				productName = productName.Replace('\\', ' ');
				if(productNamesToCodes.Contains(productName))
				{
					string modifiedProductName = null;
					for(int i = 2; i < Int32.MaxValue; i++)
					{
						modifiedProductName = productName + " [" + i + "]";
						if(!productNamesToCodes.Contains(modifiedProductName)) break;
					}
					productName = modifiedProductName;
				}
				productCodesToNames[productCode] = productName;
				productNamesToCodes[productName] = productCode;
			}
			return productName;
		}

		// Assumes GetProductName() has already been called for this product.
		public static string GetProductCode(string productName)
		{
			return (string) productNamesToCodes[productName];
		}

		private MsiUtils() { }
	}
}
