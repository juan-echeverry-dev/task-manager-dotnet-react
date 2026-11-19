using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskLane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTaskSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deletion is now permanent (spec 003 FR-006): tasks deleted before this change are removed
            // for good before the column that marked them is dropped. Down cannot bring them back.
            migrationBuilder.Sql("DELETE FROM \"Tasks\" WHERE \"DeletedAt\" IS NOT NULL;");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_OwnerId_DeletedAt_CreatedAt",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_OwnerId_DeletedAt_DueDate",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Tasks");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_OwnerId_CreatedAt",
                table: "Tasks",
                columns: new[] { "OwnerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_OwnerId_DueDate",
                table: "Tasks",
                columns: new[] { "OwnerId", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_OwnerId_CreatedAt",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_OwnerId_DueDate",
                table: "Tasks");

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_OwnerId_DeletedAt_CreatedAt",
                table: "Tasks",
                columns: new[] { "OwnerId", "DeletedAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_OwnerId_DeletedAt_DueDate",
                table: "Tasks",
                columns: new[] { "OwnerId", "DeletedAt", "DueDate" });
        }
    }
}
