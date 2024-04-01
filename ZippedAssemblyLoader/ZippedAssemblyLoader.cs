using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ZippedAssemblyLoader;

/// <summary>
/// Loader class for Assembly files inside zip files
/// </summary>
public sealed class ZippedAssemblyLoader
{
    /// <summary>
    /// Loader instance archive filename
    /// </summary>
    public string ArchiveFilename { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZippedAssemblyLoader"/> class
    /// </summary>
    /// <param name="archiveFilename">The archive filename to load assemblies from</param>
    internal ZippedAssemblyLoader(string archiveFilename)
    {
        ArchiveFilename = archiveFilename;
    }

    /// <summary>
    /// Load an assembly from the archive file by filename
    /// </summary>
    /// <param name="assemblyFilename">The filename of the assembly to load</param>
    /// <returns>The loaded Assembly, if it exists and could be loaded</returns>
    internal Assembly? LoadAssemblyByFilename(string assemblyFilename)
    {
        using var archive = ZipFile.OpenRead(ArchiveFilename);
        if (archive.Entries.FirstOrDefault(e => e.Name == assemblyFilename) is { } entry)
        {
            return LoadZippedAssembly(entry);
        }

        return null;
    }

    /// <summary>
    /// Load an assembly from the archive file by its <see cref="AssemblyName"/>
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to load</param>
    /// <returns>The loaded Assembly, if it exists and could be loaded</returns>
    internal Assembly? LoadAssemblyByName(AssemblyName assemblyName)
    {
        // Open archive into memory and iterate its entries
        using var archive = ZipFile.OpenRead(ArchiveFilename);
        foreach (var entry in archive.Entries.Where(e => e.Name.EndsWith(".dll")))
        {
            // Check if this is the correct Assembly
            if (GetZippedAssemblyName(entry) == assemblyName)
            {
                // Return the loaded Assembly
                return LoadZippedAssembly(entry);
            }
        }

        return null;
    }

    /// <summary>
    /// Load an assembly from the archive file by its Assembly name string
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to load</param>
    /// <returns>The loaded Assembly, if it exists and could be loaded</returns>
    internal Assembly? LoadAssemblyByName(string assemblyName)
    {
        // Open archive into memory and iterate its entries
        using var archive = ZipFile.OpenRead(ArchiveFilename);
        foreach (var entry in archive.Entries.Where(e => e.Name.EndsWith(".dll")))
        {
            // Check if this is the correct Assembly
            var name = GetZippedAssemblyName(entry);
            if (name?.FullName == assemblyName || name?.Name == assemblyName)
            {
                // Return the loaded Assembly
                return LoadZippedAssembly(entry);
            }
        }

        return null;
    }

    /// <summary>
    /// Register the assembly loader with the AppDomain
    /// </summary>
    internal void Register()
    {
        AppDomain.CurrentDomain.AssemblyResolve += ZipFileResolveEventHandler;
    }

    /// <summary>
    /// Unregister the assembly loader from the AppDomain
    /// </summary>
    internal void Unregister()
    {
        AppDomain.CurrentDomain.AssemblyResolve -= ZipFileResolveEventHandler;
    }

    /// <summary>
    /// Event handler callback invoked on typical assembly dependency resolution failure.
    /// Implemented on each <see cref="ZippedAssemblyLoader"/> to allow resolving dependencies from assemblies in the archive file
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="args">The dependency resolution event args</param>
    /// <returns>The resolved dependent Assembly, if it could be found</returns>
    private Assembly? ZipFileResolveEventHandler(object? sender, ResolveEventArgs args)
    {
        return LoadAssemblyByName(args.Name);
    }

    /// <summary>
    /// Load an Assembly from a zip archive entry
    /// </summary>
    /// <param name="entry">The zip archive entry</param>
    /// <returns>The loaded Assembly</returns>
    private Assembly LoadZippedAssembly(ZipArchiveEntry entry)
    {
        // Read the assembly into memory
        var assemblyBytes = new byte[entry.Length];
        entry.Open().ReadExactly(assemblyBytes);
        
        // Load the assembly from memory
        var assembly = Assembly.Load(assemblyBytes);
        
        // Set the assembly's native library resolver
        NativeLibrary.SetDllImportResolver(assembly, ResolveZippedNativeLibrary);

        return assembly;
    }

    /// <summary>
    /// Get the <see cref="AssemblyName"/> of a zipped Assembly without loading it
    /// </summary>
    /// <param name="entry">The zip archive entry</param>
    /// <returns>The AssemblyName, if it could be loaded</returns>
    private AssemblyName? GetZippedAssemblyName(ZipArchiveEntry entry)
    {
        // Get temporary filename
        var temporaryFile = Path.GetTempFileName();

        // Get the assembly name
        try
        {
            // Remove auto-created temp file
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }

            // Extract the library to temp file
            entry.ExtractToFile(temporaryFile);

            // Get AssemblyName from temp file
            return AssemblyName.GetAssemblyName(temporaryFile);
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            // Clean up temporary file
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }
        }
    }

    /// <inheritdoc cref="System.Runtime.InteropServices.DllImportResolver"/>>
    private IntPtr ResolveZippedNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // Open archive into memory
        using var archive = ZipFile.OpenRead(ArchiveFilename);

        var candidates = new List<ZipArchiveEntry>();

        // Identify candidate entries in the archive
        foreach (var entry in archive.Entries.Where(e => e.Name.Contains(libraryName)))
        {
            candidates.Add(entry);
        }

        // Check if we found any candidates for loading
        if (candidates.Count == 0)
        {
            return IntPtr.Zero;
        }

        // If any candidate's path contains our RID, load it. Otherwise, just take the first one.
        if (candidates.FirstOrDefault(c => c.FullName.Contains(RuntimeInformation.RuntimeIdentifier)) is not { } candidate)
        {
            candidate = candidates.First();
        }

        // Get temporary filename
        var temporaryFile = Path.GetTempFileName();

        // Load the native library
        try
        {
            // Remove auto-created temp file
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }

            // Extract the library to temp file
            candidate.ExtractToFile(temporaryFile);

            // Load library from temp file
            return NativeLibrary.Load(temporaryFile, assembly, searchPath);
        }
        catch (Exception)
        {
            return IntPtr.Zero;
        }
        finally
        {
            // Clean up temporary file
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }
        }
    }
}