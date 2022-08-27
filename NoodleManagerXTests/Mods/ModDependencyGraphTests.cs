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
            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Is.Empty);
            });
        }

        [Test]
        public void Test_Resolve_MultipleModsNoDeps()
        {
            graph.AddModVersion(CreateTestModVersion("AAA", "1.0"));
            graph.AddModVersion(CreateTestModVersion("BBB", "1.0"));
            graph.AddModVersion(CreateTestModVersion("CCC", "1.0"));

            graph.Resolve();
            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Has.Count.EqualTo(3));
            });
        }

        [Test]
        public void Test_Resolve_DepNotInList_Error()
        {
            var modA = CreateTestModVersion("AAA", "1.0");
            modA.Dependencies.Add(CreateTestDependency("BBB", "1.0"));
            graph.AddModVersion(modA);

            graph.Resolve();
            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.ERROR_MISSING_DEP));
                Assert.That(graph.Message, Is.Not.Null);
                Assert.That(graph.ResolvedVersions, Is.Empty);
            });
        }

        [Test]
        public void Test_Resolve_DepExistsAtMinVersion()
        {
            var modA = CreateTestModVersion("AAA", "1.0");
            modA.Dependencies.Add(CreateTestDependency("BBB", "1.0"));
            graph.AddModVersion(modA);

            graph.AddModVersion(CreateTestModVersion("BBB", "1.0"));

            graph.Resolve();
            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Has.Count.EqualTo(2));
            });
        }

        [Test]
        public void Test_Resolve_DepExistsAtMaxVersion()
        {
            var modA = CreateTestModVersion("AAA", "1.0");
            modA.Dependencies.Add(CreateTestDependency("BBB", "1.0", "2.3.5"));
            graph.AddModVersion(modA);

            graph.AddModVersion(CreateTestModVersion("BBB", "2.3.5"));

            graph.Resolve();
            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Has.Count.EqualTo(2));
                Assert.That(graph.ResolvedVersions.FindLast(v => v.Id.Equals("BBB"))?.Version, Is.EqualTo("2.3.5"));
            });
        }

        [Test]
        public void Test_Resolve_DepOutOfRange_Error()
        {
            var modA = CreateTestModVersion("AAA", "1.0");
            modA.Dependencies.Add(CreateTestDependency("BBB", "1.0", "1.4.2"));
            graph.AddModVersion(modA);

            graph.AddModVersion(CreateTestModVersion("BBB", "1.5.0"));

            graph.Resolve();
            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.ERROR_VERSION_MISMATCH));
                Assert.That(graph.ResolvedVersions, Is.Empty);
            });
        }

        private ModVersion CreateTestModVersion(string id, string version)
        {
            return new ModVersion
            {
                Id = id,
                Name = id + " name",
                Author = "Foo",
                Description = "Some mod",
                Version = version,
                DownloadUrl = "localhost",
                Dependencies = new List<ModDependencyInfo>(),
            };
        }

        private ModDependencyInfo CreateTestDependency(string id, string minVersion, string? maxVersion = null)
        {
            return new ModDependencyInfo
            {
                Id = id,
                MinVersion = minVersion,
                MaxVersion = maxVersion,
            };
        }
    }
}