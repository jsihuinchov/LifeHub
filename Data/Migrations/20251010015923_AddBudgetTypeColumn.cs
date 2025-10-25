using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetTypeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_AspNetUsers_UserId",
                table: "UserSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_SubscriptionPlan_PlanId",
                table: "UserSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserSubscriptions",
                table: "UserSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubscriptionPlan",
                table: "SubscriptionPlan");

            migrationBuilder.RenameTable(
                name: "UserSubscriptions",
                newName: "user_subscriptions");

            migrationBuilder.RenameTable(
                name: "SubscriptionPlan",
                newName: "subscription_plans");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "user_subscriptions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "user_subscriptions",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "user_subscriptions",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "PlanId",
                table: "user_subscriptions",
                newName: "plan_id");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "user_subscriptions",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "user_subscriptions",
                newName: "end_date");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "user_subscriptions",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "user_subscriptions",
                newName: "IX_user_subscriptions_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_UserSubscriptions_PlanId",
                table: "user_subscriptions",
                newName: "IX_user_subscriptions_plan_id");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "subscription_plans",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "subscription_plans",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Features",
                table: "subscription_plans",
                newName: "features");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "subscription_plans",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "StorageMB",
                table: "subscription_plans",
                newName: "storage_mb");

            migrationBuilder.RenameColumn(
                name: "SortOrder",
                table: "subscription_plans",
                newName: "sort_order");

            migrationBuilder.RenameColumn(
                name: "ShortDescription",
                table: "subscription_plans",
                newName: "short_description");

            migrationBuilder.RenameColumn(
                name: "MaxTransactions",
                table: "subscription_plans",
                newName: "max_transactions");

            migrationBuilder.RenameColumn(
                name: "MaxHabits",
                table: "subscription_plans",
                newName: "max_habits");

            migrationBuilder.RenameColumn(
                name: "LongDescription",
                table: "subscription_plans",
                newName: "long_description");

            migrationBuilder.RenameColumn(
                name: "IsFeatured",
                table: "subscription_plans",
                newName: "is_featured");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "subscription_plans",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "HasCommunityAccess",
                table: "subscription_plans",
                newName: "has_community_access");

            migrationBuilder.RenameColumn(
                name: "HasAdvancedAnalytics",
                table: "subscription_plans",
                newName: "has_advanced_analytics");

            migrationBuilder.RenameColumn(
                name: "HasAIFeatures",
                table: "subscription_plans",
                newName: "has_ai_features");

            migrationBuilder.RenameColumn(
                name: "DurationDays",
                table: "subscription_plans",
                newName: "duration_days");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "subscription_plans",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ColorCode",
                table: "subscription_plans",
                newName: "color_code");

            migrationBuilder.AddColumn<int>(
                name: "BudgetType",
                table: "Budgets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_subscriptions",
                table: "user_subscriptions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_subscription_plans",
                table: "subscription_plans",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_subscriptions_AspNetUsers_user_id",
                table: "user_subscriptions",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_subscriptions_subscription_plans_plan_id",
                table: "user_subscriptions",
                column: "plan_id",
                principalTable: "subscription_plans",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_subscriptions_AspNetUsers_user_id",
                table: "user_subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_user_subscriptions_subscription_plans_plan_id",
                table: "user_subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_subscriptions",
                table: "user_subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_subscription_plans",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "BudgetType",
                table: "Budgets");

            migrationBuilder.RenameTable(
                name: "user_subscriptions",
                newName: "UserSubscriptions");

            migrationBuilder.RenameTable(
                name: "subscription_plans",
                newName: "SubscriptionPlan");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "UserSubscriptions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "UserSubscriptions",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "start_date",
                table: "UserSubscriptions",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "plan_id",
                table: "UserSubscriptions",
                newName: "PlanId");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "UserSubscriptions",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "end_date",
                table: "UserSubscriptions",
                newName: "EndDate");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "UserSubscriptions",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_user_subscriptions_user_id",
                table: "UserSubscriptions",
                newName: "IX_UserSubscriptions_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_user_subscriptions_plan_id",
                table: "UserSubscriptions",
                newName: "IX_UserSubscriptions_PlanId");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "SubscriptionPlan",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "SubscriptionPlan",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "features",
                table: "SubscriptionPlan",
                newName: "Features");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "SubscriptionPlan",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "storage_mb",
                table: "SubscriptionPlan",
                newName: "StorageMB");

            migrationBuilder.RenameColumn(
                name: "sort_order",
                table: "SubscriptionPlan",
                newName: "SortOrder");

            migrationBuilder.RenameColumn(
                name: "short_description",
                table: "SubscriptionPlan",
                newName: "ShortDescription");

            migrationBuilder.RenameColumn(
                name: "max_transactions",
                table: "SubscriptionPlan",
                newName: "MaxTransactions");

            migrationBuilder.RenameColumn(
                name: "max_habits",
                table: "SubscriptionPlan",
                newName: "MaxHabits");

            migrationBuilder.RenameColumn(
                name: "long_description",
                table: "SubscriptionPlan",
                newName: "LongDescription");

            migrationBuilder.RenameColumn(
                name: "is_featured",
                table: "SubscriptionPlan",
                newName: "IsFeatured");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "SubscriptionPlan",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "has_community_access",
                table: "SubscriptionPlan",
                newName: "HasCommunityAccess");

            migrationBuilder.RenameColumn(
                name: "has_ai_features",
                table: "SubscriptionPlan",
                newName: "HasAIFeatures");

            migrationBuilder.RenameColumn(
                name: "has_advanced_analytics",
                table: "SubscriptionPlan",
                newName: "HasAdvancedAnalytics");

            migrationBuilder.RenameColumn(
                name: "duration_days",
                table: "SubscriptionPlan",
                newName: "DurationDays");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "SubscriptionPlan",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "color_code",
                table: "SubscriptionPlan",
                newName: "ColorCode");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserSubscriptions",
                table: "UserSubscriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubscriptionPlan",
                table: "SubscriptionPlan",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_AspNetUsers_UserId",
                table: "UserSubscriptions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_SubscriptionPlan_PlanId",
                table: "UserSubscriptions",
                column: "PlanId",
                principalTable: "SubscriptionPlan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
