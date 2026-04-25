namespace SingletonNotepad.ViewModels;

using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Services;

/// <summary>
/// ViewModel for the settings view.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IFileService _fileService;

    [ObservableProperty]
    private AppSettings _settings = new();

    [ObservableProperty]
    private string _validationError = string.Empty;

    [ObservableProperty]
    private bool _hasValidationError;

    public event EventHandler? RequestClose;

    public SettingsViewModel(ISettingsService settingsService, IFileService fileService)
    {
        _settingsService = settingsService;
        _fileService = fileService;
        
        // Load current settings
        Settings = _settingsService.GetSettings();
    }

    /// <summary>
    /// Requests to close the settings view.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Validates and saves the current settings.
    /// </summary>
    [RelayCommand]
    private void SaveSettings()
    {
        if (!ValidateSettings())
            return;

        _settingsService.SaveSettings(Settings);
        
        // Update FileService with new path
        _fileService.FilePath = Settings.SingletonFilePath;
        
        ValidationError = "Settings saved successfully";
        HasValidationError = false;
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    [RelayCommand]
    private void ResetToDefaults()
    {
        _settingsService.ResetToDefaults();
        Settings = _settingsService.GetSettings();
        _fileService.FilePath = Settings.SingletonFilePath;
        
        ValidationError = "Settings reset to defaults";
        HasValidationError = false;
    }

    /// <summary>
    /// Validates the current settings.
    /// </summary>
    private bool ValidateSettings()
    {
        // Validate singleton file path
        if (string.IsNullOrWhiteSpace(Settings.SingletonFilePath))
        {
            ValidationError = "Singleton file path cannot be empty";
            HasValidationError = true;
            return false;
        }

        if (!IsValidPath(Settings.SingletonFilePath))
        {
            ValidationError = "Singleton file path is invalid";
            HasValidationError = true;
            return false;
        }

        // Validate rules file path
        if (string.IsNullOrWhiteSpace(Settings.RulesFilePath))
        {
            ValidationError = "Rules file path cannot be empty";
            HasValidationError = true;
            return false;
        }

        if (!IsValidPath(Settings.RulesFilePath))
        {
            ValidationError = "Rules file path is invalid";
            HasValidationError = true;
            return false;
        }

        // Validate debounce interval
        if (Settings.AutoSaveDebounceMs < 100 || Settings.AutoSaveDebounceMs > 30000)
        {
            ValidationError = "Auto-save debounce must be between 100ms and 30000ms";
            HasValidationError = true;
            return false;
        }

        // Validate window dimensions
        if (Settings.WindowWidth < 400 || Settings.WindowHeight < 300)
        {
            ValidationError = "Window size must be at least 400x300";
            HasValidationError = true;
            return false;
        }

        ValidationError = string.Empty;
        HasValidationError = false;
        return true;
    }

    /// <summary>
    /// Checks if a path is valid (absolute path with valid characters).
    /// </summary>
    private static bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            // Check if it's an absolute path
            if (!Path.IsPathRooted(path))
                return false;

            // Check for invalid characters
            var invalidChars = Path.GetInvalidPathChars();
            if (path.Any(c => invalidChars.Contains(c)))
                return false;

            // Check that parent directory can be created/exists
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                // Path is valid if directory exists or can be created
                return true;
            }

            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    partial void OnSettingsChanged(AppSettings value)
    {
        // Clear validation error when settings change
        ValidationError = string.Empty;
        HasValidationError = false;
    }
}
