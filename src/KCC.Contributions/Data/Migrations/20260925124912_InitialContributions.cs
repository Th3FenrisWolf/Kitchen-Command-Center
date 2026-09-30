using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KCC.Contributions.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialContributions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kccCookedMark",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    variantKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    memberKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    created = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kccCookedMark", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "kccCookNote",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    variantKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    memberKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kccCookNote", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "kccReview",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    variantKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    memberKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    rating = table.Column<double>(type: "REAL", nullable: false),
                    text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kccReview", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_kccCookedMark_variantKey_memberKey",
                table: "kccCookedMark",
                columns: new[] { "variantKey", "memberKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kccCookNote_variantKey",
                table: "kccCookNote",
                column: "variantKey");

            migrationBuilder.CreateIndex(
                name: "IX_kccReview_variantKey_memberKey",
                table: "kccReview",
                columns: new[] { "variantKey", "memberKey" },
                unique: true);

            migrationBuilder.Sql(
                $"INSERT INTO umbracoLock (id, value, name) VALUES ({ContributionLocks.Contributions}, 1, 'KccContributions');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM umbracoLock WHERE id = {ContributionLocks.Contributions};");

            migrationBuilder.DropTable(
                name: "kccCookedMark");

            migrationBuilder.DropTable(
                name: "kccCookNote");

            migrationBuilder.DropTable(
                name: "kccReview");
        }
    }
}
