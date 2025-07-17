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
using SquadOV.Services.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SquadOV.Services.Vod
{
    public class LocalVodSharingService : IVodSharingService
    {
        private readonly IVodStorageService _storageService;
        private readonly IIdentityService _identityService;

        public LocalVodSharingService()
        {
            _storageService = Locator.Current.GetService<IVodStorageService>()!;
            _identityService = Locator.Current.GetService<IIdentityService>()!;
        }

        public async Task<bool> ExportVodAsync(string vodId, string destinationPath, bool includeMetadata = true)
        {
            try
            {
                var vodFile = await _storageService.GetVodAsync(vodId);
                if (vodFile == null || !vodFile.Exists)
                    return false;

                // Copy the video file
                await Task.Run(() => File.Copy(vodFile.Metadata.FilePath, destinationPath, true));

                // Include metadata if requested
                if (includeMetadata)
                {
                    var metadataPath = Path.ChangeExtension(destinationPath, ".json");
                    var json = JsonConvert.SerializeObject(vodFile.Metadata, Formatting.Indented);
                    await File.WriteAllTextAsync(metadataPath, json);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GenerateShareLinkAsync(string vodId)
        {
            try
            {
                var vodFile = await _storageService.GetVodAsync(vodId);
                if (vodFile == null)
                    return null;

                // Create a simple share identifier using the device identity
                var deviceId = _identityService.Device.Id;
                var shareId = $"{deviceId}:{vodId}";
                
                // For now, return a simple squadov:// protocol link
                // In a full implementation, this would involve P2P signaling
                return $"squadov://share/{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(shareId))}";
            }
            catch
            {
                return null;
            }
        }

        public async Task<VodImportResult> ImportVodAsync(string sourcePath, VodMetadata? metadata = null)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    return new VodImportResult
                    {
                        Success = false,
                        ErrorMessage = "Source file not found"
                    };
                }

                // Create metadata if not provided
                if (metadata == null)
                {
                    var fileInfo = new FileInfo(sourcePath);
                    metadata = new VodMetadata
                    {
                        Title = Path.GetFileNameWithoutExtension(sourcePath),
                        Description = "Imported VOD",
                        UserId = _identityService.User?.Username ?? "unknown",
                        GameName = "Unknown",
                        CreatedAt = fileInfo.CreationTime,
                        FileSize = fileInfo.Length
                    };
                }

                // Save the VOD
                var vodId = await _storageService.SaveVodAsync(sourcePath, metadata);

                return new VodImportResult
                {
                    Success = true,
                    VodId = vodId
                };
            }
            catch (Exception ex)
            {
                return new VodImportResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public List<VodExportFormat> GetSupportedFormats()
        {
            return new List<VodExportFormat>
            {
                new VodExportFormat
                {
                    Name = "MP4",
                    Extension = ".mp4",
                    Description = "MPEG-4 Video (Original)",
                    RequiresConversion = false
                },
                new VodExportFormat
                {
                    Name = "AVI",
                    Extension = ".avi",
                    Description = "Audio Video Interleave (Original)",
                    RequiresConversion = false
                },
                new VodExportFormat
                {
                    Name = "MKV",
                    Extension = ".mkv",
                    Description = "Matroska Video (Original)",
                    RequiresConversion = false
                },
                new VodExportFormat
                {
                    Name = "MOV",
                    Extension = ".mov",
                    Description = "QuickTime Movie (Original)",
                    RequiresConversion = false
                }
            };
        }
    }
}