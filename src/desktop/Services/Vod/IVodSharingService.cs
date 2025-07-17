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
    public interface IVodSharingService
    {
        /// <summary>
        /// Export a VOD to a specific location
        /// </summary>
        /// <param name="vodId">VOD ID</param>
        /// <param name="destinationPath">Destination file path</param>
        /// <param name="includeMetadata">Whether to include metadata file</param>
        /// <returns>True if export was successful</returns>
        Task<bool> ExportVodAsync(string vodId, string destinationPath, bool includeMetadata = true);

        /// <summary>
        /// Generate a shareable link for a VOD (for P2P sharing)
        /// </summary>
        /// <param name="vodId">VOD ID</param>
        /// <returns>Shareable link or null if failed</returns>
        Task<string?> GenerateShareLinkAsync(string vodId);

        /// <summary>
        /// Import a VOD from an external source
        /// </summary>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="metadata">VOD metadata (optional)</param>
        /// <returns>Import result with VOD ID if successful</returns>
        Task<VodImportResult> ImportVodAsync(string sourcePath, VodMetadata? metadata = null);

        /// <summary>
        /// Get available export formats
        /// </summary>
        /// <returns>List of supported export formats</returns>
        List<VodExportFormat> GetSupportedFormats();
    }

    public class VodImportResult
    {
        public bool Success { get; set; }
        public string? VodId { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class VodExportFormat
    {
        public string Name { get; set; } = "";
        public string Extension { get; set; } = "";
        public string Description { get; set; } = "";
        public bool RequiresConversion { get; set; } = false;
    }
}