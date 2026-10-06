using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerSchedulePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VolunteerShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VolunteerId = table.Column<string>(type: "text", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RequiredSkills = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerShifts_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerShiftNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VolunteerId = table.Column<string>(type: "text", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OpportunityTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ShiftStartsAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerShiftNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerShiftNotifications_VolunteerShifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "VolunteerShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerShiftNotifications_ShiftId",
                table: "VolunteerShiftNotifications",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerShiftNotifications_VolunteerId_IsRead_CreatedAt",
                table: "VolunteerShiftNotifications",
                columns: new[] { "VolunteerId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerShifts_OpportunityId_Status",
                table: "VolunteerShifts",
                columns: new[] { "OpportunityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerShifts_VolunteerId_Status_StartsAt",
                table: "VolunteerShifts",
                columns: new[] { "VolunteerId", "Status", "StartsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VolunteerShiftNotifications");

            migrationBuilder.DropTable(
                name: "VolunteerShifts");
        }
    }
}
