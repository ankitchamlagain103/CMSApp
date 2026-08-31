using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedchangesinentityforupdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "verification_remarks",
                schema: "dbo",
                table: "employee_qualifications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "verification_status",
                schema: "dbo",
                table: "employee_qualifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "verified_by",
                schema: "dbo",
                table: "employee_qualifications",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verified_ts",
                schema: "dbo",
                table: "employee_qualifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verification_remarks",
                schema: "dbo",
                table: "employee_documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "verification_status",
                schema: "dbo",
                table: "employee_documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "verified_by",
                schema: "dbo",
                table: "employee_documents",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verified_ts",
                schema: "dbo",
                table: "employee_documents",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "verification_remarks",
                schema: "dbo",
                table: "employee_qualifications");

            migrationBuilder.DropColumn(
                name: "verification_status",
                schema: "dbo",
                table: "employee_qualifications");

            migrationBuilder.DropColumn(
                name: "verified_by",
                schema: "dbo",
                table: "employee_qualifications");

            migrationBuilder.DropColumn(
                name: "verified_ts",
                schema: "dbo",
                table: "employee_qualifications");

            migrationBuilder.DropColumn(
                name: "verification_remarks",
                schema: "dbo",
                table: "employee_documents");

            migrationBuilder.DropColumn(
                name: "verification_status",
                schema: "dbo",
                table: "employee_documents");

            migrationBuilder.DropColumn(
                name: "verified_by",
                schema: "dbo",
                table: "employee_documents");

            migrationBuilder.DropColumn(
                name: "verified_ts",
                schema: "dbo",
                table: "employee_documents");
        }
    }
}
