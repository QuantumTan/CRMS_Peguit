using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMS_Peguit.infrastructure.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Leads",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Leads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Leads",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Leads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Leads",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Leads",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Suffix",
                table: "Leads",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Customers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Suffix",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_IsDeleted",
                table: "Leads",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_TenantId",
                table: "Leads",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_TenantId_LastName_FirstName",
                table: "Leads",
                columns: new[] { "TenantId", "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IsDeleted",
                table: "Customers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId",
                table: "Customers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_LastName_FirstName",
                table: "Customers",
                columns: new[] { "TenantId", "LastName", "FirstName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leads_IsDeleted",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_TenantId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_TenantId_LastName_FirstName",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Customers_IsDeleted",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_LastName_FirstName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Suffix",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Suffix",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Leads",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Leads",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
