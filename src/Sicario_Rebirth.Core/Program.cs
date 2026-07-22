using CUE4Parse.Encryption.Aes;
// CUE4Parse
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Versions;
// NetPak
using NetPak;
using System;
using System.IO;
using System.Threading.Tasks;
// UAssetAPI
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;
// Disambiguate FString between NetPak and UAssetAPI
using UAssetFString = UAssetAPI.UnrealTypes.FString;

class Program {
    static async Task Main(string[] args) {
        //string inputPakPath = @"C:\Games\YourGame\Content\Paks\GameContent.pak";
        //string outputPakPath = @"C:\Games\YourGame\Content\Paks\GameContent_P.pak";

        // Paths inside the PAK (use lowercase/normalized relative paths for CUE4Parse lookups)
        string internalAssetPath = "ProjectWingman/Content/ProjectWingman/Blueprints/Data/AircraftData/DB_Aircraft";
        string uassetPath = $"{internalAssetPath}.uasset";
        string uexpPath = $"{internalAssetPath}.uexp";

        // ---------------------------------------------------------------------
        // STEP 1: Extract via CUE4Parse
        // ---------------------------------------------------------------------
        string pakFilePath = @"G:\SteamLibrary\steamapps\common\Project Wingman\ProjectWingman\Content\Paks\ProjectWingman-WindowsNoEditor.pak";
        string paksDirectory = @"G:\SteamLibrary\steamapps\common\Project Wingman\ProjectWingman\Content\Paks";

        Console.WriteLine("[1/3] Initializing CUE4Parse provider...");

        // 1. Pass the parent directory to DefaultFileProvider
        var provider = new DefaultFileProvider(
            directory: paksDirectory,
            searchOption: SearchOption.TopDirectoryOnly,
            versions: new VersionContainer(EGame.GAME_UE4_27),
            pathComparer: StringComparer.OrdinalIgnoreCase
        );

        // 2. Manually register PAK files if auto-discovery skips them
        if(File.Exists(pakFilePath)) { 
            // Register the .pak file using a FileInfo object
            provider.RegisterVfs(new FileInfo(pakFilePath));
        }
        else {
            // Or register all .pak files in the directory
            foreach (var file in Directory.GetFiles(paksDirectory, "*.pak", SearchOption.TopDirectoryOnly)) {
                provider.RegisterVfs(file);
            }
        }

        // 3. Initialize and mount registered VFS containers
        provider.Initialize();

        provider.Mount();

        // Print results
        Console.WriteLine($"Mounted {provider.MountedVfs.Count} PAK containers.");
        Console.WriteLine($"Total indexed assets: {provider.Files.Count}");

        byte[] rawUassetBytes = null;
        byte[] rawUexpBytes = null;

        //if (provider.Files.TryGetValue(uassetPath, out var uassetGameFile)) {
        //    rawUassetBytes = await uassetGameFile.ReadAsync();
        //}
        //else {
        //    Console.WriteLine($"Error: Could not find '{uassetPath}' in CUE4Parse files dictionary.");
        //    return;
        //}

        if (provider.Files.TryGetValue(uexpPath, out var uexpGameFile)) {
            rawUexpBytes = await uexpGameFile.ReadAsync();
            Console.WriteLine("Found exp");
        }
        else {
            Console.WriteLine($"Error: Could not find '{uexpPath}' in CUE4Parse files dictionary.");
            return;
        }

        // ---------------------------------------------------------------------
        // STEP 2: Parse and Modify with UAssetAPI
        // ---------------------------------------------------------------------
        //Console.WriteLine("[2/3] Parsing asset with UAssetAPI...");

        //UAsset asset = new UAsset(EngineVersion.VER_UE4_27);

        //// 1. Prepare input streams
        //using (MemoryStream uassetStream = new MemoryStream(rawUassetBytes))
        //using (MemoryStream uexpStream = rawUexpBytes != null ? new MemoryStream(rawUexpBytes) : null) {
        //    // Create AssetBinaryReader for .uasset
        //    using (AssetBinaryReader reader = new AssetBinaryReader(uassetStream, asset)) {
        //        // Pass the .uexp stream directly into the reader's UexpStream property
        //        if (uexpStream != null) {
        //            reader.UexpStream = uexpStream;
        //        }

        //        // Parse the asset
        //        asset.Read(reader);
        //    }
        //}

        //// 2. Modify properties inside exports
        //foreach (Export export in asset.Exports) {
        //    if (export is NormalExport normalExport) {
        //        foreach (PropertyData prop in normalExport.Data) {
        //            if (prop is StrPropertyData strProp && prop.Name.Value.ToString() == "TitleText") {
        //                strProp.Value = UAssetFString.FromString("Modified Title Screen!");
        //                Console.WriteLine($"Property updated to: {strProp.Value}");
        //            }
        //        }
        //    }
        //}

        //// 3. Serialize back out to MemoryStreams
        //byte[] modifiedUassetBytes;
        //byte[] modifiedUexpBytes;

        //using (MemoryStream outputUassetStream = new MemoryStream())
        //using (MemoryStream outputUexpStream = new MemoryStream()) {
        //    // Create AssetBinaryWriter linked to the output .uasset stream
        //    using (AssetBinaryWriter writer = new AssetBinaryWriter(outputUassetStream, asset)) {
        //        // Link the output .uexp stream to the writer
        //        writer.UexpStream = outputUexpStream;

        //        // WriteData requires the AssetBinaryWriter instance
        //        asset.WriteData(writer);
        //    }

        //    modifiedUassetBytes = outputUassetStream.ToArray();
        //    modifiedUexpBytes = outputUexpStream.ToArray();
        //}

        // ---------------------------------------------------------------------
        // STEP 3: Create patch PAK with NetPak
        // ---------------------------------------------------------------------
        //Console.WriteLine("[3/3] Generating patch file with NetPak...");

        //// Fix for CS1503 String to NetPak.FString conversions:
        //// Wrap string literals in new NetPak.FString(...) where NetPak expects its FString type
        //NetPak.FString mountPoint = new NetPak.FString("../../../GameName/");
        //NetPak.FString netpakUassetPath = new NetPak.FString(uassetPath);
        //NetPak.FString netpakUexpPath = new NetPak.FString(uexpPath);

        //using (PakFile patchPak = PakFile.Create(new NetPak.FString(outputPakPath), mountPoint)) {
        //    patchPak.AddEntry(netpakUassetPath, modifiedUassetBytes);

        //    if (modifiedUexpBytes.Length > 0) {
        //        patchPak.AddEntry(netpakUexpPath, modifiedUexpBytes);
        //    }

        //    patchPak.Save(new NetPak.FString(outputPakPath));
        //}

        //Console.WriteLine($"Done! Successfully saved: {outputPakPath}");
    }
}