using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedusertypeinrole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "user_type",
                schema: "identity",
                table: "application_roles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "BOTH");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user_type",
                schema: "identity",
                table: "application_roles");
        }
    }
}
