using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Assets.Scripts;
using Assets.Scripts.UI;
using UnityEngine;

namespace StationpediaAscended.Data
{
    /// <summary>
    /// Loads and converts the Stationeers Survival Manual markdown file
    /// into Stationpedia-compatible format with collapsible sections.
    /// </summary>
    public static class SurvivalManualLoader
    {
        // Cached converted content
        private static DeviceDescriptions _survivalManualDescriptions = null;
        private static bool _isRegistered = false;
        
        /// <summary>
        /// Structure to hold parsed Part information
        /// </summary>
        private class ParsedPart
        {
            public string Title { get; set; }
            public string TocId { get; set; }
            public List<ParsedSection> Sections { get; set; } = new List<ParsedSection>();
        }
        
        private class ParsedSection
        {
            public string Title { get; set; }
            public string TocId { get; set; }
            public List<string> Content { get; set; } = new List<string>();
            public List<ParsedSection> SubSections { get; set; } = new List<ParsedSection>();
        }
        
        /// <summary>
        /// Register the Survival Manual as a Stationpedia page
        /// </summary>
        public static void RegisterSurvivalManualPage()
        {
            if (_isRegistered) return;
            
            try
            {
                // Create a StationpediaPage for the Survival Manual
                var page = new StationpediaPage
                {
                    Key = "SurvivalManual",
                    Title = "Stationeers Survival Manual"
                };
                
                // Set the page text to a brief intro - operational details will have the full content
                page.Text = "Welcome to the Stationeers Survival Manual - your comprehensive guide to surviving your first hours on an alien world.\n\n" +
                           "This manual covers everything from your initial spawn to building a sustainable base.\n\n" +
                           "<i>Expand the sections below to learn how to survive.</i>";
                
                // Register the page
                Stationpedia.Register(page, false);
                
                _isRegistered = true;
                ConsoleWindow.Print("[Stationpedia Ascended] Survival Manual page registered");
            }
            catch (Exception ex)
            {
                StationpediaAscendedMod.Log?.LogError($"Error registering Survival Manual: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get the DeviceDescriptions for the Survival Manual (for operational details rendering)
        /// </summary>
        public static DeviceDescriptions GetSurvivalManualDescriptions()
        {
            if (_survivalManualDescriptions == null)
            {
                _survivalManualDescriptions = LoadAndConvertManual();
            }
            return _survivalManualDescriptions;
        }
        
        /// <summary>
        /// Load the markdown file and convert to DeviceDescriptions format
        /// </summary>
        private static DeviceDescriptions LoadAndConvertManual()
        {
            string markdown = LoadMarkdownFile();
            if (string.IsNullOrEmpty(markdown))
            {
                return CreateFallbackManual();
            }
            
            return ConvertMarkdownToDescriptions(markdown);
        }
        
        /// <summary>
        /// Load the markdown file from disk
        /// </summary>
        private static string LoadMarkdownFile()
        {
            var possiblePaths = new List<string>();
            
#if DEBUG
            possiblePaths.Add(@"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaAscended\mod\Guides\Stationeers Survival Manual.md");
#endif
            possiblePaths.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Guides", "Stationeers Survival Manual.md"));
            possiblePaths.Add(Path.Combine(BepInEx.Paths.BepInExRootPath, "scripts", "Guides", "Stationeers Survival Manual.md"));
            
            foreach (var path in possiblePaths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    try
                    {
                        return File.ReadAllText(path);
                    }
                    catch (Exception ex)
                    {
                        StationpediaAscendedMod.Log?.LogWarning($"Failed to read Survival Manual from {path}: {ex.Message}");
                    }
                }
            }
            
            StationpediaAscendedMod.Log?.LogWarning("Survival Manual markdown file not found");
            return null;
        }
        
        /// <summary>
        /// Convert markdown content to DeviceDescriptions format
        /// </summary>
        private static DeviceDescriptions ConvertMarkdownToDescriptions(string markdown)
        {
            var descriptions = new DeviceDescriptions
            {
                deviceKey = "SurvivalManual",
                generateToc = false, // We generate TOC per-part instead
                operationalDetails = new List<OperationalDetail>()
            };
            
            // Parse the markdown into Parts
            var parts = ParseMarkdownIntoParts(markdown);
            
            // Convert each Part to an OperationalDetail
            foreach (var part in parts)
            {
                var partDetail = ConvertPartToOperationalDetail(part);
                descriptions.operationalDetails.Add(partDetail);
            }
            
            return descriptions;
        }
        
        /// <summary>
        /// Parse markdown into Part structures
        /// </summary>
        private static List<ParsedPart> ParseMarkdownIntoParts(string markdown)
        {
            var parts = new List<ParsedPart>();
            var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            ParsedPart currentPart = null;
            ParsedSection currentSection = null;
            ParsedSection currentSubSection = null;
            StringBuilder contentBuilder = new StringBuilder();
            bool inCodeBlock = false;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                
                // Track code blocks
                if (line.TrimStart().StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;
                    if (inCodeBlock)
                    {
                        contentBuilder.AppendLine("<mspace=0.5em>");
                    }
                    else
                    {
                        contentBuilder.AppendLine("</mspace>");
                    }
                    continue;
                }
                
                if (inCodeBlock)
                {
                    contentBuilder.AppendLine("  " + line);
                    continue;
                }
                
                // Check for Part header (# Part N:)
                var partMatch = Regex.Match(line, @"^#\s+Part\s+(\d+):\s*(.+)$");
                if (partMatch.Success)
                {
                    // Save previous content
                    SaveCurrentContent(currentPart, currentSection, currentSubSection, contentBuilder);
                    
                    // Start new Part
                    currentPart = new ParsedPart
                    {
                        Title = $"Part {partMatch.Groups[1].Value}: {partMatch.Groups[2].Value.Trim()}",
                        TocId = $"part{partMatch.Groups[1].Value}"
                    };
                    parts.Add(currentPart);
                    currentSection = null;
                    currentSubSection = null;
                    contentBuilder.Clear();
                    continue;
                }
                
                // Check for Section header (## )
                var sectionMatch = Regex.Match(line, @"^##\s+(.+)$");
                if (sectionMatch.Success && currentPart != null)
                {
                    // Save previous content
                    SaveCurrentContent(currentPart, currentSection, currentSubSection, contentBuilder);
                    
                    string sectionTitle = sectionMatch.Groups[1].Value.Trim();
                    currentSection = new ParsedSection
                    {
                        Title = sectionTitle,
                        TocId = GenerateTocId(sectionTitle)
                    };
                    currentPart.Sections.Add(currentSection);
                    currentSubSection = null;
                    contentBuilder.Clear();
                    continue;
                }
                
                // Check for SubSection header (### )
                var subSectionMatch = Regex.Match(line, @"^###\s+(.+)$");
                if (subSectionMatch.Success && currentSection != null)
                {
                    // Save previous content
                    SaveCurrentContent(currentPart, currentSection, currentSubSection, contentBuilder);
                    
                    string subTitle = subSectionMatch.Groups[1].Value.Trim();
                    currentSubSection = new ParsedSection
                    {
                        Title = subTitle,
                        TocId = GenerateTocId(subTitle)
                    };
                    currentSection.SubSections.Add(currentSubSection);
                    contentBuilder.Clear();
                    continue;
                }
                
                // Skip horizontal rules
                if (line.Trim().StartsWith("---"))
                {
                    continue;
                }
                
                // Skip standalone header markers
                if (line.Trim() == "```markdown" || line.Trim() == "````markdown")
                {
                    continue;
                }
                
                // Convert markdown formatting
                string convertedLine = ConvertMarkdownLine(line);
                
                // Add to content
                if (!string.IsNullOrWhiteSpace(convertedLine) || contentBuilder.Length > 0)
                {
                    contentBuilder.AppendLine(convertedLine);
                }
            }
            
            // Save final content
            SaveCurrentContent(currentPart, currentSection, currentSubSection, contentBuilder);
            
            return parts;
        }
        
        /// <summary>
        /// Save accumulated content to the appropriate section
        /// </summary>
        private static void SaveCurrentContent(ParsedPart part, ParsedSection section, ParsedSection subSection, StringBuilder content)
        {
            if (content.Length == 0) return;
            
            string contentStr = content.ToString().Trim();
            if (string.IsNullOrEmpty(contentStr)) return;
            
            if (subSection != null)
            {
                subSection.Content.Add(contentStr);
            }
            else if (section != null)
            {
                section.Content.Add(contentStr);
            }
            // Part-level content goes into first section or is ignored
        }
        
        /// <summary>
        /// Convert a single markdown line to Stationpedia format
        /// </summary>
        private static string ConvertMarkdownLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return line;
            
            // Bold: **text** -> <b>text</b>
            line = Regex.Replace(line, @"\*\*([^*]+)\*\*", "<b>$1</b>");
            
            // Italic: *text* -> <i>text</i>
            line = Regex.Replace(line, @"(?<!\*)\*([^*]+)\*(?!\*)", "<i>$1</i>");
            
            // Inline code: `text` -> <mspace=0.5em>text</mspace>
            line = Regex.Replace(line, @"`([^`]+)`", "<color=#88CCFF>$1</color>");
            
            // Convert item references like {THING:ItemName}
            // These are already in correct format, leave them
            
            // Convert bullet points: - text -> • text
            if (line.TrimStart().StartsWith("- "))
            {
                int indent = line.Length - line.TrimStart().Length;
                string indentStr = new string(' ', indent);
                line = indentStr + "• " + line.TrimStart().Substring(2);
            }
            
            // Convert numbered lists: 1. text -> keep as-is but with color
            var numberedMatch = Regex.Match(line, @"^(\s*)(\d+)\.\s+(.+)$");
            if (numberedMatch.Success)
            {
                string indent = numberedMatch.Groups[1].Value;
                string number = numberedMatch.Groups[2].Value;
                string text = numberedMatch.Groups[3].Value;
                line = $"{indent}<color=#FFA500>{number}.</color> {text}";
            }
            
            // Convert links to Things: {THING:PrefabName} format is native, leave it
            // But convert markdown links if any
            line = Regex.Replace(line, @"\[([^\]]+)\]\([^)]+\)", "$1");
            
            return line;
        }
        
        /// <summary>
        /// Generate a URL-safe TOC ID from a title
        /// </summary>
        private static string GenerateTocId(string title)
        {
            string id = title.ToLowerInvariant();
            id = Regex.Replace(id, @"[^a-z0-9]+", "_");
            id = id.Trim('_');
            return id;
        }
        
        /// <summary>
        /// Convert a ParsedPart to an OperationalDetail with TOC
        /// </summary>
        private static OperationalDetail ConvertPartToOperationalDetail(ParsedPart part)
        {
            var partDetail = new OperationalDetail
            {
                title = part.Title,
                tocId = part.TocId,
                collapsible = true,
                children = new List<OperationalDetail>()
            };
            
            // Create TOC for this Part
            var tocDetail = CreateTableOfContents(part);
            if (tocDetail != null)
            {
                partDetail.children.Add(tocDetail);
            }
            
            // Convert sections
            foreach (var section in part.Sections)
            {
                var sectionDetail = ConvertSectionToOperationalDetail(section);
                partDetail.children.Add(sectionDetail);
            }
            
            return partDetail;
        }
        
        /// <summary>
        /// Create a Table of Contents for a Part
        /// </summary>
        private static OperationalDetail CreateTableOfContents(ParsedPart part)
        {
            if (part.Sections.Count == 0) return null;
            
            var tocItems = new List<string>();
            foreach (var section in part.Sections)
            {
                tocItems.Add($"<link=toc_{section.TocId}><color=#FFFFFF>{section.Title}</color></link>");
                
                foreach (var subSection in section.SubSections)
                {
                    tocItems.Add($"  <color=#888888>-</color> <link=toc_{subSection.TocId}><color=#CCCCCC>{subSection.Title}</color></link>");
                }
            }
            
            return new OperationalDetail
            {
                title = "Contents",
                tocId = $"{part.TocId}_toc",
                collapsible = false,
                description = string.Join("\n", tocItems)
            };
        }
        
        /// <summary>
        /// Convert a ParsedSection to an OperationalDetail
        /// </summary>
        private static OperationalDetail ConvertSectionToOperationalDetail(ParsedSection section)
        {
            var sectionDetail = new OperationalDetail
            {
                title = section.Title,
                tocId = section.TocId,
                collapsible = true,
                children = new List<OperationalDetail>()
            };
            
            // Add section content
            if (section.Content.Count > 0)
            {
                sectionDetail.description = string.Join("\n\n", section.Content);
            }
            
            // Add subsections
            foreach (var subSection in section.SubSections)
            {
                var subDetail = new OperationalDetail
                {
                    title = subSection.Title,
                    tocId = subSection.TocId,
                    collapsible = true
                };
                
                if (subSection.Content.Count > 0)
                {
                    subDetail.description = string.Join("\n\n", subSection.Content);
                }
                
                sectionDetail.children.Add(subDetail);
            }
            
            return sectionDetail;
        }
        
        /// <summary>
        /// Create a fallback manual if the file isn't found
        /// </summary>
        private static DeviceDescriptions CreateFallbackManual()
        {
            return new DeviceDescriptions
            {
                deviceKey = "SurvivalManual",
                generateToc = true,
                operationalDetails = new List<OperationalDetail>
                {
                    new OperationalDetail
                    {
                        title = "Part 1: Getting Started",
                        tocId = "part1",
                        collapsible = true,
                        description = "The Stationeers Survival Manual file was not found.\n\nPlease ensure 'Stationeers Survival Manual.md' is in the Guides folder."
                    }
                }
            };
        }
        
        /// <summary>
        /// Clear cached data for hot-reload
        /// </summary>
        public static void Clear()
        {
            _survivalManualDescriptions = null;
            _isRegistered = false;
        }
    }
}
