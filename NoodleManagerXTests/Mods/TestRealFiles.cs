using Newtonsoft.Json;
using NoodleManagerX.Models.Mods;
using NoodleManagerX.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerXTests.Mods
{
    internal class TestRealFiles
    {
        [Test]
        public void Test_LatestVersion_AllAutoSelect_ParsedAndResolved()
        {
            string rawJson = TestUtils.GetTestJsonFileContents("current_list.json");
            ModInfoList? modList = JsonConvert.DeserializeObject<ModInfoList>(rawJson);
            Assert.That(modList, Is.Not.Null);

            var graph = new ModDependencyGraph();
            graph.LoadModInfoList(modList);

            var selections = new List<ModVersionSelection>
            {
                new ModVersionSelection("SRModsList", null),
                new ModVersionSelection("Trashbin", null),
                new ModVersionSelection("MultiplayerSongGrabber", null),
                new ModVersionSelection("CustomSounds", null),
                new ModVersionSelection("SRVoting", null),
                new ModVersionSelection("SRPerformanceMeter", null),
                new ModVersionSelection("SynthRiders-Websockets-Mod", null),
            };
            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Has.Count.EqualTo(8));
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["SRModsList"].Version, "1.0.1");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["Trashbin"].Version, "1.2.0");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["MultiplayerSongGrabber"].Version, "1.0.0");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["CustomSounds"].Version, "1.1.0");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["SRVoting"].Version, "1.2.0");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["SRPerformanceMeter"].Version, "1.2.1");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["SynthRiders-Websockets-Mod"].Version, "1.0.2");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["SRModCore"].Version, "1.0.0");
            });
        }
    }
}
