using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace StationpediaAscended.Data
{
    [Serializable]
    public class DescriptionsRoot
    {
        public string version;
        public List<DeviceDescriptions> devices;
        public GenericDescriptionsData genericDescriptions;
    }

    [Serializable]
    public class DeviceDescriptions
    {
        public string deviceKey;
        public string displayName;
        public string pageDescription;        // Replace entire description
        public string pageDescriptionAppend;  // Add to end of existing description
        public string pageDescriptionPrepend; // Add to beginning of existing description
        public Dictionary<string, LogicDescription> logicDescriptions;
        public Dictionary<string, ModeDescription> modeDescriptions;
        public Dictionary<string, SlotDescription> slotDescriptions;
        public Dictionary<string, VersionDescription> versionDescriptions;
        public Dictionary<string, MemoryDescription> memoryDescriptions;
        
        [JsonProperty("operationalDetails")]
        public List<OperationalDetail> operationalDetails;
        
        [JsonProperty("OperationalDetails")]
        public List<OperationalDetail> OperationalDetailsAlt { set { operationalDetails = value; } }
        
        public string operationalDetailsTitleColor; // Optional: hex color like "#FF7A18" for the category title
    }

    [Serializable]
    public class LogicDescription
    {
        public string dataType;
        public string range;
        public string description;
    }

    [Serializable]
    public class ModeDescription
    {
        public string modeValue;
        public string description;
    }

    [Serializable]
    public class SlotDescription
    {
        public string slotType;
        public string description;
    }

    [Serializable]
    public class VersionDescription
    {
        public string description;
    }

    [Serializable]
    public class MemoryDescription
    {
        public string opCode;
        public string parameters;
        public string description;
        public string byteLayout;
    }

    [Serializable]
    public class OperationalDetail
    {
        public string title;
        public string description;
        
        // Nested content support
        public List<OperationalDetail> children;  // Nested subsections
        public List<string> items;                 // Bullet list items
        public List<string> steps;                 // Numbered step list
    }

    [Serializable]
    public class PropertyDescription
    {
        public string type;
        public string threshold;
        public string description;
        public string formula;
    }

    [Serializable]
    public class GenericDescriptionsData
    {
        public Dictionary<string, string> logic;
        public Dictionary<string, string> slotTypes;
        public Dictionary<string, string> slots;
        public Dictionary<string, string> modes;
        public Dictionary<string, string> versions;
        public Dictionary<string, string> connections;
        public Dictionary<string, MemoryDescription> memory;
        public Dictionary<string, PropertyDescription> properties;
    }
}
