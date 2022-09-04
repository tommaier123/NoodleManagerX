using NoodleManagerX.Models.Mods;
using NoodleManagerX.Mods;
using Semver;
using System;

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

            var selections = new List<ModVersionSelection>();
            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Is.Empty);
            });
        }

        [Test]
        public void Test_Resolve_NoModsSelected()
        {
            graph.AddMod(CreateTestMod("AAA", new List<ModVersion>
            {
                CreateTestModVersion("1.0")
            }));

            var selections = new List<ModVersionSelection>();
            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Is.Empty);
            });
        }

        [Test]
        public void Test_Resolve_MultipleModsNoDeps()
        {
            var modA = CreateTestMod("AAA", new List<ModVersion>
            {
                CreateTestModVersion("1.0")
            });
            graph.AddMod(modA);

            var modB = CreateTestMod("BBB", new List<ModVersion>
            {
                CreateTestModVersion("2.0")
            });
            graph.AddMod(modB);

            var modC = CreateTestMod("CCC", new List<ModVersion>
            {
                CreateTestModVersion("1.2.3")
            });
            graph.AddMod(modC);

            var selections = new List<ModVersionSelection>
            {
                new ModVersionSelection(modA.Id, modA.Versions[0]),
                new ModVersionSelection(modC.Id, modC.Versions[0]),
            };
            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Has.Count.EqualTo(2));
                AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.0");
                AssertVersionsEqual(graph.ResolvedVersions["CCC"].Version, "1.2.3");
                Assert.That(graph.ResolvedVersions.ContainsKey("BBB"), Is.False);
            });
        }

        [Test]
        public void Test_Resolve_NoVersionSpecified_LargestChosen()
        {
            var modA = CreateTestMod("AAA", new List<ModVersion>
            {
                CreateTestModVersion("1.0"),
                CreateTestModVersion("1.2"),
                CreateTestModVersion("1.1.1"),
            });
            graph.AddMod(modA);

            var selections = new List<ModVersionSelection>
            {
                new ModVersionSelection(modA.Id, null)
            };
            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Has.Count.EqualTo(1));
                AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.2");
            });
        }

        [Test]
        public void Test_Resolve_UseSelectedVersion()
        {
            var desiredVersion = CreateTestModVersion("1.1.1");
            var modA = CreateTestMod("AAA", new List<ModVersion>
            {
                CreateTestModVersion("1.0"),
                CreateTestModVersion("1.2"),
                desiredVersion,
            });
            graph.AddMod(modA);

            var selections = new List<ModVersionSelection>
            {
                new ModVersionSelection(modA.Id, desiredVersion)
            };
            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Has.Count.EqualTo(1));
                AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.1.1");
            });
        }

        [Test]
        public void Test_Resolve_SelectedVersionNotFound()
        {
            var desiredVersion = CreateTestModVersion("1.1.1");
            var modA = CreateTestMod("AAA", new List<ModVersion>
            {
                CreateTestModVersion("1.0"),
                CreateTestModVersion("1.1"),
            });
            graph.AddMod(modA);

            var selections = new List<ModVersionSelection>
            {
                new ModVersionSelection(modA.Id, desiredVersion)
            };
            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.ERROR_MISSING_MOD));
                Assert.That(graph.ResolvedVersions, Is.Empty);
            });
        }

        [Test]
        public void Test_Resolve_SelectedModNotAdded()
        {
            var selections = new List<ModVersionSelection>
            {
                new ModVersionSelection("AAA", null)
            };
            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.ERROR_MISSING_MOD));
                Assert.That(graph.ResolvedVersions, Is.Empty);
            });
        }

        [Test]
        public void Test_Resolve_DepNotInList_Error()
        {
            var modA = CreateTestMod("AAA", new List<ModVersion>
            {
                CreateTestModVersion("1.0", new List<ModDependencyInfo>
                {
                    CreateTestDependency("BBB", "1.0")
                }),
            });
            graph.AddMod(modA);

            var selections = new List<ModVersionSelection>()
            {
                new ModVersionSelection(modA.Id, modA.Versions[0])
            };
            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.ERROR_MISSING_DEP));
                Assert.That(graph.Message, Is.Not.Null);
                Assert.That(graph.ResolvedVersions, Is.Empty);
            });
        }
        
        [Test]
        public void Test_Resolve_DepSelectedAtSupportedVersion()
        {
            var modA = CreateTestMod("AAA", new List<ModVersion>
            {
                CreateTestModVersion("1.2", new List<ModDependencyInfo>
                {
                    CreateTestDependency("BBB", "1.0")
                }),
            });
            graph.AddMod(modA);

            var modB = CreateTestMod("BBB", new List<ModVersion>
            {
                CreateTestModVersion("1.1"),
            });
            graph.AddMod(modB);

            var selections = new List<ModVersionSelection>
            {
                new ModVersionSelection(modA.Id, modA.Versions[0]),
                new ModVersionSelection(modB.Id, modB.Versions[0]),
            };
            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Has.Count.EqualTo(2));
                AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.2");
                AssertVersionsEqual(graph.ResolvedVersions["BBB"].Version, "1.1");
            });
        }

        [Test]
        public void Test_Resolve_DepNotSelected_TreatedLikeUnspecifiedVersion()
        {
            var modA = CreateTestMod("AAA", new List<ModVersion>
            {
                CreateTestModVersion("1.2", new List<ModDependencyInfo>
                {
                    CreateTestDependency("BBB", "1.0")
                }),
            });
            graph.AddMod(modA);

            var modB = CreateTestMod("BBB", new List<ModVersion>
            {
                CreateTestModVersion("1.0"),
            });
            graph.AddMod(modB);

            var selections = new List<ModVersionSelection>
            {
                new ModVersionSelection(modA.Id, modA.Versions[0]),
            };

            graph.Resolve(selections);

            Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
            Assert.Multiple(() =>
            {
                AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.2");
                AssertVersionsEqual(graph.ResolvedVersions["BBB"].Version, "1.0");
            });
        }

        [Test]
        public void Test_Resolve_DepVersionUnspecified_SelectMaxInRange()
        {
            var modA = CreateTestMod("AAA", new List<ModVersion>
            {
                CreateTestModVersion("1.2", new List<ModDependencyInfo>
                {
                    CreateTestDependency("BBB", "1.0", "2.3.5"),
                }),
            });
            graph.AddMod(modA);

            var modB = CreateTestMod("BBB", new List<ModVersion>
            {
                CreateTestModVersion("1.0"),
                CreateTestModVersion("1.3.5"),
                CreateTestModVersion("2.2.2"),
                CreateTestModVersion("2.3.4"),
                CreateTestModVersion("2.3.6"),
                CreateTestModVersion("2.4.0"),
            });
            graph.AddMod(modB);

            var selections = new List<ModVersionSelection>
            {
                new ModVersionSelection(modA.Id, modA.Versions[0]),
                new ModVersionSelection(modB.Id, null),
            };
            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Has.Count.EqualTo(2));
                AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.2");
                AssertVersionsEqual(graph.ResolvedVersions["BBB"].Version, "2.3.4");
            });
        }


        private static ModInfo CreateTestMod(string id, List<ModVersion> versions)
        {
            return new ModInfo
            {
                Id = id,
                Name = id + " name",
                Author = "Foo",
                Description = "Some mod",
                Versions = versions,
            };
        }

        private static ModVersion CreateTestModVersion(string version, List<ModDependencyInfo>? dependencies = null)
        {
            return new ModVersion
            {
                Version = SemVersion.Parse(version, SemVersionStyles.Any),
                DownloadUrl = "localhost",
                Dependencies = dependencies ?? new(),
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

        private static void AssertVersionsEqual(SemVersion v1, string v2)
        {
            var semVersion2 = SemVersion.Parse(v2, SemVersionStyles.Any);
            Assert.That(v1.ComparePrecedenceTo(semVersion2), Is.EqualTo(0), $"SemVersion {v1} != {v2}");
        }
    }
}