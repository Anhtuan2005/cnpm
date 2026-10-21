using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class PhoneSpecificationsInventoryAndRefundAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockLogs_ProductId",
                table: "StockLogs");

            migrationBuilder.AddColumn<string>(
                name: "MovementType",
                table: "StockLogs",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Điều chỉnh thủ công");

            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "StockLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityAfter",
                table: "StockLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityBefore",
                table: "StockLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneSpecificationsJson",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundConfirmedBy",
                table: "Orders",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundMethod",
                table: "Orders",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReference",
                table: "Orders",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLogs_OrderId",
                table: "StockLogs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLogs_ProductId_ChangedAt",
                table: "StockLogs",
                columns: new[] { "ProductId", "ChangedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_StockLogs_Orders_OrderId",
                table: "StockLogs",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockLogs_Orders_OrderId",
                table: "StockLogs");

            migrationBuilder.DropIndex(
                name: "IX_StockLogs_OrderId",
                table: "StockLogs");

            migrationBuilder.DropIndex(
                name: "IX_StockLogs_ProductId_ChangedAt",
                table: "StockLogs");

            migrationBuilder.DropColumn(
                name: "MovementType",
                table: "StockLogs");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "StockLogs");

            migrationBuilder.DropColumn(
                name: "QuantityAfter",
                table: "StockLogs");

            migrationBuilder.DropColumn(
                name: "QuantityBefore",
                table: "StockLogs");

            migrationBuilder.DropColumn(
                name: "PhoneSpecificationsJson",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundConfirmedBy",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundReference",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_StockLogs_ProductId",
                table: "StockLogs",
                column: "ProductId");
        }
    }
}
