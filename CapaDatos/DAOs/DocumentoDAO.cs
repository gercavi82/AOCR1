using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Npgsql;
using CapaModelo;

namespace CapaDatos.DAOs
{
    public class DocumentoDAO
    {
        private NpgsqlConnection CrearConexion()
        {
            return ConexionDAO.CrearConexion();
        }

        // ============================================================
        // 1. OBTENER TODOS
        // ============================================================
        public List<Documento> ObtenerTodos()
        {
            using (var con = CrearConexion())
            {
                con.Open();
                // Mapeo EXACTO según tu imagen de BD
                const string sql = @"
                    SELECT 
                        codigo_documento AS CodigoDocumento,
                        codigo_solicitud AS CodigoSolicitud,
                        tipo_documento   AS TipoDocumento,
                        nombre_archivo   AS NombreArchivo,
                        ruta_guardada    AS RutaArchivo,    -- OJO: En BD es ruta_guardada
                        tamano_bytes     AS TamanioArchivo, -- OJO: En BD es tamano_bytes
                        estado           AS Estado,
                        observaciones    AS Observaciones,
                        fecha_carga      AS FechaSubida,    -- OJO: En BD es fecha_carga
                        created_by       AS UsuarioRegistro, -- OJO: Asumo created_by
                        extension        AS ExtensionArchivo
                    FROM aocr_tbdocumento
                    ORDER BY fecha_carga DESC;";

                return con.Query<Documento>(sql).ToList();
            }
        }

        // ============================================================
        // 2. OBTENER POR ID
        // ============================================================
        public Documento ObtenerPorId(int id)
        {
            using (var con = CrearConexion())
            {
                con.Open();
                const string sql = @"
                    SELECT 
                        codigo_documento AS CodigoDocumento,
                        codigo_solicitud AS CodigoSolicitud,
                        tipo_documento   AS TipoDocumento,
                        nombre_archivo   AS NombreArchivo,
                        ruta_guardada    AS RutaArchivo,
                        tamano_bytes     AS TamanioArchivo,
                        estado           AS Estado,
                        observaciones    AS Observaciones,
                        fecha_carga      AS FechaSubida,
                        created_by       AS UsuarioRegistro,
                        extension        AS ExtensionArchivo
                    FROM aocr_tbdocumento
                    WHERE codigo_documento = @id;";

                return con.QueryFirstOrDefault<Documento>(sql, new { id });
            }
        }

        // ============================================================
        // 3. OBTENER POR SOLICITUD
        // ============================================================
        public List<Documento> ObtenerPorSolicitud(int solicitudId)
        {
            using (var con = CrearConexion())
            {
                con.Open();
                const string sql = @"
                    SELECT 
                        codigo_documento AS CodigoDocumento,
                        codigo_solicitud AS CodigoSolicitud,
                        tipo_documento   AS TipoDocumento,
                        nombre_archivo   AS NombreArchivo,
                        ruta_guardada    AS RutaArchivo,
                        tamano_bytes     AS TamanioArchivo,
                        estado           AS Estado,
                        observaciones    AS Observaciones,
                        fecha_carga      AS FechaSubida,
                        created_by       AS UsuarioRegistro,
                        extension        AS ExtensionArchivo
                    FROM aocr_tbdocumento
                    WHERE codigo_solicitud = @solicitudId
                    ORDER BY fecha_carga DESC;";

                return con.Query<Documento>(sql, new { solicitudId }).ToList();
            }
        }

        // ============================================================
        // 4. CREAR (INSERTAR)
        // ============================================================
        public int Crear(Documento d)
        {
            using (var con = CrearConexion())
            {
                con.Open();

                if (d.FechaSubida == null) d.FechaSubida = DateTime.Now;

                // Aseguramos que la extensión no sea nula si viene del modelo
                string ext = d.ExtensionArchivo ?? "";
                if (string.IsNullOrEmpty(ext) && !string.IsNullOrEmpty(d.NombreArchivo))
                {
                    ext = System.IO.Path.GetExtension(d.NombreArchivo);
                }

                const string sql = @"
                    INSERT INTO aocr_tbdocumento
                    (
                        codigo_solicitud, 
                        tipo_documento, 
                        nombre_archivo, 
                        ruta_guardada,      -- BD: ruta_guardada
                        tamano_bytes,       -- BD: tamano_bytes
                        estado, 
                        observaciones, 
                        fecha_carga,        -- BD: fecha_carga
                        created_by,         -- BD: created_by
                        extension           -- BD: extension
                    )
                    VALUES
                    (
                        @CodigoSolicitud, 
                        @TipoDocumento, 
                        @NombreArchivo, 
                        @RutaArchivo,       -- Viene del Modelo C#
                        @TamanioArchivo,    -- Viene del Modelo C#
                        @Estado, 
                        @Observaciones, 
                        @FechaSubida,       -- Viene del Modelo C#
                        @UsuarioRegistro,   -- Viene del Modelo C#
                        @ext                -- Variable local calculada arriba
                    )
                    RETURNING codigo_documento;";

                // Pasamos un objeto anónimo para incluir la variable 'ext' extra
                return con.ExecuteScalar<int>(sql, new
                {
                    d.CodigoSolicitud,
                    d.TipoDocumento,
                    d.NombreArchivo,
                    d.RutaArchivo,
                    d.TamanioArchivo,
                    d.Estado,
                    d.Observaciones,
                    d.FechaSubida,
                    d.UsuarioRegistro,
                    ext
                });
            }
        }

        // Wrapper bool para compatibilidad con BL/Controller
        public bool Insertar(Documento d)
        {
            try { return Crear(d) > 0; }
            catch (Exception) { return false; }
        }

        // ============================================================
        // 5. ACTUALIZAR
        // ============================================================
        public bool Actualizar(Documento d)
        {
            using (var con = CrearConexion())
            {
                con.Open();
                const string sql = @"
                    UPDATE aocr_tbdocumento SET
                        tipo_documento = @TipoDocumento,
                        nombre_archivo = @NombreArchivo,
                        ruta_guardada  = @RutaArchivo,    -- BD: ruta_guardada
                        tamano_bytes   = @TamanioArchivo, -- BD: tamano_bytes
                        estado         = @Estado,
                        observaciones  = @Observaciones
                    WHERE codigo_documento = @CodigoDocumento;";

                return con.Execute(sql, d) > 0;
            }
        }

        // ============================================================
        // 6. ELIMINAR
        // ============================================================
        public bool Eliminar(int id)
        {
            using (var con = CrearConexion())
            {
                con.Open();
                const string sql = @"DELETE FROM aocr_tbdocumento WHERE codigo_documento = @id;";
                return con.Execute(sql, new { id }) > 0;
            }
        }
    }
}