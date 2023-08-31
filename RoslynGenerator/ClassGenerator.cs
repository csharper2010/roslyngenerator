using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using RoslynGenerator;

[Generator]
public sealed partial class ClassGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var structPovider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (syntax, cancellationToken) => syntax.IsKind(SyntaxKind.InterfaceDeclaration)
                && ((InterfaceDeclarationSyntax)syntax).BaseList?.ChildNodes()?.Any() == true,
                transform: static (ctx, cancellationToken) => (InterfaceDeclarationSyntax)ctx.Node)
            .WithTrackingName("Syntax"); // WithTrackingName allow to record data about the step and access them from the tests

        var assemblyNameProvider = context.CompilationProvider
            .Select((compilation, cancellationToken) => compilation.AssemblyName)
            .WithTrackingName("AssemblyName");

        var valueProvider = structPovider.Combine(assemblyNameProvider);

        context.RegisterSourceOutput(valueProvider, (spc, valueProvider) =>
        {
            (var node, var assemblyName) = (valueProvider.Left, valueProvider.Right);

            var sb = new StringBuilder();
            var ns = node.GetNamespace();

            if (ns != null)
            {
                sb.AppendLine($"namespace {ns};").AppendLine();
            }

            sb.Append($"public class {node.Identifier.Text.Substring(1)} : {(ns != null ? ns + "." : "")}{node.Identifier.Text}").AppendLine(" {");

            bool isFirst = true;
            foreach (var m in node.ChildNodes().OfType<MethodDeclarationSyntax>()) {
                if (!isFirst)
                {
                    sb.AppendLine();
                }

                sb.AppendLine($"    public virtual {m.ReturnType} {m.Identifier.Text}() => default({m.ReturnType});");

                isFirst = false;
            }

            sb.AppendLine("}");

            spc.AddSource(
                node.Identifier.ValueText + ".cs", 
                SourceText.From(sb.ToString(), Encoding.UTF8));
        });
    }
}