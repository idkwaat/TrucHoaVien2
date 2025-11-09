using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCleanImageUrlToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CleanImageUrl",
                table: "ProductVariants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngravingColor",
                table: "ProductVariants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngravingFont",
                table: "ProductVariants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EngravingX",
                table: "ProductVariants",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EngravingY",
                table: "ProductVariants",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CleanImageUrl",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "EngravingColor",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "EngravingFont",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "EngravingX",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "EngravingY",
                table: "ProductVariants");
        }
    }
}
