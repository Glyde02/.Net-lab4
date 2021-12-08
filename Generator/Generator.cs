﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GeneratorLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GeneratorLib
{
    public static class Generator
    {
        public static Task GenerateAsync(GeneratingOptions options)
        {
            var directoryReader = CreateReadDirectoryBlock();
            var fileReader = CreateReadFileBlock(options.MaxRead);
            var generator = CreateGenerateBlock(options.MaxGenerate);
            var writer = CreateWriteBlock(options.DestinationDirectory, options.MaxWrite);

            var opt = new DataflowLinkOptions { PropagateCompletion = true };
            directoryReader.LinkTo(fileReader, opt);
            fileReader.LinkTo(generator, opt);
            generator.LinkTo(writer, opt);

            directoryReader.Post(options.SourceDirectory);
            directoryReader.Complete();
            return writer.Completion;
        }

        private static TransformManyBlock<string, string> CreateReadDirectoryBlock() =>
            new(path =>
            {
                if (!Directory.Exists(path))
                {
                    throw new ArgumentException("Directory doesn't exist");
                }

                return Directory.EnumerateFiles(path);
            });

        private static TransformBlock<string, string> CreateReadFileBlock(int maxRead) =>
            new(async path =>
            {
                if (!File.Exists(path))
                {
                    throw new ArgumentException("File doesn't exist");
                }

                using var reader = File.OpenText(path);
                return await reader.ReadToEndAsync();
            },
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxRead });

        private static TransformManyBlock<string, WriterOptions> CreateGenerateBlock(int maxGenerate) =>
            new(
                GenerateTests,
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxGenerate });


        private static ActionBlock<WriterOptions> CreateWriteBlock(string directory, int maxWrite) =>
            new(options =>
            {
                using var outputFile = new StreamWriter($"{directory}/{options.Filename}");
                return outputFile.WriteAsync(options.Content);
            },
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxWrite });

        public static IEnumerable<WriterOptions> GenerateTests(string sourceCode)
        {
            return CSharpSyntaxTree
                .ParseText(sourceCode)
                .GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Select(GenerateTest);
        }

        private static WriterOptions GenerateTest(ClassDeclarationSyntax classDeclaration)
        {
            const string tab = "  ";
            var methods = classDeclaration
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.PublicKeyword));

            var unit = SyntaxFactory.CompilationUnit().AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Xunit")));
            var content = SyntaxFactory
                .NamespaceDeclaration(
                    SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("Autogenerated"),
                    SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(SyntaxFactory.TriviaList(), "Tests",
                    SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed)))))
                .WithNamespaceKeyword(
                    SyntaxFactory.Token(
                        SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.CarriageReturnLineFeed),
                        SyntaxKind.NamespaceKeyword,
                        SyntaxFactory.TriviaList(SyntaxFactory.Space)))
                .WithOpenBraceToken(
                    SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.OpenBraceToken,
                    SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed)))
                .WithCloseBraceToken(
                    SyntaxFactory.Token(SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed),
                    SyntaxKind.CloseBraceToken,
                    SyntaxFactory.TriviaList()))
                .AddMembers(SyntaxFactory
                        .ClassDeclaration(SyntaxFactory.Identifier(
                            SyntaxFactory.TriviaList(),
                            classDeclaration.Identifier.Text + "Test",
                            SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed)))
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(
                            SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(tab)),
                            SyntaxKind.PublicKeyword,
                            SyntaxFactory.TriviaList(SyntaxFactory.Space))))
                        .WithKeyword(SyntaxFactory.Token(
                            SyntaxFactory.TriviaList(),
                            SyntaxKind.ClassKeyword,
                            SyntaxFactory.TriviaList(SyntaxFactory.Space)))
                        .WithOpenBraceToken(SyntaxFactory.Token(
                            SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(tab)),
                            SyntaxKind.OpenBraceToken,
                            SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed)))
                        .WithCloseBraceToken(
                            SyntaxFactory.Token(
                                SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(tab)),
                                SyntaxKind.CloseBraceToken,
                                SyntaxFactory.TriviaList()))
                        .AddMembers(methods.Select(m =>
                            SyntaxFactory
                            .MethodDeclaration(
                                SyntaxFactory.PredefinedType(
                                    SyntaxFactory.Token(
                                        SyntaxFactory.TriviaList(),
                                        SyntaxKind.VoidKeyword,
                                        SyntaxFactory.TriviaList(SyntaxFactory.Space))),
                                    SyntaxFactory.Identifier(m.Identifier.Text + "Test"))
                            .WithAttributeLists(SyntaxFactory.SingletonList(SyntaxFactory
                                .AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Fact"))))
                                .WithOpenBracketToken(SyntaxFactory.Token(
                                    SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(tab + tab)),
                                    SyntaxKind.OpenBracketToken,
                                    SyntaxFactory.TriviaList()))
                                .WithCloseBracketToken(SyntaxFactory.Token(
                                    SyntaxFactory.TriviaList(),
                                    SyntaxKind.CloseBracketToken,
                                    SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed)))))
                            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxFactory.TriviaList(
                                SyntaxFactory.Whitespace(tab + tab)),
                                SyntaxKind.PublicKeyword,
                                SyntaxFactory.TriviaList(SyntaxFactory.Space))))
                            .WithParameterList(SyntaxFactory.ParameterList().WithCloseParenToken(
                                SyntaxFactory.Token(SyntaxFactory.TriviaList(),
                                SyntaxKind.CloseParenToken,
                                SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed))))
                            .WithBody(SyntaxFactory
                                .Block()
                                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(
                                    SyntaxFactory.Whitespace(tab + tab)),
                                    SyntaxKind.OpenBraceToken,
                                    SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed)))
                                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(
                                    SyntaxFactory.Whitespace(tab + tab)),
                                    SyntaxKind.CloseBraceToken,
                                    SyntaxFactory.TriviaList(
                                        SyntaxFactory.CarriageReturnLineFeed,
                                        SyntaxFactory.Whitespace(""),
                                        SyntaxFactory.CarriageReturnLineFeed))))
                                .AddBodyStatements(SyntaxFactory.ExpressionStatement(SyntaxFactory
                                    .InvocationExpression(SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(
                                            SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(tab + tab + tab)),
                                        "Assert",
                                            SyntaxFactory.TriviaList())),
                                        SyntaxFactory.IdentifierName("True")))
                                    .WithArgumentList(
                                        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                    new SyntaxNodeOrToken[]
                                                    {
                                                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                                                                SyntaxKind.FalseLiteralExpression)),
                                                        SyntaxFactory.Token(
                                                            SyntaxFactory.TriviaList(),
                                                            SyntaxKind.CommaToken,
                                                            SyntaxFactory.TriviaList(SyntaxFactory.Space)),
                                                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                                                                SyntaxKind.StringLiteralExpression,
                                                                SyntaxFactory.Literal("autogenerated")))

                                                    }))))
                                    .WithSemicolonToken(SyntaxFactory.Token(
                                        SyntaxFactory.TriviaList(),
                                        SyntaxKind.SemicolonToken,
                                        SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed)))))
                            .Cast<MemberDeclarationSyntax>()
                            .ToArray()
                        ));
            var text = unit.NormalizeWhitespace().AddMembers(content).ToFullString();
            return new WriterOptions(classDeclaration.Identifier.Text + "Test.cs", text);
        }
    }

}