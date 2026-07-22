using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addeddualcalendarimplementation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bs_month_lengths",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bs_year = table.Column<int>(type: "integer", nullable: false),
                    bs_month = table.Column<int>(type: "integer", nullable: false),
                    days_in_month = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bs_month_lengths", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bs_month_names",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    month_number = table.Column<int>(type: "integer", nullable: false),
                    name_en = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_np = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bs_month_names", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bs_weekday_names",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    weekday_index = table.Column<int>(type: "integer", nullable: false),
                    name_en = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_np = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_weekly_holiday = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bs_weekday_names", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "calendar_events",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    event_type = table.Column<int>(type: "integer", nullable: false),
                    ad_date = table.Column<DateTime>(type: "date", nullable: false),
                    bs_year = table.Column<int>(type: "integer", nullable: false),
                    bs_month = table.Column<int>(type: "integer", nullable: false),
                    bs_day = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    icon_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    color_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_calendar_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "festival_occurrences",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    festival_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    bs_year = table.Column<int>(type: "integer", nullable: false),
                    bs_start_month = table.Column<int>(type: "integer", nullable: false),
                    bs_start_day = table.Column<int>(type: "integer", nullable: false),
                    bs_end_month = table.Column<int>(type: "integer", nullable: false),
                    bs_end_day = table.Column<int>(type: "integer", nullable: false),
                    ad_start_date = table.Column<DateTime>(type: "date", nullable: false),
                    ad_end_date = table.Column<DateTime>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    color_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_festival_occurrences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meetings",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ad_date = table.Column<DateTime>(type: "date", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    bs_year = table.Column<int>(type: "integer", nullable: false),
                    bs_month = table.Column<int>(type: "integer", nullable: false),
                    bs_day = table.Column<int>(type: "integer", nullable: false),
                    is_virtual = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_meetings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meeting_attendees",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    meeting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meeting_attendees", x => x.id);
                    table.ForeignKey(
                        name: "FK_meeting_attendees_meetings_meeting_id",
                        column: x => x.meeting_id,
                        principalSchema: "dbo",
                        principalTable: "meetings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bs_month_lengths_year_month",
                schema: "dbo",
                table: "bs_month_lengths",
                columns: new[] { "bs_year", "bs_month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bs_month_names_month_number",
                schema: "dbo",
                table: "bs_month_names",
                column: "month_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bs_weekday_names_weekday_index",
                schema: "dbo",
                table: "bs_weekday_names",
                column: "weekday_index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_calendar_events_ad_date",
                schema: "dbo",
                table: "calendar_events",
                column: "ad_date");

            migrationBuilder.CreateIndex(
                name: "ix_calendar_events_bs_date",
                schema: "dbo",
                table: "calendar_events",
                columns: new[] { "bs_year", "bs_month", "bs_day" });

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_is_deleted",
                schema: "dbo",
                table: "calendar_events",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_festival_occurrences_ad_range",
                schema: "dbo",
                table: "festival_occurrences",
                columns: new[] { "ad_start_date", "ad_end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_festival_occurrences_bs_year",
                schema: "dbo",
                table: "festival_occurrences",
                column: "bs_year");

            migrationBuilder.CreateIndex(
                name: "IX_festival_occurrences_is_deleted",
                schema: "dbo",
                table: "festival_occurrences",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_meeting_attendees_meeting_email",
                schema: "dbo",
                table: "meeting_attendees",
                columns: new[] { "meeting_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_meetings_ad_date",
                schema: "dbo",
                table: "meetings",
                column: "ad_date");

            migrationBuilder.CreateIndex(
                name: "ix_meetings_bs_date",
                schema: "dbo",
                table: "meetings",
                columns: new[] { "bs_year", "bs_month", "bs_day" });

            migrationBuilder.CreateIndex(
                name: "ix_meetings_host_user_id",
                schema: "dbo",
                table: "meetings",
                column: "host_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_meetings_is_deleted",
                schema: "dbo",
                table: "meetings",
                column: "is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bs_month_lengths",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "bs_month_names",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "bs_weekday_names",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "calendar_events",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "festival_occurrences",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "meeting_attendees",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "meetings",
                schema: "dbo");
        }
    }
}
