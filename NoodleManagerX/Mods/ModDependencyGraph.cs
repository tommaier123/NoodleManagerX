using NoodleManagerX.Models.Mods;
using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace NoodleManagerX.Mods
{
    public class ModDependencyGraph
    {
        public enum ResolvedState
        {
            UNRESOLVED,
            RESOLVING,
            RESOLVED,
            ERROR_MISSING_MOD,
            ERROR_MISSING_DEP,
            ERROR_VERSION_MISMATCH,
        }

        public ResolvedState State { get; private set; } = ResolvedState.UNRESOLVED;
        public string Message { get; private set; } = "";

        // ModInfo.id, ModVersion
        public Dictionary<string, ModVersion> ResolvedVersions { get; private set; } = new();
        public Dictionary<string, ModVersion> ResolvingVersions { get; private set; } = new();

        // ModInfo.id, ModInfo
        private Dictionary<string, ModInfo> _mods = new();


        public void AddMod(ModInfo mod)
        {
            _mods.Add(mod.Id, mod);
        }

        private bool AreDependenciesValid(List<ModDependencyInfo> dependencies, Dictionary<string, ModVersion> currentVersions)
        {
            // Dependencies must be present
            foreach (var dep in dependencies)
            {
                if (!currentVersions.ContainsKey(dep.Id))
                {
                    return false;
                }
            }

            return true;
        }

        private ModVersion GetLatestVersion(List<ModVersion> versions)
        {
            ModVersion selectedVersion = null;
            foreach (var version in versions)
            {
                if (selectedVersion == null ||
                    version.Version.ComparePrecedenceTo(selectedVersion.Version) > 0)
                {
                    // version > selectedVersion. Choose max valid
                    selectedVersion = version;
                }
            }
            return selectedVersion;
        }

        public void Resolve(List<ModVersionSelection> selectedVersions)
        {
            // Reset resolved versions until resolution is finished
            ResolvedVersions = new();
            State = ResolvedState.RESOLVING;

            ResolvingVersions = new Dictionary<string, ModVersion>();

            // Add base selections
            ResolvingAddBaseVersions(selectedVersions);
            if (State != ResolvedState.RESOLVING)
            {
                Console.WriteLine("Failed to add base versions");
                return;
            }

            // Make sure dependencies all exist
            ResolvingCheckDependenciesExist();
            if (State != ResolvedState.RESOLVING)
            {
                Console.WriteLine("Not all dependencies exist in main list");
                return;
            }


            /*

            // Check dependencies
            foreach (var mod in _modVersions.Values)
            {
                foreach (var depInfo in mod.Dependencies)
                {
                    if (finalVersions.ContainsKey(depInfo.Id))
                    {
                        // Dependency exists. Check version range
                        var existingModInfo = finalVersions[depInfo.Id];
                        if (IsVersionInRange(existingModInfo.Version, depInfo.MinVersion, depInfo.MaxVersion))
                        {
                            Console.WriteLine($"Dependency {depInfo.Id} already defined. Version is in range");
                        }
                        else
                        {
                            Message = $"Dependency {depInfo.Id} already defined at version {existingModInfo.Version}" +
                                $" outside of required range {depInfo.MinVersion} to {depInfo.MaxVersion}";
                            State = ResolvedState.ERROR_VERSION_MISMATCH;
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Dependency {depInfo.Id} not defined in list");
                        State = ResolvedState.ERROR_MISSING_DEP;
                        return;
                    }
                }
            }*/

            State = ResolvedState.RESOLVED;
            ResolvedVersions = ResolvingVersions;
            ResolvingVersions = new();
        }

        private void ResolvingAddBaseVersions(List<ModVersionSelection> selectedVersions)
        {
            foreach (var selection in selectedVersions)
            {
                if (!_mods.ContainsKey(selection.ModId))
                {
                    Console.WriteLine($"Selected mod {selection.ModId} not found in graph!");
                    State = ResolvedState.ERROR_MISSING_MOD;
                    return;
                }

                if (selection.ModVersion == null)
                {
                    // No selection, use largest
                    ResolvingVersions[selection.ModId] = GetLatestVersion(_mods[selection.ModId].Versions);
                }
                else
                {
                    var modInfo = _mods[selection.ModId];
                    var matchedModVersion = modInfo.Versions.Find(
                        v => v.Version.ComparePrecedenceTo(selection.ModVersion.Version) == 0
                    );
                    if (matchedModVersion == null)
                    {
                        Console.WriteLine($"Version {selection.ModVersion.Version} for selected mod {selection.ModId} not found in graph!");
                        State = ResolvedState.ERROR_MISSING_MOD;
                        return;
                    }
                    ResolvingVersions[selection.ModId] = selection.ModVersion;
                }
            }
        }

        private void ResolvingCheckDependenciesExist()
        {
            foreach (var modId in ResolvingVersions.Keys)
            {
                var modVersion = ResolvingVersions[modId];

                foreach (var dep in modVersion.Dependencies)
                {
                    // Dependencies must be present
                    if (!ResolvingVersions.ContainsKey(dep.Id))
                    {
                        Console.WriteLine($"Dependency missing for mod ${modId}");
                        State = ResolvedState.ERROR_MISSING_DEP;
                        return;
                    }
                }
            }
        }

        private static bool IsVersionInRange(SemVersion version, SemVersion lowInclusive, SemVersion highInclusive)
        {
            if (version.ComparePrecedenceTo(lowInclusive) == -1)
            {
                return false;
            }

            if (highInclusive != null && version.ComparePrecedenceTo(highInclusive) == 1)
            {
                return false;
            }

            return true;
        }
    }
}
