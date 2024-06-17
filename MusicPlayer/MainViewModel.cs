using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Services;
using SpotifyAPI.Web;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace MusicPlayer.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly SpotifyService _spotifyService;
        private string _searchQuery;
        private string _statusMessage;


        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                if (SetProperty(ref _isLoggedIn, value))
                {
                    OnPropertyChanged(nameof(LoginButtonText));  // Trigger UI update
                }
            }
        }

        public string LoginButtonText => IsLoggedIn ? "Connected" : "Login with Spotify";

        private string _logoutButtonText = "Sign out";
        public string LogoutButtonText
        {
            get => _logoutButtonText;
            set => SetProperty(ref _logoutButtonText, value);
        }

        public ObservableCollection<FullTrack> SearchResults { get; set; } = new ObservableCollection<FullTrack>();
        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }


        private FullTrack _currentTrack;
        public FullTrack CurrentTrack
        {
            get => _currentTrack;
            set => SetProperty(ref _currentTrack, value);
        }

        


        public async Task RefreshCurrentlyPlaying()
        {
            var playbackContext = await _spotifyService.GetCurrentlyPlayingTrack();
            if (playbackContext != null && playbackContext.Item is FullTrack track)
            {
                CurrentTrack = track; // Assuming CurrentTrack is of type FullTrack
                StatusMessage = $"Now playing: {track.Name} by {string.Join(", ", track.Artists.Select(a => a.Name))}";
            }
            else
            {
                StatusMessage = "No track is currently playing.";
            }
        }



        public ICommand RefreshPlayingTrackCommand { get; private set; }
        public ICommand SpotifyLoginCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public FullTrack SelectedTrack { get; set; }  // Bind this property to the UI

        public ICommand PlayCommand { get; private set; }
        public ICommand PauseCommand { get; }
        public ICommand LogoutCommand { get; private set; }



        public MainViewModel()
        {
            _spotifyService = new SpotifyService();
            PauseCommand = new RelayCommand(async () => await Pause(null));
            RefreshPlayingTrackCommand = new RelayCommand(async () => await RefreshCurrentlyPlaying());
            SearchCommand = new RelayCommand(async () => await ExecuteSearch());
            PlayCommand = new RelayCommand(async () => await PlaySelectedTrack());
            _spotifyService.AuthenticationCompleted += OnAuthenticationCompleted;
            LogoutCommand = new RelayCommand(Logout);

            SpotifyLoginCommand = new RelayCommand(async () => await _spotifyService.StartAuthentication());
        }

        private async Task PlaySelectedTrack()
        {
            if (SelectedTrack != null)
            {
                bool success = await _spotifyService.PlayMusicAsync(SelectedTrack.Uri);
                StatusMessage = success ? "Playing selected track." : "Failed to play the track.";
            }
        }

        private async Task Pause(object parameter)
        {
            bool success = await _spotifyService.PauseMusicAsync();
            if (success)
                StatusMessage = "Playback paused.";
            else
                StatusMessage = "Failed to pause playback.";
        }

        private void OnAuthenticationCompleted(bool isAuthenticated)
        {
            IsLoggedIn = isAuthenticated;
        }

        private void Logout()
        {
            if (IsLoggedIn)
            {
                _spotifyService.Logout();
                IsLoggedIn = false; // Uppdatera inloggningsstatusen
                StatusMessage = "Logged out successfully.";
            }
            else
            {
                StatusMessage = "You are not logged in.";
            }
        }



        private async Task ExecuteSearch()
        {
            if (string.IsNullOrEmpty(SearchQuery))
            {
                StatusMessage = "Please enter a search query.";
                return;
            }

            StatusMessage = "Searching...";
            var searchResult = await _spotifyService.GeneralSearchAsync(SearchQuery);
            SearchResults.Clear();
            if (searchResult != null && searchResult.Tracks != null && searchResult.Tracks.Items != null && searchResult.Tracks.Items.Count > 0)
            {
                foreach (var track in searchResult.Tracks.Items)
                {
                    SearchResults.Add(track);
                }
                StatusMessage = "Search completed successfully.";
            }
            else
            {
                StatusMessage = "No results found.";
            }
        }
    }
}
