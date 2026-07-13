using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addipfilterationinuser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_ip_restricted",
                schema: "identity",
                table: "application_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "user_ip_allowed",
                schema: "identity",
                table: "application_users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_ip_restricted",
                schema: "identity",
                table: "application_users");

            migrationBuilder.DropColumn(
                name: "user_ip_allowed",
                schema: "identity",
                table: "application_users");
        }
    }
}
