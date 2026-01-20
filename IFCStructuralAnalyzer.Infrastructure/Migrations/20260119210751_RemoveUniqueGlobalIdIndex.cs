using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IFCStructuralAnalyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueGlobalIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
       name: "IX_StructuralElements_GlobalId",
       table: "StructuralElements");

            migrationBuilder.CreateIndex(
                name: "IX_StructuralElements_GlobalId",
                table: "StructuralElements",
                column: "GlobalId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
       name: "IX_StructuralElements_GlobalId",
       table: "StructuralElements");

            migrationBuilder.CreateIndex(
                name: "IX_StructuralElements_GlobalId",
                table: "StructuralElements",
                column: "GlobalId",
                unique: true);
        }
    }
}
