using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;

namespace NuGet.Tools.Documentation
{
    public class AssemblyInfo
    {
        public IReadOnlyList<TypeInfo> Types { get; set; }
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
