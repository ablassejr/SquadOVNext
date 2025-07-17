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
using System;
using System.Threading.Tasks;

namespace SquadOV.Services.Engine
{
    internal interface IEngineService
    {
        void TakeScreenshot();
        
        /// <summary>
        /// Start recording a VOD
        /// </summary>
        /// <param name="gameId">Game identifier</param>
        /// <param name="gameName">Human-readable game name</param>
        /// <returns>Recording session ID</returns>
        Task<string> StartRecordingAsync(string gameId, string gameName);
        
        /// <summary>
        /// Stop recording and save the VOD
        /// </summary>
        /// <param name="sessionId">Recording session ID</param>
        /// <param name="title">VOD title</param>
        /// <param name="description">VOD description</param>
        /// <returns>VOD metadata if successful, null if failed</returns>
        Task<VodMetadata?> StopRecordingAsync(string sessionId, string title = "", string description = "");
        
        /// <summary>
        /// Check if currently recording
        /// </summary>
        bool IsRecording { get; }
        
        /// <summary>
        /// Get current recording session ID
        /// </summary>
        string? CurrentRecordingSession { get; }
        
        /// <summary>
        /// Event fired when recording starts
        /// </summary>
        event EventHandler<string>? RecordingStarted;
        
        /// <summary>
        /// Event fired when recording stops
        /// </summary>
        event EventHandler<VodMetadata?>? RecordingStopped;
    }
}
