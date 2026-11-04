using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suelta.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cuentas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodigoHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreadaEnUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuentas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recordatorios",
                columns: table => new
                {
                    CuentaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Categoria = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SinHora = table.Column<bool>(type: "INTEGER", nullable: false),
                    Repeticion = table.Column<int>(type: "INTEGER", nullable: false),
                    AnclaDia = table.Column<int>(type: "INTEGER", nullable: true),
                    Monto = table.Column<decimal>(type: "TEXT", nullable: true),
                    Dijiste = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Hecho = table.Column<bool>(type: "INTEGER", nullable: false),
                    PospuestoHasta = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreadoEnUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EliminadoEnUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecibidoEnUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recordatorios", x => new { x.CuentaId, x.Id });
                    table.ForeignKey(
                        name: "FK_Recordatorios_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_CodigoHash",
                table: "Cuentas",
                column: "CodigoHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recordatorios_CuentaId_FechaHora",
                table: "Recordatorios",
                columns: new[] { "CuentaId", "FechaHora" });

            migrationBuilder.CreateIndex(
                name: "IX_Recordatorios_CuentaId_RecibidoEnUtc",
                table: "Recordatorios",
                columns: new[] { "CuentaId", "RecibidoEnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recordatorios");

            migrationBuilder.DropTable(
                name: "Cuentas");
        }
    }
}
