#!/usr/bin/env python3
"""
Markdown to Stationpedia JSON Converter

Converts markdown guides into JSON format for the StationpediaAscended mod.
- Game Mechanics: Uses tables and minimal nesting (flat structure, H3 sections only)
- Guides: Uses heavy nesting with collapsible sections (full hierarchy)

Usage:
    # Convert a single file:
    python convert_markdown_to_json.py --input file.md --type guide --preview
    python convert_markdown_to_json.py --input file.md --type mechanics --output output.json
    
    # Convert all files in To Be Implemented folders:
    python convert_markdown_to_json.py --convert-all --base-path "." --output converted_entries.json
    
    # Preview output without writing file:
    python convert_markdown_to_json.py --input file.md --preview

Output Format:
    The tool outputs JSON that can be added to descriptions.json:
    - 'guides' array contains nested guide entries
    - 'mechanics' array contains flat game mechanic entries
    
    Add guides to descriptions.json 'guides' array.
    Add mechanics to descriptions.json 'guides' array (they appear under "Game Mechanics" button).
"""

import re
import json
import os
import argparse
from typing import Dict, List, Any, Optional, Tuple
from pathlib import Path


def slugify(text: str) -> str:
    """Convert text to a URL-friendly slug for TOC IDs."""
    slug = text.lower()
    slug = re.sub(r'[^a-z0-9\s-]', '', slug)
    slug = re.sub(r'[\s_]+', '-', slug)
    slug = re.sub(r'-+', '-', slug)
    return slug.strip('-')


def extract_title_and_subtitle(content: str) -> Tuple[str, str]:
    """Extract the main title (H1) and subtitle from markdown."""
    title_match = re.search(r'^#\s+(.+)$', content, re.MULTILINE)
    title = title_match.group(1).strip() if title_match else "Untitled"
    
    # Look for first paragraph after title as subtitle
    subtitle_match = re.search(r'^#\s+.+\n\n(.+?)(?:\n\n|\*\*Data)', content, re.MULTILINE | re.DOTALL)
    subtitle = subtitle_match.group(1).strip() if subtitle_match else ""
    
    return title, subtitle


def parse_table(lines: List[str], start_idx: int) -> Tuple[List[Dict[str, List[str]]], int]:
    """Parse a markdown table starting at the given line index."""
    table_rows = []
    idx = start_idx
    
    while idx < len(lines):
        line = lines[idx].strip()
        if not line.startswith('|') or line.startswith('|---') or line.startswith('| ---'):
            # Skip separator lines
            if '---' in line and line.startswith('|'):
                idx += 1
                continue
            if not line.startswith('|'):
                break
        
        # Parse table row
        cells = [cell.strip() for cell in line.split('|')[1:-1]]
        if cells and not all(c.startswith('---') or c.startswith('-') for c in cells):
            table_rows.append({"cells": cells})
        idx += 1
    
    return table_rows, idx


def parse_list_items(lines: List[str], start_idx: int, prefix: str = '-') -> Tuple[List[str], int]:
    """Parse a markdown list starting at the given line index."""
    items = []
    idx = start_idx
    
    while idx < len(lines):
        line = lines[idx].strip()
        if line.startswith(f'{prefix} ') or line.startswith('* '):
            # Remove the prefix and convert markdown bold to HTML
            item_text = re.sub(r'^[-*]\s+', '', line)
            item_text = re.sub(r'\*\*(.+?)\*\*', r'<b>\1</b>', item_text)
            items.append(item_text)
        elif line.startswith('  ') and items:
            # Continuation of previous item
            items[-1] += ' ' + line.strip()
        elif not line:
            # Empty line might end the list
            if idx + 1 < len(lines) and not lines[idx + 1].strip().startswith(('-', '*', ' ')):
                break
        else:
            break
        idx += 1
    
    return items, idx


def parse_numbered_list(lines: List[str], start_idx: int) -> Tuple[List[str], int]:
    """Parse a numbered/ordered list starting at the given line index."""
    items = []
    idx = start_idx
    
    while idx < len(lines):
        line = lines[idx].strip()
        match = re.match(r'^\d+\.\s+(.+)$', line)
        if match:
            item_text = match.group(1)
            item_text = re.sub(r'\*\*(.+?)\*\*', r'<b>\1</b>', item_text)
            items.append(item_text)
        elif line.startswith('  ') and items:
            # Continuation of previous item
            items[-1] += ' ' + line.strip()
        elif not line:
            if idx + 1 < len(lines) and not re.match(r'^\d+\.', lines[idx + 1].strip()):
                break
        else:
            break
        idx += 1
    
    return items, idx


def convert_markdown_formatting(text: str) -> str:
    """Convert markdown formatting to TextMeshPro/HTML tags."""
    # Bold
    text = re.sub(r'\*\*(.+?)\*\*', r'<b>\1</b>', text)
    # Italic
    text = re.sub(r'\*(.+?)\*', r'<i>\1</i>', text)
    # Code (backticks)
    text = re.sub(r'`(.+?)`', r'<color=#88FF88>\1</color>', text)
    # Links - convert [text](url) to just text for now
    text = re.sub(r'\[(.+?)\]\(.+?\)', r'\1', text)
    return text


def parse_section_content(lines: List[str], start_idx: int, end_idx: int) -> Dict[str, Any]:
    """Parse content within a section (between headers)."""
    content = {
        "description": "",
        "items": [],
        "steps": [],
        "table": []
    }
    
    description_lines = []
    idx = start_idx
    
    while idx < end_idx:
        line = lines[idx]
        stripped = line.strip()
        
        # Skip empty lines at start
        if not stripped and not description_lines:
            idx += 1
            continue
        
        # Table
        if stripped.startswith('|') and not stripped.startswith('|---'):
            table, idx = parse_table(lines, idx)
            if table:
                content["table"] = table
            continue
        
        # Numbered list (steps)
        if re.match(r'^\d+\.\s+', stripped):
            steps, idx = parse_numbered_list(lines, idx)
            content["steps"] = steps
            continue
        
        # Bullet list
        if stripped.startswith('- ') or stripped.startswith('* '):
            items, idx = parse_list_items(lines, idx)
            content["items"].extend(items)
            continue
        
        # Code block - skip
        if stripped.startswith('```'):
            idx += 1
            while idx < end_idx and not lines[idx].strip().startswith('```'):
                idx += 1
            idx += 1
            continue
        
        # Regular text goes to description
        if stripped:
            description_lines.append(convert_markdown_formatting(stripped))
        elif description_lines and description_lines[-1]:
            description_lines.append("")  # Paragraph break
        
        idx += 1
    
    content["description"] = "\n".join(description_lines).strip()
    
    # Clean up empty fields
    if not content["items"]:
        del content["items"]
    if not content["steps"]:
        del content["steps"]
    if not content["table"]:
        del content["table"]
    
    return content


def find_sections(content: str) -> List[Dict[str, Any]]:
    """Find all sections (H2, H3, H4) in the markdown."""
    lines = content.split('\n')
    sections = []
    
    for i, line in enumerate(lines):
        if line.startswith('## '):
            sections.append({
                "level": 2,
                "title": line[3:].strip(),
                "line_idx": i
            })
        elif line.startswith('### '):
            sections.append({
                "level": 3,
                "title": line[4:].strip(),
                "line_idx": i
            })
        elif line.startswith('#### '):
            sections.append({
                "level": 4,
                "title": line[5:].strip(),
                "line_idx": i
            })
    
    return sections


def convert_to_guide_format(content: str, guide_key: str) -> Dict[str, Any]:
    """Convert markdown to heavily nested guide format with collapsible sections."""
    title, subtitle = extract_title_and_subtitle(content)
    lines = content.split('\n')
    sections = find_sections(content)
    
    guide = {
        "guideKey": guide_key,
        "displayName": title,
        "pageDescription": subtitle,
        "generateToc": True,
        "tocTitle": "Contents",
        "buttonColor": "orange",
        "sortOrder": 100,
        "OperationalDetails": []
    }
    
    # Build hierarchical structure
    def build_section(section_info: Dict, section_idx: int, all_sections: List[Dict], lines: List[str]) -> Dict[str, Any]:
        """Build a section with its children."""
        start_line = section_info["line_idx"] + 1
        
        # Find end of this section (next section of same or higher level, or end)
        end_line = len(lines)
        children_sections = []
        
        for i in range(section_idx + 1, len(all_sections)):
            next_section = all_sections[i]
            if next_section["level"] <= section_info["level"]:
                end_line = next_section["line_idx"]
                break
            elif next_section["level"] == section_info["level"] + 1:
                children_sections.append((next_section, i))
        
        # Find where children start
        if children_sections:
            content_end = children_sections[0][0]["line_idx"]
        else:
            content_end = end_line
        
        # Parse content for this section
        section_content = parse_section_content(lines, start_line, content_end)
        
        result = {
            "title": section_info["title"],
            "tocId": slugify(section_info["title"]),
            "collapsible": True
        }
        
        if section_content.get("description"):
            result["description"] = section_content["description"]
        if section_content.get("items"):
            result["items"] = section_content["items"]
        if section_content.get("steps"):
            result["steps"] = section_content["steps"]
        if section_content.get("table"):
            result["table"] = section_content["table"]
        
        # Process children
        if children_sections:
            result["children"] = []
            for child_info, child_idx in children_sections:
                # Check if this child belongs to us (before next sibling)
                child_result = build_section(child_info, child_idx, all_sections, lines)
                result["children"].append(child_result)
        
        return result
    
    # Process top-level sections (H2)
    top_level_sections = [(s, i) for i, s in enumerate(sections) if s["level"] == 2]
    
    for section_info, section_idx in top_level_sections:
        section = build_section(section_info, section_idx, sections, lines)
        guide["OperationalDetails"].append(section)
    
    return guide


def convert_to_mechanics_format(content: str, guide_key: str) -> Dict[str, Any]:
    """Convert markdown to flat game mechanics format with tables and minimal nesting."""
    title, subtitle = extract_title_and_subtitle(content)
    lines = content.split('\n')
    sections = find_sections(content)
    
    # Clean up title - remove "Game Mechanic:" prefix if present
    clean_title = title.replace("Game Mechanic:", "").strip()
    
    mechanic = {
        "guideKey": guide_key,
        "displayName": clean_title,
        "pageDescription": subtitle if subtitle != "---" else "",
        "generateToc": True,
        "tocTitle": "Contents",
        "buttonColor": "blue",  # Different color for mechanics
        "sortOrder": 50,
        "flatStructure": True,  # Indicates flat/table-focused structure
        "OperationalDetails": []
    }
    
    # For mechanics, we process ONLY H3 sections directly (skip H2 headers)
    # This gives us a flat list of topics with their tables
    
    def build_flat_section(section_info: Dict, section_idx: int, all_sections: List[Dict], lines: List[str]) -> Dict[str, Any]:
        """Build a flattened section for mechanics pages."""
        start_line = section_info["line_idx"] + 1
        
        # Find end of this section (next section of same or higher level)
        end_line = len(lines)
        for i in range(section_idx + 1, len(all_sections)):
            next_section = all_sections[i]
            if next_section["level"] <= section_info["level"]:
                end_line = next_section["line_idx"]
                break
        
        # Parse content
        section_content = parse_section_content(lines, start_line, end_line)
        
        result = {
            "title": section_info["title"],
            "tocId": slugify(section_info["title"]),
            "collapsible": True
        }
        
        # Clean up description - remove "---" markers
        desc = section_content.get("description", "")
        desc = desc.replace("\n\n---", "").replace("---\n\n", "").replace("---", "").strip()
        
        if desc:
            result["description"] = desc
        if section_content.get("items"):
            result["items"] = section_content["items"]
        if section_content.get("steps"):
            result["steps"] = section_content["steps"]
        if section_content.get("table"):
            result["table"] = section_content["table"]
        
        return result
    
    # Process only H3 sections for flat mechanics format
    for i, section in enumerate(sections):
        if section["level"] == 3:
            result = build_flat_section(section, i, sections, lines)
            # Skip empty sections
            if result.get("description") or result.get("items") or result.get("steps") or result.get("table"):
                mechanic["OperationalDetails"].append(result)
    
    return mechanic


def generate_guide_key(filename: str, is_mechanic: bool = False) -> str:
    """Generate a guideKey from the filename."""
    base = Path(filename).stem
    # Convert kebab-case to PascalCase
    parts = base.replace('_', '-').split('-')
    pascal = ''.join(word.capitalize() for word in parts)
    
    if is_mechanic:
        return f"Mechanic{pascal}"
    return f"Guide{pascal}"


def process_markdown_file(filepath: str, output_type: str = "auto") -> Dict[str, Any]:
    """Process a single markdown file and return the JSON structure."""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    filename = Path(filepath).stem
    
    # Auto-detect type based on path or content
    if output_type == "auto":
        if "Game Mechanics" in filepath or "game-mechanic" in filepath.lower():
            output_type = "mechanics"
        else:
            output_type = "guide"
    
    is_mechanic = output_type == "mechanics"
    guide_key = generate_guide_key(filename, is_mechanic)
    
    if is_mechanic:
        return convert_to_mechanics_format(content, guide_key)
    else:
        return convert_to_guide_format(content, guide_key)


def convert_all_to_be_implemented(base_path: str) -> Tuple[List[Dict], List[Dict]]:
    """Convert all markdown files in the To Be Implemented folder."""
    guides_path = Path(base_path) / "Guides" / "To Be Implemented" / "Guides"
    mechanics_path = Path(base_path) / "Guides" / "To Be Implemented" / "Game Mechanics"
    
    guides = []
    mechanics = []
    
    # Process guides
    if guides_path.exists():
        for md_file in guides_path.glob("*.md"):
            print(f"Processing guide: {md_file.name}")
            result = process_markdown_file(str(md_file), "guide")
            guides.append(result)
    
    # Process game mechanics
    if mechanics_path.exists():
        for md_file in mechanics_path.glob("*.md"):
            print(f"Processing mechanic: {md_file.name}")
            result = process_markdown_file(str(md_file), "mechanics")
            mechanics.append(result)
    
    return guides, mechanics


def main():
    parser = argparse.ArgumentParser(description="Convert markdown to Stationpedia JSON format")
    parser.add_argument("--type", choices=["mechanics", "guide", "auto"], default="auto",
                       help="Output type: mechanics (flat/tables) or guide (nested)")
    parser.add_argument("--input", "-i", help="Input markdown file")
    parser.add_argument("--output", "-o", help="Output JSON file")
    parser.add_argument("--convert-all", action="store_true",
                       help="Convert all files in To Be Implemented folder")
    parser.add_argument("--base-path", default=".",
                       help="Base path to the mod folder")
    parser.add_argument("--preview", action="store_true",
                       help="Print JSON to stdout instead of writing to file")
    
    args = parser.parse_args()
    
    if args.convert_all:
        guides, mechanics = convert_all_to_be_implemented(args.base_path)
        
        output = {
            "guides": guides,
            "mechanics": mechanics
        }
        
        if args.preview:
            print(json.dumps(output, indent=2))
        else:
            output_file = args.output or "converted_entries.json"
            with open(output_file, 'w', encoding='utf-8') as f:
                json.dump(output, f, indent=2)
            print(f"\nConverted {len(guides)} guides and {len(mechanics)} mechanics")
            print(f"Output written to: {output_file}")
            print("\nTo add these to descriptions.json:")
            print("1. Open the output file")
            print("2. Add 'guides' entries to the 'guides' array in descriptions.json")
            print("3. Add 'mechanics' entries to the 'guides' array (with 'gameMechanic' button)")
    
    elif args.input:
        result = process_markdown_file(args.input, args.type)
        
        if args.preview:
            print(json.dumps(result, indent=2))
        else:
            output_file = args.output or f"{Path(args.input).stem}.json"
            with open(output_file, 'w', encoding='utf-8') as f:
                json.dump(result, f, indent=2)
            print(f"Output written to: {output_file}")
    
    else:
        parser.print_help()


if __name__ == "__main__":
    main()
