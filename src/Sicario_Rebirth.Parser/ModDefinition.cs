using System.Collections.Generic;

namespace Sicario_Rebirth.Parser {
    public class ModDefinition {

        public string SourceFilePath { get; set; } = string.Empty;
        public string ID { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description {  get; set; } = string.Empty;

        public SicarioFlags Flags { get; set; } = new();

        public Dictionary<string, List<PatchGroup>> AssetPatches { get; set; } = new();

        public Dictionary<string, string> FilePatches { get; set; } = new();
    }

    public class SicarioFlags {
        public bool IsPrivate { get; set; } = false;
        public bool IsPreview { get; set; } = false;
        public bool Overwrites { get; set; } = false;
    }

    public class PatchGroup {
        public string Name { get; set; } = string.Empty;
        public List<PatchInstruction> Patches { get; set; } = new();
    }

    public class PatchInstruction {
        public int Version { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
