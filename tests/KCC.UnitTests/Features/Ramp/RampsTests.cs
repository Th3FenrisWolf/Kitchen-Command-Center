using System.Text.Json.Nodes;
using System.Xml.Linq;
using KCC.Web.Features.Ramp;
using Moq;
using TUnit.Assertions.Enums;
using Umbraco.Cms.Core.Models;

namespace KCC.UnitTests.Features.Ramp;

public class RampsTests
{
    [Test]
    public async Task RampOptions_AreTheRampsTheCodeReads()
    {
        var path = Path.Combine(RepoPaths.WebProject, "uSync", "v17", "DataTypes", "KCCRamp.config");
        var config = XDocument.Load(path).Root.Element("Config").Value;
        var items = JsonNode.Parse(config)["items"].AsArray().Select(item => (string)item);

        _ = await Assert.That(items).IsEquivalentTo([Ramps.Device, Ramps.Light, Ramps.Dark], CollectionOrdering.Matching);
    }

    [Test]
    [Arguments("[\"Device\"]", "Device")]
    [Arguments("[\"Light\"]", "Light")]
    [Arguments("[\"Dark\"]", "Dark")]
    public async Task Of_ReadsTheDropdownsArray(string stored, string expected)
    {
        _ = await Assert.That(Ramps.Of(Storing(stored))).IsEqualTo(expected);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("[]")]
    [Arguments("[\"Sepia\"]")]
    [Arguments("[\"dark\"]")]
    [Arguments("Dark")]
    [Arguments("{\"ramp\":\"Dark\"}")]
    public async Task Of_ReadsAnythingElseAsDevice(string stored)
    {
        _ = await Assert.That(Ramps.Of(Storing(stored))).IsEqualTo(Ramps.Device);
    }

    [Test]
    public async Task Of_NoMember_IsDevice()
    {
        _ = await Assert.That(Ramps.Of(null)).IsEqualTo(Ramps.Device);
    }

    [Test]
    public async Task Set_StoresTheArrayTheDropdownSaves()
    {
        var member = new Mock<IMember>();
        string stored = null;
        member.Setup(m => m.SetValue("ramp", It.IsAny<object>(), null, null))
            .Callback<string, object, string, string>((_, value, _, _) => stored = (string)value);

        Ramps.Set(member.Object, Ramps.Dark);

        _ = await Assert.That(stored).IsEqualTo("[\"Dark\"]");
    }

    private static IMember Storing(string stored)
    {
        var member = new Mock<IMember>();
        member.Setup(m => m.GetValue<string>("ramp", null, null, false)).Returns(stored);
        return member.Object;
    }
}
