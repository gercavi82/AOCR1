using System;
using System.Web.Mvc;
using CapaModelo;
using CapaNegocio;
using CapaDatos.DAOs;

namespace CapaPresentacion.Controllers
{
    [Authorize] // No restringas aquí para no bloquear otras acciones por rol
    public class TecnicoController : Controller
    {
        // ✅ Según tu error, tu carpeta REAL parece ser: Views/Tecnico
        // Si NO es esa, cámbiala a la carpeta real (por ejemplo: "~/Views/Tecnico/")
        private const string VIEWS_TECNICO = "~/Views/Tecnico/";

        // =======================================================
        // LISTADO
        // =======================================================
        [Authorize(Roles = "Tecnico,Administrador")]
        public ActionResult Index()
        {
            var lista = TecnicoBL.ObtenerTodos();
            return View(VIEWS_TECNICO + "Index.cshtml", lista);
        }

        // =======================================================
        // CREAR
        // =======================================================
        [Authorize(Roles = "Administrador")]
        public ActionResult Crear()
        {
            return View(VIEWS_TECNICO + "Crear.cshtml");
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(Tecnico modelo)
        {
            if (!ModelState.IsValid)
                return View(VIEWS_TECNICO + "Crear.cshtml", modelo);

            string mensaje;
            bool ok = TecnicoBL.Insertar(modelo, out mensaje);

            if (!ok)
            {
                ViewBag.Error = mensaje;
                return View(VIEWS_TECNICO + "Crear.cshtml", modelo);
            }

            TempData["Success"] = "Técnico creado correctamente.";
            return RedirectToAction("Index");
        }

        // =======================================================
        // EDITAR
        // =======================================================
        [Authorize(Roles = "Administrador")]
        public ActionResult Editar(int id)
        {
            if (id <= 0) return new HttpStatusCodeResult(400, "ID inválido.");

            var modelo = TecnicoBL.ObtenerPorId(id);
            if (modelo == null)
            {
                TempData["Error"] = "Técnico no encontrado.";
                return RedirectToAction("Index");
            }

            return View(VIEWS_TECNICO + "Editar.cshtml", modelo);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Tecnico modelo)
        {
            if (!ModelState.IsValid)
                return View(VIEWS_TECNICO + "Editar.cshtml", modelo);

            string mensaje;
            bool ok = TecnicoBL.Actualizar(modelo, out mensaje);

            if (!ok)
            {
                ViewBag.Error = mensaje;
                return View(VIEWS_TECNICO + "Editar.cshtml", modelo);
            }

            TempData["Success"] = "Técnico actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // =======================================================
        // ELIMINAR
        // =======================================================
        [Authorize(Roles = "Administrador")]
        public ActionResult Eliminar(int id)
        {
            if (id <= 0) return new HttpStatusCodeResult(400, "ID inválido.");

            string mensaje;
            bool ok = TecnicoBL.Eliminar(id, out mensaje);

            TempData[ok ? "Success" : "Error"] = ok
                ? "Técnico eliminado correctamente."
                : mensaje;

            return RedirectToAction("Index");
        }

        // =======================================================
        // ASIGNAR INSPECTOR (GET)
        // =======================================================
        [HttpGet]
        [Authorize(Roles = "Tecnico,Administrador,CoordinadorInspecciones")]
        public ActionResult AsignarInspector(int? solicitudId)
        {
            if (!solicitudId.HasValue || solicitudId.Value <= 0)
            {
                TempData["Error"] = "ID de solicitud no proporcionado o inválido.";
                return RedirectToAction("Index");
            }

            var solicitud = SolicitudAOCRBL.ObtenerPorId(solicitudId.Value);
            if (solicitud == null)
            {
                TempData["Error"] = "Solicitud no encontrada.";
                return RedirectToAction("Index");
            }

            var inspectores = UsuarioBL.ObtenerInspectores();
            ViewBag.Inspectores = new SelectList(inspectores, "CodigoUsuario", "NombreCompleto");

            return View(VIEWS_TECNICO + "AsignarInspector.cshtml", solicitud);
        }

        // =======================================================
        // ASIGNAR INSPECTOR (POST)
        // =======================================================
        [HttpPost]
        [Authorize(Roles = "Tecnico,Administrador,CoordinadorInspecciones")]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarInspector(int solicitudId, int inspectorPrincipal, int? inspectorApoyo,
                                            DateTime fechaInspeccion, string observaciones)
        {
            if (solicitudId <= 0)
            {
                TempData["Error"] = "Solicitud inválida.";
                return RedirectToAction("Index");
            }

            try
            {
                string mensaje;
                bool ok = SolicitudAOCRBL.AsignarInspectores(
                    solicitudId,
                    inspectorPrincipal,
                    inspectorApoyo,
                    fechaInspeccion,
                    observaciones,
                    out mensaje
                );

                TempData[ok ? "Success" : "Error"] = mensaje;

                return ok
                    ? RedirectToAction("Index")
                    : RedirectToAction("AsignarInspector", new { solicitudId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error crítico: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

       
    }
}
