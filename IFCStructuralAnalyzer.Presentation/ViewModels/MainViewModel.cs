using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelixToolkit.Wpf;
using IFCStructuralAnalyzer.Application.DTOs;
using IFCStructuralAnalyzer.Application.Services.Interfaces;
using IFCStructuralAnalyzer.Presentation.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace IFCStructuralAnalyzer.Presentation.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        #region Services

        private readonly IIFCParserService _ifcParserService;
        private readonly IStructuralElementService _elementService;
        private readonly Rendering3DService _rendering3DService;

        #endregion

        #region File Properties

        [ObservableProperty]
        private string _fileName = "No file selected";

        [ObservableProperty]
        private string _projectName = "";

        #endregion

        #region UI State

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        #endregion

        #region Element Collections

        [ObservableProperty]
        private ObservableCollection<StructuralElementDto> _allElements = new();

        [ObservableProperty]
        private ObservableCollection<StructuralElementDto> _filteredElements = new();

        [ObservableProperty]
        private StructuralElementDto? _selectedElement;

        #endregion

        #region Filter Properties

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private int? _selectedFloor = null;

        [ObservableProperty]
        private ObservableCollection<int> _availableFloors = new();

        [ObservableProperty]
        private string _selectedElementTypeFilter = "All";

        #endregion

        #region Statistics

        [ObservableProperty]
        private int _totalElements = 0;

        [ObservableProperty]
        private int _columnCount = 0;

        [ObservableProperty]
        private int _beamCount = 0;

        [ObservableProperty]
        private int _slabCount = 0;

        [ObservableProperty]
        private double _totalVolume = 0;

        #endregion

        #region 3D Properties

        [ObservableProperty]
        private Model3DGroup _sceneModel = new();

        public HelixViewport3D? Viewport3D { get; set; }

        #endregion

        #region Constructor

        public MainViewModel(
            IIFCParserService ifcParserService,
            IStructuralElementService elementService,
            Rendering3DService rendering3DService)
        {
            _ifcParserService = ifcParserService;
            _elementService = elementService;
            _rendering3DService = rendering3DService;
        }

        #endregion

        #region Commands - File Operations

        [RelayCommand]
        private async Task OpenIFCFileAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "IFC Files (*.ifc)|*.ifc|All Files (*.*)|*.*",
                Title = "Select IFC File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await ParseAndImportIFCFileAsync(openFileDialog.FileName);
            }
        }

        private async Task ParseAndImportIFCFileAsync(string filePath)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Parsing IFC file...";

                // Parse IFC file
                var ifcModel = await _ifcParserService.ParseIFCFileAsync(filePath);

                // Update file info
                FileName = ifcModel.FileName;
                ProjectName = ifcModel.ProjectName;

                // Clear existing data
                StatusMessage = "Clearing existing database...";
                await _elementService.DeleteAllAsync();

                // Import all elements
                StatusMessage = "Importing elements to database...";
                var allElements = ifcModel.Columns
                    .Concat(ifcModel.Beams)
                    .Concat(ifcModel.Slabs)
                    .ToList();

                await _elementService.ImportElementsAsync(allElements);

                // Reload from database
                StatusMessage = "Loading elements...";
                await LoadElementsFromDatabaseAsync();

                // Render 3D
                StatusMessage = "Rendering 3D view...";
                Render3DView();

                StatusMessage = $"Successfully loaded {TotalElements} elements from {ifcModel.FloorCount} floors!";
            }
            catch (Exception ex)
            {
                // DETAYLI HATA MESAJI
                var errorMessage = $"Error: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nInner Error: {ex.InnerException.Message}";
                }

                StatusMessage = errorMessage;
                ShowErrorMessage("Error parsing IFC file", errorMessage);

                // Console'a da yaz
                System.Diagnostics.Debug.WriteLine($"FULL ERROR: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ClearDatabaseAsync()
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all elements from the database?",
                "Confirm Clear",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    StatusMessage = "Clearing database...";

                    await _elementService.DeleteAllAsync();

                    ClearAllData();

                    StatusMessage = "Database cleared successfully.";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error: {ex.Message}";
                    ShowErrorMessage("Error clearing database", ex.Message);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        #endregion

        #region Commands - Data Loading

        [RelayCommand]
        private async Task LoadElementsFromDatabaseAsync()
        {
            try
            {
                var elements = await _elementService.GetAllAsync();
                var elementList = elements.ToList();

                AllElements = new ObservableCollection<StructuralElementDto>(elementList);

                // Extract floors
                var floors = elementList
                    .Select(e => e.FloorLevel)
                    .Distinct()
                    .OrderBy(f => f)
                    .ToList();

                AvailableFloors = new ObservableCollection<int>(floors);
                SelectedFloor = null;

                // Update filters and statistics
                ApplyFilters();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading elements: {ex.Message}";
                ShowErrorMessage("Error loading elements", ex.Message);
            }
        }

        #endregion

        #region Commands - Filtering

        [RelayCommand]
        private void ApplyFilters()
        {
            var filtered = AllElements.AsEnumerable();

            // Search text filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(e =>
                    e.Name.ToLower().Contains(searchLower) ||
                    e.ElementType.ToLower().Contains(searchLower) ||
                    e.GlobalId.ToLower().Contains(searchLower));
            }

            // Floor filter
            if (SelectedFloor.HasValue)
            {
                filtered = filtered.Where(e => e.FloorLevel == SelectedFloor.Value);
            }

            // Element type filter
            if (SelectedElementTypeFilter != "All")
            {
                filtered = filtered.Where(e => e.ElementType == SelectedElementTypeFilter);
            }

            FilteredElements = new ObservableCollection<StructuralElementDto>(filtered);
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = "";
            SelectedFloor = null;
            SelectedElementTypeFilter = "All";
        }

        #endregion

        #region Commands - 3D Rendering

        [RelayCommand]
        private void Render3DView()
        {
            try
            {
                // Determine which elements to render
                var elementsToRender = DetermineElementsToRender();

                if (!elementsToRender.Any())
                {
                    SceneModel = new Model3DGroup();
                    StatusMessage = "No elements to render.";
                    return;
                }

                // Create 3D scene
                SceneModel = _rendering3DService.CreateSceneModel(elementsToRender);

                // Update camera to fit all elements
                if (Viewport3D != null)
                {
                    _rendering3DService.UpdateCamera(Viewport3D, elementsToRender);
                }

                StatusMessage = $"Rendered {elementsToRender.Count()} elements.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"3D rendering error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"3D rendering error: {ex}");
            }
        }

        [RelayCommand]
        private void ShowAllFloors()
        {
            SelectedFloor = null;
            SelectedElementTypeFilter = "All";
            ApplyFilters();
            Render3DView();
        }

        [RelayCommand]
        private void ShowOnlyColumns()
        {
            SelectedElementTypeFilter = "Column";
            ApplyFilters();
            Render3DView();
        }

        [RelayCommand]
        private void ShowOnlyBeams()
        {
            SelectedElementTypeFilter = "Beam";
            ApplyFilters();
            Render3DView();
        }

        [RelayCommand]
        private void ShowOnlySlabs()
        {
            SelectedElementTypeFilter = "Slab";
            ApplyFilters();
            Render3DView();
        }

        #endregion

        #region Property Changed Handlers

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnSelectedFloorChanged(int? value)
        {
            ApplyFilters();
            Render3DView();
        }

        partial void OnSelectedElementTypeFilterChanged(string value)
        {
            ApplyFilters();
            Render3DView();
        }

        #endregion

        #region Private Helper Methods

        private IEnumerable<StructuralElementDto> DetermineElementsToRender()
        {
            // If filters are active, show filtered elements
            if (SelectedFloor.HasValue ||
                SelectedElementTypeFilter != "All" ||
                !string.IsNullOrWhiteSpace(SearchText))
            {
                return FilteredElements;
            }

            // Otherwise show all elements
            return AllElements;
        }

        private void UpdateStatistics()
        {
            TotalElements = AllElements.Count;
            ColumnCount = AllElements.Count(e => e.ElementType == "Column");
            BeamCount = AllElements.Count(e => e.ElementType == "Beam");
            SlabCount = AllElements.Count(e => e.ElementType == "Slab");
            TotalVolume = AllElements.Sum(e => e.Volume);
        }

        private void ClearAllData()
        {
            AllElements.Clear();
            FilteredElements.Clear();
            AvailableFloors.Clear();

            FileName = "No file selected";
            ProjectName = "";
            SearchText = "";
            SelectedFloor = null;
            SelectedElementTypeFilter = "All";
            SelectedElement = null;

            TotalElements = 0;
            ColumnCount = 0;
            BeamCount = 0;
            SlabCount = 0;
            TotalVolume = 0;

            SceneModel = new Model3DGroup();
        }

        private void ShowErrorMessage(string title, string message)
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        #endregion
    }
}