using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LifeHub.Models.Entities;
using LifeHub.Models;
using System.Text.Json;

namespace LifeHub.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tus DbSets personalizados
        public DbSet<Habit> Habits { get; set; }
        public DbSet<HabitCompletion> HabitCompletions { get; set; }
        public DbSet<FinancialTransaction> FinancialTransactions { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<MedicalAppointment> MedicalAppointments { get; set; }
        public DbSet<Medication> Medications { get; set; }
        public DbSet<Community> Communities { get; set; }
        public DbSet<CommunityMember> CommunityMembers { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        
        // Añadir estos DbSets para que EF los reconozca
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        
        // AGREGAR ESTE NUEVO DbSet:
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<WellnessCheck> WellnessChecks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 🔥 CONFIGURACIÓN DE WellnessCheck (NUEVO)
            builder.Entity<WellnessCheck>(entity =>
            {
                entity.ToTable("wellness_checks");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.CheckDate).HasColumnName("check_date").IsRequired();
                
                // ✅ MANTENER campo existente Mood
                entity.Property(e => e.Mood)
                    .HasColumnName("mood")
                    .HasMaxLength(20)
                    .IsRequired()
                    .HasDefaultValue("neutral");
                
                // ✅ NUEVO campo GeneralWellness
                entity.Property(e => e.GeneralWellness)
                    .HasColumnName("general_wellness")
                    .HasConversion<int>()
                    .IsRequired(); // Regular por defecto
                
                // Campos existentes
                entity.Property(e => e.EnergyLevel).HasColumnName("energy_level").IsRequired();
                entity.Property(e => e.QuickNote).HasColumnName("quick_note");
                entity.Property(e => e.TookMedications).HasColumnName("took_medications").IsRequired();
                entity.Property(e => e.MedicationNotes).HasColumnName("medication_notes");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
                
                // ✅ NUEVOS CAMPOS
                entity.Property(e => e.Symptoms)
                    .HasColumnName("symptoms")
                    .HasMaxLength(500)
                    .HasDefaultValue("");
                    
                entity.Property(e => e.CustomSymptom)
                    .HasColumnName("custom_symptom")
                    .HasMaxLength(100);
                    
                entity.Property(e => e.SleepQuality)
                    .HasColumnName("sleep_quality")
                    .IsRequired()
                    .HasDefaultValue(5);
                
                // Índice único
                entity.HasIndex(w => new { w.UserId, w.CheckDate })
                    .IsUnique();
                
                entity.HasOne(w => w.User)
                    .WithMany()
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 🔥 CONFIGURACIÓN DE MedicalAppointment (MEJORADA)
            builder.Entity<MedicalAppointment>(entity =>
            {
                entity.ToTable("medical_appointments");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.Title).HasColumnName("title").IsRequired().HasMaxLength(200);
                entity.Property(e => e.DoctorName).HasColumnName("doctor_name").HasMaxLength(100);
                entity.Property(e => e.Specialty).HasColumnName("specialty").HasMaxLength(100);
                entity.Property(e => e.AppointmentDate).HasColumnName("appointment_date").IsRequired();
                entity.Property(e => e.Duration).HasColumnName("duration").IsRequired();
                entity.Property(e => e.Location).HasColumnName("location").HasMaxLength(200);
                entity.Property(e => e.Notes).HasColumnName("notes");
                entity.Property(e => e.ReminderSent).HasColumnName("reminder_sent");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
                
                // NUEVOS CAMPOS para el sistema mejorado
                entity.Property(e => e.HealthConcerns).HasColumnName("health_concerns");
                entity.Property(e => e.QuestionsForDoctor).HasColumnName("questions_for_doctor");
                entity.Property(e => e.PostAppointmentSummary).HasColumnName("post_appointment_summary");
                entity.Property(e => e.NextSteps).HasColumnName("next_steps");
                
                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 🔥 CONFIGURACIÓN DE Medication (MEJORADA)
            builder.Entity<Medication>(entity =>
            {
                entity.ToTable("medications");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
                entity.Property(e => e.Dosage).HasColumnName("dosage").HasMaxLength(100);
                entity.Property(e => e.Frequency).HasColumnName("frequency").HasMaxLength(100);
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.EndDate).HasColumnName("end_date");
                entity.Property(e => e.Notes).HasColumnName("notes");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
                
                // NUEVOS CAMPOS para sistema inteligente
                entity.Property(e => e.TotalQuantity).HasColumnName("total_quantity").IsRequired().HasDefaultValue(0);
                entity.Property(e => e.DosagePerIntake).HasColumnName("dosage_per_intake").IsRequired().HasDefaultValue(1);
                entity.Property(e => e.TimesPerDay).HasColumnName("times_per_day").IsRequired().HasDefaultValue(1);
                entity.Property(e => e.LowStockAlert).HasColumnName("low_stock_alert").IsRequired().HasDefaultValue(5);
                entity.Property(e => e.LastTaken).HasColumnName("last_taken");
                entity.Property(e => e.RequiresPrescription).HasColumnName("requires_prescription").HasDefaultValue(false);
                
                entity.HasOne(m => m.User)
                      .WithMany()
                      .HasForeignKey(m => m.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configurar UserSubscription
            builder.Entity<UserSubscription>(entity =>
            {
                entity.ToTable("user_subscriptions");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.PlanId).HasColumnName("plan_id");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.EndDate).HasColumnName("end_date");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                
                entity.HasOne(us => us.Plan)
                      .WithMany()
                      .HasForeignKey(us => us.PlanId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(us => us.User)
                      .WithMany()
                      .HasForeignKey(us => us.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configurar SubscriptionPlan
            builder.Entity<SubscriptionPlan>(entity =>
            {
                entity.ToTable("subscription_plans");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.ShortDescription).HasColumnName("short_description");
                entity.Property(e => e.LongDescription).HasColumnName("long_description");
                entity.Property(e => e.Price).HasColumnName("price");
                entity.Property(e => e.DurationDays).HasColumnName("duration_days");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.MaxHabits).HasColumnName("max_habits");
                entity.Property(e => e.MaxTransactions).HasColumnName("max_transactions");
                entity.Property(e => e.HasCommunityAccess).HasColumnName("has_community_access");
                entity.Property(e => e.HasAdvancedAnalytics).HasColumnName("has_advanced_analytics");
                entity.Property(e => e.HasAIFeatures).HasColumnName("has_ai_features");
                entity.Property(e => e.StorageMB).HasColumnName("storage_mb");
                entity.Property(e => e.ColorCode).HasColumnName("color_code");
                entity.Property(e => e.IsFeatured).HasColumnName("is_featured");
                entity.Property(e => e.SortOrder).HasColumnName("sort_order");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.Features).HasColumnName("features");
            });

            // Configurar UserProfile
            builder.Entity<UserProfile>(entity =>
            {
                entity.ToTable("user_profiles");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Bio).HasColumnName("bio");
                entity.Property(e => e.Location).HasColumnName("location");
                entity.Property(e => e.WebsiteUrl).HasColumnName("website_url");
                entity.Property(e => e.SocialLinks).HasColumnName("social_links");
                entity.Property(e => e.PrivacySettings).HasColumnName("privacy_settings");
                entity.Property(e => e.EmailNotifications).HasColumnName("email_notifications");
                entity.Property(e => e.PushNotifications).HasColumnName("push_notifications");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                
                entity.HasOne(up => up.User)
                      .WithMany()
                      .HasForeignKey(up => up.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 🔥 CONFIGURACIONES EXISTENTES (mantener)
            builder.Entity<Habit>(entity =>
            {
                entity.ToTable("habits");
                entity.HasKey(e => e.Id);
                // ... tu configuración existente de Habit
            });

            builder.Entity<HabitCompletion>(entity =>
            {
                entity.ToTable("habit_completions");
                entity.HasKey(e => e.Id);
                // ... tu configuración existente
            });

            builder.Entity<FinancialTransaction>(entity =>
            {
                entity.ToTable("financial_transactions");
                entity.HasKey(e => e.Id);
                // ... tu configuración existente
            });

            builder.Entity<Budget>(entity =>
            {
                entity.ToTable("budgets");
                entity.HasKey(e => e.Id);
                // ... tu configuración existente
            });

            builder.Entity<Community>(entity =>
            {
                entity.ToTable("communities");
                entity.HasKey(e => e.Id);
                // ... tu configuración existente
            });

            builder.Entity<CommunityMember>(entity =>
            {
                entity.ToTable("community_members");
                entity.HasKey(e => e.Id);
                // ... tu configuración existente
            });

            builder.Entity<ContactMessage>(entity =>
            {
                entity.ToTable("contact_messages");
                entity.HasKey(e => e.Id);
                // ... tu configuración existente
            });

            // DATA SEEDING - Los planes se crearán automáticamente
            builder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan
                {
                    Id = 1,
                    Name = "Plan Gratis",
                    Price = 0.00m,
                    DurationDays = 30,
                    ShortDescription = "Comienza tu viaje",
                    LongDescription = "Perfecto para empezar a organizar tu vida sin compromiso",
                    MaxHabits = 3,
                    MaxTransactions = 50,
                    MaxBudgets = 5,
                    HasCommunityAccess = false,
                    HasAdvancedAnalytics = false,
                    HasAIFeatures = false,
                    StorageMB = 10,
                    ColorCode = "#6B7280",
                    IsFeatured = false,
                    SortOrder = 1,
                    IsActive = true,
                    Features = JsonDocument.Parse("{\"habitos\": [\"basico\"], \"finanzas\": [\"registro_basico\"], \"limites\": {\"habitos\": 3, \"transacciones\": 50, \"presupuestos\": 5}}"),
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new SubscriptionPlan
                {
                    Id = 2,
                    Name = "Vida Básica",
                    Price = 4.99m,
                    DurationDays = 30,
                    ShortDescription = "Organización esencial",
                    LongDescription = "Todo lo necesario para mejorar tus hábitos y finanzas de manera efectiva",
                    MaxHabits = 15,
                    MaxTransactions = 200,
                    MaxBudgets = 15,
                    HasCommunityAccess = true,
                    HasAdvancedAnalytics = false,
                    HasAIFeatures = true,
                    StorageMB = 100,
                    ColorCode = "#10B981",
                    IsFeatured = true,
                    SortOrder = 2,
                    IsActive = true,
                    Features = JsonDocument.Parse("{\"habitos\": [\"seguimiento_avanzado\", \"recordatorios\"], \"finanzas\": [\"categorizacion\", \"presupuestos\"], \"comunidad\": true, \"limites\": {\"habitos\": 15, \"transacciones\": 200, \"presupuestos\": 15}}"),
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new SubscriptionPlan
                {
                    Id = 3,
                    Name = "Vida Premium",
                    Price = 9.99m,
                    DurationDays = 30,
                    ShortDescription = "Transformación completa",
                    LongDescription = "Herramientas avanzadas para todos los aspectos de tu vida con análisis profundos",
                    MaxHabits = 999,
                    MaxTransactions = 500,
                    MaxBudgets = 50,
                    HasCommunityAccess = true,
                    HasAdvancedAnalytics = true,
                    HasAIFeatures = true,
                    StorageMB = 500,
                    ColorCode = "#8B5CF6",
                    IsFeatured = true,
                    SortOrder = 3,
                    IsActive = true,
                    Features = JsonDocument.Parse("{\"habitos\": [\"gamificacion\", \"analisis_predictivo\"], \"finanzas\": [\"alertas_inteligentes\", \"planificacion\"], \"salud\": [\"seguimiento_completo\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"limites\": {\"habitos\": 999, \"transacciones\": 500, \"presupuestos\": 50}}"),
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new SubscriptionPlan
                {
                    Id = 4,
                    Name = "Vida Máxima",
                    Price = 14.99m,
                    DurationDays = 30,
                    ShortDescription = "Experiencia premium total",
                    LongDescription = "Todo ilimitado con soporte prioritario y funciones exclusivas para máxima productividad",
                    MaxHabits = 9999,
                    MaxTransactions = 9999,
                    MaxBudgets = 9999,
                    HasCommunityAccess = true,
                    HasAdvancedAnalytics = true,
                    HasAIFeatures = true,
                    StorageMB = 1000,
                    ColorCode = "#F59E0B",
                    IsFeatured = false,
                    SortOrder = 4,
                    IsActive = true,
                    Features = JsonDocument.Parse("{\"habitos\": [\"ilimitado\", \"personalizacion_avanzada\"], \"finanzas\": [\"ilimitado\", \"reportes_avanzados\"], \"salud\": [\"seguimiento_completo\", \"integracion_wearables\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"soporte\": \"prioritario\", \"limites\": {\"habitos\": 9999, \"transacciones\": 9999, \"presupuestos\": 9999}}"),
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Configurar todas las propiedades DateTime para usar UTC
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetColumnType("timestamp with time zone");
                    }
                }
            }

            // Índices existentes
            builder.Entity<HabitCompletion>()
                .HasIndex(hc => new { hc.HabitId, hc.CompletionDate })
                .IsUnique();

            builder.Entity<FinancialTransaction>()
                .HasIndex(ft => ft.TransactionDate);

            builder.Entity<CommunityMember>()
                .HasIndex(cm => new { cm.CommunityId, cm.UserId })
                .IsUnique();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is not null && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                foreach (var property in entityEntry.Properties)
                {
                    if (property.CurrentValue is DateTime dateTime)
                    {
                        if (dateTime.Kind != DateTimeKind.Utc)
                        {
                            property.CurrentValue = dateTime.ToUniversalTime();
                        }
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}