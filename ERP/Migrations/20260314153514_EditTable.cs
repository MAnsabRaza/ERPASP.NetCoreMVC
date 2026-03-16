using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class EditTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "gst",
                table: "StockDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "gst_amount",
                table: "StockDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "incl_Tax_Amount",
                table: "StockDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expiry_date",
                table: "Item",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isExpireable",
                table: "Item",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gst",
                table: "StockDetail");

            migrationBuilder.DropColumn(
                name: "gst_amount",
                table: "StockDetail");

            migrationBuilder.DropColumn(
                name: "incl_Tax_Amount",
                table: "StockDetail");

            migrationBuilder.DropColumn(
                name: "expiry_date",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "isExpireable",
                table: "Item");
        }
    }
}
