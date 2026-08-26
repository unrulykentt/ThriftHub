using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThriftHub.Migrations
{
    /// <inheritdoc />
    public partial class AddIdCardReviewArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdCardArchiveBackUrl",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdCardArchiveFrontUrl",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IdCardReviewedAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdCardVerificationStatus",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdCardArchiveBackUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IdCardArchiveFrontUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IdCardReviewedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IdCardVerificationStatus",
                table: "AspNetUsers");
        }
    }
}
