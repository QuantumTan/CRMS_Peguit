using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMS_Peguit.infrastructure.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddPaymentRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    PaymentRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionId = table.Column<int>(type: "int", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.PaymentRecordId);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "SubscriptionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_SuperAdmins_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "SuperAdmins",
                        principalColumn: "SuperAdminId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatformAuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PerformedBySuperAdminId = table.Column<int>(type: "int", nullable: false),
                    PerformedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TargetCompanyId = table.Column<int>(type: "int", nullable: true),
                    TargetCompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformAuditLogs", x => x.AuditLogId);
                    table.ForeignKey(
                        name: "FK_PlatformAuditLogs_SuperAdmins_PerformedBySuperAdminId",
                        column: x => x.PerformedBySuperAdminId,
                        principalTable: "SuperAdmins",
                        principalColumn: "SuperAdminId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_PaymentDate",
                table: "PaymentRecords",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_PaymentReference",
                table: "PaymentRecords",
                column: "PaymentReference");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_RecordedByUserId",
                table: "PaymentRecords",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_SubscriptionId",
                table: "PaymentRecords",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditLogs_ActionType",
                table: "PlatformAuditLogs",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditLogs_CreatedAt",
                table: "PlatformAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditLogs_PerformedBySuperAdminId",
                table: "PlatformAuditLogs",
                column: "PerformedBySuperAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditLogs_TargetCompanyId",
                table: "PlatformAuditLogs",
                column: "TargetCompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "PlatformAuditLogs");
        }
    }
}
