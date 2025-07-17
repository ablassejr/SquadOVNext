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
using Splat;
using SquadOV.Models.Vod;
using SquadOV.Services.Identity;
using SquadOV.Services.Vod;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SquadOV.ViewModels.Library
{
    public class VodLibraryViewModel : ReactiveObject, IRoutableViewModel
    {
        private readonly IVodStorageService _storageService;
        private readonly IVodSharingService _sharingService;
        private readonly IIdentityService _identityService;

        public Models.Localization.Localization Loc { get; } = Locator.Current.GetService<Models.Localization.Localization>()!;
        public IScreen HostScreen { get; }

        public string UrlPathSegment { get; } = "/library/vods";

        private ObservableCollection<VodFile> _vodFiles = new ObservableCollection<VodFile>();
        public ObservableCollection<VodFile> VodFiles
        {
            get => _vodFiles;
            set => this.RaiseAndSetIfChanged(ref _vodFiles, value);
        }

        private VodFile? _selectedVod;
        public VodFile? SelectedVod
        {
            get => _selectedVod;
            set => this.RaiseAndSetIfChanged(ref _selectedVod, value);
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set => this.RaiseAndSetIfChanged(ref _searchText, value);
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        private VodStorageStats? _storageStats;
        public VodStorageStats? StorageStats
        {
            get => _storageStats;
            set => this.RaiseAndSetIfChanged(ref _storageStats, value);
        }

        // Commands
        public ReactiveCommand<Unit, Unit> LoadVodsCommand { get; }
        public ReactiveCommand<Unit, Unit> SearchVodsCommand { get; }
        public ReactiveCommand<VodFile, Unit> DeleteVodCommand { get; }
        public ReactiveCommand<VodFile, Unit> ToggleFavoriteCommand { get; }
        public ReactiveCommand<VodFile, Unit> ExportVodCommand { get; }
        public ReactiveCommand<VodFile, Unit> ShareVodCommand { get; }
        public ReactiveCommand<Unit, Unit> ImportVodCommand { get; }
        public ReactiveCommand<Unit, Unit> GenerateSampleDataCommand { get; }

        // Interactions
        public Interaction<VodFile, bool> ConfirmDeleteInteraction { get; }
        public Interaction<Unit, string?> BrowseImportFileInteraction { get; }
        public Interaction<VodFile, string?> BrowseExportLocationInteraction { get; }
        public Interaction<string, Unit> ShowMessageInteraction { get; }

        public VodLibraryViewModel(IScreen screen)
        {
            HostScreen = screen;
            
            _storageService = Locator.Current.GetService<IVodStorageService>()!;
            _sharingService = Locator.Current.GetService<IVodSharingService>()!;
            _identityService = Locator.Current.GetService<IIdentityService>()!;

            // Initialize interactions
            ConfirmDeleteInteraction = new Interaction<VodFile, bool>();
            BrowseImportFileInteraction = new Interaction<Unit, string?>();
            BrowseExportLocationInteraction = new Interaction<VodFile, string?>();
            ShowMessageInteraction = new Interaction<string, Unit>();

            // Commands
            LoadVodsCommand = ReactiveCommand.CreateFromTask(LoadVodsAsync);
            
            SearchVodsCommand = ReactiveCommand.CreateFromTask(SearchVodsAsync);
            
            DeleteVodCommand = ReactiveCommand.CreateFromTask<VodFile>(DeleteVodAsync);
            
            ToggleFavoriteCommand = ReactiveCommand.CreateFromTask<VodFile>(ToggleFavoriteAsync);
            
            ExportVodCommand = ReactiveCommand.CreateFromTask<VodFile>(ExportVodAsync);
            
            ShareVodCommand = ReactiveCommand.CreateFromTask<VodFile>(ShareVodAsync);
            
            ImportVodCommand = ReactiveCommand.CreateFromTask(ImportVodAsync);
            
            GenerateSampleDataCommand = ReactiveCommand.CreateFromTask(GenerateSampleDataAsync);

            // Auto-search when search text changes
            this.WhenAnyValue(x => x.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .InvokeCommand(SearchVodsCommand);

            // Load VODs on initialization
            LoadVodsCommand.Execute().Subscribe();
        }

        private async Task LoadVodsAsync()
        {
            IsLoading = true;
            try
            {
                var userId = _identityService.User?.Username ?? "";
                var vods = await _storageService.GetVodListAsync(userId, 100);
                
                VodFiles.Clear();
                foreach (var vod in vods)
                {
                    VodFiles.Add(vod);
                }

                // Load storage stats
                StorageStats = await _storageService.GetStorageStatsAsync(userId);
            }
            catch (Exception ex)
            {
                await ShowMessageInteraction.Handle($"Failed to load VODs: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchVodsAsync()
        {
            IsLoading = true;
            try
            {
                var userId = _identityService.User?.Username ?? "";
                var vods = string.IsNullOrWhiteSpace(SearchText) 
                    ? await _storageService.GetVodListAsync(userId, 100)
                    : await _storageService.SearchVodsAsync(userId, SearchText, limit: 100);
                
                VodFiles.Clear();
                foreach (var vod in vods)
                {
                    VodFiles.Add(vod);
                }
            }
            catch (Exception ex)
            {
                await ShowMessageInteraction.Handle($"Search failed: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteVodAsync(VodFile vodFile)
        {
            try
            {
                var confirmDelete = await ConfirmDeleteInteraction.Handle(vodFile);
                if (!confirmDelete)
                    return;

                var success = await _storageService.DeleteVodAsync(vodFile.Metadata.Id, true);
                if (success)
                {
                    VodFiles.Remove(vodFile);
                    if (SelectedVod == vodFile)
                        SelectedVod = null;
                    
                    // Refresh storage stats
                    var userId = _identityService.User?.Username ?? "";
                    StorageStats = await _storageService.GetStorageStatsAsync(userId);
                }
                else
                {
                    await ShowMessageInteraction.Handle("Failed to delete VOD");
                }
            }
            catch (Exception ex)
            {
                await ShowMessageInteraction.Handle($"Delete failed: {ex.Message}");
            }
        }

        private async Task ToggleFavoriteAsync(VodFile vodFile)
        {
            try
            {
                vodFile.Metadata.IsFavorite = !vodFile.Metadata.IsFavorite;
                await _storageService.UpdateVodMetadataAsync(vodFile.Metadata);
            }
            catch (Exception ex)
            {
                await ShowMessageInteraction.Handle($"Failed to update favorite: {ex.Message}");
                // Revert the change
                vodFile.Metadata.IsFavorite = !vodFile.Metadata.IsFavorite;
            }
        }

        private async Task ExportVodAsync(VodFile vodFile)
        {
            try
            {
                var exportPath = await BrowseExportLocationInteraction.Handle(vodFile);
                if (string.IsNullOrEmpty(exportPath))
                    return;

                var success = await _sharingService.ExportVodAsync(vodFile.Metadata.Id, exportPath, true);
                if (success)
                {
                    await ShowMessageInteraction.Handle("VOD exported successfully");
                }
                else
                {
                    await ShowMessageInteraction.Handle("Failed to export VOD");
                }
            }
            catch (Exception ex)
            {
                await ShowMessageInteraction.Handle($"Export failed: {ex.Message}");
            }
        }

        private async Task ShareVodAsync(VodFile vodFile)
        {
            try
            {
                var shareLink = await _sharingService.GenerateShareLinkAsync(vodFile.Metadata.Id);
                if (!string.IsNullOrEmpty(shareLink))
                {
                    // Copy to clipboard or show share dialog
                    await ShowMessageInteraction.Handle($"Share link: {shareLink}");
                }
                else
                {
                    await ShowMessageInteraction.Handle("Failed to generate share link");
                }
            }
            catch (Exception ex)
            {
                await ShowMessageInteraction.Handle($"Share failed: {ex.Message}");
            }
        }

        private async Task ImportVodAsync()
        {
            try
            {
                var filePath = await BrowseImportFileInteraction.Handle(Unit.Default);
                if (string.IsNullOrEmpty(filePath))
                    return;

                var result = await _sharingService.ImportVodAsync(filePath);
                if (result.Success)
                {
                    await ShowMessageInteraction.Handle("VOD imported successfully");
                    await LoadVodsAsync(); // Refresh the list
                }
                else
                {
                    await ShowMessageInteraction.Handle($"Import failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                await ShowMessageInteraction.Handle($"Import failed: {ex.Message}");
            }
        }

        private async Task GenerateSampleDataAsync()
        {
            try
            {
                var userId = _identityService.User?.Username ?? "demo-user";
                var demoService = new VodDemoDataService(_storageService);
                
                await ShowMessageInteraction.Handle("Generating sample VOD data...");
                
                var sampleVods = await demoService.GenerateSampleVodsAsync(userId, 10);
                
                // Use a temp directory for demo files
                var tempDir = Path.GetTempPath();
                var demoVodDir = Path.Combine(tempDir, "SquadOVDemo");
                Directory.CreateDirectory(demoVodDir);
                
                foreach (var vod in sampleVods)
                {
                    await demoService.CreateDummyVodFileAsync(vod, demoVodDir);
                    await _storageService.SaveVodAsync(vod.FilePath, vod);
                }
                
                await ShowMessageInteraction.Handle("Sample data generated successfully!");
                await LoadVodsAsync(); // Refresh the list
            }
            catch (Exception ex)
            {
                await ShowMessageInteraction.Handle($"Failed to generate sample data: {ex.Message}");
            }
        }
    }
}
