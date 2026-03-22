using System;

namespace KN_WEB.Models
{
    public class Usuario
    {
        public int id_usuario { get; set; }
        public string nombre { get; set; }
        public string email { get; set; }
        public string password_hash { get; set; }
        public bool activo { get; set; }
        public DateTime fecha_registro { get; set; }
    }
}