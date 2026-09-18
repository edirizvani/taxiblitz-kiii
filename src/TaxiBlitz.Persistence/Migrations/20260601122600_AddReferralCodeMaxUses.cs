using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxiBlitz.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralCodeMaxUses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxUses",
                table: "ReferralCodes",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxUses",
                table: "ReferralCodes");
        }
    }
}
