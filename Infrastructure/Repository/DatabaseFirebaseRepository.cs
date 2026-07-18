using Microsoft.Extensions.Options;
using OptimusPrime.Features.Contracts.Models;
using OptimusPrime.Features.Interfaces;
using OptimusPrime.Core.Settings;
using OptimusPrime.Core.Utils;
using System.Text;
using System.Text.Json;
using OptimusPrime.Core.Network;

namespace OptimusPrime.Infrastructure.Repository;

public class DatabaseFirebaseRepository(
    HttpClient httpClient, 
    IOptions<FirebaseSettings> options,
    ILogger<DatabaseFirebaseRepository> logger) : IFirebaseRepository {

    private readonly FirebaseSettings _settings = options.Value;
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<DatabaseFirebaseRepository> _logger = logger;

    // ====================================================
    // Database Operations: Firebase
    // ====================================================

    // add instruction
    public async Task<Result<bool>> AddInstruction(InstructionModel instruction) {
        try {
            // prevent injection 
            string encodedToken = Uri.EscapeDataString(_settings.AuthToken);
            string url = $"{_settings.BaseUrl}/comandi.json?auth={encodedToken}";
            string jsonPayload = JsonSerializer.Serialize(instruction);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if(!response.IsSuccessStatusCode) {
                string responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Error during adding instruction: {ReasonPhrase}. Detail: {Body}", response.ReasonPhrase, responseBody);
                return Result<bool>.Failure("Error during adding instruction", ErrorType.DatabaseError);
            }

            return Result<bool>.Success("Instruction added successfully");

        } catch(Exception ex) {
            _logger.LogError("Error during adding instruction: {Message}", ex.Message);   
            return Result<bool>.Failure("Error during adding instruction", ErrorType.InternalError);
        }
    }

    // update system health status on Firebase
    public async Task<Result<bool>> UpdateHealthStatus(List<HealthModel> healthModels) {
        try {
            // prevent injection 
            string encodedToken = Uri.EscapeDataString(_settings.AuthToken);
            string url = $"{_settings.BaseUrl}/health.json?auth={encodedToken}";

            var payload = healthModels.ToDictionary(
                model => model.Name.ToLower(), 
                model => model.IsActive       
            );

            string json = JsonSerializer.Serialize(payload);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json" 
            );

            var response = await _httpClient.PatchAsync(url, content);

            if(!response.IsSuccessStatusCode) {
                string responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Error during updating health list: {ReasonPhrase}. Detail: {Body}", response.ReasonPhrase, responseBody);
                return Result<bool>.Failure("Error during updating health list", ErrorType.DatabaseError);
            }

            return Result<bool>.Success(true, "Health list updated successfully");

        } catch(Exception ex) {
            _logger.LogError("Error during updating health list: {Message}", ex.Message);
            return Result<bool>.Failure("Error during updating health list", ErrorType.InternalError);
        }
    }
}