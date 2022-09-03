using NoodleManagerX.Models.Mods;
using NoodleManagerX.Mods;
using Semver;

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

        /*[Test]
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
                Assert.That(GetVersionStringFromResolved(graph.ResolvedVersions, "BBB"), Is.EqualTo("2.3.5"));
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

        [Test]
        public void Test_Resolve_MultipleDepVersions_ChooseHighestInRange()
        {
            // Versions vs selections - two lists??

            var modA = CreateTestModVersion("AAA", "1.0");
            modA.Dependencies.Add(CreateTestDependency("BBB", "1.0", "1.2.1"));
            graph.AddModVersion(modA);

            graph.AddModVersion(CreateTestModVersion("BBB", "1.0"));
            graph.AddModVersion(CreateTestModVersion("BBB", "1.2"));
            graph.AddModVersion(CreateTestModVersion("BBB", "1.2.1"));
            graph.AddModVersion(CreateTestModVersion("BBB", "1.2.2"));
            graph.AddModVersion(CreateTestModVersion("BBB", "1.3"));

            graph.Resolve();
            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Has.Count.EqualTo(2));
                Assert.That(GetVersionStringFromResolved(graph.ResolvedVersions, "BBB"), Is.EqualTo("1.2.1"));
            });
        }

        private static ModVersion CreateTestModVersion(string id, string version)
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

        private static ModDependencyInfo CreateTestDependency(string id, string minVersion, string? maxVersion = null)
        {
            return new ModDependencyInfo
            {
                Id = id,
                MinVersion = SemVersion.Parse(minVersion, SemVersionStyles.Any),
                MaxVersion = maxVersion == null ? null : SemVersion.Parse(maxVersion, SemVersionStyles.Any),
            };
        }

        private string? GetVersionStringFromResolved(List<ModVersion> resolvedVersions, string modId)
        {
            return resolvedVersions.FindLast(v => v.Id.Equals(modId))?.Version?.ToString();
        }*/
    }
}