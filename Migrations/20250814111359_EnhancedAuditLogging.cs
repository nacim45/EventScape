using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace soft20181_starter.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedAuditLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Changes",
                table: "AuditLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "ActionName",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AffectedColumns",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ControllerName",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccessful",
                table: "AuditLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NewValues",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldValues",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestUrl",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Schema",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableName",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserRole",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityId",
                table: "AuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName",
                table: "AuditLogs",
                column: "EntityName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityName",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActionName",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "AffectedColumns",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ControllerName",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IsSuccessful",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "NewValues",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OldValues",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "RequestUrl",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Schema",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "TableName",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserRole",
                table: "AuditLogs");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AuditLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Changes",
                table: "AuditLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
