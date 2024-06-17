using SpotifyAPI.Web;
using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace MusicPlayer.Services
{
    public class SpotifyService
    {
        private SpotifyClient _spotifyClient;
        private readonly string _clientId = "c3c6da706dc04749a1ba88a272d0f02f"; // Replace with your actual Client ID
        private readonly string _clientSecret = "847bb612ec03400cbe3ed1f06b8891e3"; // Replace with your actual Client Secret
        private readonly string _redirectUri = "http://localhost:5000/callback/"; // Make sure this matches the redirect URI in your Spotify dashboard
        private HttpListener _httpListener = new HttpListener();
        public event Action<bool> AuthenticationCompleted;

        public SpotifyService()
        {
            _httpListener.Prefixes.Add(_redirectUri);
        }

        public async Task StartAuthentication()
        {
            var loginRequest = new LoginRequest(new Uri(_redirectUri), _clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new[] { Scopes.PlaylistReadPrivate, Scopes.UserReadPlaybackState, Scopes.UserModifyPlaybackState }
            };
            try
            {
                _httpListener.Start();
                var uri = loginRequest.ToUri();
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });

                // Listening for the OAuth callback
                var context = await _httpListener.GetContextAsync();
                var code = context.Request.QueryString.Get("code");
                var isAuthenticated = await AuthenticateWithCodeAsync(code);
                AuthenticationCompleted?.Invoke(isAuthenticated);

                // Trigger the event
                AuthenticationCompleted?.Invoke(isAuthenticated);

                // Send a response back to the browser
                var response = context.Response;
                string responseString = "<html><body>Authentication successful. You can close this window.</body></html>";
                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during authentication: {ex.Message}");
                AuthenticationCompleted?.Invoke(false);
            }
            finally
            {
                _httpListener.Stop();
            }
        }

        public void Logout()
        {
            _spotifyClient = null;
            // Här kan du även rensa sparade tokens om du lagrar dem lokalt
            Console.WriteLine("Logged out successfully.");
        }


        private async Task<bool> AuthenticateWithCodeAsync(string code)
        {
            var tokenRequest = new AuthorizationCodeTokenRequest(_clientId, _clientSecret, code, new Uri(_redirectUri));
            try
            {
                var response = await new OAuthClient().RequestToken(tokenRequest);
                _spotifyClient = new SpotifyClient(response.AccessToken);
                return true; // Returnerar true om autentiseringen lyckades
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to authenticate with Spotify: {ex.Message}");
                return false; // Returnerar false om något fel inträffar
            }
        }


        public async Task<bool> PlayMusicAsync(string spotifyUri)
        {
            if (_spotifyClient == null)
            {
                Console.WriteLine("Spotify client is not initialized.");
                return false;
            }

            try
            {
                var request = new PlayerResumePlaybackRequest
                {
                    Uris = new[] { spotifyUri }
                };
                await _spotifyClient.Player.ResumePlayback(request);
                return true;
            }
            catch (APIException e)
            {
                Console.WriteLine($"Error playing music: {e.Message}");
                return false;
            }
        }

        public async Task<bool> PauseMusicAsync()
        {
            if (_spotifyClient == null)
            {
                Console.WriteLine("Spotify client is not initialized.");
                return false;
            }

            try
            {
                await _spotifyClient.Player.PausePlayback();
                return true;
            }
            catch (APIException e)
            {
                Console.WriteLine($"Error pausing music: {e.Message}");
                return false;
            }
        }



        public async Task<CurrentlyPlayingContext> GetCurrentlyPlayingTrack()
        {
            if (_spotifyClient == null)
            {
                Console.WriteLine("Spotify client is not initialized.");
                return null;
            }

            try
            {
                var currentlyPlaying = await _spotifyClient.Player.GetCurrentPlayback();
                return currentlyPlaying;
            }
            catch (APIException ex)
            {
                Console.WriteLine($"API Exception when fetching currently playing track: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General exception when fetching currently playing track: {ex.Message}");
                return null;
            }
        }


        public async Task<SearchResponse> GeneralSearchAsync(string query)
        {
            if (_spotifyClient == null)
            {
                Console.WriteLine("Spotify client is not initialized.");
                return null;
            }

            try
            {
                var searchRequest = new SearchRequest(SearchRequest.Types.Track | SearchRequest.Types.Artist, query);
                var result = await _spotifyClient.Search.Item(searchRequest);
                return result;
            }
            catch (APIException ex)
            {
                Console.WriteLine($"API Exception during search: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General exception during search: {ex.Message}");
                return null;
            }
        }
    }
}
