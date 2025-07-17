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
using SquadOV.Services.Vod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SquadOV.Services.Vod
{
    /// <summary>
    /// Service to generate sample VOD data for testing and demonstration
    /// </summary>
    public class VodDemoDataService
    {
        private readonly IVodStorageService _storageService;

        public VodDemoDataService(IVodStorageService storageService)
        {
            _storageService = storageService;
        }

        /// <summary>
        /// Generate sample VOD metadata for testing
        /// </summary>
        /// <param name="userId">User ID to associate with the VODs</param>
        /// <param name="count">Number of sample VODs to create</param>
        /// <returns>List of created VOD metadata</returns>
        public async Task<List<VodMetadata>> GenerateSampleVodsAsync(string userId, int count = 5)
        {
            var sampleVods = new List<VodMetadata>();
            var random = new Random();

            var games = new[]
            {
                ("Valorant", "valorant"),
                ("Counter-Strike 2", "cs2"),
                ("League of Legends", "lol"),
                ("Overwatch 2", "overwatch2"),
                ("Rocket League", "rocketleague")
            };

            var titles = new[]
            {
                "Epic Clutch Win",
                "Amazing Highlight Reel",
                "Ranked Gameplay",
                "Tournament Match",
                "Practice Session",
                "Team Strategy Discussion",
                "Solo Queue Grind",
                "Championship Game",
                "Casual Fun Match",
                "Tutorial Walkthrough"
            };

            for (int i = 0; i < count; i++)
            {
                var game = games[random.Next(games.Length)];
                var title = titles[random.Next(titles.Length)];

                var metadata = new VodMetadata
                {
                    UserId = userId,
                    GameId = game.Item2,
                    GameName = game.Item1,
                    Title = $"{title} #{i + 1}",
                    Description = $"Sample VOD recording from {game.Item1}. This is demonstration data.",
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(30)),
                    Duration = TimeSpan.FromMinutes(random.Next(5, 45)),
                    FileSize = random.Next(100_000_000, 2_000_000_000), // 100MB to 2GB
                    IsFavorite = random.NextDouble() < 0.3, // 30% chance of being favorite
                    Tags = GenerateRandomTags(random),
                    FilePath = "" // Will be set when we create dummy files
                };

                // Add some game-specific properties
                switch (game.Item2)
                {
                    case "valorant":
                        metadata.SetProperty("rank", new[] { "Iron", "Bronze", "Silver", "Gold", "Platinum", "Diamond" }[random.Next(6)]);
                        metadata.SetProperty("map", new[] { "Bind", "Haven", "Split", "Ascent", "Icebox", "Breeze" }[random.Next(6)]);
                        metadata.SetProperty("agent", new[] { "Jett", "Phoenix", "Sage", "Sova", "Cypher", "Reyna" }[random.Next(6)]);
                        break;
                    case "cs2":
                        metadata.SetProperty("rank", new[] { "Silver", "Gold Nova", "Master Guardian", "Legendary Eagle", "Supreme", "Global Elite" }[random.Next(6)]);
                        metadata.SetProperty("map", new[] { "Dust2", "Mirage", "Inferno", "Cache", "Overpass", "Vertigo" }[random.Next(6)]);
                        break;
                    case "lol":
                        metadata.SetProperty("rank", new[] { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Master" }[random.Next(6)]);
                        metadata.SetProperty("champion", new[] { "Jinx", "Yasuo", "Zed", "Ahri", "Thresh", "Lee Sin" }[random.Next(6)]);
                        metadata.SetProperty("role", new[] { "Top", "Jungle", "Mid", "ADC", "Support" }[random.Next(5)]);
                        break;
                }

                sampleVods.Add(metadata);
            }

            return sampleVods;
        }

        /// <summary>
        /// Create dummy VOD files for testing (small placeholder files)
        /// </summary>
        /// <param name="vodMetadata">VOD metadata to create files for</param>
        /// <param name="vodDirectory">Directory to create files in</param>
        /// <returns>Updated metadata with file paths</returns>
        public async Task CreateDummyVodFileAsync(VodMetadata vodMetadata, string vodDirectory)
        {
            Directory.CreateDirectory(vodDirectory);
            
            var fileName = $"{vodMetadata.Id}.mp4";
            var filePath = Path.Combine(vodDirectory, fileName);

            // Create a small dummy file (1KB) to represent the VOD
            var dummyData = new byte[1024];
            var random = new Random();
            random.NextBytes(dummyData);
            
            await File.WriteAllBytesAsync(filePath, dummyData);

            vodMetadata.FilePath = filePath;
            vodMetadata.FileSize = new FileInfo(filePath).Length;
        }

        private List<string> GenerateRandomTags(Random random)
        {
            var allTags = new[]
            {
                "highlight", "clutch", "ace", "winning", "ranked", "casual",
                "team", "solo", "practice", "tournament", "funny", "epic",
                "strategy", "tutorial", "review", "analysis"
            };

            var tagCount = random.Next(0, 4); // 0-3 tags
            var selectedTags = new List<string>();

            for (int i = 0; i < tagCount; i++)
            {
                var tag = allTags[random.Next(allTags.Length)];
                if (!selectedTags.Contains(tag))
                {
                    selectedTags.Add(tag);
                }
            }

            return selectedTags;
        }
    }
}