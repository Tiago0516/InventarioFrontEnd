using System.ComponentModel.DataAnnotations;

namespace OrderDashboard.Models
{
    public class Inventario
    {
        public int InventarioId { get; set; }

        [Display(Name = "Nombre del Producto")]
        public string Nombre { get; set; }

        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [Display(Name = "Cantidad Stock")]
        public int Cantidad_Stock { get; set; }

        [Display(Name = "Precio Unitario")]
        public decimal Precio_Unitario { get; set; }
    }
}
