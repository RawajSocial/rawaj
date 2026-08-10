using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeFieldsToBillingTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeEventId",
                table: "billing_transactions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSessionId",
                table: "billing_transactions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_billing_transactions_StripeEventId",
                table: "billing_transactions",
                column: "StripeEventId",
                unique: true,
                filter: "[StripeEventId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_billing_transactions_StripeEventId",
                table: "billing_transactions");

            migrationBuilder.DropColumn(
                name: "StripeEventId",
                table: "billing_transactions");

            migrationBuilder.DropColumn(
                name: "StripeSessionId",
                table: "billing_transactions");
        }
    }
}
