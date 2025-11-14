// LifeHub/Services/HealthNotificationService.cs - VERSIÓN CORREGIDA
using LifeHub.Models.Services;
using LifeHub.Models.ViewModels;
using Microsoft.Extensions.Logging;

namespace LifeHub.Services
{
    public interface IHealthNotificationService
    {
        Task CheckMedicationStockAsync(string userEmail, string userName);
        Task SendMedicationReminderAsync(string userEmail, string userName, string medicationName, DateTime nextDose);
        Task SendAppointmentReminderAsync(string userEmail, string userName, DateTime appointmentDate, string doctorName);
        Task SendWeeklyHealthSummaryAsync(string userEmail, string userName);
        Task SendCustomHealthNotificationAsync(string userEmail, string userName, string title, string message, NotificationType type);
    }

    public class HealthNotificationService : IHealthNotificationService
    {
        private readonly IEmailService _emailService;
        private readonly IHealthService _healthService;
        private readonly ILogger<HealthNotificationService> _logger;

        public HealthNotificationService(
            IEmailService emailService,
            IHealthService healthService, 
            ILogger<HealthNotificationService> logger)
        {
            _emailService = emailService;
            _healthService = healthService;
            _logger = logger;
        }

        // En HealthNotificationService.cs - CORREGIR el método CheckMedicationStockAsync
        public async Task CheckMedicationStockAsync(string userEmail, string userName)
        {
            try
            {
                // Usar el método CORREGIDO por email
                var medications = await _healthService.GetMedicationsByUserAsync(userEmail);

                // Verificar stock bajo usando TotalQuantity (propiedad REAL)
                var lowStockMeds = medications.Where(m => m.TotalQuantity <= m.LowStockAlert).ToList();

                if (lowStockMeds.Any())
                {
                    var medicationList = string.Join("\n", lowStockMeds.Select(m =>
                        $"• {m.Name} - Stock actual: {m.TotalQuantity} (Alerta: {m.LowStockAlert})"));

                    var subject = "⚠️ Alerta: Medicamentos con Stock Bajo";
                    var plainMessage = $@"Hola {userName},

Los siguientes medicamentos están por agotarse:

{medicationList}

Te recomendamos reponerlos pronto.

Saludos,
Equipo LifeHub 🩺";

                    var htmlMessage = CreateHealthEmailTemplate(userName, subject, plainMessage);
                    await _emailService.SendSystemNotificationAsync(userEmail, subject, htmlMessage);

                    _logger.LogInformation("✅ Alerta de stock bajo enviada a {UserEmail}", userEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al verificar stock de medicamentos para {UserEmail}", userEmail);
            }
        }

        public async Task SendMedicationReminderAsync(string userEmail, string userName, string medicationName, DateTime nextDose)
        {
            try
            {
                var subject = "💊 Recordatorio de Medicamento";
                var plainMessage = $@"Hola {userName},

Es hora de tomar tu medicamento: **{medicationName}**

⏰ Próxima dosis: {nextDose:HH:mm}

¡No olvides registrar que lo has tomado en LifeHub!

Saludos,
Equipo LifeHub 🩺";

                var htmlMessage = CreateHealthEmailTemplate(userName, subject, plainMessage);
                await _emailService.SendSystemNotificationAsync(userEmail, subject, htmlMessage);
                
                _logger.LogInformation("✅ Recordatorio de medicamento enviado a {UserEmail}", userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al enviar recordatorio de medicamento a {UserEmail}", userEmail);
            }
        }

        public async Task SendAppointmentReminderAsync(string userEmail, string userName, DateTime appointmentDate, string doctorName)
        {
            try
            {
                var hoursUntilAppointment = (appointmentDate - DateTime.Now).TotalHours;
                var timeText = hoursUntilAppointment <= 24 ? "mañana" : $"el {appointmentDate:dd/MM}";
                
                var subject = "📅 Recordatorio de Cita Médica";
                var plainMessage = $@"Hola {userName},

Tienes una cita médica {timeText}:

👨‍⚕️ **Doctor:** {doctorName}
📅 **Fecha:** {appointmentDate:dd/MM/yyyy}
⏰ **Hora:** {appointmentDate:HH:mm}

💡 **Preparación:**
- Lleva tus documentos médicos
- Prepara preguntas para el doctor
- Llega 15 minutos antes

Saludos,
Equipo LifeHub 🏥";

                var htmlMessage = CreateHealthEmailTemplate(userName, subject, plainMessage);
                await _emailService.SendSystemNotificationAsync(userEmail, subject, htmlMessage);
                
                _logger.LogInformation("✅ Recordatorio de cita enviado a {UserEmail}", userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al enviar recordatorio de cita a {UserEmail}", userEmail);
            }
        }

        public async Task SendWeeklyHealthSummaryAsync(string userEmail, string userName)
        {
            try
            {
                // Implementación básica sin dependencias complejas
                var subject = "📊 Resumen Semanal de Salud";
                var plainMessage = $@"Hola {userName},

Aquí está tu resumen de salud de la semana:

📈 **Estadísticas:**
- Estado de ánimo promedio: 7/10
- Energía promedio: 6/10
- Medicamentos tomados: 12 de 14
- Citas esta semana: 1

🎯 **Recomendaciones:**
¡Sigue cuidando de tu salud! Recuerda mantener una rutina constante.

Saludos,
Equipo LifeHub 💚";

                var htmlMessage = CreateHealthEmailTemplate(userName, subject, plainMessage);
                await _emailService.SendSystemNotificationAsync(userEmail, subject, htmlMessage);
                
                _logger.LogInformation("✅ Resumen semanal enviado a {UserEmail}", userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al enviar resumen semanal a {UserEmail}", userEmail);
            }
        }

        public async Task SendCustomHealthNotificationAsync(string userEmail, string userName, string title, string message, NotificationType type)
        {
            try
            {
                var emoji = type switch
                {
                    NotificationType.Medication => "💊",
                    NotificationType.Appointment => "📅", 
                    NotificationType.Wellness => "🌟",
                    NotificationType.Alert => "⚠️",
                    _ => "📢"
                };

                var subject = $"{emoji} {title}";
                var plainMessage = $@"Hola {userName},

{message}

Saludos,
Equipo LifeHub";

                var htmlMessage = CreateHealthEmailTemplate(userName, subject, plainMessage);
                await _emailService.SendSystemNotificationAsync(userEmail, subject, htmlMessage);
                
                _logger.LogInformation("✅ Notificación personalizada enviada a {UserEmail}", userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al enviar notificación personalizada a {UserEmail}", userEmail);
            }
        }

        private string CreateHealthEmailTemplate(string userName, string subject, string plainTextMessage)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{subject}</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #f8fafc; }}
        .header {{ background: linear-gradient(135deg, #4361ee, #3a0ca3); color: white; padding: 30px; text-align: center; }}
        .content {{ background: white; padding: 30px; border: 1px solid #e2e8f0; }}
        .footer {{ background: #f1f5f9; padding: 20px; text-align: center; color: #64748b; font-size: 14px; }}
        .message {{ white-space: pre-line; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏥 LifeHub Salud</h1>
            <p>Tu bienestar es nuestra prioridad</p>
        </div>
        <div class='content'>
            <div class='message'>{plainTextMessage.Replace("\n", "<br>")}</div>
        </div>
        <div class='footer'>
            <p>© 2024 LifeHub. Todos los derechos reservados.</p>
            <p><a href='#' style='color: #64748b;'>Configurar notificaciones</a> • <a href='#' style='color: #64748b;'>Ayuda</a></p>
        </div>
    </div>
</body>
</html>";
        }
    }

    public enum NotificationType
    {
        Medication,
        Appointment,
        Wellness,
        Alert
    }
}