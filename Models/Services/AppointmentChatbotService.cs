// LifeHub/Models/Services/AppointmentChatbotService.cs (CORREGIDO)
using System.Text;
using System.Text.Json;
using LifeHub.Data;
using LifeHub.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Models.Services
{
    public class AppointmentChatbotService : IAppointmentChatbotService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentChatbotService> _logger;
        private readonly HttpClient _httpClient;

        public AppointmentChatbotService(ApplicationDbContext context, 
                                       ILogger<AppointmentChatbotService> logger,
                                       HttpClient httpClient)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<AppointmentPreparation> PrepareForAppointmentAsync(int appointmentId)
        {
            var preparation = new AppointmentPreparation();
            
            var appointment = await _context.MedicalAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                return preparation;

            // Obtener historial reciente del usuario
            var recentWellness = await _context.WellnessChecks
                .Where(w => w.UserId == appointment.UserId && 
                           w.CheckDate >= DateTime.UtcNow.AddDays(-30))
                .ToListAsync();

            var currentSymptoms = recentWellness
                .SelectMany(w => w.GetSymptomsList())
                .Distinct()
                .Select(s => s.ToString())
                .ToList();

            // Generar preparación usando Ollama
            try
            {
                var ollamaResponse = await GenerateWithOllama($@"
                Genera una preparación para cita médica con estas especificaciones:
                - Especialidad: {appointment.Specialty ?? "General"}
                - Preocupaciones de salud: {appointment.HealthConcerns ?? "No especificadas"}
                - Síntomas actuales: {string.Join(", ", currentSymptoms)}
                - Tipo de cita: {appointment.Title}

                Por favor genera:
                1. 5 preguntas específicas para el doctor
                2. Lista de documentos a llevar
                3. Pasos de preparación
                4. Instrucciones especiales
                5. Un iniciador de conversación apropiado

                Formato JSON con: questions, documents, steps, instructions, conversation_starter
                ");

                if (!string.IsNullOrEmpty(ollamaResponse))
                {
                    // Parsear respuesta de Ollama (simplificado)
                    preparation.QuestionsForDoctor = ExtractListFromResponse(ollamaResponse);
                    preparation.DocumentsToBring = ExtractDocumentsFromResponse(ollamaResponse);
                    preparation.PreparationSteps = ExtractStepsFromResponse(ollamaResponse);
                    preparation.SpecialInstructions = ExtractInstructionsFromResponse(ollamaResponse);
                    preparation.ConversationStarter = ExtractConversationStarterFromResponse(ollamaResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar preparación con Ollama");
                // Fallback a preparación básica
                preparation = await GenerateFallbackPreparation(appointment, currentSymptoms);
            }

            preparation.Checklist = await GenerateChecklistAsync(appointment.Specialty ?? "General", currentSymptoms);

            return preparation;
        }

        public async Task<List<string>> GenerateQuestionsForDoctorAsync(string specialty, List<string> currentSymptoms)
        {
            try
            {
                var ollamaResponse = await GenerateWithOllama($@"
                Genera 5 preguntas específicas para una cita con {specialty}.
                Síntomas actuales: {string.Join(", ", currentSymptoms)}
                
                Las preguntas deben ser:
                - Específicas de la especialidad
                - Prácticas y accionables
                - Centradas en el paciente
                - Incluir preguntas sobre diagnóstico y tratamiento

                Devuelve solo la lista de preguntas numeradas.
                ");

                return ExtractListFromResponse(ollamaResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar preguntas con Ollama");
                return GenerateFallbackQuestions(specialty);
            }
        }

        public async Task<Checklist> GenerateChecklistAsync(string appointmentType, List<string> healthHistory)
        {
            var checklist = new Checklist { Category = appointmentType };

            try
            {
                var ollamaResponse = await GenerateWithOllama($@"
                Genera una checklist para cita médica de {appointmentType}.
                Historial de salud: {string.Join(", ", healthHistory)}

                Incluye:
                - Documentos médicos importantes
                - Preparativos previos
                - Preguntas para llevar
                - Elementos esenciales

                Formato: lista de items con importancia.
                ");

                var items = ExtractChecklistItemsFromResponse(ollamaResponse);
                checklist.Items = items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar checklist con Ollama");
                checklist.Items = GenerateFallbackChecklist(appointmentType);
            }

            return checklist;
        }

        public async Task<string> GenerateConversationStarterAsync(string doctorName, string specialty)
        {
            try
            {
                return await GenerateWithOllama($@"
                Genera un iniciador de conversación apropiado para una cita con Dr. {doctorName} (especialidad: {specialty}).
                Debe ser profesional pero cercano, mostrando preparación y respeto.
                Máximo 2 frases.
                ");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar conversación con Ollama");
                return $"Buenos días Dr. {doctorName}, he preparado algunas preguntas sobre mi salud.";
            }
        }

        private async Task<string> GenerateWithOllama(string prompt)
        {
            try
            {
                var requestData = new
                {
                    model = "llama2", // o "mistral", "codellama" según lo que tengas instalado
                    prompt = prompt,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Ollama typically runs on http://localhost:11434
                var response = await _httpClient.PostAsync("http://localhost:11434/api/generate", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);

                return ollamaResponse?.Response ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Ollama API");
                throw;
            }
        }

        private List<string> ExtractListFromResponse(string response)
        {
            // Implementación mejorada de parsing
            var lines = response.Split('\n')
                              .Where(line => line.Trim().StartsWith("-") || 
                                           (line.Trim().Length > 0 && char.IsDigit(line.Trim()[0])))
                              .Select(line => line.Trim().TrimStart('-', '1', '2', '3', '4', '5', '.', ' ').Trim())
                              .Where(line => !string.IsNullOrEmpty(line) && line.Length > 10)
                              .Take(5)
                              .ToList();

            return lines.Any() ? lines : new List<string> { 
                "¿Cuál es el diagnóstico probable?",
                "¿Qué opciones de tratamiento existen?",
                "¿Hay cambios en el estilo de vida que deba hacer?",
                "¿Cuándo debo seguir para control?",
                "¿Hay algún efecto secundario que deba observar?"
            };
        }

        private List<string> ExtractDocumentsFromResponse(string response)
        {
            var lines = ExtractListFromResponse(response);
            if (lines.Any(l => l.ToLower().Contains("documento") || l.ToLower().Contains("identificación")))
                return lines;

            return new List<string>
            {
                "Identificación oficial",
                "Tarjeta de seguro médico",
                "Historial médico reciente",
                "Lista de medicamentos actuales",
                "Resultados de exámenes recientes"
            };
        }

        private List<string> ExtractStepsFromResponse(string response)
        {
            var lines = ExtractListFromResponse(response);
            if (lines.Any(l => l.ToLower().Contains("llegar") || l.ToLower().Contains("preparar")))
                return lines;

            return new List<string>
            {
                "Llegar 15 minutos antes",
                "Tener lista la información de síntomas",
                "Preparar preguntas específicas",
                "Revisar historial médico",
                "Anotar cualquier alergia"
            };
        }

        private string ExtractInstructionsFromResponse(string response)
        {
            if (response.Length > 50)
            {
                var sentences = response.Split('.')
                                      .Where(s => !string.IsNullOrWhiteSpace(s))
                                      .Take(2)
                                      .Select(s => s.Trim() + ".")
                                      .ToArray();
                return string.Join(" ", sentences);
            }

            return "Lleve todos sus documentos médicos y una lista de sus medicamentos actuales.";
        }

        private string ExtractConversationStarterFromResponse(string response)
        {
            if (response.Length > 30)
            {
                var firstSentence = response.Split('.')[0].Trim() + ".";
                return firstSentence.Length > 150 ? firstSentence.Substring(0, 150) + "..." : firstSentence;
            }

            return "Buenos días doctor, he estado registrando mis síntomas y tengo algunas preguntas preparadas.";
        }

        private List<ChecklistItem> ExtractChecklistItemsFromResponse(string response)
        {
            var items = new List<ChecklistItem>();
            var lines = ExtractListFromResponse(response);

            foreach (var line in lines)
            {
                items.Add(new ChecklistItem
                {
                    Description = line,
                    IsCompleted = false,
                    IsImportant = line.ToLower().Contains("importante") || 
                                 line.ToLower().Contains("esencial") ||
                                 line.ToLower().Contains("documento") ||
                                 line.ToLower().Contains("identificación")
                });
            }

            // Asegurar que haya al menos algunos items
            if (!items.Any())
            {
                items = GenerateFallbackChecklist("General");
            }

            return items;
        }

        private async Task<AppointmentPreparation> GenerateFallbackPreparation(MedicalAppointment appointment, List<string> symptoms)
        {
            return new AppointmentPreparation
            {
                QuestionsForDoctor = await GenerateQuestionsForDoctorAsync(appointment.Specialty ?? "General", symptoms),
                DocumentsToBring = new List<string>
                {
                    "Identificación oficial",
                    "Tarjeta de seguro médico",
                    "Historial médico reciente",
                    "Lista de medicamentos actuales",
                    "Resultados de exámenes recientes"
                },
                PreparationSteps = new List<string>
                {
                    "Llegar 15 minutos antes",
                    "Tener lista la información de síntomas",
                    "Preparar preguntas específicas",
                    "Revisar historial médico",
                    "Anotar cualquier alergia"
                },
                SpecialInstructions = "Lleve todos sus documentos médicos y una lista de sus medicamentos actuales.",
                ConversationStarter = await GenerateConversationStarterAsync(appointment.DoctorName ?? "el doctor", appointment.Specialty ?? "General")
            };
        }

        private List<string> GenerateFallbackQuestions(string specialty)
        {
            var baseQuestions = new List<string>
            {
                "¿Cuál es el diagnóstico probable?",
                "¿Qué opciones de tratamiento existen?",
                "¿Hay cambios en el estilo de vida que deba hacer?",
                "¿Cuándo debo seguir para control?",
                "¿Hay algún efecto secundario que deba observar?"
            };

            // Preguntas específicas por especialidad
            var specialtyQuestions = specialty.ToLower() switch
            {
                "cardiología" or "cardiology" => new List<string>
                {
                    "¿Cómo está funcionando mi corazón?",
                    "¿Necesito algún examen cardíaco específico?",
                    "¿Qué nivel de actividad física es seguro?",
                    "¿Debo controlar mi presión arterial en casa?",
                    "¿Hay señales de alerta que deba observar?"
                },
                "dermatología" or "dermatology" => new List<string>
                {
                    "¿Qué está causando esta condición de la piel?",
                    "¿Cómo debo aplicar los tratamientos tópicos?",
                    "¿Debo evitar algún producto o alimento?",
                    "¿Cuánto tiempo tomará ver mejorías?",
                    "¿Es esta condición contagiosa?"
                },
                _ => baseQuestions
            };

            return specialtyQuestions.Take(5).ToList();
        }

        private List<ChecklistItem> GenerateFallbackChecklist(string appointmentType)
        {
            var baseItems = new List<ChecklistItem>
            {
                new ChecklistItem { Description = "Identificación oficial", IsImportant = true },
                new ChecklistItem { Description = "Tarjeta de seguro médico", IsImportant = true },
                new ChecklistItem { Description = "Lista de medicamentos actuales", IsImportant = true },
                new ChecklistItem { Description = "Historial médico relevante", IsImportant = false },
                new ChecklistItem { Description = "Lista de preguntas preparadas", IsImportant = false }
            };

            // Items específicos por tipo de cita
            var specificItems = appointmentType.ToLower() switch
            {
                var t when t.Contains("cirugía") || t.Contains("surgery") => new List<ChecklistItem>
                {
                    new ChecklistItem { Description = "Resultados de exámenes pre-quirúrgicos", IsImportant = true },
                    new ChecklistItem { Description = "Instrucciones de ayuno", IsImportant = true },
                    new ChecklistItem { Description = "Ropa cómoda para después", IsImportant = false }
                },
                var t when t.Contains("análisis") || t.Contains("lab") => new List<ChecklistItem>
                {
                    new ChecklistItem { Description = "Instrucciones de ayuno (si aplica)", IsImportant = true },
                    new ChecklistItem { Description = "Hidratarse bien antes", IsImportant = false }
                },
                _ => new List<ChecklistItem>()
            };

            return baseItems.Concat(specificItems).Take(8).ToList();
        }
    }

    public class OllamaResponse
    {
        public string Model { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public bool Done { get; set; }
    }
}