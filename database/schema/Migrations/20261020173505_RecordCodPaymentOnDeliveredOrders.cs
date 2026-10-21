using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RecordCodPaymentOnDeliveredOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Successful COD deliveries already include collection. Older versions
            // omitted this flag; keep PaidAt unknown rather than inventing a date.
            migrationBuilder.Sql("""
                UPDATE [Orders]
                SET [IsPaid] = 1
                WHERE UPPER([PaymentMethod]) = N'COD'
                  AND [Status] = N'Đã giao'
                  AND [IsPaid] = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Payment collection is a fact; rolling back must not mark it unpaid.
        }
    }
}
