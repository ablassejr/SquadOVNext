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
using Splat;
using SquadOV.Models.Vod;
using SquadOV.Services.Identity;
using SquadOV.Services.Vod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using System.Reactive.Linq;
using DynamicData.Kernel;

namespace SquadOV.Services.Engine
{
    internal class EngineService: IEngineService
    {
        private LibEngine.EngineOptions _options;
        private LibEngine.Engine _engine;
        private readonly IVodStorageService _vodStorage;
        private readonly IIdentityService _identity;
        
        // Recording state
        private readonly Dictionary<string, RecordingSession> _activeSessions = new();
        private string? _currentRecordingSession;

        public bool IsRecording => !string.IsNullOrEmpty(_currentRecordingSession);
        public string? CurrentRecordingSession => _currentRecordingSession;
        
        public event EventHandler<string>? RecordingStarted;
        public event EventHandler<VodMetadata?>? RecordingStopped;

        public EngineService()
        {
            var config = Locator.Current.GetService<Config.IConfigService>()!;
            _vodStorage = Locator.Current.GetService<IVodStorageService>()!;
            _identity = Locator.Current.GetService<IIdentityService>()!;
            
            _options = new LibEngine.EngineOptions()
            {
                vodPath = config.Config.Core!.VodPath!,
                clipPath = config.Config.Core!.ClipPath!,
                screenshotPath = config.Config.Core!.ScreenshotPath!,
                matchPath = config.Config.Core!.MatchPath!,
                logPath = config.Config.Core!.LogPath!,
            };
            _engine = new LibEngine.Engine(_options);

            // Connect certain reactive properties to the engine.
            // Game support - we need to add/remove game support from the engine as those config options changes.
            // Furthermore, if individual properties change, then the engine needs to be updated about that info.
            config.Config.Games!.Support
                .ToObservableChangeSet()
                .AsObservableList()
                .Connect()
                .AutoRefresh(x => x.Enabled)
                .Subscribe(OnGameSupportChange);
        }

        private void OnGameSupportChange(IChangeSet<Models.Settings.Config.GameSupportConfig> x)
        {
            var changes = x.AsList();
            foreach (var c in changes)
            {
                switch (c.Reason)
                {
                    case ListChangeReason.Add:
                        _engine.addProcessToWatch(c.Item.Current.Executable);
                        break;
                    case ListChangeReason.AddRange:
                        foreach (var cc in c.Range)
                        {
                            _engine.addProcessToWatch(cc.Executable);
                        }
                        break;
                    case ListChangeReason.Refresh:
                        if (c.Item.Previous.HasValue)
                        {
                            if (c.Item.Current.Enabled != c.Item.Previous.Value.Enabled)
                            {
                                if (c.Item.Current.Enabled)
                                {
                                    _engine.addProcessToWatch(c.Item.Current.Executable);
                                }
                                else
                                {
                                    _engine.removeProcessToWatch(c.Item.Current.Executable);
                                }
                            }
                        }
                        break;
                    case ListChangeReason.Remove:
                        _engine.removeProcessToWatch(c.Item.Current.Executable);
                        break;
                    default: throw new ApplicationException("Unsupported change operation for syncing game support to the engine.");
                }
            }
        }

        public void TakeScreenshot()
        {
            // Original screenshot functionality
        }

        public async Task<string> StartRecordingAsync(string gameId, string gameName)
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("A recording session is already active");
            }

            var sessionId = Guid.NewGuid().ToString();
            var session = new RecordingSession
            {
                Id = sessionId,
                GameId = gameId,
                GameName = gameName,
                StartTime = DateTime.UtcNow,
                UserId = _identity.User?.Username ?? "unknown"
            };

            _activeSessions[sessionId] = session;
            _currentRecordingSession = sessionId;

            // TODO: Start actual recording with the native engine
            // For now, just simulate recording start
            
            RecordingStarted?.Invoke(this, sessionId);
            return sessionId;
        }

        public async Task<VodMetadata?> StopRecordingAsync(string sessionId, string title = "", string description = "")
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return null;
            }

            if (_currentRecordingSession != sessionId)
            {
                return null;
            }

            try
            {
                var endTime = DateTime.UtcNow;
                var duration = endTime - session.StartTime;

                // TODO: Stop actual recording with the native engine and get the recorded file path
                // For now, create a placeholder file
                var recordedFilePath = await CreatePlaceholderRecordingAsync(session, duration);

                // Create VOD metadata
                var metadata = new VodMetadata
                {
                    UserId = session.UserId,
                    GameId = session.GameId,
                    GameName = session.GameName,
                    Title = string.IsNullOrEmpty(title) ? $"{session.GameName} Recording" : title,
                    Description = string.IsNullOrEmpty(description) ? $"Recorded on {session.StartTime:yyyy-MM-dd}" : description,
                    CreatedAt = session.StartTime,
                    Duration = duration,
                    Tags = new List<string> { "recorded", "gameplay" }
                };

                // Save to VOD storage
                var vodId = await _vodStorage.SaveVodAsync(recordedFilePath, metadata);
                metadata.Id = vodId;

                // Cleanup
                _activeSessions.Remove(sessionId);
                _currentRecordingSession = null;

                RecordingStopped?.Invoke(this, metadata);
                return metadata;
            }
            catch (Exception)
            {
                // Cleanup on error
                _activeSessions.Remove(sessionId);
                _currentRecordingSession = null;
                
                RecordingStopped?.Invoke(this, null);
                return null;
            }
        }

        private async Task<string> CreatePlaceholderRecordingAsync(RecordingSession session, TimeSpan duration)
        {
            // Create a placeholder recording file for demonstration
            // In a real implementation, this would be handled by the native recording engine
            var tempDir = Path.GetTempPath();
            var fileName = $"recording_{session.Id}.mp4";
            var filePath = Path.Combine(tempDir, fileName);

            // Create a dummy file with size proportional to duration
            var fileSize = Math.Max(1024, (int)(duration.TotalMinutes * 1024 * 100)); // ~100KB per minute
            var dummyData = new byte[fileSize];
            var random = new Random();
            random.NextBytes(dummyData);

            await File.WriteAllBytesAsync(filePath, dummyData);
            return filePath;
        }

        private class RecordingSession
        {
            public string Id { get; set; } = "";
            public string GameId { get; set; } = "";
            public string GameName { get; set; } = "";
            public string UserId { get; set; } = "";
            public DateTime StartTime { get; set; }
        }
    }
}
