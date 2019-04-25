using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Tools.Documentation
{
    /// <summary>
    /// The entry point to generate the documentation for a NuGet package.
    /// </summary>
    public class Program
    {
        private static readonly JsonSerializerSettings SerializationSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// Generate the documentation for a NuGet package.
        /// </summary>
        /// <param name="path">The path to the NuGet package</param>
        /// <param name="cancellationToken">A token to cancel the task.</param>
        public static async Task Main(FileInfo path, CancellationToken cancellationToken)
        {
            if (path == null || path.Extension != ".nupkg" || !path.Exists)
            {
                Console.WriteLine("Please provide a --path option to a NuGet package.");
                return;
            }

            try
            {

                using (var reader = new PackageArchiveReader(path.OpenRead()))
                {
                    var groups = await reader.GetLibItemsAsync(cancellationToken);

                    foreach (var group in groups)
                    {
                        Console.WriteLine($"Target framework: {group.TargetFramework}");
                        Console.WriteLine();

                        foreach (var item in group.Items.Where(i => Path.GetExtension(i) == ".dll"))
                        {
                            using (var assemblyStream = await reader.GetStream(item).AsTemporaryFileStreamAsync(cancellationToken))
                            {
                                var assemblyInfo = GetAssemblyInfo(assemblyStream);
                                //var assemblyInfo = GetAssemblyInfo2(assemblyStream);
                                var json = JsonConvert.SerializeObject(assemblyInfo, Formatting.Indented, SerializationSettings);

                                Console.WriteLine(json);
                            }
                        }

                        Console.WriteLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not open NuGet package: {e}");
            }
        }

        #region Roslyn
        private static AssemblyInfo GetAssemblyInfo(FileStream assemblyStream)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var reference = MetadataReference.CreateFromStream(assemblyStream);
            var compilation = CSharpCompilation.Create("HelloWorld", new SyntaxTree[0], new[] { reference }, options);

            var visitor = new MetadataVisitor();
            var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference);

            return (AssemblyInfo)assemblySymbol.Accept(visitor);
        }
        #endregion

        #region System.Reflection.Metadata

        private static AssemblyInfo GetAssemblyInfo2(FileStream assemblyStream)
        {
            using (var peReader = new PEReader(assemblyStream))
            {
                if (!peReader.HasMetadata) throw new Exception("??");

                var assemblyInfo = peReader
                    .GetMetadataReader()
                    .GetAssemblyInfo();

                return FilterNonPublic(assemblyInfo);
            }
        }

        #endregion

        private static AssemblyInfo FilterNonPublic(AssemblyInfo assemblyInfo)
        {
            return new AssemblyInfo
            {
                Types = FilterNonPublic(assemblyInfo.Types)
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
}
