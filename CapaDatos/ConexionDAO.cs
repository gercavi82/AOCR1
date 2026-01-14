using System.Configuration;
using System.Data;
using Npgsql;

namespace CapaDatos.DAOs
{
    public class ConexionDAO
    {
        private static readonly string _cs =
            ConfigurationManager.ConnectionStrings["AOCRConnection"].ConnectionString;

        // ✅ Agregado: para compatibilidad con InspeccionDAO
        public static string CadenaConexion => _cs;

        // =========================
        // NUEVO ESTILO (Dapper)
        // =========================
        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_cs);
        }

        public static NpgsqlConnection CrearConexion()
        {
            return new NpgsqlConnection(_cs);
        }

        public static string ObtenerCadenaConexion()
        {
            return _cs;
        }

        // =========================
        // ESTILO LEGADO (ADO.NET)
        // =========================
        public static NpgsqlConnection ObtenerConexion()
        {
            var con = new NpgsqlConnection(_cs);
            con.Open();
            return con;
        }

        public static void CerrarConexion(NpgsqlConnection con)
        {
            if (con == null) return;

            try
            {
                if (con.State != ConnectionState.Closed)
                    con.Close();
            }
            finally
            {
                con.Dispose();
            }
        }
    }
}
