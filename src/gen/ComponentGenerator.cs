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

        context.RegisterPostInitializationOutput(ctx
            => ctx.AddSource("ComponentAttribute.g.cs",
                SourceText.From("""
                    namespace Marmot;
                    [System.AttributeUsage(System.AttributeTargets.Struct)]
                    public sealed class ComponentAttribute : System.Attribute;
                """, Encoding.UTF8)));

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
            public partial class World {
        """);

        foreach (var c in components)
            sb.AppendLine($"public readonly Dictionary<int, {c.FullyQualifiedName}> {c.TypeName}s = [];");

        sb.Append("""
            }
            public static partial class WorldExtensions {
            extension (World world) {
        """);

        foreach (var c in components) sb.Append($$"""
            
            public bool Has{{c.PropertyName}}(int id) => world.{{c.TypeName}}s.ContainsKey(id);
            
            public {{c.FullyQualifiedName}} Get{{c.PropertyName}}(int id) => world.{{c.TypeName}}s[id];
            public {{c.FullyQualifiedName}} Set{{c.PropertyName}}(int id, {{c.FullyQualifiedName}} value) => world.{{c.TypeName}}s[id] = value;
            
            public {{c.FullyQualifiedName}} Get{{c.PropertyName}}OrDefault(int id) {
        
                if (world.{{c.TypeName}}s.ContainsKey(id))
                    return world.{{c.TypeName}}s[id];
                    
                return default;
            }
            
            public {{c.FullyQualifiedName}} Ensure{{c.PropertyName}}(int id) {
            
                if (!world.{{c.TypeName}}s.ContainsKey(id))
                    world.{{c.TypeName}}s[id] = default;
                    
                return world.{{c.TypeName}}s[id];
            }
            
            public {{c.FullyQualifiedName}} Require{{c.PropertyName}}(int id) {
                
                if (!world.{{c.TypeName}}s.ContainsKey(id))
                    throw new KeyNotFoundException("{{c.PropertyName}} component not found for entity" + id);
                    
                return world.{{c.TypeName}}s[id];
            }
        """);

        sb.Append("}}");

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