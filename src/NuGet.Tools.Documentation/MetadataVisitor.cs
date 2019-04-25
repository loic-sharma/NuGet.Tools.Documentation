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
                Namespace = symbol.ContainingNamespace.Name,

                Fields = members.OfType<FieldInfo>().ToList(),
                Methods = members.OfType<MethodInfo>().ToList(),
                Properties = members.OfType<PropertyInfo>().ToList()
            };
        }

        public override object VisitField(IFieldSymbol symbol)
        {
            return new FieldInfo
            {
                Name = symbol.Name
            };
        }

        public override object VisitMethod(IMethodSymbol symbol)
        {
            return new MethodInfo
            {
                Name = symbol.Name,
            };
        }

        public override object VisitProperty(IPropertySymbol symbol)
        {
            return new PropertyInfo
            {
                Name = symbol.Name
            };
        }
    }
}
