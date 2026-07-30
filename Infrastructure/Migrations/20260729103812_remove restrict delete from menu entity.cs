using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removerestrictdeletefrommenuentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_menus_menus_parent_id",
                schema: "dbo",
                table: "menus");

            migrationBuilder.AddForeignKey(
                name: "FK_menus_menus_parent_id",
                schema: "dbo",
                table: "menus",
                column: "parent_id",
                principalSchema: "dbo",
                principalTable: "menus",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_menus_menus_parent_id",
                schema: "dbo",
                table: "menus");

            migrationBuilder.AddForeignKey(
                name: "FK_menus_menus_parent_id",
                schema: "dbo",
                table: "menus",
                column: "parent_id",
                principalSchema: "dbo",
                principalTable: "menus",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
