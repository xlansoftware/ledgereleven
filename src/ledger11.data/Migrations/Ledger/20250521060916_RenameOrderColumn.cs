using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ledger11.data.Migrations.Ledger
{
    /// <inheritdoc />
    public partial class RenameOrderColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Order",
                table: "Categories",
                newName: "DisplayOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "Categories",
                newName: "Order");
        }
    }
}
