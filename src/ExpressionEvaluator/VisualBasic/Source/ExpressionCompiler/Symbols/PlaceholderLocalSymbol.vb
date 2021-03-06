﻿Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend MustInherit Class PlaceholderLocalSymbol
        Inherits EELocalSymbolBase

        Private ReadOnly _name As String

        Friend Sub New(method As MethodSymbol, name As String, type As TypeSymbol)
            MyBase.New(method, type)
            _name = name
        End Sub

        Friend Overrides ReadOnly Property DeclarationKind As LocalDeclarationKind
            Get
                Return LocalDeclarationKind.Variable
            End Get
        End Property

        Friend Overrides ReadOnly Property IdentifierToken As SyntaxToken
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property IdentifierLocation As Location
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return NoLocations
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Friend Overrides Function ToOtherMethod(method As MethodSymbol, typeMap As TypeSubstitution) As EELocalSymbolBase
            ' Pseudo-variables should be rewritten in PlaceholderLocalRewriter
            ' rather than copied as locals to the target method.
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend MustOverride Overrides ReadOnly Property IsReadOnly As Boolean

        Friend MustOverride Function RewriteLocal(
            compilation As VisualBasicCompilation,
            container As EENamedTypeSymbol,
            syntax As VisualBasicSyntaxNode,
            isLValue As Boolean) As BoundExpression

        Friend Shared Function ConvertToLocalType(
            compilation As VisualBasicCompilation,
            expr As BoundExpression,
            type As TypeSymbol) As BoundExpression

            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            Dim pair = Conversions.ClassifyConversion(expr.Type, type, useSiteDiagnostics)
            Debug.Assert(pair.Value Is Nothing)

            Dim conversionKind = pair.Key
            Debug.Assert(Conversions.ConversionExists(conversionKind))
            Debug.Assert((useSiteDiagnostics Is Nothing) OrElse useSiteDiagnostics.All(Function(d) d.Severity < DiagnosticSeverity.Error))

            Return New BoundDirectCast(
                expr.Syntax,
                expr,
                conversionKind,
                type).MakeCompilerGenerated()
        End Function

    End Class

End Namespace