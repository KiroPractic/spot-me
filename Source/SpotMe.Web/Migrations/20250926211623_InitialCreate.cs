using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotMe.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StreamingHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MsPlayed = table.Column<long>(type: "bigint", nullable: false),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlayedInCountryCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SpotifyUri = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TrackName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ArtistName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AlbumName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EpisodeName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShowName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Skipped = table.Column<bool>(type: "boolean", nullable: true),
                    ReasonStart = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReasonEnd = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Shuffle = table.Column<bool>(type: "boolean", nullable: true),
                    Offline = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamingHistory", x => x.Id);
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

            migrationBuilder.CreateIndex(
                name: "IX_StreamingHistory_SpotifyUri",
                table: "StreamingHistory",
                column: "SpotifyUri",
                filter: "\"SpotifyUri\" IS NOT NULL");

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
                name: "IX_StreamingHistory_User_PlayedAt",
                table: "StreamingHistory",
                columns: new[] { "UserId", "PlayedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StreamingHistory_User_Track_Artist",
                table: "StreamingHistory",
                columns: new[] { "UserId", "TrackName", "ArtistName" },
                filter: "\"TrackName\" IS NOT NULL AND \"ArtistName\" IS NOT NULL");

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
                name: "StreamingHistory");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
