using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tickbox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingEmailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingEmail",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PendingEmailExpiresAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingEmailTokenHash",
                table: "Users",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OidcAuthorizationRequests",
                columns: table => new
                {
                    State = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CodeVerifier = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OidcAuthorizationRequests", x => x.State);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OidcAuthorizationRequests");

            migrationBuilder.DropColumn(
                name: "PendingEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PendingEmailExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PendingEmailTokenHash",
                table: "Users");
        }
    }
}
