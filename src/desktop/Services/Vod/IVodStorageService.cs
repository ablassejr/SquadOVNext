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
using SquadOV.Models.Vod;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SquadOV.Services.Vod
{
    public interface IVodStorageService
    {
        /// <summary>
        /// Save a VOD file and its metadata to storage
        /// </summary>
        /// <param name="sourceFilePath">Path to the source video file</param>
        /// <param name="metadata">VOD metadata</param>
        /// <returns>The ID of the saved VOD</returns>
        Task<string> SaveVodAsync(string sourceFilePath, VodMetadata metadata);

        /// <summary>
        /// Get a VOD by its ID
        /// </summary>
        /// <param name="vodId">VOD ID</param>
        /// <returns>VOD file with metadata, or null if not found</returns>
        Task<VodFile?> GetVodAsync(string vodId);

        /// <summary>
        /// Get a list of VODs for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="limit">Maximum number of VODs to return</param>
        /// <param name="offset">Number of VODs to skip</param>
        /// <returns>List of VOD files</returns>
        Task<List<VodFile>> GetVodListAsync(string userId, int limit = 100, int offset = 0);

        /// <summary>
        /// Search VODs by criteria
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="searchText">Search text for title/description</param>
        /// <param name="gameId">Game ID filter</param>
        /// <param name="tags">Tag filters</param>
        /// <param name="limit">Maximum number of results</param>
        /// <returns>List of matching VOD files</returns>
        Task<List<VodFile>> SearchVodsAsync(string userId, string? searchText = null, string? gameId = null, List<string>? tags = null, int limit = 100);

        /// <summary>
        /// Update VOD metadata
        /// </summary>
        /// <param name="metadata">Updated metadata</param>
        /// <returns>True if update was successful</returns>
        Task<bool> UpdateVodMetadataAsync(VodMetadata metadata);

        /// <summary>
        /// Delete a VOD
        /// </summary>
        /// <param name="vodId">VOD ID</param>
        /// <param name="deleteFile">Whether to also delete the physical file</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteVodAsync(string vodId, bool deleteFile = false);

        /// <summary>
        /// Get VODs marked as favorites
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="limit">Maximum number of results</param>
        /// <returns>List of favorite VOD files</returns>
        Task<List<VodFile>> GetFavoriteVodsAsync(string userId, int limit = 100);

        /// <summary>
        /// Get total storage statistics for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Storage statistics</returns>
        Task<VodStorageStats> GetStorageStatsAsync(string userId);
    }

    public class VodStorageStats
    {
        public int TotalVods { get; set; }
        public long TotalSize { get; set; }
        public long AvailableSpace { get; set; }
        public int FavoriteVods { get; set; }
    }
}