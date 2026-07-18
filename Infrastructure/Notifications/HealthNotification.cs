using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Options;
using OptimusPrime.Core.Settings;
using OptimusPrime.Features.Contracts.Models;
using System.Text.Json;

namespace OptimusPrime.Infrastructure.Notifications;

public class HealthNotification(
    IOptions<FirebaseSettings> options,
    ILogger<HealthNotification> logger) {

    private readonly FirebaseSettings _settings = options.Value;
    private readonly ILogger<HealthNotification> _logger = logger;

    // ====================================================
    // Notify Firebase of system health status
    // ====================================================

    public async Task SendNotification(string title, string body, List<HealthModel>? healthModels) {
        try {

            string healthData = JsonSerializer.Serialize(healthModels);
            string urlImage = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/items/poke-ball.png";

            var message = new Message() {

                Token = _settings.Notification, 

                Notification = new Notification() {
                    Title = title,
                    Body = body,
                    ImageUrl = urlImage
                },

                Data = new Dictionary<string, string>() {
                    { "evento", "health_check" },
                    { "timestamp", DateTime.Now.ToString("O") },
                    { "servizi_unhealthy", healthData }
                },

                // Android configuration 
                Android = new AndroidConfig() {
                    Priority = Priority.High,
                    Notification = new AndroidNotification() {
                        Sound = "default",
                        ChannelId = "allarme_server",
                        DefaultVibrateTimings = true,
                        ImageUrl = urlImage
                    }
                },

                // Apple/iOS configuration
                Apns = new ApnsConfig() {
                    Headers = new Dictionary<string, string>() {
                        { "apns-priority", "10" }
                    },
                    Aps = new Aps() {
                        Sound = "default",
                        MutableContent = true
                    },
                    FcmOptions = new ApnsFcmOptions() {
                        ImageUrl = urlImage
                    }
                }
            };

            await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("System notification sent: {Title}", title);

        } catch(Exception ex) {
            _logger.LogError("Error sending notification: {Message}", ex.Message);
        }
    }
}