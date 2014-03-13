//-----------------------------------------------------------------------
// <copyright file="NamedFact.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System.Collections.Generic;
    using Xunit;
    using Xunit.Sdk;

    public interface ISupportNamedFacts
    {
        void SetFactName(string testNamespace, string testClass, string testMethod);
    }

    public class NamedFact : FactAttribute
    {
        protected override IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo method)
        {
            yield return new NamedFactCommand(method);
        }

        private class NamedFactCommand : FactCommand
        {
            public string Namespace { get; set; }

            public string Class { get; set; }

            public string Method { get; set; }

            public NamedFactCommand(IMethodInfo method) :
                base(method)
            {
                this.Namespace = method.Class.Type.Namespace;

                this.Class = method.Class.Type.Name;

                this.Method = method.Name;
            }

            public override MethodResult Execute(object testClass)
            {
                ISupportNamedFacts namedTest = testClass as ISupportNamedFacts;
                if (namedTest != null)
                {
                    namedTest.SetFactName(this.Namespace, this.Class, this.Method);
                }

                return base.Execute(testClass);
            }
        }
    }
}
