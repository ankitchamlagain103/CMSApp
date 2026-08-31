using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removedinvigilator_employee_idfromexam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exams_employees_invigilator_employee_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropIndex(
                name: "IX_exams_invigilator_employee_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "invigilator_employee_id",
                schema: "dbo",
                table: "exams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "invigilator_employee_id",
                schema: "dbo",
                table: "exams",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_exams_invigilator_employee_id",
                schema: "dbo",
                table: "exams",
                column: "invigilator_employee_id");

            migrationBuilder.AddForeignKey(
                name: "FK_exams_employees_invigilator_employee_id",
                schema: "dbo",
                table: "exams",
                column: "invigilator_employee_id",
                principalSchema: "dbo",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
