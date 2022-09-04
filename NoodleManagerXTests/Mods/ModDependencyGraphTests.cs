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
            graph.AddMod(TestUtils.CreateTestMod("AAA", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.0")
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
            var modA = TestUtils.CreateTestMod("AAA", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.0")
            });
            graph.AddMod(modA);

            var modB = TestUtils.CreateTestMod("BBB", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("2.0")
            });
            graph.AddMod(modB);

            var modC = TestUtils.CreateTestMod("CCC", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.2.3")
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
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.0");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["CCC"].Version, "1.2.3");
                Assert.That(graph.ResolvedVersions.ContainsKey("BBB"), Is.False);
            });
        }

        [Test]
        public void Test_Resolve_NoVersionSpecified_LargestChosen()
        {
            var modA = TestUtils.CreateTestMod("AAA", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.0"),
                TestUtils.CreateTestModVersion("1.2"),
                TestUtils.CreateTestModVersion("1.1.1"),
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
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.2");
            });
        }

        [Test]
        public void Test_Resolve_UseSelectedVersion()
        {
            var desiredVersion = TestUtils.CreateTestModVersion("1.1.1");
            var modA = TestUtils.CreateTestMod("AAA", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.0"),
                TestUtils.CreateTestModVersion("1.2"),
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
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.1.1");
            });
        }

        [Test]
        public void Test_Resolve_SelectedVersionNotFound()
        {
            var desiredVersion = TestUtils.CreateTestModVersion("1.1.1");
            var modA = TestUtils.CreateTestMod("AAA", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.0"),
                TestUtils.CreateTestModVersion("1.1"),
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
            var modA = TestUtils.CreateTestMod("AAA", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.0", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("BBB", "1.0")
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
            var modA = TestUtils.CreateTestMod("AAA", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.2", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("BBB", "1.0")
                }),
            });
            graph.AddMod(modA);

            var modB = TestUtils.CreateTestMod("BBB", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.1"),
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
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.2");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["BBB"].Version, "1.1");
            });
        }

        [Test]
        public void Test_Resolve_DepNotSelected_TreatedLikeUnspecifiedVersion()
        {
            var modA = TestUtils.CreateTestMod("AAA", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.2", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("BBB", "1.0")
                }),
            });
            graph.AddMod(modA);

            var modB = TestUtils.CreateTestMod("BBB", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.0"),
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
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.2");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["BBB"].Version, "1.0");
            });
        }

        [Test]
        public void Test_Resolve_DepVersionUnspecified_SelectMaxInRange()
        {
            var modA = TestUtils.CreateTestMod("AAA", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.2", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("BBB", "1.0", "2.3.5"),
                }),
            });
            graph.AddMod(modA);

            var modB = TestUtils.CreateTestMod("BBB", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.0"),
                TestUtils.CreateTestModVersion("1.3.5"),
                TestUtils.CreateTestModVersion("2.2.2"),
                TestUtils.CreateTestModVersion("2.3.4"),
                TestUtils.CreateTestModVersion("2.3.6"),
                TestUtils.CreateTestModVersion("2.4.0"),
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
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.2");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["BBB"].Version, "2.3.4");
            });
        }

        [Test]
        public void Test_Resolve_AllVersionsNull()
        {
            var modA = TestUtils.CreateTestMod("AAA", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.1", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("BBB", "1.0"),
                }),
                TestUtils.CreateTestModVersion("1.2", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("BBB", "1.1"),
                }),
            });
            graph.AddMod(modA);

            var modC = TestUtils.CreateTestMod("CCC", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("2.1", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("BBB", "1.0"),
                }),
                TestUtils.CreateTestModVersion("2.2", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("BBB", "1.2"),
                }),
            });
            graph.AddMod(modC);

            var modB = TestUtils.CreateTestMod("BBB", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.0"),
                TestUtils.CreateTestModVersion("1.1"),
                TestUtils.CreateTestModVersion("1.2"),
                TestUtils.CreateTestModVersion("1.3"),
            });
            graph.AddMod(modB);

            var selections = new List<ModVersionSelection>
            {
                new ModVersionSelection(modA.Id, null),
                new ModVersionSelection(modC.Id, null),
            };

            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Has.Count.EqualTo(3));
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.2");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["CCC"].Version, "2.2");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["BBB"].Version, "1.3");
            });
        }

        [Test]
        public void Test_Resolve_VersionMismatchForMaxSelections_Error()
        {
            var modA = TestUtils.CreateTestMod("AAA", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.1", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("BBB", "1.0"),
                }),
                TestUtils.CreateTestModVersion("1.2", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("BBB", "1.1", "1.1"),
                }),
            });
            graph.AddMod(modA);

            var modC = TestUtils.CreateTestMod("CCC", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("2.1", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("BBB", "1.0"),
                }),
                TestUtils.CreateTestModVersion("2.2", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("BBB", "1.2"),
                }),
            });
            graph.AddMod(modC);

            var modB = TestUtils.CreateTestMod("BBB", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.0"),
                TestUtils.CreateTestModVersion("1.1"),
                TestUtils.CreateTestModVersion("1.2"),
                TestUtils.CreateTestModVersion("1.3"),
            });
            graph.AddMod(modB);

            var selections = new List<ModVersionSelection>
            {
                new ModVersionSelection(modA.Id, null),
                new ModVersionSelection(modC.Id, null),
            };

            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.ERROR_VERSION_MISMATCH));
                Assert.That(graph.ResolvedVersions, Is.Empty);
            });
        }

        [Test]
        public void Test_Resolve_MultiNestedDependencies()
        {
            var modA = TestUtils.CreateTestMod("AAA", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.1", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("BBB", "1.0"),
                }),
            });
            graph.AddMod(modA);

            var modB = TestUtils.CreateTestMod("BBB", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.1", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("CCC", "1.3"),
                }),
            });
            graph.AddMod(modB);

            var modC = TestUtils.CreateTestMod("CCC", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.3", new List<ModDependencyInfo>
                {
                    TestUtils.CreateTestDependency("DDD", "1.0"),
                }),
            });
            graph.AddMod(modC);

            var modD = TestUtils.CreateTestMod("DDD", new List<ModVersion>
            {
                TestUtils.CreateTestModVersion("1.0.1")
            });
            graph.AddMod(modD);


            var selections = new List<ModVersionSelection>
            {
                new ModVersionSelection(modA.Id, null),
            };

            graph.Resolve(selections);

            Assert.Multiple(() =>
            {
                Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
                Assert.That(graph.ResolvedVersions, Has.Count.EqualTo(4));
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["AAA"].Version, "1.1");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["BBB"].Version, "1.1");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["CCC"].Version, "1.3");
                TestUtils.AssertVersionsEqual(graph.ResolvedVersions["DDD"].Version, "1.0.1");
            });
        }
    }
}