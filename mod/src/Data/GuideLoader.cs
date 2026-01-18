using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace StationpediaAscended.Data
{
    /// <summary>
    /// Loads and converts custom markdown guides into Stationpedia-compatible format.
    /// Supports MIPS syntax highlighting, device references, and collapsible sections.
    /// </summary>
    public static class GuideLoader
    {
        // Device name to prefab key mapping
        private static readonly Dictionary<string, string> DeviceNameMapping = new Dictionary<string, string>
        {
            { "Daylight Sensor", "ThingStructureDaylightSensor" },
            { "Solar Panel", "ThingStructureSolarPanel" },
            { "Solar Panel (Dual)", "ThingStructureSolarPanelDual" },
            { "StructureSolarPanelDual", "ThingStructureSolarPanelDual" },
            { "Logic Writer", "ThingStructureLogicWriter" },
            { "Batch Writer", "ThingStructureLogicBatchWriter" },
            { "Logic Memory", "ThingStructureLogicMemory" },
            { "Battery", "ThingStructureBatterySmall" },
            { "Programmable Chip", "ThingMotherboardProgrammableChip" },
            { "IC10", "ThingMotherboardProgrammableChip" },
            { "Air Conditioner", "ThingStructureAirConditioner" },
            { "Tablet", "ThingItemAdvancedTablet" }
        };

        /// <summary>
        /// Load and parse a markdown guide file into DeviceDescriptions format
        /// </summary>
        /// <param name="filename">The markdown filename to load</param>
        /// <param name="pageKey">The Stationpedia page key (e.g., "DaylightSensorGuide")</param>
        /// <param name="displayTitle">The display title for the guide</param>
        /// <returns>DeviceDescriptions containing the parsed guide, or null if loading failed</returns>
        public static DeviceDescriptions LoadGuide(string filename, string pageKey, string displayTitle)
        {
            if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(pageKey))
            {
                StationpediaAscendedMod.Log?.LogWarning($"[GuideLoader] Invalid parameters: filename='{filename}', pageKey='{pageKey}'");
                return null;
            }

            string markdown = LoadMarkdownFile(filename);
            if (string.IsNullOrEmpty(markdown))
            {
                StationpediaAscendedMod.Log?.LogWarning($"[GuideLoader] Failed to load guide file: {filename}");
                return null;
            }

            return ParseMarkdownToGuide(markdown, pageKey, displayTitle);
        }

        /// <summary>
        /// Load markdown file from various possible locations
        /// </summary>
        private static string LoadMarkdownFile(string filename)
        {
            var possiblePaths = new List<string>();

            // Debug path
#if DEBUG
            possiblePaths.Add($@"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaAscended\mod\Guides\{filename}");
#endif

            // Relative to assembly
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                possiblePaths.Add(Path.Combine(assemblyPath, "Guides", filename));
            }

            // BepInEx scripts path
            if (!string.IsNullOrEmpty(BepInEx.Paths.BepInExRootPath))
            {
                possiblePaths.Add(Path.Combine(BepInEx.Paths.BepInExRootPath, "scripts", "Guides", filename));
            }

            foreach (var path in possiblePaths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    try
                    {
                        return File.ReadAllText(path, Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        StationpediaAscendedMod.Log?.LogWarning($"[GuideLoader] Failed to read file {path}: {ex.Message}");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Parse markdown content into Stationpedia DeviceDescriptions format
        /// </summary>
        /// <param name="content">The markdown content to parse</param>
        /// <param name="pageKey">The Stationpedia page key</param>
        /// <param name="displayTitle">The display title</param>
        /// <returns>DeviceDescriptions with parsed operational details</returns>
        public static DeviceDescriptions ParseMarkdownToGuide(string content, string pageKey, string displayTitle)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(pageKey))
            {
                return null;
            }

            var descriptions = new DeviceDescriptions
            {
                deviceKey = pageKey,
                displayName = displayTitle ?? pageKey,
                generateToc = true,
                operationalDetails = new List<OperationalDetail>()
            };

            // Parse the markdown content into operational details
            var details = ParseMarkdownSections(content);
            descriptions.operationalDetails = details;

            return descriptions;
        }

        /// <summary>
        /// Parse markdown content into a list of OperationalDetail sections
        /// </summary>
        private static List<OperationalDetail> ParseMarkdownSections(string content)
        {
            var sections = new List<OperationalDetail>();
            if (string.IsNullOrEmpty(content))
            {
                return sections;
            }

            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            OperationalDetail currentSection = null;
            OperationalDetail currentSubsection = null;
            StringBuilder contentBuilder = new StringBuilder();
            bool inCodeBlock = false;
            string codeBlockLanguage = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Handle code blocks
                if (line.TrimStart().StartsWith("```"))
                {
                    if (!inCodeBlock)
                    {
                        // Start of code block
                        inCodeBlock = true;
                        codeBlockLanguage = line.TrimStart().Substring(3).Trim();
                        contentBuilder.AppendLine("\n<mspace=0.5em>");
                    }
                    else
                    {
                        // End of code block
                        inCodeBlock = false;
                        contentBuilder.AppendLine("</mspace>\n");
                        codeBlockLanguage = string.Empty;
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    // Format code block content
                    string formattedCode = line;
                    if (codeBlockLanguage == "mips")
                    {
                        formattedCode = FormatMipsCode(line);
                    }
                    contentBuilder.AppendLine("  " + formattedCode);
                    continue;
                }

                // Check for table start (not in code block)
                if (IsTableLine(line))
                {
                    // Save any previous content
                    if (contentBuilder.Length > 0)
                    {
                        if (currentSubsection != null)
                        {
                            SaveCurrentContent(currentSubsection, contentBuilder);
                        }
                        else if (currentSection != null)
                        {
                            SaveCurrentContent(currentSection, contentBuilder);
                        }
                        contentBuilder.Clear();
                    }

                    // Collect table lines
                    var tableLines = new List<string> { line };
                    i++;
                    while (i < lines.Length && IsTableLine(lines[i]))
                    {
                        tableLines.Add(lines[i]);
                        i++;
                    }
                    i--; // Back up one since the for loop will increment

                    // Parse and convert table
                    string tableContent = ParseMarkdownTable(tableLines);
                    if (!string.IsNullOrEmpty(tableContent))
                    {
                        contentBuilder.AppendLine(tableContent);
                    }
                    continue;
                }

                // Check for section headers (# Title)
                var sectionMatch = Regex.Match(line, @"^#\s+(.+)$");
                if (sectionMatch.Success)
                {
                    // Save previous section
                    if (currentSection != null)
                    {
                        if (currentSubsection != null)
                        {
                            SaveCurrentContent(currentSubsection, contentBuilder);
                            currentSubsection = null;
                        }
                        else
                        {
                            SaveCurrentContent(currentSection, contentBuilder);
                        }
                    }

                    // Create new section
                    string sectionTitle = sectionMatch.Groups[1].Value.Trim();
                    currentSection = new OperationalDetail
                    {
                        title = sectionTitle,
                        tocId = GenerateTocId(sectionTitle),
                        collapsible = true,
                        children = new List<OperationalDetail>()
                    };
                    sections.Add(currentSection);
                    currentSubsection = null;
                    contentBuilder.Clear();
                    continue;
                }

                // Check for subsection headers (## Subtitle)
                var subsectionMatch = Regex.Match(line, @"^##\s+(.+)$");
                if (subsectionMatch.Success && currentSection != null)
                {
                    // Save previous subsection content
                    if (currentSubsection != null)
                    {
                        SaveCurrentContent(currentSubsection, contentBuilder);
                    }

                    // Create new subsection
                    string subsectionTitle = subsectionMatch.Groups[1].Value.Trim();
                    currentSubsection = new OperationalDetail
                    {
                        title = subsectionTitle,
                        tocId = GenerateTocId(subsectionTitle),
                        collapsible = true
                    };
                    currentSection.children.Add(currentSubsection);
                    contentBuilder.Clear();
                    continue;
                }

                // Skip horizontal rules
                if (line.Trim() == "---" || line.Trim() == "***" || line.Trim() == "___")
                {
                    continue;
                }

                // Convert markdown formatting and add to content
                string convertedLine = ConvertMarkdownLine(line);
                if (!string.IsNullOrWhiteSpace(convertedLine) || contentBuilder.Length > 0)
                {
                    contentBuilder.AppendLine(convertedLine);
                }
            }

            // Save final content
            if (currentSection != null)
            {
                if (currentSubsection != null)
                {
                    SaveCurrentContent(currentSubsection, contentBuilder);
                }
                else
                {
                    SaveCurrentContent(currentSection, contentBuilder);
                }
            }

            return sections;
        }

        /// <summary>
        /// Save accumulated content to an OperationalDetail section
        /// </summary>
        private static void SaveCurrentContent(OperationalDetail detail, StringBuilder content)
        {
            if (detail == null || content.Length == 0)
            {
                return;
            }

            string contentStr = content.ToString().Trim();
            if (!string.IsNullOrEmpty(contentStr))
            {
                detail.description = contentStr;
            }
        }

        /// <summary>
        /// Convert a single markdown line to Stationpedia format
        /// </summary>
        private static string ConvertMarkdownLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return line;
            }

            // Trim trailing whitespace but preserve leading for indentation
            line = line.TrimEnd();

            // Bold: **text** -> <b>text</b>
            line = Regex.Replace(line, @"\*\*([^*]+)\*\*", "<b>$1</b>");

            // Italic: *text* -> <i>text</i>
            line = Regex.Replace(line, @"\*([^*]+)\*", "<i>$1</i>");

            // Inline code: `text` -> <color=#88CCFF>text</color>
            line = Regex.Replace(line, @"`([^`]+)`", "<color=#88CCFF>$1</color>");

            // Convert device references
            line = ConvertDeviceReferences(line);

            // Convert bullet points: - text -> • text
            if (line.TrimStart().StartsWith("- "))
            {
                int indent = line.Length - line.TrimStart().Length;
                string indentStr = new string(' ', indent);
                line = indentStr + "• " + line.TrimStart().Substring(2);
            }

            // Convert numbered lists: 1. text -> keep as-is
            // (They're already in a good format)

            // Convert markdown links [text](url) -> text
            line = Regex.Replace(line, @"\[([^\]]+)\]\([^)]+\)", "$1");

            return line;
        }

        /// <summary>
        /// Replace device names with clickable {THING:PrefabKey} links
        /// </summary>
        public static string ConvertDeviceReferences(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // Process device name mappings
            foreach (var mapping in DeviceNameMapping)
            {
                // Case-insensitive replacement while preserving formatting
                var pattern = new Regex(Regex.Escape(mapping.Key), RegexOptions.IgnoreCase);
                text = pattern.Replace(text, $"{{THING:{mapping.Value}}}");
            }

            return text;
        }

        /// <summary>
        /// Format MIPS code with syntax highlighting
        /// </summary>
        public static string FormatMipsCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return code;
            }

            // Keywords: alias, define, yield, j, jal, jr
            var keywords = new[] { "alias", "define", "yield", "\\bj\\b", "jal", "jr" };
            foreach (var keyword in keywords)
            {
                code = Regex.Replace(code, $@"\b{keyword}\b", $"<color=#569CD6>{keyword}</color>", RegexOptions.IgnoreCase);
            }

            // Registers: r0-r15, sp, ra, db, d0-d5
            var registers = new[] { "r\\d{1,2}", "sp", "ra", "db", "d[0-5]" };
            foreach (var register in registers)
            {
                code = Regex.Replace(code, $@"\b{register}\b", $"<color=#9CDCFE>${{register}}</color>", RegexOptions.IgnoreCase);
            }

            // Instructions: l, s, add, sub, mul, div, mod, beq, bne, bgt, blt, bgtz, etc.
            var instructions = new[] { 
                "\\bl\\b", "\\bs\\b", "add", "sub", "mul", "div", "mod", 
                "beq", "bne", "bgt", "blt", "bgtz", "bltz", "addi", "subi", 
                "muli", "divi", "modi", "slt", "slti", "max", "min", "move"
            };
            foreach (var instruction in instructions)
            {
                code = Regex.Replace(code, $@"\b{instruction}\b", $"<color=#C586C0>{instruction}</color>", RegexOptions.IgnoreCase);
            }

            // Labels (word followed by colon at start): label:
            code = Regex.Replace(code, @"^(\s*)(\w+):", "$1<color=#DCDCAA>$2:</color>", RegexOptions.Multiline);

            // Comments: # to end of line
            code = Regex.Replace(code, @"#.*$", m => $"<color=#6A9955>{m.Value}</color>", RegexOptions.Multiline);

            // Numbers: decimal or hex
            code = Regex.Replace(code, @"\b(\d+|0x[0-9A-Fa-f]+)\b", $"<color=#B5CEA8>$1</color>");

            return code;
        }

        /// <summary>
        /// Generate a URL-safe TOC ID from a title
        /// </summary>
        private static string GenerateTocId(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return "section";
            }

            string id = title.ToLowerInvariant();
            id = Regex.Replace(id, @"[^a-z0-9]+", "_");
            id = id.Trim('_');
            return string.IsNullOrEmpty(id) ? "section" : id;
        }

        /// <summary>
        /// Detect if a line is part of a markdown table (starts with |)
        /// </summary>
        private static bool IsTableLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }
            return line.TrimStart().StartsWith("|") && line.Contains("|");
        }

        /// <summary>
        /// Detect if a line is a markdown table separator (|---|---|)
        /// </summary>
        private static bool IsSeparatorLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            string trimmed = line.Trim();
            if (!trimmed.StartsWith("|") || !trimmed.EndsWith("|"))
            {
                return false;
            }

            // Split by | and check if all non-empty parts contain only - and spaces
            var parts = trimmed.Split('|');
            foreach (var part in parts)
            {
                string trimmedPart = part.Trim();
                if (string.IsNullOrEmpty(trimmedPart))
                {
                    continue;
                }
                if (!Regex.IsMatch(trimmedPart, @"^-+(\s*:-?|:-?\s*)?$"))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Extract cell values from a markdown table row
        /// </summary>
        private static List<string> ExtractTableCells(string line)
        {
            var cells = new List<string>();
            if (string.IsNullOrEmpty(line))
            {
                return cells;
            }

            // Split by | and filter empty entries from start/end
            var parts = line.Split('|');
            for (int i = 1; i < parts.Length - 1; i++)
            {
                string cell = parts[i].Trim();
                // Remove inline formatting for cleaner output
                cell = Regex.Replace(cell, @"\*\*([^*]+)\*\*", "$1");  // Remove bold markers
                cell = Regex.Replace(cell, @"\*([^*]+)\*", "$1");       // Remove italic markers
                cell = Regex.Replace(cell, @"`([^`]+)`", "$1");         // Remove code markers
                cells.Add(cell);
            }
            return cells;
        }

        /// <summary>
        /// Convert markdown table lines to plain indented text format
        /// Format: First column becomes bold header, remaining columns become indented key-value pairs
        /// </summary>
        private static string ParseMarkdownTable(List<string> tableLines)
        {
            if (tableLines == null || tableLines.Count < 2)
            {
                return string.Empty;
            }

            var result = new StringBuilder();
            List<string> headers = null;

            // Extract headers from first line
            headers = ExtractTableCells(tableLines[0]);
            if (headers.Count == 0)
            {
                return string.Empty;
            }

            // Find and skip separator line
            int dataStartIndex = 1;
            for (int i = 1; i < tableLines.Count; i++)
            {
                if (IsSeparatorLine(tableLines[i]))
                {
                    dataStartIndex = i + 1;
                    break;
                }
            }

            // Process data rows
            bool isFirstRow = true;
            for (int i = dataStartIndex; i < tableLines.Count; i++)
            {
                var cells = ExtractTableCells(tableLines[i]);
                if (cells.Count == 0)
                {
                    continue;
                }

                // Add blank line between rows (but not before first row)
                if (!isFirstRow)
                {
                    result.AppendLine();
                }
                isFirstRow = false;

                // First column becomes the bold header
                string headerValue = cells[0];
                result.AppendLine($"<b>{headerValue}</b>");

                // Remaining columns become indented key-value pairs
                for (int j = 1; j < cells.Count && j < headers.Count; j++)
                {
                    string columnHeader = headers[j];
                    string cellValue = cells[j];
                    result.AppendLine($"    {columnHeader}: {cellValue}");
                }
            }

            return result.ToString().TrimEnd();
        }
    }
}
