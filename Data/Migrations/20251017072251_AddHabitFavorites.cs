using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FavoriteOrder",
                table: "Habits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Habits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 1,
                column: "features",
                value: System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"basico\"], \"finanzas\": [\"registro_basico\"], \"limites\": {\"habitos\": 3, \"transacciones\": 50, \"presupuestos\": 5}}", new System.Text.Json.JsonDocumentOptions()));

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "features", "MaxBudgets" },
                values: new object[] { System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"seguimiento_avanzado\", \"recordatorios\"], \"finanzas\": [\"categorizacion\", \"presupuestos\"], \"comunidad\": true, \"limites\": {\"habitos\": 15, \"transacciones\": 200, \"presupuestos\": 15}}", new System.Text.Json.JsonDocumentOptions()), 15 });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "features", "MaxBudgets" },
                values: new object[] { System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"gamificacion\", \"analisis_predictivo\"], \"finanzas\": [\"alertas_inteligentes\", \"planificacion\"], \"salud\": [\"seguimiento_completo\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"limites\": {\"habitos\": 999, \"transacciones\": 500, \"presupuestos\": 50}}", new System.Text.Json.JsonDocumentOptions()), 50 });

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "features", "MaxBudgets" },
                values: new object[] { System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"ilimitado\", \"personalizacion_avanzada\"], \"finanzas\": [\"ilimitado\", \"reportes_avanzados\"], \"salud\": [\"seguimiento_completo\", \"integracion_wearables\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"soporte\": \"prioritario\", \"limites\": {\"habitos\": 9999, \"transacciones\": 9999, \"presupuestos\": 9999}}", new System.Text.Json.JsonDocumentOptions()), 9999 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FavoriteOrder",
                table: "Habits");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Habits");

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
    }
}
