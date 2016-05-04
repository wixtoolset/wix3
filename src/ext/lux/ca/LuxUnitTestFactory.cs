// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Lux.CustomActions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Deployment.WindowsInstaller;
    using Microsoft.Tools.WindowsInstallerXml.Lux;

    /// <summary>
    /// Factory class for Lux unit tests. Returns a LuxUnitTest of the appropriate type.
    /// </summary>
    public class LuxUnitTestFactory
    {
        /// <summary>
        /// Supported Lux unit test types.
        /// </summary>
        private enum TestType
        {
            /// <summary>
            /// Tests an input expression for true/false
            /// </summary>
            Expression,
            
            /// <summary>
            /// Tests a property and compares it to the given value
            /// </summary>
            PropertyValue,

            /// <summary>
            /// Test a variable at the index in a delimited property
            /// </summary>
            DelimitedList,

            /// <summary>
            /// Tests a variable in a key/value delimited property
            /// </summary>
            DelimitedKeyValue,

            /// <summary>
            /// Test is not implemented/supported
            /// </summary>
            Unknown 
        }
 
        /// <summary>
        /// Creates the appropriate unit test class and returns the base class.
        /// </summary>
        /// <param name="session">MSI session handle.</param>
        /// <param name="record">Record from the LuxCustomAction MSI table.</param>
        /// <param name="logger">Logger to record unit test output.</param>
        /// <returns>A Lux unit test appropriate for the given record. Returns null on error.</returns>
        public LuxUnitTest CreateUnitTest(Session session, Record record, LuxLogger logger)
        {
            string wixUnitTestId = record["WixUnitTest"] as string;
            string customAction = record["CustomAction_"] as string;
            string property = record["Property"] as string;
            LuxOperator op = (LuxOperator)Convert.ToInt16(record["Operator"] as object);
            string value = record["Value"] as string;
            string expression = record["Expression"] as string;
            string condition = record["Condition"] as string;
            string valueSeparator = record["ValueSeparator"] as string;
            string nameValueSeparator = record["NameValueSeparator"] as string;
            string index = record["Index"] as string;

            switch (this.DetermineTestType(expression, property, op, index, valueSeparator, nameValueSeparator))
            {
                case TestType.Expression:
                    return new LuxExpressionUnitTest(session, logger, wixUnitTestId, condition, expression);
                case TestType.PropertyValue:
                    return new LuxPropertyValueUnitTest(session, logger, wixUnitTestId, condition, property, op, value);
                case TestType.DelimitedList:
                    return new LuxDelimitedListUnitTest(session, logger, wixUnitTestId, condition, property, op, value, valueSeparator, index);
                case TestType.DelimitedKeyValue:
                    return new LuxDelimitedKeyValueUnitTest(session, logger, wixUnitTestId, condition, property, op, value, nameValueSeparator, index);
                default:
                    logger.Log(Constants.TestNotCreated, wixUnitTestId);
                    return null;
            }
        }

        /// <summary>
        /// Figures out the test type based on which columns (input parameters) are non-null for a 
        /// particular row in the LuxCustomAction MSI table.
        /// </summary>
        /// <param name="expression">Expression from the unit test row in the LuxUnitTest MSi table.</param>
        /// <param name="property">Property from the unit test row in the LuxUnitTest MSi table.</param>
        /// <param name="op">Operation from the unit test row in the LuxUnitTest MSi table.</param>
        /// <param name="index">Index from the unit test row in the LuxUnitTest MSi table.</param>
        /// <param name="valueSeparator">Value separator from the unit test row in the LuxUnitTest MSi table.</param>
        /// <param name="nameValueSeparator">Name/value separator from the unit test row in the LuxUnitTest MSi table.</param>
        /// <returns>The correct test type, or TestType.Unknown if unable to figure out the test from the non-null inputs.</returns>
        private TestType DetermineTestType(string expression, string property, LuxOperator op, string index, string valueSeparator, string nameValueSeparator)
        {
            // Expression
            if (!string.IsNullOrEmpty(expression))
            {
                return TestType.Expression;
            }
            else if (!string.IsNullOrEmpty(property) &&
                     op > 0 &&
                     !string.IsNullOrEmpty(index) &&
                     !string.IsNullOrEmpty(valueSeparator))
            {
                return TestType.DelimitedList;
            }
            else if (!string.IsNullOrEmpty(property) &&
                     op > 0 &&
                     !string.IsNullOrEmpty(index) &&
                     !string.IsNullOrEmpty(nameValueSeparator))
            {
                return TestType.DelimitedKeyValue;
            }
            else if (!string.IsNullOrEmpty(property) &&
                     op > 0)
            {
                return TestType.PropertyValue;
            }
            else
            {
                return TestType.Unknown;
            }

        }        
    }
}
