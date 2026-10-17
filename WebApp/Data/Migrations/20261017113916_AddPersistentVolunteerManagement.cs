using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentVolunteerManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VolunteerOpportunities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RequiredSkills = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    VolunteersNeeded = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerOpportunities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VolunteerId = table.Column<string>(type: "text", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerApplications_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VolunteerApplications_VolunteerOpportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "VolunteerOpportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VolunteerId = table.Column<string>(type: "text", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByUserId = table.Column<string>(type: "text", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerAssignments_VolunteerApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "VolunteerApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VolunteerAssignments_VolunteerOpportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "VolunteerOpportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerApplications_OpportunityId_Status",
                table: "VolunteerApplications",
                columns: new[] { "OpportunityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerApplications_VolunteerId_OpportunityId",
                table: "VolunteerApplications",
                columns: new[] { "VolunteerId", "OpportunityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAssignments_ApplicationId",
                table: "VolunteerAssignments",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAssignments_OpportunityId_Status",
                table: "VolunteerAssignments",
                columns: new[] { "OpportunityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAssignments_VolunteerId_Status",
                table: "VolunteerAssignments",
                columns: new[] { "VolunteerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOpportunities_Status_StartDateTime",
                table: "VolunteerOpportunities",
                columns: new[] { "Status", "StartDateTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VolunteerAssignments");

            migrationBuilder.DropTable(
                name: "VolunteerApplications");

            migrationBuilder.DropTable(
                name: "VolunteerOpportunities");
        }
    }
}
