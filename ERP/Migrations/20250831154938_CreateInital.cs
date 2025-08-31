using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Migrations
{
    /// <inheritdoc />
    public partial class CreateInital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<bool>(type: "bit", nullable: false),
                    account_name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Brand",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    brand_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    brand_description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brand", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    category_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    category_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    company_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<bool>(type: "bit", nullable: false),
                    website_path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    company_email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    zipcode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    logo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Company", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Module",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    module_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    module_icon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    moduel_href = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Module", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    role_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transporter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    transporter_no = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<bool>(type: "bit", nullable: false),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transporter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UOM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    uom_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UOM", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    categoryId = table.Column<int>(type: "int", nullable: false),
                    sub_category_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sub_category_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubCategory_Category_categoryId",
                        column: x => x.categoryId,
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    bank_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    account_no = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    opening_balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<bool>(type: "bit", nullable: false),
                    companyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bank", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bank_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChartOfAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    companyId = table.Column<int>(type: "int", nullable: false),
                    accountTypeId = table.Column<int>(type: "int", nullable: false),
                    parentAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartOfAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChartOfAccount_AccountType_accountTypeId",
                        column: x => x.accountTypeId,
                        principalTable: "AccountType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChartOfAccount_ChartOfAccount_parentAccountId",
                        column: x => x.parentAccountId,
                        principalTable: "ChartOfAccount",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChartOfAccount_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<bool>(type: "bit", nullable: false),
                    country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    credit_limit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    current_balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    companyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customer_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vender",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<bool>(type: "bit", nullable: false),
                    country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    current_balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    companyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vender", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vender_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Warehouse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    warehouse_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    warehouse_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<bool>(type: "bit", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    companyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warehouse_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Component",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    component_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    moduleId = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Component", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Component_Module_moduleId",
                        column: x => x.moduleId,
                        principalTable: "Module",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    companyId = table.Column<int>(type: "int", nullable: false),
                    roleId = table.Column<int>(type: "int", nullable: false),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    image = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_User_Role_roleId",
                        column: x => x.roleId,
                        principalTable: "Role",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Item",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    remark = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    item_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    item_barcode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<bool>(type: "bit", nullable: false),
                    categoryId = table.Column<int>(type: "int", nullable: false),
                    subCategoryId = table.Column<int>(type: "int", nullable: false),
                    uomId = table.Column<int>(type: "int", nullable: false),
                    brandId = table.Column<int>(type: "int", nullable: false),
                    qty = table.Column<int>(type: "int", nullable: false),
                    purchase_rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    sale_rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Item", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Item_Brand_brandId",
                        column: x => x.brandId,
                        principalTable: "Brand",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Item_Category_categoryId",
                        column: x => x.categoryId,
                        principalTable: "Category",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Item_SubCategory_subCategoryId",
                        column: x => x.subCategoryId,
                        principalTable: "SubCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Item_UOM_uomId",
                        column: x => x.uomId,
                        principalTable: "UOM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentVoucher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    voucher_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    companyId = table.Column<int>(type: "int", nullable: false),
                    venderId = table.Column<int>(type: "int", nullable: false),
                    bankAccountId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentVoucher", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentVoucher_Bank_bankAccountId",
                        column: x => x.bankAccountId,
                        principalTable: "Bank",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentVoucher_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentVoucher_Vender_venderId",
                        column: x => x.venderId,
                        principalTable: "Vender",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    roleId = table.Column<int>(type: "int", nullable: false),
                    moduleId = table.Column<int>(type: "int", nullable: false),
                    componentId = table.Column<int>(type: "int", nullable: false),
                    view = table.Column<bool>(type: "bit", nullable: false),
                    create = table.Column<bool>(type: "bit", nullable: false),
                    delete = table.Column<bool>(type: "bit", nullable: false),
                    edit = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permission_Component_componentId",
                        column: x => x.componentId,
                        principalTable: "Component",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Permission_Module_moduleId",
                        column: x => x.moduleId,
                        principalTable: "Module",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Permission_Role_roleId",
                        column: x => x.roleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bank_companyId",
                table: "Bank",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccount_accountTypeId",
                table: "ChartOfAccount",
                column: "accountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccount_companyId",
                table: "ChartOfAccount",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccount_parentAccountId",
                table: "ChartOfAccount",
                column: "parentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Component_moduleId",
                table: "Component",
                column: "moduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_companyId",
                table: "Customer",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_Item_brandId",
                table: "Item",
                column: "brandId");

            migrationBuilder.CreateIndex(
                name: "IX_Item_categoryId",
                table: "Item",
                column: "categoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Item_subCategoryId",
                table: "Item",
                column: "subCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Item_uomId",
                table: "Item",
                column: "uomId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVoucher_bankAccountId",
                table: "PaymentVoucher",
                column: "bankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVoucher_companyId",
                table: "PaymentVoucher",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVoucher_venderId",
                table: "PaymentVoucher",
                column: "venderId");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_componentId",
                table: "Permission",
                column: "componentId");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_moduleId",
                table: "Permission",
                column: "moduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_roleId",
                table: "Permission",
                column: "roleId");

            migrationBuilder.CreateIndex(
                name: "IX_SubCategory_categoryId",
                table: "SubCategory",
                column: "categoryId");

            migrationBuilder.CreateIndex(
                name: "IX_User_companyId",
                table: "User",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_User_roleId",
                table: "User",
                column: "roleId");

            migrationBuilder.CreateIndex(
                name: "IX_Vender_companyId",
                table: "Vender",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_companyId",
                table: "Warehouse",
                column: "companyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChartOfAccount");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "Item");

            migrationBuilder.DropTable(
                name: "PaymentVoucher");

            migrationBuilder.DropTable(
                name: "Permission");

            migrationBuilder.DropTable(
                name: "Transporter");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Warehouse");

            migrationBuilder.DropTable(
                name: "AccountType");

            migrationBuilder.DropTable(
                name: "Brand");

            migrationBuilder.DropTable(
                name: "SubCategory");

            migrationBuilder.DropTable(
                name: "UOM");

            migrationBuilder.DropTable(
                name: "Bank");

            migrationBuilder.DropTable(
                name: "Vender");

            migrationBuilder.DropTable(
                name: "Component");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Company");

            migrationBuilder.DropTable(
                name: "Module");
        }
    }
}
