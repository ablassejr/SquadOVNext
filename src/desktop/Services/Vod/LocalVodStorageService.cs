//
//  Copyright (C) 2022 Michael Bao
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
//
using Newtonsoft.Json;
using Splat;
using SquadOV.Models.Vod;
using SquadOV.Services.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SquadOV.Services.Vod
{
    public class LocalVodStorageService : IVodStorageService
    {
        private readonly IConfigService _config;
        private readonly object _lock = new object();

        public LocalVodStorageService()
        {
            _config = Locator.Current.GetService<IConfigService>()!;
            EnsureDirectoriesExist();
        }

        private string VodPath => _config.Config.Core?.VodPath ?? Path.Combine(_config.UserFolder, "Storage", "VOD");
        private string MetadataPath => Path.Combine(VodPath, "metadata");
        private string ThumbnailPath => Path.Combine(VodPath, "thumbnails");

        private void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(VodPath);
            Directory.CreateDirectory(MetadataPath);
            Directory.CreateDirectory(ThumbnailPath);
        }

        private string GetMetadataFilePath(string vodId)
        {
            return Path.Combine(MetadataPath, $"{vodId}.json");
        }

        private string GetVodFilePath(string vodId, string originalExtension)
        {
            return Path.Combine(VodPath, $"{vodId}{originalExtension}");
        }

        public async Task<string> SaveVodAsync(string sourceFilePath, VodMetadata metadata)
        {
            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException($"Source file not found: {sourceFilePath}");

            var extension = Path.GetExtension(sourceFilePath);
            var vodId = metadata.Id;
            var destinationPath = GetVodFilePath(vodId, extension);

            // Copy the file to the VOD directory
            await Task.Run(() => File.Copy(sourceFilePath, destinationPath, true));

            // Update metadata with the new file path
            metadata.FilePath = destinationPath;
            metadata.FileSize = new FileInfo(destinationPath).Length;

            // Save metadata
            await SaveMetadataAsync(metadata);

            return vodId;
        }

        public async Task<VodFile?> GetVodAsync(string vodId)
        {
            var metadataPath = GetMetadataFilePath(vodId);
            if (!File.Exists(metadataPath))
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(metadataPath);
                var metadata = JsonConvert.DeserializeObject<VodMetadata>(json);
                if (metadata == null)
                    return null;

                var vodFile = new VodFile(metadata);
                vodFile.UpdateFileStatus();
                return vodFile;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<VodFile>> GetVodListAsync(string userId, int limit = 100, int offset = 0)
        {
            var metadataFiles = Directory.GetFiles(MetadataPath, "*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Skip(offset)
                .Take(limit);

            var vodFiles = new List<VodFile>();

            foreach (var file in metadataFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var metadata = JsonConvert.DeserializeObject<VodMetadata>(json);
                    
                    if (metadata != null && (string.IsNullOrEmpty(userId) || metadata.UserId == userId))
                    {
                        var vodFile = new VodFile(metadata);
                        vodFile.UpdateFileStatus();
                        vodFiles.Add(vodFile);
                    }
                }
                catch
                {
                    // Skip corrupted metadata files
                    continue;
                }
            }

            return vodFiles;
        }

        public async Task<List<VodFile>> SearchVodsAsync(string userId, string? searchText = null, string? gameId = null, List<string>? tags = null, int limit = 100)
        {
            var allVods = await GetVodListAsync(userId, int.MaxValue);

            var filtered = allVods.Where(vod =>
            {
                // Filter by search text
                if (!string.IsNullOrEmpty(searchText))
                {
                    var text = searchText.ToLowerInvariant();
                    if (!vod.Metadata.Title.ToLowerInvariant().Contains(text) &&
                        !vod.Metadata.Description.ToLowerInvariant().Contains(text) &&
                        !vod.Metadata.GameName.ToLowerInvariant().Contains(text))
                        return false;
                }

                // Filter by game ID
                if (!string.IsNullOrEmpty(gameId) && vod.Metadata.GameId != gameId)
                    return false;

                // Filter by tags
                if (tags != null && tags.Any())
                {
                    if (!tags.Any(tag => vod.Metadata.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
                        return false;
                }

                return true;
            });

            return filtered.Take(limit).ToList();
        }

        public async Task<bool> UpdateVodMetadataAsync(VodMetadata metadata)
        {
            try
            {
                await SaveMetadataAsync(metadata);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteVodAsync(string vodId, bool deleteFile = false)
        {
            try
            {
                var metadataPath = GetMetadataFilePath(vodId);
                
                if (deleteFile)
                {
                    var vodFile = await GetVodAsync(vodId);
                    if (vodFile != null && File.Exists(vodFile.Metadata.FilePath))
                    {
                        File.Delete(vodFile.Metadata.FilePath);
                    }

                    // Also delete thumbnail if it exists
                    if (vodFile != null && !string.IsNullOrEmpty(vodFile.Metadata.ThumbnailPath) && File.Exists(vodFile.Metadata.ThumbnailPath))
                    {
                        File.Delete(vodFile.Metadata.ThumbnailPath);
                    }
                }

                if (File.Exists(metadataPath))
                {
                    File.Delete(metadataPath);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<VodFile>> GetFavoriteVodsAsync(string userId, int limit = 100)
        {
            var allVods = await GetVodListAsync(userId, int.MaxValue);
            return allVods.Where(vod => vod.Metadata.IsFavorite).Take(limit).ToList();
        }

        public async Task<VodStorageStats> GetStorageStatsAsync(string userId)
        {
            var allVods = await GetVodListAsync(userId, int.MaxValue);
            
            var stats = new VodStorageStats
            {
                TotalVods = allVods.Count,
                TotalSize = allVods.Sum(vod => vod.Metadata.FileSize),
                FavoriteVods = allVods.Count(vod => vod.Metadata.IsFavorite)
            };

            // Calculate available space
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(VodPath) ?? "C:");
                stats.AvailableSpace = driveInfo.AvailableFreeSpace;
            }
            catch
            {
                stats.AvailableSpace = 0;
            }

            return stats;
        }

        private async Task SaveMetadataAsync(VodMetadata metadata)
        {
            lock (_lock)
            {
                var metadataPath = GetMetadataFilePath(metadata.Id);
                var json = JsonConvert.SerializeObject(metadata, Formatting.Indented);
                File.WriteAllText(metadataPath, json);
            }
        }
    }
}