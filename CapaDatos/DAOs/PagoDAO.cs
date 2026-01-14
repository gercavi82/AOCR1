using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using CapaModelo;

namespace CapaDatos.DAOs
{
    public class PagoDAO
    {
        private NpgsqlConnection CrearConexion() => ConexionDAO.CrearConexion();

        private static Pago Map(IDataRecord r)
        {
            return new Pago
            {
                CodigoPago = r["codigo_pago"] != DBNull.Value ? Convert.ToInt32(r["codigo_pago"]) : 0,
                CodigoSolicitud = r["codigo_solicitud"] != DBNull.Value ? Convert.ToInt32(r["codigo_solicitud"]) : 0,
                NumeroTransaccion = r["numero_factura"]?.ToString(),
                Monto = r["monto"] != DBNull.Value ? Convert.ToDecimal(r["monto"]) : 0m,
                Moneda = r["moneda"]?.ToString(),
                Concepto = r["concepto"]?.ToString(),
                MetodoPago = r["metodo_pago"]?.ToString(),
                Estado = r["estado"]?.ToString(),
                FechaPago = r["fecha_pago"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["fecha_pago"]) : null,
                FechaValidacion = r["fecha_validacion"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["fecha_validacion"]) : null,
                UsuarioValidacion = r["validado_por"]?.ToString(),
                ObservacionesValidacion = r["observaciones"]?.ToString(),
                RutaComprobante = r["comprobante_ruta"]?.ToString()
            };
        }

        public List<Pago> ObtenerTodos()
        {
            var lista = new List<Pago>();
            const string sql = "SELECT * FROM aocr_tbpago WHERE deleted_at IS NULL ORDER BY fecha_pago DESC;";
            using (var cn = CrearConexion())
            using (var cmd = new NpgsqlCommand(sql, cn))
            {
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        lista.Add(Map(dr));
                }
            }
            return lista;
        }

        public Pago ObtenerPorId(int id)
        {
            const string sql = "SELECT * FROM aocr_tbpago WHERE codigo_pago = @id;";
            using (var cn = CrearConexion())
            using (var cmd = new NpgsqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                        return Map(dr);
                }
            }
            return null;
        }

        public List<Pago> ObtenerPorSolicitud(int codigoSolicitud)
        {
            var lista = new List<Pago>();
            const string sql = "SELECT * FROM aocr_tbpago WHERE codigo_solicitud = @sol AND deleted_at IS NULL;";
            using (var cn = CrearConexion())
            using (var cmd = new NpgsqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@sol", codigoSolicitud);
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        lista.Add(Map(dr));
                }
            }
            return lista;
        }

        public bool Insertar(Pago p)
        {
            const string sql = @"INSERT INTO aocr_tbpago
                (codigo_solicitud, monto, metodo_pago, fecha_pago, numero_transaccion, ruta_comprobante, estado)
                VALUES (@sol, @monto, @met, @fecha, @num, @ruta, @est);";

            using (var cn = CrearConexion())
            using (var cmd = new NpgsqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@sol", p.CodigoSolicitud);
                cmd.Parameters.AddWithValue("@monto", p.Monto);
                cmd.Parameters.AddWithValue("@met", (object)p.MetodoPago ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@fecha", (object)p.FechaPago ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@num", (object)p.NumeroTransaccion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ruta", (object)p.RutaComprobante ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@est", p.Estado ?? "PENDIENTE");
                cn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool Actualizar(Pago p)
        {
            const string sql = @"UPDATE aocr_tbpago 
                SET monto = @m, estado = @e, fecha_validacion = @fv, 
                    validado_por = @uv, observaciones = @ov 
                WHERE codigo_pago = @id;";

            using (var cn = CrearConexion())
            using (var cmd = new NpgsqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@m", p.Monto);
                cmd.Parameters.AddWithValue("@e", p.Estado);
                cmd.Parameters.AddWithValue("@fv", (object)p.FechaValidacion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@uv", (object)p.UsuarioValidacion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ov", (object)p.ObservacionesValidacion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", p.CodigoPago);
                cn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool ExistePorNumeroTransaccion(string numero)
        {
            const string sql = "SELECT COUNT(*) FROM aocr_tbpago WHERE numero_factura = @num;";
            using (var cn = CrearConexion())
            using (var cmd = new NpgsqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@num", (object)numero ?? DBNull.Value);
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public List<Pago> ObtenerPagosValidadosHoy()
        {
            var lista = new List<Pago>();
            const string sql = @"SELECT * FROM aocr_tbpago 
                                 WHERE estado = 'APROBADO' 
                                 AND DATE(fecha_validacion) = CURRENT_DATE;";

            using (var cn = CrearConexion())
            using (var cmd = new NpgsqlCommand(sql, cn))
            {
                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        lista.Add(Map(dr));
                }
            }
            return lista;
        }

        public decimal ObtenerMontoRecaudadoMes(int anio, int mes)
        {
            const string sql = @"SELECT COALESCE(SUM(monto), 0) 
                                 FROM aocr_tbpago 
                                 WHERE EXTRACT(YEAR FROM fecha_pago) = @a 
                                 AND EXTRACT(MONTH FROM fecha_pago) = @m 
                                 AND estado = 'APROBADO';";

            using (var cn = CrearConexion())
            using (var cmd = new NpgsqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@a", anio);
                cmd.Parameters.AddWithValue("@m", mes);
                cn.Open();
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }
    }
}
