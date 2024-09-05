namespace NullForgiving
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using System.Collections.Immutable;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NullForgivingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "TZ001";

        private static readonly string Title = "Null-Forgiving Operator Usage";

        private static readonly string MessageFormat = "Avoid using the null-forgiving operator (!) as it can lead to null reference exceptions.";

        private static readonly string Description = "The null-forgiving operator suppresses nullability warnings, which can lead to runtime exceptions.";

        private const string Category = "Safety";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SuppressNullableWarningExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var node = (PostfixUnaryExpressionSyntax)context.Node;

            // Check if the operator is a null-forgiving operator (!)
            if (node.OperatorToken.IsKind(SyntaxKind.ExclamationToken))
            {
                // Report a diagnostic at the location of the null-forgiving operator
                context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation()));
            }
        }
    }
}
