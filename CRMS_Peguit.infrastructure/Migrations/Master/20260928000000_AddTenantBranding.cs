using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMS_Peguit.infrastructure.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddTenantBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantBrandings",
                columns: table => new
                {
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LogoImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LogoVersion = table.Column<int>(type: "int", nullable: false),
                    AccentColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HidePoweredBy = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantBrandings", x => x.CompanyId);
                    table.ForeignKey(
                        name: "FK_TenantBrandings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Backfill existing tenants with DisplayName = current company name
            migrationBuilder.Sql(@"
                INSERT INTO TenantBrandings (CompanyId, DisplayName, LogoVersion, HidePoweredBy, UpdatedAt)
                SELECT c.CompanyId, c.CompanyName, 1, 0, GETUTCDATE()
                FROM Companies c
                WHERE NOT EXISTS (SELECT 1 FROM TenantBrandings b WHERE b.CompanyId = c.CompanyId);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TenantBrandings");
        }
    }
}
