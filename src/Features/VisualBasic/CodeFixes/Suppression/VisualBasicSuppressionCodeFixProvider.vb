' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Globalization
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Suppression
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Suppression
    <ExportSuppressionFixProvider(PredefinedCodeFixProviderNames.Suppression, LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSuppressionCodeFixProvider
        Inherits AbstractSuppressionCodeFixProvider

        Protected Overrides Function CreatePragmaRestoreDirectiveTrivia(diagnostic As Diagnostic, needsTrailingEndOfLine As Boolean) As SyntaxTriviaList
            Dim errorCodes = GetErrorCodes(diagnostic)
            Dim pragmaDirective = SyntaxFactory.EnableWarningDirectiveTrivia(errorCodes)
            Return CreatePragmaDirectiveTrivia(pragmaDirective, diagnostic, True, needsTrailingEndOfLine)
        End Function

        Protected Overrides Function CreatePragmaDisableDirectiveTrivia(diagnostic As Diagnostic, needsLeadinEndOfLine As Boolean) As SyntaxTriviaList
            Dim errorCodes = GetErrorCodes(diagnostic)
            Dim pragmaDirective = SyntaxFactory.DisableWarningDirectiveTrivia(errorCodes)
            Return CreatePragmaDirectiveTrivia(pragmaDirective, diagnostic, needsLeadinEndOfLine, True)
        End Function

        Private Shared Function GetErrorCodes(diagnostic As Diagnostic) As SeparatedSyntaxList(Of IdentifierNameSyntax)
            Dim text = diagnostic.Id
            If SyntaxFacts.GetKeywordKind(text) <> SyntaxKind.None Then
                text = "[" & text & "]"
            End If
            Return New SeparatedSyntaxList(Of IdentifierNameSyntax)().Add(SyntaxFactory.IdentifierName(text))
        End Function

        Private Function CreatePragmaDirectiveTrivia(enableOrDisablePragmaDirective As StructuredTriviaSyntax, diagnostic As Diagnostic, needsLeadingEndOfLine As Boolean, needsTrailingEndOfLine As Boolean) As SyntaxTriviaList
            Dim pragmaDirectiveTrivia = SyntaxFactory.Trivia(enableOrDisablePragmaDirective.WithAdditionalAnnotations(Formatter.Annotation))
            Dim endOfLineTrivia = SyntaxFactory.EndOfLine("
")
            Dim triviaList = SyntaxFactory.TriviaList(pragmaDirectiveTrivia)

            Dim title = diagnostic.Descriptor.Title.ToString(CultureInfo.CurrentUICulture)
            If Not String.IsNullOrWhiteSpace(title) Then
                Dim titleComment = SyntaxFactory.CommentTrivia(String.Format(" ' {0}", title)).WithAdditionalAnnotations(Formatter.Annotation)
                triviaList = triviaList.Add(titleComment)
            End If

            If needsLeadingEndOfLine Then
                triviaList = triviaList.Insert(0, endOfLineTrivia)
            End If

            If needsTrailingEndOfLine Then
                triviaList = triviaList.Add(endOfLineTrivia)
            End If

            Return triviaList
        End Function

        Protected Overrides Function GetAdjustedTokenForPragmaDisable(token As SyntaxToken, root As SyntaxNode, lines As TextLineCollection, indexOfLine As Integer) As SyntaxToken
            Dim containingStatement = token.GetAncestor(Of StatementSyntax)
            If containingStatement IsNot Nothing AndAlso
                containingStatement.GetFirstToken() <> token Then
                indexOfLine = lines.IndexOf(containingStatement.GetFirstToken().SpanStart)
                Dim line = lines(indexOfLine)
                token = root.FindToken(line.Start)
            End If

            Return token
        End Function

        Protected Overrides Function GetAdjustedTokenForPragmaRestore(token As SyntaxToken, root As SyntaxNode, lines As TextLineCollection, indexOfLine As Integer) As SyntaxToken
            Dim containingStatement = token.GetAncestor(Of StatementSyntax)
            While True
                If TokenHasTrailingLineContinuationChar(token) Then
                    indexOfLine = indexOfLine + 1
                ElseIf containingStatement IsNot Nothing AndAlso
                        containingStatement.GetLastToken() <> token Then
                    indexOfLine = lines.IndexOf(containingStatement.GetLastToken().SpanStart)
                    containingStatement = Nothing
                Else
                    Exit While
                End If

                Dim line = lines(indexOfLine)
                token = root.FindToken(line.End)
            End While

            Return token
        End Function

        Private Shared Function TokenHasTrailingLineContinuationChar(token As SyntaxToken) As Boolean
            Return token.TrailingTrivia.Any(Function(t) t.Kind = SyntaxKind.LineContinuationTrivia)
        End Function

        Protected Overrides ReadOnly Property DefaultFileExtension() As String
            Get
                Return ".vb"
            End Get
        End Property

        Protected Overrides ReadOnly Property SingleLineCommentStart() As String
            Get
                Return "'"
            End Get
        End Property

        Protected Overrides ReadOnly Property TitleForPragmaWarningSuppressionFix As String
            Get
                Return VBFeaturesResources.SuppressWithPragma
            End Get
        End Property

        Protected Overrides Function IsAttributeListWithAssemblyAttributes(node As SyntaxNode) As Boolean
            Dim attributesStatement = TryCast(node, AttributesStatementSyntax)
            Return attributesStatement IsNot Nothing AndAlso
                attributesStatement.AttributeLists.All(Function(attributeList) attributeList.Attributes.All(Function(a) a.Target.AttributeModifier.Kind() = SyntaxKind.AssemblyKeyword))
        End Function

        Protected Overrides Function IsEndOfLine(trivia As SyntaxTrivia) As Boolean
            Return trivia.Kind = SyntaxKind.EndOfLineTrivia
        End Function

        Protected Overrides Function IsEndOfFileToken(token As SyntaxToken) As Boolean
            Return token.Kind = SyntaxKind.EndOfFileToken
        End Function

        Protected Overrides Function AddGlobalSuppressMessageAttribute(newRoot As SyntaxNode, targetSymbol As ISymbol, diagnostic As Diagnostic) As SyntaxNode
            Dim compilationRoot = DirectCast(newRoot, CompilationUnitSyntax)
            Dim attributeList = CreateAttributeList(targetSymbol, diagnostic, isAssemblyAttribute:=True)
            Dim attributeStatement = SyntaxFactory.AttributesStatement(New SyntaxList(Of AttributeListSyntax)().Add(attributeList))
            Dim leadingTrivia = If(Not compilationRoot.Attributes.Any(),
                SyntaxFactory.TriviaList(SyntaxFactory.CommentTrivia(GlobalSuppressionsFileHeaderComment)),
                Nothing)
            leadingTrivia = leadingTrivia.AddRange(compilationRoot.GetLeadingTrivia())
            compilationRoot = compilationRoot.WithoutLeadingTrivia()
            Return compilationRoot.AddAttributes(attributeStatement).WithLeadingTrivia(leadingTrivia)
        End Function

        Protected Overrides Function AddLocalSuppressMessageAttribute(targetNode As SyntaxNode, targetSymbol As ISymbol, diagnostic As Diagnostic) As SyntaxNode
            Dim memberNode = DirectCast(targetNode, StatementSyntax)
            Dim attributeList = CreateAttributeList(targetSymbol, diagnostic, isAssemblyAttribute:=False)
            Dim leadingTrivia = memberNode.GetLeadingTrivia()
            memberNode = memberNode.WithoutLeadingTrivia()
            Return memberNode.AddAttributeLists(attributeList).WithLeadingTrivia(leadingTrivia)
        End Function

        Private Function CreateAttributeList(targetSymbol As ISymbol, diagnostic As Diagnostic, isAssemblyAttribute As Boolean) As AttributeListSyntax
            Dim attributeTarget = If(isAssemblyAttribute, SyntaxFactory.AttributeTarget(SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)), Nothing)
            Dim attributeName = SyntaxFactory.ParseName(SuppressMessageAttributeName)
            Dim attributeArguments = CreateAttributeArguments(targetSymbol, diagnostic, isAssemblyAttribute)

            Dim attribute As AttributeSyntax = SyntaxFactory.Attribute(attributeTarget, attributeName, attributeArguments) _
                .WithAdditionalAnnotations(Simplifier.Annotation)
            Dim attributeList = SyntaxFactory.AttributeList().AddAttributes(attribute)
            Return attributeList.WithAdditionalAnnotations(Formatter.Annotation)
        End Function

        Private Function CreateAttributeArguments(targetSymbol As ISymbol, diagnostic As Diagnostic, isAssemblyAttribute As Boolean) As ArgumentListSyntax
            ' SuppressMessage("Rule Category", "Rule Id", Justification := "Justification", MessageId := "MessageId", Scope := "Scope", Target := "Target")
            Dim category = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(diagnostic.Descriptor.Category))
            Dim categoryArgument = SyntaxFactory.SimpleArgument(category)

            Dim title = diagnostic.Descriptor.Title.ToString(CultureInfo.CurrentUICulture)
            Dim ruleIdText = If(String.IsNullOrWhiteSpace(title), diagnostic.Id, String.Format("{0}:{1}", diagnostic.Id, title))
            Dim ruleId = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(ruleIdText))
            Dim ruleIdArgument = SyntaxFactory.SimpleArgument(ruleId)

            Dim justificationExpr = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(FeaturesResources.SuppressionPendingJustification))
            Dim justificationArgument = SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName("Justification")), expression:=justificationExpr)

            Dim attributeArgumentList = SyntaxFactory.ArgumentList().AddArguments(categoryArgument, ruleIdArgument, justificationArgument)

            If isAssemblyAttribute Then
                Dim scopeString = GetScopeString(targetSymbol.Kind)
                If scopeString IsNot Nothing Then
                    Dim scopeExpr = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(scopeString))
                    Dim scopeArgument = SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName("Scope")), expression:=scopeExpr)

                    Dim targetString = GetTargetString(targetSymbol)
                    Dim targetExpr = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(targetString))
                    Dim targetArgument = SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName("Target")), expression:=targetExpr)

                    attributeArgumentList = attributeArgumentList.AddArguments(scopeArgument, targetArgument)
                End If
            End If

            Return attributeArgumentList
        End Function
    End Class
End Namespace
