using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class PatientIndexesAdded_3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_patients_birth_date_offset",
                schema: "public",
                table: "patients",
                column: "birth_date_offset");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_patients_birth_date_offset",
                schema: "public",
                table: "patients");
        }
    }
}
