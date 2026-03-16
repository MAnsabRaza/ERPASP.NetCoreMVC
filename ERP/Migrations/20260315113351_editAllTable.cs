using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class editAllTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Component_Module_moduleId",
                table: "Component");

            migrationBuilder.DropForeignKey(
                name: "FK_Item_Brand_brandId",
                table: "Item");

            migrationBuilder.DropForeignKey(
                name: "FK_Item_SubCategory_subCategoryId",
                table: "Item");

            migrationBuilder.DropForeignKey(
                name: "FK_Item_UOM_uomId",
                table: "Item");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntry_Customer_customerId",
                table: "JournalEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntry_Vender_venderId",
                table: "JournalEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_Ledger_ChartOfAccount_chartOfAccountId",
                table: "Ledger");

            migrationBuilder.DropForeignKey(
                name: "FK_Ledger_Company_companyId",
                table: "Ledger");

            migrationBuilder.DropForeignKey(
                name: "FK_Ledger_JournalEntry_journalEntryId",
                table: "Ledger");

            migrationBuilder.DropForeignKey(
                name: "FK_Permission_Component_componentId",
                table: "Permission");

            migrationBuilder.DropForeignKey(
                name: "FK_Permission_Role_roleId",
                table: "Permission");

            migrationBuilder.DropForeignKey(
                name: "FK_StockDetail_Item_itemId",
                table: "StockDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMaster_Company_companyId",
                table: "StockMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMaster_Customer_customerId",
                table: "StockMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMaster_Transporter_transporterId",
                table: "StockMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMaster_Vender_venderId",
                table: "StockMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_SubCategory_Category_categoryId",
                table: "SubCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Company_companyId",
                table: "User");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Bank",
                newName: "branch_code");

            migrationBuilder.AddColumn<int>(
                name: "companyId",
                table: "UOM",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "companyId",
                table: "Transporter",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "companyId",
                table: "SubCategory",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "fiscalYearId",
                table: "StockMaster",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_status",
                table: "StockMaster",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "voucher_no",
                table: "StockMaster",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "taxSetupId",
                table: "StockDetail",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cheque_date",
                table: "PaymentVoucher",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cheque_no",
                table: "PaymentVoucher",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "journal_entryId",
                table: "PaymentVoucher",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "payment_type",
                table: "PaymentVoucher",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "fiscalYearId",
                table: "JournalEntry",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "max_stock_level",
                table: "Item",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "min_stock_level",
                table: "Item",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_type",
                table: "Customer",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "companyId",
                table: "Category",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "companyId",
                table: "Brand",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "account_title",
                table: "Bank",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FinancialYear",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateTime>(type: "date", nullable: false),
                    year_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    start_date = table.Column<DateTime>(type: "date", nullable: false),
                    end_date = table.Column<DateTime>(type: "date", nullable: false),
                    companyId = table.Column<int>(type: "int", nullable: true),
                    userId = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialYear", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialYear_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinancialYear_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaxSetup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tax_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    percentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    applicable_on = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    companyId = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxSetup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxSetup_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UOM_companyId",
                table: "UOM",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_Transporter_companyId",
                table: "Transporter",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_SubCategory_companyId",
                table: "SubCategory",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMaster_fiscalYearId",
                table: "StockMaster",
                column: "fiscalYearId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDetail_taxSetupId",
                table: "StockDetail",
                column: "taxSetupId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVoucher_journal_entryId",
                table: "PaymentVoucher",
                column: "journal_entryId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_fiscalYearId",
                table: "JournalEntry",
                column: "fiscalYearId");

            migrationBuilder.CreateIndex(
                name: "IX_Category_companyId",
                table: "Category",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_Brand_companyId",
                table: "Brand",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialYear_companyId",
                table: "FinancialYear",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialYear_userId",
                table: "FinancialYear",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxSetup_companyId",
                table: "TaxSetup",
                column: "companyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Brand_Company_companyId",
                table: "Brand",
                column: "companyId",
                principalTable: "Company",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Category_Company_companyId",
                table: "Category",
                column: "companyId",
                principalTable: "Company",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Component_Module_moduleId",
                table: "Component",
                column: "moduleId",
                principalTable: "Module",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Item_Brand_brandId",
                table: "Item",
                column: "brandId",
                principalTable: "Brand",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Item_SubCategory_subCategoryId",
                table: "Item",
                column: "subCategoryId",
                principalTable: "SubCategory",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Item_UOM_uomId",
                table: "Item",
                column: "uomId",
                principalTable: "UOM",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntry_Customer_customerId",
                table: "JournalEntry",
                column: "customerId",
                principalTable: "Customer",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntry_FinancialYear_fiscalYearId",
                table: "JournalEntry",
                column: "fiscalYearId",
                principalTable: "FinancialYear",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntry_Vender_venderId",
                table: "JournalEntry",
                column: "venderId",
                principalTable: "Vender",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Ledger_ChartOfAccount_chartOfAccountId",
                table: "Ledger",
                column: "chartOfAccountId",
                principalTable: "ChartOfAccount",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Ledger_Company_companyId",
                table: "Ledger",
                column: "companyId",
                principalTable: "Company",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Ledger_JournalEntry_journalEntryId",
                table: "Ledger",
                column: "journalEntryId",
                principalTable: "JournalEntry",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentVoucher_JournalEntry_journal_entryId",
                table: "PaymentVoucher",
                column: "journal_entryId",
                principalTable: "JournalEntry",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Permission_Component_componentId",
                table: "Permission",
                column: "componentId",
                principalTable: "Component",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Permission_Role_roleId",
                table: "Permission",
                column: "roleId",
                principalTable: "Role",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockDetail_Item_itemId",
                table: "StockDetail",
                column: "itemId",
                principalTable: "Item",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockDetail_TaxSetup_taxSetupId",
                table: "StockDetail",
                column: "taxSetupId",
                principalTable: "TaxSetup",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMaster_Company_companyId",
                table: "StockMaster",
                column: "companyId",
                principalTable: "Company",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMaster_Customer_customerId",
                table: "StockMaster",
                column: "customerId",
                principalTable: "Customer",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMaster_FinancialYear_fiscalYearId",
                table: "StockMaster",
                column: "fiscalYearId",
                principalTable: "FinancialYear",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMaster_Transporter_transporterId",
                table: "StockMaster",
                column: "transporterId",
                principalTable: "Transporter",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMaster_Vender_venderId",
                table: "StockMaster",
                column: "venderId",
                principalTable: "Vender",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubCategory_Category_categoryId",
                table: "SubCategory",
                column: "categoryId",
                principalTable: "Category",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubCategory_Company_companyId",
                table: "SubCategory",
                column: "companyId",
                principalTable: "Company",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transporter_Company_companyId",
                table: "Transporter",
                column: "companyId",
                principalTable: "Company",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UOM_Company_companyId",
                table: "UOM",
                column: "companyId",
                principalTable: "Company",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Company_companyId",
                table: "User",
                column: "companyId",
                principalTable: "Company",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Brand_Company_companyId",
                table: "Brand");

            migrationBuilder.DropForeignKey(
                name: "FK_Category_Company_companyId",
                table: "Category");

            migrationBuilder.DropForeignKey(
                name: "FK_Component_Module_moduleId",
                table: "Component");

            migrationBuilder.DropForeignKey(
                name: "FK_Item_Brand_brandId",
                table: "Item");

            migrationBuilder.DropForeignKey(
                name: "FK_Item_SubCategory_subCategoryId",
                table: "Item");

            migrationBuilder.DropForeignKey(
                name: "FK_Item_UOM_uomId",
                table: "Item");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntry_Customer_customerId",
                table: "JournalEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntry_FinancialYear_fiscalYearId",
                table: "JournalEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntry_Vender_venderId",
                table: "JournalEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_Ledger_ChartOfAccount_chartOfAccountId",
                table: "Ledger");

            migrationBuilder.DropForeignKey(
                name: "FK_Ledger_Company_companyId",
                table: "Ledger");

            migrationBuilder.DropForeignKey(
                name: "FK_Ledger_JournalEntry_journalEntryId",
                table: "Ledger");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentVoucher_JournalEntry_journal_entryId",
                table: "PaymentVoucher");

            migrationBuilder.DropForeignKey(
                name: "FK_Permission_Component_componentId",
                table: "Permission");

            migrationBuilder.DropForeignKey(
                name: "FK_Permission_Role_roleId",
                table: "Permission");

            migrationBuilder.DropForeignKey(
                name: "FK_StockDetail_Item_itemId",
                table: "StockDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_StockDetail_TaxSetup_taxSetupId",
                table: "StockDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMaster_Company_companyId",
                table: "StockMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMaster_Customer_customerId",
                table: "StockMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMaster_FinancialYear_fiscalYearId",
                table: "StockMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMaster_Transporter_transporterId",
                table: "StockMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMaster_Vender_venderId",
                table: "StockMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_SubCategory_Category_categoryId",
                table: "SubCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_SubCategory_Company_companyId",
                table: "SubCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_Transporter_Company_companyId",
                table: "Transporter");

            migrationBuilder.DropForeignKey(
                name: "FK_UOM_Company_companyId",
                table: "UOM");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Company_companyId",
                table: "User");

            migrationBuilder.DropTable(
                name: "FinancialYear");

            migrationBuilder.DropTable(
                name: "TaxSetup");

            migrationBuilder.DropIndex(
                name: "IX_UOM_companyId",
                table: "UOM");

            migrationBuilder.DropIndex(
                name: "IX_Transporter_companyId",
                table: "Transporter");

            migrationBuilder.DropIndex(
                name: "IX_SubCategory_companyId",
                table: "SubCategory");

            migrationBuilder.DropIndex(
                name: "IX_StockMaster_fiscalYearId",
                table: "StockMaster");

            migrationBuilder.DropIndex(
                name: "IX_StockDetail_taxSetupId",
                table: "StockDetail");

            migrationBuilder.DropIndex(
                name: "IX_PaymentVoucher_journal_entryId",
                table: "PaymentVoucher");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntry_fiscalYearId",
                table: "JournalEntry");

            migrationBuilder.DropIndex(
                name: "IX_Category_companyId",
                table: "Category");

            migrationBuilder.DropIndex(
                name: "IX_Brand_companyId",
                table: "Brand");

            migrationBuilder.DropColumn(
                name: "companyId",
                table: "UOM");

            migrationBuilder.DropColumn(
                name: "companyId",
                table: "Transporter");

            migrationBuilder.DropColumn(
                name: "companyId",
                table: "SubCategory");

            migrationBuilder.DropColumn(
                name: "fiscalYearId",
                table: "StockMaster");

            migrationBuilder.DropColumn(
                name: "payment_status",
                table: "StockMaster");

            migrationBuilder.DropColumn(
                name: "voucher_no",
                table: "StockMaster");

            migrationBuilder.DropColumn(
                name: "taxSetupId",
                table: "StockDetail");

            migrationBuilder.DropColumn(
                name: "cheque_date",
                table: "PaymentVoucher");

            migrationBuilder.DropColumn(
                name: "cheque_no",
                table: "PaymentVoucher");

            migrationBuilder.DropColumn(
                name: "journal_entryId",
                table: "PaymentVoucher");

            migrationBuilder.DropColumn(
                name: "payment_type",
                table: "PaymentVoucher");

            migrationBuilder.DropColumn(
                name: "fiscalYearId",
                table: "JournalEntry");

            migrationBuilder.DropColumn(
                name: "max_stock_level",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "min_stock_level",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "customer_type",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "companyId",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "companyId",
                table: "Brand");

            migrationBuilder.DropColumn(
                name: "account_title",
                table: "Bank");

            migrationBuilder.RenameColumn(
                name: "branch_code",
                table: "Bank",
                newName: "name");

            migrationBuilder.AddForeignKey(
                name: "FK_Component_Module_moduleId",
                table: "Component",
                column: "moduleId",
                principalTable: "Module",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Item_Brand_brandId",
                table: "Item",
                column: "brandId",
                principalTable: "Brand",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Item_SubCategory_subCategoryId",
                table: "Item",
                column: "subCategoryId",
                principalTable: "SubCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Item_UOM_uomId",
                table: "Item",
                column: "uomId",
                principalTable: "UOM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Ledger_ChartOfAccount_chartOfAccountId",
                table: "Ledger",
                column: "chartOfAccountId",
                principalTable: "ChartOfAccount",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ledger_Company_companyId",
                table: "Ledger",
                column: "companyId",
                principalTable: "Company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ledger_JournalEntry_journalEntryId",
                table: "Ledger",
                column: "journalEntryId",
                principalTable: "JournalEntry",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Permission_Component_componentId",
                table: "Permission",
                column: "componentId",
                principalTable: "Component",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Permission_Role_roleId",
                table: "Permission",
                column: "roleId",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockDetail_Item_itemId",
                table: "StockDetail",
                column: "itemId",
                principalTable: "Item",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMaster_Company_companyId",
                table: "StockMaster",
                column: "companyId",
                principalTable: "Company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMaster_Customer_customerId",
                table: "StockMaster",
                column: "customerId",
                principalTable: "Customer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMaster_Transporter_transporterId",
                table: "StockMaster",
                column: "transporterId",
                principalTable: "Transporter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMaster_Vender_venderId",
                table: "StockMaster",
                column: "venderId",
                principalTable: "Vender",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubCategory_Category_categoryId",
                table: "SubCategory",
                column: "categoryId",
                principalTable: "Category",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Company_companyId",
                table: "User",
                column: "companyId",
                principalTable: "Company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
