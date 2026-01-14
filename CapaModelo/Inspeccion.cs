using System;

namespace CapaModelo
{
    public class Inspeccion
    {
        public int CodigoInspeccion { get; set; }
        public int CodigoSolicitud { get; set; }

        public int? CodigoInspector { get; set; }

        public DateTime? FechaProgramada { get; set; }
        public TimeSpan? HoraProgramada { get; set; }
        public int? DuracionEstimada { get; set; }

        public string Lugar { get; set; }
        public string Latitud { get; set; }
        public string Longitud { get; set; }

        public string Tipo { get; set; }
        public string ObservacionesGenerales { get; set; }
        public string Comentarios { get; set; }
        public string HallazgosPrincipales { get; set; }

        public string Estado { get; set; }

        // ✅ EXISTE EN BD (campo resultado)
        public string Resultado { get; set; }

        // ✅ EXISTE EN BD (campo rutainforme)
        public string RutaInforme { get; set; }

        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // ✅ Alias de compatibilidad (por si alguna vista/controlador usaba InformePdf)
        public string InformePdf
        {
            get { return RutaInforme; }
            set { RutaInforme = value; }
        }
    }
}
