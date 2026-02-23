using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreaStoreProcedure_Facturas_Crear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE PROCEDURE Facturas_Crear
	-- Add the parameters for the stored procedure here
	@fechaInicio datetime,
	@fechaFin datetime
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	
--Inserto las facturas por usuario segun sus peticiones
declare @montoPorCadaPeticion decimal (4,4) = 1.0/2 --1 dolar por cada 2 peticiones

insert into Facturas(UsuarioId, Monto, FechaEmision,FechaLimiteDePago,Pagada)

select UsuarioId, 
count(*) * @montoPorCadaPeticion as monto,
GETDATE() as FechaEmision,
Dateadd(d,60,getdate()) as FechaLimitePago,
0 as pagada
from Peticiones 
inner join LlavesAPI
on LlavesAPI.Id = Peticiones.LlaveId
where LlavesAPI.TipoLlave != 1 and FechaPeticion >= @fechaInicio 
  and FechaPeticion < @fechaFin
group by UsuarioId

----
--Genero registro de las facturas emitidas (a mes vencido), por lo cual hago un case para calcularlo, especialmente para enero por ejemplo
Insert into FacturasEmitidas(Mes,Año)
Select 
	Case MONTH(GETDATE())
	when 1 then 12
	else MONTH(GETDATE()) - 1  End as MES,

	Case MONTH(GETDATE())
	when 1 then Year(GETDATE()) - 1
	else YEAR(GETDATE()) End as Año
END


");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE Facturas_Crear");
        }
    }
}
