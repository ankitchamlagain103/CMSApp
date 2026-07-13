using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addederrorlogsandsystemaccesslogalsoconfigtypesandconfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "config_types",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_code = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_types", x => x.id);
                    table.UniqueConstraint("AK_config_types_type_code", x => x.type_code);
                });

            migrationBuilder.CreateTable(
                name: "error_logs",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fingerprint_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    exception_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    stack_trace = table.Column<string>(type: "text", nullable: true),
                    path = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    error_count = table.Column<int>(type: "integer", nullable: false),
                    last_occurred_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_error_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_access_logs",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    controller = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    http_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_access_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "configs",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_code = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    additional_value1 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    additional_value2 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    additional_value3 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configs", x => x.id);
                    table.ForeignKey(
                        name: "FK_configs_config_types_type_code",
                        column: x => x.type_code,
                        principalSchema: "dbo",
                        principalTable: "config_types",
                        principalColumn: "type_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_config_types_type_code",
                schema: "dbo",
                table: "config_types",
                column: "type_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configs_type_code_code",
                schema: "dbo",
                table: "configs",
                columns: new[] { "type_code", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_error_logs_fingerprint_hash",
                schema: "dbo",
                table: "error_logs",
                column: "fingerprint_hash");

            migrationBuilder.CreateIndex(
                name: "ix_system_access_logs_user_id",
                schema: "dbo",
                table: "system_access_logs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "error_logs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "system_access_logs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "config_types",
                schema: "dbo");
        }
    }
}
