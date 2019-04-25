using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace NuGet.Tools.Documentation
{
    /// <summary>
    /// See: https://github.com/dotnet/docfx/blob/dev/src/Microsoft.DocAsCode.Metadata.ManagedReference.Roslyn/Visitors/SymbolVisitorAdapter.cs
    /// See: https://github.com/dotnet/docfx/blob/dev/src/Microsoft.DocAsCode.Metadata.ManagedReference.Roslyn/Visitors/CSYamlModelGenerator.cs
    /// </summary>
    public class MetadataVisitor : SymbolVisitor<object>
    {
        public override object VisitAssembly(IAssemblySymbol symbol)
        {
            var members = symbol
                .GlobalNamespace
                .GetMembers()
                .Select(m => m.Accept(this))
                .ToList();

            // Namespaces return a list of objets.
            var namespaceMembers = members.OfType<List<TypeInfo>>().SelectMany(l => l).ToList();

            // TODO: Nested types!
            return new AssemblyInfo
            {
                Types = namespaceMembers.OfType<TypeInfo>().ToList()
            };
        }

        public override object VisitNamespace(INamespaceSymbol symbol)
        {
            var members = symbol.GetMembers().Select(m => m.Accept(this)).ToList();

            var namespaces = members.OfType<List<TypeInfo>>();
            var types = members.OfType<TypeInfo>();

            return namespaces.SelectMany(n => n).Concat(types).ToList();
        }

        public override object VisitNamedType(INamedTypeSymbol symbol)
        {
            var members = symbol.GetMembers().Select(m => m.Accept(this)).ToList();

            return new TypeInfo
            {
                Name = symbol.Name,
                Namespace = symbol.ContainingNamespace.GetFullName(),

                Fields = members.OfType<FieldInfo>().ToList(),
                Methods = members.OfType<MethodInfo>().ToList(),
                Properties = members.OfType<PropertyInfo>().ToList()
            };
        }

        public override object VisitField(IFieldSymbol symbol)
        {
            return new FieldInfo
            {
                Name = symbol.Name,
                Type = symbol.Type.GetFullName()
            };
        }

        public override object VisitMethod(IMethodSymbol symbol)
        {
            return new MethodInfo
            {
                Name = symbol.Name,

                ReturnType = symbol.ReturnType.GetFullName(),
                ParameterTypes = symbol.Parameters.Select(p => p.Type.GetFullName()).ToList(),
            };
        }

        public override object VisitProperty(IPropertySymbol symbol)
        {
            return new PropertyInfo
            {
                Name = symbol.Name,
                Type = symbol.Type.GetFullName(),

                HasGetter = symbol.GetMethod != null,
                HasSetter = symbol.SetMethod != null,
            };
        }
    }

    internal static class ISymbolExtensions
    {
        public static string GetFullName(this INamespaceSymbol symbol)
        {
            if (symbol == null || symbol.Name == string.Empty) return null;

            var parent = symbol.ContainingNamespace.GetFullName();

            return (parent != null)
                ? $"{parent}.{symbol.Name}"
                : symbol.Name;
        }

        public static string GetFullName(this ITypeSymbol symbol)
        {
            var @namespace = symbol.ContainingNamespace.GetFullName();
            var type = symbol.Name;

            var result = (@namespace != null)
                ? $"{@namespace}.{type}"
                : type;

            if (symbol is INamedTypeSymbol namedType)
            {
                if (namedType.IsGenericType)
                {
                    result += "<???>";
                }
            }

            return result;
        }
    }
}
