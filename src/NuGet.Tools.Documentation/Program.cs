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
                                // There are two implementations:
                                // 1. GetAssemblyInfo - Uses Roslyn like DocFX does. This is easier to work with and higher level.
                                // 2. GetAssembly2Info - Uses System.Metadata.Reflection like symbol server. Low level and harder to work with.
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

        /// <summary>
        /// Gets the assembly's information using Roslyn APIs.
        /// </summary>
        /// <param name="assemblyStream"></param>
        /// <returns></returns>
        private static AssemblyInfo GetAssemblyInfo(FileStream assemblyStream)
        {
            // See: https://github.com/dotnet/docfx/blob/dev/src/Microsoft.DocAsCode.Metadata.ManagedReference/ExtractMetadata/CompilationUtility.cs#L60
            // See: https://github.com/dotnet/docfx/blob/dev/src/Microsoft.DocAsCode.Metadata.ManagedReference/ExtractMetadata/CompilationUtility.cs#L80
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var reference = MetadataReference.CreateFromStream(assemblyStream);
            var compilation = CSharpCompilation.Create("HelloWorld", new SyntaxTree[0], new[] { reference }, options);

            var visitor = new MetadataVisitor();
            var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference);

            return (AssemblyInfo)assemblySymbol.Accept(visitor);
        }

        /// <summary>
        /// Get the assembly's information using System.Reflection.Metadata.
        /// </summary>
        /// <param name="assemblyStream"></param>
        /// <returns></returns>
        private static AssemblyInfo GetAssemblyInfo2(FileStream assemblyStream)
        {
            using (var peReader = new PEReader(assemblyStream))
            {
                if (!peReader.HasMetadata) throw new Exception("??");

                var assemblyInfo = peReader
                    .GetMetadataReader()
                    .GetAssemblyInfo();

                return assemblyInfo.FilterNonPublic();
            }
        }
    }
}
