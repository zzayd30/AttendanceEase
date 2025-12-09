using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceManagementSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnableCascadeBatchDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sections_Batches_BatchId",
                table: "Sections");

            migrationBuilder.AddForeignKey(
                name: "FK_Sections_Batches_BatchId",
                table: "Sections",
                column: "BatchId",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sections_Batches_BatchId",
                table: "Sections");

            migrationBuilder.AddForeignKey(
                name: "FK_Sections_Batches_BatchId",
                table: "Sections",
                column: "BatchId",
                principalTable: "Batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
