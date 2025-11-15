using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotMe.Web.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadedFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StreamingHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MsPlayed = table.Column<long>(type: "bigint", nullable: false),
                    Platform = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PlayedInCountryCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SpotifyUri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TrackName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ArtistName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AlbumName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EpisodeName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ShowName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Skipped = table.Column<bool>(type: "boolean", nullable: true),
                    ReasonStart = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReasonEnd = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Shuffle = table.Column<bool>(type: "boolean", nullable: true),
                    Offline = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamingHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamingHistory_UploadedFiles_UploadedFileId",
                        column: x => x.UploadedFileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpotifyTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RefreshToken = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TokenType = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotifyTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpotifyTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpotifyTokens_UserId",
                table: "SpotifyTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamingHistory_SpotifyUri",
                table: "StreamingHistory",
                column: "SpotifyUri",
                filter: "\"SpotifyUri\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StreamingHistory_UploadedFileId",
                table: "StreamingHistory",
                column: "UploadedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamingHistory_User_Artist_PlayedAt",
                table: "StreamingHistory",
                columns: new[] { "UserId", "ArtistName", "PlayedAt" },
                filter: "\"ArtistName\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StreamingHistory_User_ContentType_PlayedAt",
                table: "StreamingHistory",
                columns: new[] { "UserId", "ContentType", "PlayedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StreamingHistory_User_Track_Artist",
                table: "StreamingHistory",
                columns: new[] { "UserId", "TrackName", "ArtistName" },
                filter: "\"TrackName\" IS NOT NULL AND \"ArtistName\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_User_FileHash_Unique",
                table: "UploadedFiles",
                columns: new[] { "UserId", "FileHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_UserId",
                table: "UploadedFiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailAddress",
                table: "Users",
                column: "EmailAddress",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpotifyTokens");

            migrationBuilder.DropTable(
                name: "StreamingHistory");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "UploadedFiles");
        }
    }
}
