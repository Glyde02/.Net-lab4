using GeneratorLib;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    public class Generating
    {
        private const string sourceCode = @"
        using System;

        namespace SrcFiles
        {
            public class Class1
            {
                public void Method1()
                {

                }

                public void Method2()
                {

                }
            }
        }";

        [Fact]
        public void Count_Num_Of_Test_Classes()
        {
            var tests = Generator.GenerateTests(sourceCode);

            Assert.Single(tests);
        }

        [Fact]
        public void Correct_Class_Name()
        {
            var tests = Generator.GenerateTests(sourceCode).ToArray();

            var className1 = CSharpSyntaxTree.ParseText(tests[0].Content)
                .GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Single()
                .Identifier
                .ValueText;

            Assert.Equal("Class1Test", className1);
        }

        [Fact]
        public void Correct_Num_Of_Methods()
        {
            var tests = Generator.GenerateTests(sourceCode).ToArray();

            var count = CSharpSyntaxTree.ParseText(tests[0].Content)
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Count();

            Assert.Equal(2, count);
        }
    }
}
