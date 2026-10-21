using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderAssuranceMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CodSettledAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CodSettlementAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodSettlementConfirmedBy",
                table: "Orders",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodSettlementReference",
                table: "Orders",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerReceiptConfirmedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerReceiptIssue",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerReceiptIssueAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerReceiptResolution",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerReceiptStatus",
                table: "Orders",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Chờ khách xác nhận");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryProofUrl",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundReceiptConfirmedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReceiptIssue",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundReceiptIssueAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReceiptResolution",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReceiptStatus",
                table: "Orders",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Chờ khách xác nhận");

            migrationBuilder.CreateTable(
                name: "OrderAssuranceEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActorId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAssuranceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderAssuranceEvents_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderAssuranceEvents_OrderId_CreatedAt",
                table: "OrderAssuranceEvents",
                columns: new[] { "OrderId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderAssuranceEvents");

            migrationBuilder.DropColumn(
                name: "CodSettledAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CodSettlementAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CodSettlementConfirmedBy",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CodSettlementReference",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerReceiptConfirmedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerReceiptIssue",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerReceiptIssueAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerReceiptResolution",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerReceiptStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryProofUrl",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundReceiptConfirmedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundReceiptIssue",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundReceiptIssueAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundReceiptResolution",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundReceiptStatus",
                table: "Orders");
        }
    }
}
