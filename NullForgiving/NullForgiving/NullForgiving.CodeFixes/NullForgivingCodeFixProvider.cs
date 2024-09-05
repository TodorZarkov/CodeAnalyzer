namespace NullForgiving
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Rename;
    using Microsoft.CodeAnalysis.Simplification;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullForgivingCodeFixProvider)), Shared]
    public class NullForgivingCodeFixProvider : CodeFixProvider
    {
        private const string title = "Make variable nullable";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create("TZ001"); } 
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the variable declaration identified by the diagnostic
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var token = root.FindToken(diagnosticSpan.Start);
            var node = token.Parent.AncestorsAndSelf().OfType<VariableDeclarationSyntax>().FirstOrDefault();

            if (node != null)
            {
                context.RegisterCodeFix(
                    Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                        title: title,
                        createChangedDocument: c => MakeNullableAsync(context.Document, node, c),
                        equivalenceKey: title),
                    diagnostic);
            }
        }

        private async Task<Document> MakeNullableAsync(Document document, VariableDeclarationSyntax variableDeclaration, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // 1. Change the variable's type to nullable
            var variableType = variableDeclaration.Type;

            // Create a nullable version of the type (e.g., string -> string?)
            var nullableType = SyntaxFactory.NullableType(variableType)
                                              .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);

            // Replace the original type with the new nullable type
            editor.ReplaceNode(variableType, nullableType);

            // 2. Remove the null-forgiving operator (!) from the variable assignment or use
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Find all expressions that use the null-forgiving operator (e.g., nullableString!)
            var nullForgivingExpressions = root.DescendantNodes()
                .OfType<PostfixUnaryExpressionSyntax>()
                .Where(e => e.OperatorToken.IsKind(SyntaxKind.ExclamationToken));

            foreach (var nullForgivingExpression in nullForgivingExpressions)
            {
                // Replace the entire null-forgiving expression with the original expression (without the !)
                var newExpression = nullForgivingExpression.Operand
                    .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);

                // Replace the null-forgiving operator with just the original expression
                editor.ReplaceNode(nullForgivingExpression, newExpression);
            }

            // Return the updated document
            return editor.GetChangedDocument();
        }
    }
}
