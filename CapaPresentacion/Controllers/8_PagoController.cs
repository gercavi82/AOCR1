using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;
using CapaNegocio;
using CapaModelo;

namespace CapaPresentacion.Controllers
{
    public class PagoController : Controller
    {
        private readonly PagoBL _bl = new PagoBL();

        // ============================================================
        // DETALLE: lista de pagos por solicitud
        // ============================================================
        public ActionResult Detalle(int solicitudId)
        {
            var pagos = _bl.ObtenerPorSolicitud(solicitudId);
            if (pagos == null)
                pagos = new List<Pago>();

            ViewBag.SolicitudId = solicitudId;
            return View(pagos);
        }

        // ============================================================
        // SUBIR COMPROBANTE
        // ============================================================
        [HttpPost]
        public ActionResult SubirComprobante(int id, int solicitudId, HttpPostedFileBase archivo)
        {
            try
            {
                if (archivo != null && archivo.ContentLength > 0)
                {
                    string carpetaVirtual = "/PDF/Pagos/";
                    string nombreArchivo = id + Path.GetExtension(archivo.FileName);
                    string rutaVirtual = carpetaVirtual + nombreArchivo;
                    string rutaFisica = Server.MapPath(rutaVirtual);

                    var carpetaFisica = Path.GetDirectoryName(rutaFisica);
                    if (!Directory.Exists(carpetaFisica))
                    {
                        Directory.CreateDirectory(carpetaFisica);
                    }

                    archivo.SaveAs(rutaFisica);

                    var pago = _bl.ObtenerPorId(id);
                    if (pago != null)
                    {
                        string usuario = Session["CodigoUsuario"]?.ToString() ?? "SISTEMA";

                        pago.RutaComprobante = rutaVirtual;
                        pago.UsuarioValidacion = usuario;
                        pago.FechaValidacion = DateTime.Now;

                        _bl.Actualizar(pago);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al subir comprobante: " + ex.Message;
            }

            return RedirectToAction("Detalle", new { solicitudId });
        }

        // ============================================================
        // VALIDAR (APROBAR) PAGO
        // ============================================================
        public ActionResult Validar(int id, int solicitudId)
        {
            try
            {
                string usuario = Session["CodigoUsuario"]?.ToString() ?? "SISTEMA";
                var pago = _bl.ObtenerPorId(id);

                if (pago != null)
                {
                    pago.Estado = "APROBADO";
                    pago.FechaValidacion = DateTime.Now;
                    pago.UsuarioValidacion = usuario;

                    _bl.Actualizar(pago);
                    TempData["Success"] = "Pago aprobado correctamente.";
                }
                else
                {
                    TempData["Error"] = "Pago no encontrado.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al aprobar pago: " + ex.Message;
            }

            return RedirectToAction("Detalle", new { solicitudId });
        }

        // ============================================================
        // RECHAZAR PAGO
        // ============================================================
        public ActionResult Rechazar(int id, int solicitudId, string motivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    TempData["Error"] = "Debe indicar el motivo del rechazo.";
                    return RedirectToAction("Detalle", new { solicitudId });
                }

                string usuario = Session["CodigoUsuario"]?.ToString() ?? "SISTEMA";
                var pago = _bl.ObtenerPorId(id);

                if (pago != null)
                {
                    pago.Estado = "RECHAZADO";
                    pago.FechaValidacion = DateTime.Now;
                    pago.UsuarioValidacion = usuario;
                    pago.ObservacionesValidacion = motivo;

                    _bl.Actualizar(pago);
                    TempData["Success"] = "Pago rechazado correctamente.";
                }
                else
                {
                    TempData["Error"] = "Pago no encontrado.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al rechazar pago: " + ex.Message;
            }

            return RedirectToAction("Detalle", new { solicitudId });
        }
    }
}
