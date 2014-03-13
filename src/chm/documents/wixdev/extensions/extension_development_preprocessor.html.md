---
title: Creating a Preprocessor Extension
layout: documentation
after: extension_development_simple_example
---

# Creating a Preprocessor Extension

The preprocessor in WiX allows extensibilty at a few levels. This sample will demonstrate how to add a PreprocessorExtension to your WixExtension that will handle variables and functions you define in your own namespace.

This sample assumes you have already reviewed the [Creating a Skeleton Extension](extension_development_simple_example.html) topic.

1. Add a new class to your project called SamplePreprocessorExtension.
1. If you added a new file for this class, add a using statement that refers to the Microsoft.Tools.WindowsInstallerXml namespace.

        using Microsoft.Tools.WindowsInstallerXml;
1. Make your SamplePreprocessorExtension class implement PreprocessorExtension.

        public class SamplePreprocessorExtension : PreprocessorExtension
1. Add your SamplePreprocessorExtension to your [previously created SampleWixExtension class](extension_development_simple_example.html) and override the PreprocessorExtension property from the base class. This will cause your extension to know what to do when WiX asks your extension for its preprocessor extension.

        private SamplePreprocessorExtension preprocessorExtension; 
        
        public override PreprocessorExtension PreprocessorExtension 
        { 
            get
            { 
                if (this.preprocessorExtension == null) 
                { 
                    this.preprocessorExtension = new SamplePreprocessorExtension();
                } 
                return this.preprocessorExtension; 
        
            } 
        }
1. In your SamplePreprocessorExtension class, specify the prefixes or namespaces that your extension will handle. For example, if you want to be able to define a variable named $(sample.ReplaceMe), then you need to specify that your extension will handle the &quot;sample&quot; prefix.

        private static string[] prefixes = { "sample" }; 
        public override string[] Prefixes { get { return prefixes; } }
1. Now that you have specified the prefixes that your extension will handle, you need to handle variables and functions that are passed to you from WiX. You do this by overriding the GetVariable and EvaluateFunction methods from the PreprocessorExtension base class.

        public override string GetVariableValue(string prefix, string name) 
        { 
             string result = null; 
            // Based on the namespace and name, define the resulting string. 
            switch (prefix) 
            { 
                case "sample": 
                    switch (name) 
                    { 
                        case "ReplaceMe": 
                           // This could be looked up from anywhere you can access from your code. 
                           result = "replaced"; 
                           break; 
                    } 
                    break; 
            }  
            return result; 
        }  
           
        public override string EvaluateFunction(string prefix, string function, string[] args) 
        { 
            string result = null; 
            switch (prefix) 
            { 
                case "sample": 
                    switch (function)  
                    { 
                        case "ToUpper": 
                            if (0 < args.Length)  
                            { 
                                result = args[0].ToUpper(); 
                            } 
                            else 
                            { 
                                result = String.Empty;  
                            } 
                            break;  
                    }  
                    break;  
            }  
            return result; 
        }
1. Build the project.

You can now pass your extension on the command line to Candle and expect variables and functions in your namespace to be passed to your extension and be evaluated. To demonstrate this, try adding the following properties to your WiX source file:

    <Property Id="VARIABLETEST" Value="$(sample.ReplaceMe)" />
    <Property Id="FUNCTIONTEST" Value="$(sample.ToUpper(uppercase))" />

The resulting .msi file will have entries in the Property table with the values &quot;replaced&quot; and &quot;UPPERCASE&quot; in the Property table.
