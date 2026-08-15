using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Safarni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkChatOwnershipToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Chat ownership previously used unconstrained string identifiers. Existing chat
            // data is intentionally discarded before converting the columns and enforcing the
            // new Users foreign keys, so every environment can apply this migration reliably.
            migrationBuilder.Sql(
                """
                UPDATE [AIRequestLogs]
                SET [ConversationId] = NULL
                WHERE [ConversationId] IS NOT NULL;

                DELETE FROM [ConversationMessages];
                DELETE FROM [Conversations];
                DELETE FROM [ChatFolders];
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_ChatFolders_FolderId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_FolderId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_ConversationMessages_ConversationId",
                table: "ConversationMessages");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Conversations",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "ChatFolders",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ChatFolders_Id_UserId",
                table: "ChatFolders",
                columns: new[] { "Id", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_FolderId_UserId",
                table: "Conversations",
                columns: new[] { "FolderId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserId_CreatedAt",
                table: "Conversations",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_ConversationId_CreatedAt",
                table: "ConversationMessages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatFolders_UserId_CreatedAt",
                table: "ChatFolders",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ChatFolders_Users_UserId",
                table: "ChatFolders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_ChatFolders_FolderId_UserId",
                table: "Conversations",
                columns: new[] { "FolderId", "UserId" },
                principalTable: "ChatFolders",
                principalColumns: new[] { "Id", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Users_UserId",
                table: "Conversations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatFolders_Users_UserId",
                table: "ChatFolders");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_ChatFolders_FolderId_UserId",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Users_UserId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_FolderId_UserId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_UserId_CreatedAt",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_ConversationMessages_ConversationId_CreatedAt",
                table: "ConversationMessages");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ChatFolders_Id_UserId",
                table: "ChatFolders");

            migrationBuilder.DropIndex(
                name: "IX_ChatFolders_UserId_CreatedAt",
                table: "ChatFolders");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Conversations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ChatFolders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_FolderId",
                table: "Conversations",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_ConversationId",
                table: "ConversationMessages",
                column: "ConversationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_ChatFolders_FolderId",
                table: "Conversations",
                column: "FolderId",
                principalTable: "ChatFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
