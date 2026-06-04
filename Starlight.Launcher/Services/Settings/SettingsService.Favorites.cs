using Microsoft.Extensions.Logging;
using Starlight.Launcher.Models.Data;
using System.Text.Json;

namespace Starlight.Launcher.Services.Settings;

public sealed partial class SettingsService
{
    private List<FavoriteServer> LoadFavorites()
    {
        try
        {
            if (!File.Exists(_favoritesPath))
            {
                _logger.LogInformation("Can't find favorites file, fallback to empty.");
                return new();
            }

            var json = File.ReadAllText(_favoritesPath);
            _logger.LogInformation("Successfully loaded favorites");
            return JsonSerializer.Deserialize<List<FavoriteServer>>(json) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load favorites, using empty list");
            return new();
        }
    }

    public List<FavoriteServer> GetFavorites()
    {
        _favoritesLock.Wait();
        try
        {
            return _favorites;
        }
        finally
        {
            _favoritesLock.Release();
        }
    }

    public void WriteFavorites(List<FavoriteServer> favorites)
    {
        _favoritesLock.Wait();
        try
        {
            _favorites = favorites;
            RebuildFavoritesIndex();
        }
        finally
        {
            _favoritesLock.Release();
        }

        FavoritesChanged?.Invoke();

        ScheduleSave(settings: false, favorites: true);
    }

    public async Task<List<FavoriteServer>> GetFavoritesAsync()
    {
        await _favoritesLock.WaitAsync();
        try
        {
            return _favorites;
        }
        finally
        {
            _favoritesLock.Release();
        }
    }

    public async Task WriteFavoritesAsync(List<FavoriteServer> favorites)
    {
        await _favoritesLock.WaitAsync();
        try
        {
            _favorites = favorites;
            RebuildFavoritesIndex();
        }
        finally
        {
            _favoritesLock.Release();
        }

        FavoritesChanged?.Invoke();

        ScheduleSave(settings: false, favorites: true);
    }
    private void RebuildFavoritesIndex()
    {
        _favoriteAddresses = new HashSet<string>(
            _favorites.Select(f => f.Address),
            StringComparer.OrdinalIgnoreCase);
    }
}
