using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class addNewTabl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JournalDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    journalEntryId = table.Column<int>(type: "int", nullable: false),
                    chartOfAccountId = table.Column<int>(type: "int", nullable: false),
                    debit_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    credit_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalDetail_ChartOfAccount_chartOfAccountId",
                        column: x => x.chartOfAccountId,
                        principalTable: "ChartOfAccount",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JournalDetail_JournalEntry_journalEntryId",
                        column: x => x.journalEntryId,
                        principalTable: "JournalEntry",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalDetail_chartOfAccountId",
                table: "JournalDetail",
                column: "chartOfAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalDetail_journalEntryId",
                table: "JournalDetail",
                column: "journalEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JournalDetail");
        }
    }
}
