using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountAccessReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AdminLockedAt",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            // Older versions stored manual locks as a ten-year Identity lockout.
            // The automatic lockout in those versions lasted fifteen minutes.
            migrationBuilder.Sql("""
                UPDATE [AspNetUsers]
                SET [AdminLockedAt] = SYSDATETIMEOFFSET()
                WHERE [LockoutEnabled] = 1
                  AND [LockoutEnd] > DATEADD(day, 1, SYSUTCDATETIME())
                """);

            migrationBuilder.CreateTable(
                name: "AccountAccessEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TemporaryLockUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountAccessEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountAccessEvents_UserId_Id",
                table: "AccountAccessEvents",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountAccessEvents_UserId_TemporaryLockUntil",
                table: "AccountAccessEvents",
                columns: new[] { "UserId", "TemporaryLockUntil" },
                unique: true,
                filter: "[TemporaryLockUntil] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountAccessEvents");

            migrationBuilder.DropColumn(
                name: "AdminLockedAt",
                table: "AspNetUsers");
        }
    }
}
