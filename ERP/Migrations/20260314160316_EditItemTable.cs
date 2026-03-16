using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class EditItemTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "Item");

            migrationBuilder.AddColumn<decimal>(
                name: "purchase_dic",
                table: "Item",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sale_dic",
                table: "Item",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "purchase_dic",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "sale_dic",
                table: "Item");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "Item",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
