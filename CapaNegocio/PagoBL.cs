using System;
using System.Collections.Generic;
using CapaDatos.DAOs;
using CapaModelo;

namespace CapaNegocio
{
    public class PagoBL
    {
        private readonly PagoDAO _pagoDAO;

        public PagoBL()
        {
            _pagoDAO = new PagoDAO();
        }

        public List<Pago> ObtenerTodos()
        {
            return _pagoDAO.ObtenerTodos();
        }

        public List<Pago> ObtenerPorSolicitud(int codigoSolicitud)
        {
            return _pagoDAO.ObtenerPorSolicitud(codigoSolicitud);
        }

        public Pago ObtenerPorId(int codigoPago)
        {
            return _pagoDAO.ObtenerPorId(codigoPago);
        }

        public bool Registrar(Pago pago)
        {
            if (pago == null) return false;

            if (pago.FechaPago == DateTime.MinValue)
                pago.FechaPago = DateTime.Now;

            if (string.IsNullOrWhiteSpace(pago.Estado))
                pago.Estado = "PENDIENTE";

            return _pagoDAO.Insertar(pago);
        }

        public bool Actualizar(Pago pago)
        {
            if (pago == null || pago.CodigoPago <= 0) return false;
            return _pagoDAO.Actualizar(pago);
        }

        // =====================================================
        // ✅ CORREGIDO: Método de validación con campos correctos
        // =====================================================
        public bool ProcesarPago(int idPago, string nuevoEstado, string observaciones, string usuarioResponsable)
        {
            try
            {
                var pago = _pagoDAO.ObtenerPorId(idPago);
                if (pago == null) return false;

                if (pago.Estado == "APROBADO" || pago.Estado == "RECHAZADO")
                {
                    return false;
                }

                pago.Estado = nuevoEstado;
                pago.ObservacionesValidacion = observaciones;
                pago.UsuarioValidacion = usuarioResponsable;
                pago.FechaValidacion = DateTime.Now;

                return _pagoDAO.Actualizar(pago);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ExistePorNumeroTransaccion(string numeroTransaccion)
        {
            return _pagoDAO.ExistePorNumeroTransaccion(numeroTransaccion);
        }

        public List<Pago> ObtenerPagosValidadosHoy()
        {
            return _pagoDAO.ObtenerPagosValidadosHoy();
        }

        public decimal ObtenerMontoRecaudadoMes(int año, int mes)
        {
            return _pagoDAO.ObtenerMontoRecaudadoMes(año, mes);
        }
    }
}
