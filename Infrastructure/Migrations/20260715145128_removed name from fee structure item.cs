using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removednamefromfeestructureitem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_fee_structure_items_fee_structure_id",
                schema: "dbo",
                table: "fee_structure_items");

            migrationBuilder.DropColumn(
                name: "name",
                schema: "dbo",
                table: "fee_structure_items");

            migrationBuilder.AddColumn<string>(
                name: "fee_category_code",
                schema: "dbo",
                table: "fee_structure_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_fee_structure_items_structure_category",
                schema: "dbo",
                table: "fee_structure_items",
                columns: new[] { "fee_structure_id", "fee_category_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_fee_structure_items_structure_category",
                schema: "dbo",
                table: "fee_structure_items");

            migrationBuilder.DropColumn(
                name: "fee_category_code",
                schema: "dbo",
                table: "fee_structure_items");

            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "dbo",
                table: "fee_structure_items",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_fee_structure_items_fee_structure_id",
                schema: "dbo",
                table: "fee_structure_items",
                column: "fee_structure_id");
        }
    }
}
