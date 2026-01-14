using System;
using System.Collections.Generic;
using CapaModelo;
using CapaDatos.DAOs;

namespace CapaNegocio
{
    public static class SolicitudAOCRBL
    {
        // 1. Crear Solicitud
        public static int CrearSolicitud(SolicitudAOCR solicitud, out string mensaje)
        {
            try
            {
                solicitud.FechaSolicitud = DateTime.Now;
                solicitud.Estado = "PENDIENTE";
                int id = new SolicitudAOCRDAO().InsertarConReturn(solicitud);
                mensaje = "Solicitud creada exitosamente.";
                return id;
            }
            catch (Exception ex)
            {
                mensaje = "Error al crear solicitud: " + ex.Message;
                return 0;
            }
        }

        // 2. Obtener solicitudes por usuario
        public static List<SolicitudAOCR> ListarPorUsuario(int codigoUsuario)
        {
            return new SolicitudAOCRDAO().ObtenerPorUsuario(codigoUsuario);
        }

        // 3. Obtener por ID
        public static SolicitudAOCR ObtenerPorId(int id)
        {
            return new SolicitudAOCRDAO().ObtenerPorId(id);
        }

        // 4. Actualizar solicitud
        public static bool ActualizarSolicitud(SolicitudAOCR solicitud, out string mensaje)
        {
            try
            {
                bool ok = new SolicitudAOCRDAO().ActualizarGeneral(solicitud);
                mensaje = ok ? "Solicitud actualizada correctamente." : "No se pudo actualizar la solicitud.";
                return ok;
            }
            catch (Exception ex)
            {
                mensaje = "Error al actualizar: " + ex.Message;
                return false;
            }
        }

        // 5. Cambiar estado
        public static bool CambiarEstado(int idSolicitud, string nuevoEstado, int codigoUsuario, string observaciones, out string mensaje)
        {
            try
            {
                bool ok = new SolicitudAOCRDAO().CambiarEstado(idSolicitud, nuevoEstado, codigoUsuario, observaciones);
                mensaje = ok ? "Estado actualizado correctamente." : "No fue posible cambiar el estado.";
                return ok;
            }
            catch (Exception ex)
            {
                mensaje = "Error cambiando estado: " + ex.Message;
                return false;
            }
        }

        public static List<SolicitudAOCR> ListarActivas() => new SolicitudAOCRDAO().ListarActivas();

        public static List<SolicitudAOCR> ListarPorEstado(string estado) => new SolicitudAOCRDAO().ObtenerPorEstado(estado);

        public static List<SolicitudAOCR> ListarPendientesRevision() => new SolicitudAOCRDAO().ObtenerPendientesRevision();

        public static List<SolicitudAOCR> ListarParaValidacionJefatura() => new SolicitudAOCRDAO().ObtenerParaValidacionJefatura();

        // 11. Marcar Para Inspeccion
        public static bool MarcarParaInspeccion(int idSolicitud)
        {
            try
            {
                return new SolicitudAOCRDAO().CambiarEstado(idSolicitud, "INSPECCION_SOLICITADA", 0, "Cambio automático por sistema");
            }
            catch { return false; }
        }

        // 12. Asignar inspectores (CORREGIDO: Delega al DAO)
        public static bool AsignarInspectores(int id, int principal, int? apoyo, DateTime fecha, string obs, out string mensaje)
        {
            // Ya no hay código SQL aquí, se movió al DAO para evitar errores de Npgsql en esta capa
            return new SolicitudAOCRDAO().AsignarInspectores(id, principal, apoyo, fecha, obs, out mensaje);
        }
    }
}