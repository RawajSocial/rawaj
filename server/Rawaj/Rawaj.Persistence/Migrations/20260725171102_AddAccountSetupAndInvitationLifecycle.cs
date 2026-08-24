using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rawaj.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountSetupAndInvitationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_setups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    CompletedSteps = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_setups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_account_setups_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_setups_UserId",
                table: "account_setups",
                column: "UserId",
                unique: true);

            // Every user who already exists necessarily already completed registration (the only
            // "account setup" step that exists today) — backfill so pre-existing invited members
            // aren't wrongly gated by a row that simply didn't exist yet when they signed up.
            migrationBuilder.Sql(
                "INSERT INTO account_setups (Id, UserId, CurrentStep, CompletedSteps, CompletedAt, CreatedAt) " +
                "SELECT NEWID(), u.Id, 1, 'registration', u.CreatedAt, u.CreatedAt FROM users u;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_setups");
        }
    }
}
