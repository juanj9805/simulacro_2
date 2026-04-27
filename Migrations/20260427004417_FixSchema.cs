using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace simulationTest.Migrations
{
    /// <inheritdoc />
    public partial class FixSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_consultations_pets_PetId",
                table: "consultations");

            migrationBuilder.DropForeignKey(
                name: "FK_consultations_veterinaries_VeterinaryId",
                table: "consultations");

            migrationBuilder.DropForeignKey(
                name: "FK_pets_owners_OwnerId",
                table: "pets");

            migrationBuilder.DropForeignKey(
                name: "FK_treatments_consultations_ConsultationId",
                table: "treatments");

            migrationBuilder.DropForeignKey(
                name: "FK_treatmentsMedicines_medicines_MedicineId",
                table: "treatmentsMedicines");

            migrationBuilder.DropForeignKey(
                name: "FK_treatmentsMedicines_treatments_TreatmentId",
                table: "treatmentsMedicines");

            migrationBuilder.DropIndex(
                name: "IX_treatmentsMedicines_MedicineId",
                table: "treatmentsMedicines");

            migrationBuilder.DropIndex(
                name: "IX_treatmentsMedicines_TreatmentId",
                table: "treatmentsMedicines");

            migrationBuilder.DropIndex(
                name: "IX_treatments_ConsultationId",
                table: "treatments");

            migrationBuilder.DropIndex(
                name: "IX_pets_OwnerId",
                table: "pets");

            migrationBuilder.DropIndex(
                name: "IX_consultations_PetId",
                table: "consultations");

            migrationBuilder.DropIndex(
                name: "IX_consultations_VeterinaryId",
                table: "consultations");

            migrationBuilder.DropColumn(
                name: "MedicineId",
                table: "treatmentsMedicines");

            migrationBuilder.DropColumn(
                name: "TreatmentId",
                table: "treatmentsMedicines");

            migrationBuilder.DropColumn(
                name: "ConsultationId",
                table: "treatments");

            migrationBuilder.DropColumn(
                name: "PetId",
                table: "consultations");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "pets",
                newName: "Species");

            migrationBuilder.RenameColumn(
                name: "VeterinaryId",
                table: "consultations",
                newName: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_treatmentsMedicines_IdMedicine",
                table: "treatmentsMedicines",
                column: "IdMedicine");

            migrationBuilder.CreateIndex(
                name: "IX_treatmentsMedicines_IdTreatment",
                table: "treatmentsMedicines",
                column: "IdTreatment");

            migrationBuilder.CreateIndex(
                name: "IX_treatments_IdConsultation",
                table: "treatments",
                column: "IdConsultation");

            migrationBuilder.CreateIndex(
                name: "IX_pets_IdOwner",
                table: "pets",
                column: "IdOwner");

            migrationBuilder.CreateIndex(
                name: "IX_consultations_IdPet",
                table: "consultations",
                column: "IdPet");

            migrationBuilder.CreateIndex(
                name: "IX_consultations_IdVeterinary",
                table: "consultations",
                column: "IdVeterinary");

            migrationBuilder.AddForeignKey(
                name: "FK_consultations_pets_IdPet",
                table: "consultations",
                column: "IdPet",
                principalTable: "pets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_consultations_veterinaries_IdVeterinary",
                table: "consultations",
                column: "IdVeterinary",
                principalTable: "veterinaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pets_owners_IdOwner",
                table: "pets",
                column: "IdOwner",
                principalTable: "owners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_treatments_consultations_IdConsultation",
                table: "treatments",
                column: "IdConsultation",
                principalTable: "consultations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_treatmentsMedicines_medicines_IdMedicine",
                table: "treatmentsMedicines",
                column: "IdMedicine",
                principalTable: "medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_treatmentsMedicines_treatments_IdTreatment",
                table: "treatmentsMedicines",
                column: "IdTreatment",
                principalTable: "treatments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_consultations_pets_IdPet",
                table: "consultations");

            migrationBuilder.DropForeignKey(
                name: "FK_consultations_veterinaries_IdVeterinary",
                table: "consultations");

            migrationBuilder.DropForeignKey(
                name: "FK_pets_owners_IdOwner",
                table: "pets");

            migrationBuilder.DropForeignKey(
                name: "FK_treatments_consultations_IdConsultation",
                table: "treatments");

            migrationBuilder.DropForeignKey(
                name: "FK_treatmentsMedicines_medicines_IdMedicine",
                table: "treatmentsMedicines");

            migrationBuilder.DropForeignKey(
                name: "FK_treatmentsMedicines_treatments_IdTreatment",
                table: "treatmentsMedicines");

            migrationBuilder.DropIndex(
                name: "IX_treatmentsMedicines_IdMedicine",
                table: "treatmentsMedicines");

            migrationBuilder.DropIndex(
                name: "IX_treatmentsMedicines_IdTreatment",
                table: "treatmentsMedicines");

            migrationBuilder.DropIndex(
                name: "IX_treatments_IdConsultation",
                table: "treatments");

            migrationBuilder.DropIndex(
                name: "IX_pets_IdOwner",
                table: "pets");

            migrationBuilder.DropIndex(
                name: "IX_consultations_IdPet",
                table: "consultations");

            migrationBuilder.DropIndex(
                name: "IX_consultations_IdVeterinary",
                table: "consultations");

            migrationBuilder.RenameColumn(
                name: "Species",
                table: "pets",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "consultations",
                newName: "VeterinaryId");

            migrationBuilder.AddColumn<int>(
                name: "MedicineId",
                table: "treatmentsMedicines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TreatmentId",
                table: "treatmentsMedicines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConsultationId",
                table: "treatments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PetId",
                table: "consultations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_treatmentsMedicines_MedicineId",
                table: "treatmentsMedicines",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_treatmentsMedicines_TreatmentId",
                table: "treatmentsMedicines",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_treatments_ConsultationId",
                table: "treatments",
                column: "ConsultationId");

            migrationBuilder.CreateIndex(
                name: "IX_pets_OwnerId",
                table: "pets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_consultations_PetId",
                table: "consultations",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_consultations_VeterinaryId",
                table: "consultations",
                column: "VeterinaryId");

            migrationBuilder.AddForeignKey(
                name: "FK_consultations_pets_PetId",
                table: "consultations",
                column: "PetId",
                principalTable: "pets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_consultations_veterinaries_VeterinaryId",
                table: "consultations",
                column: "VeterinaryId",
                principalTable: "veterinaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pets_owners_OwnerId",
                table: "pets",
                column: "OwnerId",
                principalTable: "owners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_treatments_consultations_ConsultationId",
                table: "treatments",
                column: "ConsultationId",
                principalTable: "consultations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_treatmentsMedicines_medicines_MedicineId",
                table: "treatmentsMedicines",
                column: "MedicineId",
                principalTable: "medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_treatmentsMedicines_treatments_TreatmentId",
                table: "treatmentsMedicines",
                column: "TreatmentId",
                principalTable: "treatments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
