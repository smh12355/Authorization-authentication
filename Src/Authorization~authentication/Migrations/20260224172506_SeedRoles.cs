using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authorization_authentication.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow;

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "Id", "Name", "Description", "CreatedAt", "CreatedBy" },
                values: new object[,]
                {
                    { Guid.Parse("11111111-1111-1111-1111-111111111111"), "Admin", "Administrator with full access", now, "System" },
                    { Guid.Parse("22222222-2222-2222-2222-222222222222"), "User", "Standard user with basic access", now, "System" },
                    { Guid.Parse("33333333-3333-3333-3333-333333333333"), "Manager", "Manager with elevated permissions", now, "System" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("33333333-3333-3333-3333-333333333333")
                });
        }
    }
}
