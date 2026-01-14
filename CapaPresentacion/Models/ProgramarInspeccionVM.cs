using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace CapaPresentacion.Models
{
    public class ProgramarInspeccionVM
    {
        public int SolicitudId { get; set; }

        public int? TecnicoSeleccionadoId { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public string Observaciones { get; set; }

        public List<SelectListItem> Tecnicos { get; set; } = new List<SelectListItem>();

        // para mostrar datos de la solicitud (si deseas)
        public string NombreOperador { get; set; }
        public string RUC { get; set; }
        public string TipoOperacion { get; set; }
        public string Matricula { get; set; }
        public string Modelo { get; set; }
    }
}
