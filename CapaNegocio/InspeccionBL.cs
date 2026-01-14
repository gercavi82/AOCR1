using System;
using System.Collections.Generic;
using CapaDatos.DAOs;
using CapaModelo;

namespace CapaNegocio
{
    public static class InspeccionBL
    {
        public static Inspeccion ObtenerPorId(int id)
        {
            if (id <= 0) throw new ArgumentException("ID inválido.");
            return InspeccionDAO.ObtenerPorId(id);
        }

        public static List<Inspeccion> ListarTodas()
        {
            return InspeccionDAO.ListarTodas();
        }

        public static List<Inspeccion> ListarPorInspector(int codigoInspector)
        {
            if (codigoInspector <= 0) return new List<Inspeccion>();
            return InspeccionDAO.ListarPorInspector(codigoInspector);
        }

        // ✅✅✅ FALTABA ESTE MÉTODO
        public static bool Crear(Inspeccion model, int codigoUsuario)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (model.CodigoSolicitud <= 0) throw new Exception("Código de solicitud inválido.");

            model.CreatedBy = codigoUsuario;
            model.UpdatedBy = codigoUsuario;
            if (string.IsNullOrWhiteSpace(model.Estado))
                model.Estado = "CREADA";

            int id = InspeccionDAO.Crear(model);
            model.CodigoInspeccion = id;

            return id > 0;
        }

        public static bool Actualizar(Inspeccion model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (model.CodigoInspeccion <= 0) throw new Exception("Código de inspección inválido.");

            return InspeccionDAO.Actualizar(model);
        }

        public static bool CambiarEstado(int id, string estado, int updatedBy)
        {
            if (id <= 0) throw new Exception("ID inválido.");
            if (string.IsNullOrWhiteSpace(estado)) throw new Exception("Estado requerido.");

            return InspeccionDAO.CambiarEstado(id, estado, updatedBy);
        }

        public static bool GuardarInforme(int id, string rutaInforme, int updatedBy)
        {
            if (id <= 0) throw new Exception("ID inválido.");
            if (string.IsNullOrWhiteSpace(rutaInforme)) throw new Exception("Ruta de informe requerida.");

            return InspeccionDAO.GuardarInforme(id, rutaInforme, updatedBy);
        }

        public static bool CerrarInspeccion(int id, string resultado, int updatedBy)
        {
            if (id <= 0) throw new Exception("ID inválido.");
            if (string.IsNullOrWhiteSpace(resultado)) throw new Exception("Resultado requerido.");

            return InspeccionDAO.Cerrar(id, resultado, updatedBy);
        }
    }
}
