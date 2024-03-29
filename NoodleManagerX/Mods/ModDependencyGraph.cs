﻿using NoodleManagerX.Models.Mods;
using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
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
            ERROR_FINAL_CHECK,
        }

        public ResolvedState State { get; private set; } = ResolvedState.UNRESOLVED;
        public string Message { get; private set; } = "";

        // ModInfo.id, ModVersion
        public Dictionary<string, ModVersion> ResolvedVersions { get; private set; } = new();
        // ModInfo.id, ModVersion
        public Dictionary<string, ModVersion> ResolvingVersions { get; private set; } = new();

        // ModInfo.id, ModInfo
        private Dictionary<string, ModInfo> _mods = new();
        private bool IsDirty = false;

        public void LoadMods(List<ModInfo> mods)
        {
            foreach (var mod in mods)
            {
                AddMod(mod);
            }
        }

        public void AddMod(ModInfo mod)
        {
            _mods.Add(mod.Id, mod);
        }

        private List<ModVersionSelection> GetNewSelectionFromResolving(List<ModVersionSelection> originalSelection)
        {
            var newSelection = new List<ModVersionSelection>();
            foreach (var originalSelect in originalSelection)
            {
                if (ResolvingVersions.ContainsKey(originalSelect.ModId))
                {
                    newSelection.Add(new ModVersionSelection(originalSelect.ModId, ResolvingVersions[originalSelect.ModId]));
                }
                else
                {
                    newSelection.Add(originalSelect);
                }
            }
            return newSelection;
        }

        public ResolvedState Resolve(List<ModVersionSelection> selectedVersions)
        {
            // Reset resolved versions until resolution is finished
            ResolvingVersions = new Dictionary<string, ModVersion>();
            State = ResolvedState.RESOLVING;

            // Add base selections
            ResolvingAddBaseVersions(selectedVersions);
            if (State != ResolvedState.RESOLVING)
            {
                Console.WriteLine("Failed to add base versions");
                return State;
            }
            if (IsDirty)
            {
                IsDirty = false;
                return Resolve(GetNewSelectionFromResolving(selectedVersions));
            }

            // Make sure dependencies all exist
            ResolvingCheckDependenciesExist();
            if (State != ResolvedState.RESOLVING)
            {
                Console.WriteLine("Not all dependencies exist in main list");
                return State;
            }

            // If any dependencies need to be selected, do so and re-resolve
            var additionalSelections = SelectMissingDependencies();
            if (additionalSelections.Count > 0)
            {
                Console.WriteLine($"Missing dependencies selected; re-resolving");
                var newSelection = selectedVersions;
                newSelection.AddRange(additionalSelections);
                return Resolve(newSelection);
            }

            // Validate final state
            ValidateState(selectedVersions);
            if (State != ResolvedState.RESOLVING)
            {
                Console.WriteLine("Failed final resolution validation");
                return State;
            }

            ResolvedVersions = ResolvingVersions;
            State = ResolvedState.RESOLVED;
            return State;
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
                    // No selection, use largest supported
                    var latestSupportedVersion = GetLatestSupportedVersion(
                        _mods[selection.ModId],
                        selectedVersions
                    );
                    if (latestSupportedVersion == null)
                    {
                        Console.WriteLine($"No supported version found for dependency {selection.ModId}");
                        State = ResolvedState.ERROR_VERSION_MISMATCH;
                        return;
                    }
                    else
                    {
                        // Version selected, re-resolve using it
                        ResolvingVersions[selection.ModId] = latestSupportedVersion;
                        IsDirty = true;
                        return;
                    }
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

        private List<ModVersionSelection> SelectMissingDependencies()
        {
            var addedSelections = new List<ModVersionSelection>();
            foreach (var modId in ResolvingVersions.Keys)
            {
                var modVersion = ResolvingVersions[modId];

                foreach (var dep in modVersion.Dependencies)
                {
                    // If dependency wasn't selected, select it and resolve again
                    if (!ResolvingVersions.ContainsKey(dep.Id))
                    {
                        Console.WriteLine($"Dependency {dep.Id} not selected; selecting at \"max\" version");
                        addedSelections.Add(new ModVersionSelection(dep.Id, null));
                    }
                }
            }
            return addedSelections;
        }

        private void ResolvingCheckDependenciesExist()
        {
            foreach (var modId in ResolvingVersions.Keys)
            {
                var modVersion = ResolvingVersions[modId];

                foreach (var dep in modVersion.Dependencies)
                {
                    // Dependencies must be present
                    if (!_mods.ContainsKey(dep.Id))
                    {
                        Console.WriteLine($"Dependency {dep.Id} missing for mod {modId}");
                        State = ResolvedState.ERROR_MISSING_DEP;
                        return;
                    }
                }
            }
        }

        private void ValidateState(List<ModVersionSelection> selectedVersions)
        {
            // Make sure all selected versions were resolved
            foreach (var selection in selectedVersions)
            {
                if (!ResolvingVersions.ContainsKey(selection.ModId))
                {
                    Console.WriteLine($"Final check for {selection.ModId} failed (not found in resolved list)");
                    State = ResolvedState.ERROR_FINAL_CHECK;
                    return;
                }

                // Make sure all dependencies exist
                var version = ResolvingVersions[selection.ModId];
                foreach (var dep in version.Dependencies)
                {
                    if (!ResolvingVersions.ContainsKey(dep.Id))
                    {
                        Console.WriteLine($"Final check for {selection.ModId} failed (dependency {dep.Id} not found)");
                        State = ResolvedState.ERROR_FINAL_CHECK;
                        return;
                    }

                    var depVersion = ResolvingVersions[dep.Id].Version;
                    if (!VersionInRange(depVersion, dep.MinVersion, dep.MaxVersion))
                    {
                        Console.WriteLine($"Final check for {selection.ModId} failed (dependency {dep.Id} version {depVersion} not in range)");
                        State = ResolvedState.ERROR_FINAL_CHECK;
                        return;
                    }
                }
            }
        }

        private ModVersion GetLatestSupportedVersion(ModInfo mod, List<ModVersionSelection> selectedVersions)
        {
            ModVersion latestSupportedVersion = null;

            // Put versions into set for easier filtering
            var filteredVersions = new HashSet<SemVersion>();
            foreach (var modVersion in mod.Versions)
            {
                filteredVersions.Add(modVersion.Version);
            }

            // Filter out versions that aren't in valid dependency ranges from selection
            foreach (var selection in selectedVersions)
            {
                if (selection.ModVersion == null)
                {
                    //Console.WriteLine($"Version not specified for {selection.ModId}; ignoring dependency check for now");
                    continue;
                }

                foreach (var dependency in selection.ModVersion.Dependencies)
                {
                    if (dependency.Id == mod.Id)
                    {
                        // Some selection depends on this mod.
                        // Filter out versions that are outside of this dependency range
                        filteredVersions.RemoveWhere(version => version.ComparePrecedenceTo(dependency.MinVersion) < 0);
                        if (dependency.MaxVersion != null)
                        {
                            filteredVersions.RemoveWhere(version => version.ComparePrecedenceTo(dependency.MaxVersion) > 0);
                        }
                    }
                }
            }

            // Select latest version from remaining
            foreach (var version in mod.Versions)
            {
                if (!filteredVersions.Contains(version.Version))
                {
                    continue;
                }

                if (latestSupportedVersion == null ||
                    version.Version.ComparePrecedenceTo(latestSupportedVersion.Version) > 0)
                {
                    // version > selectedVersion. Choose max valid
                    latestSupportedVersion = version;
                }
            }

            return latestSupportedVersion;
        }

        public static bool VersionInRange(SemVersion version, SemVersion lowInclusive, SemVersion highInclusive)
        {
            if (version.ComparePrecedenceTo(lowInclusive) < 0)
            {
                return false;
            }

            if (highInclusive != null && version.ComparePrecedenceTo(highInclusive) > 0)
            {
                return false;
            }

            return true;
        }
    }
}
