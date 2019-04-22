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

                        foreach (var item in group.Items.Where(IsLibrary))
                        {
                            using (var libraryStream = await reader.GetStream(item).AsTemporaryFileStreamAsync())
                            using (var peReader = new PEReader(libraryStream))
                            {
                                // TODO:
                                if (!peReader.HasMetadata) continue;
                                
                                var metadataReader = peReader.GetMetadataReader();
                                var assemblyInfo = metadataReader.GetAssemblyInfo();

                                var json = JsonConvert.SerializeObject(assemblyInfo, Formatting.Indented);

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

        private static bool IsLibrary(string path)
        {
            return (Path.GetExtension(path) == ".dll");
        }
    }
}
