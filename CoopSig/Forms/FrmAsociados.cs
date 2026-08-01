using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CoopSig.Data;
using CoopSig.Models;

namespace CoopSig.Forms
{
    /// <summary>
    /// Búsqueda y listado de asociados (HU-1, HU-4). El padrón se carga en
    /// memoria una única vez al abrir la pantalla y se filtra en cliente
    /// (ver "Rendimiento" en plan.md): sobre 4.202 registros es más rápido
    /// y simple que consultar la base en cada tecla.
    /// </summary>
    public class FrmAsociados : Form
    {
        private readonly AsociadoRepository _repositorio = new AsociadoRepository();
        private List<Asociado> _padron = new List<Asociado>();

        /// <summary>Opción del filtro que representa "no filtrar por servicio".</summary>
        private const string TodosLosServicios = "(Todos los servicios)";

        private TextBox _txtBuscar;
        private ComboBox _cmbFiltroServicio;
        private CheckBox _chkIncluirBajas;

        /// <summary>
        /// Activo mientras se repuebla el filtro de servicios. Vaciar y volver a
        /// llenar un ComboBox dispara SelectedIndexChanged varias veces, y sin
        /// esta guarda cada recarga del padrón filtraría la grilla de más.
        /// </summary>
        private bool _poblandoFiltroServicio;
        private DataGridView _grilla;
        private Button _btnNuevo;
        private Button _btnEditar;
        private Button _btnBaja;
        private Timer _timerDebounce;

        /// <summary>
        /// Activo mientras se puebla la grilla. DataGridView.Rows.Add dispara
        /// SelectionChanged apenas existe la primera fila, es decir ANTES de que
        /// se le asigne el Tag con el asociado. Sin esta guarda el manejador lee
        /// un Tag todavía nulo y la carga muere en la primera fila.
        /// </summary>
        private bool _poblandoGrilla;

        public FrmAsociados()
        {
            InicializarComponentes();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            RecargarYFiltrar();
            _txtBuscar.Focus();
        }

        private void InicializarComponentes()
        {
            Text = "Asociados";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(820, 520);
            MinimumSize = new Size(700, 400);
            Font = new Font("Segoe UI", 9.5F);

            var lblBuscar = new Label
            {
                Text = "Buscar (documento o apellido):",
                Location = new Point(12, 15),
                AutoSize = true
            };

            _txtBuscar = new TextBox
            {
                Location = new Point(12, 35),
                Size = new Size(320, 25),
                Font = new Font("Segoe UI", 11F)
            };
            _txtBuscar.TextChanged += (s, e) => ReiniciarDebounce();
            _txtBuscar.KeyDown += TxtBuscar_KeyDown;

            var lblServicio = new Label
            {
                Text = "Servicio:",
                Location = new Point(345, 15),
                AutoSize = true
            };

            // Filtro aparte del buscador de texto: se combinan, no compiten.
            // Las opciones salen del padrón ya cargado en memoria, así que no
            // hace falta otra consulta a la base.
            _cmbFiltroServicio = new ComboBox
            {
                Location = new Point(345, 35),
                Size = new Size(250, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbFiltroServicio.SelectedIndexChanged += (s, e) =>
            {
                if (!_poblandoFiltroServicio)
                {
                    AplicarFiltro();
                }
            };

            // HU-4: por defecto el listado muestra solo activos.
            _chkIncluirBajas = new CheckBox
            {
                Text = "Incluir bajas",
                Location = new Point(610, 38),
                AutoSize = true,
                Checked = false
            };
            _chkIncluirBajas.CheckedChanged += (s, e) => AplicarFiltro();

            _grilla = new DataGridView
            {
                Location = new Point(12, 70),
                Size = new Size(796, 380),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };
            _grilla.Columns.Add("Apellido", "Apellido");
            _grilla.Columns.Add("Nombre", "Nombre");
            _grilla.Columns.Add("Documento", "Documento");
            _grilla.Columns.Add("Servicio", "Servicio");
            _grilla.Columns.Add("Estado", "Estado");
            _grilla.CellDoubleClick += (s, e) => AbrirFichaSeleccionada();
            _grilla.KeyDown += Grilla_KeyDown;
            _grilla.SelectionChanged += (s, e) => ActualizarBotones();

            var menuContextual = new ContextMenuStrip();
            menuContextual.Items.Add("Editar").Click += (s, e) => AbrirFichaSeleccionada();
            menuContextual.Items.Add("Baja / Reactivar").Click += (s, e) => AlternarBajaSeleccionado();
            _grilla.ContextMenuStrip = menuContextual;

            _btnNuevo = new Button
            {
                Text = "&Nuevo",
                Location = new Point(12, 460),
                Size = new Size(110, 32),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnNuevo.Click += (s, e) => AbrirFichaNueva();

            _btnEditar = new Button
            {
                Text = "&Editar",
                Location = new Point(130, 460),
                Size = new Size(110, 32),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnEditar.Click += (s, e) => AbrirFichaSeleccionada();

            _btnBaja = new Button
            {
                Text = "&Baja",
                Location = new Point(248, 460),
                Size = new Size(110, 32),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnBaja.Click += (s, e) => AlternarBajaSeleccionado();

            Controls.Add(lblBuscar);
            Controls.Add(_txtBuscar);
            Controls.Add(lblServicio);
            Controls.Add(_cmbFiltroServicio);
            Controls.Add(_chkIncluirBajas);
            Controls.Add(_grilla);
            Controls.Add(_btnNuevo);
            Controls.Add(_btnEditar);
            Controls.Add(_btnBaja);

            // HU-1: filtra mientras se tipea, sin botón "Buscar" — debounce de 300 ms.
            _timerDebounce = new Timer { Interval = 300 };
            _timerDebounce.Tick += (s, e) =>
            {
                _timerDebounce.Stop();
                AplicarFiltro();
            };

            ActualizarBotones();
        }

        private void ReiniciarDebounce()
        {
            _timerDebounce.Stop();
            _timerDebounce.Start();
        }

        private void TxtBuscar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // HU-5: buscar y abrir la ficha es posible sin usar el mouse.
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (_grilla.Rows.Count > 0)
                {
                    _grilla.Focus();
                    _grilla.Rows[0].Selected = true;
                    _grilla.CurrentCell = _grilla.Rows[0].Cells[0];
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private void Grilla_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                AbrirFichaSeleccionada();
            }
        }

        private void CargarPadron()
        {
            try
            {
                // Texto vacío + incluirBajas=true trae el padrón completo; el
                // filtro de texto y el de "incluir bajas" se aplican en memoria.
                _padron = _repositorio.Buscar(string.Empty, true);
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo cargar el padrón de asociados.", ex);
                _padron = new List<Asociado>();
            }
        }

        private void AplicarFiltro()
        {
            var texto = _txtBuscar.Text;
            var incluirBajas = _chkIncluirBajas.Checked;
            var servicio = ServicioSeleccionado();

            var filtrados = _padron
                .Where(a => incluirBajas || a.Activo)
                .Where(a => servicio == null
                    || string.Equals(a.Servicio, servicio, StringComparison.OrdinalIgnoreCase))
                .Where(a => AsociadoRepository.Coincide(a, texto))
                .OrderBy(a => a.Apellido)
                .ThenBy(a => a.Nombre)
                .ToList();

            _poblandoGrilla = true;
            try
            {
                _grilla.Rows.Clear();
                foreach (var asociado in filtrados)
                {
                    var indice = _grilla.Rows.Add(
                        asociado.Apellido,
                        asociado.Nombre,
                        asociado.Documento,
                        asociado.Servicio,
                        asociado.Activo ? "Activo" : "Baja");
                    _grilla.Rows[indice].Tag = asociado;
                }
            }
            finally
            {
                _poblandoGrilla = false;
            }

            ActualizarBotones();
        }

        private void ActualizarBotones()
        {
            // Durante la carga la selección cambia una vez por fila y los Tag
            // aún no están asignados: se actualiza una sola vez al terminar.
            if (_poblandoGrilla)
            {
                return;
            }

            var asociado = ObtenerSeleccionado();
            _btnEditar.Enabled = asociado != null;

            if (asociado != null)
            {
                _btnBaja.Text = asociado.Activo ? "&Baja" : "&Reactivar";
                _btnBaja.Enabled = true;
            }
            else
            {
                _btnBaja.Text = "&Baja";
                _btnBaja.Enabled = false;
            }
        }

        /// <summary>
        /// Asociado de la fila seleccionada, o null si no hay selección o la
        /// fila todavía no tiene su Tag asignado.
        /// </summary>
        private Asociado ObtenerSeleccionado()
        {
            var fila = _grilla.CurrentRow;
            return fila == null ? null : fila.Tag as Asociado;
        }

        private void AbrirFichaNueva()
        {
            using (var ficha = new FrmAsociadoDetalle(null))
            {
                if (ficha.ShowDialog(this) == DialogResult.OK)
                {
                    RecargarYFiltrar();
                }
            }
        }

        private void AbrirFichaSeleccionada()
        {
            var asociado = ObtenerSeleccionado();
            if (asociado == null)
            {
                return;
            }

            using (var ficha = new FrmAsociadoDetalle(asociado.Documento))
            {
                if (ficha.ShowDialog(this) == DialogResult.OK)
                {
                    RecargarYFiltrar();
                }
            }
        }

        private void AlternarBajaSeleccionado()
        {
            var asociado = ObtenerSeleccionado();
            if (asociado == null)
            {
                return;
            }

            try
            {
                if (asociado.Activo)
                {
                    var confirmacion = MessageBox.Show(
                        string.Format("¿Confirma dar de baja a {0}?", asociado.NombreCompleto),
                        "Confirmar baja",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirmacion != DialogResult.Yes)
                    {
                        return;
                    }

                    _repositorio.DarDeBaja(asociado.Documento);
                }
                else
                {
                    _repositorio.Reactivar(asociado.Documento);
                }

                RecargarYFiltrar();
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo actualizar el estado del asociado.", ex);
            }
        }

        private void RecargarYFiltrar()
        {
            CargarPadron();
            ActualizarFiltroDeServicios();
            AplicarFiltro();
        }

        /// <summary>
        /// Rearma las opciones del filtro a partir de los servicios presentes en
        /// el padrón, conservando la selección actual si ese servicio sigue
        /// existiendo. Se rearma en cada recarga porque editar un asociado puede
        /// hacer aparecer o desaparecer un servicio de la lista.
        /// </summary>
        private void ActualizarFiltroDeServicios()
        {
            var seleccionPrevia = ServicioSeleccionado();

            var servicios = _padron
                .Select(a => a.Servicio)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase)
                .Cast<object>()
                .ToArray();

            _poblandoFiltroServicio = true;
            try
            {
                _cmbFiltroServicio.Items.Clear();
                _cmbFiltroServicio.Items.Add(TodosLosServicios);
                _cmbFiltroServicio.Items.AddRange(servicios);

                var indice = seleccionPrevia == null
                    ? 0
                    : _cmbFiltroServicio.Items.IndexOf(seleccionPrevia);
                _cmbFiltroServicio.SelectedIndex = indice >= 0 ? indice : 0;
            }
            finally
            {
                _poblandoFiltroServicio = false;
            }
        }

        /// <summary>Servicio elegido, o null cuando está en "todos".</summary>
        private string ServicioSeleccionado()
        {
            return _cmbFiltroServicio.SelectedIndex <= 0
                ? null
                : _cmbFiltroServicio.SelectedItem as string;
        }

        private static void MostrarError(string mensaje, Exception ex)
        {
            MessageBox.Show(
                mensaje + Environment.NewLine + Environment.NewLine + ex.Message,
                "Aviso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
