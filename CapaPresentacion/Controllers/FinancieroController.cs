using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CapaNegocio;
using CapaModelo;

namespace CapaPresentacion.Controllers
{
    [Authorize]
    public class FinancieroController : Controller
    {
        private readonly SolicitudBL _solicitudBL;
        private readonly PagoBL _pagoBL;

        public FinancieroController()
        {
            _solicitudBL = new SolicitudBL();
            _pagoBL = new PagoBL();
        }

        // ==========================================
        // 0. DASHBOARD / INDEX
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Financiero,Administrador")]
        public ActionResult Index()
        {
            try
            {
                var solicitudes = _solicitudBL.ObtenerTodos()
                    .Where(s => s.Estado == "PENDIENTE")
                    .ToList();

                return View("Index", solicitudes);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar el dashboard: " + ex.Message;
                return View("Index", new List<SolicitudAOCR>());
            }
        }

        // ==========================================
        // 1. LISTADO DE PAGOS PENDIENTES
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Financiero,Administrador")]
        public ActionResult ValidacionPagos()
        {
            try
            {
                var pagos = _pagoBL.ObtenerTodos()
                    .Where(p => p.Estado == "PENDIENTE")
                    .ToList();

                foreach (var p in pagos)
                    p.Solicitud = _solicitudBL.ObtenerDetalle(p.CodigoSolicitud);

                return View("ValidacionPagos", pagos);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar: " + ex.Message;
                return View("ValidacionPagos", new List<Pago>());
            }
        }

        // ==========================================
        // 2. VISTA DETALLE DE UN PAGO
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Financiero,Administrador")]
        public ActionResult ValidarPago(int id)
        {
            var pago = _pagoBL.ObtenerPorId(id);
            if (pago == null) return RedirectToAction("ValidacionPagos");

            pago.Solicitud = _solicitudBL.ObtenerDetalle(pago.CodigoSolicitud);
            return View("ValidarPago", pago);
        }

        // ==========================================
        // 3. PROCESAR VALIDACIÓN (APROBAR O RECHAZAR)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Financiero,Administrador")]
        public ActionResult ProcesarValidacion(int CodigoPago, string aprobado, string observaciones, decimal montoConfirmado)
        {
            try
            {
                if (aprobado == "false" && string.IsNullOrWhiteSpace(observaciones))
                {
                    TempData["Error"] = "Para rechazar un pago debe ingresar el motivo en observaciones.";
                    return RedirectToAction("ValidarPago", new { id = CodigoPago });
                }

                var pago = _pagoBL.ObtenerPorId(CodigoPago);
                if (pago == null)
                {
                    TempData["Error"] = "No se encontró el pago.";
                    return RedirectToAction("ValidacionPagos");
                }

                pago.Monto = montoConfirmado;
                pago.Estado = (aprobado == "true") ? "APROBADO" : "RECHAZADO";
                pago.ObservacionesValidacion = observaciones;
                pago.UsuarioValidacion = User.Identity.Name ?? "SISTEMA";
                pago.FechaValidacion = DateTime.Now;

                bool resultado = _pagoBL.Actualizar(pago);

                if (resultado)
                {
                    TempData["Exito"] = $"Pago N° {CodigoPago} procesado correctamente.";
                    return RedirectToAction("ValidacionPagos");
                }

                TempData["Error"] = "No se pudo actualizar el pago en la base de datos.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error crítico: " + ex.Message;
            }

            return RedirectToAction("ValidarPago", new { id = CodigoPago });
        }
    }
}
