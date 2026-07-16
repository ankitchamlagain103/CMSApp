using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class changesonfeestructureandother : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_fee_structures_class_category",
                schema: "dbo",
                table: "fee_structures");

            migrationBuilder.DropIndex(
                name: "ix_enrollment_fee_selections_enrollment_category",
                schema: "dbo",
                table: "enrollment_fee_selections");

            migrationBuilder.DropColumn(
                name: "amount",
                schema: "dbo",
                table: "fee_structures");

            migrationBuilder.DropColumn(
                name: "fee_category_code",
                schema: "dbo",
                table: "fee_structures");

            migrationBuilder.DropColumn(
                name: "frequency_type",
                schema: "dbo",
                table: "fee_structures");

            migrationBuilder.DropColumn(
                name: "is_optional",
                schema: "dbo",
                table: "fee_structures");

            migrationBuilder.DropColumn(
                name: "is_refundable",
                schema: "dbo",
                table: "fee_structures");

            migrationBuilder.DropColumn(
                name: "fee_category_code",
                schema: "dbo",
                table: "enrollment_fee_selections");

            migrationBuilder.AddColumn<Guid>(
                name: "fee_structure_item_id",
                schema: "dbo",
                table: "enrollment_fee_selections",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "fee_structure_items",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_structure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    frequency_type = table.Column<int>(type: "integer", nullable: false),
                    is_optional = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_refundable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    updated_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_structure_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_fee_structure_items_fee_structures_fee_structure_id",
                        column: x => x.fee_structure_id,
                        principalSchema: "dbo",
                        principalTable: "fee_structures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fee_structures_academic_class",
                schema: "dbo",
                table: "fee_structures",
                column: "academic_class_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_enrollment_fee_selections_enrollment_item",
                schema: "dbo",
                table: "enrollment_fee_selections",
                columns: new[] { "enrollment_id", "fee_structure_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_fee_selections_fee_structure_item_id",
                schema: "dbo",
                table: "enrollment_fee_selections",
                column: "fee_structure_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_fee_structure_items_fee_structure_id",
                schema: "dbo",
                table: "fee_structure_items",
                column: "fee_structure_id");

            migrationBuilder.AddForeignKey(
                name: "FK_enrollment_fee_selections_fee_structure_items_fee_structure~",
                schema: "dbo",
                table: "enrollment_fee_selections",
                column: "fee_structure_item_id",
                principalSchema: "dbo",
                principalTable: "fee_structure_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_enrollment_fee_selections_fee_structure_items_fee_structure~",
                schema: "dbo",
                table: "enrollment_fee_selections");

            migrationBuilder.DropTable(
                name: "fee_structure_items",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "ix_fee_structures_academic_class",
                schema: "dbo",
                table: "fee_structures");

            migrationBuilder.DropIndex(
                name: "ix_enrollment_fee_selections_enrollment_item",
                schema: "dbo",
                table: "enrollment_fee_selections");

            migrationBuilder.DropIndex(
                name: "IX_enrollment_fee_selections_fee_structure_item_id",
                schema: "dbo",
                table: "enrollment_fee_selections");

            migrationBuilder.DropColumn(
                name: "fee_structure_item_id",
                schema: "dbo",
                table: "enrollment_fee_selections");

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                schema: "dbo",
                table: "fee_structures",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "fee_category_code",
                schema: "dbo",
                table: "fee_structures",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "frequency_type",
                schema: "dbo",
                table: "fee_structures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_optional",
                schema: "dbo",
                table: "fee_structures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_refundable",
                schema: "dbo",
                table: "fee_structures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "fee_category_code",
                schema: "dbo",
                table: "enrollment_fee_selections",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_fee_structures_class_category",
                schema: "dbo",
                table: "fee_structures",
                columns: new[] { "academic_class_id", "fee_category_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_enrollment_fee_selections_enrollment_category",
                schema: "dbo",
                table: "enrollment_fee_selections",
                columns: new[] { "enrollment_id", "fee_category_code" },
                unique: true);
        }
    }
}
