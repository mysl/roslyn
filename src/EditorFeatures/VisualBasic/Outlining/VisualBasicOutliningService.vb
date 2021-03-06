' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Composition

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    <ExportLanguageService(GetType(IOutliningService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicOutliningService
        Inherits AbstractOutliningService

        Private Shared ReadOnly _defaultNodeOutlinerMap As ImmutableDictionary(Of Type, ImmutableArray(Of AbstractSyntaxNodeOutliner)) = CreateDefaultNodeOutlinerMap()
        Private Shared ReadOnly _defaultTriviaOutlinerMap As ImmutableDictionary(Of Integer, ImmutableArray(Of AbstractSyntaxTriviaOutliner)) = CreateDefaultTriviaOutlinerMap()

        Shared Function CreateDefaultNodeOutlinerMap() As ImmutableDictionary(Of Type, ImmutableArray(Of AbstractSyntaxNodeOutliner))
            Dim builder = ImmutableDictionary.CreateBuilder(Of Type, ImmutableArray(Of AbstractSyntaxNodeOutliner))()

            builder.Add(Of AccessorStatementSyntax, AccessorDeclarationOutliner)()
            builder.Add(Of ClassStatementSyntax, TypeDeclarationOutliner, MetadataAsSource.TypeDeclarationOutliner)()
            builder.Add(Of CompilationUnitSyntax, CompilationUnitOutliner)()
            builder.Add(Of SubNewStatementSyntax, ConstructorDeclarationOutliner, MetadataAsSource.ConstructorDeclarationOutliner)()
            builder.Add(Of DelegateStatementSyntax, DelegateDeclarationOutliner, MetadataAsSource.DelegateDeclarationOutliner)()
            builder.Add(Of DocumentationCommentTriviaSyntax, DocumentationCommentOutliner)()
            builder.Add(Of EnumStatementSyntax, EnumDeclarationOutliner, MetadataAsSource.EnumDeclarationOutliner)()
            builder.Add(Of EnumMemberDeclarationSyntax, MetadataAsSource.EnumMemberDeclarationOutliner)()
            builder.Add(Of EventStatementSyntax, EventDeclarationOutliner, MetadataAsSource.EventDeclarationOutliner)()
            builder.Add(Of DeclareStatementSyntax, ExternalMethodDeclarationOutliner)()
            builder.Add(Of FieldDeclarationSyntax, FieldDeclarationOutliner, MetadataAsSource.FieldDeclarationOutliner)()
            builder.Add(Of InterfaceStatementSyntax, TypeDeclarationOutliner, MetadataAsSource.TypeDeclarationOutliner)()
            builder.Add(Of MethodStatementSyntax, MethodDeclarationOutliner, MetadataAsSource.MethodDeclarationOutliner)()
            builder.Add(Of ModuleStatementSyntax, TypeDeclarationOutliner, MetadataAsSource.TypeDeclarationOutliner)()
            builder.Add(Of MultiLineLambdaExpressionSyntax, MultilineLambdaOutliner)()
            builder.Add(Of NamespaceStatementSyntax, NamespaceDeclarationOutliner)()
            builder.Add(Of OperatorStatementSyntax, OperatorDeclarationOutliner, MetadataAsSource.OperatorDeclarationOutliner)()
            builder.Add(Of PropertyStatementSyntax, PropertyDeclarationOutliner, MetadataAsSource.PropertyDeclarationOutliner)()
            builder.Add(Of RegionDirectiveTriviaSyntax, RegionDirectiveOutliner, MetadataAsSource.RegionDirectiveOutliner)()
            builder.Add(Of StructureStatementSyntax, TypeDeclarationOutliner, MetadataAsSource.TypeDeclarationOutliner)()
            builder.Add(Of XmlCDataSectionSyntax, XmlExpressionOutliner)()
            builder.Add(Of XmlCommentSyntax, XmlExpressionOutliner)()
            builder.Add(Of XmlDocumentSyntax, XmlExpressionOutliner)()
            builder.Add(Of XmlElementSyntax, XmlExpressionOutliner)()
            builder.Add(Of XmlProcessingInstructionSyntax, XmlExpressionOutliner)()

            Return builder.ToImmutable()
        End Function

        Shared Function CreateDefaultTriviaOutlinerMap() As ImmutableDictionary(Of Integer, ImmutableArray(Of AbstractSyntaxTriviaOutliner))
            Dim builder = ImmutableDictionary.CreateBuilder(Of Integer, ImmutableArray(Of AbstractSyntaxTriviaOutliner))()

            builder.Add(SyntaxKind.DisabledTextTrivia, ImmutableArray.Create(Of AbstractSyntaxTriviaOutliner)(New DisabledTextTriviaOutliner()))

            Return builder.ToImmutable()
        End Function

        Private Sub New()
            MyBase.New(_defaultNodeOutlinerMap, _defaultTriviaOutlinerMap)
        End Sub

    End Class
End Namespace
