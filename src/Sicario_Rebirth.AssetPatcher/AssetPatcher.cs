using System.Diagnostics;
using Sicario_Rebirth.Parser;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace Sicario_Rebirth.AssetPatcher {
    public class AssetPatcher {
        private readonly string _paksDir;
        private readonly string _repakExePath;
        private readonly EngineVersion _engineVersion;

        public AssetPatcher(string paksDirectory, string repakExePath, EngineVersion engineVersion = EngineVersion.VER_UE4_24) {
            _paksDir = paksDirectory;
            _repakExePath = repakExePath;
            _engineVersion = engineVersion;
        }

        /// <summary>
        /// Orchestrates the full Extraction -> Patching -> Staging -> Repaking workflow.
        /// </summary>
        public void PatchAndRepak(string targetFileName, Action<PropertyData> patchAction, string outputPakName) {
            // 1. Extract raw bytes from PAK using CUE4Parse
            Console.WriteLine("[1/5] Extracting target asset with CUE4Parse...");
            var (exactInternalPath, uassetBytes, uexpBytes) = ExtractAssetBytes(targetFileName);

            // 2. Parse combined memory streams into UAsset
            Console.WriteLine("[2/5] Parsing asset with UAssetAPI...");
            UAsset asset = ParseAssetFromMemory(uassetBytes, uexpBytes);

            // 3. Mutate properties using callback logic
            Console.WriteLine("[3/5] Applying property mutations...");
            ApplyPatchesToAsset(asset, patchAction);

            // 4. Save modified asset to disk and create virtual mount path staging
            Console.WriteLine("[4/5] Writing modified asset and staging directory structure...");
            string stagingDir = StageModifiedAsset(asset, exactInternalPath, targetFileName);

            // 5. Execute repak process
            Console.WriteLine("[5/5] Packing modified files into .pak archive via repak...");
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string outputPakPath = Path.GetFullPath(Path.Combine(baseDir, outputPakName));

            RunRepak(stagingDir, outputPakPath);

            // Cleanup
            if (Directory.Exists(stagingDir)) {
                Directory.Delete(stagingDir, true);
            }

            Console.WriteLine($"\nSuccessfully generated PAK at:\n -> {outputPakPath}");
        }

        // =========================================================================
        // MODULAR PIPELINE FUNCTIONS
        // =========================================================================

        private (string exactInternalPath, byte[] uassetBytes, byte[]? uexpBytes) ExtractAssetBytes(string targetFileName) {
            var provider = new DefaultFileProvider(
                directory: _paksDir,
                searchOption: SearchOption.TopDirectoryOnly,
                versions: new VersionContainer(EGame.GAME_UE4_24),
                pathComparer: StringComparer.OrdinalIgnoreCase
            );

            FileInfo specificPak = new FileInfo(Path.Combine(_paksDir, "ProjectWingman-WindowsNoEditor.pak"));
            if (specificPak.Exists) {
                provider.RegisterVfs(specificPak);
            }

            provider.Initialize();
            provider.Mount();

            var matchedEntry = provider.Files.FirstOrDefault(file => file.Key.EndsWith(targetFileName, StringComparison.OrdinalIgnoreCase));
            if (matchedEntry.Value == null) {
                throw new FileNotFoundException($"Could not locate '{targetFileName}' inside mounted PAK files.");
            }

            string exactInternalPath = matchedEntry.Key;
            string basePathWithoutExtension = exactInternalPath.Substring(0, exactInternalPath.LastIndexOf('.'));

            byte[] uassetBytes = provider.SaveAsset(basePathWithoutExtension + ".uasset")
                ?? throw new InvalidOperationException($"Failed to extract .uasset data for {basePathWithoutExtension}.uasset");

            byte[]? uexpBytes = null;
            string uexpPath = basePathWithoutExtension + ".uexp";
            if (provider.Files.ContainsKey(uexpPath)) {
                uexpBytes = provider.SaveAsset(uexpPath);
            }

            return (exactInternalPath, uassetBytes, uexpBytes);
        }

        private UAsset ParseAssetFromMemory(byte[] uassetBytes, byte[]? uexpBytes) {
            using MemoryStream combinedStream = new MemoryStream();
            combinedStream.Write(uassetBytes, 0, uassetBytes.Length);
            if (uexpBytes != null) {
                combinedStream.Write(uexpBytes, 0, uexpBytes.Length);
            }
            combinedStream.Position = 0;

            using (AssetBinaryReader reader = new AssetBinaryReader(combinedStream)) {
                return new UAsset(reader, _engineVersion);
            }
        }

        private void ApplyPatchesToAsset(UAsset asset, Action<PropertyData> patchAction) {
            foreach (Export export in asset.Exports) {
                if (export is DataTableExport dataTableExport) {
                    foreach (StructPropertyData row in dataTableExport.Table.Data) {
                        foreach (PropertyData rowProp in row.Value) {
                            patchAction(rowProp);
                        }
                    }
                }
            }
        }

        private string StageModifiedAsset(UAsset asset, string exactInternalPath, string targetFileName) {
            string tempWorkDir = Path.Combine(Path.GetTempPath(), "SicarioTemp_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempWorkDir);

            string baseFileName = Path.GetFileNameWithoutExtension(targetFileName);
            string tempOutputPath = Path.Combine(tempWorkDir, "Modified_" + baseFileName);

            asset.UseSeparateBulkDataFiles = true;
            asset.Write(tempOutputPath + ".uasset");

            // Prepare staging directory matching Project Wingman's internal path
            string stagingDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "staging_mymods_p");
            if (Directory.Exists(stagingDir)) {
                Directory.Delete(stagingDir, true);
            }

            string internalDirectory = Path.GetDirectoryName(exactInternalPath)!.TrimStart('\\', '/');
            string fullStagingAssetPath = Path.Combine(stagingDir, internalDirectory);
            Directory.CreateDirectory(fullStagingAssetPath);

            // Copy generated binaries into staging folder
            File.Copy(tempOutputPath + ".uasset", Path.Combine(fullStagingAssetPath, targetFileName), true);

            string tempUexp = tempOutputPath + ".uexp";
            if (File.Exists(tempUexp)) {
                string targetUexpName = Path.ChangeExtension(targetFileName, ".uexp");
                File.Copy(tempUexp, Path.Combine(fullStagingAssetPath, targetUexpName), true);
            }

            // Clean up initial temp dump directory
            Directory.Delete(tempWorkDir, true);

            return stagingDir;
        }

        private void RunRepak(string stagingDir, string outputPakPath) {
            if (!File.Exists(_repakExePath)) {
                throw new FileNotFoundException($"Could not locate repak.exe at: {_repakExePath}");
            }

            var startInfo = new ProcessStartInfo {
                FileName = _repakExePath,
                Arguments = $"pack -v --version V8B --compression Zlib \"{stagingDir}\" \"{outputPakPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            string stdout = process?.StandardOutput.ReadToEnd() ?? "";
            string stderr = process?.StandardError.ReadToEnd() ?? "";
            process?.WaitForExit();

            if (process?.ExitCode != 0) {
                throw new Exception($"repak execution failed with code {process?.ExitCode}:\n{stderr}");
            }

            Console.WriteLine(stdout);
        }
    }
}
