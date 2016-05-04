// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Candle.PreProcessor
{
    using System;
    using System.IO;
    using System.Text;
    using WixTest;
    
    /// <summary>
    /// Test how Candle handles preprocessing for variables.
    /// </summary>
    public class VariableTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Candle\PreProcessor\VariablesTests");

        [NamedFact]
        [Description("Verify that Candle can accept a <variable=value> and preprocess the wxs file by replacing the variable with the value.")]
        [Priority(2)]
        public void PreProcessorParamWithValueSpecified()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(VariableTests.TestDataDirectory, @"PreProcessorParam\Product.wxs"));
            candle.PreProcessorParams.Add("MyVariable", "foo");
            candle.Run();

            Verifier.VerifyWixObjProperty(candle.ExpectedOutputFiles[0], "MyProperty1", "foo");
        }

        [NamedFact]
        [Description("Verify that Candle displays an error when a preprocessor variable is undefined.")]
        [Priority(2)]
        public void UndefinedPreProcessorVariable()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(VariableTests.TestDataDirectory, @"PreProcessorParam\Product.wxs"));
            string outputString = String.Format("Undefined preprocessor variable '$(var.MyVariable)'.");
            candle.ExpectedWixMessages.Add(new WixMessage(150, outputString, WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 150;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that Candle can preprocess a variable")]
        [Priority(1)]
        public void PreProcessorVariable()
        {
            string outputFile = Candle.Compile(Path.Combine(VariableTests.TestDataDirectory, @"PreProcessorVariable\Product.wxs"));
            
            Verifier.VerifyWixObjProperty(outputFile, "MyProperty1", "bar");
        }
        
        [NamedFact]
        [Description("Verify that Candle can preprocess to a file.")]
        [Priority(3)]
        public void PreProcessToFile()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(VariableTests.TestDataDirectory, @"PreProcessToFile\Product.wxs"));
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory); 
            candle.PreProcessFile = Path.Combine(tempDirectory, "PreProcessedFile.wxs");
            candle.Run(false);

            ///TODO: we need to verify the preprocessed file contents
        }

        [NamedFact]
        [Description("Verify that Candle can preprocess environment variables specified in the authoring")]
        [Priority(1)]
        public void EnvironmentVariable()
        {
            string outputFile = Candle.Compile(Path.Combine(VariableTests.TestDataDirectory, @"EnvironmentVariable\Product.wxs"));

            Verifier.VerifyWixObjProperty(outputFile, "MyProperty", Environment.ExpandEnvironmentVariables("%WIX_ROOT%"));
        }

        [NamedFact]
        [Description("Verify that Candle can preprocess system variables specified in the authoring")]
        [Priority(2)]
        public void SystemVariable()
        {
            string outputFile = Candle.Compile(Path.Combine(VariableTests.TestDataDirectory, @"SystemVariable\Product.wxs"));
            
            // The currentdirectory is returned without a \ in the end, modifying it to contain a \ as $(sys.CURRENTDIR) contains a \ in the end
            string expectedValue = String.Concat(Environment.CurrentDirectory, Path.DirectorySeparatorChar);
            Verifier.VerifyWixObjProperty(outputFile, "MyProperty", expectedValue);
        }

        [NamedFact]
        [Description("Verify that Candle can evaluate preprocessor variables before include, warning and error")]
        [Priority(1)]
        public void PreprocessorVariableEvaluation()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(VariableTests.TestDataDirectory, @"PreprocessorVariableEvaluation\Product.wxs"));
            candle.PreProcessorParams.Add("Action", "Warning");
            candle.ExpectedWixMessages.Add(new WixMessage(1096, "Test Warning", WixMessage.MessageTypeEnum.Warning));
            candle.Run();

            candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(VariableTests.TestDataDirectory, @"PreprocessorVariableEvaluation\Product.wxs"));
            candle.PreProcessorParams.Add("Action", "Error");
            candle.ExpectedWixMessages.Add(new WixMessage(250, "Test Error", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 250;
            candle.Run();
        }


        [NamedFact]
        [Description("Verify that Candle generates the appropriate error for -d without anything after it.")]
        [Priority(2)]
        public void MissingParameter()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.BasicProductWxs);
            candle.OtherArguments = " -d";
            candle.ExpectedWixMessages.Add(new WixMessage(289, "The variable definition '-d' is not valid.  Variable definitions should be in the form -dname=value where the value is optional.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode=289;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that Candle generates the appropriate error for missing variable names (e.g. -d=value)")]
        [Priority(2)]
        public void MissingVariableName()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.BasicProductWxs);
            candle.OtherArguments = " -d=value";
            candle.ExpectedWixMessages.Add(new WixMessage(289, "The variable definition '-d=value' is not valid.  Variable definitions should be in the form -dname=value where the value is optional.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 289;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that Candle generates the appropriate error for invalid variable name (e.g. -d=)")]
        [Priority(2)]
        public void InvalidVariable()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.BasicProductWxs);
            candle.OtherArguments = " -d=";
            candle.ExpectedWixMessages.Add(new WixMessage(289, "The variable definition '-d=' is not valid.  Variable definitions should be in the form -dname=value where the value is optional.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 289;
            candle.Run();

            candle = new Candle();
            candle.SourceFiles.Add(WixTests.BasicProductWxs);
            candle.OtherArguments = " -d===";
            candle.ExpectedWixMessages.Add(new WixMessage(289, "The variable definition '-d===' is not valid.  Variable definitions should be in the form -dname=value where the value is optional.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 289;
            candle.Run();
        } 

        [NamedFact]
        [Description("Verify that Candle generates the appropriate error for redefinition of variables through commandline")]
        [Priority(2)]
        public void CommandlineVariableRedefinition()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.BasicProductWxs);
            candle.OtherArguments = " -dVar=value1 -dVar=value2";
            candle.ExpectedWixMessages.Add(new WixMessage(288, "The variable 'Var' with value 'value2' was previously declared with value 'value1'.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 288;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that Candle generates the appropriate error for redefinition of variables in authoring")]
        [Priority(2)]
        public void AuthoringVariableRedefinition()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(VariableTests.TestDataDirectory, @"AuthoringVariableRedefinition\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(1118, "The variable 'Var' with value 'value2' was previously declared with value 'value1'.", WixMessage.MessageTypeEnum.Warning));
            candle.ExpectedExitCode = 0;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that Candle generates the appropriate error for variables redefinition through commandline for variables already defined in authoring")]
        [Priority(2)]
        public void VariableRedefinition()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(VariableTests.TestDataDirectory, @"VariableRedefinition\Product.wxs"));
            candle.PreProcessorParams.Add("Var", "value1");
            candle.ExpectedWixMessages.Add(new WixMessage(1118, "The variable 'Var' with value 'value2' was previously declared with value 'value1'.", WixMessage.MessageTypeEnum.Warning));
            candle.ExpectedExitCode = 0;
            candle.Run();
        } 
    }
}
