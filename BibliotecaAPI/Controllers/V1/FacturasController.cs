using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace BibliotecaAPI.Controllers.V1
{
    [Route("api/v1/facturas")]
    [Authorize]
    [ApiController]
    [DeshabilitarLimitarPeticiones]
    public class FacturasController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public FacturasController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpPost]
        public async Task<ActionResult> Pagar(FacturaPagarDTO facturaPagarDTO)
        {
            var facturaBD = await context.Facturas
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.Id == facturaPagarDTO.FacturaId);

            if (facturaBD is null)
            {
                return NotFound();
            }
           

            if (facturaBD.Pagada)
            {
                ModelState.AddModelError(nameof(facturaPagarDTO.FacturaId), "La factura ya fue saldada");
                return ValidationProblem();
            }

            //Supongo que el pago fue exitoso

            facturaBD.Pagada = true;
            await context.SaveChangesAsync();

            var hayFacturasPendientesVencidas = await context.Facturas
                .AnyAsync(x => x.UsuarioId == facturaBD.UsuarioId && !x.Pagada && x.FechaLimiteDePago < DateTime.Today);

            if (!hayFacturasPendientesVencidas)
            {
                facturaBD.Usuario!.MalaPaga = false;
                await context.SaveChangesAsync();
            }
            return NoContent(); 
        }
    }
    }
