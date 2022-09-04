using Newtonsoft.Json;
using NoodleManagerX.Models.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerXTests.Mods
{
    internal class TestParseModInfoList
    {
        [Test]
        public void Test_ParseModInfoList_VariousValidVersions()
        {
            string rawJson = TestUtils.GetTestJsonFileContents("mods_many_versions.json");
            ModInfoList? modList = JsonConvert.DeserializeObject<ModInfoList>(rawJson);
            Assert.That(modList, Is.Not.Null);
        }

        [Test]
        public void Test_ParseModInfoList_BadOnlyMajor()
        {
            string rawJson = TestUtils.GetTestJsonFileContents("invalid_version.json");
            Assert.Throws<FormatException>(() => JsonConvert.DeserializeObject<ModInfoList>(rawJson));
        }
    }
}
