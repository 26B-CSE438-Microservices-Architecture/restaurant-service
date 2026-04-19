using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantService.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCuisineTypeToRestaurant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CuisineType",
                table: "Restaurants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_CuisineType",
                table: "Restaurants",
                column: "CuisineType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Restaurants_CuisineType",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "CuisineType",
                table: "Restaurants");
        }
    }
}
