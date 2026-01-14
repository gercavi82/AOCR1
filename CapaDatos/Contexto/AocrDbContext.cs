using System.Data.Entity;
using CapaModelo;

namespace CapaDatos.Contexto
{
    public class AocrDbContext : DbContext
    {
        public AocrDbContext()
            : base("AOCRConnection") // nombre exacto del Web.config
        {
        }

        public DbSet<SolicitudAOCR> Solicitudes { get; set; }
        public DbSet<Pago> Pagos { get; set; }
    }
}
