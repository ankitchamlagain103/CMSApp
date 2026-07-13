using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedminorchangesinclasssubjectstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_class_subjects_class_subject",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.AddColumn<Guid>(
                name: "class_section_id",
                schema: "dbo",
                table: "class_subjects",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_class_subjects_class_section_id",
                schema: "dbo",
                table: "class_subjects",
                column: "class_section_id");

            migrationBuilder.CreateIndex(
                name: "ix_class_subjects_class_subject_section",
                schema: "dbo",
                table: "class_subjects",
                columns: new[] { "academic_class_id", "subject_code", "class_section_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_class_subjects_class_sections_class_section_id",
                schema: "dbo",
                table: "class_subjects",
                column: "class_section_id",
                principalSchema: "dbo",
                principalTable: "class_sections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_class_subjects_class_sections_class_section_id",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropIndex(
                name: "IX_class_subjects_class_section_id",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropIndex(
                name: "ix_class_subjects_class_subject_section",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropColumn(
                name: "class_section_id",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.CreateIndex(
                name: "ix_class_subjects_class_subject",
                schema: "dbo",
                table: "class_subjects",
                columns: new[] { "academic_class_id", "subject_code" },
                unique: true);
        }
    }
}
