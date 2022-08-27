using System;
using System.Collections.Generic;
using System.Linq;
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
            ERROR_MISSING_DEP,
            ERROR_VERSION_MISMATCH,
        }

        public ResolvedState State { get; private set; } = ResolvedState.UNRESOLVED;
        public string Message { get; private set; } = "";
        public List<ModVersion> ResolvedVersions { get; private set; } = new List<ModVersion>();

        private Dictionary<string, ModVersion> _modVersions = new Dictionary<string, ModVersion>();


        public void AddModVersion(ModVersion version)
        {
            _modVersions.Add(version.Id, version);
        }

        public void Resolve()
        {
            // Reset resolved versions until resolution is finished
            ResolvedVersions = new List<ModVersion>();
            State = ResolvedState.RESOLVING;

            var finalVersions = new Dictionary<string, ModVersion>();

            // Add base mods
            foreach (var mod in _modVersions.Values)
            {
                finalVersions.Add(mod.Id, mod);
            }

            // Check dependencies
            foreach (var mod in _modVersions.Values)
            {
                foreach (var depInfo in mod.Dependencies)
                {
                    if (finalVersions.ContainsKey(depInfo.Id))
                    {
                        /*// Dependency exists. Check version range
                        var existingModInfo = finalVersions[depInfo.Id];
                        if (IsVersionInRange(existingModInfo.Version, depInfo.MinVersion, depInfo.MaxVersion))
                        {
                            Console.WriteLine($"Dependency {depInfo.Name} already exists. Version is in range");
                        }
                        else
                        {
                            Message = $"Dependency {depInfo.Name} from mod {mod.Name} already defined at version " +
                                $"{existingModInfo.Version} outside of required range {depInfo.MinVersion} to {depInfo.MaxVersion}";
                            State = ResolvedState.ERROR_VERSION_MISMATCH;
                            return;
                        }*/
                    }
                    else
                    {
                        Console.WriteLine($"Dependency {depInfo.Name} not defined in list");
                        State = ResolvedState.ERROR_MISSING_DEP;
                        return;
                    }
                }
            }

            State = ResolvedState.RESOLVED;
            ResolvedVersions = finalVersions.Values.ToList();
        }

        private bool IsVersionInRange(string version, string lowInclusive, string highInclusive)
        {
            return true;
        }
    }
}
