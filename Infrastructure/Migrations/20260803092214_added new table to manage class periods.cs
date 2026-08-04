using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addednewtabletomanageclassperiods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "period_code",
                schema: "dbo",
                table: "teacher_assignments");

            migrationBuilder.DropColumn(
                name: "period_code",
                schema: "dbo",
                table: "exams");

            migrationBuilder.AddColumn<Guid>(
                name: "time_period_id",
                schema: "dbo",
                table: "teacher_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "time_period_id",
                schema: "dbo",
                table: "exams",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "time_periods",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_time_periods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "class_time_periods",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_time_periods", x => x.id);
                    table.ForeignKey(
                        name: "FK_class_time_periods_academic_classes_academic_class_id",
                        column: x => x.academic_class_id,
                        principalSchema: "dbo",
                        principalTable: "academic_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_time_periods_time_periods_time_period_id",
                        column: x => x.time_period_id,
                        principalSchema: "dbo",
                        principalTable: "time_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teacher_assignments_time_period_id",
                schema: "dbo",
                table: "teacher_assignments",
                column: "time_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_exams_time_period_id",
                schema: "dbo",
                table: "exams",
                column: "time_period_id");

            migrationBuilder.CreateIndex(
                name: "ix_class_time_periods_class_period",
                schema: "dbo",
                table: "class_time_periods",
                columns: new[] { "academic_class_id", "time_period_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_class_time_periods_time_period_id",
                schema: "dbo",
                table: "class_time_periods",
                column: "time_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_time_periods_is_deleted",
                schema: "dbo",
                table: "time_periods",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_time_periods_name",
                schema: "dbo",
                table: "time_periods",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_exams_time_periods_time_period_id",
                schema: "dbo",
                table: "exams",
                column: "time_period_id",
                principalSchema: "dbo",
                principalTable: "time_periods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_teacher_assignments_time_periods_time_period_id",
                schema: "dbo",
                table: "teacher_assignments",
                column: "time_period_id",
                principalSchema: "dbo",
                principalTable: "time_periods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exams_time_periods_time_period_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropForeignKey(
                name: "FK_teacher_assignments_time_periods_time_period_id",
                schema: "dbo",
                table: "teacher_assignments");

            migrationBuilder.DropTable(
                name: "class_time_periods",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "time_periods",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_teacher_assignments_time_period_id",
                schema: "dbo",
                table: "teacher_assignments");

            migrationBuilder.DropIndex(
                name: "IX_exams_time_period_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "time_period_id",
                schema: "dbo",
                table: "teacher_assignments");

            migrationBuilder.DropColumn(
                name: "time_period_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.AddColumn<string>(
                name: "period_code",
                schema: "dbo",
                table: "teacher_assignments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "period_code",
                schema: "dbo",
                table: "exams",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
