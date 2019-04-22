using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace NuGet.Tools.Documentation
{
    // This is forked from: https://github.com/dotnet/metadata-tools/blob/04a483752c19eb1a28ab0642038ae7c2b7b2cdac/src/Microsoft.Metadata.Visualizer/MetadataVisualizer.SignatureVisualizer.cs#L13
    // Also interesting: https://github.com/tunnelvisionlabs/dotnet-compatibility/pull/24/files
    internal sealed class SignatureTypeProvider : ISignatureTypeProvider<string, object>
    {
        private readonly MetadataReader _reader;

        public SignatureTypeProvider(MetadataReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Boolean: return "bool";
                case PrimitiveTypeCode.Byte: return "uint8";
                case PrimitiveTypeCode.Char: return "char";
                case PrimitiveTypeCode.Double: return "float64";
                case PrimitiveTypeCode.Int16: return "int16";
                case PrimitiveTypeCode.Int32: return "int32";
                case PrimitiveTypeCode.Int64: return "int64";
                case PrimitiveTypeCode.IntPtr: return "native int";
                case PrimitiveTypeCode.Object: return "object";
                case PrimitiveTypeCode.SByte: return "int8";
                case PrimitiveTypeCode.Single: return "float32";
                case PrimitiveTypeCode.String: return "string";
                case PrimitiveTypeCode.TypedReference: return "typedref";
                case PrimitiveTypeCode.UInt16: return "uint16";
                case PrimitiveTypeCode.UInt32: return "uint32";
                case PrimitiveTypeCode.UInt64: return "uint64";
                case PrimitiveTypeCode.UIntPtr: return "native uint";
                case PrimitiveTypeCode.Void: return "void";
                default: return "<bad metadata>";
            }
        }

        // Original:
        // return $"typedef{RowId(handle)}";
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind = 0)
        {
            var typeDefinition = reader.GetTypeDefinition(handle);

            var @namespace = reader.GetString(typeDefinition.Namespace);
            var name = reader.GetString(typeDefinition.Name);

            return $"{@namespace}.{name}";
        }

        // Original:
        // Return $"typeref{RowId(handle)}";
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind = 0)
        {
            var typeReference = reader.GetTypeReference(handle);

            var @namespace = reader.GetString(typeReference.Name);
            var name = reader.GetString(typeReference.Name);

            return $"{@namespace}.{name}";
        }

        // Original:
        // Return $"typespec{RowId(handle)}";
        public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind = 0)
        {
            return $"typespec{RowId(handle)}";
        }
            

        public string GetSZArrayType(string elementType) =>
            elementType + "[]";

        public string GetPointerType(string elementType)
            => elementType + "*";

        public string GetByReferenceType(string elementType)
            => elementType + "&";

        public string GetGenericMethodParameter(object genericContext, int index)
            => "!!" + index;

        public string GetGenericTypeParameter(object genericContext, int index)
            => "!" + index;

        public string GetPinnedType(string elementType)
            => elementType + " pinned";

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
            => genericType + "<" + string.Join(",", typeArguments) + ">";

        public string GetModifiedType(string modifierType, string unmodifiedType, bool isRequired) =>
            unmodifiedType + (isRequired ? " modreq(" : " modopt(") + modifierType + ")";

        public string GetArrayType(string elementType, ArrayShape shape)
        {
            var builder = new StringBuilder();

            builder.Append(elementType);
            builder.Append('[');

            for (int i = 0; i < shape.Rank; i++)
            {
                int lowerBound = 0;

                if (i < shape.LowerBounds.Length)
                {
                    lowerBound = shape.LowerBounds[i];
                    builder.Append(lowerBound);
                }

                builder.Append("...");

                if (i < shape.Sizes.Length)
                {
                    builder.Append(lowerBound + shape.Sizes[i] - 1);
                }

                if (i < shape.Rank - 1)
                {
                    builder.Append(',');
                }
            }

            builder.Append(']');
            return builder.ToString();
        }

        public string GetFunctionPointerType(MethodSignature<string> signature)
            => $"methodptr({MethodSignature(signature)})";

        // Forked from: https://github.com/dotnet/metadata-tools/blob/04a483752c19eb1a28ab0642038ae7c2b7b2cdac/src/Microsoft.Metadata.Visualizer/MetadataVisualizer.cs#L666
        // They use an extension method that I inlined here.
        private string RowId(EntityHandle handle)
            => handle.IsNil ? "nil" : $"#{MetadataTokens.GetRowNumber(_reader, handle):x}";

        // Copied from: https://github.com/dotnet/metadata-tools/blob/04a483752c19eb1a28ab0642038ae7c2b7b2cdac/src/Microsoft.Metadata.Visualizer/MetadataVisualizer.cs#L461
        private static string MethodSignature(MethodSignature<string> signature)
        {
            var builder = new StringBuilder();
            builder.Append(signature.ReturnType);
            builder.Append(' ');
            builder.Append('(');

            for (int i = 0; i < signature.ParameterTypes.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");

                    if (i == signature.RequiredParameterCount)
                    {
                        builder.Append("... ");
                    }
                }

                builder.Append(signature.ParameterTypes[i]);
            }

            builder.Append(')');
            return builder.ToString();
        }

    }
}
