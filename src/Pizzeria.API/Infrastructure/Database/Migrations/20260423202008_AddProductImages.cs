using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pizzeria.API.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default '[]'::jsonb para que las filas existentes arranquen con lista vacía
            // válida; los serializadores de EF ya producen "[]" para List<ProductImage> vacías,
            // así que no hay divergencia entre filas viejas y nuevas.
            migrationBuilder.AddColumn<string>(
                name: "images",
                table: "pizzas",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "images",
                table: "ingredients",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "images",
                table: "pizzas");

            migrationBuilder.DropColumn(
                name: "images",
                table: "ingredients");
        }
    }
}
