using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using CapaModelo;
using CapaNegocio;
using CapaPresentacion.Models;

namespace CapaPresentacion.Controllers
{
    public class AccountController : Controller
    {
        // ============================
        // GET: Login
        // ============================
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // ============================
        // POST: Login
        // ============================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
                return View(model);

            string mensaje;
            Usuario usuario;
            List<string> roles;

            bool ok = UsuarioBL.Autenticar(
                model.Usuario,
                model.Contrasena,
                out usuario,
                out roles,
                out mensaje
            );

            if (!ok)
            {
                ModelState.AddModelError("", mensaje);
                return View(model);
            }

            // 🔐 ADMIN SUPREMO
            if (usuario.NombreUsuario.Equals("USU_ADMIN", StringComparison.InvariantCultureIgnoreCase))
            {
                roles = new List<string>
                {
                    "Administrador",
                    "Tecnico",
                    "Solicitante",
                    "Financiero",
                    "Inspector",
                    "JefaturaTecnica",
                    "Direccion",
                    "CoordinacionLegal"
                };
            }

            // ============================
            // COOKIE DE AUTENTICACIÓN
            // ============================
            string rolesString = roles != null && roles.Count > 0
                ? string.Join(",", roles)
                : string.Empty;

            var ticket = new FormsAuthenticationTicket(
                1,
                usuario.NombreUsuario,
                DateTime.Now,
                DateTime.Now.AddMinutes(60),
                model.Recordarme,
                rolesString,
                FormsAuthentication.FormsCookiePath
            );

            string encrypted = FormsAuthentication.Encrypt(ticket);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
            {
                HttpOnly = true
            };

            if (model.Recordarme)
                cookie.Expires = DateTime.Now.AddDays(7);

            Response.Cookies.Add(cookie);

            // ============================
            // ✅ SESIÓN (ESTO ES LO CLAVE)
            // ============================
            Session["CodigoUsuario"] = usuario.Id;
            Session["NombreUsuario"] = usuario.NombreCompleto;
            Session["Correo"] = usuario.Email;

            Session["Roles"] = roles;                 // 🔑 LISTA COMPLETA
            Session["Rol"] = roles.Count > 0 ? roles[0] : null; // 🔑 ROL ACTIVO

            // ============================
            // REDIRECCIÓN
            // ============================
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // ============================
        // CAMBIAR ROL
        // ============================
        [Authorize]
        public ActionResult CambiarRol(string rolSeleccionado)
        {
            var roles = Session["Roles"] as List<string>;

            if (roles != null && roles.Contains(rolSeleccionado))
            {
                Session["Rol"] = rolSeleccionado;
            }

            return RedirectToAction("Index", "Home");
        }

        // ============================
        // LOGOUT
        // ============================
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // ============================
        // RECUPERAR CONTRASEÑA
        // ============================
        [HttpPost]
        [AllowAnonymous]
        public JsonResult EnviarRecuperar(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Json(new { ok = false, mensaje = "Debe ingresar un correo electrónico válido." });

            string mensaje;
            bool enviado = UsuarioBL.RestablecerContrasenaPorEmail(email, out mensaje);

            return Json(new { ok = enviado, mensaje });
        }

        // ============================
        // MODAL REGISTRO
        // ============================
        public ActionResult _ModalRegistroUsuario()
        {
            var model = new UsuarioCreateViewModel
            {
                RolesDisponibles = RolBL.ObtenerActivos()
            };
            return PartialView("_ModalRegistroUsuario", model);
        }
    }
}
