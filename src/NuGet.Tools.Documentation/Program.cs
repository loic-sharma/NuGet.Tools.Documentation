using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using NuGet.Packaging;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
            // TODO REMOVE
            path = new FileInfo("C:\\Users\\sharm\\Desktop\\SourceLinkDemo.GoodDependency.dll");
            var assemblyInfo2 = GetAssemblyInfo(path.OpenRead());
            var json2 = JsonConvert.SerializeObject(assemblyInfo2, Formatting.Indented, SerializationSettings);
            Console.WriteLine(json2);
            return;

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
                                // GetAssemblyInfo - Uses System.Metadata.Reflection like symbol server. Low level and harder to work with.
                                var assemblyInfo = GetAssemblyInfo(assemblyStream);
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
        /// Get the assembly's information using System.Reflection.Metadata.
        /// </summary>
        /// <param name="assemblyStream"></param>
        /// <returns></returns>
        private static AssemblyInfo GetAssemblyInfo(FileStream assemblyStream)
        {
            using (var peReader = new PEReader(assemblyStream))
            {
                // TODO: Skip PDB if not source link
                // SEE: https://github.com/dotnet/roslyn/blob/master/src/Compilers/CSharp/Test/Emit/PDB/PortablePdbTests.cs#L320

                // TODO: Get sequence points
                // SEE: https://github.com/dotnet/roslyn/blob/master/src/Compilers/CSharp/Test/Emit/PDB/PortablePdbTests.cs#L24
                peReader.TryOpenAssociatedPortablePdb(assemblyStream.Name, File.OpenRead, out var pdbReaderProvider, out var pdbPath);

                var peReader2 = peReader.GetMetadataReader();
                var pdbReader = pdbReaderProvider.GetMetadataReader();

                var sourceLinkBytes = pdbReader
                    .GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                    .Select(handle => pdbReader.GetCustomDebugInformation(handle))
                    .Where(info => pdbReader.GetGuid(info.Kind) == new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A"))
                    .Select(info => pdbReader.GetBlobBytes(info.Value))
                    .FirstOrDefault();

                SourceLinkDocument sourceLinkDocument = null;

                if (sourceLinkBytes != null)
                {
                    // TODO: SourceLink validation:
                    // See: https://github.com/dotnet/sourcelink/blob/master/src/SourceLink.Common/GenerateSourceLinkFile.cs#L44
                    using (var stream = new MemoryStream(sourceLinkBytes))
                    using (var reader = new StreamReader(stream))
                    {
                        sourceLinkDocument = (SourceLinkDocument)new JsonSerializer().Deserialize(reader, typeof(SourceLinkDocument));
                    }
                }

                foreach (var methodHandle in peReader2.MethodDefinitions)
                {
                    var method = peReader2.GetMethodDefinition(methodHandle);
                    var methodDebugInfo = pdbReader.GetMethodDebugInformation(methodHandle);

                    var name = peReader2.GetString(method.Name);

                    var document = pdbReader.GetDocument(methodDebugInfo.Document);
                    var fileName = pdbReader.GetString(document.Name);

                    var points = methodDebugInfo.GetSequencePoints();
                    var startLine = int.MaxValue;
                    var endLine = int.MinValue;

                    foreach (var point in points)
                    {
                        if (point.IsHidden) continue;
                        if (point.StartLine < startLine) startLine =  point.StartLine;
                        if (point.EndLine > endLine) endLine =  point.EndLine;
                    }

                    string sourcePath = string.Empty;
                    foreach (var link in sourceLinkDocument.Documents)
                    {
                        // TODO, don't use regex? If use, add timeout?
                        var from = $"^{Regex.Escape(link.Key).Replace("\\*", "(.*)")}$";
                        var to = link.Value.Replace("*", "$1");

                        var result = Regex.Replace(fileName, from, to);
                        if (result != fileName)
                        {
                            sourcePath = result;
                            break;
                        }
                    }

                    // Check for empty sourcePath
                    Console.WriteLine($"{name}: {sourcePath}#L{startLine}-L{endLine}");
                }

                if (!peReader.HasMetadata) throw new Exception("??");

                return peReader
                    .GetMetadataReader()
                    .GetAssemblyInfo()
                    .FilterNonPublic();
            }
        }
    }

    internal class SourceLinkDocument
    {
        public Dictionary<string, string> Documents {get; set; }
    }
}
