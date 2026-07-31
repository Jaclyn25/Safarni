using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FolderId",
                table: "Conversations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatFolders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_FolderId",
                table: "Conversations",
                column: "FolderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_ChatFolders_FolderId",
                table: "Conversations",
                column: "FolderId",
                principalTable: "ChatFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_ChatFolders_FolderId",
                table: "Conversations");

            migrationBuilder.DropTable(
                name: "ChatFolders");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_FolderId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "Conversations");
        }
    }
}
