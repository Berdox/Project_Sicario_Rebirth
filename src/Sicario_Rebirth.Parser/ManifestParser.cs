using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using System.Diagnostics;


namespace Sicario_Rebirth.Parser {
    public class ManifestParser {

        public static readonly Regex VarRegex = new(@"\{\{\s*vars\.(\w+)\s*\}\}", RegexOptions.Compiled);

        public List<ModDefinition> ParseFile(string filePath) {
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException($"Manifest file not found: {filePath}");
            }

            string manifestText = File.ReadAllText(filePath);
            return ParseFile(manifestText, sourceFilePath: filePath);
        }

        public List<ModDefinition> ParseStream(Stream stream, string? sourceFilePath = null) { 
            using var reader = new StreamReader(stream);
            string manifestText = reader.ReadToEnd();
            return ParseText(manifestText, sourceFilePath);
        }

        public List<ModDefinition> ParseText(string manifestText, string? sourceFilePath = null) {
            
            var modDefinitions = new List<ModDefinition>();

            if (string.IsNullOrWhiteSpace(manifestText)) { 
                return modDefinitions;
            }

            JsonNode root = JsonNode.Parse(manifestText)
                  ?? throw new InvalidDataException("Invalid or null JSON manifest payload.");

            JsonArray? modsArray = root["mods"]?.AsArray();

            if(modsArray == null) {
                return modDefinitions;
            }

            foreach(JsonNode modNode in modsArray) {

                if(modNode == null) { 
                    continue; 
                }

                var modDef = new ModDefinition {
                    SourceFilePath = sourceFilePath ?? string.Empty,
                    ID = modNode["_id"]?.ToString() ?? string.Empty,
                };

                JsonNode? meta = modNode["_meta"];
                if (meta != null) { 
                    modDef.DisplayName = meta["displayname"]?.ToString() ?? "Untitled Mod";
                    modDef.Author = meta["author"]?.ToString() ?? "Unknown";
                    modDef.Description = meta["description"]?.ToString() ?? string.Empty;
                }

                JsonNode? sicario_flags = modNode["_sicario"];
                if (sicario_flags != null) {
                    modDef.Flags.IsPrivate = sicario_flags["private"]?.GetValue<bool>() ?? false;
                    modDef.Flags.IsPreview = sicario_flags["preview"]?.GetValue<bool>() ?? false;
                    modDef.Flags.Overwrite = sicario_flags["overwrites"]?.GetValue<bool>() ?? false;
                }

                var localVars = new Dictionary<string, string>();
                JsonObject? varsObj = modNode["_vars"]?.AsObject();

                if (varsObj != null) { 
                    foreach(var (key, valueNode) in varsObj) {
                        if (valueNode != null) {
                            localVars[key] = valueNode.ToString();
                        }
                    }
                }


        }
}
