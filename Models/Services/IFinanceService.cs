using LifeHub.Models.Entities;

namespace LifeHub.Services
{
    public interface IFinanceService
    {
        // Transactions - Versión simplificada
        Task<List<FinancialTransaction>> GetUserTransactionsAsync(string userId);
        Task<FinancialTransaction?> GetTransactionByIdAsync(int id);
        Task<bool> CreateTransactionAsync(FinancialTransaction transaction);
        Task<bool> UpdateTransactionAsync(FinancialTransaction transaction);
        Task<bool> DeleteTransactionAsync(int id);

        // Budgets
        Task<List<Budget>> GetUserBudgetsAsync(string userId);
        Task<Budget?> GetBudgetByIdAsync(int id);
        Task<bool> CreateBudgetAsync(Budget budget);
        Task<bool> UpdateBudgetAsync(Budget budget);
        Task<bool> DeleteBudgetAsync(int id);

        // Reports & Analytics - Versión simplificada
        Task<FinanceSummary> GetFinanceSummaryAsync(string userId);
        Task<List<BudgetStatus>> GetBudgetStatusAsync(string userId);
        Task<bool> CheckBudgetAlertsAsync(string userId);

        // NUEVOS MÉTODOS PARA EL DASHBOARD MEJORADO
        Task<List<MonthlyData>> GetMonthlyDataAsync(string userId, int months = 6);
        Task<List<CategoryData>> GetExpenseByCategoryAsync(string userId);
        Task<List<CategoryData>> GetIncomeByCategoryAsync(string userId);

        // MÉTODOS PARA CONSEJOS FINANCIEROS
        Task<FinancialAdvice> GetRandomAdviceAsync();
        Task<FinancialAdvice> GetDailyAdviceAsync();
        Task<List<FinancialAdvice>> GetAdviceByCategoryAsync(string category);
        Task<List<string>> GetAdviceCategoriesAsync();

        // AGREGAR ESTE MÉTODO FALTANTE
        Task<List<CategorySummary>> GetCategorySummaryAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);

        // NUEVA SECCIÓN: Gráficos y Visualizaciones
        Task<ChartData> GetMonthlyTrendChartAsync(string userId, int months = 6);
        Task<ChartData> GetExpenseDistributionChartAsync(string userId);
        Task<ChartData> GetIncomeDistributionChartAsync(string userId);
        Task<ChartData> GetBudgetProgressChartAsync(string userId);
    
        // Métodos auxiliares para gráficos
        Task<ChartData> GetCategoryComparisonChartAsync(string userId, string chartType = "bar");
        Task<ChartData> GetSavingsProgressChartAsync(string userId);
    }

    // Tus modelos existentes se mantienen, solo actualiza FinanceSummary:

    public class FinanceSummary
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetAmount => TotalIncome - TotalExpenses;
        public decimal SavingsRate => TotalIncome > 0 ? (NetAmount / TotalIncome) * 100 : 0;
        public List<MonthlyData> MonthlyData { get; set; } = new();
        public List<CategoryData> TopExpenseCategories { get; set; } = new();

        // NUEVAS PROPIEDADES
        public int TotalTransactions { get; set; }
        public decimal AverageTransaction { get; set; }
        public decimal IncomeGrowth { get; set; }
        public decimal ExpenseGrowth { get; set; }
        public decimal MonthlyGoalProgress { get; set; }
    }

    // Los otros modelos que ya tienes se mantienen igual:
    public class MonthlyData
    {
        public string Month { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expenses { get; set; }
        public decimal Savings { get; set; }
    }

    public class CategoryData
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class CategorySummary
    {
        public string Category { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageAmount { get; set; }
    }

    public class MonthlySummary
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public List<CategorySummary> CategoryBreakdown { get; set; } = new();
    }

    public class BudgetStatus
    {
        public Budget Budget { get; set; } = null!;
        public decimal SpentAmount { get; set; }
        public decimal RemainingAmount => Budget.MonthlyAmount - SpentAmount;
        public decimal UsagePercentage => Budget.MonthlyAmount > 0 ? (SpentAmount / Budget.MonthlyAmount) * 100 : 0;
        public bool IsOverBudget => SpentAmount > Budget.MonthlyAmount;
    }

    public class DetailedFinanceReport
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetSavings { get; set; }
        public List<CategoryAnalysis> IncomeByCategory { get; set; } = new();
        public List<CategoryAnalysis> ExpensesByCategory { get; set; } = new();
        public List<MonthlyAnalysis> MonthlyAnalysis { get; set; } = new();
        public List<FinancialTransaction> LargestIncome { get; set; } = new();
        public List<FinancialTransaction> LargestExpenses { get; set; } = new();
    }

    public class CategoryAnalysis
    {
        public string Category { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageAmount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class MonthlyAnalysis
    {
        public DateTime Month { get; set; }
        public decimal Income { get; set; }
        public decimal Expenses { get; set; }
        public decimal Savings { get; set; }
        public int TransactionCount { get; set; }

        // === NUEVOS MÉTODOS PARA CONSEJOS FINANCIEROS ===

        public async Task<FinancialAdvice> GetRandomAdviceAsync()
        {
            var adviceList = await GetFinancialAdviceListAsync();
            var random = new Random();
            return adviceList[random.Next(adviceList.Count)];
        }

        public async Task<FinancialAdvice> GetDailyAdviceAsync()
        {
            var adviceList = await GetFinancialAdviceListAsync();
            var today = DateTime.Today;
            var seed = today.Year * 10000 + today.Month * 100 + today.Day;
            var random = new Random(seed);

            return adviceList[random.Next(adviceList.Count)];
        }

        public async Task<List<FinancialAdvice>> GetAdviceByCategoryAsync(string category)
        {
            var adviceList = await GetFinancialAdviceListAsync();
            return adviceList.Where(a => a.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<List<string>> GetAdviceCategoriesAsync()
        {
            var adviceList = await GetFinancialAdviceListAsync();
            return adviceList.Select(a => a.Category).Distinct().ToList();
        }

        private async Task<List<FinancialAdvice>> GetFinancialAdviceListAsync()
        {
            // Base de datos local de consejos - 55+ consejos
            return new List<FinancialAdvice>
            {
                new() { Id = 1, Category = "Ahorro", Difficulty = "Beginner", Advice = "💡 Automatiza tus ahorros: configura transferencias automáticas del 10% de tus ingresos a una cuenta de ahorros" },
                new() { Id = 2, Category = "Ahorro", Difficulty = "Beginner", Advice = "💰 La regla 50-30-20: destina 50% a necesidades, 30% a deseos y 20% al ahorro" },
                new() { Id = 3, Category = "Ahorro", Difficulty = "Intermediate", Advice = "🎯 Establece metas SMART: Específicas, Medibles, Alcanzables, Relevantes y con Tiempo definido" },
                new() { Id = 4, Category = "Ahorro", Difficulty = "Beginner", Advice = "📱 Usa la regla de las 24 horas: espera un día antes de compras no esenciales para evitar impulsos" },
                new() { Id = 5, Category = "Ahorro", Difficulty = "Intermediate", Advice = "🏦 Crea fondos separados: emergencia (3-6 meses de gastos), vacaciones, regalos, salud" },
                new() { Id = 6, Category = "Ahorro", Difficulty = "Advanced", Advice = "📈 Ahorra los aumentos de sueldo: destina el 50% de cualquier aumento a tus ahorros" },
                new() { Id = 7, Category = "Ahorro", Difficulty = "Beginner", Advice = "🔄 Redondea tus gastos: redondea cada compra al siguiente 10 y ahorra la diferencia" },
                new() { Id = 8, Category = "Ahorro", Difficulty = "Intermediate", Advice = "🎁 Aprovecha los reembolsos: guarda cualquier reembolso o dinero inesperado directamente en ahorros" },
                new() { Id = 9, Category = "Ahorro", Difficulty = "Beginner", Advice = "📊 Revisa suscripciones: cancela al menos una suscripción que no uses mensualmente" },
                new() { Id = 10, Category = "Ahorro", Difficulty = "Intermediate", Advice = "🍽️ Cocina en casa: preparar comida 2 veces más por semana puede ahorrarte hasta 30% en alimentación" },
                new() { Id = 11, Category = "Ahorro", Difficulty = "Advanced", Advice = "🚗 Transporte inteligente: usa transporte público 2 días por semana, ahorrarás en gasolina y mantenimiento" },
                new() { Id = 12, Category = "Ahorro", Difficulty = "Beginner", Advice = "🎯 Ahorro por objetivos: divide metas grandes en mini-metas mensuales más alcanzables" },
                new() { Id = 13, Category = "Ahorro", Difficulty = "Intermediate", Advice = "🏠 Reduce costos de vivienda: considera roommates o alquilar un espacio más pequeño si es posible" },
                new() { Id = 14, Category = "Ahorro", Difficulty = "Beginner", Advice = "📱 Usa apps de cashback: obtén reembolsos en tus compras habituales sin esfuerzo adicional" },
                new() { Id = 15, Category = "Ahorro", Difficulty = "Advanced", Advice = "💡 Ahorro por desafío: prueba el desafío de 52 semanas, ahorrando $1 la semana 1, $2 la semana 2, etc." },

                // 💳 CATEGORÍA: GASTOS (15 consejos)
                new() { Id = 16, Category = "Gastos", Difficulty = "Beginner", Advice = "📝 Registra todos tus gastos por 30 días: el simple acto de anotar reduce gastos innecesarios en 15%" },
                new() { Id = 17, Category = "Gastos", Difficulty = "Beginner", Advice = "🛒 Haz listas de compras: compra solo lo planeado y evita compras impulsivas" },
                new() { Id = 18, Category = "Gastos", Difficulty = "Intermediate", Advice = "📊 Analiza patrones: revisa tus gastos cada domingo para identificar tendencias y ajustar" },
                new() { Id = 19, Category = "Gastos", Difficulty = "Beginner", Advice = "❔ Pregunta '¿Lo necesito o lo quiero?': esta simple pregunta puede reducir gastos impulsivos hasta 40%" },
                new() { Id = 20, Category = "Gastos", Difficulty = "Intermediate", Advice = "🎯 Establece límites por categoría: asigna montos máximos para entretenimiento, comida fuera, etc." },
                new() { Id = 21, Category = "Gastos", Difficulty = "Advanced", Advice = "📱 Usa solo efectivo para gastos discrecionales: el dolor de pagar en efectivo reduce gastos en 20%" },
                new() { Id = 22, Category = "Gastos", Difficulty = "Beginner", Advice = "🕒 Evita compras nocturnas: la mayoría de las compras impulsivas ocurren después de las 8 PM" },
                new() { Id = 23, Category = "Gastos", Difficulty = "Intermediate", Advice = "📦 Espera 30 días para compras grandes: si todavía lo quieres después de un mes, considéralo seriamente" },
                new() { Id = 24, Category = "Gastos", Difficulty = "Beginner", Advice = "🍽️ Come antes de comprar: ir al supermercado con hambre aumenta los gastos en 25%" },
                new() { Id = 25, Category = "Gastos", Difficulty = "Intermediate", Advice = "🔍 Compara precios: siempre revisa al menos 3 opciones antes de compras importantes" },
                new() { Id = 26, Category = "Gastos", Difficulty = "Advanced", Advice = "📅 Compra en temporada baja: viajes, ropa y electrónicos tienen mejores precios en temporadas específicas" },
                new() { Id = 27, Category = "Gastos", Difficulty = "Beginner", Advice = "💡 Usa iluminación LED: reduce tu factura de electricidad hasta 80% en iluminación" },
                new() { Id = 28, Category = "Gastos", Difficulty = "Intermediate", Advice = "🚰 Reduce consumo de agua: instala aireadores en grifos y toma duchas más cortas" },
                new() { Id = 29, Category = "Gastos", Difficulty = "Beginner", Advice = "📱 Desactiva notificaciones de compras: menos tentación, menos gastos impulsivos" },
                new() { Id = 30, Category = "Gastos", Difficulty = "Advanced", Advice = "🏠 Optimiza servicios del hogar: revisa seguros, planes de teléfono y TV cada 6 meses" },

                // 📈 CATEGORÍA: INVERSIONES (10 consejos)
                new() { Id = 31, Category = "Inversiones", Difficulty = "Beginner", Advice = "⏰ Comienza temprano: gracias al interés compuesto, empezar a los 25 vs 35 puede duplicar tu patrimonio" },
                new() { Id = 32, Category = "Inversiones", Difficulty = "Beginner", Advice = "📊 Diversifica: no pongas todos tus huevos en la misma canasta, distribuye tu riesgo" },
                new() { Id = 33, Category = "Inversiones", Difficulty = "Intermediate", Advice = "🔄 Inversión automática: configura aportes automáticos a fondos indexados cada mes" },
                new() { Id = 34, Category = "Inversiones", Difficulty = "Advanced", Advice = "📉 Compra cuando hay miedo: los mejores momentos para invertir son cuando otros tienen pánico" },
                new() { Id = 35, Category = "Inversiones", Difficulty = "Beginner", Advice = "🎯 Enfócate en el largo plazo: el tiempo en el mercado supera al timing del mercado" },
                new() { Id = 36, Category = "Inversiones", Difficulty = "Intermediate", Advice = "💰 Reinvierte dividendos: el interés compuesto es la octava maravilla del mundo" },
                new() { Id = 37, Category = "Inversiones", Difficulty = "Beginner", Advice = "📚 Edúcate constantemente: dedica 1 hora semanal a aprender sobre finanzas personales" },
                new() { Id = 38, Category = "Inversiones", Difficulty = "Advanced", Advice = "🏦 Minimiza comisiones: incluso 1% de comisión anual puede reducir tu patrimonio final en 30%" },
                new() { Id = 39, Category = "Inversiones", Difficulty = "Intermediate", Advice = "📅 Dollar-cost averaging: invierte la misma cantidad regularmente sin importar las condiciones del mercado" },
                new() { Id = 40, Category = "Inversiones", Difficulty = "Beginner", Advice = "🛡️ Primero fondos de emergencia: antes de invertir, asegura 3-6 meses de gastos esenciales" },

                // 🎯 CATEGORÍA: METAS Y PLANIFICACIÓN (10 consejos)
                new() { Id = 41, Category = "Metas", Difficulty = "Beginner", Advice = "📝 Escribe tus metas: las personas que escriben sus metas tienen 42% más probabilidad de lograrlas" },
                new() { Id = 42, Category = "Metas", Difficulty = "Intermediate", Advice = "🎯 Divide y vencerás: divide metas grandes en hitos mensuales y celebra cada logro" },
                new() { Id = 43, Category = "Metas", Difficulty = "Beginner", Advice = "📊 Revisa progreso semanal: 15 minutos cada domingo para ajustar tu plan según el progreso" },
                new() { Id = 44, Category = "Metas", Difficulty = "Advanced", Advice = "🔄 Planificación por escenarios: prepara planes A, B y C para diferentes situaciones económicas" },
                new() { Id = 45, Category = "Metas", Difficulty = "Intermediate", Advice = "💰 Automatiza tus metas: configura transferencias automáticas hacia cada meta específica" },
                new() { Id = 46, Category = "Metas", Difficulty = "Beginner", Advice = "🎉 Recompénsate: celebra los hitos alcanzados con recompensas que no saboteen tu progreso" },
                new() { Id = 47, Category = "Metas", Difficulty = "Intermediate", Advice = "📱 Usa visualizaciones: gráficos y progreso visual aumentan la motivación en 35%" },
                new() { Id = 48, Category = "Metas", Difficulty = "Advanced", Advice = "🔄 Revisión trimestral: cada 3 meses, evalúa si tus metas siguen alineadas con tus prioridades" },
                new() { Id = 49, Category = "Metas", Difficulty = "Beginner", Advice = "🤝 Comparte tus metas: contárselo a alguien aumenta tu compromiso y accountability" },
                new() { Id = 50, Category = "Metas", Difficulty = "Intermediate", Advice = "📈 Ajusta por inflación: considera aumentos de precios al planificar metas a largo plazo" },

                // 🧠 CATEGORÍA: PSICOLOGÍA FINANCIERA (5 consejos)
                new() { Id = 51, Category = "Psicología", Difficulty = "Beginner", Advice = "🎭 Identifica tus triggers emocionales: ¿compras por estrés, aburrimiento o felicidad?" },
                new() { Id = 52, Category = "Psicología", Difficulty = "Intermediate", Advice = "🔄 Cambia tu mentalidad: de 'no puedo gastar' a 'elijo ahorrar para...'" },
                new() { Id = 53, Category = "Psicología", Difficulty = "Beginner", Advice = "📚 Aprende de los errores: cada 'desliz' financiero es una oportunidad de aprendizaje" },
                new() { Id = 54, Category = "Psicología", Difficulty = "Advanced", Advice = "🧘 Practica mindfulness financiero: 5 minutos diarios de conciencia sobre tus hábitos de gasto" },
                new() { Id = 55, Category = "Psicología", Difficulty = "Intermediate", Advice = "🎯 Enfócate en el progreso, no en la perfección: pequeños pasos consistentes crean grandes resultados" }
            };
        }
    }     

    // En tu IFinanceService - modelos básicos
    public class ChartData
    {
        public string Type { get; set; } = "line";
        public string[] Labels { get; set; } = Array.Empty<string>();
        public List<ChartDataset> Datasets { get; set; } = new();
        // Solo mantener options básicos
        public ChartOptions Options { get; set; } = new();
    }

    public class ChartDataset
    {
        public string Label { get; set; } = string.Empty;
        public decimal[] Data { get; set; } = Array.Empty<decimal>();
        public string[] BackgroundColor { get; set; } = Array.Empty<string>();
        public string[] BorderColor { get; set; } = Array.Empty<string>();
        public int BorderWidth { get; set; } = 2;
        public bool Fill { get; set; } = false;
        public decimal? Tension { get; set; } = 0.4m;
        
        // Propiedades para puntos (opcionales)
        public string[] PointBackgroundColor { get; set; } = Array.Empty<string>();
        public string[] PointBorderColor { get; set; } = Array.Empty<string>();
        public int PointBorderWidth { get; set; } = 2;
        public int PointRadius { get; set; } = 3;
        public int PointHoverRadius { get; set; } = 5;
        public string PointStyle { get; set; } = "circle";
        public int[] BorderDash { get; set; } = Array.Empty<int>();
    }

    public class ChartOptions
    {
        public bool Responsive { get; set; } = true;
        public bool MaintainAspectRatio { get; set; } = false;
        public ChartPlugins Plugins { get; set; } = new();
    }

    public class ChartPlugins
    {
        public ChartLegend Legend { get; set; } = new();
    }

    public class ChartLegend
    {
        public bool Display { get; set; } = true;
        public string Position { get; set; } = "top";
    }
}