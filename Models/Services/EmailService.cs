using LifeHub.Models.ViewModels;
using LifeHub.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace LifeHub.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly ApplicationDbContext _context;

        public EmailService(
            IConfiguration configuration, 
            ILogger<EmailService> logger,
            ApplicationDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        // Clase interna para manejar la configuración SMTP
        private class SmtpConfig
        {
            public string Host { get; set; } = string.Empty;
            public int Port { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public bool EnableSsl { get; set; }
        }

        private SmtpConfig GetSmtpConfig()
        {
            // Intentar cargar desde archivo .env si existe
            TryLoadEnvFile();

            // Prioridad: Variables de entorno > User Secrets > appsettings.json
            return new SmtpConfig
            {
                Host = Environment.GetEnvironmentVariable("SMTP_HOST")
               ?? _configuration["SmtpSettings:Host"]
               ?? "smtp.gmail.com",

                Port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT")
               ?? _configuration["SmtpSettings:Port"]
               ?? "587"),

                Username = Environment.GetEnvironmentVariable("SMTP_USERNAME")
                  ?? _configuration["SmtpSettings:Username"]
                  ?? throw new InvalidOperationException("SMTP username no configurado"),

                Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD")
                  ?? _configuration["SmtpSettings:Password"]
                  ?? throw new InvalidOperationException("SMTP password no configurado"),

                EnableSsl = bool.Parse(Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL")
                     ?? _configuration["SmtpSettings:EnableSsl"]
                     ?? "true")
            };
        }

        private void TryLoadEnvFile()
        {
            var envFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (!File.Exists(envFilePath)) return;

            foreach (var line in File.ReadAllLines(envFilePath))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }

        public async Task<bool> SendContactEmailAsync(ContactViewModel model)
        {
            try
            {
                var smtpConfig = GetSmtpConfig();

                using var client = new SmtpClient(smtpConfig.Host, smtpConfig.Port)
                {
                    EnableSsl = smtpConfig.EnableSsl,
                    Credentials = new NetworkCredential(smtpConfig.Username, smtpConfig.Password)
                };

                // 1. VERIFICAR SI ES USUARIO EXISTENTE (sin guardar en BD)
                var userExists = await UserExistsInDatabase(model.Email);
                var userInfo = userExists ? await GetUserSubscriptionInfo(model.Email) : string.Empty;

                // Email para el equipo de LifeHub
                var teamMessage = new MailMessage
                {
                    From = new MailAddress(smtpConfig.Username, "LifeHub Notificaciones"),
                    Subject = $"[{model.TipoConsulta}] {model.Asunto} - {(userExists ? "USUARIO EXISTENTE" : "NUEVO CONTACTO")}",
                    Body = BuildTeamEmailBody(model, userExists, userInfo),
                    IsBodyHtml = true
                };
                teamMessage.To.Add(GetTeamEmailByType(model.TipoConsulta));

                // Email de confirmación automática PARA EL USUARIO
                var userMessage = new MailMessage
                {
                    From = new MailAddress(smtpConfig.Username, "Equipo LifeHub"),
                    Subject = GetUserEmailSubject(model.TipoConsulta),
                    Body = await BuildUserAutomaticResponseAsync(model, userExists, userInfo),
                    IsBodyHtml = true
                };
                userMessage.To.Add(model.Email);

                // Enviar ambos correos
                await client.SendMailAsync(teamMessage);

                // Solo enviar respuesta automática si no es un tipo que requiere intervención humana
                if (ShouldSendAutomaticResponse(model.TipoConsulta))
                {
                    await client.SendMailAsync(userMessage);
                }

                _logger.LogInformation("✅ Email de contacto enviado exitosamente para {Email} - Usuario: {UserType}", 
                    model.Email, userExists ? "Existente" : "Nuevo");
                
                // ✅ NO SE GUARDA EN LA BASE DE DATOS - SOLO SE ENVÍA POR CORREO
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al enviar email de contacto para {Email}", model.Email);
                return false;
            }
        }

        // MÉTODO ELIMINADO: SaveContactMessageToDatabase - No guardamos en BD

        // MÉTODO MEJORADO PARA OBTENER PLANES REALES DE LA BD (solo lectura)
        private async Task<string> BuildPlansInfoFromDatabase()
        {
            try
            {
                var plans = await _context.SubscriptionPlans
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.SortOrder)
                    .ToListAsync();

                if (!plans.Any())
                    return GetDefaultPlansInfo();

                var sb = new StringBuilder();
                sb.AppendLine("<div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>");
                sb.AppendLine("<h3 style='color: #4361ee;'>📊 Nuestros Planes</h3>");
                sb.AppendLine("<div style='display: grid; gap: 15px; margin-top: 15px;'>");

                foreach (var plan in plans)
                {
                    var borderColor = plan.IsFeatured ? "#3a0ca3" : "#4361ee";
                    var badge = plan.IsFeatured ? "<span style='background: #ffd700; color: #333; padding: 2px 8px; border-radius: 10px; font-size: 0.8rem; margin-left: 8px;'>POPULAR</span>" : "";

                    sb.AppendLine($@"<div style='background: white; padding: 15px; border-radius: 6px; border-left: 4px solid {borderColor};'>
                        <h4 style='margin: 0 0 10px 0;'>{plan.Name} {badge}</h4>
                        <p style='font-size: 1.2rem; font-weight: bold; color: #4361ee; margin: 0 0 10px 0;'>{plan.FormattedPrice}/{plan.Period}</p>
                        <p style='color: #666; margin: 0 0 10px 0;'>{plan.ShortDescription}</p>
                        <ul style='margin: 0;'>");

                    // Agregar características basadas en los campos booleanos
                    if (plan.MaxHabits > 0)
                        sb.AppendLine($"<li>Hasta {plan.MaxHabits} hábitos simultáneos</li>");
                    
                    if (plan.MaxTransactions > 0)
                        sb.AppendLine($"<li>{plan.MaxTransactions} transacciones mensuales</li>");
                    
                    if (plan.HasCommunityAccess)
                        sb.AppendLine("<li>Acceso a la comunidad</li>");
                    
                    if (plan.HasAdvancedAnalytics)
                        sb.AppendLine("<li>Análisis avanzados</li>");
                    
                    if (plan.HasAIFeatures)
                        sb.AppendLine("<li>Funciones de IA</li>");
                    
                    if (plan.StorageMB > 0)
                        sb.AppendLine($"<li>{plan.StorageMB} MB de almacenamiento</li>");

                    sb.AppendLine("</ul></div>");
                }

                sb.AppendLine("</div></div>");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener planes de la BD");
                return GetDefaultPlansInfo();
            }
        }

        private string GetDefaultPlansInfo()
        {
            return @"
<div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
    <h3 style='color: #4361ee;'>📊 Nuestros Planes</h3>
    <div style='display: grid; gap: 15px; margin-top: 15px;'>
        <div style='background: white; padding: 15px; border-radius: 6px; border-left: 4px solid #4cc9f0;'>
            <h4 style='margin: 0 0 10px 0;'>🌟 Plan Básico - Gratis</h4>
            <ul style='margin: 0;'>
                <li>3 hábitos simultáneos</li>
                <li>50 transacciones mensuales</li>
                <li>10 MB de almacenamiento</li>
            </ul>
        </div>
        <div style='background: white; padding: 15px; border-radius: 6px; border-left: 4px solid #4361ee;'>
            <h4 style='margin: 0 0 10px 0;'>🚀 Plan Premium - $9.99/mes</h4>
            <ul style='margin: 0;'>
                <li>Hábitos ilimitados</li>
                <li>Transacciones ilimitadas</li>
                <li>Acceso a la comunidad</li>
                <li>100 MB de almacenamiento</li>
            </ul>
        </div>
    </div>
</div>";
        }

        // MÉTODO PARA VERIFICAR SI EL USUARIO EXISTE (solo lectura)
        private async Task<bool> UserExistsInDatabase(string email)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar usuario en BD");
                return false;
            }
        }

        // MÉTODO PARA OBTENER INFORMACIÓN DEL USUARIO (solo lectura)
        private async Task<string> GetUserSubscriptionInfo(string email)
        {
            try
            {
                var userSubscription = await _context.UserSubscriptions
                    .Include(us => us.Plan)
                    .Where(us => us.User.Email == email && us.IsActive)
                    .FirstOrDefaultAsync();

                if (userSubscription != null)
                {
                    return $@"
                    <div style='background: #e8f5e8; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                        <h4 style='color: #2e7d32; margin-top: 0;'>📋 Tu Suscripción Actual</h4>
                        <p><strong>Plan:</strong> {userSubscription.Plan.Name}</p>
                        <p><strong>Válido hasta:</strong> {userSubscription.EndDate:dd/MM/yyyy}</p>
                        <p><strong>Estado:</strong> <span style='color: #2e7d32;'>Activa</span></p>
                    </div>";
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener información de suscripción");
                return string.Empty;
            }
        }

        private string GetTeamEmailByType(TipoConsulta tipo)
        {
            return tipo switch
            {
                TipoConsulta.SoporteTecnico or TipoConsulta.ReportarBug => "soporte@lifehub.com",
                TipoConsulta.InformacionPlanes or TipoConsulta.Demostracion => "ventas@lifehub.com",
                TipoConsulta.FacturacionPagos => "facturacion@lifehub.com",
                TipoConsulta.Colaboraciones => "colaboraciones@lifehub.com",
                TipoConsulta.PrensaMedios => "prensa@lifehub.com",
                TipoConsulta.QuejasSugerencias => "calidad@lifehub.com",
                _ => "info@lifehub.com"
            };
        }

        private string GetUserEmailSubject(TipoConsulta tipo)
        {
            return tipo switch
            {
                TipoConsulta.SoporteTecnico => "Hemos recibido tu solicitud de soporte - LifeHub",
                TipoConsulta.InformacionPlanes => "Información sobre nuestros planes - LifeHub",
                TipoConsulta.FacturacionPagos => "Confirmación de consulta de facturación - LifeHub",
                TipoConsulta.Colaboraciones => "Gracias por tu interés en colaborar - LifeHub",
                TipoConsulta.PrensaMedios => "Confirmación de contacto para medios - LifeHub",
                TipoConsulta.QuejasSugerencias => "Hemos recibido tus comentarios - LifeHub",
                TipoConsulta.ReportarBug => "Hemos recibido tu reporte - LifeHub",
                TipoConsulta.Demostracion => "Confirmación de solicitud de demostración - LifeHub",
                _ => "Hemos recibido tu mensaje - LifeHub"
            };
        }

        private bool ShouldSendAutomaticResponse(TipoConsulta tipo)
        {
            // ✅ ENVIAR RESPUESTA AUTOMÁTICA PARA TODOS LOS TIPOS
            return tipo switch
            {
                TipoConsulta.SoporteTecnico => true,
                TipoConsulta.InformacionPlanes => true,
                TipoConsulta.FacturacionPagos => true,
                TipoConsulta.Colaboraciones => true,
                TipoConsulta.PrensaMedios => true,
                TipoConsulta.QuejasSugerencias => true,
                TipoConsulta.ReportarBug => true,
                TipoConsulta.Demostracion => true,
                TipoConsulta.Otro => true,
                _ => true // Por defecto, enviar respuesta
            };
        }

        // MÉTODO ASYNC MEJORADO
        private async Task<string> BuildUserAutomaticResponseAsync(ContactViewModel model, bool userExists, string userInfo)
        {
            // Agregar información del usuario si existe
            var userSpecificContent = userExists ? userInfo : string.Empty;

            return model.TipoConsulta switch
            {
                TipoConsulta.SoporteTecnico => BuildSupportAutoResponse(model, userSpecificContent),
                TipoConsulta.InformacionPlanes => await BuildPlansAutoResponseAsync(model, userSpecificContent),
                TipoConsulta.FacturacionPagos => BuildBillingAutoResponse(model, userSpecificContent),
                TipoConsulta.Colaboraciones => BuildCollaborationAutoResponse(model, userSpecificContent),
                TipoConsulta.PrensaMedios => BuildPressAutoResponse(model, userSpecificContent),
                TipoConsulta.QuejasSugerencias => BuildFeedbackAutoResponse(model, userSpecificContent),
                TipoConsulta.ReportarBug => BuildBugReportAutoResponse(model, userSpecificContent),
                TipoConsulta.Demostracion => BuildDemoAutoResponse(model, userSpecificContent),
                TipoConsulta.Otro => BuildGenericAutoResponse(model, userSpecificContent),
                _ => BuildGenericAutoResponse(model, userSpecificContent)
            };
        }

        // MÉTODO MEJORADO SIN GUARDADO EN BD
        private string BuildTeamEmailBody(ContactViewModel model, bool userExists, string userInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<h2>📩 Nuevo mensaje de contacto recibido</h2>");
            
            // BADGE DE USUARIO EXISTENTE/NUEVO
            sb.AppendLine($"<div style='background: {(userExists ? "#e8f5e8" : "#fff3cd")}; padding: 10px 15px; border-radius: 6px; margin-bottom: 15px;'>");
            sb.AppendLine($"<strong>{(userExists ? "✅ USUARIO EXISTENTE" : "🆕 NUEVO CONTACTO")}</strong>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div style='background: #f8f9fa; padding: 20px; border-radius: 8px;'>");
            sb.AppendLine($"<p><strong>Nombre:</strong> {WebUtility.HtmlEncode(model.Nombre)}</p>");
            sb.AppendLine($"<p><strong>Email:</strong> {WebUtility.HtmlEncode(model.Email)}</p>");
            sb.AppendLine($"<p><strong>Tipo de consulta:</strong> {model.TipoConsulta}</p>");
            sb.AppendLine($"<p><strong>Asunto:</strong> {WebUtility.HtmlEncode(model.Asunto)}</p>");
            sb.AppendLine($"<p><strong>Fecha:</strong> {model.FechaEnvio:dd/MM/yyyy HH:mm}</p>");
            sb.AppendLine($"<p><strong>IP:</strong> {model.IPAddress ?? "No disponible"}</p>");
            
            if (!string.IsNullOrEmpty(model.UrlProblema))
            {
                sb.AppendLine($"<p><strong>URL del problema:</strong> {WebUtility.HtmlEncode(model.UrlProblema)}</p>");
            }
            if (!string.IsNullOrEmpty(model.PlanInteres))
            {
                sb.AppendLine($"<p><strong>Plan de interés:</strong> {WebUtility.HtmlEncode(model.PlanInteres)}</p>");
            }
            
            sb.AppendLine("</div>");

            // INFORMACIÓN DEL USUARIO (si existe)
            if (!string.IsNullOrEmpty(userInfo))
            {
                sb.AppendLine(userInfo);
            }

            sb.AppendLine("<div style='margin-top: 20px;'>");
            sb.AppendLine("<h3>📝 Mensaje:</h3>");
            sb.AppendLine($"<div style='background: white; padding: 15px; border-left: 4px solid #4361ee;'>");
            sb.AppendLine($"<p>{WebUtility.HtmlEncode(model.Mensaje).Replace("\n", "<br>")}</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            // ✅ NOTA: Este mensaje NO se guarda en la base de datos
            sb.AppendLine("<div style='background: #e7f3ff; padding: 10px; border-radius: 6px; margin-top: 15px;'>");
            sb.AppendLine("<p style='margin: 0; font-size: 0.9rem; color: #0066cc;'>");
            sb.AppendLine("💡 <strong>Nota:</strong> Este mensaje fue enviado directamente por correo y NO está almacenado en la base de datos.");
            sb.AppendLine("</p>");
            sb.AppendLine("</div>");

            return WrapInEmailTemplate(sb.ToString(), "Nuevo Contacto - LifeHub");
        }

        private string BuildSupportAutoResponse(ContactViewModel model, string userInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2 style='color: #4361ee;'>¡Hola {WebUtility.HtmlEncode(model.Nombre)}!</h2>");
            sb.AppendLine("<p>Hemos recibido tu solicitud de soporte técnico. Nuestro equipo especializado la está revisando.</p>");

            if (!string.IsNullOrEmpty(userInfo))
            {
                sb.AppendLine(userInfo);
            }

            sb.AppendLine("<div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>");
            sb.AppendLine("<h4 style='color: #4361ee; margin-top: 0;'>🔧 Información de tu solicitud</h4>");
            sb.AppendLine($"<p><strong>Asunto:</strong> {WebUtility.HtmlEncode(model.Asunto)}</p>");
            if (!string.IsNullOrEmpty(model.UrlProblema))
            {
                sb.AppendLine($"<p><strong>URL reportada:</strong> {WebUtility.HtmlEncode(model.UrlProblema)}</p>");
            }
            sb.AppendLine($"<p><strong>Número de ticket:</strong> #{DateTime.Now:yyyyMMddHHmm}</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("<p><strong>¿Qué puedes esperar?</strong></p>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li>Respuesta en menos de 24 horas</li>");
            sb.AppendLine("<li>Solución paso a paso a tu problema</li>");
            sb.AppendLine("<li>Seguimiento hasta que se resuelva completamente</li>");
            sb.AppendLine("</ul>");

            sb.AppendLine("<p>Mientras tanto, puedes consultar nuestra <a href='https://lifehub.com/faq' style='color: #4361ee;'>base de conocimiento</a> donde encontrarás soluciones a problemas comunes.</p>");

            sb.AppendLine("<p><strong>Equipo de Soporte Técnico LifeHub</strong><br>");
            sb.AppendLine("soporte@lifehub.com</p>");

            return WrapInEmailTemplate(sb.ToString(), "Solicitud de Soporte - LifeHub");
        }

        private string BuildCollaborationAutoResponse(ContactViewModel model, string userInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2 style='color: #4361ee;'>¡Hola {WebUtility.HtmlEncode(model.Nombre)}!</h2>");
            sb.AppendLine("<p>Gracias por tu interés en colaborar con LifeHub. Nos emociona conocer nuevas ideas y asociaciones.</p>");

            sb.AppendLine("<div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>");
            sb.AppendLine("<h4 style='color: #4361ee; margin-top: 0;'>🤝 Áreas de Colaboración</h4>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li><strong>Desarrollo:</strong> Contribuciones técnicas y nuevas funcionalidades</li>");
            sb.AppendLine("<li><strong>Contenido:</strong> Artículos, tutoriales y recursos educativos</li>");
            sb.AppendLine("<li><strong>Comunidad:</strong> Moderación y crecimiento de nuestra comunidad</li>");
            sb.AppendLine("<li><strong>Diseño:</strong> Mejoras UX/UI y recursos visuales</li>");
            sb.AppendLine("<li><strong>Traducción:</strong> Localización a otros idiomas</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("</div>");

            sb.AppendLine("<p>Hemos registrado tu propuesta y nuestro equipo de colaboraciones te contactará en las próximas 48 horas para discutir los siguientes pasos.</p>");

            sb.AppendLine("<p><strong>Equipo de Colaboraciones LifeHub</strong><br>");
            sb.AppendLine("colaboraciones@lifehub.com</p>");

            return WrapInEmailTemplate(sb.ToString(), "Colaboración - LifeHub");
        }

        private string BuildPressAutoResponse(ContactViewModel model, string userInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2 style='color: #4361ee;'>¡Hola {WebUtility.HtmlEncode(model.Nombre)}!</h2>");
            sb.AppendLine("<p>Gracias por contactar al equipo de prensa y medios de LifeHub.</p>");

            sb.AppendLine("<div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>");
            sb.AppendLine("<h4 style='color: #4361ee; margin-top: 0;'>📰 Recursos para Medios</h4>");
            sb.AppendLine("<p>Te proporcionamos acceso a nuestros recursos:</p>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li><a href='https://lifehub.com/press-kit' style='color: #4361ee;'>Kit de prensa</a> - Logos, imágenes y información de la empresa</li>");
            sb.AppendLine("<li><a href='https://lifehub.com/media' style='color: #4361ee;'>Galería multimedia</a> - Capturas de pantalla y videos</li>");
            sb.AppendLine("<li><a href='https://lifehub.com/press-releases' style='color: #4361ee;'>Comunicados de prensa</a> - Últimas noticias y anuncios</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("</div>");

            sb.AppendLine("<p>Nuestro equipo de prensa revisará tu solicitud y te contactará pronto para coordinar una entrevista o proporcionar la información específica que necesites.</p>");

            sb.AppendLine("<p><strong>Equipo de Prensa LifeHub</strong><br>");
            sb.AppendLine("prensa@lifehub.com</p>");

            return WrapInEmailTemplate(sb.ToString(), "Contacto de Prensa - LifeHub");
        }

        private string BuildDemoAutoResponse(ContactViewModel model, string userInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2 style='color: #4361ee;'>¡Hola {WebUtility.HtmlEncode(model.Nombre)}!</h2>");
            sb.AppendLine("<p>Gracias por solicitar una demostración de LifeHub. Estamos emocionados de mostrarte todo lo que nuestra plataforma puede hacer por ti.</p>");

            sb.AppendLine("<div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>");
            sb.AppendLine("<h4 style='color: #4361ee; margin-top: 0;'>🎯 Lo que verás en la demostración</h4>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li>Gestión completa de hábitos y rutinas</li>");
            sb.AppendLine("<li>Seguimiento financiero y presupuestos</li>");
            sb.AppendLine("<li>Organización de salud y medicamentos</li>");
            sb.AppendLine("<li>Comunidades y colaboración</li>");
            sb.AppendLine("<li>Análisis avanzados y reportes</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("</div>");

            sb.AppendLine("<p>Nuestro equipo de ventas te contactará dentro de las próximas 24 horas para coordinar el día y hora que mejor te convenga para la demostración.</p>");

            sb.AppendLine("<p><strong>Duración aproximada:</strong> 30-45 minutos<br>");
            sb.AppendLine("<strong>Modalidad:</strong> Online (videollamada)</p>");

            sb.AppendLine("<p><strong>Equipo de Ventas LifeHub</strong><br>");
            sb.AppendLine("ventas@lifehub.com</p>");

            return WrapInEmailTemplate(sb.ToString(), "Solicitud de Demostración - LifeHub");
        }

        private string BuildSupportResponseEmailBody(string userName, string message)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2 style='color: #4361ee;'>Hola {WebUtility.HtmlEncode(userName)}</h2>");
            sb.AppendLine("<p>Gracias por contactarte con el equipo de soporte de LifeHub. Aquí está nuestra respuesta:</p>");
            
            sb.AppendLine("<div style='background: #f8f9fa; padding: 15px; border-left: 4px solid #4361ee; margin: 20px 0;'>");
            sb.AppendLine($"<p>{WebUtility.HtmlEncode(message).Replace("\n", "<br>")}</p>");
            sb.AppendLine("</div>");
            
            sb.AppendLine("<p>Si tienes más preguntas, no dudes en respondernos a este mismo email.</p>");
            sb.AppendLine("<p><strong>Equipo de Soporte LifeHub</strong><br>");
            sb.AppendLine("soporte@lifehub.com</p>");

            return WrapInEmailTemplate(sb.ToString(), "Respuesta de Soporte - LifeHub");
        }

        private string WrapInEmailTemplate(string content, string title)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{title}</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #4361ee, #3a0ca3); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: white; padding: 30px; border: 1px solid #e2e8f0; }}
        .footer {{ background: #f8fafc; padding: 20px; text-align: center; color: #64748b; font-size: 14px; border-radius: 0 0 10px 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>LifeHub</h1>
            <p>Tu vida, tu bienestar, en un solo lugar</p>
        </div>
        <div class='content'>
            {content}
        </div>
        <div class='footer'>
            <p>© 2024 LifeHub. Todos los derechos reservados.</p>
            <p><a href='#' style='color: #64748b;'>Política de privacidad</a> • <a href='#' style='color: #64748b;'>Términos de servicio</a></p>
        </div>
    </div>
</body>
</html>";
        }

        private async Task<string> BuildPlansAutoResponseAsync(ContactViewModel model, string userInfo)
        {
            var plansInfo = await BuildPlansInfoFromDatabase();
            
            var sb = new StringBuilder();
            sb.AppendLine($"<h2 style='color: #4361ee;'>¡Hola {WebUtility.HtmlEncode(model.Nombre)}!</h2>");
            sb.AppendLine("<p>Gracias por tu interés en los planes de LifeHub. Aquí tienes información detallada:</p>");
            
            // Información específica del usuario si existe
            if (!string.IsNullOrEmpty(userInfo))
            {
                sb.AppendLine(userInfo);
                sb.AppendLine("<p><strong>💡 ¿Quieres mejorar tu plan?</strong> Podemos ayudarte a actualizar según tus necesidades.</p>");
            }
            
            sb.AppendLine(plansInfo);
            
            sb.AppendLine("<p><strong>💡 ¿Listo para comenzar?</strong> Visita nuestra página de planes para activar tu prueba gratuita de 14 días.</p>");
            sb.AppendLine("<p style='text-align: center; margin: 25px 0;'>");
            sb.AppendLine("<a href='https://lifehub.com/planes' style='background: #4361ee; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; display: inline-block;'>Ver Planes Detallados</a>");
            sb.AppendLine("</p>");
            
            sb.AppendLine("<p>Si tienes preguntas específicas, nuestro equipo de ventas te contactará en las próximas 24 horas.</p>");
            sb.AppendLine("<p><strong>Equipo LifeHub</strong><br>");
            
            return WrapInEmailTemplate(sb.ToString(), "Información de Planes - LifeHub");
        }

        private string BuildBillingAutoResponse(ContactViewModel model, string userInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2 style='color: #4361ee;'>Hola {WebUtility.HtmlEncode(model.Nombre)}</h2>");
            sb.AppendLine("<p>Hemos recibido tu consulta sobre facturación y pagos. Aquí tienes información útil:</p>");
            
            // Información de suscripción si existe
            if (!string.IsNullOrEmpty(userInfo))
            {
                sb.AppendLine(userInfo);
            }
            
            sb.AppendLine("<div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>");
            sb.AppendLine("<h4 style='color: #4361ee; margin-top: 0;'>📋 Soluciones Comunes</h4>");
            
            sb.AppendLine("<div style='margin: 15px 0;'>");
            sb.AppendLine("<strong>🔹 ¿Problemas con el pago?</strong>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li>Verifica que tu tarjeta no esté vencida</li>");
            sb.AppendLine("<li>Confirma que el código CVV sea correcto</li>");
            sb.AppendLine("<li>Intenta con otra tarjeta o método de pago</li>");
            sb.AppendLine("</ul></div>");
            
            sb.AppendLine("<div style='margin: 15px 0;'>");
            sb.AppendLine("<strong>🔹 ¿Necesitas factura?</strong>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li>Descarga tus facturas desde tu panel de usuario</li>");
            sb.AppendLine("<li>Configura tus datos fiscales en 'Mi Cuenta'</li>");
            sb.AppendLine("<li>Las facturas se generan automáticamente cada mes</li>");
            sb.AppendLine("</ul></div>");
            
            sb.AppendLine("<div style='margin: 15px 0;'>");
            sb.AppendLine("<strong>🔹 ¿Quieres cancelar?</strong>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li>Puedes cancelar en cualquier momento desde 'Configuración'</li>");
            sb.AppendLine("<li>No hay cargos por cancelación</li>");
            sb.AppendLine("<li>Conservarás acceso hasta el final del período pagado</li>");
            sb.AppendLine("</ul></div>");
            sb.AppendLine("</div>");
            
            sb.AppendLine("<p>Si esto no resuelve tu problema, nuestro equipo de facturación te contactará en menos de 24 horas.</p>");
            sb.AppendLine("<p><strong>Equipo de Facturación LifeHub</strong></p>");
            
            return WrapInEmailTemplate(sb.ToString(), "Facturación y Pagos - LifeHub");
        }

        private string BuildBugReportAutoResponse(ContactViewModel model, string userInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2 style='color: #4361ee;'>¡Gracias por reportar, {WebUtility.HtmlEncode(model.Nombre)}!</h2>");
            sb.AppendLine("<p>Hemos recibido tu reporte y nuestro equipo técnico ya está investigando.</p>");
            
            sb.AppendLine("<div style='background: #fff3cd; padding: 15px; border-radius: 6px; margin: 20px 0;'>");
            sb.AppendLine("<h4 style='color: #856404; margin-top: 0;'>📝 Ticket #" + DateTime.Now.ToString("yyyyMMddHHmm") + "</h4>");
            sb.AppendLine($"<p><strong>Problema:</strong> {WebUtility.HtmlEncode(model.Asunto)}</p>");
            if (!string.IsNullOrEmpty(model.UrlProblema))
            {
                sb.AppendLine($"<p><strong>URL:</strong> {WebUtility.HtmlEncode(model.UrlProblema)}</p>");
            }
            sb.AppendLine($"<p><strong>Fecha reporte:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
            sb.AppendLine("</div>");
            
            sb.AppendLine("<p><strong>¿Qué sigue?</strong></p>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li>Nuestro equipo analizará el problema</li>");
            sb.AppendLine("<li>Te notificaremos cuando sea solucionado</li>");
            sb.AppendLine("<li>Si necesitamos más información, te contactaremos</li>");
            sb.AppendLine("</ul>");
            
            sb.AppendLine("<p>Gracias por ayudarnos a mejorar LifeHub. 🚀</p>");
            sb.AppendLine("<p><strong>Equipo Técnico LifeHub</strong></p>");
            
            return WrapInEmailTemplate(sb.ToString(), "Reporte de Error - LifeHub");
        }

        private string BuildFeedbackAutoResponse(ContactViewModel model, string userInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2 style='color: #4361ee;'>¡Gracias por tus comentarios, {WebUtility.HtmlEncode(model.Nombre)}!</h2>");
            sb.AppendLine("<p>Tu opinión es muy valiosa para nosotros. Hemos registrado tus comentarios y los revisaremos cuidadosamente.</p>");
            
            sb.AppendLine("<div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>");
            sb.AppendLine("<h4 style='margin-top: 0;'>📋 Resumen de tu feedback</h4>");
            sb.AppendLine($"<p><strong>Tipo:</strong> {WebUtility.HtmlEncode(model.TipoConsulta.ToString())}</p>");
            sb.AppendLine($"<p><strong>Mensaje:</strong> {WebUtility.HtmlEncode(model.Mensaje)}</p>");
            sb.AppendLine("</div>");
            
            sb.AppendLine("<p><strong>¿Sabías que?</strong> Muchas de nuestras mejores funciones nacieron de sugerencias como la tuya.</p>");
            sb.AppendLine("<p>Seguimos trabajando para hacer de LifeHub la mejor plataforma para tu bienestar.</p>");
            
            sb.AppendLine("<p><strong>Equipo de Calidad LifeHub</strong><br>");
            sb.AppendLine("<em>Escuchando siempre a nuestra comunidad</em></p>");
            
            return WrapInEmailTemplate(sb.ToString(), "Feedback Recibido - LifeHub");
        }

        private string BuildGenericAutoResponse(ContactViewModel model, string userInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<h2 style='color: #4361ee;'>¡Hola {WebUtility.HtmlEncode(model.Nombre)}!</h2>");
            sb.AppendLine("<p>Hemos recibido tu mensaje y nuestro equipo te responderá personalmente en un plazo máximo de 24 horas.</p>");

            sb.AppendLine("<div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>");
            sb.AppendLine("<h4 style='margin-top: 0;'>📬 Resumen de tu consulta</h4>");
            sb.AppendLine($"<p><strong>Tipo:</strong> {WebUtility.HtmlEncode(model.TipoConsulta.ToString())}</p>");
            sb.AppendLine($"<p><strong>Asunto:</strong> {WebUtility.HtmlEncode(model.Asunto)}</p>");
            sb.AppendLine($"<p><strong>Fecha:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("<p>Mientras tanto, puedes explorar:</p>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li><a href='#faq' style='color: #4361ee;'>Preguntas frecuentes</a></li>");
            sb.AppendLine("<li><a href='#blog' style='color: #4361ee;'>Blog de bienestar</a></li>");
            sb.AppendLine("<li><a href='#comunidad' style='color: #4361ee;'>Nuestra comunidad</a></li>");
            sb.AppendLine("</ul>");

            sb.AppendLine("<p><strong>Equipo LifeHub</strong><br>");
            sb.AppendLine("<em>Tu vida, tu bienestar, en un solo lugar</em></p>");

            return WrapInEmailTemplate(sb.ToString(), "Confirmación de Contacto - LifeHub");
        }

        public async Task<bool> SendSupportResponseAsync(string toEmail, string userName, string subject, string message)
        {
            try
            {
                var smtpConfig = GetSmtpConfig();
                
                using var client = new SmtpClient(smtpConfig.Host, smtpConfig.Port)
                {
                    EnableSsl = smtpConfig.EnableSsl,
                    Credentials = new NetworkCredential(smtpConfig.Username, smtpConfig.Password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpConfig.Username, "Equipo LifeHub"),
                    Subject = $"Re: {subject}",
                    Body = BuildSupportResponseEmailBody(userName, message),
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("✅ Respuesta de soporte enviada a {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al enviar respuesta de soporte a {Email}", toEmail);
                return false;
            }
        }

        public Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
        {
            // Implementación para email de bienvenida
            return Task.FromResult(true);
        }

        // Agregar este método en LifeHub/Services/EmailService.cs
        // En LifeHub/Services/EmailService.cs - AGREGAR este método
        public async Task<bool> SendSystemNotificationAsync(string toEmail, string subject, string htmlMessage, string? plainTextMessage = null)
        {
            try
            {
                var smtpConfig = GetSmtpConfig();

                using var client = new SmtpClient(smtpConfig.Host, smtpConfig.Port)
                {
                    EnableSsl = smtpConfig.EnableSsl,
                    Credentials = new NetworkCredential(smtpConfig.Username, smtpConfig.Password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpConfig.Username, "LifeHub Salud"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                // Agregar versión texto plano si se proporciona
                if (!string.IsNullOrEmpty(plainTextMessage))
                {
                    var plainTextView = AlternateView.CreateAlternateViewFromString(
                        plainTextMessage, null, "text/plain");
                    mailMessage.AlternateViews.Add(plainTextView);
                }

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("✅ Notificación del sistema enviada a {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al enviar notificación del sistema a {Email}", toEmail);
                return false;
            }
        }

        public Task<bool> SendSystemNotificationAsync(string toEmail, string subject, string htmlMessage)
        {
            throw new NotImplementedException();
        }
    }
}