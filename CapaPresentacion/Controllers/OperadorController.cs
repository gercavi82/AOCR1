using System.Web.Mvc;

namespace CapaPresentacion.Controllers
{
    [Authorize]
    public class OperadorController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        // Si ya tienes Modificacion en el menú:
        public ActionResult Modificacion()
        {
            return View();
        }
    }
}
