//-------------------------------------------------------------------------------------------------
// <copyright file="LuxUnitTest.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Lux unit-test framework unit test classes.
// </summary>
//-------------------------------------------------------------------------------------------------


namespace Microsoft.Tools.WindowsInstallerXml.Lux.CustomActions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Deployment.WindowsInstaller;
    using Microsoft.Tools.WindowsInstallerXml.Lux;

    /// <summary>
    /// Result for a Lux unit test.
    /// </summary>
    public enum LuxTestResult
    {
        /// <summary>
        /// Unit test passed.
        /// </summary>
        Pass,

        /// <summary>
        /// Unit test failed.
        /// </summary>
        Fail,

        /// <summary>
        /// The unit test was skipped.
        /// </summary>
        Skipped,

        /// <summary>
        /// There was an error processing the unit test.
        /// </summary>
        Error
    }

    #region BaseClass
    /// <summary>
    /// Abstract class for a Lux unit test.
    /// </summary>
    public class LuxUnitTest
    {
        private Session session = null;
        private LuxTestResult result = LuxTestResult.Skipped;
        private LuxLogger logger = null;
        private string wixUnitTestId = null;
        private string condition = null;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the LuxUnitTest class. This constructor can only be called by subclasses.
        /// </summary>
        /// <param name="session">MSI session where the unit test will be running</param>
        /// <param name="logger">Logger to record test output</param>
        /// <param name="wixUnitTestId">Wix unit test id</param>
        /// <param name="condition">MSI condition to determine if test should run</param>
        protected LuxUnitTest(Session session, LuxLogger logger, string wixUnitTestId, string condition)
        {
            this.Session = session;
            this.Logger = logger;
            this.WixUnitTestId = wixUnitTestId;
            this.Condition = condition;
        }
        #endregion

        #region variables
        /// <summary>
        /// Gets or sets the MSI session handle.
        /// </summary>
        public Session Session
        {
            get { return this.session; }
            set { this.session = value; }
        }

        /// <summary>
        /// Gets or sets the result of the unit test.
        /// </summary>
        public LuxTestResult Result
        {
            get { return this.result; }
            set { this.result = value; }
        }

        /// <summary>
        /// Gets or sets the logging interface class.
        /// </summary>
        public LuxLogger Logger
        {
            get { return this.logger; }
            set { this.logger = value; }
        }

        /// <summary>
        /// Gets or sets the Wix unit test id
        /// </summary>
        public string WixUnitTestId
        {
            get { return this.wixUnitTestId; }
            set { this.wixUnitTestId = value; }
        }

        /// <summary>
        /// Gets or sets the unit test condition
        /// </summary>
        public string Condition
        {
            get { return this.condition; }
            set { this.condition = value; }
        }
        #endregion

        #region Methods

        /// <summary>
        /// Logs the unit test result using the logger supplied to the unit test class.
        /// </summary>
        public virtual void LogResult()
        {
        }
        
        /// <summary>
        /// Determines whether or not the test should be run by evaluating the test condition.
        /// </summary>
        /// <returns>true if the test should be run, false otherwise.</returns>
        public bool IsTestConditionMet()
        {
            try
            {
                return this.Session.EvaluateCondition(this.Condition, true);
            }
            catch (System.ArgumentNullException)
            {
                // If the condition is null, return true
                return true;
            }
            catch (System.Exception)
            {
                // for any other exception, don't run the test
                return false;
            }
        }


        /// <summary>
        /// Runs the unit test and sets the result field.
        /// </summary>
        public virtual void RunTest()
        {
        }
        #endregion

        
    }
    #endregion

    #region Subclasses
    /// <summary>
    /// Unit test class for expressions.
    /// </summary>
    public class LuxExpressionUnitTest : LuxUnitTest
    {
        private string expression;

        /// <summary>
        /// Initializes a new instance of the LuxExpressionUnitTest class
        /// </summary>
        /// <param name="session">MSI session handle</param>
        /// <param name="logger">Logger to capture test output</param>
        /// <param name="wixUnitTestId">Wix unit test id</param>
        /// <param name="condition">MSI condition to determine if the test should run</param>
        /// <param name="expression">Expression to evaluate as true or false</param>
        public LuxExpressionUnitTest(Session session, LuxLogger logger, string wixUnitTestId, string condition, string expression)
            : base(session, logger, wixUnitTestId, condition)
        {
            this.expression = expression;
        }

        /// <summary>
        /// Logs the unit test result using the logger supplied to the unit test class.
        /// </summary>
        public override void LogResult()
        {
            switch (this.Result)
            {
                case LuxTestResult.Pass:
                    this.Logger.Log(Constants.TestPassedExpressionTrue, this.WixUnitTestId, this.expression);
                    break;
                case LuxTestResult.Fail:
                    this.Logger.Log(Constants.TestFailedExpressionFalse, this.WixUnitTestId, this.expression);
                    break;
                case LuxTestResult.Error:
                    this.Logger.Log(Constants.TestFailedExpressionSyntaxError, this.WixUnitTestId, this.expression);
                    break;
                case LuxTestResult.Skipped:
                    this.Logger.Log(Constants.TestSkipped, this.WixUnitTestId, this.Condition);
                    break;
                default:
                    this.Logger.Log(Constants.TestUnknownResult, this.WixUnitTestId);
                    break;
            }
        }

        /// <summary>
        /// Runs the unit test and sets the result field.
        /// </summary>
        public override void RunTest()
        {
            if (Session.EvaluateCondition(this.expression))
            {
                Result = LuxTestResult.Pass;
            }
            else
            {
                Result = LuxTestResult.Fail;
            }
        }
    }

    /// <summary>
    /// Unit test class for property values.
    /// </summary>
    public class LuxPropertyValueUnitTest : LuxUnitTest 
    {
        private string property;
        private LuxOperator op;
        private string expectedValue;
        private string actualValue;

        /// <summary>
        /// Initializes a new instance of the LuxPropertyValueUnitTest class
        /// </summary>
        /// <param name="session">MSI session handle</param>
        /// <param name="logger">Logger to capture test output</param>
        /// <param name="wixUnitTestId">Wix unit test id</param>
        /// <param name="condition">MSI condition to determine if the test should run</param>
        /// <param name="property">MSI property which has a value consisting of delimited key/value pairs.</param>
        /// <param name="op">Comparison operator type</param>
        /// <param name="value">Expected value</param>
        public LuxPropertyValueUnitTest(Session session, LuxLogger logger, string wixUnitTestId, string condition, string property, LuxOperator op, string value)
            : base(session, logger, wixUnitTestId, condition)
        {
            this.property = property;
            this.op = op;
            this.expectedValue = value;
        }

        #region properties
        /// <summary>
        /// Gets or sets the MSI property to test
        /// </summary>
        protected string Property
        {
            get { return this.property; }
            set { this.property = value; }
        }

        /// <summary>
        /// Gets or sets the lux comparison operator
        /// </summary>
        protected LuxOperator Op
        {
            get { return this.op; }
            set { this.op = value; }
        }

        /// <summary>
        /// Gets or sets the expected value of the given MSI property
        /// </summary>
        protected string ExpectedValue
        {
            get { return this.expectedValue; }
            set { this.expectedValue = value; }
        }

        /// <summary>
        /// Gets or sets the actual value of the given MSI property
        /// </summary>
        protected string ActualValue
        {
            get { return this.actualValue; }
            set { this.actualValue = value; }
        }
        #endregion

        /// <summary>
        /// Logs the unit test result using the logger supplied to the unit test class.
        /// </summary>
        public override void LogResult()
        {
            switch (this.Result)
            {
                case LuxTestResult.Pass:
                    this.Logger.Log(Constants.TestPassedPropertyValueMatch, this.WixUnitTestId, this.property, this.expectedValue);
                    break;
                case LuxTestResult.Fail:
                    this.Logger.Log(Constants.TestFailedPropertyValueMismatch, this.WixUnitTestId, this.property, this.expectedValue, this.actualValue);
                    break;
                case LuxTestResult.Error:
                    this.Logger.Log(Constants.TestUnknownOperation, this.WixUnitTestId);
                    break;
                case LuxTestResult.Skipped:
                    this.Logger.Log(Constants.TestSkipped, this.WixUnitTestId, this.Condition);
                    break;
                default:
                    this.Logger.Log(Constants.TestUnknownResult, this.WixUnitTestId);
                    break;
            }
        }

        /// <summary>
        /// Runs the unit test and sets the result field.
        /// </summary>
        public override void RunTest()
        {
            // Get the property
            this.actualValue = this.Session[this.property];
       
            this.RunTestInternal();
        }


        /// <summary>
        /// Compares the expected value to the actual value, and sets the test result.
        /// </summary>
        protected void RunTestInternal()
        {
            string actualVal;
            string expectedVal;

            switch (this.op)
            {
                case LuxOperator.CaseInsensitiveEqual:
                    actualVal = this.Session.Format(this.actualValue);
                    expectedVal = this.Session.Format(this.expectedValue);
                    if (actualVal.Equals(expectedVal, StringComparison.OrdinalIgnoreCase))
                    {
                        this.Result = LuxTestResult.Pass;
                    }
                    else
                    {
                        this.Result = LuxTestResult.Fail;
                    }
                    break;
                case LuxOperator.Equal:
                    actualVal = this.Session.Format(this.actualValue);
                    expectedVal = this.Session.Format(this.expectedValue);
                    if (actualVal.Equals(expectedVal))
                    {
                        this.Result = LuxTestResult.Pass;
                    }
                    else
                    {
                        this.Result = LuxTestResult.Fail;
                    }
                    break;
                case LuxOperator.CaseInsensitiveNotEqual:
                    actualVal = this.Session.Format(this.actualValue);
                    expectedVal = this.Session.Format(this.expectedValue);
                    if (!actualVal.Equals(expectedVal, StringComparison.OrdinalIgnoreCase))
                    {
                        this.Result = LuxTestResult.Pass;
                    }
                    else
                    {
                        this.Result = LuxTestResult.Fail;
                    }
                    break;
                case LuxOperator.NotEqual:
                    actualVal = this.Session.Format(this.actualValue);
                    expectedVal = this.Session.Format(this.expectedValue);
                    if (!actualVal.Equals(expectedVal))
                    {
                        this.Result = LuxTestResult.Pass;
                    }
                    else
                    {
                        this.Result = LuxTestResult.Fail;
                    }
                    break;
                default:
                    {
                        this.Result = LuxTestResult.Error;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Unit test for delimited value lists
    /// </summary>
    public class LuxDelimitedListUnitTest : LuxPropertyValueUnitTest
    {
        private string valueSeparator;
        private int index;

        /// <summary>
        /// Initializes a new instance of the LuxDelimitedListUnitTest class
        /// </summary>
        /// <param name="session">MSI session handle</param>
        /// <param name="logger">Logger to capture test output</param>
        /// <param name="wixUnitTestId">Wix unit test id</param>
        /// <param name="condition">MSI condition to determine if the test should run</param>
        /// <param name="property">MSI property which has a value consisting of delimited key/value pairs.</param>
        /// <param name="op">Comparison operator type</param>
        /// <param name="value">Expected value</param>
        /// <param name="valueSeparator">Delimiter char/string</param>
        /// <param name="index">Index of the value in the delimited value array to test</param>
        public LuxDelimitedListUnitTest(Session session, LuxLogger logger, string wixUnitTestId, string condition, string property, LuxOperator op, string value, string valueSeparator, string index)
            : base(session, logger, wixUnitTestId, condition, property, op, value)
        {
            this.valueSeparator = valueSeparator;
            this.index = Convert.ToInt32(index);
        }

        /// <summary>
        /// Runs the unit test and sets the result field.
        /// </summary>
        public override void RunTest()
        {
            // use the value separator to split the property, and then grab the value at the specified index
            List<string> valueList = this.Session[this.Property].Split(this.valueSeparator.ToCharArray()).ToList<string>();
            if (valueList.Count <= this.index)
            {
                this.Logger.Log(Constants.TestFailedIndexOutOfBounds, this.WixUnitTestId, this.index.ToString(), this.Session[this.Property]);
                this.Result = LuxTestResult.Error;
                return;
            }
            else
            {
                // Get the property
                this.ActualValue = valueList[this.index];
                this.RunTestInternal();
            }
        }
    }

    /// <summary>
    /// Class that represents a single Lux delimited key/value unit test
    /// </summary>
    public class LuxDelimitedKeyValueUnitTest : LuxPropertyValueUnitTest
    {
        private string nameValueSeparator;
        private string index;

        /// <summary>
        /// Initializes a new instance of the LuxDelimitedKeyValueUnitTest class
        /// </summary>
        /// <param name="session">MSI session handle</param>
        /// <param name="logger">Logger to capture test output</param>
        /// <param name="wixUnitTestId">Wix unit test id</param>
        /// <param name="condition">MSI condition to determine if the test should run</param>
        /// <param name="property">MSI property which has a value consisting of delimited key/value pairs.</param>
        /// <param name="op">Comparison operator type</param>
        /// <param name="value">Expected value</param>
        /// <param name="nameValueSeparator">Delimiter char/string</param>
        /// <param name="index">Key in the key/value pairings to test</param>
        public LuxDelimitedKeyValueUnitTest(Session session, LuxLogger logger, string wixUnitTestId, string condition, string property, LuxOperator op, string value, string nameValueSeparator, string index)
            : base(session, logger, wixUnitTestId, condition, property, op, value)
        {
            this.nameValueSeparator = nameValueSeparator;
            this.index = index;
        }

        /// <summary>
        /// Runs the unit test and sets the result field.
        /// </summary>
        public override void RunTest()
        {
            // use the name value separator to split the property and create a dictionary
            List<string> valueList = this.Session[this.Property].Split(this.nameValueSeparator.ToCharArray()).ToList<string>();
            
            // make sure that there are enough values to create key value pairs
            if ((valueList.Count % 2) != 0)
            {
                this.Logger.Log(Constants.TestFailedExpectedEvenNameValueContent, this.WixUnitTestId, this.Session[this.Property]);
                this.Result = LuxTestResult.Error;
                return;
            }

            Dictionary<string, string> nameValueDict = new Dictionary<string, string>();
            for (int i = 0; i < valueList.Count; i += 2)
            {
                nameValueDict.Add(valueList[i], valueList[i + 1]);
            }

            if (nameValueDict.ContainsKey(this.index))
            {
                this.ActualValue = nameValueDict[this.index];
                this.RunTestInternal();
            }
            else
            {
                this.Logger.Log(Constants.TestFailedIndexUnknown, this.WixUnitTestId, this.index, this.Session[this.Property]);
                this.Result = LuxTestResult.Error;
            }

            return;
        }

    }
    #endregion
}
