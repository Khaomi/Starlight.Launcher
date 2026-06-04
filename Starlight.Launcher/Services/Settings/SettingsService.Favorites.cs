using Starlight.Launcher.Models.Data;

namespace Starlight.Launcher.Services.Settings;

public sealed partial class SettingsService
{
    public List<FavoriteServer> GetFavorites()
    {
        _favoritesLock.Wait();
        try
        {
            return _favorites.ToList();
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
            return _favorites.ToList();
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
