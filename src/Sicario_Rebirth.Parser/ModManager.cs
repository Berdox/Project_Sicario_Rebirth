using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sicario_Rebirth.Parser {
    public class ModManager {

        private readonly ManifestParser _parser = new();

        public List<ModDefinition> LoadedMods { get; } = new();

        public void LoadManifest(string filePath) {
            var mods = _parser.ParseFile(filePath);
            LoadedMods.AddRange(mods);
        }

        public void LoadManifestDirectory(string directoryPath) {
            foreach(string file in Directory.EnumerateFiles(directoryPath, "*.dtp")) {
                LoadManifest(file);
            }
        }

        public void ListMods() {
            if (LoadedMods.Count == 0) {
                Console.WriteLine("No mods currently loaded.");
                return;
            }

            Console.WriteLine($"==========================================");
            Console.WriteLine($"  TOTAL LOADED MODS: {LoadedMods.Count}");
            Console.WriteLine($"==========================================\n");

            for (int i = 0; i < LoadedMods.Count; i++) {
                var mod = LoadedMods[i];

                Console.WriteLine($"[{i + 1}] {mod.DisplayName}");
                Console.WriteLine(new string('-', 50));
                Console.WriteLine($"  ID          : {(string.IsNullOrEmpty(mod.ID) ? "(None)" : mod.ID)}");
                Console.WriteLine($"  Author      : {mod.Author}");
                Console.WriteLine($"  Description : {mod.Description}");
                Console.WriteLine($"  Source File : {mod.SourceFilePath}");

                // Print Sicario Flags
                Console.WriteLine($"  Flags       : Overwrites={mod.Flags.Overwrites} | Private={mod.Flags.IsPrivate} | Preview={mod.Flags.IsPreview}");

                // Print Asset Patches
                if (mod.AssetPatches.Count > 0) {
                    Console.WriteLine($"\n  Target Asset Patches ({mod.AssetPatches.Count} Files):");

                    foreach (var (assetPath, patchGroups) in mod.AssetPatches) {
                        Console.WriteLine($"    └─ Target File: {assetPath}");

                        foreach (var group in patchGroups) {
                            Console.WriteLine($"       Group: {group.Name}");

                            foreach (var patch in group.Patches) {
                                Console.WriteLine($"         • [{patch.Type}] (v{patch.Version})");
                                Console.WriteLine($"           Desc     : {patch.Description}");
                                Console.WriteLine($"           Template : {patch.Template}");
                                Console.WriteLine($"           Value    : {patch.Value}");
                            }
                        }
                    }
                }
                else {
                    Console.WriteLine("\n  Target Asset Patches: None");
                }

                // Print Raw File Overwrites
                if (mod.FilePatches.Count > 0) {
                    Console.WriteLine($"\n  File Overwrites ({mod.FilePatches.Count}):");
                    foreach (var (target, replacement) in mod.FilePatches) {
                        Console.WriteLine($"    └─ {target} => {replacement}");
                    }
                }

                Console.WriteLine(); // Blank line between mods
            }
        }
    }
}
