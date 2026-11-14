using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace ManualMate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VoyageEmbeddingsVector512 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "FileEmbeddings",
                type: "vector(512)",
                nullable: false,
                oldClrType: typeof(Vector),
                oldType: "vector(384)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "FileEmbeddings",
                type: "vector(384)",
                nullable: false,
                oldClrType: typeof(Vector),
                oldType: "vector(512)");
        }
    }
}
