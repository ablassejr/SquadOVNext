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
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Platform.Storage;
using ReactiveUI;
using SquadOV.Models.Vod;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace SquadOV.Views.Library
{
    public partial class VodLibrary : ReactiveUserControl<ViewModels.Library.VodLibraryViewModel>
    {
        public VodLibrary()
        {
            InitializeComponent();
            
            this.WhenActivated(d =>
            {
                if (ViewModel != null)
                {
                    // Handle file import dialog
                    ViewModel.BrowseImportFileInteraction.RegisterHandler(async interaction =>
                    {
                        var topLevel = TopLevel.GetTopLevel(this);
                        if (topLevel != null)
                        {
                            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                            {
                                Title = "Select VOD file to import",
                                AllowMultiple = false,
                                FileTypeFilter = new[]
                                {
                                    new FilePickerFileType("Video Files")
                                    {
                                        Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv" }
                                    }
                                }
                            });

                            interaction.SetOutput(files.FirstOrDefault()?.Path.LocalPath);
                        }
                        else
                        {
                            interaction.SetOutput(null);
                        }
                    });

                    // Handle export location dialog
                    ViewModel.BrowseExportLocationInteraction.RegisterHandler(async interaction =>
                    {
                        var topLevel = TopLevel.GetTopLevel(this);
                        if (topLevel != null)
                        {
                            var vodFile = interaction.Input;
                            var defaultName = $"{vodFile.Metadata.Title}.mp4";
                            
                            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                            {
                                Title = "Export VOD to...",
                                SuggestedFileName = defaultName,
                                FileTypeChoices = new[]
                                {
                                    new FilePickerFileType("Video Files")
                                    {
                                        Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov" }
                                    }
                                }
                            });

                            interaction.SetOutput(file?.Path.LocalPath);
                        }
                        else
                        {
                            interaction.SetOutput(null);
                        }
                    });

                    // Handle delete confirmation dialog
                    ViewModel.ConfirmDeleteInteraction.RegisterHandler(async interaction =>
                    {
                        var vodFile = interaction.Input;
                        // For now, just return true. In a full implementation, you'd show a proper dialog
                        // TODO: Implement proper confirmation dialog
                        interaction.SetOutput(true);
                    });

                    // Handle message display
                    ViewModel.ShowMessageInteraction.RegisterHandler(async interaction =>
                    {
                        var message = interaction.Input;
                        // For now, just log the message. In a full implementation, you'd show a proper message box
                        // TODO: Implement proper message dialog
                        System.Console.WriteLine($"Message: {message}");
                        interaction.SetOutput(Unit.Default);
                    });
                }
            });
        }
    }
}
