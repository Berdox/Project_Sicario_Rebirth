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
            return ParseText(manifestText, filePath);
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

            if (modsArray == null) {
                return modDefinitions;
            }

            foreach (JsonNode modNode in modsArray) {

                if (modNode == null) {
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
                    modDef.Flags.Overwrites = sicario_flags["overwrites"]?.GetValue<bool>() ?? false;
                }

                var localVars = new Dictionary<string, string>();
                JsonObject? varsObj = modNode["_vars"]?.AsObject();

                if (varsObj != null) {
                    foreach (var (key, valueNode) in varsObj) {
                        if (valueNode != null) {
                            localVars[key] = valueNode.ToString();
                        }
                    }
                }

                JsonObject? assetPatchesObj = modNode["assetPatches"]?.AsObject();
                if (assetPatchesObj != null) {
                    foreach (var (assetPath, groupsNode) in assetPatchesObj) {
                        var groupsList = new List<PatchGroup>();

                        if (groupsNode is JsonArray groupsArray) {
                            foreach (JsonNode? groupNode in groupsArray) {
                                if (groupNode == null) continue;

                                var group = new PatchGroup {
                                    Name = groupNode["name"]?.ToString() ?? "Unnamed Group"
                                };

                                JsonArray? patchesArray = groupNode["patches"]?.AsArray();
                                if (patchesArray != null) {
                                    foreach (JsonNode? patchNode in patchesArray) {
                                        if (patchNode == null) continue;

                                        string rawValue = patchNode["value"]?.ToString() ?? string.Empty;

                                        group.Patches.Add(new PatchInstruction {
                                            Version = patchNode["version"]?.GetValue<int>() ?? 1,
                                            Description = patchNode["description"]?.ToString() ?? string.Empty,
                                            Template = patchNode["template"]?.ToString() ?? string.Empty,
                                            Type = patchNode["type"]?.ToString() ?? string.Empty,
                                            // Automatically resolve {{ vars.xyz }} interpolations
                                            Value = ResolveVariables(rawValue, localVars)
                                        });
                                    }
                                }

                                groupsList.Add(group);
                            }
                        }

                        modDef.AssetPatches[assetPath] = groupsList;
                    }
                }


                JsonObject? filePatchesObj = modNode["filePatches"]?.AsObject();
                if (filePatchesObj != null) {
                    foreach (var (key, value) in filePatchesObj) {
                        if (value != null) {
                            modDef.FilePatches[key] = value.ToString();
                        }
                    }
                }

               modDefinitions.Add(modDef);
            }

            return modDefinitions;
        }

        private static string ResolveVariables(string input, Dictionary<string, string> vars) {
            if (string.IsNullOrEmpty(input) || !input.Contains("{{")) {
                return input;
            }

            return VarRegex.Replace(input, match => {
                string varName = match.Groups[1].Value;
                return vars.TryGetValue(varName, out string? value) ? value : match.Value;
            });
        }
    }
}
