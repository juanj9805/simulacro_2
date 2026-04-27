using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace simulationTest.Migrations
{
    /// <inheritdoc />
    public partial class ConventionFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_treatmentsMedicines_Medicines_MedicineId",
                table: "treatmentsMedicines");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Medicines",
                table: "Medicines");

            migrationBuilder.RenameTable(
                name: "Medicines",
                newName: "medicines");

            migrationBuilder.AddPrimaryKey(
                name: "PK_medicines",
                table: "medicines",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_treatmentsMedicines_medicines_MedicineId",
                table: "treatmentsMedicines",
                column: "MedicineId",
                principalTable: "medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_treatmentsMedicines_medicines_MedicineId",
                table: "treatmentsMedicines");

            migrationBuilder.DropPrimaryKey(
                name: "PK_medicines",
                table: "medicines");

            migrationBuilder.RenameTable(
                name: "medicines",
                newName: "Medicines");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Medicines",
                table: "Medicines",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_treatmentsMedicines_Medicines_MedicineId",
                table: "treatmentsMedicines",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
