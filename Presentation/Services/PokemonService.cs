using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using OptimusPrime.Features.Contracts.Models;

namespace OptimusPrime.Presentation.Services;

public class PokemonService(
    IHttpClientFactory httpClientFactory,
    IDistributedCache cache, 
    ILogger<PokemonService> logger) {

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<PokemonService> _logger = logger;

    private readonly List<PokemonModel> Pokemons = [];
    private const string CACHE_KEY = "pokemon_list_151";

    // ==========================================
    // Retrieve Pokemon and get thier image 
    // ==========================================

    private async Task GetPokemons() {
        if(Pokemons.Count > 0) {
            return;
        }

        try {
            string? cachedData = await _cache.GetStringAsync(CACHE_KEY);
            if(!string.IsNullOrEmpty(cachedData)) {
                var cachedList = JsonSerializer.Deserialize<List<PokemonModel>>(cachedData);
                if(cachedList != null) {
                    Pokemons.AddRange(cachedList);
                    _logger.LogDebug("Pokémon caricati istantaneamente da Redis!");
                    return;
                }
            }

        } catch(Exception ex) {
            _logger.LogWarning($"Redis non raggiungibile: {ex.Message}");
        }

        try {
            var http = _httpClientFactory.CreateClient("PokeApi");
            var response = await http.GetFromJsonAsync<PokeApiListResponse>("https://pokeapi.co/api/v2/pokemon?limit=151");

            if(response?.Results != null) {
                foreach(var p in response.Results) {

                    string id = p.Url.TrimEnd('/').Split('/').Last();
                    string imageUrl = $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{id}.png";

                    Pokemons.Add(new PokemonModel {
                        Name = p.Name,
                        Url = p.Url,
                        ImageUrl = imageUrl
                    });
                }

                try {
                    var cacheOptions = new DistributedCacheEntryOptions {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
                    };
                    await _cache.SetStringAsync(CACHE_KEY, JsonSerializer.Serialize(Pokemons), cacheOptions);

                    _logger.LogDebug("Pokémon scaricati dall'API e salvati in Redis."); // da rimuovere

                } catch(Exception ex) {
                    _logger.LogWarning("Impossibile salvare Pokémon su Redis: {Message}", ex.Message);
                }
            }

        } catch(Exception ex) {
            _logger.LogError("Errore durante il recupero dei 151 Pokémon: {Message}", ex.Message);
        }
    }

    // prende 6 Pokémon casuali 
    public async Task<List<PokemonModel>> GetRandomPokemon() {
        await GetPokemons();
        if(Pokemons.Count == 0) return [];

        // peschiamo 6 indici unici
        HashSet<int> indici = [];
        while(indici.Count < 6) {
            indici.Add(Random.Shared.Next(0, Pokemons.Count));
        }

        return [.. indici.Select(i => Pokemons[i])];
    }

    // ==========================
    // Risposte dalle API esterne
    // ==========================

    private class PokeApiListResponse {
        [JsonPropertyName("results")]
        public List<PokeApiResult>? Results { get; set; }
    }

    private class PokeApiResult {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}