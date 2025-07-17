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
using ReactiveUI;
using System;
using System.IO;

namespace SquadOV.Models.Vod
{
    public class VodFile : ReactiveObject
    {
        private VodMetadata _metadata;
        public VodMetadata Metadata
        {
            get => _metadata;
            set => this.RaiseAndSetIfChanged(ref _metadata, value);
        }

        private bool _exists = false;
        public bool Exists
        {
            get => _exists;
            private set => this.RaiseAndSetIfChanged(ref _exists, value);
        }

        private bool _isAccessible = false;
        public bool IsAccessible
        {
            get => _isAccessible;
            private set => this.RaiseAndSetIfChanged(ref _isAccessible, value);
        }

        public VodFile(VodMetadata metadata)
        {
            _metadata = metadata;
            UpdateFileStatus();
        }

        public void UpdateFileStatus()
        {
            try
            {
                if (!string.IsNullOrEmpty(Metadata.FilePath))
                {
                    Exists = File.Exists(Metadata.FilePath);
                    if (Exists)
                    {
                        // Try to get file info to check accessibility
                        var fileInfo = new FileInfo(Metadata.FilePath);
                        IsAccessible = fileInfo.Exists;
                        
                        // Update file size if it has changed
                        if (Metadata.FileSize != fileInfo.Length)
                        {
                            Metadata.FileSize = fileInfo.Length;
                        }
                    }
                    else
                    {
                        IsAccessible = false;
                    }
                }
                else
                {
                    Exists = false;
                    IsAccessible = false;
                }
            }
            catch
            {
                Exists = false;
                IsAccessible = false;
            }
        }

        public string GetFormattedFileSize()
        {
            const long kb = 1024;
            const long mb = kb * 1024;
            const long gb = mb * 1024;

            if (Metadata.FileSize >= gb)
                return $"{Metadata.FileSize / (double)gb:F2} GB";
            if (Metadata.FileSize >= mb)
                return $"{Metadata.FileSize / (double)mb:F2} MB";
            if (Metadata.FileSize >= kb)
                return $"{Metadata.FileSize / (double)kb:F2} KB";
            
            return $"{Metadata.FileSize} bytes";
        }

        public string GetFormattedDuration()
        {
            if (Metadata.Duration.TotalHours >= 1)
                return Metadata.Duration.ToString(@"h\:mm\:ss");
            else
                return Metadata.Duration.ToString(@"mm\:ss");
        }
    }
}