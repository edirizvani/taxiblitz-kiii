using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxiBlitz.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTourSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Tours",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tours_Slug",
                table: "Tours",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tours_Slug",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Tours");
        }
    }
}
