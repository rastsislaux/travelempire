using TravelEmpire.Simulation.Content;

namespace TravelEmpire.Simulation;

/// <summary>Discovers map packs from disk and falls back to embedded built-ins.</summary>
public static class MapPackRegistry
{
    public static IReadOnlyList<MapPackInfo> ListPacks(string? contentRoot = null)
    {
        var packs = new List<MapPackInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pack in EnumerateDiskPacks(contentRoot))
        {
            if (!seen.Add(pack.Id)) continue;
            packs.Add(new MapPackInfo
            {
                Id = pack.Id,
                Name = pack.Name,
                CityCount = pack.Cities.Count,
                IsTutorial = pack.Id.Equals("aurelia", StringComparison.OrdinalIgnoreCase)
            });
        }

        foreach (var pack in BuiltInContent.AllMapPacks())
        {
            if (!seen.Add(pack.Id)) continue;
            packs.Add(new MapPackInfo
            {
                Id = pack.Id,
                Name = pack.Name,
                CityCount = pack.Cities.Count,
                IsTutorial = pack.Id.Equals("aurelia", StringComparison.OrdinalIgnoreCase)
            });
        }

        return packs.OrderByDescending(p => p.IsTutorial).ThenBy(p => p.Name).ToList();
    }

    public static MapPack Load(string packId, string? contentRoot = null)
    {
        foreach (var pack in EnumerateDiskPacks(contentRoot))
        {
            if (pack.Id.Equals(packId, StringComparison.OrdinalIgnoreCase))
                return pack;
        }

        foreach (var pack in BuiltInContent.AllMapPacks())
        {
            if (pack.Id.Equals(packId, StringComparison.OrdinalIgnoreCase))
                return pack;
        }

        throw new KeyNotFoundException($"Unknown map pack '{packId}'.");
    }

    public static string? ResolveContentRoot(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            var maps = Path.Combine(candidate, "maps");
            if (Directory.Exists(maps))
                return Path.GetFullPath(candidate);
        }

        return null;
    }

    private static IEnumerable<MapPack> EnumerateDiskPacks(string? contentRoot)
    {
        if (string.IsNullOrWhiteSpace(contentRoot))
            yield break;

        var mapsRoot = Path.Combine(contentRoot, "maps");
        if (!Directory.Exists(mapsRoot))
            yield break;

        foreach (var dir in Directory.EnumerateDirectories(mapsRoot))
        {
            var name = Path.GetFileName(dir);
            if (name.StartsWith('_') || name.StartsWith('.'))
                continue;
            var packFile = Path.Combine(dir, "pack.json");
            var citiesFile = Path.Combine(dir, "cities.json");
            if (!File.Exists(packFile) || !File.Exists(citiesFile))
                continue;

            MapPack pack;
            try
            {
                pack = ContentLoader.LoadMapPack(dir);
            }
            catch
            {
                continue;
            }

            yield return pack;
        }
    }
}
