using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Login_AlejandroRR
{
    // ====================================================
    // CLASE GLOBAL: CARRITO
    // ====================================================
    public static class Carrito
    {
        public static List<int> JuegosIds = new List<int>();
        public static void Agregar(int id) { if (!JuegosIds.Contains(id)) JuegosIds.Add(id); }
        public static void Eliminar(int id) { if (JuegosIds.Contains(id)) JuegosIds.Remove(id); }
        public static void Limpiar() { JuegosIds.Clear(); }
    }

    // ====================================================
    // CLASE 1: LOGIN
    // ====================================================
    public partial class Form1 : Form
    {
        private TextBox txtUsuario, txtPass;
        private Label lblEstado;
        // AJUSTA TU PUERTO SI ES NECESARIO (3306 estándar / 3307 XAMPP)
        private string connectionString = "Server=localhost;Port=3306;Database=LoginAlejandroDB;Uid=root;Pwd=;";

        Color cFondo = Color.FromArgb(18, 18, 18);
        Color cInput = Color.FromArgb(32, 32, 32);
        Color cTexto = Color.FromArgb(240, 240, 240);

        public Form1()
        {
            InitializeComponent();
            ConfigurarFormularioManual();
        }
        private void Form1_Load(object sender, EventArgs e) { }

        private void ConfigurarFormularioManual()
        {
            this.Size = new Size(350, 480); this.StartPosition = FormStartPosition.CenterScreen; this.BackColor = cFondo; this.Text = "Login"; this.Controls.Clear();
            Label lblT = new Label { Text = "Iniciar Sesión", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = cTexto, Top = 30, Left = 90, AutoSize = true };
            Label l1 = new Label { Text = "Usuario:", Top = 90, Left = 50, ForeColor = Color.Gray };
            txtUsuario = new TextBox { Top = 115, Left = 50, Width = 230, BackColor = cInput, ForeColor = cTexto, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11) };
            Label l2 = new Label { Text = "Contraseña:", Top = 160, Left = 50, ForeColor = Color.Gray };
            txtPass = new TextBox { Top = 185, Left = 50, Width = 230, BackColor = cInput, ForeColor = cTexto, BorderStyle = BorderStyle.FixedSingle, PasswordChar = '●', Font = new Font("Segoe UI", 11) };
            Button btn = new Button { Text = "ENTRAR", Top = 240, Left = 50, Width = 230, Height = 45, BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0; btn.Click += Login_Click;
            Button btnReg = new Button { Text = "Crear Cuenta", Top = 300, Left = 50, Width = 230, Height = 35, BackColor = cInput, ForeColor = cTexto, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnReg.FlatAppearance.BorderSize = 0; btnReg.Click += (s, e) => new FormRegistro(connectionString).ShowDialog();
            lblEstado = new Label { Top = 350, Left = 20, Width = 300, ForeColor = Color.IndianRed, TextAlign = ContentAlignment.MiddleCenter };
            this.Controls.Add(lblT); this.Controls.Add(l1); this.Controls.Add(txtUsuario); this.Controls.Add(l2); this.Controls.Add(txtPass); this.Controls.Add(btn); this.Controls.Add(btnReg); this.Controls.Add(lblEstado);
        }

        private void Login_Click(object sender, EventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT id, password, es_admin, banned FROM Usuarios WHERE nombre_usuario=@u", conn);
                    cmd.Parameters.AddWithValue("@u", txtUsuario.Text);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            if (r.GetString("password") == txtPass.Text)
                            {
                                if (r.GetBoolean("banned")) { MessageBox.Show("Baneado"); return; }
                                this.Hide(); new FormHome(txtUsuario.Text, r.GetInt32("id"), r.GetBoolean("es_admin"), connectionString).ShowDialog(); this.Show(); txtPass.Text = "";
                            }
                            else lblEstado.Text = "Pass incorrecta";
                        }
                        else lblEstado.Text = "Usuario no existe";
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error conexión: " + ex.Message); }
        }
    }

    public class FormRegistro : Form
    {
        public FormRegistro(string c) { this.Size = new Size(300, 250); BackColor = Color.FromArgb(18, 18, 18); StartPosition = FormStartPosition.CenterParent; TextBox tU = new TextBox { Top = 45, Left = 20, Width = 240 }, tP = new TextBox { Top = 90, Left = 20, Width = 240 }; Button b = new Button { Text = "Guardar", Top = 140, Left = 20, BackColor = Color.SeaGreen }; b.Click += (s, e) => { using (var k = new MySqlConnection(c)) { k.Open(); new MySqlCommand($"INSERT INTO Usuarios(nombre_usuario,password) VALUES('{tU.Text}','{tP.Text}')", k).ExecuteNonQuery(); } Close(); }; Controls.Add(new Label { Text = "Usuario", Top = 20, Left = 20, ForeColor = Color.White }); Controls.Add(tU); Controls.Add(new Label { Text = "Pass", Top = 70, Left = 20, ForeColor = Color.White }); Controls.Add(tP); Controls.Add(b); }
    }

    // ====================================================
    // CLASE 3: HOME
    // ====================================================
    public class FormHome : Form
    {
        private Panel pnlSidebar, pnlTopBar;
        private FlowLayoutPanel flowContent;
        private System.Windows.Forms.Timer tmrMenu;
        private bool sidebarExpandida = false;
        private const int ANCHO_SIDEBAR = 220;
        private bool esAdmin; private int userId; private string connectionString; private bool viendoBiblioteca = false;
        private TrackBar trackPrecio; private Label lblPrecioMax;

        Color cFondo = Color.FromArgb(18, 18, 18); Color cPanel = Color.FromArgb(32, 32, 32); Color cTexto = Color.FromArgb(240, 240, 240); Color cAcento = Color.SteelBlue;

        public FormHome(string nombreUsuario, int idU, bool admin, string connStr)
        {
            this.userId = idU; this.esAdmin = admin; this.connectionString = connStr;
            this.Size = new Size(1200, 750); this.StartPosition = FormStartPosition.CenterScreen; this.BackColor = cFondo; this.ForeColor = cTexto; this.DoubleBuffered = true;
            InicializarComponentes(nombreUsuario); CargarJuegos();
        }

        private void InicializarComponentes(string usuario)
        {
            tmrMenu = new System.Windows.Forms.Timer { Interval = 10 };
            tmrMenu.Tick += (s, e) => { if (sidebarExpandida) { pnlSidebar.Width -= 20; if (pnlSidebar.Width <= 0) { pnlSidebar.Width = 0; sidebarExpandida = false; tmrMenu.Stop(); } } else { pnlSidebar.Width += 20; if (pnlSidebar.Width >= ANCHO_SIDEBAR) { pnlSidebar.Width = ANCHO_SIDEBAR; sidebarExpandida = true; tmrMenu.Stop(); } } };

            pnlTopBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(25, 25, 25) }; this.Controls.Add(pnlTopBar);
            Button btnMenu = new Button { Text = "☰", Font = new Font("Segoe UI", 16), FlatStyle = FlatStyle.Flat, ForeColor = cTexto, Size = new Size(60, 60), Location = new Point(0, 0), Cursor = Cursors.Hand }; btnMenu.FlatAppearance.BorderSize = 0; btnMenu.Click += (s, e) => tmrMenu.Start(); pnlTopBar.Controls.Add(btnMenu);
            Label lblApp = new Label { Text = "NINTENDO ESHOP", ForeColor = Color.Gray, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = true, Top = 20, Left = 70 }; pnlTopBar.Controls.Add(lblApp);

            Label lblFiltro = new Label { Text = "Precio Max:", Top = 10, Left = 300, AutoSize = true, ForeColor = Color.Gray };
            lblPrecioMax = new Label { Text = "100€", Top = 35, Left = 300, AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            trackPrecio = new TrackBar { Top = 10, Left = 380, Width = 200, Minimum = 0, Maximum = 100, Value = 100, TickStyle = TickStyle.None };
            trackPrecio.Scroll += (s, e) => { lblPrecioMax.Text = trackPrecio.Value + "€"; CargarJuegos(); };
            pnlTopBar.Controls.Add(lblFiltro); pnlTopBar.Controls.Add(lblPrecioMax); pnlTopBar.Controls.Add(trackPrecio);

            Label lblUser = new Label { Text = usuario.ToUpper() + (esAdmin ? " [ADMIN]" : ""), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = cAcento, Location = new Point(pnlTopBar.Width - 200, 20), Anchor = AnchorStyles.Top | AnchorStyles.Right }; pnlTopBar.Controls.Add(lblUser);

            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = 0, BackColor = Color.FromArgb(28, 28, 28) }; this.Controls.Add(pnlSidebar);

            // --- LOGO NINTENDO (AGRANDADO) ---
            PictureBox pbLogo = new PictureBox();
            // ANTES: Size(180, 70) -> AHORA: Size(200, 100) (Más ancho y alto)
            pbLogo.Size = new Size(200, 100);
            // ANTES: Point(20, 20) -> AHORA: Point(10, 10) (Más centrado)
            pbLogo.Location = new Point(10, 10);
            pbLogo.SizeMode = PictureBoxSizeMode.Zoom; // Mantiene proporción sin estirar
            try
            {
                string rutaLogo = Path.Combine(Application.StartupPath, "Imagenes", "Nintendo.png");
                if (File.Exists(rutaLogo)) pbLogo.Image = Image.FromFile(rutaLogo);
            }
            catch { }
            pnlSidebar.Controls.Add(pbLogo);

            // IMPORTANTE: Bajamos los botones para que no toquen el logo gigante
            // ANTES: y = 120 -> AHORA: y = 140
            int y = 140;
            CrearBtnMenu("TIENDA", y, (s, e) => { viendoBiblioteca = false; CargarJuegos(); });
            CrearBtnMenu("MIS JUEGOS", y += 60, (s, e) => { viendoBiblioteca = true; CargarJuegos(); });
            CrearBtnMenu("CARRITO", y += 60, (s, e) => { new FormCarrito(userId, connectionString).ShowDialog(); if (viendoBiblioteca) CargarJuegos(); });
            if (esAdmin) { Button btnAdmin = CrearBtnMenu("PANEL ADMIN", y += 60, (s, e) => new FormAdmin(connectionString).ShowDialog()); btnAdmin.ForeColor = Color.Gold; }
            Button btnSalir = new Button { Text = "CERRAR SESIÓN", Dock = DockStyle.Bottom, Height = 50, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(192, 57, 43), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) }; btnSalir.FlatAppearance.BorderSize = 0; btnSalir.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); }; pnlSidebar.Controls.Add(btnSalir);

            flowContent = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = cFondo, AutoScroll = true, Padding = new Padding(20, 90, 20, 20) };
            this.Controls.Add(flowContent);

            pnlSidebar.BringToFront(); pnlTopBar.BringToFront();
        }

        private Button CrearBtnMenu(string txt, int top, EventHandler evt)
        {
            Button b = new Button { Text = txt, Top = top, Left = 0, Width = 220, Height = 50, FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = Color.Silver, Font = new Font("Segoe UI", 11), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0;
            b.MouseEnter += (s, e) => { b.BackColor = Color.FromArgb(50, 50, 50); b.ForeColor = Color.White; }; b.MouseLeave += (s, e) => { b.BackColor = Color.Transparent; b.ForeColor = Color.Silver; }; b.Click += evt; pnlSidebar.Controls.Add(b); return b;
        }

        private void CargarJuegos()
        {
            flowContent.SuspendLayout(); flowContent.Controls.Clear();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = viendoBiblioteca
                        ? "SELECT j.id, j.titulo, j.imagen FROM Juegos j INNER JOIN Biblioteca b ON j.id = b.id_juego WHERE b.id_usuario = @u"
                        : "SELECT id, titulo, imagen FROM Juegos WHERE precio <= @max";
                    MySqlCommand cmd = new MySqlCommand(query, conn); if (viendoBiblioteca) cmd.Parameters.AddWithValue("@u", userId); else cmd.Parameters.AddWithValue("@max", trackPrecio.Value);
                    DataTable dt = new DataTable(); new MySqlDataAdapter(cmd).Fill(dt);

                    if (dt.Rows.Count == 0 && viendoBiblioteca)
                    {
                        Label l = new Label { Text = "Aún no tienes juegos comprados.", ForeColor = Color.Gray, AutoSize = true, Font = new Font("Segoe UI", 12), Margin = new Padding(20) };
                        flowContent.Controls.Add(l);
                    }

                    foreach (DataRow row in dt.Rows) CrearFicha(Convert.ToInt32(row["id"]), row["titulo"].ToString(), row["imagen"] as byte[]);
                }
            }
            catch { }
            flowContent.ResumeLayout();
        }

        private void CrearFicha(int id, string titulo, byte[] imgData)
        {
            Panel p = new Panel { Size = new Size(200, 260), BackColor = cPanel, Margin = new Padding(25) };
            PictureBox pb = new PictureBox { Size = new Size(180, 180), Location = new Point(10, 10), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(40, 40, 40) };
            if (imgData != null && imgData.Length > 0) using (var ms = new MemoryStream(imgData)) pb.Image = Image.FromStream(ms);

            pb.Cursor = Cursors.Hand; pb.Click += (s, e) => { new FormDetalleJuego(id, userId, connectionString).ShowDialog(); if (viendoBiblioteca) CargarJuegos(); };
            Label lbl = new Label { Text = titulo, Dock = DockStyle.Bottom, Height = 50, ForeColor = cTexto, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 11) };
            Button btnLine = new Button { Dock = DockStyle.Bottom, Height = 5, BackColor = cAcento, FlatStyle = FlatStyle.Flat }; btnLine.FlatAppearance.BorderSize = 0;
            p.Controls.Add(pb); p.Controls.Add(lbl); p.Controls.Add(btnLine);
            flowContent.Controls.Add(p);
        }
    }

    // ====================================================
    // CLASE 4: ADMIN
    // ====================================================
    public class FormAdmin : Form
    {
        private Panel pnlSidebar, pnlGridContainer;
        private DataGridView dgv;
        private Button btnSwitchUser, btnSwitchJuegos;
        private TextBox txtId, txtNombreTitulo, txtPassFab, txtBuscar, txtDesc;
        private NumericUpDown numAnio, numPrecio;
        private CheckBox chk1, chk2;
        private PictureBox pbPreview;
        private string rutaImgTemp;
        private string connStr;
        private bool modoJuegos = false;

        Color cBg = Color.FromArgb(18, 18, 18); Color cSide = Color.FromArgb(28, 28, 28); Color cInput = Color.FromArgb(45, 45, 45);
        Color cBtnGreen = Color.SeaGreen; Color cBtnBlue = Color.SteelBlue; Color cBtnRed = Color.IndianRed;

        public FormAdmin(string c)
        {
            connStr = c; this.Size = new Size(1100, 650); this.StartPosition = FormStartPosition.CenterScreen; this.BackColor = cBg; this.Text = "Panel de Administración";
            InicializarInterfaz(); CargarModoUsuarios();
        }

        private void InicializarInterfaz()
        {
            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = 360, BackColor = cSide, Padding = new Padding(0) };
            pnlGridContainer = new Panel { Dock = DockStyle.Fill, BackColor = cBg, Padding = new Padding(20) };
            this.Controls.Add(pnlGridContainer); this.Controls.Add(pnlSidebar);

            Panel pnlTopBtns = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent };
            btnSwitchUser = CrearBtnSwitch("USUARIOS", 180, 0); btnSwitchJuegos = CrearBtnSwitch("JUEGOS", 180, 180);
            btnSwitchUser.Click += (s, e) => CargarModoUsuarios(); btnSwitchJuegos.Click += (s, e) => CargarModoJuegos();
            pnlTopBtns.Controls.Add(btnSwitchUser); pnlTopBtns.Controls.Add(btnSwitchJuegos); pnlSidebar.Controls.Add(pnlTopBtns);

            dgv = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = cBg, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, EnableHeadersVisualStyles = false, ReadOnly = true, AllowUserToAddRows = false };
            dgv.ColumnHeadersHeight = 40;
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.Black, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            dgv.DefaultCellStyle = new DataGridViewCellStyle { BackColor = cInput, ForeColor = Color.White, SelectionBackColor = cBtnBlue, SelectionForeColor = Color.White };
            dgv.SelectionChanged += Dgv_SelectionChanged;

            Panel pnlSearch = new Panel { Dock = DockStyle.Top, Height = 60 };
            txtBuscar = new TextBox { Top = 15, Left = 0, Width = 300, BackColor = cInput, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11) };
            Button btnBuscar = new Button { Text = "🔍", Top = 15, Left = 310, Width = 50, Height = 27, BackColor = Color.DimGray, FlatStyle = FlatStyle.Flat };
            btnBuscar.Click += (s, e) => { string q = modoJuegos ? $"SELECT * FROM Juegos WHERE titulo LIKE '%{txtBuscar.Text}%'" : $"SELECT * FROM Usuarios WHERE nombre_usuario LIKE '%{txtBuscar.Text}%'"; CargarTabla(q); };
            pnlSearch.Controls.Add(txtBuscar); pnlSearch.Controls.Add(btnBuscar);
            pnlGridContainer.Controls.Add(dgv); pnlGridContainer.Controls.Add(pnlSearch);
        }

        private void CargarModoUsuarios()
        {
            modoJuegos = false; ResaltarBoton(btnSwitchUser, btnSwitchJuegos); LimpiarInputsSidebar();
            CrearLabel("ID:", 80); txtId = CrearInput(100, true); CrearLabel("Usuario:", 150); txtNombreTitulo = CrearInput(170, false); CrearLabel("Contraseña:", 220); txtPassFab = CrearInput(240, false);
            chk1 = new CheckBox { Text = "Es Administrador", Top = 290, Left = 15, ForeColor = Color.White, AutoSize = true }; chk2 = new CheckBox { Text = "Baneado", Top = 320, Left = 15, ForeColor = Color.Red, AutoSize = true }; pnlSidebar.Controls.Add(chk1); pnlSidebar.Controls.Add(chk2);
            CrearBotonesAccion(360); CargarTabla("SELECT * FROM Usuarios");
        }

        private void CargarModoJuegos()
        {
            modoJuegos = true; ResaltarBoton(btnSwitchJuegos, btnSwitchUser); LimpiarInputsSidebar();
            CrearLabel("ID:", 80); txtId = CrearInput(100, true); CrearLabel("Título:", 140); txtNombreTitulo = CrearInput(160, false);
            CrearLabel("Año:", 200); numAnio = new NumericUpDown { Top = 220, Left = 15, Width = 100, BackColor = cInput, ForeColor = Color.White, Maximum = 2030, Value = 2024 }; pnlSidebar.Controls.Add(numAnio);
            CrearLabel("Precio:", 200, 140); numPrecio = new NumericUpDown { Top = 220, Left = 145, Width = 100, BackColor = cInput, ForeColor = Color.White, DecimalPlaces = 2, Maximum = 999 }; pnlSidebar.Controls.Add(numPrecio);
            CrearLabel("Fabricante:", 250); txtPassFab = CrearInput(270, false);
            CrearLabel("Descripción:", 310); txtDesc = new TextBox { Top = 330, Left = 15, Width = 330, Height = 60, Multiline = true, BackColor = cInput, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle }; pnlSidebar.Controls.Add(txtDesc);
            Button btnImg = new Button { Text = "Imagen...", Top = 410, Left = 15, Width = 100, BackColor = Color.Gray, FlatStyle = FlatStyle.Flat };
            btnImg.Click += (s, e) => { OpenFileDialog ofd = new OpenFileDialog(); if (ofd.ShowDialog() == DialogResult.OK) { rutaImgTemp = ofd.FileName; pbPreview.Image = Image.FromFile(rutaImgTemp); } };
            pbPreview = new PictureBox { Top = 400, Left = 130, Size = new Size(60, 60), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Black };
            pnlSidebar.Controls.Add(btnImg); pnlSidebar.Controls.Add(pbPreview);
            CrearBotonesAccion(480); CargarTabla("SELECT id, titulo, precio, fabricante, descripcion FROM Juegos");
        }

        private void AccionGuardar()
        {
            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                if (!modoJuegos) { new MySqlCommand($"INSERT INTO Usuarios (nombre_usuario, password, es_admin, banned) VALUES ('{txtNombreTitulo.Text}', '{txtPassFab.Text}', {(chk1.Checked ? 1 : 0)}, {(chk2.Checked ? 1 : 0)})", conn).ExecuteNonQuery(); CargarTabla("SELECT * FROM Usuarios"); }
                else
                {
                    byte[] img = null; if (!string.IsNullOrEmpty(rutaImgTemp)) img = File.ReadAllBytes(rutaImgTemp);
                    var cmd = new MySqlCommand("INSERT INTO Juegos (titulo, anio, precio, fabricante, descripcion, imagen) VALUES (@t, @a, @p, @f, @d, @i)", conn);
                    cmd.Parameters.AddWithValue("@t", txtNombreTitulo.Text); cmd.Parameters.AddWithValue("@a", numAnio.Value); cmd.Parameters.AddWithValue("@p", numPrecio.Value); cmd.Parameters.AddWithValue("@f", txtPassFab.Text); cmd.Parameters.AddWithValue("@d", txtDesc.Text); cmd.Parameters.AddWithValue("@i", img);
                    cmd.ExecuteNonQuery(); CargarTabla("SELECT id, titulo, precio, fabricante, descripcion FROM Juegos");
                }
                LimpiarCampos();
            }
        }

        private void AccionModificar()
        {
            if (string.IsNullOrEmpty(txtId.Text)) return;
            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                if (!modoJuegos)
                {
                    new MySqlCommand($"UPDATE Usuarios SET nombre_usuario='{txtNombreTitulo.Text}', password='{txtPassFab.Text}', es_admin={(chk1.Checked ? 1 : 0)}, banned={(chk2.Checked ? 1 : 0)} WHERE id={txtId.Text}", conn).ExecuteNonQuery();
                    CargarTabla("SELECT * FROM Usuarios");
                }
                else
                {
                    string sql = "UPDATE Juegos SET titulo=@t, anio=@a, precio=@p, fabricante=@f, descripcion=@d";
                    if (!string.IsNullOrEmpty(rutaImgTemp)) sql += ", imagen=@i";
                    sql += " WHERE id=@id";
                    var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@t", txtNombreTitulo.Text); cmd.Parameters.AddWithValue("@a", numAnio.Value); cmd.Parameters.AddWithValue("@p", numPrecio.Value); cmd.Parameters.AddWithValue("@f", txtPassFab.Text); cmd.Parameters.AddWithValue("@d", txtDesc.Text); cmd.Parameters.AddWithValue("@id", txtId.Text);
                    if (!string.IsNullOrEmpty(rutaImgTemp)) { byte[] img = File.ReadAllBytes(rutaImgTemp); cmd.Parameters.AddWithValue("@i", img); }
                    cmd.ExecuteNonQuery();
                    CargarTabla("SELECT id, titulo, precio, fabricante, descripcion FROM Juegos");
                }
                LimpiarCampos(); MessageBox.Show("Registro actualizado.");
            }
        }

        private void AccionEliminar()
        {
            if (string.IsNullOrEmpty(txtId.Text)) return;
            if (MessageBox.Show("¿Eliminar?", "Confirmar", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                using (var conn = new MySqlConnection(connStr)) { conn.Open(); new MySqlCommand($"DELETE FROM {(modoJuegos ? "Juegos" : "Usuarios")} WHERE id={txtId.Text}", conn).ExecuteNonQuery(); }
                if (modoJuegos) CargarModoJuegos(); else CargarModoUsuarios(); LimpiarCampos();
            }
        }

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count > 0)
            {
                var r = dgv.SelectedRows[0]; txtId.Text = r.Cells["id"].Value.ToString();
                if (!modoJuegos)
                {
                    txtNombreTitulo.Text = r.Cells["nombre_usuario"].Value.ToString(); txtPassFab.Text = r.Cells["password"].Value.ToString();
                    chk1.Checked = Convert.ToBoolean(r.Cells["es_admin"].Value); chk2.Checked = Convert.ToBoolean(r.Cells["banned"].Value);
                }
                else
                {
                    txtNombreTitulo.Text = r.Cells["titulo"].Value.ToString();
                    txtPassFab.Text = r.Cells["fabricante"].Value.ToString();
                    if (r.Cells["descripcion"].Value != null) txtDesc.Text = r.Cells["descripcion"].Value.ToString();
                    numPrecio.Value = Convert.ToDecimal(r.Cells["precio"].Value);
                    try
                    {
                        using (var c = new MySqlConnection(connStr))
                        {
                            c.Open(); var cmd = new MySqlCommand("SELECT imagen, anio FROM Juegos WHERE id=" + txtId.Text, c);
                            using (var rd = cmd.ExecuteReader()) if (rd.Read())
                                {
                                    numAnio.Value = Convert.ToInt32(rd["anio"]);
                                    byte[] b = rd["imagen"] as byte[]; if (b != null && b.Length > 0) using (var ms = new MemoryStream(b)) pbPreview.Image = Image.FromStream(ms); else pbPreview.Image = null;
                                }
                        }
                    }
                    catch { }
                }
            }
        }

        private void LimpiarInputsSidebar() { for (int i = pnlSidebar.Controls.Count - 1; i >= 0; i--) { if (pnlSidebar.Controls[i] is Panel) continue; pnlSidebar.Controls.RemoveAt(i); } }
        private void LimpiarCampos() { if (txtId != null) txtId.Text = ""; if (txtNombreTitulo != null) txtNombreTitulo.Text = ""; if (txtPassFab != null) txtPassFab.Text = ""; if (txtDesc != null) txtDesc.Text = ""; if (pbPreview != null) pbPreview.Image = null; rutaImgTemp = ""; }
        private void CargarTabla(string q) { using (var c = new MySqlConnection(connStr)) { c.Open(); var dt = new DataTable(); new MySqlDataAdapter(q, c).Fill(dt); dgv.DataSource = dt; } }
        private void CrearLabel(string t, int top, int left = 15) { pnlSidebar.Controls.Add(new Label { Text = t, Top = top, Left = left, ForeColor = Color.Gray, AutoSize = true }); }
        private TextBox CrearInput(int top, bool ro) { var t = new TextBox { Top = top, Left = 15, Width = 330, BackColor = ro ? Color.FromArgb(60, 60, 60) : cInput, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, ReadOnly = ro }; pnlSidebar.Controls.Add(t); return t; }
        private Button CrearBtnSwitch(string t, int w, int x) { return new Button { Text = t, Top = 0, Left = x, Width = w, Height = 50, FlatStyle = FlatStyle.Flat, BackColor = cSide, ForeColor = Color.Gray, Font = new Font("Segoe UI", 10, FontStyle.Bold) }; }
        private void ResaltarBoton(Button a, Button i) { a.ForeColor = Color.White; a.BackColor = cInput; i.ForeColor = Color.Gray; i.BackColor = cSide; }
        private void CrearBotonesAccion(int top) { CrearBtnCRUD("AGREGAR", top, cBtnGreen, AccionGuardar); CrearBtnCRUD("MODIFICAR", top + 40, cBtnBlue, AccionModificar); CrearBtnCRUD("ELIMINAR", top + 80, cBtnRed, AccionEliminar); CrearBtnCRUD("LIMPIAR", top + 120, Color.Gray, LimpiarCampos); }
        private void CrearBtnCRUD(string t, int top, Color c, Action ev) { Button b = new Button { Text = t, Top = top, Left = 15, Width = 330, Height = 35, BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; b.Click += (s, e) => ev(); pnlSidebar.Controls.Add(b); }
    }

    // ====================================================
    // CLASE 5: DETALLE JUEGO
    // ====================================================
    public class FormDetalleJuego : Form
    {
        public FormDetalleJuego(int id, int u, string c)
        {
            this.Size = new Size(600, 450); this.StartPosition = FormStartPosition.CenterParent; this.BackColor = Color.FromArgb(25, 25, 25); this.ForeColor = Color.White; this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            using (var kn = new MySqlConnection(c))
            {
                kn.Open(); var cmd = new MySqlCommand("SELECT * FROM Juegos WHERE id=" + id, kn); using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        byte[] img = r["imagen"] as byte[];
                        PictureBox pb = new PictureBox { Left = 20, Top = 20, Size = new Size(220, 300), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(40, 40, 40), BorderStyle = BorderStyle.FixedSingle };
                        if (img != null && img.Length > 0) using (var ms = new MemoryStream(img)) pb.Image = Image.FromStream(ms);
                        this.Controls.Add(pb);

                        this.Controls.Add(new Label { Text = r["titulo"].ToString(), Left = 260, Top = 20, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.White, AutoSize = true });
                        this.Controls.Add(new Label { Text = Convert.ToDecimal(r["precio"]).ToString("C2"), Left = 260, Top = 60, Font = new Font("Segoe UI", 14), ForeColor = Color.LightGreen, AutoSize = true });
                        this.Controls.Add(new Label { Text = "Fabricante: " + r["fabricante"].ToString(), Left = 260, Top = 100, ForeColor = Color.Gray, AutoSize = true });
                        this.Controls.Add(new Label { Text = "Año: " + r["anio"].ToString(), Left = 260, Top = 125, ForeColor = Color.Gray, AutoSize = true });
                        this.Controls.Add(new Label { Text = "Descripción:", Left = 260, Top = 160, ForeColor = Color.Silver, AutoSize = true });
                        Label lblDesc = new Label { Text = r["descripcion"].ToString(), Left = 260, Top = 185, Width = 300, Height = 120, ForeColor = Color.WhiteSmoke, Font = new Font("Segoe UI", 9), BackColor = Color.FromArgb(35, 35, 35), Padding = new Padding(5) };
                        this.Controls.Add(lblDesc);

                        Button btnBuy = new Button { Text = "COMPRAR", Left = 260, Top = 330, Width = 140, Height = 40, BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
                        btnBuy.Click += (s, e) => {
                            try
                            {
                                using (var connCompra = new MySqlConnection(c))
                                {
                                    connCompra.Open();
                                    new MySqlCommand($"INSERT INTO Biblioteca (id_usuario, id_juego, fecha_compra) VALUES({u},{id},NOW())", connCompra).ExecuteNonQuery();
                                }
                                MessageBox.Show("¡Juego añadido a tu biblioteca!"); this.Close();
                            }
                            catch (MySqlException ex)
                            {
                                if (ex.Number == 1062) MessageBox.Show("Ya tienes este juego.");
                                else MessageBox.Show("Error: " + ex.Message);
                            }
                        };
                        Button btnCart = new Button { Text = "AÑADIR AL CARRO", Left = 410, Top = 330, Width = 140, Height = 40, BackColor = Color.DarkGoldenrod, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
                        btnCart.Click += (s, e) => { Carrito.Agregar(id); MessageBox.Show("Añadido al carrito"); };
                        this.Controls.Add(btnBuy); this.Controls.Add(btnCart);
                    }
                }
            }
        }
    }

    // ====================================================
    // CLASE 6: CARRITO
    // ====================================================
    public class FormCarrito : Form
    {
        private DataGridView dgv;
        private List<int> idsTemp;

        public FormCarrito(int u, string c)
        {
            Size = new Size(500, 500); BackColor = Color.FromArgb(25, 25, 25); StartPosition = FormStartPosition.CenterParent; this.Text = "Tu Carrito";
            idsTemp = new List<int>(Carrito.JuegosIds);

            dgv = new DataGridView { Top = 20, Left = 20, Width = 440, Height = 350, BackgroundColor = Color.FromArgb(40, 40, 40), BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, RowHeadersVisible = false, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.Black, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            dgv.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, SelectionBackColor = Color.SteelBlue };
            dgv.EnableHeadersVisualStyles = false;

            dgv.Columns.Add("id", "ID"); dgv.Columns["id"].Visible = false;
            dgv.Columns.Add("titulo", "Juego"); dgv.Columns["titulo"].Width = 200;
            dgv.Columns.Add("precio", "Precio");

            DataGridViewButtonColumn btnCol = new DataGridViewButtonColumn();
            btnCol.Name = "eliminar"; btnCol.Text = "🗑"; btnCol.UseColumnTextForButtonValue = true; btnCol.HeaderText = ""; btnCol.Width = 40;
            btnCol.FlatStyle = FlatStyle.Flat; btnCol.DefaultCellStyle.BackColor = Color.IndianRed; btnCol.DefaultCellStyle.ForeColor = Color.White;
            dgv.Columns.Add(btnCol);

            if (idsTemp.Count > 0)
            {
                using (var k = new MySqlConnection(c))
                {
                    k.Open();
                    string ids = string.Join(",", idsTemp);
                    var cmd = new MySqlCommand($"SELECT id, titulo, precio FROM Juegos WHERE id IN ({ids})", k);
                    using (var r = cmd.ExecuteReader()) while (r.Read()) dgv.Rows.Add(r["id"], r["titulo"], r["precio"]);
                }
            }

            dgv.CellContentClick += (s, e) => {
                if (e.ColumnIndex == dgv.Columns["eliminar"].Index && e.RowIndex >= 0)
                {
                    int id = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["id"].Value);
                    Carrito.Eliminar(id);
                    dgv.Rows.RemoveAt(e.RowIndex);
                }
            };

            Button btnPagar = new Button { Text = "PAGAR TODO", Top = 400, Left = 20, Width = 440, Height = 40, BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnPagar.Click += (s, e) => {
                if (dgv.Rows.Count == 0) return;
                using (var k = new MySqlConnection(c))
                {
                    k.Open(); foreach (DataGridViewRow row in dgv.Rows)
                    {
                        try
                        {
                            int id = Convert.ToInt32(row.Cells["id"].Value);
                            new MySqlCommand($"INSERT INTO Biblioteca (id_usuario, id_juego, fecha_compra) VALUES({u},{id},NOW())", k).ExecuteNonQuery();
                        }
                        catch { }
                    }
                }
                Carrito.Limpiar(); MessageBox.Show("Compra realizada. ¡A jugar!"); Close();
            };
            Controls.Add(dgv); Controls.Add(btnPagar);
        }
    }
}