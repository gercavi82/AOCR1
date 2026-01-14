using System;
using System.Collections.Generic;
using System.Reflection;
using CapaDatos.DAOs;
using CapaModelo;

namespace CapaNegocio
{
    public class HallazgoBL
    {
        private readonly HallazgoDAO _hallazgoDAO;

        public HallazgoBL()
        {
            _hallazgoDAO = new HallazgoDAO();
        }

        // ============================================================
        // LISTAR POR INSPECCIÓN
        // ============================================================
        public List<Hallazgo> ObtenerPorInspeccion(int idInspeccion)
        {
            if (idInspeccion <= 0)
                throw new ArgumentException("ID de inspección inválido");

            return _hallazgoDAO.ObtenerPorInspeccion(idInspeccion);
        }

        // ============================================================
        // OBTENER POR ID
        // ============================================================
        public Hallazgo ObtenerPorId(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID inválido");

            return _hallazgoDAO.ObtenerPorId(id);
        }

        // ============================================================
        // CREAR HALLAZGO
        // ============================================================
        public bool Crear(Hallazgo h, string usuario)
        {
            ValidarHallazgo(h);

            // ✅ Verificar que la inspección exista (InspeccionDAO es static)
            var inspeccion = InspeccionDAO.ObtenerPorId(h.CodigoInspeccion);
            if (inspeccion == null)
                throw new Exception("La inspección asociada no existe.");

            // ✅ Normalizar severidad/criticidad si aplica
            NormalizarSeveridadOCriticidad(h);

            h.FechaDeteccion = DateTime.Now;
            h.CreatedAt = DateTime.Now;
            h.CreatedBy = string.IsNullOrWhiteSpace(usuario) ? "SYSTEM" : usuario.Trim();
            h.Estado = "ABIERTO";

            return _hallazgoDAO.Crear(h) > 0;
        }

        // ============================================================
        // ACTUALIZAR HALLAZGO
        // ============================================================
        public bool Actualizar(Hallazgo h, string usuario)
        {
            if (h == null || h.CodigoHallazgo <= 0)
                throw new Exception("ID inválido para actualizar.");

            ValidarHallazgo(h);

            // ✅ Verificar que la inspección exista (InspeccionDAO es static)
            var inspeccion = InspeccionDAO.ObtenerPorId(h.CodigoInspeccion);
            if (inspeccion == null)
                throw new Exception("La inspección asociada no existe.");

            // ✅ Normalizar severidad/criticidad si aplica
            NormalizarSeveridadOCriticidad(h);

            h.UpdatedAt = DateTime.Now;
            h.UpdatedBy = string.IsNullOrWhiteSpace(usuario) ? "SYSTEM" : usuario.Trim();

            return _hallazgoDAO.Actualizar(h) > 0;
        }

        // ============================================================
        // CERRAR HALLAZGO
        // ============================================================
        public bool CerrarHallazgo(int idHallazgo, string usuario)
        {
            var h = _hallazgoDAO.ObtenerPorId(idHallazgo);

            if (h == null)
                throw new Exception("Hallazgo no encontrado");

            if (string.Equals(h.Estado, "CERRADO", StringComparison.OrdinalIgnoreCase))
                throw new Exception("El hallazgo ya está cerrado");

            h.Estado = "CERRADO";
            h.FechaCierre = DateTime.Now;
            h.UpdatedAt = DateTime.Now;
            h.UpdatedBy = string.IsNullOrWhiteSpace(usuario) ? "SYSTEM" : usuario.Trim();

            return _hallazgoDAO.Cerrar(h) > 0;
        }

        // ============================================================
        // ELIMINAR (SOFT DELETE)
        // ============================================================
        public bool Eliminar(int idHallazgo, string usuario)
        {
            var h = _hallazgoDAO.ObtenerPorId(idHallazgo);

            if (h == null)
                throw new Exception("Hallazgo no encontrado");

            return _hallazgoDAO.Eliminar(idHallazgo, string.IsNullOrWhiteSpace(usuario) ? "SYSTEM" : usuario.Trim()) > 0;
        }

        // ============================================================
        // VALIDACIONES
        // ============================================================
        private void ValidarHallazgo(Hallazgo h)
        {
            if (h == null)
                throw new Exception("Datos inválidos.");

            if (h.CodigoInspeccion <= 0)
                throw new Exception("Debe asignarse a una inspección.");

            if (string.IsNullOrWhiteSpace(h.Descripcion))
                throw new Exception("Debe ingresar una descripción del hallazgo.");

            // ✅ Soporta Criticidad o Severidad (según exista en tu modelo)
            string sev = GetStringProp(h, "Criticidad");
            if (string.IsNullOrWhiteSpace(sev))
                sev = GetStringProp(h, "Severidad");

            if (string.IsNullOrWhiteSpace(sev))
                throw new Exception("Debe especificar criticidad/severidad (ALTA / MEDIA / BAJA).");

            sev = sev.Trim().ToUpperInvariant();
            if (sev != "ALTA" && sev != "MEDIA" && sev != "BAJA")
                throw new Exception("Criticidad/Severidad inválida. Use: ALTA / MEDIA / BAJA.");
        }

        // ============================================================
        // NORMALIZACIÓN Criticidad/Severidad (evita datos sucios)
        // ============================================================
        private void NormalizarSeveridadOCriticidad(Hallazgo h)
        {
            // Toma el valor que exista
            string val = GetStringProp(h, "Criticidad");
            bool usaCriticidad = !string.IsNullOrWhiteSpace(val);

            if (!usaCriticidad)
                val = GetStringProp(h, "Severidad");

            if (string.IsNullOrWhiteSpace(val))
                return;

            val = val.Trim().ToUpperInvariant();
            if (val == "ALTO") val = "ALTA"; // tolerancia
            if (val == "MEDIO") val = "MEDIA"; // tolerancia
            if (val == "BAJO") val = "BAJA"; // tolerancia

            // Escribe en la propiedad que exista
            if (TieneProp(h, "Criticidad"))
                SetStringProp(h, "Criticidad", val);

            if (TieneProp(h, "Severidad"))
                SetStringProp(h, "Severidad", val);
        }

        // ============================================================
        // HELPERS REFLECTION (para no depender del nombre exacto)
        // ============================================================
        private bool TieneProp(object obj, string propName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propName)) return false;
            var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return p != null;
        }

        private string GetStringProp(object obj, string propName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propName)) return null;
            var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p == null) return null;
            var v = p.GetValue(obj, null);
            return v?.ToString();
        }

        private void SetStringProp(object obj, string propName, string value)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propName)) return;
            var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p == null) return;
            if (!p.CanWrite) return;

            p.SetValue(obj, value, null);
        }
    }
}
