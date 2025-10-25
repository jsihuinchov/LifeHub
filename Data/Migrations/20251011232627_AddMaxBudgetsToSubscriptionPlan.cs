using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxBudgetsToSubscriptionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxBudgets",
                table: "subscription_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "features", "MaxBudgets" },
                values: new object[] { System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"basico\"], \"finanzas\": [\"registro_basico\"], \"limites\": {\"habitos\": 3, \"transacciones\": 50}}", new System.Text.Json.JsonDocumentOptions()), 5 });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "features", "MaxBudgets" },
                values: new object[] { System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"seguimiento_avanzado\", \"recordatorios\"], \"finanzas\": [\"categorizacion\", \"presupuestos\"], \"comunidad\": true, \"limites\": {\"habitos\": 15, \"transacciones\": 200}}", new System.Text.Json.JsonDocumentOptions()), 5 });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "features", "MaxBudgets" },
                values: new object[] { System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"gamificacion\", \"analisis_predictivo\"], \"finanzas\": [\"alertas_inteligentes\", \"planificacion\"], \"salud\": [\"seguimiento_completo\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"limites\": {\"habitos\": 999, \"transacciones\": 500}}", new System.Text.Json.JsonDocumentOptions()), 5 });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "features", "MaxBudgets" },
                values: new object[] { System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"ilimitado\", \"personalizacion_avanzada\"], \"finanzas\": [\"ilimitado\", \"reportes_avanzados\"], \"salud\": [\"seguimiento_completo\", \"integracion_wearables\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"soporte\": \"prioritario\", \"limites\": {\"habitos\": 9999, \"transacciones\": 9999}}", new System.Text.Json.JsonDocumentOptions()), 5 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxBudgets",
                table: "subscription_plans");

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 1,
                column: "features",
                value: System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"basico\"], \"finanzas\": [\"registro_basico\"], \"limites\": {\"habitos\": 3, \"transacciones\": 50}}", new System.Text.Json.JsonDocumentOptions()));

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 2,
                column: "features",
                value: System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"seguimiento_avanzado\", \"recordatorios\"], \"finanzas\": [\"categorizacion\", \"presupuestos\"], \"comunidad\": true, \"limites\": {\"habitos\": 15, \"transacciones\": 200}}", new System.Text.Json.JsonDocumentOptions()));

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 3,
                column: "features",
                value: System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"gamificacion\", \"analisis_predictivo\"], \"finanzas\": [\"alertas_inteligentes\", \"planificacion\"], \"salud\": [\"seguimiento_completo\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"limites\": {\"habitos\": 999, \"transacciones\": 500}}", new System.Text.Json.JsonDocumentOptions()));

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 4,
                column: "features",
                value: System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"ilimitado\", \"personalizacion_avanzada\"], \"finanzas\": [\"ilimitado\", \"reportes_avanzados\"], \"salud\": [\"seguimiento_completo\", \"integracion_wearables\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"soporte\": \"prioritario\", \"limites\": {\"habitos\": 9999, \"transacciones\": 9999}}", new System.Text.Json.JsonDocumentOptions()));
        }
    }
}
