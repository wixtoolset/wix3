// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuild.Tools.MdCompiler
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public class VariableSubstitutions
    {
        private static readonly Regex ParseVariables = new Regex(@"{{([a-zA-Z_][a-zA-Z0-9_\-\.]*)}}", RegexOptions.Compiled);

        public VariableSubstitutions(IEnumerable<string> defines)
        {
            this.Variables = new Dictionary<string, string>();
            foreach (string define in defines)
            {
                string[] defineSplit = define.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (!this.Variables.ContainsKey(defineSplit[0]))
                {
                    this.Variables.Add(defineSplit[0], defineSplit.Length > 1 ? defineSplit[1] : null);
                }
            }
        }

        public IDictionary<string, string> Variables { get; private set; }

        public string Substitute(string filename, int lineNumber, string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                int replaceCount = 0;

                Match m = VariableSubstitutions.ParseVariables.Match(text);
                while (m.Success)
                {
                    int offset = 0;
                    string beginning = text.Substring(0, m.Index);
                    string variableName = m.Groups[1].Value;
                    string end = text.Substring(m.Index + m.Length);

                    // This is an arbitrary upper limit for variable replacements to prevent
                    // inifite loops.
                    if (replaceCount > 20)
                    {
                        Console.Error.WriteLine("Infinite loop in variable: {0} in file: {1} on line: {2}, column: {3}", variableName, filename, lineNumber, m.Index + 1);
                        break;
                    }

                    string variableValue;
                    if (this.Variables.TryGetValue(variableName, out variableValue))
                    {
                        text = String.Concat(beginning, variableValue, end);
                    }
                    else // skip the entire preprocess variable because we couldn't replace it.
                    {
                        Console.Error.WriteLine("Unknown preprocessor variable: {0} in file: {1} on line: {2}, column: {3}", variableName, filename, lineNumber, m.Index + 1);
                        offset = m.Length;
                    }

                    m = VariableSubstitutions.ParseVariables.Match(text, m.Index + offset);
                }
            }

            return text;
        }
    }
}
