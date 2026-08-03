using System.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Marmot.Backend.Generators;

[Generator]
public sealed class ComponentGenerator : IIncrementalGenerator  {

    private readonly record struct ComponentInfo(string TypeName, string FullyQualifiedName, string PropertyName);

    public void Initialize(IncrementalGeneratorInitializationContext context) {

        var components = context.SyntaxProvider.ForAttributeWithMetadataName(

            "Marmot.ComponentAttribute",

            static (node, _) => node is StructDeclarationSyntax or ClassDeclarationSyntax,

            static (ctx, _) => {

                var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
                var typeName = symbol.Name;

                var propertyName = typeName.EndsWith("Component")
                    ? typeName.Substring(0, typeName.Length - "Component".Length)
                    : typeName;

                return new ComponentInfo(typeName, symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), propertyName);

            }).WithComparer(ComponentInfoComparer.Instance);

        var collected = components.Collect();

        context.RegisterSourceOutput(collected, static (spc, list) => Generate(spc, list));
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<ComponentInfo> components) {

        if (components.IsDefaultOrEmpty) return;

        var sb = new StringBuilder();

        sb.Append("""
            #nullable enable
            using System.Collections.Generic;
            namespace Marmot;
        """);

        sb.Append("""
            public static partial class SceneExtensions {
        """);

        foreach (var c in components) sb.Append($$"""
            
            extension (int entity) {
            
                public bool Has{{c.PropertyName}}() => Scene.GetComponents<{{c.FullyQualifiedName}}>().ContainsKey(entity);
        
                public {{c.FullyQualifiedName}} Get{{c.PropertyName}}() => Scene.GetComponents<{{c.FullyQualifiedName}}>()[entity];
                public {{c.FullyQualifiedName}} Set{{c.PropertyName}}({{c.FullyQualifiedName}} value) => Scene.GetComponents<{{c.FullyQualifiedName}}>()[entity] = value;
                
                public {{c.FullyQualifiedName}} Get{{c.PropertyName}}OrDefault() {
                
                    var GetComponents = Scene.GetComponents<{{c.FullyQualifiedName}}>();
                    if (GetComponents.ContainsKey(entity))
                        return GetComponents[entity];
                
                    return new {{c.FullyQualifiedName}}();
                }
                
                public {{c.FullyQualifiedName}} Ensure{{c.PropertyName}}() {
                
                    var GetComponents = Scene.GetComponents<{{c.FullyQualifiedName}}>();
                    if (!GetComponents.ContainsKey(entity))
                        GetComponents[entity] = new {{c.FullyQualifiedName}}();
                
                    return GetComponents[entity];
                }
                
                public {{c.FullyQualifiedName}} Require{{c.PropertyName}}() {
                
                    var GetComponents = Scene.GetComponents<{{c.FullyQualifiedName}}>();
                    if (!GetComponents.ContainsKey(entity))
                        throw new KeyNotFoundException("{{c.PropertyName}} component not found for entity" + entity);
                
                    return GetComponents[entity];
                }
            }
        """);

        sb.Append("}");

        context.AddSource("Components.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private sealed class ComponentInfoComparer : IEqualityComparer<ComponentInfo> {

        public static readonly ComponentInfoComparer Instance = new();

        public bool Equals(ComponentInfo x, ComponentInfo y) =>
            x.TypeName == y.TypeName &&
            x.FullyQualifiedName == y.FullyQualifiedName &&
            x.PropertyName == y.PropertyName;

        public int GetHashCode(ComponentInfo obj) =>
            (obj.TypeName, obj.FullyQualifiedName, obj.PropertyName).GetHashCode();
    }
}