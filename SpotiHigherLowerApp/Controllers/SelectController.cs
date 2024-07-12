using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotifyAPI.Web;
using SpotiHigherLowerApp.Models.ViewModel;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;


namespace SpotiHigherLowerApp.Controllers
{
    [Authorize]
    public class SelectController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var accessToken = User.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value;

            if (accessToken == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var spotify = new SpotifyClient(accessToken);
            try
            {
                var playlists = await spotify.Playlists.CurrentUsers().ConfigureAwait(false);

                // Mapear las playlists a PlaylistViewModel
                var playlistViewModels = playlists.Items.Select(p => new PlaylistViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Owner = p.Owner.DisplayName // Aquí podrías ajustar según la estructura de datos de Spotify
                }).ToList();

                return View(playlistViewModels);
            }
            catch (APIException ex)
            {
                // Manejar errores de la API de Spotify, como falta de permisos, etc.
                ModelState.AddModelError("", $"Error al obtener las playlists de Spotify: {ex.Message}");
                return View(new List<PlaylistViewModel>());
            }
        }

    }
}
