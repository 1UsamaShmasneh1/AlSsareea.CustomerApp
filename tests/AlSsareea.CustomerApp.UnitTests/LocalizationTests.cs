using System.Xml.Linq;

namespace AlSsareea.CustomerApp.UnitTests;

public sealed class LocalizationTests
{
    [Fact]
    public void English_arabic_and_hebrew_have_identical_keys()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string strings = Path.Combine(root, "src", "AlSsareea.CustomerApp", "Resources", "Strings");
        HashSet<string> english = Keys(Path.Combine(strings, "AppResources.resx"));
        Assert.Equal(english, Keys(Path.Combine(strings, "AppResources.ar.resx")));
        Assert.Equal(english, Keys(Path.Combine(strings, "AppResources.he.resx")));
        Assert.Contains("ErrorOffline", english); Assert.Contains("OrderDelivered", english);
    }
    [Fact]
    public void Every_translation_is_nonempty_and_contains_native_script()
    {
        string strings = StringsPath(); Dictionary<string, string> arabic = Values(Path.Combine(strings, "AppResources.ar.resx")); Dictionary<string, string> hebrew = Values(Path.Combine(strings, "AppResources.he.resx"));
        Assert.All(arabic.Values, value => Assert.False(string.IsNullOrWhiteSpace(value))); Assert.All(hebrew.Values, value => Assert.False(string.IsNullOrWhiteSpace(value))); Assert.Contains(arabic.Values, value => value.Any(character => character is >= '\u0600' and <= '\u06ff')); Assert.Contains(hebrew.Values, value => value.Any(character => character is >= '\u0590' and <= '\u05ff'));
    }
    [Fact]
    public void Format_placeholders_match_across_languages()
    {
        string strings = StringsPath(); Dictionary<string, string> english = Values(Path.Combine(strings, "AppResources.resx")); foreach (string culture in new[] { "ar", "he" }) { Dictionary<string, string> translated = Values(Path.Combine(strings, $"AppResources.{culture}.resx")); foreach (KeyValuePair<string, string> item in english) Assert.Equal(Placeholders(item.Value), Placeholders(translated[item.Key])); }
    }
    [Fact]
    public void Pages_use_resource_keys_for_significant_user_text()
    {
        string root = RootPath(); string source = string.Join("\n", Directory.GetFiles(Path.Combine(root, "src", "AlSsareea.CustomerApp"), "*Pages.cs").Select(File.ReadAllText));
        Assert.DoesNotContain("Text = \"Welcome", source, StringComparison.Ordinal); Assert.DoesNotContain("Placeholder = \"", source, StringComparison.Ordinal); Assert.DoesNotContain("Title = \"", source, StringComparison.Ordinal); Assert.DoesNotContain("No orders yet", source, StringComparison.OrdinalIgnoreCase);
    }
    private static string RootPath() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    private static string StringsPath() => Path.Combine(RootPath(), "src", "AlSsareea.CustomerApp", "Resources", "Strings");
    private static Dictionary<string, string> Values(string file) => XDocument.Load(file).Root!.Elements("data").ToDictionary(x => x.Attribute("name")!.Value, x => x.Element("value")!.Value, StringComparer.Ordinal);
    private static string[] Placeholders(string value) => System.Text.RegularExpressions.Regex.Matches(value, "\\{\\d+(?:[^}]*)?\\}").Select(match => match.Value.Split(':', ',')[0] + "}").Order().ToArray();
    private static HashSet<string> Keys(string file) => XDocument.Load(file).Root!.Elements("data").Select(x => x.Attribute("name")!.Value).ToHashSet(StringComparer.Ordinal);
}
