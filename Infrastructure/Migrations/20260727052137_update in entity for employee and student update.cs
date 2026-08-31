using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateinentityforemployeeandstudentupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                schema: "dbo",
                table: "students",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_students_user_id",
                schema: "dbo",
                table: "students",
                column: "user_id",
                unique: true,
                filter: "user_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_students_user_id",
                schema: "dbo",
                table: "students");

            migrationBuilder.DropColumn(
                name: "user_id",
                schema: "dbo",
                table: "students");
        }
    }
}
