using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    public class NotificationClientService : IAsyncDisposable
    {
        private readonly HttpClient _http;
        private readonly LocalStorageService _localStorage;
        private HubConnection? _hubConnection;
        private Timer? _fallbackTimer;

        public List<NotificationDto> Notifications { get; private set; } = new();
        public int UnreadCount => Notifications.Count(n => !n.Lue);

        public event Action<NotificationDto>? OnNotificationReceived;
        public event Action? OnUnreadCountChanged;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public NotificationClientService(HttpClient http, LocalStorageService localStorage)
        {
            _http = http;
            _localStorage = localStorage;
        }

        public async Task InitializeAsync()
        {
            await ChargerNotificationsInitialesAsync();
            await DemarrerSignalRAsync();

            // Démarrer le timer de secours (polling toutes les 30s si déconnecté de SignalR)
            _fallbackTimer = new Timer(async _ =>
            {
                if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
                {
                    await PollFallbackNotificationsAsync();
                }
            }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public async Task DemarrerSignalRAsync()
        {
            var token = await _localStorage.GetItemAsync("authToken");
            if (string.IsNullOrEmpty(token)) return;

            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }

            var baseAddress = _http.BaseAddress?.ToString() ?? "https://localhost:7179/";
            var hubUrl = new Uri(new Uri(baseAddress), "hubs/notifications").ToString();

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        return await _localStorage.GetItemAsync("authToken");
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<NotificationDto>("ReceiveNotification", (notification) =>
            {
                // Éviter les doublons
                if (!Notifications.Any(n => n.Id == notification.Id))
                {
                    Notifications.Insert(0, notification);
                    OnNotificationReceived?.Invoke(notification);
                    OnUnreadCountChanged?.Invoke();
                }
            });

            try
            {
                await _hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Erreur lors de la connexion : {ex.Message}. Le polling de secours prendra le relais.");
            }
        }

        public async Task ChargerNotificationsInitialesAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync("authToken");
                if (string.IsNullOrEmpty(token)) return;

                var resultats = await _http.GetFromJsonAsync<List<NotificationDto>>("api/notification");
                if (resultats != null)
                {
                    Notifications = resultats;
                    OnUnreadCountChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationClientService] Erreur chargement initial : {ex.Message}");
            }
        }

        public async Task PollFallbackNotificationsAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync("authToken");
                if (string.IsNullOrEmpty(token)) return;

                var resultats = await _http.GetFromJsonAsync<List<NotificationDto>>("api/notification/non-lues");
                if (resultats != null)
                {
                    bool modifie = false;
                    foreach (var notif in resultats)
                    {
                        if (!Notifications.Any(n => n.Id == notif.Id))
                        {
                            Notifications.Insert(0, notif);
                            OnNotificationReceived?.Invoke(notif);
                            modifie = true;
                        }
                    }
                    if (modifie)
                    {
                        OnUnreadCountChanged?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationClientService] Erreur polling de secours : {ex.Message}");
            }
        }

        public async Task MarquerCommeLueAsync(int notificationId)
        {
            var notif = Notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notif != null)
            {
                notif.Lue = true;
                OnUnreadCountChanged?.Invoke();
            }

            try
            {
                await _http.PutAsync($"api/notification/{notificationId}/marquer-lue", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationClientService] Erreur marquer lue : {ex.Message}");
            }
        }

        public async Task MarquerToutesCommeLuesAsync()
        {
            foreach (var n in Notifications)
            {
                n.Lue = true;
            }
            OnUnreadCountChanged?.Invoke();

            try
            {
                await _http.PutAsync("api/notification/marquer-toutes-lues", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationClientService] Erreur tout marquer comme lu : {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            _fallbackTimer?.Dispose();
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
