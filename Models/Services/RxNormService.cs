using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace LifeHub.Models.Services
{
    public class RxNormService : IMedicationApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RxNormService> _logger;

        public RxNormService(HttpClient httpClient, IMemoryCache cache, ILogger<RxNormService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            
            _httpClient.BaseAddress = new Uri("https://rxnav.nlm.nih.gov/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "LifeHub-App/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
            
            _logger.LogInformation("✅ RxNorm Service inicializado");
        }

        public async Task<List<MedicationSuggestion>> SearchMedicationsAsync(string searchTerm)
        {
            _logger.LogInformation("🔍 [RxNorm] Buscando: '{SearchTerm}'", searchTerm);

            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                return new List<MedicationSuggestion>();

            var cacheKey = $"rxnorm_{searchTerm.ToLower()}";

            if (_cache.TryGetValue(cacheKey, out List<MedicationSuggestion>? cachedResults))
            {
                _logger.LogInformation("✅ [RxNorm] Cache hit: {Count} resultados", cachedResults?.Count ?? 0);
                return cachedResults ?? new List<MedicationSuggestion>();
            }

            try
            {
                var englishTerm = TranslateToEnglish(searchTerm);
                _logger.LogInformation("🌐 [RxNorm] Búsqueda internacional: '{EnglishTerm}'", englishTerm);
                
                var results = new List<MedicationSuggestion>();

                // Estrategia 1: Búsqueda exacta
                var exactResults = await SearchExact(englishTerm);
                results.AddRange(exactResults);

                // Estrategia 2: Búsqueda aproximada
                if (results.Count < 5)
                {
                    var approxResults = await SearchApproximate(englishTerm);
                    results.AddRange(approxResults);
                }

                // Estrategia 3: Fallback local
                if (!results.Any())
                {
                    results = GetLocalMedications(searchTerm);
                }

                var finalResults = ProcessResults(results, searchTerm);
                
                _logger.LogInformation("🎯 [RxNorm] Búsqueda completada: {Count} resultados", finalResults.Count);

                _cache.Set(cacheKey, finalResults, TimeSpan.FromHours(24));
                return finalResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [RxNorm] Error en búsqueda");
                return GetLocalMedications(searchTerm);
            }
        }

        private async Task<List<MedicationSuggestion>> SearchExact(string term)
        {
            try
            {
                var url = $"REST/drugs.json?name={Uri.EscapeDataString(term)}";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var rxNormResponse = JsonSerializer.Deserialize<RxNormResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return ProcessExactResponse(rxNormResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ [RxNorm] Búsqueda exacta falló: {Error}", ex.Message);
            }
            
            return new List<MedicationSuggestion>();
        }

        private async Task<List<MedicationSuggestion>> SearchApproximate(string term)
        {
            try
            {
                var url = $"REST/approximateTerm.json?term={Uri.EscapeDataString(term)}&maxEntries=10";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var approxResponse = JsonSerializer.Deserialize<RxNormApproximateResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return ProcessApproximateResponse(approxResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ [RxNorm] Búsqueda aproximada falló: {Error}", ex.Message);
            }
            
            return new List<MedicationSuggestion>();
        }

        private List<MedicationSuggestion> ProcessExactResponse(RxNormResponse? response)
        {
            var results = new List<MedicationSuggestion>();

            if (response?.DrugGroup?.ConceptGroup != null)
            {
                foreach (var conceptGroup in response.DrugGroup.ConceptGroup)
                {
                    if (conceptGroup.ConceptProperties != null)
                    {
                        foreach (var concept in conceptGroup.ConceptProperties)
                        {
                            if (!string.IsNullOrEmpty(concept.Name))
                            {
                                var suggestion = new MedicationSuggestion
                                {
                                    Id = concept.Rxcui ?? Guid.NewGuid().ToString(),
                                    Name = concept.Name,
                                    Source = "RxNorm NIH",
                                    DosageForm = GetDosageFormFromName(concept.Name),
                                    Strength = GetStrengthFromName(concept.Name)
                                };

                                if (!IsTooGeneric(suggestion.Name))
                                {
                                    results.Add(suggestion);
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }

        private List<MedicationSuggestion> ProcessApproximateResponse(RxNormApproximateResponse? response)
        {
            var results = new List<MedicationSuggestion>();

            if (response?.ApproximateGroup?.Candidate?.Any() == true)
            {
                foreach (var candidate in response.ApproximateGroup.Candidate)
                {
                    if (!string.IsNullOrEmpty(candidate.Name))
                    {
                        var suggestion = new MedicationSuggestion
                        {
                            Id = candidate.Rxcui ?? Guid.NewGuid().ToString(),
                            Name = candidate.Name,
                            Source = "RxNorm NIH",
                            Strength = candidate.Strength
                        };

                        if (!IsTooGeneric(suggestion.Name))
                        {
                            results.Add(suggestion);
                        }
                    }
                }
            }

            return results;
        }

        private List<MedicationSuggestion> ProcessResults(List<MedicationSuggestion> results, string originalSearch)
        {
            var uniqueResults = results
                .GroupBy(m => m.Name.ToLower())
                .Select(g => g.First())
                .ToList();

            var sortedResults = uniqueResults
                .OrderBy(m => m.Name.ToLower().StartsWith(originalSearch.ToLower()) ? 0 : 1)
                .ThenBy(m => m.Name.Length)
                .ThenBy(m => m.Name)
                .Take(15)
                .ToList();

            return sortedResults;
        }

        private bool IsTooGeneric(string name)
        {
            var genericTerms = new[] { "product", "pack", "kit", "device", "system", "unknown" };
            return genericTerms.Any(term => name.ToLower().Contains(term));
        }

        private string TranslateToEnglish(string spanishTerm)
        {
            var translations = new Dictionary<string, string>
            {
                ["paracetamol"] = "acetaminophen",
                ["ibuprofeno"] = "ibuprofen", 
                ["amoxicilina"] = "amoxicillin",
                ["omeprazol"] = "omeprazole",
                ["loratadina"] = "loratadine",
                ["aspirina"] = "aspirin",
                ["metformina"] = "metformin",
                ["hidroxicloroquina"] = "hydroxychloroquine",
                ["losartán"] = "losartan",
                ["atorvastatina"] = "atorvastatin",
                ["simvastatina"] = "simvastatin",
                ["metoprolol"] = "metoprolol",
                ["clonazepam"] = "clonazepam",
                ["sertralina"] = "sertralina",
                ["fluoxetina"] = "fluoxetine",
                ["diazepam"] = "diazepam",
                ["alprazolam"] = "alprazolam",
                ["warfarina"] = "warfarin",
                ["insulina"] = "insulin",
                ["levotiroxina"] = "levothyroxine",
                ["prednisona"] = "prednisone"
            };

            var lowerTerm = spanishTerm.ToLower().Trim();
            return translations.ContainsKey(lowerTerm) ? translations[lowerTerm] : spanishTerm;
        }

        private string? GetDosageFormFromName(string name)
        {
            var forms = new Dictionary<string, string>
            {
                ["tablet"] = "Tableta",
                ["capsule"] = "Cápsula",
                ["injection"] = "Inyección",
                ["solution"] = "Solución",
                ["cream"] = "Crema",
                ["ointment"] = "Pomada",
                ["inhaler"] = "Inhalador"
            };

            var lowerName = name.ToLower();
            foreach (var form in forms)
            {
                if (lowerName.Contains(form.Key))
                    return form.Value;
            }

            return null;
        }

        private string? GetStrengthFromName(string name)
        {
            var patterns = new[] 
            {
                @"\b(\d+\s*(mg|mcg|g|ml|%))\b",
                @"\b(\d+\s*/\s*\d+\s*(mg|mcg|g|ml))\b"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(name, pattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Value;
            }

            return null;
        }

        private List<MedicationSuggestion> GetLocalMedications(string searchTerm)
        {
            var allMeds = GetComprehensiveLocalDatabase();
            
            return allMeds
                .Where(m => m.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }

        private List<MedicationSuggestion> GetComprehensiveLocalDatabase()
        {
            return new List<MedicationSuggestion>
            {
                new() { Id = "1", Name = "Paracetamol", DosageForm = "Tableta", Strength = "500mg", Source = "Base Local" },
                new() { Id = "2", Name = "Ibuprofeno", DosageForm = "Tableta", Strength = "400mg", Source = "Base Local" },
                new() { Id = "3", Name = "Aspirina", DosageForm = "Tableta", Strength = "100mg", Source = "Base Local" },
                new() { Id = "4", Name = "Amoxicilina", DosageForm = "Cápsula", Strength = "500mg", Source = "Base Local" },
                new() { Id = "5", Name = "Omeprazol", DosageForm = "Cápsula", Strength = "20mg", Source = "Base Local" },
                new() { Id = "6", Name = "Loratadina", DosageForm = "Tableta", Strength = "10mg", Source = "Base Local" },
                new() { Id = "7", Name = "Metformina", DosageForm = "Tableta", Strength = "850mg", Source = "Base Local" },
                new() { Id = "8", Name = "Losartán", DosageForm = "Tableta", Strength = "50mg", Source = "Base Local" },
                new() { Id = "9", Name = "Atorvastatina", DosageForm = "Tableta", Strength = "20mg", Source = "Base Local" },
                new() { Id = "10", Name = "Clonazepam", DosageForm = "Tableta", Strength = "2mg", Source = "Base Local" }
            };
        }

        public Task<MedicationInfo?> GetMedicationDetailsAsync(string medicationId)
        {
            return Task.FromResult<MedicationInfo?>(null);
        }
    }
}