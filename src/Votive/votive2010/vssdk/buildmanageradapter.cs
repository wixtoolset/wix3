// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Reflection;

namespace Microsoft.VisualStudio.Package
{
    /// <summary>
    /// Allows detection and use of different BuildManagerAdapter
    /// implementations.
    /// </summary>
    public class BuildManagerAdapter
    {
        private static Assembly buildManagerAssembly;

        private static Type buildManagerAccessorType;

        private static Type buildManagerAccessorInterfaceType;

        private object accessor = null;

        /// <summary>
        /// Instantiates a new instance of BuildManagerAdapter.
        /// </summary>
        public BuildManagerAdapter()
        {
            if (BuildManagerAdapter.buildManagerAssembly == null)
            {
                LoadBuildManagerAssembly();
            }
        }

        /// <summary>
        /// Loads the correct assembly and type information for accessing
        /// the IVsBuildManagerAccessor service.
        /// </summary>
        private static void LoadBuildManagerAssembly()
        {
            try
            {
                BuildManagerAdapter.buildManagerAssembly = Assembly.Load("Microsoft.VisualStudio.Shell.Interop.10.0, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                BuildManagerAdapter.buildManagerAccessorType = buildManagerAssembly.GetType("Microsoft.VisualStudio.Shell.Interop.SVsBuildManagerAccessor");
                BuildManagerAdapter.buildManagerAccessorInterfaceType = buildManagerAssembly.GetType("Microsoft.VisualStudio.Shell.Interop.IVsBuildManagerAccessor");

                if (BuildManagerAdapter.buildManagerAccessorType != null && BuildManagerAdapter.buildManagerAccessorInterfaceType != null)
                {
                    return;
                }
            }
            catch
            {
            }

            // If the first one didn't work, this one must. Don't swallow
            // exceptions here.
            BuildManagerAdapter.buildManagerAssembly = Assembly.Load("Microsoft.VisualStudio.CommonIDE, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            BuildManagerAdapter.buildManagerAccessorType = buildManagerAssembly.GetType("Microsoft.VisualStudio.CommonIDE.BuildManager.SVsBuildManagerAccessor");
            BuildManagerAdapter.buildManagerAccessorInterfaceType = buildManagerAssembly.GetType("Microsoft.VisualStudio.CommonIDE.BuildManager.IVsBuildManagerAccessor");

            if (BuildManagerAdapter.buildManagerAccessorType == null || BuildManagerAdapter.buildManagerAccessorInterfaceType == null)
            {
                throw new TypeLoadException("TypeLoadException: Microsoft.VisualStudio.CommonIDE.BuildManager.SVsBuildManagerAccessor");
            }
        }

        /// <summary>
        /// Makes a call to the loaded accessor's BeginDesignTimeBuild method.
        /// </summary>
        /// <returns></returns>
        public int BeginDesignTimeBuild()
        {
            MethodInfo beginBuild = BuildManagerAdapter.buildManagerAccessorInterfaceType.GetMethod("BeginDesignTimeBuild");

            return (int)beginBuild.Invoke(accessor, null);
        }

        /// <summary>
        /// Makes a call to the loaded accessor's BeginOneOffBuild method.
        /// </summary>
        /// <returns></returns>
        public int BeginOneOffBuild()
        {
            MethodInfo beginBuild = BuildManagerAdapter.buildManagerAccessorInterfaceType.GetMethod("BeginOneOffBuild");

            return (int)beginBuild.Invoke(accessor, null);
        }

        /// <summary>
        /// Makes a call to the loaded accessor's ClaimUIThreadForBuild method.
        /// </summary>
        /// <returns></returns>
        public int ClaimUIThreadForBuild()
        {
            MethodInfo claimThread = BuildManagerAdapter.buildManagerAccessorInterfaceType.GetMethod("ClaimUIThreadForBuild");

            return (int)claimThread.Invoke(accessor, null);
        }

        /// <summary>
        /// Makes a call to the loaded accessor's EndDesignTimeBuild method.
        /// </summary>
        /// <returns></returns>
        public int EndDesignTimeBuild()
        {
            MethodInfo endBuild = BuildManagerAdapter.buildManagerAccessorInterfaceType.GetMethod("EndDesignTimeBuild");

            return (int)endBuild.Invoke(accessor, null);
        }

        /// <summary>
        /// Makes a call to the loaded accessor's EndOneOffBuild method.
        /// </summary>
        /// <returns></returns>
        public int EndOneOffBuild()
        {
            MethodInfo endBuild = BuildManagerAdapter.buildManagerAccessorInterfaceType.GetMethod("EndOneOffBuild");

            return (int)endBuild.Invoke(accessor, null);
        }

        /// <summary>
        /// Gets an instance of the SVsBuildManagerAccessor from the specified
        /// provider.
        /// </summary>
        /// <param name="site">The service provider.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool GetAccessor(IServiceProvider site)
        {
            if (accessor != null)
            {
                return true;
            }

            accessor = site.GetService(BuildManagerAdapter.buildManagerAccessorType);

            if (accessor == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Makes a call to the loaded accessor's GetSolutionConfiguration method.
        /// </summary>
        /// <param name="rootProject"></param>
        /// <returns></returns>
        public string GetSolutionConfiguration(object rootProject)
        {
            MethodInfo getConfig = BuildManagerAdapter.buildManagerAccessorInterfaceType.GetMethod("GetSolutionConfiguration",
                new Type[] { typeof(object), typeof(string).MakeByRefType() });
            if (getConfig != null)
            {
                string ret = String.Empty;
                getConfig.Invoke(accessor, new Object[] { rootProject, ret });
                return ret;
            }
            else
            {
                getConfig = BuildManagerAdapter.buildManagerAccessorInterfaceType.GetMethod("GetSolutionConfiguration");
                return (string)getConfig.Invoke(accessor, new Object[] { rootProject });
            }
        }

        /// <summary>
        /// Checks if the loaded accessor has a ClaimUIThreadForBuild method.
        /// </summary>
        /// <returns></returns>
        public bool HasClaimUIThreadForBuild()
        {
            MethodInfo claimThread = BuildManagerAdapter.buildManagerAccessorInterfaceType.GetMethod("ClaimUIThreadForBuild");

            return null != claimThread;
        }

        /// <summary>
        /// Makes a call to the loaded accessor's IsBuildInProgress method.
        /// </summary>
        /// <returns></returns>
        public int IsBuildInProgress()
        {
            MethodInfo progressMethod = BuildManagerAdapter.buildManagerAccessorInterfaceType.GetMethod("IsBuildInProgress");

            return (int)progressMethod.Invoke(accessor, null);
        }

        /// <summary>
        /// Makes a call to the loaded accessor's RegisterLogger method.
        /// </summary>
        /// <param name="submissionId"></param>
        /// <param name="punkLogger"></param>
        /// <returns></returns>
        public int RegisterLogger(int submissionId, object punkLogger)
        {
            MethodInfo registerLogger = BuildManagerAdapter.buildManagerAccessorInterfaceType.GetMethod("RegisterLogger");

            return (int)registerLogger.Invoke(accessor, new Object[] { submissionId, punkLogger });
        }

        /// <summary>
        /// Makes a call to the loaded accessor's ReleaseUIThreadForBuild method.
        /// </summary>
        /// <returns></returns>
        public int ReleaseUIThreadForBuild()
        {
            MethodInfo releaseThread = BuildManagerAdapter.buildManagerAccessorInterfaceType.GetMethod("ReleaseUIThreadForBuild");

            return (int)releaseThread.Invoke(accessor, null);
        }

        /// <summary>
        /// Makes a call to the loaded accessor's UnregisterLoggers method.
        /// </summary>
        /// <param name="submissionId"></param>
        /// <returns></returns>
        public int UnregisterLoggers(int submissionId)
        {
            MethodInfo unregisterLoggers = BuildManagerAdapter.buildManagerAccessorInterfaceType.GetMethod("UnregisterLoggers");

            return (int)unregisterLoggers.Invoke(accessor, new Object[] { submissionId });
        }
    }
}
