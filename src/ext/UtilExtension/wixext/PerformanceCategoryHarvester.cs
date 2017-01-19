// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.Linq;

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.Tools.WindowsInstallerXml;

    using Util = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.Util;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Harvest WiX authoring for a file from the file system.
    /// </summary>
    public sealed class PerformanceCategoryHarvester : HarvesterExtension
    {
        /// <summary>
        /// Harvest a performance category.
        /// </summary>
        /// <param name="argument">The name of the performance category.</param>
        /// <returns>A harvested performance category.</returns>
        public override Wix.Fragment[] Harvest(string argument)
        {
            if (null == argument)
            {
                throw new ArgumentNullException("argument");
            }

            Util.PerformanceCategory perf = this.HarvestPerformanceCategory(argument);

            Wix.Component component = new Wix.Component();
            component.Id = CompilerCore.GetIdentifierFromName(argument);
            component.KeyPath = Wix.YesNoType.yes;
            component.AddChild(perf);

            Wix.Directory directory = new Wix.Directory();
            directory.Id = "TARGETDIR";
            //directory.Name = directory.Id;
            directory.AddChild(component);

            Wix.Fragment fragment = new Wix.Fragment();
            fragment.AddChild(directory);

            return new Wix.Fragment[] { fragment };
        }

        /// <summary>
        /// Harvest a performance category.
        /// </summary>
        /// <param name="category">The name of the performance category.</param>
        /// <returns>A harvested file.</returns>
        public Util.PerformanceCategory HarvestPerformanceCategory(string category)
        {
            if (null == category)
            {
                throw new ArgumentNullException("category");
            }

            if (PerformanceCounterCategory.Exists(category))
            {
                Util.PerformanceCategory perfCategory = new Util.PerformanceCategory();

                // Get the performance counter category and set the appropriate WiX attributes
                PerformanceCounterCategory pcc = PerformanceCounterCategory.GetCategories().Single(c => string.Equals(c.CategoryName, category));
                perfCategory.Id = CompilerCore.GetIdentifierFromName(pcc.CategoryName);
                perfCategory.Name = pcc.CategoryName;
                perfCategory.Help = pcc.CategoryHelp;
                if (PerformanceCounterCategoryType.MultiInstance == pcc.CategoryType)
                {
                    perfCategory.MultiInstance = Util.YesNoType.yes;
                }
                
                // If it's multi-instance, check if there are any instances and get counters from there; else we get 
                // the counters straight up. For multi-instance, GetCounters() fails if there are any instances. If there
                // are no instances, then GetCounters(instance) can't be called since there is no instance. Instances
                // will exist for each counter even if only one of the counters was "intialized."
                string[] instances = pcc.GetInstanceNames();
                bool hasInstances = instances.Length > 0;
                PerformanceCounter[] counters = hasInstances
                    ? pcc.GetCounters(instances.First())
                    : pcc.GetCounters();
                    
                foreach (PerformanceCounter counter in counters)
                {
                    Util.PerformanceCounter perfCounter = new Util.PerformanceCounter();

                    // Get the performance counter and set the appropriate WiX attributes
                    perfCounter.Name = counter.CounterName;
                    perfCounter.Type = CounterTypeToWix(counter.CounterType);
                    perfCounter.Help = counter.CounterHelp;

                    perfCategory.AddChild(perfCounter);
                }

                return perfCategory;
            }
            else
            {
                throw new WixException(UtilErrors.PerformanceCategoryNotFound(category));
            }
        }

        /// <summary>
        /// Get the WiX performance counter type.
        /// </summary>
        /// <param name="pct">The performance counter value to get.</param>
        /// <returns>The WiX performance counter type.</returns>
        private Util.PerformanceCounterTypesType CounterTypeToWix(PerformanceCounterType pct)
        {
            Util.PerformanceCounterTypesType type;

            switch (pct)
            {
                case PerformanceCounterType.AverageBase:
                    type = Util.PerformanceCounterTypesType.averageBase;
                    break;
                case PerformanceCounterType.AverageCount64:
                    type = Util.PerformanceCounterTypesType.averageCount64;
                    break;
                case PerformanceCounterType.AverageTimer32:
                    type = Util.PerformanceCounterTypesType.averageTimer32;
                    break;
                case PerformanceCounterType.CounterDelta32:
                    type = Util.PerformanceCounterTypesType.counterDelta32;
                    break;
                case PerformanceCounterType.CounterTimerInverse:
                    type = Util.PerformanceCounterTypesType.counterTimerInverse;
                    break;
                case PerformanceCounterType.SampleFraction:
                    type = Util.PerformanceCounterTypesType.sampleFraction;
                    break;
                case PerformanceCounterType.Timer100Ns:
                    type = Util.PerformanceCounterTypesType.timer100Ns;
                    break;
                case PerformanceCounterType.CounterTimer:
                    type = Util.PerformanceCounterTypesType.counterTimer;
                    break;
                case PerformanceCounterType.RawFraction:
                    type = Util.PerformanceCounterTypesType.rawFraction;
                    break;
                case PerformanceCounterType.Timer100NsInverse:
                    type = Util.PerformanceCounterTypesType.timer100NsInverse;
                    break;
                case PerformanceCounterType.CounterMultiTimer:
                    type = Util.PerformanceCounterTypesType.counterMultiTimer;
                    break;
                case PerformanceCounterType.CounterMultiTimer100Ns:
                    type = Util.PerformanceCounterTypesType.counterMultiTimer100Ns;
                    break;
                case PerformanceCounterType.CounterMultiTimerInverse:
                    type = Util.PerformanceCounterTypesType.counterMultiTimerInverse;
                    break;
                case PerformanceCounterType.CounterMultiTimer100NsInverse:
                    type = Util.PerformanceCounterTypesType.counterMultiTimer100NsInverse;
                    break;
                case PerformanceCounterType.ElapsedTime:
                    type = Util.PerformanceCounterTypesType.elapsedTime;
                    break;
                case PerformanceCounterType.SampleBase:
                    type = Util.PerformanceCounterTypesType.sampleBase;
                    break;
                case PerformanceCounterType.RawBase:
                    type = Util.PerformanceCounterTypesType.rawBase;
                    break;
                case PerformanceCounterType.CounterMultiBase:
                    type = Util.PerformanceCounterTypesType.counterMultiBase;
                    break;
                case PerformanceCounterType.RateOfCountsPerSecond64:
                    type = Util.PerformanceCounterTypesType.rateOfCountsPerSecond64;
                    break;
                case PerformanceCounterType.RateOfCountsPerSecond32:
                    type = Util.PerformanceCounterTypesType.rateOfCountsPerSecond32;
                    break;
                case PerformanceCounterType.CountPerTimeInterval64:
                    type = Util.PerformanceCounterTypesType.countPerTimeInterval64;
                    break;
                case PerformanceCounterType.CountPerTimeInterval32:
                    type = Util.PerformanceCounterTypesType.countPerTimeInterval32;
                    break;
                case PerformanceCounterType.SampleCounter:
                    type = Util.PerformanceCounterTypesType.sampleCounter;
                    break;
                case PerformanceCounterType.CounterDelta64:
                    type = Util.PerformanceCounterTypesType.counterDelta64;
                    break;
                case PerformanceCounterType.NumberOfItems64:
                    type = Util.PerformanceCounterTypesType.numberOfItems64;
                    break;
                case PerformanceCounterType.NumberOfItems32:
                    type = Util.PerformanceCounterTypesType.numberOfItems32;
                    break;
                case PerformanceCounterType.NumberOfItemsHEX64:
                    type = Util.PerformanceCounterTypesType.numberOfItemsHEX64;
                    break;
                case PerformanceCounterType.NumberOfItemsHEX32:
                    type = Util.PerformanceCounterTypesType.numberOfItemsHEX32;
                    break;
                default:
                    throw new WixException(UtilErrors.UnsupportedPerformanceCounterType(pct.ToString()));
            }

            return type;
        }
    }
}
