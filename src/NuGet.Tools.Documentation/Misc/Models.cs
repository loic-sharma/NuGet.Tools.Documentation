using Newtonsoft.Json;
using NuGet.Tools.Documentation;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace NuGet.Tools.Documentation
{
    public class AssemblyInfo
    {
        public IReadOnlyList<TypeInfo> Types { get; set; }

        public AssemblyInfo FilterNonPublic()
        {
            return new AssemblyInfo
            {
                Types = FilterNonPublic(Types)
            };
        }

        private static IReadOnlyList<TypeInfo> FilterNonPublic(IReadOnlyList<TypeInfo> types)
        {
            return types
                .Where(t => t.Attributes.HasFlag(TypeAttributes.Public))
                .Where(t => !t.Attributes.HasFlag(TypeAttributes.NestedAssembly))
                .Where(t => !t.Attributes.HasFlag(TypeAttributes.NestedPrivate))
                .Select(t => new TypeInfo
                {
                    Name = t.Name,
                    Namespace = t.Namespace,

                    Attributes = t.Attributes,

                    Fields = FilterNonPublic(t.Fields),
                    Methods = FilterNonPublic(t.Methods),
                    Properties = FilterNonPublic(t.Properties),
                })
                .ToList();
        }

        private static IReadOnlyList<FieldInfo> FilterNonPublic(IReadOnlyList<FieldInfo> fields)
        {
            return fields
                .Where(f => f.Attributes.HasFlag(FieldAttributes.Public))
                .Where(f => !f.Attributes.HasFlag(FieldAttributes.SpecialName))
                .ToList();
        }

        private static IReadOnlyList<MethodInfo> FilterNonPublic(IReadOnlyList<MethodInfo> methods)
        {
            return methods
                .Where(m => m.Attributes.HasFlag(MethodAttributes.Public))
                .Where(m => !m.Attributes.HasFlag(MethodAttributes.PrivateScope))
                .ToList();
        }

        private static IReadOnlyList<PropertyInfo> FilterNonPublic(IReadOnlyList<PropertyInfo> properties)
        {
            return properties;
            //return properties.Where(p => (p.Attributes & PropertyAttributes.) != 0).ToList();
        }
    }

    /// <summary>
    /// See <see cref="TypeDefinition"/>
    /// </summary>
    public class TypeInfo
    {
        public string Name { get; set; }
        public string Namespace { get; set; }

        [JsonConverter(typeof(FlagConverter))]
        public TypeAttributes Attributes { get; set; }

        public IReadOnlyList<FieldInfo> Fields { get; set; }
        public IReadOnlyList<MethodInfo> Methods { get; set; }
        public IReadOnlyList<PropertyInfo> Properties { get; set; }

        // Custom Attributes
        // Base type
        // Events
        // Generic parameters
    }

    /// <summary>
    /// See <see cref="FieldDefinition"/>
    /// </summary>
    public class FieldInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }

        [JsonConverter(typeof(FlagConverter))]
        public FieldAttributes Attributes { get; set; }

        // Type
        // Custom attributes
        // Default value
    }

    /// <summary>
    /// See <see cref="MethodDefinition"/>
    /// </summary>
    public class MethodInfo
    {
        public string Name { get; set; }

        public string ReturnType { get; set; }
        public IReadOnlyList<string> ParameterTypes { get; set; }

        [JsonConverter(typeof(FlagConverter))]
        public MethodAttributes Attributes { get; set; }
    }

    /// <summary>
    /// See <see cref="PropertyDefinition"/>
    /// </summary>
    public class PropertyInfo
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }

        [JsonConverter(typeof(FlagConverter))]
        public PropertyAttributes Attributes { get; set; }

        // Getter
        // Setter
        // Default value
        // Custom attributes
    }
}
