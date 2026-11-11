using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminLockDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AdminLockUntil",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AdminLockUntil",
                table: "AccountAccessEvents",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminLockUntil",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AdminLockUntil",
                table: "AccountAccessEvents");
        }
    }
}
