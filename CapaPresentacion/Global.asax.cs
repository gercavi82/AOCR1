using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Security.Principal;
using Dapper;
using CapaDatos.DAOs;

namespace CapaPresentacion
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            HttpCookie authCookie = Context.Request.Cookies[FormsAuthentication.FormsCookieName];

            if (authCookie == null || string.IsNullOrEmpty(authCookie.Value))
                return;

            FormsAuthenticationTicket authTicket;
            try
            {
                authTicket = FormsAuthentication.Decrypt(authCookie.Value);
            }
            catch
            {
                return; // Cookie corrupta o inválida
            }

            if (authTicket == null || authTicket.Expired)
                return;

            string[] roles;

            if (!string.IsNullOrEmpty(authTicket.UserData))
            {
                roles = authTicket.UserData.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                // Fallback: cargar roles desde la base de datos
                roles = GetRolesFromDB(authTicket.Name);
            }

            var identity = new GenericIdentity(authTicket.Name);
            var principal = new GenericPrincipal(identity, roles);

            Context.User = principal;
            System.Threading.Thread.CurrentPrincipal = principal;
        }

        private string[] GetRolesFromDB(string username)
        {
            try
            {
                using (var cn = ConexionDAO.CrearConexion())
                {
                    cn.Open();
                    string sql = @"
                        SELECT r.descripcion
                        FROM usuario u
                        INNER JOIN usuario_rol ur ON u.codigousuario::text = ur.codigousuario::text
                        INNER JOIN rol r ON r.codigorol = ur.codigorol
                        WHERE (LOWER(u.nombreusuario) = LOWER(@user) OR LOWER(u.correo) = LOWER(@user))
                          AND ur.activo = true 
                          AND r.activo = true;";

                    return cn.Query<string>(sql, new { user = username }).ToArray();
                }
            }
            catch
            {
                return new string[] { };
            }
        }
    }
}
