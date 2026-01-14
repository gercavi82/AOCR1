using System.Collections.Generic;
using System.Web.Mvc;

namespace CapaPresentacion.Models
{
    public class ResultadoInspeccionVM
    {
        public int IdInspeccion { get; set; }
        public int SolicitudId { get; set; }

        public string Resultado { get; set; } // APROBADO/OBSERVADO/RECHAZADO
        public string Observaciones { get; set; }

        public List<SelectListItem> Resultados { get; set; } = new List<SelectListItem>();
    }
}
