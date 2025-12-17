using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherApp.Migrations
{
    /// <inheritdoc />
    public partial class AddFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_Users_UserId",
                table: "Favorites");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Favorites",
                table: "Favorites");

            migrationBuilder.RenameTable(
                name: "Favorites",
                newName: "FavoriteCities");

            migrationBuilder.RenameIndex(
                name: "IX_Favorites_UserId",
                table: "FavoriteCities",
                newName: "IX_FavoriteCities_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FavoriteCities",
                table: "FavoriteCities",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FavoriteCities_Users_UserId",
                table: "FavoriteCities",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FavoriteCities_Users_UserId",
                table: "FavoriteCities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FavoriteCities",
                table: "FavoriteCities");

            migrationBuilder.RenameTable(
                name: "FavoriteCities",
                newName: "Favorites");

            migrationBuilder.RenameIndex(
                name: "IX_FavoriteCities_UserId",
                table: "Favorites",
                newName: "IX_Favorites_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Favorites",
                table: "Favorites",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_Users_UserId",
                table: "Favorites",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
