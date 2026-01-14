using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Npgsql;
using CapaModelo;

namespace CapaDatos.DAOs
{
    public class SolicitudAOCRDAO
    {
        private string ConnectionString =>
            ConfigurationManager.ConnectionStrings["AOCRConnection"].ConnectionString;

        // ============================
        // LISTADOS
        // ============================
        public List<SolicitudAOCR> ListarActivas()
        {
            return ObtenerPorFiltro("deleted_at IS NULL");
        }

        public List<SolicitudAOCR> ObtenerTodos()
        {
            return ObtenerPorFiltro("1=1");
        }

        public List<SolicitudAOCR> ObtenerPorUsuario(int codigoUsuario)
        {
            return ObtenerPorFiltro(
                "codigo_usuario = @u",
                cmd => cmd.Parameters.AddWithValue("@u", codigoUsuario)
            );
        }

        public List<SolicitudAOCR> ObtenerPorEstado(string estado)
        {
            return ObtenerPorFiltro(
                "estado = @e",
                cmd => cmd.Parameters.AddWithValue("@e", estado)
            );
        }

        public List<SolicitudAOCR> ObtenerPendientesRevision()
        {
            return ObtenerPorEstado("ENVIADO_A_INSPECTOR");
        }

        public List<SolicitudAOCR> ObtenerParaValidacionJefatura()
        {
            return ObtenerPorFiltro(
                "estado = @e AND deleted_at IS NULL",
                cmd => cmd.Parameters.AddWithValue("@e", "ENVIADO_A_JEFATURA")
            );
        }

        // ============================
        // OBTENER INDIVIDUAL
        // ============================
        public SolicitudAOCR ObtenerPorId(int id)
        {
            using (var cn = new NpgsqlConnection(ConnectionString))
            {
                cn.Open();
                string sql = @"SELECT * FROM aocr_tbsolicitud
                               WHERE codigo_solicitud = @id AND deleted_at IS NULL";

                using (var cmd = new NpgsqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var rd = cmd.ExecuteReader())
                    {
                        return rd.Read() ? Mapear(rd) : null;
                    }
                }
            }
        }

        // ✅ COMPATIBILIDAD: tu Controller llama ObtenerPorCodigo
        public SolicitudAOCR ObtenerPorCodigo(int codigo)
        {
            return ObtenerPorId(codigo);
        }

        // ============================
        // INSERTAR
        // ============================
        public int InsertarConReturn(SolicitudAOCR s)
        {
            using (var cn = new NpgsqlConnection(ConnectionString))
            {
                cn.Open();
                string sql = @"
                    INSERT INTO aocr_tbsolicitud
                    (nombre_operador, fecha_solicitud, estado, codigo_usuario)
                    VALUES (@n, @f, @e, @u)
                    RETURNING codigo_solicitud";

                using (var cmd = new NpgsqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@n", s.NombreOperador);
                    cmd.Parameters.AddWithValue("@f", s.FechaSolicitud);
                    cmd.Parameters.AddWithValue("@e", s.Estado);
                    cmd.Parameters.AddWithValue("@u", s.CodigoUsuario);

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        // ============================
        // ACTUALIZAR
        // ============================
        public bool ActualizarGeneral(SolicitudAOCR s)
        {
            using (var cn = new NpgsqlConnection(ConnectionString))
            {
                cn.Open();
                string sql = @"
                    UPDATE aocr_tbsolicitud
                    SET nombre_operador = @n,
                        updated_at = NOW(),
                        updated_by = @u
                    WHERE codigo_solicitud = @id";

                using (var cmd = new NpgsqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@n", s.NombreOperador ?? "");
                    cmd.Parameters.AddWithValue("@u", (s.UpdatedBy ?? "0"));
                    cmd.Parameters.AddWithValue("@id", s.CodigoSolicitud);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ✅ COMPATIBILIDAD: tu Controller llama Actualizar(...)
        public bool Actualizar(SolicitudAOCR s)
        {
            return ActualizarGeneral(s);
        }

        public bool CambiarEstado(int id, string estado, int usuario, string obs = "")
        {
            using (var cn = new NpgsqlConnection(ConnectionString))
            {
                cn.Open();
                string sql = @"
                    UPDATE aocr_tbsolicitud
                    SET estado = @e,
                        observaciones = @o,
                        updated_at = NOW(),
                        updated_by = @u
                    WHERE codigo_solicitud = @id";

                using (var cmd = new NpgsqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@e", estado ?? "");
                    cmd.Parameters.AddWithValue("@o", obs ?? "");
                    cmd.Parameters.AddWithValue("@u", usuario.ToString());
                    cmd.Parameters.AddWithValue("@id", id);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // ============================
        // ASIGNACIÓN DE INSPECTORES (COMPLETO)
        // ============================
        public bool AsignarInspectores(int id, int principal, int? apoyo, DateTime fecha, string obs, out string mensaje)
        {
            try
            {
                using (var cn = new NpgsqlConnection(ConnectionString))
                {
                    const string sql = @"UPDATE aocr_tbsolicitud 
                                         SET inspector_principal_id = @p, 
                                             inspector_apoyo_id = @a, 
                                             fecha_inspeccion = @f, 
                                             observaciones_tecnicas = @o,
                                             estado = 'INSPECCION_ASIGNADA'
                                         WHERE codigo_solicitud = @id";

                    using (var cmd = new NpgsqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@p", principal);
                        cmd.Parameters.AddWithValue("@a", (object)apoyo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@f", fecha);
                        cmd.Parameters.AddWithValue("@o", obs ?? "");
                        cmd.Parameters.AddWithValue("@id", id);

                        cn.Open();
                        bool ok = cmd.ExecuteNonQuery() > 0;
                        mensaje = ok ? "Asignación realizada con éxito." : "No se encontró la solicitud.";
                        return ok;
                    }
                }
            }
            catch (Exception ex)
            {
                mensaje = "Error en base de datos: " + ex.Message;
                return false;
            }
        }

        // ============================
        // ACTUALIZAR TÉCNICO
        // ============================
        public bool ActualizarTecnico(int solicitudId, int tecnicoId, int usuarioId)
        {
            try
            {
                using (var cn = new NpgsqlConnection(ConnectionString))
                {
                    const string sql = @"UPDATE aocr_tbsolicitud 
                                         SET codigo_tecnico = @t, 
                                             updated_at = NOW(), 
                                             updated_by = @u 
                                         WHERE codigo_solicitud = @id";

                    using (var cmd = new NpgsqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@t", tecnicoId);
                        cmd.Parameters.AddWithValue("@u", usuarioId.ToString());
                        cmd.Parameters.AddWithValue("@id", solicitudId);

                        cn.Open();
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al actualizar el técnico en la base de datos: " + ex.Message);
            }
        }

        // ============================
        // MÉTODOS INTERNOS
        // ============================
        private List<SolicitudAOCR> ObtenerPorFiltro(string where, Action<NpgsqlCommand> parametros = null)
        {
            var lista = new List<SolicitudAOCR>();
            using (var cn = new NpgsqlConnection(ConnectionString))
            {
                cn.Open();
                string sql = $@"SELECT * FROM aocr_tbsolicitud WHERE {where} ORDER BY fecha_solicitud DESC";
                using (var cmd = new NpgsqlCommand(sql, cn))
                {
                    parametros?.Invoke(cmd);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            lista.Add(Mapear(rd));
                        }
                    }
                }
            }
            return lista;
        }

        private SolicitudAOCR Mapear(IDataRecord rd)
        {
            return new SolicitudAOCR
            {
                CodigoSolicitud = Convert.ToInt32(rd["codigo_solicitud"]),
                NombreOperador = rd["nombre_operador"]?.ToString(),
                Estado = rd["estado"]?.ToString(),
                FechaSolicitud = Convert.ToDateTime(rd["fecha_solicitud"]),
                CodigoUsuario = Convert.ToInt32(rd["codigo_usuario"])
            };
        }
    }
}
