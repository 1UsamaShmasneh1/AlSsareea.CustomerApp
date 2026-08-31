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
    private static HashSet<string> Keys(string file) => XDocument.Load(file).Root!.Elements("data").Select(x => x.Attribute("name")!.Value).ToHashSet(StringComparer.Ordinal);
}
