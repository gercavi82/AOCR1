using System;
using System.Collections.Generic;
using Npgsql;
using CapaModelo;

namespace CapaDatos.DAOs
{
    public static class InspeccionDAO
    {
        private static string CS => ConexionDAO.CadenaConexion;

        // ✅ Tabla real
        private const string TABLA = "public.aocr_tbinspeccion";

        public static Inspeccion ObtenerPorId(int id)
        {
            using (var conn = new NpgsqlConnection(CS))
            {
                conn.Open();

                string sql = $@"
                    SELECT
                        codigo_inspeccion,
                        codigo_solicitud,
                        codigo_inspector,
                        fecha_programada,
                        hora_programmada,   -- ⚠️ cambia a hora_programada si aplica
                        lugar,
                        tipo,
                        observaciones_generales,
                        comentarios,
                        hallazgos_principales,
                        estado,
                        resultado,
                        created_at,
                        created_by,
                        updated_at,
                        updated_by
                    FROM {TABLA}
                    WHERE codigo_inspeccion = @id;
                ";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    using (var dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) return null;

                        return new Inspeccion
                        {
                            CodigoInspeccion = dr.GetInt32(0),
                            CodigoSolicitud = dr.GetInt32(1),
                            CodigoInspector = dr.IsDBNull(2) ? (int?)null : dr.GetInt32(2),
                            FechaProgramada = dr.IsDBNull(3) ? (DateTime?)null : dr.GetDateTime(3),
                            HoraProgramada = dr.IsDBNull(4) ? (TimeSpan?)null : dr.GetTimeSpan(4),
                            Lugar = dr.IsDBNull(5) ? null : dr.GetString(5),
                            Tipo = dr.IsDBNull(6) ? null : dr.GetString(6),
                            ObservacionesGenerales = dr.IsDBNull(7) ? null : dr.GetString(7),
                            Comentarios = dr.IsDBNull(8) ? null : dr.GetString(8),
                            HallazgosPrincipales = dr.IsDBNull(9) ? null : dr.GetString(9),
                            Estado = dr.IsDBNull(10) ? null : dr.GetString(10),
                            Resultado = dr.IsDBNull(11) ? null : dr.GetString(11),
                            CreatedAt = dr.IsDBNull(12) ? (DateTime?)null : dr.GetDateTime(12),
                            CreatedBy = dr.IsDBNull(13) ? (int?)null : dr.GetInt32(13),
                            UpdatedAt = dr.IsDBNull(14) ? (DateTime?)null : dr.GetDateTime(14),
                            UpdatedBy = dr.IsDBNull(15) ? (int?)null : dr.GetInt32(15)
                        };
                    }
                }
            }
        }

        public static List<Inspeccion> ListarTodas()
        {
            var lista = new List<Inspeccion>();

            using (var conn = new NpgsqlConnection(CS))
            {
                conn.Open();

                string sql = $@"
                    SELECT
                        codigo_inspeccion,
                        codigo_solicitud,
                        codigo_inspector,
                        fecha_programada,
                        estado,
                        resultado
                    FROM {TABLA}
                    ORDER BY codigo_inspeccion DESC;
                ";

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new Inspeccion
                        {
                            CodigoInspeccion = dr.GetInt32(0),
                            CodigoSolicitud = dr.GetInt32(1),
                            CodigoInspector = dr.IsDBNull(2) ? (int?)null : dr.GetInt32(2),
                            FechaProgramada = dr.IsDBNull(3) ? (DateTime?)null : dr.GetDateTime(3),
                            Estado = dr.IsDBNull(4) ? null : dr.GetString(4),
                            Resultado = dr.IsDBNull(5) ? null : dr.GetString(5)
                        });
                    }
                }
            }

            return lista;
        }

        public static List<Inspeccion> ListarPorInspector(int codigoInspector)
        {
            var lista = new List<Inspeccion>();

            using (var conn = new NpgsqlConnection(CS))
            {
                conn.Open();

                string sql = $@"
                    SELECT
                        codigo_inspeccion,
                        codigo_solicitud,
                        codigo_inspector,
                        fecha_programada,
                        estado,
                        resultado
                    FROM {TABLA}
                    WHERE codigo_inspector = @ci
                    ORDER BY fecha_programada DESC NULLS LAST, codigo_inspeccion DESC;
                ";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ci", codigoInspector);

                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new Inspeccion
                            {
                                CodigoInspeccion = dr.GetInt32(0),
                                CodigoSolicitud = dr.GetInt32(1),
                                CodigoInspector = dr.IsDBNull(2) ? (int?)null : dr.GetInt32(2),
                                FechaProgramada = dr.IsDBNull(3) ? (DateTime?)null : dr.GetDateTime(3),
                                Estado = dr.IsDBNull(4) ? null : dr.GetString(4),
                                Resultado = dr.IsDBNull(5) ? null : dr.GetString(5)
                            });
                        }
                    }
                }
            }

            return lista;
        }

        // CREA y DEVUELVE ID
        public static int Crear(Inspeccion i)
        {
            using (var conn = new NpgsqlConnection(CS))
            {
                conn.Open();

                string sql = $@"
                    INSERT INTO {TABLA}
                        (codigo_solicitud, codigo_inspector, estado, created_at, created_by, updated_at, updated_by)
                    VALUES
                        (@sol, @insp, @estado, NOW(), @by, NOW(), @by)
                    RETURNING codigo_inspeccion;
                ";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@sol", i.CodigoSolicitud);
                    cmd.Parameters.AddWithValue("@insp", (object)i.CodigoInspector ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@estado", string.IsNullOrWhiteSpace(i.Estado) ? "CREADA" : i.Estado);
                    cmd.Parameters.AddWithValue("@by", i.CreatedBy ?? 0);

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public static bool Actualizar(Inspeccion i)
        {
            using (var conn = new NpgsqlConnection(CS))
            {
                conn.Open();

                string sql = $@"
                    UPDATE {TABLA}
                    SET
                        codigo_inspector = @insp,
                        fecha_programada = @fp,
                        hora_programmada = @hp,  -- ⚠️ cambia a hora_programada si aplica
                        lugar = @lug,
                        tipo = @tipo,
                        observaciones_generales = @obs,
                        comentarios = @com,
                        hallazgos_principales = @hall,
                        estado = @estado,
                        updated_at = NOW(),
                        updated_by = @uby
                    WHERE codigo_inspeccion = @id;
                ";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@insp", (object)i.CodigoInspector ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@fp", (object)i.FechaProgramada ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@hp", (object)i.HoraProgramada ?? DBNull.Value);

                    cmd.Parameters.AddWithValue("@lug", (object)i.Lugar ?? "");
                    cmd.Parameters.AddWithValue("@tipo", (object)i.Tipo ?? "");

                    cmd.Parameters.AddWithValue("@obs", (object)i.ObservacionesGenerales ?? "");
                    cmd.Parameters.AddWithValue("@com", (object)i.Comentarios ?? "");
                    cmd.Parameters.AddWithValue("@hall", (object)i.HallazgosPrincipales ?? "");

                    cmd.Parameters.AddWithValue("@estado", (object)i.Estado ?? "CREADA");

                    cmd.Parameters.AddWithValue("@uby", i.UpdatedBy ?? 0);
                    cmd.Parameters.AddWithValue("@id", i.CodigoInspeccion);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public static bool CambiarEstado(int id, string estado, int updatedBy)
        {
            using (var conn = new NpgsqlConnection(CS))
            {
                conn.Open();

                string sql = $@"
                    UPDATE {TABLA}
                    SET estado = @estado,
                        updated_at = NOW(),
                        updated_by = @uby
                    WHERE codigo_inspeccion = @id;
                ";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@estado", estado);
                    cmd.Parameters.AddWithValue("@uby", updatedBy);
                    cmd.Parameters.AddWithValue("@id", id);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public static bool Cerrar(int id, string resultado, int updatedBy)
        {
            using (var conn = new NpgsqlConnection(CS))
            {
                conn.Open();

                string sql = $@"
                    UPDATE {TABLA}
                    SET estado = 'CERRADA',
                        resultado = @res,
                        updated_at = NOW(),
                        updated_by = @uby
                    WHERE codigo_inspeccion = @id;
                ";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@res", (object)resultado ?? "");
                    cmd.Parameters.AddWithValue("@uby", updatedBy);
                    cmd.Parameters.AddWithValue("@id", id);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        public static bool GuardarInforme(int id, string rutaInforme, int updatedBy)
        {
            using (var conn = new NpgsqlConnection(CS))
            {
                conn.Open();

                string sql = $@"
            UPDATE {TABLA}
            SET ruta_informe = @ruta,
                updated_at = NOW(),
                updated_by = @uby
            WHERE codigo_inspeccion = @id;
        ";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ruta", (object)rutaInforme ?? "");
                    cmd.Parameters.AddWithValue("@uby", updatedBy);
                    cmd.Parameters.AddWithValue("@id", id);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

    }
}
