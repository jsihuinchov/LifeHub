using Microsoft.EntityFrameworkCore;
using LifeHub.Data;
using LifeHub.Models.Entities;
using LifeHub.Models.ViewModels;
using LifeHub.Services;

namespace LifeHub.Services
{
    public class HabitService : IHabitService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionService _subscriptionService;

        public HabitService(ApplicationDbContext context, ISubscriptionService subscriptionService)
        {
            _context = context;
            _subscriptionService = subscriptionService;
        }

        public async Task<List<Habit>> GetUserHabitsAsync(string userId)
        {
            return await _context.Habits
                .Where(h => h.UserId == userId && h.IsActive)
                .Include(h => h.HabitCompletions)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }

        public async Task<Habit?> GetHabitByIdAsync(int id, string userId)
        {
            return await _context.Habits
                .Include(h => h.HabitCompletions)
                .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);
        }

        public async Task<bool> CreateHabitAsync(Habit habit, string userId)
        {
            try
            {
                // ✅ SOLO asignar datos y guardar - la validación ya se hizo en el controller
                habit.UserId = userId;
                habit.CreatedAt = DateTime.UtcNow;

                _context.Habits.Add(habit);
                var result = await _context.SaveChangesAsync();

                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en CreateHabitAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateHabitAsync(Habit habit, string userId)
        {
            var existingHabit = await GetHabitByIdAsync(habit.Id, userId);
            if (existingHabit == null) return false;

            existingHabit.Name = habit.Name;
            existingHabit.Description = habit.Description;
            existingHabit.Category = habit.Category;
            existingHabit.Frequency = habit.Frequency;
            existingHabit.TargetCount = habit.TargetCount;
            existingHabit.ColorCode = habit.ColorCode;
            existingHabit.Icon = habit.Icon;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteHabitAsync(int id, string userId)
        {
            var habit = await GetHabitByIdAsync(id, userId);
            if (habit == null) return false;

            // Soft delete
            habit.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleHabitCompletionAsync(int habitId, DateTime date, string userId)
        {
            var habit = await GetHabitByIdAsync(habitId, userId);
            if (habit == null) return false;

            // ✅ CORREGIDO: Convertir todas las fechas a UTC explícitamente
            var today = DateTime.UtcNow.Date;
            var completionDate = date.ToUniversalTime().Date; // Convertir a UTC

            // No permitir fechas futuras
            if (completionDate > today)
            {
                return false; // No se pueden completar hábitos en fechas futuras
            }

            // No permitir fechas antes del inicio del hábito
            if (completionDate < habit.StartDate.Date)
            {
                return false; // No se pueden completar hábitos antes de su creación
            }

            var existingCompletion = await _context.HabitCompletions
                .FirstOrDefaultAsync(hc => hc.HabitId == habitId &&
                                           hc.CompletionDate.Date == completionDate);

            if (existingCompletion != null)
            {
                // Solo permitir toggle si la fecha es hoy o pasada (no futura)
                if (completionDate <= today)
                {
                    // Toggle completion
                    existingCompletion.Completed = !existingCompletion.Completed;
                    existingCompletion.Notes = existingCompletion.Completed ? "Completado" : null;

                    // Update streak count
                    if (existingCompletion.Completed)
                    {
                        existingCompletion.StreakCount = await CalculateCurrentStreakAsync(habitId, completionDate);
                    }
                    else
                    {
                        existingCompletion.StreakCount = 0;
                    }
                }
                else
                {
                    return false; // No se puede modificar completados futuros
                }
            }
            else
            {
                // Solo crear nuevos completados para fechas pasadas o hoy
                if (completionDate <= today)
                {
                    // Create new completion
                    var streakCount = await CalculateCurrentStreakAsync(habitId, completionDate);
                    var completion = new HabitCompletion
                    {
                        HabitId = habitId,
                        CompletionDate = completionDate, // Ya está en UTC
                        Completed = true,
                        Notes = "Completado",
                        StreakCount = streakCount + 1,
                        CreatedAt = DateTime.UtcNow // ✅ Usar UTC aquí también
                    };
                    _context.HabitCompletions.Add(completion);
                }
                else
                {
                    return false; // No se pueden crear completados futuros
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<HabitCompletion>> GetHabitCompletionsAsync(int habitId, string userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.HabitCompletions
                .Where(hc => hc.HabitId == habitId && hc.Habit.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(hc => hc.CompletionDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(hc => hc.CompletionDate <= endDate.Value);

            return await query.OrderByDescending(hc => hc.CompletionDate).ToListAsync();
        }

        public async Task<int> GetCurrentStreakAsync(int habitId, string userId)
        {
            var habit = await GetHabitByIdAsync(habitId, userId);
            if (habit == null) return 0;

            return await CalculateCurrentStreakAsync(habitId, DateTime.UtcNow.Date);
        }

        public async Task<bool> CanUserCreateMoreHabitsAsync(string userId)
        {
            try
            {
                Console.WriteLine($"🔍 HabitService.CanUserCreateMoreHabitsAsync - Iniciando");

                // Usar directamente el servicio existente
                var canCreate = await _subscriptionService.CanUserCreateHabitAsync(userId);

                Console.WriteLine($"🔍 Resultado de _subscriptionService.CanUserCreateHabitAsync: {canCreate}");

                return canCreate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en CanUserCreateMoreHabitsAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<int> GetUserHabitLimitAsync(string userId)
        {
            var subscription = await _subscriptionService.GetUserSubscriptionAsync(userId);
            return subscription?.Plan?.MaxHabits ?? 3;
        }

        public async Task<(int current, int max)> GetHabitUsageAsync(string userId)
        {
            var subscription = await _subscriptionService.GetUserSubscriptionAsync(userId);
            if (subscription?.Plan == null) return (0, 3);

            try
            {
                var currentCount = await _context.Habits
                    .CountAsync(h => h.UserId == userId && h.IsActive);

                return (currentCount, subscription.Plan.MaxHabits);
            }
            catch (Exception)
            {
                return (0, subscription.Plan.MaxHabits);
            }
        }

        private async Task<int> CalculateCurrentStreakAsync(int habitId, DateTime currentDate)
        {
            // ✅ CORREGIDO: Usar UTC consistentemente
            var today = DateTime.UtcNow.Date;
            var validDate = currentDate > today ? today : currentDate;

            var completions = await _context.HabitCompletions
                .Where(hc => hc.HabitId == habitId && hc.Completed && hc.CompletionDate <= today)
                .OrderByDescending(hc => hc.CompletionDate)
                .ToListAsync();

            var streak = 0;
            var date = validDate.Date;

            // Solo contar días consecutivos hacia atrás desde la fecha actual
            while (date <= today && completions.Any(c => c.CompletionDate.Date == date))
            {
                streak++;
                date = date.AddDays(-1);
            }

            return streak;
        }

        private async Task<int> GetOverallCurrentStreakAsync(string userId)
        {
            var habits = await GetUserHabitsAsync(userId);
            if (!habits.Any()) return 0;

            var overallStreak = 0;
            foreach (var habit in habits)
            {
                var streak = await GetCurrentStreakAsync(habit.Id, userId);
                overallStreak = Math.Max(overallStreak, streak);
            }

            return overallStreak;
        }

        // Agrega estos métodos a tu HabitService existente

        public async Task<List<Habit>> GetFavoriteHabitsAsync(string userId)
        {
            return await _context.Habits
                .Where(h => h.UserId == userId && h.IsActive && h.IsFavorite)
                .Include(h => h.HabitCompletions)
                .OrderBy(h => h.FavoriteOrder)
                .ThenByDescending(h => h.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ToggleFavoriteAsync(int habitId, string userId)
        {
            var habit = await GetHabitByIdAsync(habitId, userId);
            if (habit == null) return false;

            habit.IsFavorite = !habit.IsFavorite;

            if (habit.IsFavorite)
            {
                // Asignar el siguiente orden disponible
                var maxOrder = await _context.Habits
                    .Where(h => h.UserId == userId && h.IsActive && h.IsFavorite)
                    .MaxAsync(h => (int?)h.FavoriteOrder) ?? 0;
                habit.FavoriteOrder = maxOrder + 1;
            }
            else
            {
                habit.FavoriteOrder = 0;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateFavoriteOrderAsync(int habitId, string userId, int newOrder)
        {
            var habit = await GetHabitByIdAsync(habitId, userId);
            if (habit == null || !habit.IsFavorite) return false;

            // Reordenar los favoritos
            var favorites = await GetFavoriteHabitsAsync(userId);

            // Remover el hábito que se está moviendo
            var habitToMove = favorites.FirstOrDefault(h => h.Id == habitId);
            if (habitToMove == null) return false;

            favorites.Remove(habitToMove);

            // Insertar en la nueva posición
            if (newOrder >= favorites.Count)
            {
                favorites.Add(habitToMove);
            }
            else
            {
                favorites.Insert(newOrder, habitToMove);
            }

            // Actualizar órdenes
            for (int i = 0; i < favorites.Count; i++)
            {
                favorites[i].FavoriteOrder = i + 1;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<HabitDetailStatsViewModel?> GetHabitDetailStatsAsync(int habitId, string userId)
        {
            var habit = await GetHabitByIdAsync(habitId, userId);
            if (habit == null) return null;

            var completions = await _context.HabitCompletions
                .Where(hc => hc.HabitId == habitId && hc.Completed)
                .OrderBy(hc => hc.CompletionDate)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var yearStart = new DateTime(today.Year, 1, 1);

            // Estadísticas básicas
            var stats = new HabitDetailStatsViewModel
            {
                HabitId = habitId,
                HabitName = habit.Name ?? string.Empty,
                HabitDescription = habit.Description ?? string.Empty,
                HabitIcon = habit.Icon ?? "📝",
                HabitColor = habit.ColorCode ?? "#3b82f6",
                Category = habit.Category ?? string.Empty,
                Frequency = habit.Frequency,
                TargetCount = habit.TargetCount,
                StartDate = habit.StartDate,

                TotalCompletions = completions.Count,
                CurrentStreak = await GetCurrentStreakAsync(habitId, userId),
                LongestStreak = completions.Any() ? completions.Max(hc => hc.StreakCount) : 0,

                TodayCompleted = completions.Any(hc => hc.CompletionDate.Date == today),
                WeekCompletions = completions.Count(hc => hc.CompletionDate >= weekStart),
                MonthCompletions = completions.Count(hc => hc.CompletionDate >= monthStart),
                YearCompletions = completions.Count(hc => hc.CompletionDate >= yearStart),

                AverageCompletionsPerWeek = CalculateAverageCompletionsPerWeek(completions, habit.StartDate),
                SuccessRate = CalculateSuccessRate(completions, habit.StartDate, habit.Frequency),
                BestDayOfWeek = GetBestDayOfWeek(completions),
                MostProductiveMonth = GetMostProductiveMonth(completions)
            };

            // Datos para gráficos
            stats.WeeklyData = GetLast12WeeksData(completions);
            stats.MonthlyData = GetLast6MonthsData(completions);
            stats.DailyCompletionPattern = GetDailyCompletionPattern(completions);

            // Insights y recomendaciones
            stats.Insights = GenerateInsights(stats, habit);

            return stats;
        }

        private double CalculateAverageCompletionsPerWeek(List<HabitCompletion> completions, DateTime startDate)
        {
            if (!completions.Any()) return 0;

            var totalWeeks = Math.Max(1, (DateTime.UtcNow - startDate).Days / 7);
            return (double)completions.Count / totalWeeks;
        }

        private double CalculateSuccessRate(List<HabitCompletion> completions, DateTime startDate, int targetFrequency)
        {
            var totalDays = (DateTime.UtcNow - startDate).Days;
            if (totalDays <= 0) return 0;

            var expectedCompletions = (totalDays / 7.0) * targetFrequency;
            if (expectedCompletions <= 0) return 0;

            return Math.Min((completions.Count / expectedCompletions) * 100, 100);
        }

        private string GetBestDayOfWeek(List<HabitCompletion> completions)
        {
            if (!completions.Any()) return "No hay datos suficientes";

            var dayStats = completions
                .GroupBy(hc => hc.CompletionDate.DayOfWeek)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .First();

            var dayNames = new Dictionary<DayOfWeek, string>
            {
                [DayOfWeek.Monday] = "Lunes",
                [DayOfWeek.Tuesday] = "Martes",
                [DayOfWeek.Wednesday] = "Miércoles",
                [DayOfWeek.Thursday] = "Jueves",
                [DayOfWeek.Friday] = "Viernes",
                [DayOfWeek.Saturday] = "Sábado",
                [DayOfWeek.Sunday] = "Domingo"
            };

            return $"{dayNames[dayStats.Day]} ({dayStats.Count} veces)";
        }

        private string GetMostProductiveMonth(List<HabitCompletion> completions)
        {
            if (!completions.Any()) return "No hay datos suficientes";

            var monthStats = completions
                .GroupBy(hc => hc.CompletionDate.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .First();

            var monthNames = new Dictionary<int, string>
            {
                [1] = "Enero",
                [2] = "Febrero",
                [3] = "Marzo",
                [4] = "Abril",
                [5] = "Mayo",
                [6] = "Junio",
                [7] = "Julio",
                [8] = "Agosto",
                [9] = "Septiembre",
                [10] = "Octubre",
                [11] = "Noviembre",
                [12] = "Diciembre"
            };

            return $"{monthNames[monthStats.Month]} ({monthStats.Count} veces)";
        }

        private Dictionary<string, int> GetLast12WeeksData(List<HabitCompletion> completions)
        {
            var data = new Dictionary<string, int>();
            var today = DateTime.UtcNow.Date;

            for (int i = 11; i >= 0; i--)
            {
                // CORREGIDO: Calcular correctamente el inicio de cada semana
                var weekStart = today.AddDays(-(int)today.DayOfWeek - (i * 7));
                var weekEnd = weekStart.AddDays(6);
                var weekKey = $"{weekStart:dd/MM} - {weekEnd:dd/MM}";

                var weekCompletions = completions.Count(hc =>
                    hc.CompletionDate >= weekStart && hc.CompletionDate <= weekEnd);

                data[weekKey] = weekCompletions;
            }

            return data;
        }

        private Dictionary<string, int> GetLast6MonthsData(List<HabitCompletion> completions)
        {
            var data = new Dictionary<string, int>();
            var today = DateTime.UtcNow;

            for (int i = 5; i >= 0; i--)
            {
                var month = today.AddMonths(-i);
                var monthKey = month.ToString("MMM yyyy");
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var monthCompletions = completions.Count(hc =>
                    hc.CompletionDate >= monthStart && hc.CompletionDate <= monthEnd);

                data[monthKey] = monthCompletions;
            }

            return data;
        }

        private Dictionary<string, int> GetDailyCompletionPattern(List<HabitCompletion> completions)
        {
            var pattern = new Dictionary<string, int>();
            var dayNames = new[] { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };

            for (int i = 0; i < 7; i++)
            {
                var dayCompletions = completions.Count(hc => (int)hc.CompletionDate.DayOfWeek == i);
                // Ajustar: Domingo = 0, pero queremos que sea el último
                var adjustedIndex = i == 0 ? 6 : i - 1;
                pattern[dayNames[adjustedIndex]] = dayCompletions;
            }

            return pattern;
        }

        private List<string> GenerateInsights(HabitDetailStatsViewModel stats, Habit habit)
        {
            var insights = new List<string>();

            if (stats.SuccessRate >= 80)
            {
                insights.Add("🎯 ¡Excelente trabajo! Mantienes una consistencia impresionante con este hábito.");
            }
            else if (stats.SuccessRate >= 60)
            {
                insights.Add("📈 Vas por buen camino. Sigue así para consolidar el hábito.");
            }
            else if (stats.SuccessRate >= 40)
            {
                insights.Add("💪 Estás construyendo el hábito. Intenta ser más consistente esta semana.");
            }
            else
            {
                insights.Add("🌱 Todo gran hábito comienza con pequeños pasos. No te rindas.");
            }

            if (stats.CurrentStreak >= 7)
            {
                insights.Add($"🔥 ¡Racha impresionante de {stats.CurrentStreak} días! El hábito se está volviendo automático.");
            }

            if (stats.WeekCompletions >= habit.Frequency)
            {
                insights.Add("✅ Esta semana cumpliste con tu frecuencia objetivo. ¡Fantástico!");
            }
            else
            {
                var remaining = habit.Frequency - stats.WeekCompletions;
                insights.Add($"📅 Te faltan {remaining} días esta semana para alcanzar tu objetivo de frecuencia.");
            }

            if (stats.AverageCompletionsPerWeek < habit.Frequency * 0.7)
            {
                insights.Add("💡 Considera ajustar tu frecuencia objetivo si te resulta difícil de mantener.");
            }

            if (stats.TotalCompletions >= 30)
            {
                insights.Add("🏆 ¡Llevas más de 30 completados! Los estudios muestran que a los 66 días un hábito se vuelve automático.");
            }

            return insights;
        }

        public async Task<HabitStatsViewModel> GetHabitStatsAsync(string userId)
        {
            var habits = await GetUserHabitsAsync(userId);
            var completions = await _context.HabitCompletions
                .Where(hc => hc.Habit.UserId == userId && hc.Completed)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var stats = new HabitStatsViewModel
            {
                TotalHabits = habits.Count,
                ActiveHabits = habits.Count(h => h.IsActive),
                TotalCompletions = completions.Count,
                TodayCompletions = completions.Count(hc => hc.CompletionDate.Date == today),
                WeekCompletions = completions.Count(hc => hc.CompletionDate >= weekStart),
                MonthCompletions = completions.Count(hc => hc.CompletionDate >= monthStart),
                LongestStreak = completions.Any() ? completions.Max(hc => hc.StreakCount) : 0,
                CurrentStreak = await GetOverallCurrentStreakAsync(userId),
                CompletionRate = habits.Any() ? (double)completions.Count / (habits.Count * 30) * 100 : 0,

                // Usar las implementaciones reales
                WeeklyConsistency = CalculateWeeklyConsistency(completions, habits),
                MostSuccessfulHabit = await GetMostSuccessfulHabitAsync(userId),
                MostDifficultHabit = await GetMostDifficultHabitAsync(userId),
                ImprovementRate = CalculateImprovementRate(userId, completions),
                BestTimeOfDay = DetermineBestTimeOfDay(completions),
                WeeklyDistribution = CalculateWeeklyDistribution(completions),
                WeeklyTrendData = GetLast12WeeksData(completions),
                PerfectWeeks = CalculatePerfectWeeks(habits, completions)
            };

            // Categorías más populares
            stats.TopCategories = habits
                .Where(h => !string.IsNullOrEmpty(h.Category))
                .GroupBy(h => h.Category!)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToDictionary(g => g.Key, g => g.Count());

            return stats;
        }

        // ========== IMPLEMENTACIONES REALES DE LAS NUEVAS MÉTRICAS ==========

        private double CalculateWeeklyConsistency(List<HabitCompletion> completions, List<Habit> habits)
        {
            if (!completions.Any()) return 0;

            // Calcular cuántos días de la última semana tuvieron al menos un hábito completado
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-6); // Últimos 7 días
            
            var activeDays = completions
                .Where(c => c.CompletionDate >= weekStart && c.CompletionDate <= today)
                .Select(c => c.CompletionDate.Date)
                .Distinct()
                .Count();

            return (activeDays / 7.0) * 100;
        }

        private async Task<string> GetMostSuccessfulHabitAsync(string userId)
        {
            var habits = await GetUserHabitsAsync(userId);
            if (!habits.Any()) return "No hay hábitos";

            var habitSuccessRates = new List<(string Name, double SuccessRate)>();

            foreach (var habit in habits)
            {
                var completions = await _context.HabitCompletions
                    .Where(hc => hc.HabitId == habit.Id && hc.Completed)
                    .ToListAsync();

                var successRate = CalculateHabitSuccessRate(completions, habit.StartDate, habit.Frequency);
                habitSuccessRates.Add((habit.Name, successRate));
            }

            var mostSuccessful = habitSuccessRates
                .OrderByDescending(x => x.SuccessRate)
                .FirstOrDefault();

            return mostSuccessful.SuccessRate > 0 ? mostSuccessful.Name : "No hay datos";
        }

        private async Task<string> GetMostDifficultHabitAsync(string userId)
        {
            var habits = await GetUserHabitsAsync(userId);
            if (!habits.Any()) return "No hay hábitos";

            var habitSuccessRates = new List<(string Name, double SuccessRate)>();

            foreach (var habit in habits)
            {
                var completions = await _context.HabitCompletions
                    .Where(hc => hc.HabitId == habit.Id && hc.Completed)
                    .ToListAsync();

                var successRate = CalculateHabitSuccessRate(completions, habit.StartDate, habit.Frequency);
                habitSuccessRates.Add((habit.Name, successRate));
            }

            var mostDifficult = habitSuccessRates
                .Where(x => x.SuccessRate > 0) // Solo hábitos con algún progreso
                .OrderBy(x => x.SuccessRate)
                .FirstOrDefault();

            return mostDifficult.SuccessRate > 0 ? mostDifficult.Name : "No hay datos";
        }

        private double CalculateHabitSuccessRate(List<HabitCompletion> completions, DateTime startDate, int frequency)
        {
            var totalDays = (DateTime.UtcNow - startDate).Days;
            if (totalDays <= 0) return 0;

            var expectedCompletions = (totalDays / 7.0) * frequency;
            if (expectedCompletions <= 0) return 0;

            return Math.Min((completions.Count / expectedCompletions) * 100, 100);
        }

        private double CalculateImprovementRate(string userId, List<HabitCompletion> completions)
        {
            var today = DateTime.UtcNow.Date;
            var currentMonthStart = new DateTime(today.Year, today.Month, 1);
            var lastMonthStart = currentMonthStart.AddMonths(-1);
            var lastMonthEnd = currentMonthStart.AddDays(-1);

            // Completados del mes actual (hasta hoy)
            var currentMonthCompletions = completions
                .Count(c => c.CompletionDate >= currentMonthStart && c.CompletionDate <= today);

            // Completados del mes anterior (mes completo)
            var lastMonthCompletions = completions
                .Count(c => c.CompletionDate >= lastMonthStart && c.CompletionDate <= lastMonthEnd);

            if (lastMonthCompletions == 0) 
                return currentMonthCompletions > 0 ? 100 : 0;

            return ((currentMonthCompletions - lastMonthCompletions) / (double)lastMonthCompletions) * 100;
        }

        private string DetermineBestTimeOfDay(List<HabitCompletion> completions)
        {
            if (!completions.Any()) return "No hay datos";

            var morningCount = completions.Count(c => c.CompletionDate.Hour >= 6 && c.CompletionDate.Hour < 12);
            var afternoonCount = completions.Count(c => c.CompletionDate.Hour >= 12 && c.CompletionDate.Hour < 18);
            var eveningCount = completions.Count(c => c.CompletionDate.Hour >= 18 && c.CompletionDate.Hour < 24);
            var nightCount = completions.Count(c => c.CompletionDate.Hour >= 0 && c.CompletionDate.Hour < 6);

            var counts = new[]
            {
                (Count: morningCount, Period: "Mañana"),
                (Count: afternoonCount, Period: "Tarde"),
                (Count: eveningCount, Period: "Noche"),
                (Count: nightCount, Period: "Madrugada")
            };

            var bestPeriod = counts.OrderByDescending(x => x.Count).First();
            return bestPeriod.Count > 0 ? bestPeriod.Period : "No hay datos";
        }

        private Dictionary<string, int> CalculateWeeklyDistribution(List<HabitCompletion> completions)
        {
            var distribution = new Dictionary<string, int>
            {
                ["Monday"] = 0,
                ["Tuesday"] = 0,
                ["Wednesday"] = 0,
                ["Thursday"] = 0,
                ["Friday"] = 0,
                ["Saturday"] = 0,
                ["Sunday"] = 0
            };

            foreach (var completion in completions)
            {
                var day = completion.CompletionDate.DayOfWeek.ToString();
                if (distribution.ContainsKey(day))
                {
                    distribution[day]++;
                }
            }

            return distribution;
        }

        private int CalculatePerfectWeeks(List<Habit> habits, List<HabitCompletion> completions)
        {
            if (!habits.Any()) return 0;

            var perfectWeeks = 0;
            var today = DateTime.UtcNow.Date;

            // Revisar las últimas 8 semanas
            for (int weeksBack = 0; weeksBack < 8; weeksBack++)
            {
                var weekStart = today.AddDays(-(7 * (weeksBack + 1)) + 1); // +1 para empezar en lunes
                var weekEnd = weekStart.AddDays(6);

                var weekPerfect = true;

                foreach (var habit in habits)
                {
                    var expectedCompletions = habit.Frequency;
                    var actualCompletions = completions
                        .Count(c => c.HabitId == habit.Id &&
                                   c.CompletionDate >= weekStart &&
                                   c.CompletionDate <= weekEnd);

                    if (actualCompletions < expectedCompletions)
                    {
                        weekPerfect = false;
                        break;
                    }
                }

                if (weekPerfect) perfectWeeks++;
            }

            return perfectWeeks;
        }
        
        
    }
}