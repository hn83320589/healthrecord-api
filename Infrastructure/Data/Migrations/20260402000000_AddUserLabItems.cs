using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthRecord.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLabItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create UserLabItems table
            migrationBuilder.CreateTable(
                name: "UserLabItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ItemCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Unit = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "其他")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalMin = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    NormalMax = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    IsPreset = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLabItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLabItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserLabItems_UserId_ItemCode_ItemName",
                table: "UserLabItems",
                columns: new[] { "UserId", "ItemCode", "ItemName" },
                unique: true);

            // 2. LabResultDetails: copy nhi_code/nhi_item_name → item_code/item_name (for existing data)
            migrationBuilder.Sql(
                "UPDATE `LabResultDetails` SET `ItemCode` = COALESCE(`NhiCode`, `ItemCode`), " +
                "`ItemName` = COALESCE(`NhiItemName`, `ItemName`) WHERE `NhiCode` IS NOT NULL");

            // 3. Drop the old (NhiCode, NhiItemName) index
            migrationBuilder.DropIndex(
                name: "IX_LabResultDetails_NhiCode_NhiItemName",
                table: "LabResultDetails");

            // 4. Drop the old (UserId, ItemCode) index — will be recreated as (UserId, ItemCode, ItemName)
            migrationBuilder.DropIndex(
                name: "IX_LabResultDetails_UserId_ItemCode",
                table: "LabResultDetails");

            // 5. Drop NhiCode and NhiItemName columns
            migrationBuilder.DropColumn(name: "NhiCode", table: "LabResultDetails");
            migrationBuilder.DropColumn(name: "NhiItemName", table: "LabResultDetails");

            // 6. Create new composite index
            migrationBuilder.CreateIndex(
                name: "IX_LabResultDetails_UserId_ItemCode_ItemName",
                table: "LabResultDetails",
                columns: new[] { "UserId", "ItemCode", "ItemName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserLabItems");

            migrationBuilder.DropIndex(
                name: "IX_LabResultDetails_UserId_ItemCode_ItemName",
                table: "LabResultDetails");

            migrationBuilder.AddColumn<string>(
                name: "NhiCode",
                table: "LabResultDetails",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NhiItemName",
                table: "LabResultDetails",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultDetails_NhiCode_NhiItemName",
                table: "LabResultDetails",
                columns: new[] { "NhiCode", "NhiItemName" });

            migrationBuilder.CreateIndex(
                name: "IX_LabResultDetails_UserId_ItemCode",
                table: "LabResultDetails",
                columns: new[] { "UserId", "ItemCode" });
        }
    }
}
