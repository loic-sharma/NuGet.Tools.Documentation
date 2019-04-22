using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace NuGet.Tools.Documentation
{
    /// <summary>
    /// Extensions to <see cref="MetadataReader"/>
    /// </summary>
    public static class MetadataReaderExtensions
    {
        /// <summary>
        /// Get the information for an assembly.
        /// </summary>
        /// <param name="reader">The metadata reader.</param>
        /// <returns>The assembly's information.</returns>
        public static AssemblyInfo GetAssemblyInfo(this MetadataReader reader)
        {
            return new AssemblyInfo
            {
                Types = reader.GetCollection(reader.TypeDefinitions, GetTypeInfo)
            };
        }
   
        /// <summary>
        /// Get the information for a type.
        /// </summary>
        /// <param name="reader">The metadata reader.</param>
        /// <param name="handle">The type definition to read.</param>
        /// <returns>The type's information.</returns>
        public static TypeInfo GetTypeInfo(this MetadataReader reader, TypeDefinitionHandle handle)
        {
            var typeDefinition = reader.GetTypeDefinition(handle);

            var fieldDefinitions = typeDefinition.GetFields();
            var methodDefinitions = typeDefinition.GetMethods();
            var propertyDefinitions = typeDefinition.GetProperties();

            return new TypeInfo
            {
                Name = reader.GetString(typeDefinition.Name),
                Namespace = reader.GetString(typeDefinition.Namespace),

                Attributes = typeDefinition.Attributes,

                Fields = reader.GetCollection(fieldDefinitions, GetFieldInfo),
                Methods = reader.GetCollection(methodDefinitions, GetMethodInfo),
                Properties = reader.GetCollection(propertyDefinitions, GetPropertyInfo),
            };
        }

        /// <summary>
        /// Get the information for a field.
        /// </summary>
        /// <param name="reader">The metadata reader.</param>
        /// <param name="handle">The field definition to read.</param>
        /// <returns>The field's information.</returns>
        public static FieldInfo GetFieldInfo(this MetadataReader reader, FieldDefinitionHandle handle)
        {
            var fieldDefinition = reader.GetFieldDefinition(handle);

            return new FieldInfo
            {
                Name = reader.GetString(fieldDefinition.Name),
                Type = fieldDefinition.DecodeSignature(new SignatureTypeProvider(reader), null),

                Attributes = fieldDefinition.Attributes,
            };
        }

        /// <summary>
        /// Get the information for a method.
        /// </summary>
        /// <param name="reader">The metadata reader.</param>
        /// <param name="handle">The method definition to read.</param>
        /// <returns>The method's information.</returns>
        public static MethodInfo GetMethodInfo(this MetadataReader reader, MethodDefinitionHandle handle)
        {
            var methodDefinition = reader.GetMethodDefinition(handle);
            var signature = methodDefinition.DecodeSignature(new SignatureTypeProvider(reader), null);

            return new MethodInfo
            {
                Name = reader.GetString(methodDefinition.Name),

                ReturnType = signature.ReturnType,
                ParameterTypes = signature.ParameterTypes.ToList(),

                Attributes = methodDefinition.Attributes,
            };
        }

        /// <summary>
        /// Get the information for a property.
        /// </summary>
        /// <param name="reader">The metadata reader.</param>
        /// <param name="handle">The property definition to read.</param>
        /// <returns>The property's information.</returns>
        public static PropertyInfo GetPropertyInfo(this MetadataReader reader, PropertyDefinitionHandle handle)
        {
            var propertyDefinition = reader.GetPropertyDefinition(handle);
            var accessors = propertyDefinition.GetAccessors();
            var signature = propertyDefinition.DecodeSignature(new SignatureTypeProvider(reader), null);

            return new PropertyInfo
            {
                Name = reader.GetString(propertyDefinition.Name),

                Type = signature.ReturnType,

                HasGetter = !accessors.Getter.IsNil,
                HasSetter = !accessors.Setter.IsNil,

                Attributes = propertyDefinition.Attributes,
            };
        }

        private static IReadOnlyList<TResult> GetCollection<TInput, TResult>(
            this MetadataReader reader,
            IReadOnlyCollection<TInput> collection,
            Func<MetadataReader, TInput, TResult> map)
        {
            return collection.Select(item => map(reader, item)).ToList();
        }
    }

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
