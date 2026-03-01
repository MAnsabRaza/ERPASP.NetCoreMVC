using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class updateJournalEnteryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "customerId",
                table: "JournalEntry",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "venderId",
                table: "JournalEntry",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_customerId",
                table: "JournalEntry",
                column: "customerId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_venderId",
                table: "JournalEntry",
                column: "venderId");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntry_Customer_customerId",
                table: "JournalEntry",
                column: "customerId",
                principalTable: "Customer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntry_Vender_venderId",
                table: "JournalEntry",
                column: "venderId",
                principalTable: "Vender",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntry_Customer_customerId",
                table: "JournalEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntry_Vender_venderId",
                table: "JournalEntry");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntry_customerId",
                table: "JournalEntry");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntry_venderId",
                table: "JournalEntry");

            migrationBuilder.DropColumn(
                name: "customerId",
                table: "JournalEntry");

            migrationBuilder.DropColumn(
                name: "venderId",
                table: "JournalEntry");
        }
    }
}
