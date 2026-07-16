using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class minorupdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_class_subjects_classwide_unique",
                schema: "dbo",
                table: "class_subjects",
                columns: new[] { "academic_class_id", "subject_code" },
                unique: true,
                filter: "class_section_id IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_class_subjects_mandatory_classwide",
                schema: "dbo",
                table: "class_subjects",
                sql: "is_mandatory = false OR class_section_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_class_subjects_classwide_unique",
                schema: "dbo",
                table: "class_subjects");

            migrationBuilder.DropCheckConstraint(
                name: "ck_class_subjects_mandatory_classwide",
                schema: "dbo",
                table: "class_subjects");
        }
    }
}
