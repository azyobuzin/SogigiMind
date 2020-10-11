using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace SogigiMind.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blobs",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    content = table.Column<byte[]>(nullable: false),
                    content_length = table.Column<long>(nullable: false),
                    content_type = table.Column<string>(nullable: true),
                    etag = table.Column<string>(nullable: true),
                    last_modified = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "end_users",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    acct = table.Column<string>(nullable: true),
                    inserted_at = table.Column<DateTime>(nullable: false),
                    updated_at = table.Column<DateTime>(nullable: false),
                    xmin = table.Column<uint>(type: "xid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_end_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "remote_images",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    url = table.Column<string>(nullable: false),
                    is_sensitive = table.Column<bool>(nullable: true),
                    is_public = table.Column<bool>(nullable: true),
                    inserted_at = table.Column<DateTime>(nullable: false),
                    updated_at = table.Column<DateTime>(nullable: false),
                    xmin = table.Column<uint>(type: "xid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_remote_images", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "estimation_logs",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(nullable: false),
                    remote_image_id = table.Column<long>(nullable: false),
                    result = table.Column<string>(type: "jsonb", nullable: false),
                    inserted_at = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estimation_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_estimation_logs_remote_images_remote_image_id",
                        column: x => x.remote_image_id,
                        principalTable: "remote_images",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_estimation_logs_end_users_user_id",
                        column: x => x.user_id,
                        principalTable: "end_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fetch_attempts",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    remote_image_id = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    content_length = table.Column<long>(nullable: true),
                    content_type = table.Column<string>(nullable: true),
                    start_time = table.Column<DateTime>(nullable: false),
                    inserted_at = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fetch_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_fetch_attempts_remote_images_remote_image_id",
                        column: x => x.remote_image_id,
                        principalTable: "remote_images",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "personal_sensitivities",
                columns: table => new
                {
                    user_id = table.Column<long>(nullable: false),
                    remote_image_id = table.Column<long>(nullable: false),
                    is_sensitive = table.Column<bool>(nullable: false),
                    inserted_at = table.Column<DateTime>(nullable: false),
                    updated_at = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_personal_sensitivities", x => new { x.user_id, x.remote_image_id });
                    table.ForeignKey(
                        name: "fk_personal_sensitivities_remote_images_remote_image_id",
                        column: x => x.remote_image_id,
                        principalTable: "remote_images",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_personal_sensitivities_end_users_user_id",
                        column: x => x.user_id,
                        principalTable: "end_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "thumbnails",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fetch_attempt_id = table.Column<long>(nullable: false),
                    blob_id = table.Column<long>(nullable: false),
                    content_type = table.Column<string>(nullable: false),
                    width = table.Column<int>(nullable: false),
                    height = table.Column<int>(nullable: false),
                    is_animation = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_thumbnails", x => x.id);
                    table.ForeignKey(
                        name: "fk_thumbnails_blobs_blob_id",
                        column: x => x.blob_id,
                        principalTable: "blobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_thumbnails_fetch_attempts_fetch_attempt_id",
                        column: x => x.fetch_attempt_id,
                        principalTable: "fetch_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_end_users_acct",
                table: "end_users",
                column: "acct",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_estimation_logs_inserted_at",
                table: "estimation_logs",
                column: "inserted_at");

            migrationBuilder.CreateIndex(
                name: "ix_estimation_logs_remote_image_id",
                table: "estimation_logs",
                column: "remote_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_estimation_logs_user_id",
                table: "estimation_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_fetch_attempts_remote_image_id",
                table: "fetch_attempts",
                column: "remote_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_personal_sensitivities_remote_image_id",
                table: "personal_sensitivities",
                column: "remote_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_remote_images_url",
                table: "remote_images",
                column: "url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_thumbnails_blob_id",
                table: "thumbnails",
                column: "blob_id");

            migrationBuilder.CreateIndex(
                name: "ix_thumbnails_fetch_attempt_id",
                table: "thumbnails",
                column: "fetch_attempt_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "estimation_logs");

            migrationBuilder.DropTable(
                name: "personal_sensitivities");

            migrationBuilder.DropTable(
                name: "thumbnails");

            migrationBuilder.DropTable(
                name: "end_users");

            migrationBuilder.DropTable(
                name: "blobs");

            migrationBuilder.DropTable(
                name: "fetch_attempts");

            migrationBuilder.DropTable(
                name: "remote_images");
        }
    }
}
