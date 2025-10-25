using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LifeHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserProfileTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    website_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    social_links = table.Column<string>(type: "text", nullable: true),
                    privacy_settings = table.Column<string>(type: "text", nullable: true),
                    email_notifications = table.Column<bool>(type: "boolean", nullable: false),
                    push_notifications = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_profiles_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                column: "features",
                value: System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"seguimiento_avanzado\", \"recordatorios\"], \"finanzas\": [\"categorizacion\", \"presupuestos\"], \"comunidad\": true, \"limites\": {\"habitos\": 15, \"transacciones\": 200, \"presupuestos\": 15}}", new System.Text.Json.JsonDocumentOptions()));

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 3,
                column: "features",
                value: System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"gamificacion\", \"analisis_predictivo\"], \"finanzas\": [\"alertas_inteligentes\", \"planificacion\"], \"salud\": [\"seguimiento_completo\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"limites\": {\"habitos\": 999, \"transacciones\": 500, \"presupuestos\": 50}}", new System.Text.Json.JsonDocumentOptions()));

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 4,
                column: "features",
                value: System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"ilimitado\", \"personalizacion_avanzada\"], \"finanzas\": [\"ilimitado\", \"reportes_avanzados\"], \"salud\": [\"seguimiento_completo\", \"integracion_wearables\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"soporte\": \"prioritario\", \"limites\": {\"habitos\": 9999, \"transacciones\": 9999, \"presupuestos\": 9999}}", new System.Text.Json.JsonDocumentOptions()));

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_user_id",
                table: "user_profiles",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

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
                column: "features",
                value: System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"seguimiento_avanzado\", \"recordatorios\"], \"finanzas\": [\"categorizacion\", \"presupuestos\"], \"comunidad\": true, \"limites\": {\"habitos\": 15, \"transacciones\": 200, \"presupuestos\": 15}}", new System.Text.Json.JsonDocumentOptions()));

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 3,
                column: "features",
                value: System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"gamificacion\", \"analisis_predictivo\"], \"finanzas\": [\"alertas_inteligentes\", \"planificacion\"], \"salud\": [\"seguimiento_completo\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"limites\": {\"habitos\": 999, \"transacciones\": 500, \"presupuestos\": 50}}", new System.Text.Json.JsonDocumentOptions()));

            migrationBuilder.UpdateData(
                table: "subscription_plans",
                keyColumn: "id",
                keyValue: 4,
                column: "features",
                value: System.Text.Json.JsonDocument.Parse("{\"habitos\": [\"ilimitado\", \"personalizacion_avanzada\"], \"finanzas\": [\"ilimitado\", \"reportes_avanzados\"], \"salud\": [\"seguimiento_completo\", \"integracion_wearables\"], \"comunidad\": true, \"analiticas\": true, \"ia\": true, \"soporte\": \"prioritario\", \"limites\": {\"habitos\": 9999, \"transacciones\": 9999, \"presupuestos\": 9999}}", new System.Text.Json.JsonDocumentOptions()));
        }
    }
}
