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
                if (!peReader.HasMetadata) throw new Exception("??");

                return peReader
                    .GetMetadataReader()
                    .GetAssemblyInfo()
                    .FilterNonPublic();
            }
        }
    }
}
