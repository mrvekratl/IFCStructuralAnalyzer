using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IFCStructuralAnalyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Density = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    CompressiveStrength = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StructuralElements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GlobalId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LocationX = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    LocationY = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    LocationZ = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    Width = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    Depth = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    Height = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    FloorLevel = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: true),
                    IFCType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ElementType = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    Length = table.Column<double>(type: "float", nullable: true),
                    Area = table.Column<double>(type: "float", nullable: true),
                    Thickness = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructuralElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StructuralElements_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Materials",
                columns: new[] { "Id", "Category", "CompressiveStrength", "Density", "Name" },
                values: new object[,]
                {
                    { 1, "Concrete", 30.0, 2500.0, "C30/37 Concrete" },
                    { 2, "Concrete", 35.0, 2500.0, "C35/45 Concrete" },
                    { 3, "Steel", 420.0, 7850.0, "S420 Steel" },
                    { 4, "Steel", 500.0, 7850.0, "S500 Steel" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Name",
                table: "Materials",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_StructuralElements_FloorLevel",
                table: "StructuralElements",
                column: "FloorLevel");

            migrationBuilder.CreateIndex(
                name: "IX_StructuralElements_GlobalId",
                table: "StructuralElements",
                column: "GlobalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StructuralElements_IFCType",
                table: "StructuralElements",
                column: "IFCType");

            migrationBuilder.CreateIndex(
                name: "IX_StructuralElements_MaterialId",
                table: "StructuralElements",
                column: "MaterialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StructuralElements");

            migrationBuilder.DropTable(
                name: "Materials");
        }
    }
}
