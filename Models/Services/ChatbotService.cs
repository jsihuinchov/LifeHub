using System.Text;
using System.Text.Json;
using LifeHub.Data;
using LifeHub.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Models.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatbotService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IAppointmentChatbotService _appointmentService;

        // Estados de conversación
        private readonly Dictionary<string, ConversationState> _conversationStates = new();

        public ChatbotService(
            ApplicationDbContext context,
            ILogger<ChatbotService> logger,
            HttpClient httpClient,
            IAppointmentChatbotService appointmentService)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _appointmentService = appointmentService;
        }

        public async Task<ChatResponse> ProcessMessageAsync(string userId, string userMessage)
        {
            try
            {
                _logger.LogInformation("🔍 Mensaje recibido: {Message}", userMessage);

                // Guardar mensaje del usuario
                await SaveMessageAsync(userId, userMessage, true);

                // Obtener estado de conversación
                var state = GetConversationState(userId);
                _logger.LogInformation("🔍 Estado actual: AppointmentId={AppointmentId}, Step={Step}", 
                    state.CurrentAppointmentId, state.Step);

                ChatResponse response;

                // 🔥 PRIMERO: Si estamos en medio de preparación de cita, continuar ese flujo
                if (state.CurrentAppointmentId.HasValue && state.Step > 0)
                {
                    _logger.LogInformation("🔍 Continuando preparación de cita");
                    response = await ContinueAppointmentPreparationAsync(userId, userMessage, state);
                }
                // 🔥 SEGUNDO: Si el usuario quiere específicamente preparar una cita
                else if (ShouldStartAppointmentPreparation(userMessage))
                {
                    _logger.LogInformation("🔍 Iniciando preparación de cita por solicitud explícita");
                    response = await HandleAppointmentIntent(userId, userMessage);
                }
                // 🔥 TERCERO: CONVERSACIÓN ABIERTA CON IA - EL USUARIO PUEDE DECIR CUALQUIER COSA
                else
                {
                    _logger.LogInformation("🔍 Procesando conversación abierta con IA");
                    response = await HandleOpenConversationAsync(userId, userMessage, state);
                }

                _logger.LogInformation("🔍 Respuesta generada: {Response}", response.Message);

                // Guardar respuesta del bot
                await SaveMessageAsync(userId, response.Message, false, 
                    JsonSerializer.Serialize(response.QuickActions));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing chatbot message");
                
                return new ChatResponse
                {
                    Message = "¡Hola! Soy tu asistente de salud personal. Parece que hubo un error técnico, pero estoy aquí para ayudarte. ¿En qué puedo asistirte hoy?",
                    QuickActions = GetGeneralQuickActions()
                };
            }
        }

        // 🔥 CONVERSACIÓN ABIERTA - EL USUARIO PUEDE DECIR CUALQUIER COSA
        private async Task<ChatResponse> HandleOpenConversationAsync(string userId, string userMessage, ConversationState state)
        {
            try
            {
                // 🔥 OBTENER CONTEXTO DEL USUARIO DE FORMA ASÍNCRONA
                var userContextTask = GetUserContextAsync(userId);
                
                // 🔥 CONSTRUIR PROMPT MEJORADO
                var prompt = $@"
Eres LifeHub, un asistente de salud virtual inteligente y empático. Tu propósito es ayudar a los usuarios con cualquier pregunta o inquietud relacionada con la salud.

CONTEXTO DEL USUARIO:
{await userContextTask}

MENSAJE DEL USUARIO:
""{userMessage}""

INSTRUCCIONES ESPECÍFICAS:
1. Responde de manera natural, conversacional y útil
2. Si es una pregunta médica, proporciona información general (no diagnósticos)
3. Si necesitas más contexto, pregunta amablemente
4. Mantén un tono cálido, profesional y alentador
5. Si es relevante, sugiere recursos o próximos pasos
6. Sé conciso pero completo (150-300 palabras)
7. Usa emojis apropiados para hacer la conversación más amigable
8. Responde en español

RESPUESTA AMABLE Y ÚTIL:
";

                _logger.LogInformation("💬 [Conversación Abierta] Procesando mensaje con IA...");
                
                // 🔥 USAR TIMEOUT MÁS LARGO PARA CONVERSACIÓN ABIERTA
                var aiResponse = await GenerateWithOllama(prompt, 60000); // 60 segundos para conversación abierta
                
                _logger.LogInformation("✅ [Conversación Abierta] Respuesta generada exitosamente");

                // 🔥 ANÁLISIS MÁS INTELIGENTE DE ACCIONES SUGERIDAS
                var suggestedActions = await AnalyzeSuggestedActionsAsync(aiResponse, userMessage, userId);
                
                return new ChatResponse
                {
                    Message = aiResponse,
                    QuickActions = suggestedActions.Any() ? suggestedActions : GetGeneralQuickActions()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Conversación Abierta] Error en conversación con IA");
                
                // 🔥 FALLBACK MEJORADO
                return new ChatResponse
                {
                    Message = "¡Hola! 👋 Veo que tienes una pregunta sobre salud. En este momento estoy teniendo dificultades técnicas, pero aquí hay algunas formas en que puedo ayudarte:\n\n" +
                             "• 🩺 **Preguntas generales de salud**\n" +
                             "• 💊 **Información sobre medicamentos**\n" +
                             "• 📅 **Preparación de citas médicas**\n" +
                             "• 🥗 **Consejos de estilo de vida saludable**\n\n" +
                             "¿Podrías intentar formular tu pregunta de otra manera o contarme más sobre lo que necesitas?",
                    QuickActions = GetGeneralQuickActions()
                };
            }
        }

        // 🔥 OBTENER CONTEXTO PERSONALIZADO DEL USUARIO
        private async Task<string> GetUserContextAsync(string userId)
        {
            try
            {
                var contextParts = new List<string>();

                // Obtener citas próximas
                var upcomingAppointments = await _context.MedicalAppointments
                    .Where(a => a.UserId == userId && a.AppointmentDate >= DateTime.UtcNow)
                    .OrderBy(a => a.AppointmentDate)
                    .Take(2)
                    .ToListAsync();

                if (upcomingAppointments.Any())
                {
                    var appointmentsText = string.Join(", ", upcomingAppointments.Select(a => 
                        $"{a.Title} con Dr. {a.DoctorName} el {a.AppointmentDate:dd/MM/yyyy}"));
                    contextParts.Add($"Citas próximas: {appointmentsText}");
                }
                else
                {
                    contextParts.Add("No tienes citas próximas programadas");
                }

                // Obtener medicamentos activos
                var activeMeds = await _context.Medications
                    .Where(m => m.UserId == userId && m.IsActive)
                    .ToListAsync();

                if (activeMeds.Any())
                {
                    var medsText = string.Join(", ", activeMeds.Select(m => m.Name));
                    contextParts.Add($"Medicamentos activos: {medsText}");
                }
                else
                {
                    contextParts.Add("No tienes medicamentos activos registrados");
                }

                // Obtener registro de salud reciente
                var recentCheck = await _context.WellnessChecks
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.CheckDate)
                    .FirstOrDefaultAsync();

                if (recentCheck != null)
                {
                    var daysAgo = (DateTime.UtcNow - recentCheck.CheckDate).Days;
                    var daysText = daysAgo == 0 ? "Hoy" : 
                                  daysAgo == 1 ? "Ayer" : 
                                  $"Hace {daysAgo} días";
                    contextParts.Add($"Último registro de salud: {recentCheck.GeneralWellness} ({daysText})");
                }
                else
                {
                    contextParts.Add("No hay registros de salud recientes");
                }

                return contextParts.Any() ? string.Join("; ", contextParts) : "Usuario nuevo sin historial registrado";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo contexto del usuario");
                return "Contexto del usuario no disponible";
            }
        }

        // 🔥 ANÁLISIS MÁS INTELIGENTE DE ACCIONES SUGERIDAS
        private async Task<List<QuickAction>> AnalyzeSuggestedActionsAsync(string aiResponse, string userMessage, string userId)
        {
            try
            {
                var actions = new List<QuickAction>();
                var lowerResponse = aiResponse.ToLower();
                var lowerMessage = userMessage.ToLower();

                // 🔥 ANÁLISIS MÁS DETALLADO BASADO EN EL CONTENIDO
                if (lowerResponse.Contains("cita") || lowerResponse.Contains("doctor") || lowerMessage.Contains("cita"))
                {
                    actions.Add(new QuickAction { Text = "📅 Preparar cita", Action = "start_appointment" });
                    actions.Add(new QuickAction { Text = "📝 Preguntas para doctor", Action = "prepare_questions" });
                }

                if (lowerResponse.Contains("medicamento") || lowerResponse.Contains("pastilla") || lowerMessage.Contains("medicamento"))
                {
                    actions.Add(new QuickAction { Text = "💊 Mis medicamentos", Action = "view_medications" });
                    actions.Add(new QuickAction { Text = "🔍 Buscar medicamento", Action = "search_medications" });
                }

                if (lowerResponse.Contains("síntoma") || lowerResponse.Contains("dolor") || lowerResponse.Contains("malestar"))
                {
                    actions.Add(new QuickAction { Text = "📝 Registrar síntomas", Action = "record_symptoms" });
                    actions.Add(new QuickAction { Text = "📊 Ver mi historial", Action = "view_health_history" });
                }

                // 🔥 NUEVAS CATEGORÍAS DE SALUD
                if (lowerResponse.Contains("comida") || lowerResponse.Contains("dieta") || lowerResponse.Contains("alimentación") || lowerResponse.Contains("nutrición"))
                {
                    actions.Add(new QuickAction { Text = "🥗 Consejos nutrición", Action = "nutrition_tips" });
                }

                if (lowerResponse.Contains("ejercicio") || lowerResponse.Contains("deporte") || lowerResponse.Contains("actividad física"))
                {
                    actions.Add(new QuickAction { Text = "🏃 Plan ejercicio", Action = "exercise_plan" });
                }

                if (lowerResponse.Contains("sueño") || lowerResponse.Contains("dormir") || lowerResponse.Contains("insomnio"))
                {
                    actions.Add(new QuickAction { Text = "💤 Mejorar sueño", Action = "sleep_improvement" });
                }

                if (lowerResponse.Contains("estrés") || lowerResponse.Contains("ansiedad") || lowerResponse.Contains("mental"))
                {
                    actions.Add(new QuickAction { Text = "😌 Salud mental", Action = "mental_health" });
                }

                // Si no hay suficientes acciones, agregar generales
                if (actions.Count < 2)
                {
                    actions.AddRange(GetGeneralQuickActions().Take(3));
                }

                return actions;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error analizando acciones sugeridas");
                return GetGeneralQuickActions();
            }
        }

        // 🔥 ACCIONES RÁPIDAS GENERALES
        private List<QuickAction> GetGeneralQuickActions()
        {
            return new List<QuickAction>
            {
                new() { Text = "📅 Preparar cita", Action = "start_appointment" },
                new() { Text = "💊 Mis medicamentos", Action = "view_medications" },
                new() { Text = "📝 Registrar salud", Action = "record_health" },
                new() { Text = "📊 Ver análisis", Action = "view_analysis" },
                new() { Text = "🩺 Preguntas salud", Action = "health_questions" }
            };
        }

        // 🔥 DETECCIÓN MEJORADA DE INICIO DE PREPARACIÓN DE CITA
        private bool ShouldStartAppointmentPreparation(string userMessage)
        {
            var lowerMessage = userMessage.ToLower();
            var appointmentKeywords = new[]
            {
                "preparar cita", "prepara mi cita", "prapara mi cita", "preparame cita",
                "cita médica", "cita con el doctor", "cita con doctor", "quiero preparar cita",
                "necesito preparar cita", "ayuda con cita", "preparación cita"
            };

            return appointmentKeywords.Any(keyword => lowerMessage.Contains(keyword));
        }

        public async Task<ChatResponse> StartAppointmentPreparationAsync(int appointmentId)
        {
            var appointment = await _context.MedicalAppointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return new ChatResponse
                {
                    Message = "No encuentro esa cita médica. ¿Podrías verificar el ID?",
                    QuickActions = new List<QuickAction>
                    {
                        new() { Text = "Ver mis citas", Action = "view_appointments" }
                    }
                };
            }

            var state = GetConversationState(appointment.UserId);
            state.CurrentAppointmentId = appointmentId;
            state.Step = 1;
            state.AppointmentData = new Dictionary<string, string>();
            state.CurrentTopic = "preparacion_cita";

            var message = @"¡Perfecto! Vamos a preparar tu cita médica.

¿Qué te gustaría preparar primero? Puedo ayudarte con:

📝 **Preguntas para el doctor**
📋 **Documentos necesarios**  
🎯 **Preparación específica**
💬 **Practicar la conversación**

¿Por dónde quieres empezar?";

            return new ChatResponse
            {
                Message = message,
                QuickActions = new List<QuickAction>
                {
                    new() { Text = "📝 Preguntas para el doctor", Action = "prepare_questions" },
                    new() { Text = "📋 Documentos a llevar", Action = "prepare_documents" },
                    new() { Text = "🎯 Preparación general", Action = "prepare_general" },
                    new() { Text = "💬 Practicar conversación", Action = "practice_conversation" }
                }
            };
        }

        private async Task<ChatResponse> ContinueAppointmentPreparationAsync(string userId, string userMessage, ConversationState state)
        {
            // Si el usuario quiere salir del modo preparación
            if (userMessage.ToLower().Contains("salir") || userMessage.ToLower().Contains("volver") || 
                userMessage.ToLower().Contains("cancelar") || userMessage.ToLower().Contains("atrás"))
            {
                _conversationStates.Remove(userId);
                return new ChatResponse
                {
                    Message = "¡De acuerdo! Salimos del modo preparación de cita. ¿En qué más puedo ayudarte?",
                    QuickActions = GetGeneralQuickActions()
                };
            }

            var appointment = await _context.MedicalAppointments
                .FirstOrDefaultAsync(a => a.Id == state.CurrentAppointmentId);

            if (appointment == null)
            {
                _conversationStates.Remove(userId);
                return new ChatResponse
                {
                    Message = "Parece que la cita ya no existe. ¿Quieres preparar otra cita?",
                    QuickActions = new List<QuickAction>
                    {
                        new() { Text = "Ver mis citas", Action = "view_appointments" }
                    }
                };
            }

            // Detección simple de intenciones dentro de la preparación
            var lowerMessage = userMessage.ToLower();
            
            if (lowerMessage.Contains("pregunta") || lowerMessage.Contains("1") || state.CurrentTopic == "preguntas")
            {
                state.CurrentTopic = "preguntas";
                return await HandleQuestionsPreparation(userId, appointment);
            }
            
            if (lowerMessage.Contains("documento") || lowerMessage.Contains("2") || state.CurrentTopic == "documentos")
            {
                state.CurrentTopic = "documentos";
                return await HandleDocumentsPreparation(userId, appointment);
            }
            
            if (lowerMessage.Contains("preparacion") || lowerMessage.Contains("3") || state.CurrentTopic == "preparacion")
            {
                state.CurrentTopic = "preparacion";
                return await HandleGeneralPreparation(userId, appointment);
            }
            
            if (lowerMessage.Contains("conversacion") || lowerMessage.Contains("4") || state.CurrentTopic == "conversacion")
            {
                state.CurrentTopic = "conversacion";
                return await HandleConversationPractice(userId, appointment, userMessage);
            }
            
            if (lowerMessage.Contains("terminar") || lowerMessage.Contains("finish") || lowerMessage.Contains("listo"))
            {
                return await CompletePreparation(userId, appointment);
            }

            // Si no reconocemos el mensaje, usar IA para responder en contexto
            return await HandlePreparationContextResponse(userId, userMessage, appointment, state);
        }

        // 🔥 RESPUESTA CONTEXTUAL DENTRO DE LA PREPARACIÓN
        private async Task<ChatResponse> HandlePreparationContextResponse(string userId, string userMessage, MedicalAppointment appointment, ConversationState state)
        {
            var prompt = $@"
Estás en medio de preparar una cita médica con Dr. {appointment.DoctorName} para {appointment.Specialty}.

CONTEXTO ACTUAL:
- Estamos en el paso: {state.CurrentTopic}
- Cita: {appointment.Title}
- Fecha: {appointment.AppointmentDate:dd/MM/yyyy}

MENSAJE DEL USUARIO:
""{userMessage}""

RESPONDE:
1. Si es una pregunta sobre la preparación de la cita, responde específicamente
2. Si quiere cambiar de tema dentro de la preparación, sugiere las opciones
3. Si quiere salir de la preparación, confirma amablemente
4. Mantén el foco en la preparación de la cita
5. Responde en español

RESPUESTA:";

            var response = await GenerateWithOllama(prompt, 45000);
            
            return new ChatResponse
            {
                Message = response,
                QuickActions = new List<QuickAction>
                {
                    new() { Text = "📝 Preguntas", Action = "prepare_questions" },
                    new() { Text = "📋 Documentos", Action = "prepare_documents" },
                    new() { Text = "💬 Conversación", Action = "practice_conversation" },
                    new() { Text = "✅ Terminar", Action = "finish_preparation" },
                    new() { Text = "🚪 Salir", Action = "exit_preparation" }
                }
            };
        }

        private async Task<ChatResponse> HandleAppointmentIntent(string userId, string userMessage)
        {
            try
            {
                var appointments = await _context.MedicalAppointments
                    .Where(a => a.UserId == userId && a.AppointmentDate >= DateTime.UtcNow)
                    .OrderBy(a => a.AppointmentDate)
                    .Take(3)
                    .ToListAsync();

                if (appointments.Any())
                {
                    var message = "**📅 Encontré tus próximas citas:**\n\n" +
                        string.Join("\n", appointments.Select((a, index) => 
                            $"**{index + 1}.** **{a.Title}** con Dr. {a.DoctorName}\n  📅 {a.AppointmentDate:dd/MM/yyyy} ⏰ {a.AppointmentDate:HH:mm}"));

                    message += "\n\n¿Qué cita te gustaría preparar? (Responde con el número o di 'la primera', 'la segunda', etc.)";

                    var quickActions = appointments.Select((a, index) => new QuickAction
                    {
                        Text = $"{index + 1}️⃣ {a.Title}",
                        Action = $"prepare_appointment:{a.Id}"
                    }).ToList();

                    quickActions.Add(new QuickAction { Text = "📋 Ver todas las citas", Action = "view_appointments" });
                    quickActions.Add(new QuickAction { Text = "🏠 Volver al inicio", Action = "go_home" });

                    return new ChatResponse
                    {
                        Message = message,
                        QuickActions = quickActions
                    };
                }
                else
                {
                    return new ChatResponse
                    {
                        Message = "No tienes citas próximas programadas. ¿Te gustaría crear una nueva cita médica?",
                        QuickActions = new List<QuickAction>
                        {
                            new() { Text = "➕ Crear nueva cita", Action = "create_appointment" },
                            new() { Text = "📅 Ver citas pasadas", Action = "view_past_appointments" },
                            new() { Text = "🏠 Volver al inicio", Action = "go_home" }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo citas del usuario");
                return await HandleOpenConversationAsync(userId, userMessage, GetConversationState(userId));
            }
        }

        // Métodos de preparación de citas (preguntas, documentos, etc.)
        private async Task<ChatResponse> HandleQuestionsPreparation(string userId, MedicalAppointment appointment)
        {
            var recentWellness = await _context.WellnessChecks
                .Where(w => w.UserId == userId && w.CheckDate >= DateTime.UtcNow.AddDays(-30))
                .ToListAsync();

            var symptoms = recentWellness
                .SelectMany(w => w.GetSymptomsList())
                .Distinct()
                .Select(s => s.ToString())
                .ToList();

            var questions = await _appointmentService.GenerateQuestionsForDoctorAsync(
                appointment.Specialty ?? "General", symptoms);

            var message = $"**📝 Preguntas para Dr. {appointment.DoctorName}**\n\nBasándome en tu historial reciente, te sugiero estas preguntas:\n\n{string.Join("\n\n", questions.Select((q, i) => $"{i + 1}. {q}"))}\n\n¿Te gustaría:\n- 🔄 **Generar preguntas diferentes**\n- ✏️ **Modificar alguna pregunta**  \n- ✅ **Continuar con documentos**\n- 💬 **Practicar la conversación**";

            return new ChatResponse
            {
                Message = message,
                QuickActions = new List<QuickAction>
                {
                    new() { Text = "🔄 Generar otras preguntas", Action = "regenerate_questions" },
                    new() { Text = "📋 Ver documentos", Action = "prepare_documents" },
                    new() { Text = "💬 Practicar conversación", Action = "practice_conversation" },
                    new() { Text = "✅ Terminar preparación", Action = "finish_preparation" }
                }
            };
        }

        private async Task<ChatResponse> HandleDocumentsPreparation(string userId, MedicalAppointment appointment)
        {
            var preparation = await _appointmentService.PrepareForAppointmentAsync(appointment.Id);

            var message = $"**📋 Documentos para tu cita de {appointment.Specialty}**\n\nDocumentos esenciales:\n{string.Join("\n", preparation.DocumentsToBring.Select(d => $"• {d}"))}\n\n¿Necesitas ayuda con algo específico sobre los documentos?";

            return new ChatResponse
            {
                Message = message,
                QuickActions = new List<QuickAction>
                {
                    new() { Text = "📝 Ver preguntas", Action = "prepare_questions" },
                    new() { Text = "🎯 Preparación general", Action = "prepare_general" },
                    new() { Text = "💬 Conversación", Action = "practice_conversation" }
                }
            };
        }

        private async Task<ChatResponse> HandleGeneralPreparation(string userId, MedicalAppointment appointment)
        {
            var preparation = await _appointmentService.PrepareForAppointmentAsync(appointment.Id);

            var message = $"**🎯 Preparación para {appointment.Title}**\n\nPasos recomendados:\n{string.Join("\n", preparation.PreparationSteps.Select((s, i) => $"{i + 1}. {s}"))}\n\nInstrucciones especiales:\n{preparation.SpecialInstructions}\n\n¿En qué más puedo ayudarte?";

            return new ChatResponse
            {
                Message = message,
                QuickActions = new List<QuickAction>
                {
                    new() { Text = "📝 Preguntas", Action = "prepare_questions" },
                    new() { Text = "📋 Documentos", Action = "prepare_documents" },
                    new() { Text = "💬 Practicar", Action = "practice_conversation" }
                }
            };
        }

        private async Task<ChatResponse> HandleConversationPractice(string userId, MedicalAppointment appointment, string userMessage)
        {
            var conversationStarter = await _appointmentService.GenerateConversationStarterAsync(
                appointment.DoctorName ?? "el doctor", appointment.Specialty ?? "General");

            if (userMessage.ToLower().Contains("practicar") || userMessage.ToLower().Contains("conversación"))
            {
                return new ChatResponse
                {
                    Message = $"**💬 Practiquemos la conversación**\n\nPuedes empezar diciendo:\n*\"{conversationStarter}\"*\n\n¿Cómo te gustaría que responda el doctor? O dime qué quieres practicar específicamente.",
                    QuickActions = new List<QuickAction>
                    {
                        new() { Text = "📝 Volver a preguntas", Action = "prepare_questions" },
                        new() { Text = "🎯 Otro tema", Action = "change_topic" }
                    }
                };
            }

            var doctorResponse = await GenerateWithOllama($"Eres un doctor de {appointment.Specialty} respondiendo a un paciente. Responde de manera profesional pero amable, haciendo 1-2 preguntas de seguimiento. Mantén la conversación fluida y natural.");

            return new ChatResponse
            {
                Message = $"**Dr. {appointment.DoctorName}:** {doctorResponse}\n\n¿Cómo quieres continuar la conversación?",
                QuickActions = new List<QuickAction>
                {
                    new() { Text = "📝 Cambiar de tema", Action = "prepare_questions" },
                    new() { Text = "🔄 Empezar de nuevo", Action = "practice_conversation" }
                }
            };
        }

        private async Task<ChatResponse> CompletePreparation(string userId, MedicalAppointment appointment)
        {
            var preparation = await _appointmentService.PrepareForAppointmentAsync(appointment.Id);

            // Limpiar estado de conversación
            _conversationStates.Remove(userId);

            var message = $"**✅ Preparación completada**\n\n¡Excelente! Estás listo para tu cita con Dr. {appointment.DoctorName}.\n\n**Resumen:**\n• 📝 Tienes preguntas preparadas\n• 📋 Sabes qué documentos llevar  \n• 🎯 Conoces los pasos de preparación\n• 💬 Has practicado la conversación\n\n¡Mucha suerte en tu cita! 🍀";

            return new ChatResponse
            {
                Message = message,
                IsComplete = true,
                Preparation = preparation,
                QuickActions = GetGeneralQuickActions()
            };
        }

        // 🔥 MÉTODO FALTANTE: GetIntentFallback
        private string GetIntentFallback(string message)
        {
            var lowerMessage = message.ToLower();
            
            // 🔍 DETECCIÓN ESPECÍFICA PARA SELECCIÓN DE CITAS
            if (lowerMessage.Contains("1") || lowerMessage.Contains("primera") || lowerMessage.Contains("1ra") || 
                lowerMessage.Contains("primero") || lowerMessage.Contains("una") || lowerMessage.Contains("esa") ||
                lowerMessage.Contains("esa cita") || lowerMessage.Contains("esa misma") || lowerMessage.Contains("la cita"))
                return "seleccionar_cita";
                
            if (lowerMessage.Contains("2") || lowerMessage.Contains("segunda") || lowerMessage.Contains("2da"))
                return "seleccionar_cita";
                
            if (lowerMessage.Contains("3") || lowerMessage.Contains("tercera") || lowerMessage.Contains("3ra"))
                return "seleccionar_cita";
            
            if (lowerMessage.Contains("cita") || lowerMessage.Contains("doctor") || lowerMessage.Contains("médico") || 
                lowerMessage.Contains("consulta") || lowerMessage.Contains("preparar cita") ||
                lowerMessage.Contains("prepara mi cita") || lowerMessage.Contains("preparame") || lowerMessage.Contains("prapara"))
                return "cita";
                
            if (lowerMessage.Contains("pregunta") || lowerMessage.Contains("preguntar") || 
                lowerMessage.Contains("qué preguntar") || lowerMessage.Contains("qué decir") || lowerMessage.Contains("prepare_questions"))
                return "preguntas";
                
            if (lowerMessage.Contains("documento") || lowerMessage.Contains("llevar") || 
                lowerMessage.Contains("papel") || lowerMessage.Contains("papeles") || lowerMessage.Contains("prepare_documents"))
                return "documentos";
                
            if (lowerMessage.Contains("medicamento") || lowerMessage.Contains("pastilla") || 
                lowerMessage.Contains("tratamiento") || lowerMessage.Contains("medicina"))
                return "medicamentos";
                
            if (lowerMessage.Contains("síntoma") || lowerMessage.Contains("dolor") || 
                lowerMessage.Contains("malestar") || lowerMessage.Contains("enfermo"))
                return "síntomas";
                
            if (lowerMessage.Contains("hola") || lowerMessage.Contains("buenos") || 
                lowerMessage.Contains("buenas") || lowerMessage.Contains("saludos"))
                return "saludo";

            if (lowerMessage.Contains("comida") || lowerMessage.Contains("dieta") || lowerMessage.Contains("alimentación") || lowerMessage.Contains("nutrición"))
                return "nutricion";

            if (lowerMessage.Contains("ejercicio") || lowerMessage.Contains("deporte") || lowerMessage.Contains("actividad física"))
                return "ejercicio";

            if (lowerMessage.Contains("sueño") || lowerMessage.Contains("dormir") || lowerMessage.Contains("insomnio"))
                return "sueno";

            if (lowerMessage.Contains("estrés") || lowerMessage.Contains("ansiedad") || lowerMessage.Contains("mental"))
                return "estres";
                
            return "ayuda";
        }

        // 🔥 MÉTODO MEJORADO PARA GENERAR CON OLLAMA
        private async Task<string> GenerateWithOllama(string prompt, int timeoutMs = 45000)
        {
            try
            {
                _logger.LogInformation("🤖 [Ollama] Enviando prompt de {PromptLength} caracteres", prompt.Length);
                
                var requestData = new
                {
                    model = "llama2",
                    prompt = prompt,
                    stream = false,
                    options = new 
                    {
                        temperature = 0.7,
                        top_p = 0.9,
                        top_k = 40
                    }
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("🤖 [Ollama] Timeout configurado: {TimeoutMs}ms", timeoutMs);
                
                using var timeoutCts = new CancellationTokenSource(timeoutMs);
                var response = await _httpClient.PostAsync("http://localhost:11434/api/generate", content, timeoutCts.Token);
                
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var ollamaResponse = JsonSerializer.Deserialize<OllamaApiResponse>(responseContent);

                var generatedResponse = ollamaResponse?.Response?.Trim();
                
                _logger.LogInformation("🤖 [Ollama] Respuesta recibida: {ResponseLength} caracteres", 
                    generatedResponse?.Length ?? 0);

                if (string.IsNullOrWhiteSpace(generatedResponse))
                {
                    _logger.LogWarning("🤖 [Ollama] Respuesta vacía recibida");
                    return "No pude generar una respuesta en este momento. Por favor, intenta reformular tu pregunta.";
                }

                return generatedResponse;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("⏰ [Ollama] Timeout después de {TimeoutMs}ms", timeoutMs);
                return "Estoy procesando tu pregunta. Por favor, espera un momento más o intenta con una pregunta más concisa.";
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "🌐 [Ollama] Error de conexión HTTP");
                return "No puedo conectarme al servicio en este momento. Por favor, verifica que Ollama esté ejecutándose en http://localhost:11434";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Ollama] Error inesperado");
                return "Ocurrió un error inesperado. Por favor, intenta de nuevo en un momento.";
            }
        }

        private ConversationState GetConversationState(string userId)
        {
            if (_conversationStates.ContainsKey(userId))
                return _conversationStates[userId];

            var newState = new ConversationState();
            _conversationStates[userId] = newState;
            return newState;
        }

        private async Task SaveMessageAsync(string userId, string message, bool isUser, string? quickActions = null)
        {
            // Implementación de guardado (puedes mantener la existente)
            var chatMessage = new ChatMessage
            {
                UserId = userId,
                Message = message,
                IsUser = isUser,
                Timestamp = DateTime.UtcNow,
                QuickActions = quickActions
            };

            // En una implementación real, guardarías en la base de datos
            // _context.ChatMessages.Add(chatMessage);
            // await _context.SaveChangesAsync();
        }

        public async Task<List<ChatMessage>> GetConversationHistoryAsync(string userId)
        {
            return new List<ChatMessage>();
        }

        public async Task ClearConversationHistoryAsync(string userId)
        {
            _conversationStates.Remove(userId);
        }
    }

    public class ConversationState
    {
        public int? CurrentAppointmentId { get; set; }
        public int Step { get; set; }
        public Dictionary<string, string> AppointmentData { get; set; } = new();
        public string CurrentTopic { get; set; } = string.Empty;
        public List<string> RecentMessages { get; set; } = new();
    }

    public class OllamaApiResponse
    {
        public string Model { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public bool Done { get; set; }
    }
}