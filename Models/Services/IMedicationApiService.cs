using System.Text.Json.Serialization;

namespace LifeHub.Models.Services
{
    public interface IMedicationApiService
    {
        Task<List<MedicationSuggestion>> SearchMedicationsAsync(string searchTerm);
        Task<MedicationInfo?> GetMedicationDetailsAsync(string medicationId);
    }

    public class MedicationSuggestion
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? DosageForm { get; set; }
        public string? Route { get; set; }
        public string? Manufacturer { get; set; }
        public string? Strength { get; set; }
        public string Source { get; set; } = "RxNorm";
    }

    public class MedicationInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? GenericName { get; set; }
        public string? DosageForm { get; set; }
        public string? Route { get; set; }
        public string? Strength { get; set; }
        public string? Manufacturer { get; set; }
        public List<string> ActiveIngredients { get; set; } = new List<string>();
    }

    // Clases para RxNorm
    public class RxNormResponse
    {
        [JsonPropertyName("drugGroup")]
        public DrugGroup? DrugGroup { get; set; }
    }

    public class DrugGroup
    {
        [JsonPropertyName("conceptGroup")]
        public List<ConceptGroup>? ConceptGroup { get; set; }
    }

    public class ConceptGroup
    {
        [JsonPropertyName("conceptProperties")]
        public List<ConceptProperty>? ConceptProperties { get; set; }
    }

    public class ConceptProperty
    {
        [JsonPropertyName("rxcui")]
        public string? Rxcui { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("synonym")]
        public string? Synonym { get; set; }
        
        [JsonPropertyName("tty")]
        public string? Tty { get; set; }
    }

    public class RxNormApproximateResponse
    {
        [JsonPropertyName("approximateGroup")]
        public ApproximateGroup? ApproximateGroup { get; set; }
    }

    public class ApproximateGroup
    {
        [JsonPropertyName("candidate")]
        public List<Candidate>? Candidate { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("rxcui")]
        public string? Rxcui { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("strength")]
        public string? Strength { get; set; }
    }
}