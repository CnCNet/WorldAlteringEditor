using System.Text;
using MapEditorLibrary;
using MapEditorLibrary.Initialization;
using MapEditorLibrary.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rampastring.Tools;

namespace MapEditorMCP.Tests;

[TestClass]
public sealed class MapLoaderHouseColorTests
{
    private const string MapText = """
        [Countries]
        0=BG_Naval

        [BG_Naval]
        Color=DarkGreen
        Side=Allied

        [Houses]
        0=BG_Naval House

        [BG_Naval House]
        Country=BG_Naval
        Color=DarkBlue
        """;

    [TestMethod]
    public void LoadingAndSavingKeepsTheCountryColorSeparateFromTheHouseColor()
    {
        bool wasRA2YR = Constants.IsRA2YR;
        Constants.IsRA2YR = true;

        try
        {
            var map = new Map { StandardHouseTypes = new List<HouseType>() };
            var rules = new Rules();
            rules.Colors.Add(new RulesColor(0, "DarkGreen", "85,255,100"));
            rules.Colors.Add(new RulesColor(1, "DarkBlue", "170,255,100"));
            typeof(Map).GetProperty(nameof(Map.Rules))!.SetValue(map, rules);

            IniFile mapIni = Load(MapText);
            MapLoader.ReadHouseTypes(map, mapIni);
            MapLoader.ReadHouses(map, mapIni);

            Assert.AreEqual("DarkBlue", map.Houses.Single().Color);
            Assert.AreEqual("DarkGreen", map.HouseTypes.Single().Color);

            var savedIni = new IniFile();
            MapWriter.WriteHouseTypes(map, savedIni);
            MapWriter.WriteHouses(map, savedIni);

            Assert.AreEqual("DarkGreen", savedIni.GetStringValue("BG_Naval", "Color", null));
            Assert.AreEqual("DarkBlue", savedIni.GetStringValue("BG_Naval House", "Color", null));
        }
        finally
        {
            Constants.IsRA2YR = wasRA2YR;
        }
    }

    private static IniFile Load(string text)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        return new IniFile(stream);
    }
}
