using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthRecord.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Email = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HeightCm = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    BloodType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChronicConditions = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Allergies = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefreshToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefreshTokenExpiry = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmergencyContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Relationship = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyContacts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NhiImportLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataDate = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateRangeStart = table.Column<DateOnly>(type: "date", nullable: false),
                    DateRangeEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    HealthRecordCount = table.Column<int>(type: "int", nullable: false),
                    MedicationCount = table.Column<int>(type: "int", nullable: false),
                    LabCount = table.Column<int>(type: "int", nullable: false),
                    SkippedLabs = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhiImportLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NhiImportLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HealthRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClinicDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Hospital = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HospitalCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VisitSeq = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrimaryIcdCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrimaryDiagnosis = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecondaryDiagnoses = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Copay = table.Column<int>(type: "int", nullable: true),
                    TotalPoints = table.Column<int>(type: "int", nullable: true),
                    Source = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NhiImportLogId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthRecords_NhiImportLogs_NhiImportLogId",
                        column: x => x.NhiImportLogId,
                        principalTable: "NhiImportLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HealthRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BloodPressureDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    HealthRecordId = table.Column<int>(type: "int", nullable: true),
                    NhiImportLogId = table.Column<int>(type: "int", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Systolic = table.Column<int>(type: "int", nullable: false),
                    Diastolic = table.Column<int>(type: "int", nullable: false),
                    Pulse = table.Column<int>(type: "int", nullable: false),
                    MeasurementPosition = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloodPressureDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloodPressureDetails_HealthRecords_HealthRecordId",
                        column: x => x.HealthRecordId,
                        principalTable: "HealthRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BloodPressureDetails_NhiImportLogs_NhiImportLogId",
                        column: x => x.NhiImportLogId,
                        principalTable: "NhiImportLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BloodPressureDetails_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LabResultDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    HealthRecordId = table.Column<int>(type: "int", nullable: true),
                    NhiImportLogId = table.Column<int>(type: "int", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ItemName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemCode = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Unit = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalMin = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    NormalMax = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    IsNumeric = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ValueNumeric = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    ValueText = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAbnormal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NhiCode = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NhiItemName = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NhiRawValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NhiRawRange = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResultDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabResultDetails_HealthRecords_HealthRecordId",
                        column: x => x.HealthRecordId,
                        principalTable: "HealthRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LabResultDetails_NhiImportLogs_NhiImportLogId",
                        column: x => x.NhiImportLogId,
                        principalTable: "NhiImportLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LabResultDetails_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MedicationDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    HealthRecordId = table.Column<int>(type: "int", nullable: true),
                    NhiImportLogId = table.Column<int>(type: "int", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DrugName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NhiDrugCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Days = table.Column<int>(type: "int", nullable: true),
                    DrugType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationDetails_HealthRecords_HealthRecordId",
                        column: x => x.HealthRecordId,
                        principalTable: "HealthRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MedicationDetails_NhiImportLogs_NhiImportLogId",
                        column: x => x.NhiImportLogId,
                        principalTable: "NhiImportLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MedicationDetails_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BloodPressureDetails_HealthRecordId",
                table: "BloodPressureDetails",
                column: "HealthRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodPressureDetails_NhiImportLogId",
                table: "BloodPressureDetails",
                column: "NhiImportLogId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodPressureDetails_UserId",
                table: "BloodPressureDetails",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContacts_UserId",
                table: "EmergencyContacts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecords_NhiImportLogId",
                table: "HealthRecords",
                column: "NhiImportLogId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecords_UserId",
                table: "HealthRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultDetails_HealthRecordId",
                table: "LabResultDetails",
                column: "HealthRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultDetails_NhiCode_NhiItemName",
                table: "LabResultDetails",
                columns: new[] { "NhiCode", "NhiItemName" });

            migrationBuilder.CreateIndex(
                name: "IX_LabResultDetails_NhiImportLogId",
                table: "LabResultDetails",
                column: "NhiImportLogId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultDetails_UserId_ItemCode",
                table: "LabResultDetails",
                columns: new[] { "UserId", "ItemCode" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationDetails_HealthRecordId",
                table: "MedicationDetails",
                column: "HealthRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationDetails_NhiImportLogId",
                table: "MedicationDetails",
                column: "NhiImportLogId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationDetails_UserId",
                table: "MedicationDetails",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NhiImportLogs_UserId",
                table: "NhiImportLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloodPressureDetails");

            migrationBuilder.DropTable(
                name: "EmergencyContacts");

            migrationBuilder.DropTable(
                name: "LabResultDetails");

            migrationBuilder.DropTable(
                name: "MedicationDetails");

            migrationBuilder.DropTable(
                name: "HealthRecords");

            migrationBuilder.DropTable(
                name: "NhiImportLogs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
