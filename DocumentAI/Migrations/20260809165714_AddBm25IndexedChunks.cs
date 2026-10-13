using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentAI.Migrations
{
    /// <inheritdoc />
    public partial class AddBm25IndexedChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bm25IndexedChunks",
                columns: table => new
                {
                    ChunkId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DocumentId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndexedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bm25IndexedChunks", x => x.ChunkId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bm25IndexedChunks_UserId_DocumentId",
                table: "Bm25IndexedChunks",
                columns: new[] { "UserId", "DocumentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bm25IndexedChunks");
        }
    }
}
