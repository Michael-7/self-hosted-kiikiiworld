using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kiikiiworld.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOriginalUrlToMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalUrl",
                table: "Media",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalUrl",
                table: "Media");
        }
    }
}
