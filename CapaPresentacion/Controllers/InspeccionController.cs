using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using CapaNegocio;
using CapaModelo;
using CapaDatos.DAOs;

namespace CapaPresentacion.Controllers
{
    [Authorize]
    public class InspeccionController : Controller
    {
        private readonly HallazgoBL _hallazgoBL;

        private const string ROL_ADMIN = "Administrador";
        private const string ROL_COORD = "CoordinadorInspecciones";
        private const string ROL_INSPECTOR = "Inspector";
        private const string ROL_JEFATURA = "JefaturaTecnica";

        // Seguridad: tamaño máximo permitido para PDF (10MB)
        private const int MAX_PDF_BYTES = 10 * 1024 * 1024;

        // Carpeta de informes
        private const string CARPETA_VIRTUAL_INFORMES = "~/Uploads/Inspecciones";

        public InspeccionController()
        {
            _hallazgoBL = new HallazgoBL();
        }

        private int ObtenerCodigoUsuario()
        {
            if (Session["CodigoUsuario"] != null &&
                int.TryParse(Session["CodigoUsuario"].ToString(), out var id))
                return id;

            return 0;
        }

        private bool EsAdmin() => User != null && User.IsInRole(ROL_ADMIN);

        private bool PuedeAccederInspeccion(Inspeccion ins)
        {
            if (ins == null) return false;
            if (EsAdmin()) return true;

            if (User.IsInRole(ROL_COORD) || User.IsInRole(ROL_JEFATURA))
                return true;

            var codigoUsuario = ObtenerCodigoUsuario();
            if (User.IsInRole(ROL_INSPECTOR))
                return ins.CodigoInspector.HasValue && ins.CodigoInspector.Value == codigoUsuario;

            return false;
        }

        // ============================================================
        // ✅ LISTADO (POR ROL)
        // ============================================================
        [Authorize(Roles = ROL_COORD + "," + ROL_INSPECTOR + "," + ROL_JEFATURA + "," + ROL_ADMIN)]
        public ActionResult Index()
        {
            List<Inspeccion> lista;

            if (EsAdmin() || User.IsInRole(ROL_COORD) || User.IsInRole(ROL_JEFATURA))
                lista = InspeccionBL.ListarTodas();
            else
                lista = InspeccionBL.ListarPorInspector(ObtenerCodigoUsuario());

            return View("~/Views/Inspeccion/Index.cshtml", lista);
        }

        // ============================================================
        // ✅ DETALLE
        // ============================================================
        [Authorize(Roles = ROL_COORD + "," + ROL_INSPECTOR + "," + ROL_JEFATURA + "," + ROL_ADMIN)]
        public ActionResult Detalle(int id)
        {
            if (id <= 0) return new HttpStatusCodeResult(400, "ID inválido.");

            var inspeccion = InspeccionDAO.ObtenerPorId(id);
            if (inspeccion == null) return HttpNotFound("Inspección no encontrada.");

            if (!PuedeAccederInspeccion(inspeccion))
                return new HttpStatusCodeResult(403, "No autorizado para ver esta inspección.");

            ViewBag.Hallazgos = _hallazgoBL.ObtenerPorInspeccion(id);

            return View("~/Views/Inspeccion/Detalle.cshtml", inspeccion);
        }

        // ============================================================
        // ✅ CREAR (GET)
        // ============================================================
        [Authorize(Roles = ROL_COORD + "," + ROL_ADMIN)]
        public ActionResult Crear(int codigoSolicitud)
        {
            if (codigoSolicitud <= 0) return new HttpStatusCodeResult(400, "Código de solicitud inválido.");

            var modelo = new Inspeccion { CodigoSolicitud = codigoSolicitud };
            return View("~/Views/Inspeccion/Crear.cshtml", modelo);
        }

        // ============================================================
        // ✅ CREAR (POST)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = ROL_COORD + "," + ROL_ADMIN)]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(Inspeccion model)
        {
            if (model == null) return new HttpStatusCodeResult(400, "Modelo inválido.");

            if (!ModelState.IsValid)
                return View("~/Views/Inspeccion/Crear.cshtml", model);

            var codigoUsuario = ObtenerCodigoUsuario();
            if (codigoUsuario <= 0)
            {
                ViewBag.Error = "No se pudo identificar el usuario en sesión.";
                return View("~/Views/Inspeccion/Crear.cshtml", model);
            }

            bool ok = InspeccionBL.Crear(model, codigoUsuario);

            if (ok)
                return RedirectToAction("Detalle", new { id = model.CodigoInspeccion });

            ViewBag.Error = "No se pudo crear la inspección.";
            return View("~/Views/Inspeccion/Crear.cshtml", model);
        }

        // ============================================================
        // ✅ EDITAR (GET)
        // ============================================================
        [Authorize(Roles = ROL_COORD + "," + ROL_ADMIN)]
        public ActionResult Editar(int id)
        {
            if (id <= 0) return new HttpStatusCodeResult(400, "ID inválido.");

            var inspeccion = InspeccionDAO.ObtenerPorId(id);
            if (inspeccion == null) return HttpNotFound("Inspección no encontrada.");

            return View("~/Views/Inspeccion/Editar.cshtml", inspeccion);
        }

        // ============================================================
        // ✅ EDITAR (POST)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = ROL_COORD + "," + ROL_ADMIN)]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Inspeccion model)
        {
            if (model == null) return new HttpStatusCodeResult(400, "Modelo inválido.");

            if (!ModelState.IsValid)
                return View("~/Views/Inspeccion/Editar.cshtml", model);

            model.UpdatedAt = DateTime.Now;
            model.UpdatedBy = ObtenerCodigoUsuario();

            bool ok = InspeccionBL.Actualizar(model);

            TempData[ok ? "Success" : "Error"] = ok
                ? "Inspección actualizada correctamente."
                : "No se pudo actualizar la inspección.";

            return RedirectToAction("Detalle", new { id = model.CodigoInspeccion });
        }

        // ============================================================
        // ✅ CAMBIAR ESTADO
        // ============================================================
        [HttpPost]
        [Authorize(Roles = ROL_JEFATURA + "," + ROL_COORD + "," + ROL_ADMIN)]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarEstado(int id, string estado)
        {
            if (id <= 0) return new HttpStatusCodeResult(400, "ID inválido.");

            if (string.IsNullOrWhiteSpace(estado))
            {
                TempData["Error"] = "Debe seleccionar un estado.";
                return RedirectToAction("Detalle", new { id });
            }

            try
            {
                int codigoUsuario = ObtenerCodigoUsuario();
                bool ok = InspeccionBL.CambiarEstado(id, estado.Trim(), codigoUsuario);

                TempData[ok ? "Success" : "Error"] = ok
                    ? "Estado actualizado correctamente."
                    : "No se pudo actualizar el estado.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Detalle", new { id });
        }

        // ============================================================
        // ✅✅✅ VER INFORME (ÚNICO) - SEGURO
        // ============================================================
        [HttpGet]
        [Authorize(Roles = ROL_COORD + "," + ROL_INSPECTOR + "," + ROL_JEFATURA + "," + ROL_ADMIN)]
        public ActionResult VerInforme(int id)
        {
            if (id <= 0) return new HttpStatusCodeResult(400, "ID inválido.");

            var inspeccion = InspeccionDAO.ObtenerPorId(id);
            if (inspeccion == null)
                return HttpNotFound("Inspección no encontrada.");

            if (!PuedeAccederInspeccion(inspeccion))
                return new HttpStatusCodeResult(403, "No autorizado para ver el informe.");

            // Preferido: RutaInforme (guárdalo así en BD)
            var rutaRelativa = inspeccion.RutaInforme;

            if (string.IsNullOrWhiteSpace(rutaRelativa))
                return HttpNotFound("La inspección aún no tiene informe cargado.");

            if (!rutaRelativa.StartsWith("/"))
                rutaRelativa = "/" + rutaRelativa;

            var fullPath = Server.MapPath("~" + rutaRelativa);

            var baseDir = Server.MapPath(CARPETA_VIRTUAL_INFORMES);
            if (!EsRutaDentroDeBase(fullPath, baseDir))
                return new HttpStatusCodeResult(400, "Ruta de informe inválida.");

            if (!System.IO.File.Exists(fullPath))
                return HttpNotFound("El archivo del informe no existe en el servidor.");

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.AddHeader("Content-Disposition", "inline; filename=InformeInspeccion_" + id + ".pdf");

            return File(fullPath, "application/pdf");
        }

        // ============================================================
        // ✅✅✅ SUBIR INFORME - SEGURO (PDF)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = ROL_INSPECTOR + "," + ROL_ADMIN)]
        [ValidateAntiForgeryToken]
        public ActionResult SubirInforme(int id)
        {
            if (id <= 0) return new HttpStatusCodeResult(400, "ID inválido.");

            var inspeccion = InspeccionDAO.ObtenerPorId(id);
            if (inspeccion == null) return HttpNotFound("Inspección no encontrada.");

            if (!PuedeAccederInspeccion(inspeccion))
                return new HttpStatusCodeResult(403, "No autorizado para subir informe.");

            HttpPostedFileBase archivo = Request.Files["Informe"];
            if (archivo == null || archivo.ContentLength <= 0)
            {
                TempData["Error"] = "No se recibió ningún archivo.";
                return RedirectToAction("Detalle", new { id });
            }

            if (archivo.ContentLength > MAX_PDF_BYTES)
            {
                TempData["Error"] = "El archivo supera el tamaño permitido (10 MB).";
                return RedirectToAction("Detalle", new { id });
            }

            if (!archivo.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Solo se permiten archivos PDF (.pdf).";
                return RedirectToAction("Detalle", new { id });
            }

            if (!TieneFirmaPdf(archivo))
            {
                TempData["Error"] = "El archivo no parece un PDF válido (firma).";
                return RedirectToAction("Detalle", new { id });
            }

            string carpetaFisica = Server.MapPath(CARPETA_VIRTUAL_INFORMES);
            if (!Directory.Exists(carpetaFisica))
                Directory.CreateDirectory(carpetaFisica);

            string nombreArchivo = Guid.NewGuid().ToString("N") + ".pdf";
            string rutaFisica = Path.Combine(carpetaFisica, nombreArchivo);

            archivo.SaveAs(rutaFisica);

            string rutaRelativa = "/Uploads/Inspecciones/" + nombreArchivo;
            int codigoUsuario = ObtenerCodigoUsuario();

            bool ok = InspeccionBL.GuardarInforme(id, rutaRelativa, codigoUsuario);

            TempData[ok ? "Success" : "Error"] = ok
                ? "Informe cargado y asociado correctamente."
                : "El PDF se guardó, pero no se pudo asociar en base de datos.";

            return RedirectToAction("Detalle", new { id });
        }

        // ============================================================
        // ✅ REGISTRAR HALLAZGO
        // ============================================================
        [HttpPost]
        [Authorize(Roles = ROL_INSPECTOR + "," + ROL_ADMIN)]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarHallazgo(Hallazgo h)
        {
            if (h == null || h.CodigoInspeccion <= 0)
                return RedirectToAction("Index");

            if (!ModelState.IsValid)
                return RedirectToAction("Detalle", new { id = h.CodigoInspeccion });

            var inspeccion = InspeccionDAO.ObtenerPorId(h.CodigoInspeccion);
            if (inspeccion == null) return HttpNotFound("Inspección no encontrada.");

            if (!PuedeAccederInspeccion(inspeccion))
                return new HttpStatusCodeResult(403, "No autorizado para registrar hallazgos.");

            var codigoUsuario = ObtenerCodigoUsuario();
            string usuarioNombre = User?.Identity?.Name ?? codigoUsuario.ToString();

            bool ok = _hallazgoBL.Crear(h, usuarioNombre);

            TempData[ok ? "Success" : "Error"] = ok
                ? "Hallazgo registrado correctamente."
                : "Error al registrar hallazgo.";

            return RedirectToAction("Detalle", new { id = h.CodigoInspeccion });
        }

        // ============================================================
        // ✅ CERRAR INSPECCIÓN
        // ============================================================
        [HttpPost]
        [Authorize(Roles = ROL_JEFATURA + "," + ROL_ADMIN)]
        [ValidateAntiForgeryToken]
        public ActionResult Cerrar(int id, string resultado)
        {
            if (id <= 0) return new HttpStatusCodeResult(400, "ID inválido.");

            var inspeccion = InspeccionDAO.ObtenerPorId(id);
            if (inspeccion == null) return HttpNotFound("Inspección no encontrada.");

            var codigoUsuario = ObtenerCodigoUsuario();
            bool ok = InspeccionBL.CerrarInspeccion(id, resultado, codigoUsuario);

            TempData[ok ? "Success" : "Error"] = ok
                ? "Inspección cerrada correctamente."
                : "No se pudo cerrar la inspección.";

            return RedirectToAction("Detalle", new { id });
        }

        // ============================================================
        // ✅✅✅ PLANIFICACIÓN (GET ÚNICO)
        // ============================================================
        [HttpGet]
        [Authorize(Roles = ROL_COORD + "," + ROL_ADMIN)]
        public ActionResult Planificacion(int id)
        {
            if (id <= 0) return new HttpStatusCodeResult(400, "ID inválido.");

            var inspeccion = InspeccionDAO.ObtenerPorId(id);
            if (inspeccion == null) return HttpNotFound("Inspección no encontrada.");

            if (!PuedeAccederInspeccion(inspeccion))
                return new HttpStatusCodeResult(403, "No autorizado para planificar esta inspección.");

            return View("~/Views/Inspeccion/Planificacion.cshtml", inspeccion);
        }

        // ============================================================
        // ✅✅✅ PLANIFICACIÓN (POST ÚNICO)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = ROL_COORD + "," + ROL_ADMIN)]
        [ValidateAntiForgeryToken]
        public ActionResult Planificacion(
            int codigoInspeccion,
            DateTime fechaInspeccion,
            TimeSpan horaInicio,
            int duracionEstimada,
            string ubicacion,
            string latitud,
            string longitud,
            string tipoInspeccion,
            string alcance,
            string equiposNecesarios,
            string contactoSitio,
            string telefonoContacto,
            string observaciones)
        {
            if (codigoInspeccion <= 0)
                return new HttpStatusCodeResult(400, "ID de inspección inválido.");

            var inspeccion = InspeccionDAO.ObtenerPorId(codigoInspeccion);
            if (inspeccion == null)
            {
                TempData["Error"] = "No se encontró la inspección.";
                return RedirectToAction("Index");
            }

            if (!PuedeAccederInspeccion(inspeccion))
                return new HttpStatusCodeResult(403, "No autorizado para planificar esta inspección.");

            inspeccion.FechaProgramada = fechaInspeccion;
            inspeccion.HoraProgramada = horaInicio;
            inspeccion.DuracionEstimada = duracionEstimada;

            inspeccion.Lugar = ubicacion;
            inspeccion.Latitud = latitud;
            inspeccion.Longitud = longitud;

            inspeccion.Tipo = tipoInspeccion;
            inspeccion.ObservacionesGenerales = observaciones;
            inspeccion.HallazgosPrincipales = alcance;

            inspeccion.Comentarios =
                $"Contacto: {contactoSitio} - Tel: {telefonoContacto}. Equipos: {equiposNecesarios}";

            inspeccion.Estado = "PROGRAMADA";
            inspeccion.UpdatedAt = DateTime.Now;
            inspeccion.UpdatedBy = ObtenerCodigoUsuario();

            bool ok = InspeccionBL.Actualizar(inspeccion);

            TempData[ok ? "Success" : "Error"] = ok
                ? "Planificación guardada correctamente."
                : "Error al guardar la planificación.";

            return RedirectToAction("Detalle", new { id = codigoInspeccion });
        }

        // ============================================================
        // ✅ HELPERS DE SEGURIDAD
        // ============================================================
        private bool TieneFirmaPdf(HttpPostedFileBase archivo)
        {
            try
            {
                if (archivo == null || archivo.InputStream == null || !archivo.InputStream.CanRead)
                    return false;

                byte[] header = new byte[4];
                int read = archivo.InputStream.Read(header, 0, 4);
                archivo.InputStream.Position = 0;

                if (read < 4) return false;

                string sig = Encoding.ASCII.GetString(header);
                return sig == "%PDF";
            }
            catch
            {
                try { if (archivo?.InputStream != null) archivo.InputStream.Position = 0; } catch { }
                return false;
            }
        }

        private bool EsRutaDentroDeBase(string archivoFullPath, string baseDirFullPath)
        {
            try
            {
                var archivo = Path.GetFullPath(archivoFullPath);
                var baseDir = Path.GetFullPath(baseDirFullPath);

                if (!baseDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    baseDir += Path.DirectorySeparatorChar;

                return archivo.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
