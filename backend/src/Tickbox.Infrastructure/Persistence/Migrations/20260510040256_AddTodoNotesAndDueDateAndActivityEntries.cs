using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tickbox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoNotesAndDueDateAndActivityEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                table: "Todos",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Todos",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TodoActivityEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TodoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoActivityEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoActivityEntries_Todos_TodoId",
                        column: x => x.TodoId,
                        principalTable: "Todos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoActivityEntries_TodoId_OccurredAt",
                table: "TodoActivityEntries",
                columns: new[] { "TodoId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TodoActivityEntries");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Todos");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Todos");
        }
    }
}
