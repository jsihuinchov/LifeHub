using Microsoft.ML.Data;

namespace LifeHub.Models.IA.DataModels
{
    // Modelo para entrenamiento de detección de anomalías
    public class FinancialTransactionData
    {
        [LoadColumn(0)]
        public float Amount { get; set; }

        [LoadColumn(1)]
        public string Category { get; set; } = string.Empty;

        [LoadColumn(2)]
        public int DayOfWeek { get; set; }

        [LoadColumn(3)]
        public int DayOfMonth { get; set; }

        [LoadColumn(4)]
        public int Month { get; set; }

        [LoadColumn(5)]
        public bool IsWeekend { get; set; }

        [LoadColumn(6)]
        public float Label { get; set; } // 0 = normal, 1 = anomalía
    }

    // Modelo para predicción de gastos
    public class SpendingPredictionData
    {
        [LoadColumn(0)]
        public float Month { get; set; }

        [LoadColumn(1)]
        public float Year { get; set; }

        [LoadColumn(2)]
        public string Category { get; set; } = string.Empty;

        [LoadColumn(3)]
        public float PreviousMonthSpending { get; set; }

        [LoadColumn(4)]
        public float AverageSpending { get; set; }

        [LoadColumn(5)]
        public float Label { get; set; } // Cantidad a predecir
    }

    // Output de predicciones
    public class TransactionPrediction
    {
        [ColumnName("PredictedLabel")]
        public float Amount { get; set; }

        public float[] Score { get; set; } = Array.Empty<float>();
    }
}