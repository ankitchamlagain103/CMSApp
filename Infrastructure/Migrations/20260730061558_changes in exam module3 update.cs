using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class changesinexammodule3update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exams_exam_rooms_room_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropTable(
                name: "exam_hall_arrangement_classes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "exam_seat_allocations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "exam_hall_arrangements",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "exam_rooms",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_exams_room_id",
                schema: "dbo",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "room_id",
                schema: "dbo",
                table: "exams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "room_id",
                schema: "dbo",
                table: "exams",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "exam_rooms",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    building = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    deleted_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    floor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_rooms", x => x.id);
                    table.ForeignKey(
                        name: "FK_exam_rooms_academic_years_academic_year_id",
                        column: x => x.academic_year_id,
                        principalSchema: "dbo",
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exam_hall_arrangements",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invigilator_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    hall_capacity = table.Column<int>(type: "integer", nullable: false),
                    reserved_seats = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    sort_strategy = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_hall_arrangements", x => x.id);
                    table.ForeignKey(
                        name: "FK_exam_hall_arrangements_employees_invigilator_employee_id",
                        column: x => x.invigilator_employee_id,
                        principalSchema: "dbo",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exam_hall_arrangements_exam_rooms_room_id",
                        column: x => x.room_id,
                        principalSchema: "dbo",
                        principalTable: "exam_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exam_hall_arrangements_exams_exam_id",
                        column: x => x.exam_id,
                        principalSchema: "dbo",
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exam_hall_arrangement_classes",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_hall_arrangement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    student_limit = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_hall_arrangement_classes", x => x.id);
                    table.ForeignKey(
                        name: "FK_exam_hall_arrangement_classes_academic_classes_academic_cla~",
                        column: x => x.academic_class_id,
                        principalSchema: "dbo",
                        principalTable: "academic_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exam_hall_arrangement_classes_exam_hall_arrangements_exam_h~",
                        column: x => x.exam_hall_arrangement_id,
                        principalSchema: "dbo",
                        principalTable: "exam_hall_arrangements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_seat_allocations",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_hall_arrangement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exam_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attendance_status = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    exam_roll_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    seat_number = table.Column<int>(type: "integer", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_seat_allocations", x => x.id);
                    table.ForeignKey(
                        name: "FK_exam_seat_allocations_academic_classes_academic_class_id",
                        column: x => x.academic_class_id,
                        principalSchema: "dbo",
                        principalTable: "academic_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exam_seat_allocations_exam_hall_arrangements_exam_hall_arra~",
                        column: x => x.exam_hall_arrangement_id,
                        principalSchema: "dbo",
                        principalTable: "exam_hall_arrangements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exam_seat_allocations_exams_exam_id",
                        column: x => x.exam_id,
                        principalSchema: "dbo",
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exam_seat_allocations_students_student_id",
                        column: x => x.student_id,
                        principalSchema: "dbo",
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exams_room_id",
                schema: "dbo",
                table: "exams",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_hall_arrangement_classes_academic_class_id",
                schema: "dbo",
                table: "exam_hall_arrangement_classes",
                column: "academic_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_hall_arrangement_classes_arrangement_class",
                schema: "dbo",
                table: "exam_hall_arrangement_classes",
                columns: new[] { "exam_hall_arrangement_id", "academic_class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exam_hall_arrangements_exam_id",
                schema: "dbo",
                table: "exam_hall_arrangements",
                column: "exam_id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_hall_arrangements_invigilator_employee_id",
                schema: "dbo",
                table: "exam_hall_arrangements",
                column: "invigilator_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_hall_arrangements_room_id",
                schema: "dbo",
                table: "exam_hall_arrangements",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_rooms_is_deleted",
                schema: "dbo",
                table: "exam_rooms",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_exam_rooms_year_name",
                schema: "dbo",
                table: "exam_rooms",
                columns: new[] { "academic_year_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exam_seat_allocations_academic_class_id",
                schema: "dbo",
                table: "exam_seat_allocations",
                column: "academic_class_id");

            migrationBuilder.CreateIndex(
                name: "ix_exam_seat_allocations_arrangement_seat",
                schema: "dbo",
                table: "exam_seat_allocations",
                columns: new[] { "exam_hall_arrangement_id", "seat_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exam_seat_allocations_exam_student",
                schema: "dbo",
                table: "exam_seat_allocations",
                columns: new[] { "exam_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exam_seat_allocations_student_id",
                schema: "dbo",
                table: "exam_seat_allocations",
                column: "student_id");

            migrationBuilder.AddForeignKey(
                name: "FK_exams_exam_rooms_room_id",
                schema: "dbo",
                table: "exams",
                column: "room_id",
                principalSchema: "dbo",
                principalTable: "exam_rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
