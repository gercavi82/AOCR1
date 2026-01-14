using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CapaDatos.DAOs; // Solo para SolicitudDAO (Cabecera)
using CapaModelo;
using CapaNegocio;    // <--- IMPORTANTE: Usamos la Capa de Negocio

namespace CapaPresentacion.Controllers
{
    [Authorize]
    public class DocumentoController : Controller
    {
        // 1. Usamos la BL en lugar del DAO
        private readonly DocumentoBL _documentoBL;
        private readonly SolicitudAOCRDAO _solicitudDAO; // Solo para obtener datos de la solicitud (padre)
        private readonly string _rutaDocumentos;

        public DocumentoController()
        {
            _documentoBL = new DocumentoBL();
            _solicitudDAO = new SolicitudAOCRDAO();

            if (System.Web.HttpContext.Current != null)
            {
                _rutaDocumentos = System.Web.HttpContext.Current.Server.MapPath("~/Documentos/");
                if (!Directory.Exists(_rutaDocumentos))
                {
                    Directory.CreateDirectory(_rutaDocumentos);
                }
            }
        }

        #region Vistas Principales

        // GET: Documento/Lista/5
        public ActionResult Lista(int solicitudId)
        {
            try
            {
                var solicitud = _solicitudDAO.ObtenerPorId(solicitudId);
                if (solicitud == null) return RedirectToAction("Index", "SolicitudAOCR");

                // Llamada a la BL
                var documentos = _documentoBL.ObtenerPorSolicitud(solicitudId) ?? new List<Documento>();

                ViewBag.Stats = new
                {
                    Total = documentos.Count,
                    Aprobados = documentos.Count(d => d.Estado == "APROBADO"),
                    Pendientes = documentos.Count(d => d.Estado == "PENDIENTE")
                };

                ViewBag.Solicitud = solicitud;
                ViewBag.SolicitudId = solicitudId;

                return View(documentos);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Documento/RevisarDocumentos
        // [Authorize(Roles = "Administrador,Inspector")] // Descomentar luego
        public ActionResult RevisarDocumentos()
        {
            try
            {
                var todos = _documentoBL.ObtenerTodos() ?? new List<Documento>();

                // Filtramos por PENDIENTE (tu BL usa mayúsculas)
                var pendientes = todos
                    .Where(d => d.Estado != null && d.Estado.ToUpper() == "PENDIENTE")
                    .OrderByDescending(d => d.FechaSubida)
                    .ToList();

                return View(pendientes);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar bandeja: {ex.Message}";
                // Retornamos lista vacía para no causar error 302
                return View(new List<Documento>());
            }
        }

        // GET: Documento/Detalle/5
        public ActionResult Detalle(int id)
        {
            var doc = _documentoBL.ObtenerPorId(id);
            if (doc == null) return RedirectToAction("RevisarDocumentos");
            return View(doc);
        }

        #endregion

        #region Subir y Descargar

        // GET: Documento/Subir/5
        public ActionResult Subir(int solicitudId)
        {
            ViewBag.SolicitudId = solicitudId;
            ViewBag.TiposDocumento = ObtenerTiposDocumento();
            return View(new Documento { CodigoSolicitud = solicitudId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Subir(int solicitudId, string tipoDocumento, HttpPostedFileBase archivo, string observaciones)
        {
            string rutaCompleta = null;
            try
            {
                if (archivo == null || archivo.ContentLength == 0)
                {
                    TempData["Error"] = "Seleccione un archivo válido.";
                    return RedirectToAction("Subir", new { solicitudId });
                }

                // 1. Guardar físico (El Controller maneja el Stream HTTP)
                string ext = Path.GetExtension(archivo.FileName);
                string nombreFisico = $"{solicitudId}_{Guid.NewGuid()}{ext}";
                rutaCompleta = Path.Combine(_rutaDocumentos, nombreFisico);

                archivo.SaveAs(rutaCompleta);

                // 2. Preparar objeto para la BL
                var doc = new Documento
                {
                    CodigoSolicitud = solicitudId,
                    TipoDocumento = tipoDocumento,
                    NombreArchivo = Path.GetFileName(archivo.FileName),
                    RutaArchivo = rutaCompleta,
                    TamanioArchivo = archivo.ContentLength,
                    Observaciones = observaciones,
                    UsuarioRegistro = User.Identity.Name ?? "System"
                };

                // 3. La BL valida reglas de negocio y guarda en BD
                if (_documentoBL.Crear(doc))
                {
                    TempData["Exito"] = "Documento subido correctamente.";
                    return RedirectToAction("Lista", new { solicitudId });
                }
                else
                {
                    // Si la BL dice que no (reglas de negocio), borramos el archivo físico
                    if (System.IO.File.Exists(rutaCompleta)) System.IO.File.Delete(rutaCompleta);
                    TempData["Error"] = "No se pudo guardar el documento.";
                    return RedirectToAction("Subir", new { solicitudId });
                }
            }
            catch (Exception ex)
            {
                // Si hubo excepción en la BL (ej: extensión no permitida), borramos el archivo
                if (rutaCompleta != null && System.IO.File.Exists(rutaCompleta))
                    System.IO.File.Delete(rutaCompleta);

                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Subir", new { solicitudId });
            }
        }

        public ActionResult Descargar(int id)
        {
            try
            {
                var doc = _documentoBL.ObtenerPorId(id); // Usamos BL
                if (doc == null) return HttpNotFound();

                if (!System.IO.File.Exists(doc.RutaArchivo))
                {
                    TempData["Error"] = "El archivo físico no existe en el servidor.";
                    return RedirectToAction("Lista", new { solicitudId = doc.CodigoSolicitud });
                }

                byte[] bytes = System.IO.File.ReadAllBytes(doc.RutaArchivo);
                return File(bytes, "application/octet-stream", doc.NombreArchivo);
            }
            catch
            {
                return HttpNotFound();
            }
        }

        #endregion

        #region Gestión (Eliminar / Aprobar / Rechazar)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Eliminar(int id)
        {
            try
            {
                // La BL se encarga de borrar el archivo físico y el registro BD
                if (_documentoBL.Eliminar(id))
                {
                    TempData["Exito"] = "Documento eliminado.";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar.";
                }
                return RedirectToAction("RevisarDocumentos"); // O volver al historial
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("RevisarDocumentos");
            }
        }

        [HttpPost]
        public JsonResult CambiarEstado(int id, string estado, string observaciones)
        {
            try
            {
                bool resultado = false;
                string usuario = User.Identity.Name ?? "System";

                // Usamos los métodos específicos de la BL
                if (estado.ToUpper() == "APROBADO")
                {
                    resultado = _documentoBL.Aprobar(id, usuario, observaciones);
                }
                else if (estado.ToUpper() == "RECHAZADO")
                {
                    resultado = _documentoBL.Rechazar(id, usuario, observaciones);
                }
                else
                {
                    return Json(new { success = false, mensaje = "Estado no válido" });
                }

                return Json(new { success = resultado, mensaje = resultado ? "Actualizado" : "Error al actualizar" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        // Vista Editar solo para cambiar metadatos si fuera necesario
        public ActionResult Editar(int id)
        {
            var doc = _documentoBL.ObtenerPorId(id);
            if (doc == null) return HttpNotFound();

            ViewBag.TiposDocumento = ObtenerTiposDocumento();
            return View(doc);
        }

        #endregion

        #region Auxiliares
        private SelectList ObtenerTiposDocumento()
        {
            return new SelectList(new[] {
                new { Val="AOC", Txt="Certificado AOC" },
                new { Val="MEL", Txt="Lista MEL" },
                new { Val="MANUAL_OPS", Txt="Manual Operaciones" },
                new { Val="BORRADOR_AOCR", Txt="Borrador AOCR" },
                new { Val="OTRO", Txt="Otro" }
            }, "Val", "Txt");
        }
        #endregion
    }
}