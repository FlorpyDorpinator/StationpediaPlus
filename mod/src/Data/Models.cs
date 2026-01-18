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
        public List<GuideDescription> guides;  // Custom guides that appear under the Guides button
        public List<GuideDescription> mechanics;  // Game mechanics that appear under the Game Mechanics button
        public GenericDescriptionsData genericDescriptions;
    }

    /// <summary>
    /// A custom guide page that appears under the "Guides" button in Stationpedia.
    /// Uses the same OperationalDetails format as devices for consistent editing.
    /// </summary>
    [Serializable]
    public class GuideDescription
    {
        /// <summary>Unique key for this guide (e.g., "DaylightSensorGuide")</summary>
        public string guideKey;
        
        /// <summary>Display name shown on the guide button (e.g., "Daylight Sensor Guide")</summary>
        public string displayName;
        
        /// <summary>Main description text at the top of the guide page</summary>
        public string pageDescription;
        
        /// <summary>Text to prepend to the page description</summary>
        public string pageDescriptionPrepend;
        
        /// <summary>Text to append to the page description</summary>
        public string pageDescriptionAppend;
        
        /// <summary>Image file to display at the top of the guide (like vanilla guides)</summary>
        public string pageImage;
        
        /// <summary>Operational details sections for the guide content</summary>
        [JsonProperty("operationalDetails")]
        public List<OperationalDetail> operationalDetails;
        
        [JsonProperty("OperationalDetails")]
        public List<OperationalDetail> OperationalDetailsAlt { set { operationalDetails = value; } }
        
        /// <summary>Optional: hex color for section titles</summary>
        public string operationalDetailsTitleColor;
        
        /// <summary>If true, generates a Table of Contents</summary>
        public bool generateToc { get; set; } = false;
        
        /// <summary>Custom title for the TOC panel</summary>
        public string tocTitle { get; set; }
        
        /// <summary>If true, TOC entries are flat (no nested indentation for children)</summary>
        public bool tocFlat { get; set; } = false;
        
        /// <summary>Custom background color for sections</summary>
        public string operationalDetailsBackgroundColor { get; set; }
        
        /// <summary>Button color: "blue" (default, like Universe), "orange" (like Guides), or hex color</summary>
        public string buttonColor { get; set; } = "blue";
        
        /// <summary>Sort order - lower numbers appear first</summary>
        public int sortOrder { get; set; } = 100;
    }

    [Serializable]
    public class DeviceDescriptions
    {
        public string deviceKey;
        public string displayName;
        public string pageDescription;        // Replace entire description
        public string pageDescriptionAppend;  // Add to end of existing description
        public string pageDescriptionPrepend; // Add to beginning of existing description
        public string pageImage;              // Image to display at top of page (like vanilla guides)
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
        
        /// <summary>If true, generates a Table of Contents panel at the top of Operational Details</summary>
        public bool generateToc { get; set; } = false;
        
        /// <summary>Custom title for the TOC panel (default: "Contents")</summary>
        public string tocTitle { get; set; }
        
        /// <summary>If true, TOC entries are flat (no nested indentation for children)</summary>
        public bool tocFlat { get; set; } = false;
        
        /// <summary>Custom background color for operational details sections (hex format)</summary>
        public string operationalDetailsBackgroundColor { get; set; }
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

    /// <summary>
    /// Represents a single row in a markdown-style table.
    /// The first row is treated as the header row with bold text.
    /// </summary>
    [Serializable]
    public class TableRow
    {
        /// <summary>Cell contents for this row. Column count is determined by the first row.</summary>
        public List<string> cells;
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
        
        // Advanced features
        /// <summary>If true (default), this detail renders as a collapsible StationpediaCategory. Set to false for inline headers.</summary>
        public bool collapsible { get; set; } = true;
        
        /// <summary>If set, this section appears in the Table of Contents with this ID for scroll-to linking</summary>
        public string tocId { get; set; }
        
        /// <summary>If set, displays this image file (relative to mod images folder) inline</summary>
        public string imageFile { get; set; }
        
        /// <summary>Custom background color for this section (hex format like "#1A2B3C")</summary>
        public string backgroundColor { get; set; }
        
        /// <summary>If set, displays a clickable YouTube link that opens in the system browser</summary>
        public string youtubeUrl { get; set; }
        
        /// <summary>Custom label for the YouTube link (default: "Watch on YouTube")</summary>
        public string youtubeLabel { get; set; }
        
        /// <summary>If set, displays an embedded video player for this MP4/video file (relative to mod images folder)</summary>
        public string videoFile { get; set; }
        
        /// <summary>If set, displays a markdown-style table. First row is headers (bold), cells are center-aligned.</summary>
        public List<TableRow> table { get; set; }
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
        
        // Flat dictionary to capture any top-level string values not in nested categories
        // This is populated during deserialization for properties like Flashpoint, Autoignition, etc.
        [Newtonsoft.Json.JsonExtensionData]
        public Dictionary<string, Newtonsoft.Json.Linq.JToken> AdditionalData { get; set; }
    }
}
