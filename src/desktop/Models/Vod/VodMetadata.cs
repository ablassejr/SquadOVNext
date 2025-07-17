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
using System.Collections.Generic;

namespace SquadOV.Models.Vod
{
    public class VodMetadata : ReactiveObject
    {
        private string _id = "";
        public string Id
        {
            get => _id;
            set => this.RaiseAndSetIfChanged(ref _id, value);
        }

        private string _userId = "";
        public string UserId
        {
            get => _userId;
            set => this.RaiseAndSetIfChanged(ref _userId, value);
        }

        private string _gameId = "";
        public string GameId
        {
            get => _gameId;
            set => this.RaiseAndSetIfChanged(ref _gameId, value);
        }

        private string _gameName = "";
        public string GameName
        {
            get => _gameName;
            set => this.RaiseAndSetIfChanged(ref _gameName, value);
        }

        private string _title = "";
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        private string _filePath = "";
        public string FilePath
        {
            get => _filePath;
            set => this.RaiseAndSetIfChanged(ref _filePath, value);
        }

        private string _thumbnailPath = "";
        public string ThumbnailPath
        {
            get => _thumbnailPath;
            set => this.RaiseAndSetIfChanged(ref _thumbnailPath, value);
        }

        private DateTime _createdAt = DateTime.UtcNow;
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => this.RaiseAndSetIfChanged(ref _createdAt, value);
        }

        private TimeSpan _duration = TimeSpan.Zero;
        public TimeSpan Duration
        {
            get => _duration;
            set => this.RaiseAndSetIfChanged(ref _duration, value);
        }

        private long _fileSize = 0;
        public long FileSize
        {
            get => _fileSize;
            set => this.RaiseAndSetIfChanged(ref _fileSize, value);
        }

        private Dictionary<string, string> _properties = new Dictionary<string, string>();
        public Dictionary<string, string> Properties
        {
            get => _properties;
            set => this.RaiseAndSetIfChanged(ref _properties, value);
        }

        private bool _isFavorite = false;
        public bool IsFavorite
        {
            get => _isFavorite;
            set => this.RaiseAndSetIfChanged(ref _isFavorite, value);
        }

        private List<string> _tags = new List<string>();
        public List<string> Tags
        {
            get => _tags;
            set => this.RaiseAndSetIfChanged(ref _tags, value);
        }

        public VodMetadata()
        {
            Id = Guid.NewGuid().ToString();
        }

        public string GetProperty(string key, string defaultValue = "")
        {
            return Properties.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public void SetProperty(string key, string value)
        {
            Properties[key] = value;
        }
    }
}