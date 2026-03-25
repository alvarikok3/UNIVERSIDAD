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
                      AND UPPER(r.origen) LIKE '%' + UPPER(@origen) + '%'
                      AND UPPER(r.destino) LIKE '%' + UPPER(@destino) + '%'";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@origen", (origen ?? "").Trim());
                da.SelectCommand.Parameters.AddWithValue("@destino", (destino ?? "").Trim());
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

                    object resultadoUsuario = cmdUsuario.ExecuteScalar();

                    if (resultadoUsuario != null)
                    {
                        idUsuario = Convert.ToInt32(resultadoUsuario);
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

                object resultadoReserva = cmd.ExecuteScalar();
                idReserva = Convert.ToInt32(resultadoReserva);
            }

            return RedirectToAction("ElegirAsiento", new { idReserva = idReserva });
        }

        public ActionResult ElegirAsiento(int idReserva)
        {
            string conexion = ConfigurationManager.ConnectionStrings["KN_WEB_DB"].ConnectionString;
            DataTable asientosOcupados = new DataTable();

            string origen = "";
            string destino = "";
            DateTime fechaViaje = DateTime.Now;

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

                string queryReserva = @"SELECT origen, destino, fecha_viaje
                                        FROM Reserva
                                        WHERE id_reserva = @id_reserva";

                SqlCommand cmdReserva = new SqlCommand(queryReserva, con);
                cmdReserva.Parameters.AddWithValue("@id_reserva", idReserva);

                SqlDataReader reader = cmdReserva.ExecuteReader();

                if (reader.Read())
                {
                    origen = reader["origen"].ToString();
                    destino = reader["destino"].ToString();
                    fechaViaje = Convert.ToDateTime(reader["fecha_viaje"]);
                }

                reader.Close();

                string queryAsientos = @"
                    SELECT ar.numero_asiento
                    FROM AsientoReserva ar
                    INNER JOIN Reserva r ON ar.id_reserva = r.id_reserva
                    WHERE r.origen = @origen
                      AND r.destino = @destino
                      AND r.fecha_viaje = @fecha_viaje
                      AND r.estado = 1";

                SqlDataAdapter da = new SqlDataAdapter(queryAsientos, con);
                da.SelectCommand.Parameters.AddWithValue("@origen", origen);
                da.SelectCommand.Parameters.AddWithValue("@destino", destino);
                da.SelectCommand.Parameters.AddWithValue("@fecha_viaje", fechaViaje.Date);
                da.Fill(asientosOcupados);
            }

            ViewBag.IdReserva = idReserva;
            ViewBag.AsientosOcupados = asientosOcupados;
            ViewBag.Origen = origen;
            ViewBag.Destino = destino;
            ViewBag.FechaViaje = fechaViaje.ToString("yyyy-MM-dd");

            return View();
        }

        [HttpPost]
        public ActionResult GuardarAsiento(int idReserva, string numeroAsiento)
        {
            string conexion = ConfigurationManager.ConnectionStrings["KN_WEB_DB"].ConnectionString;

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

                string origen = "";
                string destino = "";
                DateTime fechaViaje = DateTime.Now;

                string queryReserva = @"SELECT origen, destino, fecha_viaje
                                        FROM Reserva
                                        WHERE id_reserva = @id_reserva";

                SqlCommand cmdReserva = new SqlCommand(queryReserva, con);
                cmdReserva.Parameters.AddWithValue("@id_reserva", idReserva);

                SqlDataReader reader = cmdReserva.ExecuteReader();

                if (reader.Read())
                {
                    origen = reader["origen"].ToString();
                    destino = reader["destino"].ToString();
                    fechaViaje = Convert.ToDateTime(reader["fecha_viaje"]);
                }

                reader.Close();

                string queryValidar = @"
                    SELECT COUNT(*)
                    FROM AsientoReserva ar
                    INNER JOIN Reserva r ON ar.id_reserva = r.id_reserva
                    WHERE ar.numero_asiento = @numero_asiento
                      AND r.origen = @origen
                      AND r.destino = @destino
                      AND r.fecha_viaje = @fecha_viaje
                      AND r.estado = 1";

                SqlCommand cmdValidar = new SqlCommand(queryValidar, con);
                cmdValidar.Parameters.AddWithValue("@numero_asiento", numeroAsiento);
                cmdValidar.Parameters.AddWithValue("@origen", origen);
                cmdValidar.Parameters.AddWithValue("@destino", destino);
                cmdValidar.Parameters.AddWithValue("@fecha_viaje", fechaViaje.Date);

                int existe = (int)cmdValidar.ExecuteScalar();

                if (existe > 0)
                {
                    TempData["MensajeReserva"] = "Ese asiento ya fue reservado para ese viaje y esa fecha.";
                    return RedirectToAction("ElegirAsiento", new { idReserva = idReserva });
                }

                string queryInsert = @"INSERT INTO AsientoReserva (id_reserva, numero_asiento)
                                       VALUES (@id_reserva, @numero_asiento)";

                SqlCommand cmdInsert = new SqlCommand(queryInsert, con);
                cmdInsert.Parameters.AddWithValue("@id_reserva", idReserva);
                cmdInsert.Parameters.AddWithValue("@numero_asiento", numeroAsiento);

                cmdInsert.ExecuteNonQuery();
            }

            return RedirectToAction("DatosCliente", new { idReserva = idReserva });
        }

        public ActionResult DatosCliente(int idReserva)
        {
            ViewBag.IdReserva = idReserva;
            return View();
        }

        [HttpPost]
        public ActionResult DatosCliente(int idReserva, string nombreCliente, string correoCliente, string telefonoCliente)
        {
            string conexion = ConfigurationManager.ConnectionStrings["KN_WEB_DB"].ConnectionString;

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

                string query = @"UPDATE Reserva
                                 SET nombre_cliente = @nombre_cliente,
                                     correo_cliente = @correo_cliente,
                                     telefono_cliente = @telefono_cliente
                                 WHERE id_reserva = @id_reserva";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@nombre_cliente", nombreCliente);
                cmd.Parameters.AddWithValue("@correo_cliente", correoCliente);
                cmd.Parameters.AddWithValue("@telefono_cliente", telefonoCliente);
                cmd.Parameters.AddWithValue("@id_reserva", idReserva);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Pago", new { idReserva = idReserva });
        }

        public ActionResult Pago(int idReserva)
        {
            ViewBag.IdReserva = idReserva;
            return View();
        }

        [HttpPost]
        public ActionResult Pago(int idReserva, string nombreTarjeta, string numeroTarjeta, string fechaExpiracion, string cvv)
        {
            if (string.IsNullOrWhiteSpace(nombreTarjeta) ||
                string.IsNullOrWhiteSpace(numeroTarjeta) ||
                string.IsNullOrWhiteSpace(fechaExpiracion) ||
                string.IsNullOrWhiteSpace(cvv))
            {
                ViewBag.IdReserva = idReserva;
                ViewBag.Mensaje = "Debes completar todos los datos del pago.";
                return View();
            }

            string ultimos4 = numeroTarjeta.Length >= 4
                ? numeroTarjeta.Substring(numeroTarjeta.Length - 4)
                : numeroTarjeta;

            string mascara = "****-****-****-" + ultimos4;
            string conexion = ConfigurationManager.ConnectionStrings["KN_WEB_DB"].ConnectionString;

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

                string query = @"UPDATE Reserva
                                 SET estado_pago = 'Pagado',
                                     tarjeta_mascara = @tarjeta_mascara,
                                     fecha_pago = GETDATE()
                                 WHERE id_reserva = @id_reserva";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@tarjeta_mascara", mascara);
                cmd.Parameters.AddWithValue("@id_reserva", idReserva);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("ConfirmacionFinal", new { idReserva = idReserva });
        }

        public ActionResult ConfirmacionFinal(int idReserva)
        {
            string conexion = ConfigurationManager.ConnectionStrings["KN_WEB_DB"].ConnectionString;
            DataTable tabla = new DataTable();

            using (SqlConnection con = new SqlConnection(conexion))
            {
                string query = "SELECT * FROM Reserva WHERE id_reserva = @id_reserva";
                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@id_reserva", idReserva);
                da.Fill(tabla);
            }

            if (tabla.Rows.Count > 0)
            {
                ViewBag.Reserva = tabla.Rows[0];
            }

            return View();
        }

        public ActionResult HistorialReservas()
        {
            if (Session["UsuarioEmail"] == null)
            {
                return RedirectToAction("Inicio");
            }

            string conexion = ConfigurationManager.ConnectionStrings["KN_WEB_DB"].ConnectionString;
            DataTable tabla = new DataTable();

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

                string query = @"
                    SELECT 
                        r.id_reserva,
                        r.origen,
                        r.destino,
                        r.fecha_viaje,
                        r.hora_salida,
                        r.cantidad_pasajeros,
                        r.estado_pago,
                        r.estado,
                        ar.numero_asiento
                    FROM Reserva r
                    LEFT JOIN AsientoReserva ar ON r.id_reserva = ar.id_reserva
                    INNER JOIN Usuario u ON r.id_usuario = u.id_usuario
                    WHERE u.email = @email
                    ORDER BY r.id_reserva DESC";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.SelectCommand.Parameters.AddWithValue("@email", Session["UsuarioEmail"].ToString());
                da.Fill(tabla);
            }

            ViewBag.Historial = tabla;
            return View();
        }

        public ActionResult CancelarReserva(int idReserva)
        {
            string conexion = ConfigurationManager.ConnectionStrings["KN_WEB_DB"].ConnectionString;

            using (SqlConnection con = new SqlConnection(conexion))
            {
                con.Open();

                string query = @"UPDATE Reserva
                                 SET estado = 0
                                 WHERE id_reserva = @id_reserva";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id_reserva", idReserva);
                cmd.ExecuteNonQuery();
            }

            TempData["MensajeReserva"] = "Reserva cancelada correctamente.";
            return RedirectToAction("HistorialReservas");
        }

        public ActionResult CerrarSesion()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Inicio");
        }
    }
}