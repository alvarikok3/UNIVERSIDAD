using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace KN_WEB.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Horario()
        {
            return View();
        }

        public ActionResult Inicio()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Inicio(string email, string password)
        {
            string conexion = ConfigurationManager.ConnectionStrings["KN_WEB_DB"].ConnectionString;

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

                string query = @"SELECT nombre, email
                                 FROM Usuario
                                 WHERE email = @email 
                                   AND password_hash = @password_hash 
                                   AND activo = 1";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@password_hash", password);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    Session["UsuarioNombre"] = reader["nombre"].ToString();
                    Session["UsuarioEmail"] = reader["email"].ToString();
                    return RedirectToAction("Index");
                }

                reader.Close();
            }

            ViewBag.Mensaje = "Correo o contraseña incorrectos.";
            return View();
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string nombre, string apellido, string email, string password, string confirmarPassword)
        {
            if (password != confirmarPassword)
            {
                ViewBag.Mensaje = "Las contraseñas no coinciden.";
                return View();
            }

            string nombreCompleto = nombre + " " + apellido;
            string conexion = ConfigurationManager.ConnectionStrings["KN_WEB_DB"].ConnectionString;

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

                string consultaCorreo = "SELECT COUNT(*) FROM Usuario WHERE email = @email";
                SqlCommand cmdValidar = new SqlCommand(consultaCorreo, con);
                cmdValidar.Parameters.AddWithValue("@email", email);

                int existe = (int)cmdValidar.ExecuteScalar();

                if (existe > 0)
                {
                    ViewBag.Mensaje = "Ese correo ya está registrado.";
                    return View();
                }

                string query = @"INSERT INTO Usuario (nombre, email, password_hash, activo)
                                 VALUES (@nombre, @email, @password_hash, @activo)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@nombre", nombreCompleto);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@password_hash", password);
                cmd.Parameters.AddWithValue("@activo", true);

                cmd.ExecuteNonQuery();
            }

            ViewBag.Mensaje = "Usuario registrado correctamente en la base de datos.";
            return View();
        }

        public ActionResult Informacion()
        {
            return View();
        }

        public ActionResult Reservas()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Reservas(string origen, string destino, string fechaViaje, int cantidadPasajeros = 1)
        {
            string conexion = ConfigurationManager.ConnectionStrings["KN_WEB_DB"].ConnectionString;
            DataTable tablaResultados = new DataTable();

            using (SqlConnection con = new SqlConnection(conexion))
            {
                string query = @"
                    SELECT 
                        r.origen,
                        r.destino,
                        h.hora_salida,
                        h.hora_llegada,
                        h.dias_servicio,
                        b.capacidad,
                        b.placa
                    FROM Ruta r
                    INNER JOIN Horario h ON r.id_ruta = h.id_ruta
                    INNER JOIN Bus b ON h.id_bus = b.id_bus
                    WHERE r.activa = 1
                      AND UPPER(LTRIM(RTRIM(r.origen))) LIKE '%' + UPPER(LTRIM(RTRIM(@origen))) + '%'
                      AND UPPER(LTRIM(RTRIM(r.destino))) LIKE '%' + UPPER(LTRIM(RTRIM(@destino))) + '%'";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@origen", origen ?? "");
                da.SelectCommand.Parameters.AddWithValue("@destino", destino ?? "");
                da.Fill(tablaResultados);
            }

            ViewBag.FechaViaje = fechaViaje;
            ViewBag.CantidadPasajeros = cantidadPasajeros;
            ViewBag.Origen = origen;
            ViewBag.Destino = destino;
            ViewBag.Resultados = tablaResultados;

            return View();
        }

        public ActionResult CerrarSesion()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Inicio");
        }
    }
}