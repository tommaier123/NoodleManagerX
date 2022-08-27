using NoodleManagerX.Mods;

namespace NoodleManagerXTests.Mods
{
    public class ModDependencyGraphTests
    {
        private ModDependencyGraph graph;

        [SetUp]
        public void Setup()
        {
            graph = new ModDependencyGraph();
        }

        [Test]
        public void Test_Resolve_NoVersions()
        {
            Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.UNRESOLVED));

            graph.Resolve();

            Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
            Assert.That(graph.ResolvedVersions.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_Resolve_MultipleModsNoDeps()
        {
            graph.AddModVersion(CreateTestModVersion("AAA", "1.0"));
            graph.AddModVersion(CreateTestModVersion("BBB", "1.0"));
            graph.AddModVersion(CreateTestModVersion("CCC", "1.0"));

            graph.Resolve();

            Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
            Assert.That(graph.ResolvedVersions.Count, Is.EqualTo(3));
        }

        [Test]
        public void Test_Resolve_DepNotInList_Error()
        {
            var modA = CreateTestModVersion("AAA", "1.0");
            modA.Dependencies.Add(new ModDependencyInfo
            {
                Id = "BBB",
                Name = "BBB_name",
                Author = "Bar",
                MinVersion = "1.0"
            });
            graph.AddModVersion(modA);

            graph.Resolve();

            Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.ERROR_MISSING_DEP));
            Assert.That(graph.Message, Is.Not.Null);
            Assert.That(graph.ResolvedVersions.Count, Is.EqualTo(0));
        }

        private ModVersion CreateTestModVersion(string id, string version)
        {
            return new ModVersion
            {
                Id = id,
                Name = id + "_name",
                Author = "Foo",
                Description = "Some mod",
                Version = version,
                DownloadUrl = "localhost",
                Dependencies = new List<ModDependencyInfo>(),
            };
        }
    }
}