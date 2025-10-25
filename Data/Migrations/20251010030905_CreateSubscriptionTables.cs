using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LifeHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateSubscriptionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "subscription_plans",
                columns: new[] { "id", "color_code", "created_at", "duration_days", "features", "has_ai_features", "has_advanced_analytics", "has_community_access", "is_active", "is_featured", "long_description", "max_habits", "max_transactions", "name", "price", "short_description", "sort_order", "storage_mb" },
                values: new object[,]
                {
                    { 1, "#6B7280", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"basico\"], \"finanzas\": [\"registro_basico\"], \"limites\": {\"habitos\": 3, \"transacciones\": 50}}", new System.Text.Json.JsonDocumentOptions()), false, false, false, true, false, "Perfecto para empezar a organizar tu vida sin compromiso", 3, 50, "Plan Gratis", 0.00m, "Comienza tu viaje", 1, 10 },
                    { 2, "#10B981", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"seguimiento_avanzado\", \"recordatorios\"], \"finanzas\": [\"categorizacion\", \"presupuestos\"], \"comunidad\": true, \"limites\": {\"habitos\": 15, \"transacciones\": 200}}", new System.Text.Json.JsonDocumentOptions()), true, false, true, true, true, "Todo lo necesario para mejorar tus hábitos y finanzas de manera efectiva", 15, 200, "Vida Básica", 4.99m, "Organización esencial", 2, 100 },
                    { 3, "#8B5CF6", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"gamificacion\", \"analisis_predictivo\"], \"finanzas\": [\"alertas_inteligentes\", \"planificacion\"], \"salud\": [\"seguimiento_completo\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"limites\": {\"habitos\": 999, \"transacciones\": 500}}", new System.Text.Json.JsonDocumentOptions()), true, true, true, true, true, "Herramientas avanzadas para todos los aspectos de tu vida con análisis profundos", 999, 500, "Vida Premium", 9.99m, "Transformación completa", 3, 500 },
                    { 4, "#F59E0B", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"ilimitado\", \"personalizacion_avanzada\"], \"finanzas\": [\"ilimitado\", \"reportes_avanzados\"], \"salud\": [\"seguimiento_completo\", \"integracion_wearables\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"soporte\": \"prioritario\", \"limites\": {\"habitos\": 9999, \"transacciones\": 9999}}", new System.Text.Json.JsonDocumentOptions()), true, true, true, true, false, "Todo ilimitado con soporte prioritario y funciones exclusivas para máxima productividad", 9999, 9999, "Vida Máxima", 14.99m, "Experiencia premium total", 4, 1000 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 4);
        }
    }
}
