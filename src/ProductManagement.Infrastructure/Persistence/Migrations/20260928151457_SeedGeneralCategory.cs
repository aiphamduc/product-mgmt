using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagement.Infrastructure.Persistence.Migrations
{
    public partial class SeedGeneralCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "product",
                table: "Category",
                columns: new[] { "Id", "IsActive", "Name", "ParentId" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), true, "General", null });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "product",
                table: "Category",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));
        }
    }
}
