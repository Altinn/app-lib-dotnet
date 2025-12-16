using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.App.ProcessEngine.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "process_engine_jobs",
                columns: table => new
                {
                    Identifier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    actor_user_id_or_org_number = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    actor_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    instance_org = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    instance_app = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    instance_owner_party_id = table.Column<int>(type: "integer", nullable: false),
                    instance_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_engine_jobs", x => x.Identifier);
                }
            );

            migrationBuilder.CreateTable(
                name: "process_engine_tasks",
                columns: table => new
                {
                    Identifier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProcessingOrder = table.Column<int>(type: "integer", nullable: false),
                    command_data = table.Column<string>(type: "text", nullable: false),
                    actor_user_id_or_org_number = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    actor_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    BackoffUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryStrategy = table.Column<string>(type: "text", nullable: true),
                    RequeueCount = table.Column<int>(type: "integer", nullable: false),
                    JobIdentifier = table.Column<string>(type: "character varying(500)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_engine_tasks", x => x.Identifier);
                    table.ForeignKey(
                        name: "FK_process_engine_tasks_process_engine_jobs_JobIdentifier",
                        column: x => x.JobIdentifier,
                        principalTable: "process_engine_jobs",
                        principalColumn: "Identifier",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_process_engine_jobs_CreatedAt",
                table: "process_engine_jobs",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_process_engine_jobs_Status",
                table: "process_engine_jobs",
                column: "Status"
            );

            migrationBuilder.CreateIndex(
                name: "IX_process_engine_tasks_BackoffUntil",
                table: "process_engine_tasks",
                column: "BackoffUntil"
            );

            migrationBuilder.CreateIndex(
                name: "IX_process_engine_tasks_CreatedAt",
                table: "process_engine_tasks",
                column: "CreatedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_process_engine_tasks_JobIdentifier",
                table: "process_engine_tasks",
                column: "JobIdentifier"
            );

            migrationBuilder.CreateIndex(
                name: "IX_process_engine_tasks_Status",
                table: "process_engine_tasks",
                column: "Status"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "process_engine_tasks");

            migrationBuilder.DropTable(name: "process_engine_jobs");
        }
    }
}
