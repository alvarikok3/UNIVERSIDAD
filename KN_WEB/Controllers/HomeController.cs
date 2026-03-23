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

        public ActionResult ConfirmarReserva(string origen, string destino, string salida, string llegada, string dias, string bus, int capacidad, string fechaViaje, int cantidadPasajeros = 1)
        {
            ViewBag.Origen = origen;
            ViewBag.Destino = destino;
            ViewBag.Salida = salida;
            ViewBag.Llegada = llegada;
            ViewBag.Dias = dias;
            ViewBag.Bus = bus;
            ViewBag.Capacidad = capacidad;
            ViewBag.FechaViaje = fechaViaje;
            ViewBag.CantidadPasajeros = cantidadPasajeros;

            return View();
        }

        [HttpPost]
        public ActionResult ConfirmarReserva(string origen, string destino, string salida, string llegada, string fechaViaje, int cantidadPasajeros)
        {
            string conexion = ConfigurationManager.ConnectionStrings["KN_WEB_DB"].ConnectionString;
            int? idUsuario = null;
            int idReserva = 0;

            if (Session["UsuarioEmail"] != null)
            {
                using (SqlConnection con = new SqlConnection(conexion))
                {
                    con.Open();

                    string queryUsuario = "SELECT id_usuario FROM Usuario WHERE email = @email";
                    SqlCommand cmdUsuario = new SqlCommand(queryUsuario, con);
                    cmdUsuario.Parameters.AddWithValue("@email", Session["UsuarioEmail"].ToString());

                    object resultado = cmdUsuario.ExecuteScalar();

                    if (resultado != null)
                    {
                        idUsuario = Convert.ToInt32(resultado);
                    }
                }
            }

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

                string query = @"INSERT INTO Reserva
                                 (origen, destino, fecha_viaje, hora_salida, hora_llegada, cantidad_pasajeros, id_usuario)
                                 VALUES
                                 (@origen, @destino, @fecha_viaje, @hora_salida, @hora_llegada, @cantidad_pasajeros, @id_usuario);
                                 SELECT SCOPE_IDENTITY();";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@origen", origen);
                cmd.Parameters.AddWithValue("@destino", destino);
                cmd.Parameters.AddWithValue("@fecha_viaje", Convert.ToDateTime(fechaViaje));
                cmd.Parameters.AddWithValue("@hora_salida", TimeSpan.Parse(salida));
                cmd.Parameters.AddWithValue("@hora_llegada", TimeSpan.Parse(llegada));
                cmd.Parameters.AddWithValue("@cantidad_pasajeros", cantidadPasajeros);

                if (idUsuario.HasValue)
                    cmd.Parameters.AddWithValue("@id_usuario", idUsuario.Value);
                else
                    cmd.Parameters.AddWithValue("@id_usuario", DBNull.Value);

                object resultado = cmd.ExecuteScalar();
                idReserva = Convert.ToInt32(resultado);
            }

            return RedirectToAction("ElegirAsiento", new { idReserva = idReserva });
        }

        public ActionResult ElegirAsiento(int idReserva)
        {
            string conexion = ConfigurationManager.ConnectionStrings["KN_WEB_DB"].ConnectionString;
            DataTable asientosOcupados = new DataTable();

            using (SqlConnection con = new SqlConnection(conexion))
            {
                string query = "SELECT numero_asiento FROM AsientoReserva";
                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.Fill(asientosOcupados);
            }

            ViewBag.IdReserva = idReserva;
            ViewBag.AsientosOcupados = asientosOcupados;
            return View();
        }

        [HttpPost]
        public ActionResult GuardarAsiento(int idReserva, string numeroAsiento)
        {
            string conexion = ConfigurationManager.ConnectionStrings["KN_WEB_DB"].ConnectionString;

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

                string query = @"INSERT INTO AsientoReserva (id_reserva, numero_asiento)
                                 VALUES (@id_reserva, @numero_asiento)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id_reserva", idReserva);
                cmd.Parameters.AddWithValue("@numero_asiento", numeroAsiento);

                cmd.ExecuteNonQuery();
            }

            TempData["MensajeReserva"] = "Reserva y asiento guardados correctamente.";
            return RedirectToAction("Reservas");
        }

        public ActionResult CerrarSesion()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Inicio");
        }
    }
}