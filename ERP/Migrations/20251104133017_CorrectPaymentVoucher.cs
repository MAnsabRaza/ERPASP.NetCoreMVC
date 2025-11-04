using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class CorrectPaymentVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "transporterId",
                table: "StockMaster",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "venderId",
                table: "PaymentVoucher",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "bankAccountId",
                table: "PaymentVoucher",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "customerId",
                table: "PaymentVoucher",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVoucher_customerId",
                table: "PaymentVoucher",
                column: "customerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentVoucher_Customer_customerId",
                table: "PaymentVoucher",
                column: "customerId",
                principalTable: "Customer",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentVoucher_Customer_customerId",
                table: "PaymentVoucher");

            migrationBuilder.DropIndex(
                name: "IX_PaymentVoucher_customerId",
                table: "PaymentVoucher");

            migrationBuilder.DropColumn(
                name: "customerId",
                table: "PaymentVoucher");

            migrationBuilder.AlterColumn<int>(
                name: "transporterId",
                table: "StockMaster",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "venderId",
                table: "PaymentVoucher",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "bankAccountId",
                table: "PaymentVoucher",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
