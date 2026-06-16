using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BuscadorFicheros
{
    /// <summary>
    /// Ventana de búsqueda integrada en Inventor. Permite buscar piezas y
    /// ensamblajes por código y abrirlos directamente en Inventor.
    /// </summary>
    public class SearchForm : Form
    {
        private readonly Inventor.Application _app;

        private TextBox _txtCodigo;
        private RadioButton _rdoTodos;
        private RadioButton _rdoPiezas;
        private RadioButton _rdoEnsamblajes;
        private Button _btnBuscar;
        private Button _btnAbrir;
        private ListView _lista;
        private Label _lblEstado;

        public SearchForm(Inventor.Application app)
        {
            _app = app;
            InicializarUI();
        }

        private void InicializarUI()
        {
            Text = "Buscar ficheros en la red";
            Width = 780;
            Height = 540;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(640, 440);

            var lblCodigo = new Label { Text = "Código a buscar:", Left = 12, Top = 16, Width = 110 };
            Controls.Add(lblCodigo);

            _txtCodigo = new TextBox
            {
                Left = 125, Top = 12, Width = 380,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _txtCodigo.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { OnBuscar(s, e); e.SuppressKeyPress = true; }
            };
            Controls.Add(_txtCodigo);

            _btnBuscar = new Button
            {
                Text = "Buscar", Left = 515, Top = 10, Width = 110, Height = 26,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnBuscar.Click += OnBuscar;
            Controls.Add(_btnBuscar);

            _rdoTodos = new RadioButton { Text = "Todos", Checked = true, Left = 125, Top = 44, Width = 70 };
            _rdoPiezas = new RadioButton { Text = "Piezas (.ipt)", Left = 200, Top = 44, Width = 110 };
            _rdoEnsamblajes = new RadioButton { Text = "Ensamblajes (.iam)", Left = 315, Top = 44, Width = 150 };
            Controls.Add(_rdoTodos);
            Controls.Add(_rdoPiezas);
            Controls.Add(_rdoEnsamblajes);

            _lista = new ListView
            {
                Left = 12, Top = 75, Width = 748, Height = 380,
                View = View.Details, FullRowSelect = true, MultiSelect = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            _lista.Columns.Add("Nombre", 230);
            _lista.Columns.Add("Tipo", 70);
            _lista.Columns.Add("Tamaño", 80);
            _lista.Columns.Add("Modificado", 120);
            _lista.Columns.Add("Ruta", 240);
            _lista.DoubleClick += OnAbrir;
            Controls.Add(_lista);

            _lblEstado = new Label
            {
                Left = 12, Top = 468, Width = 500,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(_lblEstado);

            _btnAbrir = new Button
            {
                Text = "Abrir en Inventor", Left = 580, Top = 463, Width = 180, Height = 30,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _btnAbrir.Click += OnAbrir;
            Controls.Add(_btnAbrir);

            AcceptButton = _btnBuscar;

            var carpetas = Config.CargarCarpetas();
            _lblEstado.Text = carpetas.Count > 0
                ? $"{carpetas.Count} carpeta(s) configurada(s)."
                : "Sin carpetas. Edita: " + Config.ConfigFile;
        }

        private TipoBusqueda TipoSeleccionado()
        {
            if (_rdoPiezas.Checked) return TipoBusqueda.Piezas;
            if (_rdoEnsamblajes.Checked) return TipoBusqueda.Ensamblajes;
            return TipoBusqueda.Todos;
        }

        private void OnBuscar(object sender, EventArgs e)
        {
            var codigo = _txtCodigo.Text.Trim();
            if (codigo.Length == 0)
            {
                MessageBox.Show("Escribe un código para buscar.", "Buscador",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var carpetas = Config.CargarCarpetas();
            if (carpetas.Count == 0)
            {
                MessageBox.Show(
                    "No hay carpetas de red configuradas." + Environment.NewLine +
                    "Edita el fichero:" + Environment.NewLine + Config.ConfigFile,
                    "Buscador", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _lista.Items.Clear();
            _lblEstado.Text = "Buscando...";
            Cursor = Cursors.WaitCursor;
            Application.DoEvents();

            var buscador = new FileSearcher(carpetas);
            List<ResultadoBusqueda> resultados = buscador.Buscar(codigo, TipoSeleccionado());

            foreach (var r in resultados)
            {
                var item = new ListViewItem(r.Nombre);
                item.SubItems.Add(r.Extension.TrimStart('.').ToUpperInvariant());
                item.SubItems.Add(r.TamanoLegible);
                item.SubItems.Add(r.Modificado.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(r.Ruta);
                item.Tag = r.Ruta;
                _lista.Items.Add(item);
            }

            Cursor = Cursors.Default;
            var msg = $"{resultados.Count} resultado(s) para \"{codigo}\".";
            if (buscador.Truncado) msg += $" (limitado a {FileSearcher.MaxResultados}, afina el código)";
            _lblEstado.Text = msg;

            if (_lista.Items.Count > 0)
            {
                _lista.Items[0].Selected = true;
                _lista.Select();
            }
        }

        private void OnAbrir(object sender, EventArgs e)
        {
            if (_lista.SelectedItems.Count == 0)
            {
                MessageBox.Show("Selecciona un fichero de la lista.", "Buscador",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ruta = _lista.SelectedItems[0].Tag.ToString();
            try
            {
                _app.Documents.Open(ruta, true);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo abrir el fichero:" + Environment.NewLine + ruta +
                    Environment.NewLine + Environment.NewLine + ex.Message,
                    "Error al abrir", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
