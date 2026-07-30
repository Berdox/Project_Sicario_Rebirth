//using CUE4Parse.Encryption.Aes;
//// CUE4Parse
//using CUE4Parse.FileProvider;
//using CUE4Parse.UE4.Pak;
//using CUE4Parse.UE4.Versions;
//// NetPak
//using NetPak;
//using System;
//using System.IO;
//using System.Threading.Tasks;
//// UAssetAPI
//using UAssetAPI;
//using UAssetAPI.ExportTypes;
//using UAssetAPI.PropertyTypes.Objects;
//using UAssetAPI.UnrealTypes;
//// Disambiguate FString between NetPak and UAssetAPI
//using UAssetFString = UAssetAPI.UnrealTypes.FString;

//class Program {
//    static async Task Main(string[] args) {
//        //string inputPakPath = @"C:\Games\YourGame\Content\Paks\GameContent.pak";
//        //string outputPakPath = @"C:\Games\YourGame\Content\Paks\GameContent_P.pak";

//        // Paths inside the PAK (use lowercase/normalized relative paths for CUE4Parse lookups)
//        string internalAssetPath = "ProjectWingman/Content/ProjectWingman/Blueprints/Data/AircraftData/DB_Aircraft";
//        string uassetPath = $"{internalAssetPath}.uasset";
//        string uexpPath = $"{internalAssetPath}.uexp";

//        // ---------------------------------------------------------------------
//        // STEP 1: Extract via CUE4Parse
//        // ---------------------------------------------------------------------
//        string pakFilePath = @"G:\SteamLibrary\steamapps\common\Project Wingman\ProjectWingman\Content\Paks\ProjectWingman-WindowsNoEditor.pak";
//        string paksDirectory = @"G:\SteamLibrary\steamapps\common\Project Wingman\ProjectWingman\Content\Paks";

//        Console.WriteLine("[1/3] Initializing CUE4Parse provider...");

//        // 1. Pass the parent directory to DefaultFileProvider
//        var provider = new DefaultFileProvider(
//            directory: paksDirectory,
//            searchOption: SearchOption.TopDirectoryOnly,
//            versions: new VersionContainer(EGame.GAME_UE4_24),
//            pathComparer: StringComparer.OrdinalIgnoreCase
//        );

//        // 2. Manually register PAK files if auto-discovery skips them
//        if(File.Exists(pakFilePath)) { 
//            // Register the .pak file using a FileInfo object
//            provider.RegisterVfs(new FileInfo(pakFilePath));
//        }
//        else {
//            // Or register all .pak files in the directory
//            foreach (var file in Directory.GetFiles(paksDirectory, "*.pak", SearchOption.TopDirectoryOnly)) {
//                provider.RegisterVfs(file);
//            }
//        }

//        // 3. Initialize and mount registered VFS containers
//        provider.Initialize();

//        provider.Mount();

//        // Print results
//        Console.WriteLine($"Mounted {provider.MountedVfs.Count} PAK containers.");
//        Console.WriteLine($"Total indexed assets: {provider.Files.Count}");

//        byte[] rawUassetBytes = null;
//        byte[] rawUexpBytes = null;

//        if (provider.Files.TryGetValue(uassetPath, out var uassetGameFile)) {
//            rawUassetBytes = await uassetGameFile.ReadAsync();
//        }
//        else {
//            Console.WriteLine($"Error: Could not find '{uassetPath}' in CUE4Parse files dictionary.");
//            return;
//        }

//        if (provider.Files.TryGetValue(uexpPath, out var uexpGameFile)) {
//            rawUexpBytes = await uexpGameFile.ReadAsync();
//            Console.WriteLine("Found exp");
//        }
//        else {
//            Console.WriteLine($"Error: Could not find '{uexpPath}' in CUE4Parse files dictionary.");
//            return;
//        }

//        // ---------------------------------------------------------------------
//        // STEP 2: Parse and Modify with UAssetAPI
//        // ---------------------------------------------------------------------
//        Console.WriteLine("[2/3] Parsing asset with UAssetAPI...");

//        UAsset asset = new UAsset(EngineVersion.VER_UE4_27);

//        // 1. Prepare input streams
//        using (MemoryStream uassetStream = new MemoryStream(rawUassetBytes))
//        using (MemoryStream uexpStream = rawUexpBytes != null ? new MemoryStream(rawUexpBytes) : null) {
//            // Create AssetBinaryReader for .uasset
//            using (AssetBinaryReader reader = new AssetBinaryReader(uassetStream, asset)) {
//                // Pass the .uexp stream directly into the reader's UexpStream property
//                if (uexpStream != null) {
//                    reader.UexpStream = uexpStream;
//                }

//                // Parse the asset
//                asset.Read(reader);
//            }
//        }

//        // 2. Modify properties inside exports
//        foreach (Export export in asset.Exports) {
//            if (export is NormalExport normalExport) {
//                foreach (PropertyData prop in normalExport.Data) {
//                    if (prop is StrPropertyData strProp && prop.Name.Value.ToString() == "TitleText") {
//                        strProp.Value = UAssetFString.FromString("Modified Title Screen!");
//                        Console.WriteLine($"Property updated to: {strProp.Value}");
//                    }
//                }
//            }
//        }

//        //// 3. Serialize back out to MemoryStreams
//        //byte[] modifiedUassetBytes;
//        //byte[] modifiedUexpBytes;

//        //using (MemoryStream outputUassetStream = new MemoryStream())
//        //using (MemoryStream outputUexpStream = new MemoryStream()) {
//        //    // Create AssetBinaryWriter linked to the output .uasset stream
//        //    using (AssetBinaryWriter writer = new AssetBinaryWriter(outputUassetStream, asset)) {
//        //        // Link the output .uexp stream to the writer
//        //        writer.UexpStream = outputUexpStream;

//        //        // WriteData requires the AssetBinaryWriter instance
//        //        asset.WriteData(writer);
//        //    }

//        //    modifiedUassetBytes = outputUassetStream.ToArray();
//        //    modifiedUexpBytes = outputUexpStream.ToArray();
//        //}

//        // ---------------------------------------------------------------------
//        // STEP 3: Create patch PAK with NetPak
//        // ---------------------------------------------------------------------
//        //Console.WriteLine("[3/3] Generating patch file with NetPak...");

//        //// Fix for CS1503 String to NetPak.FString conversions:
//        //// Wrap string literals in new NetPak.FString(...) where NetPak expects its FString type
//        //NetPak.FString mountPoint = new NetPak.FString("../../../GameName/");
//        //NetPak.FString netpakUassetPath = new NetPak.FString(uassetPath);
//        //NetPak.FString netpakUexpPath = new NetPak.FString(uexpPath);

//        //using (PakFile patchPak = PakFile.Create(new NetPak.FString(outputPakPath), mountPoint)) {
//        //    patchPak.AddEntry(netpakUassetPath, modifiedUassetBytes);

//        //    if (modifiedUexpBytes.Length > 0) {
//        //        patchPak.AddEntry(netpakUexpPath, modifiedUexpBytes);
//        //    }

//        //    patchPak.Save(new NetPak.FString(outputPakPath));
//        //}

//        //Console.WriteLine($"Done! Successfully saved: {outputPakPath}");
//    }
//}



using System;
using System.Diagnostics;
using System.IO;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

class Program {
    static void Main(string[] args) {
        string paksDirectory = @"G:\SteamLibrary\steamapps\common\Project Wingman\ProjectWingman\Content\Paks";
        string targetFileName = "DB_Aircraft.uasset";

        // =========================================================================
        // STEP 1: Extract .uasset and .uexp with CUE4Parse
        // =========================================================================
        Console.WriteLine("[1/3] Extracting target asset with CUE4Parse...");

        var provider = new DefaultFileProvider(
            directory: paksDirectory,
            searchOption: SearchOption.TopDirectoryOnly,
            versions: new VersionContainer(EGame.GAME_UE4_24), // Project Wingman uses UE 4.27
            pathComparer: StringComparer.OrdinalIgnoreCase
        );

        // Register specific PAK file if present
        FileInfo specificPak = new FileInfo(Path.Combine(paksDirectory, "ProjectWingman-WindowsNoEditor.pak"));
        if (specificPak.Exists) {
            provider.RegisterVfs(specificPak);
        }

        // Initialize and index VFS contents
        provider.Initialize();
        provider.Mount();

        Console.WriteLine($"Mounted {provider.MountedVfs.Count} PAK file(s).");
        Console.WriteLine($"Total files found across all PAKs: {provider.Files.Count}\n");

        // Locate the primary .uasset file dynamically in provider.Files
        var matchedEntry = provider.Files.FirstOrDefault(file => file.Key.EndsWith(targetFileName, StringComparison.OrdinalIgnoreCase));

        if (matchedEntry.Value == null) {
            throw new FileNotFoundException($"Could not locate '{targetFileName}' inside mounted PAK files.");
        }

        string exactInternalPath = matchedEntry.Key;
        Console.WriteLine($"Found target path in PAK: {exactInternalPath}");

        // Resolve base internal path without extension
        string basePathWithoutExtension = exactInternalPath.Substring(0, exactInternalPath.LastIndexOf('.'));

        // Extract .uasset bytes
        byte[] uassetBytes = provider.SaveAsset(basePathWithoutExtension + ".uasset");
        if (uassetBytes == null) {
            throw new InvalidOperationException($"Failed to extract .uasset data for {basePathWithoutExtension}.uasset");
        }

        // Extract .uexp bytes if present
        byte[] uexpBytes = null;
        string uexpPath = basePathWithoutExtension + ".uexp";
        if (provider.Files.ContainsKey(uexpPath)) {
            uexpBytes = provider.SaveAsset(uexpPath);
        }

        Console.WriteLine($"Successfully extracted .uasset ({uassetBytes.Length} bytes) and .uexp ({uexpBytes?.Length ?? 0} bytes).");

        // =========================================================================
        // STEP 2: Combine Streams and Parse with UAssetAPI
        // =========================================================================
        Console.WriteLine("[2/3] Modifying asset properties with UAssetAPI...");

        // Combine .uasset + .uexp into one continuous stream for UAssetAPI
        MemoryStream combinedStream = new MemoryStream();
        combinedStream.Write(uassetBytes, 0, uassetBytes.Length);
        if (uexpBytes != null) {
            combinedStream.Write(uexpBytes, 0, uexpBytes.Length);
        }
        combinedStream.Position = 0; // Reset stream pointer to beginning

        // Instantiate UAsset with UE4.27
        UAsset asset;
        using (AssetBinaryReader reader = new AssetBinaryReader(combinedStream)) {
            asset = new UAsset(reader, EngineVersion.VER_UE4_24);
        }

        Console.WriteLine($"Successfully loaded asset! Found {asset.Exports.Count} export(s).");

        // Modify DataTable export properties
        foreach (Export export in asset.Exports) {
            if (export is DataTableExport dataTableExport) {
                // DataTable row data lives inside dataTableExport.Table.Data
                foreach (StructPropertyData row in dataTableExport.Table.Data) {
                    foreach (PropertyData rowProp in row.Value) {
                        ModifyPropertyRecursive(rowProp);
                    }
                }
            }
        }

        // =========================================================================
        // STEP 3: Save the modified asset back out
        // =========================================================================
        Console.WriteLine("[3/3] Saving modified asset...");

        string baseFileName = Path.GetFileNameWithoutExtension(targetFileName);
        string outputBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modified_" + baseFileName);

        asset.UseSeparateBulkDataFiles = true;

        // Write() automatically creates "Modified_DB_Aircraft.uasset" and "Modified_DB_Aircraft.uexp"
        asset.Write(outputBasePath + ".uasset");

        Console.WriteLine($"Saved modified asset to:\n  -> {outputBasePath}.uasset\n  -> {outputBasePath}.uexp");





        // =========================================================================
        // STEP 4: Package into PAK file using UnrealPak.exe
        // =========================================================================
        Console.WriteLine("[4/4] Packing modified files into .pak archive...");

        // 1. Resolve paths
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string unrealPakPath = Path.GetFullPath(Path.Combine(baseDir, "lib", "UnrealPakTool", "UnrealPak.exe"));

        if (!File.Exists(unrealPakPath)) {
            throw new FileNotFoundException($"Could not locate UnrealPak.exe at: {unrealPakPath}");
        }

        // Destination PAK output path (placed inside your staging directory or output folder)
        string outputPakPath = Path.GetFullPath(Path.Combine(baseDir, "ProjectWingman-PrezUnlock_P.pak"));

        // Base path to your generated files
        string modifiedUassetPath = Path.GetFullPath(outputBasePath + ".uasset");
        string modifiedUexpPath = Path.GetFullPath(outputBasePath + ".uexp");

        // 2. Define internal Unreal engine target mount paths
        // Note: Mount paths must use forward slashes (/) and start with standard game relative paths!
        // Example internal path base: "ProjectWingman/Content/ProjectWingman/Blueprints/Data/AircraftData/DB_Aircraft"
        string internalBasePath = exactInternalPath.Substring(0, exactInternalPath.LastIndexOf('.'));
        string internalUassetPath = internalBasePath + ".uasset";
        string internalUexpPath = internalBasePath + ".uexp";

        // 3. Create response file for UnrealPak.exe
        string responseFilePath = Path.GetFullPath(Path.Combine(baseDir, "pak_file_list.txt"));

        using (StreamWriter writer = new StreamWriter(responseFilePath)) {
            // Format: "LocalPathOnDisk" "MountPathInGame"
            writer.WriteLine($"\"{modifiedUassetPath}\" \"../../../{internalUassetPath}\"");
            writer.WriteLine($"\"{modifiedUexpPath}\" \"../../../{internalUexpPath}\"");
        }

        Console.WriteLine($"Generated response file at: {responseFilePath}");

        // Ensure minimal engine directory structure exists beside UnrealPak.exe
        string engineConfigDir = Path.Combine(Path.GetDirectoryName(unrealPakPath), "Engine", "Config");
        Directory.CreateDirectory(engineConfigDir);
        string baseEngineIni = Path.Combine(engineConfigDir, "BaseEngine.ini");

        if (!File.Exists(baseEngineIni)) {
            File.WriteAllText(baseEngineIni, "[DerivedDataBackendGraph]\nMinimumFreeSpaceGigaBytes=1\n");
        }

        // 4. Run UnrealPak.exe process with -NoDDC
        ProcessStartInfo startInfo = new ProcessStartInfo {
            FileName = unrealPakPath,
            // -NoDDC prevents DerivedDataCache crashes in standalone builds
            // -compressed uses the modern compression flag
            Arguments = $"\"{outputPakPath}\" -create=\"{responseFilePath}\" -compressed -NoDDC",
            WorkingDirectory = Path.GetDirectoryName(unrealPakPath), // Set working dir to UnrealPak.exe folder
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (Process process = new Process { StartInfo = startInfo }) {
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0) {
                Console.WriteLine($"\n[SUCCESS] Successfully created PAK file at:\n  -> {outputPakPath}");
            }
            else {
                Console.WriteLine($"\n[ERROR] UnrealPak failed with exit code {process.ExitCode}:");
                Console.WriteLine(error);
                Console.WriteLine(output);
            }
        }
    }

   public static void ModifyPropertyRecursive(PropertyData prop) {
        // Check if we reached the target IntProperty "PilotCount"
        if (prop.Name.ToString().Contains("PilotCount") && prop is IntPropertyData intProp) {
            // Change PilotCount from 1 to 2
            intProp.Value = 2;
            Console.WriteLine($"Updated {prop.Name} to {intProp.Value}");
            return;
        }

        // If it's a nested struct (like BoneDetails), drill down into its child properties
        if (prop is StructPropertyData structProp) {
            foreach (PropertyData childProp in structProp.Value) {
                ModifyPropertyRecursive(childProp);
            }
        }
        // If it's an array of structs/properties, loop through elements
        else if (prop is ArrayPropertyData arrayProp) {
            foreach (PropertyData elemProp in arrayProp.Value) {
                ModifyPropertyRecursive(elemProp);
            }
        }
    }
}