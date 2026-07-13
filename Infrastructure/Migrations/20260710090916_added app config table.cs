using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedappconfigtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_configs",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    config_param = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    config_value = table.Column<string>(type: "character varying(555)", maxLength: 555, nullable: false),
                    config_group = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_enable = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_configs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_configs_config_param",
                schema: "dbo",
                table: "app_configs",
                column: "config_param",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_configs",
                schema: "dbo");
        }
    }
}
