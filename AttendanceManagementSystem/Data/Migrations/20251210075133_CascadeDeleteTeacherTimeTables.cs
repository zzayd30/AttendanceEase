using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceManagementSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteTeacherTimeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeTables_Teachers_TeacherId",
                table: "TimeTables");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeTables_Teachers_TeacherId",
                table: "TimeTables",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeTables_Teachers_TeacherId",
                table: "TimeTables");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeTables_Teachers_TeacherId",
                table: "TimeTables",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
