using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateonexistingcode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_enrollments_academic_classes_academic_class_id",
                schema: "dbo",
                table: "enrollments");

            migrationBuilder.DropIndex(
                name: "ix_teacher_assignments_teacher_class_subject",
                schema: "dbo",
                table: "teacher_assignments");

            migrationBuilder.DropIndex(
                name: "ix_academic_classes_year_grade_section",
                schema: "dbo",
                table: "academic_classes");

            migrationBuilder.DropColumn(
                name: "capacity",
                schema: "dbo",
                table: "academic_classes");

            migrationBuilder.DropColumn(
                name: "section_code",
                schema: "dbo",
                table: "academic_classes");

            migrationBuilder.RenameColumn(
                name: "academic_class_id",
                schema: "dbo",
                table: "enrollments",
                newName: "class_section_id");

            migrationBuilder.RenameIndex(
                name: "ix_enrollments_student_class",
                schema: "dbo",
                table: "enrollments",
                newName: "ix_enrollments_student_section");

            migrationBuilder.RenameIndex(
                name: "ix_enrollments_academic_class_id",
                schema: "dbo",
                table: "enrollments",
                newName: "ix_enrollments_class_section_id");

            migrationBuilder.AddColumn<Guid>(
                name: "class_section_id",
                schema: "dbo",
                table: "teacher_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "class_sections",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    deleted_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_class_sections_academic_classes_academic_class_id",
                        column: x => x.academic_class_id,
                        principalSchema: "dbo",
                        principalTable: "academic_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teacher_assignments_class_section_id",
                schema: "dbo",
                table: "teacher_assignments",
                column: "class_section_id");

            migrationBuilder.CreateIndex(
                name: "ix_teacher_assignments_teacher_subject_section",
                schema: "dbo",
                table: "teacher_assignments",
                columns: new[] { "teacher_id", "class_subject_id", "class_section_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_academic_classes_year_grade",
                schema: "dbo",
                table: "academic_classes",
                columns: new[] { "academic_year_id", "grade_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_class_sections_class_section",
                schema: "dbo",
                table: "class_sections",
                columns: new[] { "academic_class_id", "section_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_class_sections_is_deleted",
                schema: "dbo",
                table: "class_sections",
                column: "is_deleted");

            migrationBuilder.AddForeignKey(
                name: "FK_enrollments_class_sections_class_section_id",
                schema: "dbo",
                table: "enrollments",
                column: "class_section_id",
                principalSchema: "dbo",
                principalTable: "class_sections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_teacher_assignments_class_sections_class_section_id",
                schema: "dbo",
                table: "teacher_assignments",
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
                name: "FK_enrollments_class_sections_class_section_id",
                schema: "dbo",
                table: "enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_teacher_assignments_class_sections_class_section_id",
                schema: "dbo",
                table: "teacher_assignments");

            migrationBuilder.DropTable(
                name: "class_sections",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_teacher_assignments_class_section_id",
                schema: "dbo",
                table: "teacher_assignments");

            migrationBuilder.DropIndex(
                name: "ix_teacher_assignments_teacher_subject_section",
                schema: "dbo",
                table: "teacher_assignments");

            migrationBuilder.DropIndex(
                name: "ix_academic_classes_year_grade",
                schema: "dbo",
                table: "academic_classes");

            migrationBuilder.DropColumn(
                name: "class_section_id",
                schema: "dbo",
                table: "teacher_assignments");

            migrationBuilder.RenameColumn(
                name: "class_section_id",
                schema: "dbo",
                table: "enrollments",
                newName: "academic_class_id");

            migrationBuilder.RenameIndex(
                name: "ix_enrollments_student_section",
                schema: "dbo",
                table: "enrollments",
                newName: "ix_enrollments_student_class");

            migrationBuilder.RenameIndex(
                name: "ix_enrollments_class_section_id",
                schema: "dbo",
                table: "enrollments",
                newName: "ix_enrollments_academic_class_id");

            migrationBuilder.AddColumn<int>(
                name: "capacity",
                schema: "dbo",
                table: "academic_classes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "section_code",
                schema: "dbo",
                table: "academic_classes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_teacher_assignments_teacher_class_subject",
                schema: "dbo",
                table: "teacher_assignments",
                columns: new[] { "teacher_id", "class_subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_academic_classes_year_grade_section",
                schema: "dbo",
                table: "academic_classes",
                columns: new[] { "academic_year_id", "grade_code", "section_code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_enrollments_academic_classes_academic_class_id",
                schema: "dbo",
                table: "enrollments",
                column: "academic_class_id",
                principalSchema: "dbo",
                principalTable: "academic_classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
