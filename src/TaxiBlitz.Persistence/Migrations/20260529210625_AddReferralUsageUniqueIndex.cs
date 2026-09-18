using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxiBlitz.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralUsageUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove duplicate usages caused by the race condition bug — keep the earliest record per (code, user)
            migrationBuilder.Sql(@"
                DELETE ru
                FROM ReferralUsages ru
                INNER JOIN (
                    SELECT MIN(Id) AS KeepId, ReferralCodeId, UsedByUserId
                    FROM ReferralUsages
                    GROUP BY ReferralCodeId, UsedByUserId
                    HAVING COUNT(*) > 1
                ) dupes ON ru.ReferralCodeId = dupes.ReferralCodeId
                         AND ru.UsedByUserId = dupes.UsedByUserId
                         AND ru.Id <> dupes.KeepId;
            ");

            migrationBuilder.DropIndex(
                name: "IX_ReferralUsages_ReferralCodeId",
                table: "ReferralUsages");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralUsages_ReferralCodeId_UsedByUserId",
                table: "ReferralUsages",
                columns: new[] { "ReferralCodeId", "UsedByUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReferralUsages_ReferralCodeId_UsedByUserId",
                table: "ReferralUsages");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralUsages_ReferralCodeId",
                table: "ReferralUsages",
                column: "ReferralCodeId");
        }
    }
}
