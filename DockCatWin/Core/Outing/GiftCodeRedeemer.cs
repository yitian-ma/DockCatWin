using System.Security.Cryptography;
using System.Text;

namespace DockCatWin.Core.Outing;

public sealed class GiftCodeRedeemer
{
    private const int RequiredCodeLength = 6;
    private static readonly string Pepper = string.Join("", ["Dock", "Cat", ":gift-code:", "v1", ":standard-collectables"]);

    private readonly Dictionary<string, string> entriesByHash;
    private readonly Dictionary<string, string> hiddenEntriesByCode;

    public GiftCodeRedeemer()
        : this(StandardEntries, HiddenEntriesByCode)
    {
    }

    public GiftCodeRedeemer(
        IEnumerable<GiftCodeEntry> entries,
        IReadOnlyDictionary<string, string> hiddenEntriesByCode)
    {
        entriesByHash = entries
            .GroupBy(entry => entry.Hash, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().CollectableID, StringComparer.OrdinalIgnoreCase);
        this.hiddenEntriesByCode = hiddenEntriesByCode.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    public string? CollectableIDFor(string rawCode, OutingCatalog catalog)
    {
        var normalizedAnyLengthCode = NormalizedAnyLengthCode(rawCode);
        if (normalizedAnyLengthCode is null)
        {
            return null;
        }

        if (hiddenEntriesByCode.TryGetValue(normalizedAnyLengthCode, out var hiddenCollectableID)
            && catalog.Collectables.Any(item => item.Id == hiddenCollectableID))
        {
            return hiddenCollectableID;
        }

        var normalizedCode = NormalizedCode(normalizedAnyLengthCode);
        if (normalizedCode is null)
        {
            return null;
        }

        var hash = Hash(normalizedCode);
        return entriesByHash.TryGetValue(hash, out var collectableID)
            && catalog.Collectables.Any(item => item.Id == collectableID && item.IsStandardRarity)
            ? collectableID
            : null;
    }

    public static string? NormalizedCode(string rawCode)
    {
        var normalized = NormalizedAnyLengthCode(rawCode);
        return normalized?.Length == RequiredCodeLength ? normalized : null;
    }

    public static string? NormalizedAnyLengthCode(string rawCode)
    {
        var normalized = new string(rawCode
            .ToUpperInvariant()
            .Where(character => !char.IsWhiteSpace(character))
            .ToArray());
        return normalized.Length == 0 ? null : normalized;
    }

    public static string Hash(string normalizedCode)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{Pepper}:{normalizedCode}"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static readonly GiftCodeEntry[] StandardEntries =
    [
        new(["ff3cc5ecb1249807", "03b47dc2223f0661", "47a6c22196eee0a1", "31e7410e2cb02a73"], ["le", "af"]),
        new(["9d4f6a1ada0b0382", "0770cdc11be34ae6", "5d98e8bfad92447f", "12955243648d6c07"], ["pe", "bble"]),
        new(["28ccd9ba4d6011ed", "89b2c7de55aae700", "cd9fe14ca077f62a", "97184c96712fcea4"], ["small_", "feather"]),
        new(["49fba80fa6d17ffd", "2e2e1b72f2bb1489", "fee98b4d7af125b3", "c76e677746b7edaa"], ["pretty_", "button"]),
        new(["18649238ce2e20db", "1017877494c0b63b", "e869fdfa032c4c93", "252c3ed4eeb44f73"], ["small_", "pinecone"]),
        new(["578292210e1ad5b1", "572edda6036a26c3", "fc263c964bc59b05", "32032cf6de8fab43"], ["tw", "ig"]),
        new(["b3da4f11cc6d6bff", "fd9210ce45490410", "c0e7b2c96b5718f9", "46ecd97f5ee77312"], ["fly", "er"]),
        new(["40471ba6b003ae0f", "5d6c2b99eff648e2", "7037777ad9913b4d", "946b4d319008d32c"], ["colorful_", "bottle_cap"]),
        new(["a9d2a521e2b828be", "0013da92ac4eb5b5", "089f176c0d30f649", "07deef83257cf875"], ["shell_", "chip"]),
        new(["8160b487a6ff6a51", "379062394af0b755", "f69816f73750b2e9", "b745972157d8eef8"], ["mar", "ble"]),
        new(["6b72191d09f16911", "295d87d94df8051e", "5587eee61719ec8a", "02de13f5f6efd93a"], ["patterned_", "ribbon"]),
        new(["86c74544851623af", "588ddcfb321de703", "013544287697afa7", "90ceb3fd64284236"], ["wooden_", "whistle"]),
        new(["10c819f36ab56e15", "effeb07c30433d4c", "91d98a8a859dde15", "87b6725c980a6918"], ["dried_", "flower"]),
        new(["39335d96f2386ea8", "3f59bf458adbe447", "6d133ec2f8fb6955", "816442e684b8b92f"], ["toy_", "fish"]),
        new(["e1007e49bf135a05", "d06462a735058b5b", "4475c56c36e81477", "9043103eb9db3014"], ["engraved_", "bell"]),
        new(["3e73eb8ea57f54ad", "dc95d16fea3ee550", "e81be6981983dfd1", "98c7d7fb27236aa7"], ["pin", "wheel"]),
        new(["0dd437e5b879464f", "9fb1833ae4642084", "e84cda6be074a423", "5c6d48adec81a2be"], ["paw_", "badge"]),
        new(["0ecb5eeda82e2d5f", "67f6661925991a9f", "a0fdcd2f9b69247a", "dcf0ab7afe6cf577"], ["tiny_", "bottle"]),
        new(["6cc139a38f582f3e", "d5f51729de60e4e8", "8f4cb0908a3300ba", "1b2fecf522b6164a"], ["crescent_", "pendant"]),
        new(["eee5d727e37aef15", "d6b98eb80670c7dc", "73b113e5af3609f8", "22e0d912509b04bd"], ["alpaca_", "plush"]),
        new(["d2f25ccf8c3cccd0", "5eb80a01e21bb324", "21c60ffbf676c53f", "490dd63b8c2ac9c0"], ["pearl_", "hairpin"]),
        new(["1d406253503e44fd", "4d2a18c0c57ea8cc", "f61fd2fc0da9824c", "e95dd53e4cad866e"], ["com", "pass"]),
        new(["37dcc83ebe733216", "159fd033b14df702", "2a45783acd768c2f", "78f44442396384f5"], ["tiny_", "porcelain_cat"]),
        new(["02184a237b6c167b", "084eda15755b2711", "6b8dbc2c045ac717", "3a3e8b8d3422e566"], ["chipmunk_", "plush"]),
        new(["1a85ae5c00b6f045", "75767d81ed4fd1e3", "57408dad5413221d", "22719d13863fcd29"], ["wa", "nd"]),
        new(["6db577a43decaf8b", "0fad52f81a0246b9", "0b20a07e2280679c", "993757dbd7957bda"], ["firefly_", "amber"])
    ];

    private static readonly IReadOnlyDictionary<string, string> HiddenEntriesByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["CRYSTAL"] = "crystal_petal",
        ["TWILIGHT"] = "twilight_petal",
        ["STARRY"] = "starry_petal"
    };
}

public sealed record GiftCodeEntry(string[] HashParts, string[] CollectableIDParts)
{
    public string Hash => string.Concat(HashParts);
    public string CollectableID => string.Concat(CollectableIDParts);
}
