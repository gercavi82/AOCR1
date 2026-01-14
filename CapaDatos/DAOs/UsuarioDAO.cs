using System;
using System.Collections.Generic;
using Npgsql;
using CapaModelo;
using Dapper;
using System.Linq;

namespace CapaDatos.DAOs
{
    public static class UsuarioDAO
    {
        // ==========================================
        // CONFIGURACIÓN DE CONEXIÓN
        // ==========================================
        private static readonly string Host = "172.20.16.55";
        private static readonly int Puerto = 5432;
        private static readonly string BaseDatos = "dgac_des";
        private static readonly string UsuarioDB = "root";
        private static readonly string Clave = "control";

        private static string GetConnectionString() =>
            $"Host={Host};Port={Puerto};Database={BaseDatos};Username={UsuarioDB};Password={Clave};";

        // ==========================================
        // AUTENTICACIÓN: OBTENER POR LOGIN
        // ==========================================
        public static Usuario ObtenerPorNombreUsuario(string loginInput)
        {
            using (var conn = new NpgsqlConnection(GetConnectionString()))
            {
                // Mapeamos las columnas de la DB a las propiedades de tu clase Usuario
                string sql = @"SELECT idusuario AS Id, 
                                      codigousuario AS NombreUsuario, 
                                      correo AS Email, 
                                      clave AS Contrasena, 
                                      nombreusuario AS NombreCompleto, 
                                      rol AS Rol,
                                      (estadoactividad = '1') AS Activo
                               FROM usuario 
                               WHERE (codigousuario = @p1 OR correo = @p1) LIMIT 1";

                return conn.QueryFirstOrDefault<Usuario>(sql, new { p1 = loginInput });
            }
        }

        // ==========================================
        // OBTENER ROLES (Sincronizado con BL)
        // ==========================================
        /// <summary>
        /// Obtiene la lista de roles activos para un usuario dado su ID numérico.
        /// </summary>
        public static List<string> ObtenerRoles(int idUsuario)
        {
            using (var conn = new NpgsqlConnection(GetConnectionString()))
            {
                // JOIN: Conectamos el ID numérico (usuario) con el código de texto (usuario_rol)
                string sql = @"
                    SELECT r.descripcion
                    FROM usuario u
                    INNER JOIN usuario_rol ur ON u.codigousuario = ur.codigousuario
                    INNER JOIN rol r ON r.codigorol = ur.codigorol
                    WHERE u.idusuario = @id
                      AND ur.activo = true
                      AND r.activo = true;";

                var roles = conn.Query<string>(sql, new { id = idUsuario }).AsList();

                // Si no hay roles en la tabla intermedia, devolvemos el rol básico de la tabla usuario (fallback)
                if (roles.Count == 0)
                {
                    string sqlFallback = "SELECT rol FROM usuario WHERE idusuario = @id";
                    var rolBasico = conn.QueryFirstOrDefault<string>(sqlFallback, new { id = idUsuario });
                    if (!string.IsNullOrEmpty(rolBasico))
                    {
                        roles.Add(rolBasico);
                    }
                }

                return roles;
            }
        }

        // ==========================================
        // CREAR USUARIO
        // ==========================================
        public static int Crear(Usuario usuario)
        {
            using (var conn = new NpgsqlConnection(GetConnectionString()))
            {
                string sql = @"INSERT INTO usuario (codigousuario, clave, correo, estadoactividad, nombreusuario, rol) 
                               VALUES (@NombreUsuario, @Contrasena, @Email, '1', @NombreCompleto, @Rol) 
                               RETURNING idusuario";

                return conn.ExecuteScalar<int>(sql, usuario);
            }
        }

        // ==========================================
        // RESTABLECER CONTRASEÑA
        // ==========================================
        public static bool RestablecerContrasena(string email, string nuevaClave, out string mensaje)
        {
            using (var conn = new NpgsqlConnection(GetConnectionString()))
            {
                string sql = "UPDATE usuario SET clave = @clave WHERE LOWER(correo) = LOWER(@correo)";
                var rows = conn.Execute(sql, new { clave = nuevaClave, correo = email });

                if (rows > 0)
                {
                    mensaje = "Contraseña restablecida con éxito.";
                    return true;
                }
                mensaje = "El correo no existe.";
                return false;
            }
        }

        // ==========================================
        // ACTUALIZAR ÚLTIMA CONEXIÓN
        // ==========================================
        public static void ActualizarUltimaConexion(int idUsuario)
        {
            using (var conn = new NpgsqlConnection(GetConnectionString()))
            {
                string sql = "UPDATE usuario SET fechaultimaconexion = CURRENT_TIMESTAMP WHERE idusuario = @id";
                conn.Execute(sql, new { id = idUsuario });
            }
        }

        // ==========================================
        // OBTENER TÉCNICOS
        // ==========================================
        public static List<Usuario> ListarPorRol(string rol)
        {
            var lista = new List<Usuario>();
            using (var cn = new NpgsqlConnection(GetConnectionString()))
            {
                cn.Open();
                string sql = @"SELECT * FROM usuario WHERE LOWER(rol) = LOWER(@rol)";
                using (var cmd = new NpgsqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@rol", rol);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            lista.Add(new Usuario
                            {
                                IdUsuario = Convert.ToInt32(rd["idusuario"]),
                                NombreUsuario = rd["nombreusuario"].ToString(),
                                ApellidoUsuario = rd["apellidousuario"].ToString(),
                                Rol = rd["rol"].ToString()
                            });
                        }
                    }
                }
            }
            return lista;
        }


    }
}