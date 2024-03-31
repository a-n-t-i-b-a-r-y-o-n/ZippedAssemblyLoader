using System.Reflection;

namespace ZippedAssemblyLoader;

/// <summary>
/// Static assembly loading context for loading Assemblies from zip archives
/// </summary>
public static class ZippedAssemblyLoadContext
{
    /// <summary>
    /// Gets the list of registered assembly loaders
    /// </summary>
    private static List<ZippedAssemblyLoader> RegisteredLoaders { get; } = [];

    /// <summary>
    /// Registers an archive file for library resolution and loads an Assembly from it
    /// </summary>
    /// <param name="assemblyFilename">The filename of the Assembly library within the archive</param>
    /// <param name="archiveFilename">The filename of the archive containing the Assembly</param>
    /// <returns>The Assembly, if it could be loaded</returns>
    public static Assembly? Load(string assemblyFilename, string archiveFilename)
    {
        var loader = new ZippedAssemblyLoader(archiveFilename);
        loader.Register();
        RegisteredLoaders.Add(loader);
        
        if (loader.LoadAssemblyByFilename(assemblyFilename) is { } assembly)
        {
            return assembly;
        }

        return null;
    }

    /// <summary>
    /// Registers an archive for library resolution and loads an Assembly from it
    /// </summary>
    /// <param name="assemblyName">The Assembly's <see cref="AssemblyName"/></param>
    /// <param name="archiveFilename">The filename of the archive containing the Assembly</param>
    /// <returns>The Assembly, if it could be loaded</returns>
    public static Assembly? Load(AssemblyName assemblyName, string archiveFilename)
    {
        var loader = new ZippedAssemblyLoader(archiveFilename);
        loader.Register();
        RegisteredLoaders.Add(loader);
        
        if (loader.LoadAssemblyByName(assemblyName) is { } assembly)
        {
            return assembly;
        }

        return null;
    }

    /// <summary>
    /// Removes a loaded archive file from import resolution registration
    /// </summary>
    /// <param name="archiveFilename">The filename of the archive containing Assemblies</param>
    public static void UnregisterArchive(string archiveFilename)
    {
        if (RegisteredLoaders.FirstOrDefault(l => l.ArchiveFilename == archiveFilename) is { } loader)
        {
            loader.Unregister();
            RegisteredLoaders.Remove(loader);
        }
    }

    /// <summary>
    /// Removes all registered zip files from import resolution registration
    /// </summary>
    public static void UnregisterAllArchives()
    {
        foreach (var loader in RegisteredLoaders)
        {
            loader.Unregister();
        }

        RegisteredLoaders.Clear();
    }
}