using NoodleManagerX.Models.Mods;
using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerXTests.Mods
{
    public class TestUtils
    {
        public static string GetTestJsonFileContents(string filename)
        {
            var fullPath = Path.Combine(".", "Mods", "json", filename);
            return File.ReadAllText(fullPath);
        }

        public static ModInfo CreateTestMod(string id, List<ModVersion> versions)
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

        public static ModVersion CreateTestModVersion(string version, List<ModDependencyInfo>? dependencies = null)
        {
            return new ModVersion
            {
                Version = SemVersion.Parse(version, SemVersionStyles.Any),
                DownloadUrl = "localhost",
                Dependencies = dependencies ?? new(),
            };
        }

        public static ModDependencyInfo CreateTestDependency(string id, string minVersion, string? maxVersion = null)
        {
            return new ModDependencyInfo
            {
                Id = id,
                MinVersion = SemVersion.Parse(minVersion, SemVersionStyles.Any),
                MaxVersion = maxVersion == null ? null : SemVersion.Parse(maxVersion, SemVersionStyles.Any),
            };
        }
        public static void AssertVersionsEqual(SemVersion v1, string v2)
        {
            var semVersion2 = SemVersion.Parse(v2, SemVersionStyles.Any);
            Assert.That(v1.ComparePrecedenceTo(semVersion2), Is.EqualTo(0), $"SemVersion {v1} != {v2}");
        }
    }
}
