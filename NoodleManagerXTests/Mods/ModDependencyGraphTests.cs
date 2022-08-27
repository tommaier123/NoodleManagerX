using NoodleManagerX.Mods;

namespace NoodleManagerXTests.Mods
{
    public class ModDependencyGraphTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test_Resolve_NoDependencies_Pass()
        {
            var graph = new ModDependencyGraph();
            Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.UNRESOLVED));
            graph.Resolve();
            Assert.That(graph.State, Is.EqualTo(ModDependencyGraph.ResolvedState.RESOLVED));
        }
    }
}