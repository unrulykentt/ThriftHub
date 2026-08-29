using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThriftHub.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SmsVerificationCode",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SmsVerificationCodeExpiresAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmsVerificationCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SmsVerificationCodeExpiresAt",
                table: "AspNetUsers");
        }
    }
}
