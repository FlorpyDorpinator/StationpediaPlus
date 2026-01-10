using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace StationpediaAscended.UI.StationPlanner
{
    /// <summary>
    /// Root container for the Station Planner file system
    /// </summary>
    [Serializable]
    public class PlannerFileSystem
    {
        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";
        
        [JsonProperty("rootFolder")]
        public PlannerFolder RootFolder { get; set; } = new PlannerFolder
        {
            Id = "root",
            Name = "Notes",
            IsExpanded = true
        };
        
        [JsonProperty("lastOpenedFileId")]
        public string LastOpenedFileId { get; set; }
        
        [JsonProperty("windowPosition")]
        public PlannerWindowPosition WindowPosition { get; set; } = new PlannerWindowPosition();
    }
    
    /// <summary>
    /// Represents a folder in the planner file hierarchy
    /// </summary>
    [Serializable]
    public class PlannerFolder
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [JsonProperty("name")]
        public string Name { get; set; } = "New Folder";
        
        [JsonProperty("isExpanded")]
        public bool IsExpanded { get; set; } = false;
        
        [JsonProperty("subFolders")]
        public List<PlannerFolder> SubFolders { get; set; } = new List<PlannerFolder>();
        
        [JsonProperty("files")]
        public List<PlannerFile> Files { get; set; } = new List<PlannerFile>();
        
        [JsonProperty("sortOrder")]
        public int SortOrder { get; set; } = 0;
        
        /// <summary>
        /// If true, this folder persists across all game saves (global).
        /// If false, the folder only exists in the current save file.
        /// Default is false (per-save).
        /// </summary>
        [JsonProperty("persistAcrossSaves")]
        public bool PersistAcrossSaves { get; set; } = false;
        
        /// <summary>
        /// The save name this folder belongs to (only relevant when PersistAcrossSaves is false).
        /// Null or empty means it was created in main menu (should be global).
        /// </summary>
        [JsonProperty("saveName")]
        public string SaveName { get; set; } = "";
        
        /// <summary>
        /// Find a file by ID within this folder and all subfolders
        /// </summary>
        public PlannerFile FindFile(string fileId)
        {
            foreach (var file in Files)
            {
                if (file.Id == fileId)
                    return file;
            }
            
            foreach (var subFolder in SubFolders)
            {
                var found = subFolder.FindFile(fileId);
                if (found != null)
                    return found;
            }
            
            return null;
        }
        
        /// <summary>
        /// Find a folder by ID within this folder and all subfolders
        /// </summary>
        public PlannerFolder FindFolder(string folderId)
        {
            if (Id == folderId)
                return this;
            
            foreach (var subFolder in SubFolders)
            {
                var found = subFolder.FindFolder(folderId);
                if (found != null)
                    return found;
            }
            
            return null;
        }
        
        /// <summary>
        /// Remove a file by ID from this folder or any subfolder
        /// </summary>
        public bool RemoveFile(string fileId)
        {
            var file = Files.Find(f => f.Id == fileId);
            if (file != null)
            {
                Files.Remove(file);
                return true;
            }
            
            foreach (var subFolder in SubFolders)
            {
                if (subFolder.RemoveFile(fileId))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Remove a subfolder by ID
        /// </summary>
        public bool RemoveFolder(string folderId)
        {
            var folder = SubFolders.Find(f => f.Id == folderId);
            if (folder != null)
            {
                SubFolders.Remove(folder);
                return true;
            }
            
            foreach (var subFolder in SubFolders)
            {
                if (subFolder.RemoveFolder(folderId))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get all files in this folder and all subfolders
        /// </summary>
        public List<PlannerFile> GetAllFiles()
        {
            var allFiles = new List<PlannerFile>(Files);
            
            foreach (var subFolder in SubFolders)
            {
                allFiles.AddRange(subFolder.GetAllFiles());
            }
            
            return allFiles;
        }
    }
    
    /// <summary>
    /// Represents a note file in the planner
    /// </summary>
    [Serializable]
    public class PlannerFile
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [JsonProperty("name")]
        public string Name { get; set; } = "New Note";
        
        [JsonProperty("content")]
        public string Content { get; set; } = "";
        
        [JsonProperty("created")]
        public DateTime Created { get; set; } = DateTime.Now;
        
        [JsonProperty("lastModified")]
        public DateTime LastModified { get; set; } = DateTime.Now;
        
        [JsonProperty("sortOrder")]
        public int SortOrder { get; set; } = 0;
        
        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();
        
        [JsonProperty("pinned")]
        public bool Pinned { get; set; } = false;
    }
    
    /// <summary>
    /// Stores window position for persistence
    /// </summary>
    [Serializable]
    public class PlannerWindowPosition
    {
        [JsonProperty("x")]
        public float X { get; set; } = 0;
        
        [JsonProperty("y")]
        public float Y { get; set; } = 0;
        
        [JsonProperty("width")]
        public float Width { get; set; } = 700;
        
        [JsonProperty("height")]
        public float Height { get; set; } = 500;
    }
}
