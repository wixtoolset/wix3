//-------------------------------------------------------------------------------------------------
// <copyright file="OfficeShimBuilder.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Setup.exe builder for Fabricator Extensions.
// </summary>
//-------------------------------------------------------------------------------------------------
namespace Microsoft.Tools.WindowsInstallerXml.Extensions.OfficeAddin
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Builder of the Office shim.
    /// </summary>
    internal sealed class OfficeShimBuilder
    {
        private FabricatorCore core;

        private string shimDllPath;
        private string applicationId;
        private string addinPath;
        private Guid addinClsid;
        private string addinProgid;

        /// <summary>
        /// Creates a new OfficeShimBuilder object.
        /// </summary>
        /// <param name="core">Core build object for message handling.</param>
        public OfficeShimBuilder(FabricatorCore core)
        {
            this.core = core;
            this.addinClsid = Guid.Empty;
            string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            this.shimDllPath = Path.Combine(assemblyPath, "ctoashim.dll");
        }

        /// <summary>
        /// Gets and sets the path to the Add-in that the shim points at.
        /// </summary>
        public string AddinPath
        {
            get { return this.addinPath; }
            set { this.addinPath = value; }
        }

        /// <summary>
        /// Gets and sets the applicaiton id for the Add-in.
        /// </summary>
        public string AddinId
        {
            get { return this.applicationId; }
            set { this.applicationId = value; }
        }

        /// <summary>
        /// Gets and sets the CLSID for the Add-in.
        /// </summary>
        public Guid AddinClsid
        {
            get { return this.addinClsid; }
            set { this.addinClsid = value; }
        }

        /// <summary>
        /// Gets and sets the ProgId for the Add-in.
        /// </summary>
        public string AddinProgId
        {
            get { return this.addinProgid; }
            set { this.addinProgid = value; }
        }

        /// <summary>
        /// Creates the shim.dll with information updated from the managed Office addin.
        /// </summary>
        /// <param name="outputFile">Output path for shim.dll</param>
        /// <returns>True if build succeeded, false if anything failed.</returns>
        public bool Build(string outputFile)
        {
            if (null == this.addinPath)
            {
                throw new ArgumentNullException("AddinPath");
            }

            if (null == this.applicationId)
            {
                throw new ArgumentNullException("AddinId");
            }

            if (null == outputFile)
            {
                throw new ArgumentNullException("outputFile");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            string clrVersion;
            string assemblyName;
            string className;
            this.GetAddinInformation(out clrVersion, out assemblyName, out className);

            int hr = NativeMethods.UpdateShim(this.shimDllPath, this.applicationId, clrVersion, assemblyName, className, outputFile);
            if (hr != 0)
            {
                throw new ApplicationException(String.Format("Failed create shim.dll to: {0} from: {1}", outputFile, this.shimDllPath));
            }

            return true;
        }

        /// <summary>
        /// Gets the CLSID and ProgId information out of the managed Add-in.
        /// </summary>
        /// <param name="clrVersion">Version of CLR add-in requires.</param>
        /// <param name="partialAssemblyName">Partial assembly name of addin.</param>
        /// <param name="className">Name of add-in class name.</param>
        private void GetAddinInformation(out string clrVersion, out string partialAssemblyName, out string className)
        {
            clrVersion = null;
            partialAssemblyName = null;
            className = null;

            Assembly addinAssembly = Assembly.LoadFile(this.addinPath);
            AssemblyName assemblyName = addinAssembly.GetName();
            string publicKeyToken = this.GetPublicKeyTokenString(assemblyName);

            // Find the Type that implements the connection interface and grab the rest
            // of the information off of it.
            foreach (Type type in addinAssembly.GetTypes())
            {
                foreach (Type implementedInterface in type.GetInterfaces())
                {
                    if ("Extensibility.IDTExtensibility2" == implementedInterface.FullName)
                    {
                        className = type.FullName;
                        foreach (object attrib in type.GetCustomAttributes(false))
                        {
                            if (attrib is GuidAttribute)
                            {
                                GuidAttribute guidAttribute = (GuidAttribute)attrib;
                                this.addinClsid = new Guid(guidAttribute.Value);
                            }
                            else if (attrib is ProgIdAttribute)
                            {
                                ProgIdAttribute progidAttribute = (ProgIdAttribute)attrib;
                                this.addinProgid = progidAttribute.Value;
                            }
                        }

                        break;
                    }
                }

                if (null != className)
                {
                    clrVersion = addinAssembly.ImageRuntimeVersion;
                    partialAssemblyName = String.Concat(assemblyName.Name, ",PublicKeyToken=", publicKeyToken);

                    break;
                }
            }
        }

        /// <summary>
        /// Gets the public key token from the assembly name as a string.
        /// </summary>
        /// <param name="assemblyName">Assembly name to get public key token.</param>
        /// <returns>String public key token.</returns>
        private string GetPublicKeyTokenString(AssemblyName assemblyName)
        {
            StringBuilder sb = new StringBuilder();
            byte[] publicKey = assemblyName.GetPublicKeyToken();
            if (null != publicKey && 0 < publicKey.Length)
            {
                for (int i = 0; i < publicKey.GetLength(0); ++i)
                {
                    sb.AppendFormat("{0:X2}", publicKey[i]);
                }
            }

            return sb.ToString();
        }
    }
}
