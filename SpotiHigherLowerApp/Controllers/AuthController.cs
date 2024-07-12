using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;

using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SpotiHigherLowerApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Login()
        {
            // Generar un estado aleatorio
            var state = Guid.NewGuid().ToString("N");

            // Guardar el estado en la sesión o en una cookie para validar después
            HttpContext.Session.SetString("SpotifyOAuthState", state);

            var loginRequest = new LoginRequest(
                new Uri(_configuration["Spotify:RedirectUri"]),
                _configuration["Spotify:ClientId"],
                LoginRequest.ResponseType.Code)
            {
                Scope = new[] { Scopes.PlaylistReadPrivate, Scopes.PlaylistReadCollaborative },
                State = state // Pasar el estado generado a la solicitud de autenticación
            };

            var uri = loginRequest.ToUri();
            return Redirect(uri.ToString());
        }


        public async Task<IActionResult> Game(string code, string state)
        {
            // Obtener el estado almacenado en la sesión o en la cookie
            var expectedState = HttpContext.Session.GetString("SpotifyOAuthState");

            // Verificar que el estado devuelto por Spotify coincida con el estado esperado
            if (state != expectedState)
            {
                // Manejar el error de estado inválido aquí
                return BadRequest("Invalid OAuth state.");
            }

            // Limpiar el estado de la sesión o de la cookie después de verificarlo
            HttpContext.Session.Remove("SpotifyOAuthState");

            var tokenResponse = await new OAuthClient().RequestToken(
                new AuthorizationCodeTokenRequest(
                    _configuration["Spotify:ClientId"],
                    _configuration["Spotify:ClientSecret"],
                    code,
                    new Uri(_configuration["Spotify:RedirectUri"])
                )
            );

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, "spotify_user"),
        new Claim("access_token", tokenResponse.AccessToken)
    };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity)
            );

            return RedirectToAction("Index", "Select");
        }


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
