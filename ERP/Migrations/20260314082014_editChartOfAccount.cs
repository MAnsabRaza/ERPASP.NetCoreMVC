using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class editChartOfAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChartOfAccount_ChartOfAccount_parentAccountId",
                table: "ChartOfAccount");

            migrationBuilder.DropIndex(
                name: "IX_ChartOfAccount_parentAccountId",
                table: "ChartOfAccount");

            migrationBuilder.DropColumn(
                name: "parentAccountId",
                table: "ChartOfAccount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "parentAccountId",
                table: "ChartOfAccount",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccount_parentAccountId",
                table: "ChartOfAccount",
                column: "parentAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChartOfAccount_ChartOfAccount_parentAccountId",
                table: "ChartOfAccount",
                column: "parentAccountId",
                principalTable: "ChartOfAccount",
                principalColumn: "Id");
        }
    }
}
