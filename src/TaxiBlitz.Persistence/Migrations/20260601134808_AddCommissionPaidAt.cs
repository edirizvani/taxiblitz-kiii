using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxiBlitz.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionPaidAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CommissionPaidAt",
                table: "ReferralUsages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxUses",
                table: "ReferralCodes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommissionPaidAt",
                table: "ReferralUsages");

            migrationBuilder.AlterColumn<int>(
                name: "MaxUses",
                table: "ReferralCodes",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
