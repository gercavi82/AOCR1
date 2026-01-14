using System;

namespace CapaModelo
{
    public class Pago
    {
        public int CodigoPago { get; set; }
        public int CodigoSolicitud { get; set; }

        // Número de transacción, mapeado desde "numero_factura" en la base de datos
        public string NumeroTransaccion { get; set; }

        public decimal Monto { get; set; }
        public string Moneda { get; set; }
        public string Concepto { get; set; }
        public string MetodoPago { get; set; }

        // Estado: PENDIENTE, APROBADO, RECHAZADO
        public string Estado { get; set; }

        public DateTime? FechaPago { get; set; }
        public DateTime? FechaValidacion { get; set; }

        public string UsuarioValidacion { get; set; } // validado_por
        public string ObservacionesValidacion { get; set; } // observaciones

        public string RutaComprobante { get; set; }

        // Relación opcional con la solicitud
        public SolicitudAOCR Solicitud { get; set; }
    }
}
