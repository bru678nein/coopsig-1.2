using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CoopSig.Data;
using CoopSig.Models;
using CoopSig.Utils;

namespace CoopSig.Forms
{
    /// <summary>
    /// Bonos de un asociado. Se busca la persona igual que en la pantalla de
    /// asociados —documento o apellido, filtrando mientras se tipea— y abajo
    /// aparecen sus bonos, del más reciente al más antiguo.
    ///
    /// El padrón se carga en memoria; los bonos NO: son unos 31.000 y se
    /// consultan por documento recién cuando hay una persona elegida.
    /// </summary>
    public class FrmBonos : Form
    {
        private readonly AsociadoRepository _asociados = new AsociadoRepository();
        private readonly BonoRepository _bonos = new BonoRepository();

        private List<Asociado> _padron = new List<Asociado>();
        private Asociado _asociadoElegido;

        private TextBox _txtBuscar;
        private ListBox _lstPersonas;
        private Label _lblPersona;
        private DataGridView _grilla;
        private Button _btnNuevo;
        private Button _btnEditar;
        private Timer _timerDebounce;

        /// <summary>
        /// Misma guarda que en la pantalla de asociados: poblar la grilla
        /// dispara SelectionChanged antes de que las filas tengan su Tag.
        /// </summary>
        private bool _poblandoGrilla;

        private bool _poblandoPersonas;

        public FrmBonos()
        {
            InicializarComponentes();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            CargarPadron();
            FiltrarPersonas();
            _txtBuscar.Focus();
        }

        private void InicializarComponentes()
        {
            Text = "Bonos";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 580);
            MinimumSize = new Size(780, 500);
            Font = new Font("Segoe UI", 9.5F);

            var lblBuscar = new Label
            {
                Text = "Buscar asociado (documento o apellido):",
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

            _lstPersonas = new ListBox
            {
                Location = new Point(12, 65),
                Size = new Size(320, 180),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
            };
            _lstPersonas.SelectedIndexChanged += (s, e) =>
            {
                if (!_poblandoPersonas)
                {
                    ElegirPersona(_lstPersonas.SelectedItem as Asociado);
                }
            };

            _lblPersona = new Label
            {
                Location = new Point(348, 15),
                Size = new Size(540, 46),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Text = "Elija un asociado para ver sus bonos"
            };

            _grilla = new DataGridView
            {
                Location = new Point(348, 65),
                Size = new Size(540, 440),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };
            _grilla.Columns.Add("Periodo", "Período");
            _grilla.Columns.Add("Servicio", "Servicio");
            _grilla.Columns.Add("Horas", "Horas");
            _grilla.Columns.Add("Neto", "Neto a cobrar");
            _grilla.Columns.Add("Concepto", "Concepto");
            _grilla.Columns["Horas"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grilla.Columns["Neto"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grilla.CellDoubleClick += (s, e) => AbrirBonoSeleccionado();
            _grilla.SelectionChanged += (s, e) => ActualizarBotones();
            _grilla.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    AbrirBonoSeleccionado();
                }
            };

            _btnNuevo = new Button
            {
                Text = "&Nuevo bono",
                Location = new Point(348, 515),
                Size = new Size(140, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnNuevo.Click += (s, e) => AbrirBonoNuevo();

            _btnEditar = new Button
            {
                Text = "&Editar",
                Location = new Point(498, 515),
                Size = new Size(120, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnEditar.Click += (s, e) => AbrirBonoSeleccionado();

            Controls.Add(lblBuscar);
            Controls.Add(_txtBuscar);
            Controls.Add(_lstPersonas);
            Controls.Add(_lblPersona);
            Controls.Add(_grilla);
            Controls.Add(_btnNuevo);
            Controls.Add(_btnEditar);

            _timerDebounce = new Timer { Interval = 300 };
            _timerDebounce.Tick += (s, e) =>
            {
                _timerDebounce.Stop();
                FiltrarPersonas();
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
                // Sin mouse: Enter baja a la lista de personas.
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (_lstPersonas.Items.Count > 0)
                {
                    _lstPersonas.Focus();
                    _lstPersonas.SelectedIndex = 0;
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private void CargarPadron()
        {
            try
            {
                // Se incluyen las bajas: un asociado dado de baja no recibe
                // bonos nuevos, pero sus bonos históricos se consultan igual.
                _padron = _asociados.Buscar(string.Empty, true);
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo cargar el padrón de asociados.", ex);
                _padron = new List<Asociado>();
            }
        }

        private void FiltrarPersonas()
        {
            var texto = _txtBuscar.Text;

            var encontrados = _padron
                .Where(a => AsociadoRepository.Coincide(a, texto))
                .OrderBy(a => a.Apellido)
                .ThenBy(a => a.Nombre)
                .Take(200)
                .Cast<object>()
                .ToArray();

            _poblandoPersonas = true;
            try
            {
                _lstPersonas.Items.Clear();
                _lstPersonas.DisplayMember = "NombreCompleto";
                _lstPersonas.Items.AddRange(encontrados);
            }
            finally
            {
                _poblandoPersonas = false;
            }
        }

        private void ElegirPersona(Asociado asociado)
        {
            _asociadoElegido = asociado;

            if (asociado == null)
            {
                _lblPersona.Text = "Elija un asociado para ver sus bonos";
                _grilla.Rows.Clear();
                ActualizarBotones();
                return;
            }

            _lblPersona.Text = string.Format(
                "{0}   ·   Documento {1}{2}",
                asociado.NombreCompleto,
                asociado.Documento,
                asociado.Activo ? string.Empty : "   ·   DADO DE BAJA");
            _lblPersona.ForeColor = asociado.Activo ? Color.Black : Color.Firebrick;

            CargarBonos();
        }

        private void CargarBonos()
        {
            if (_asociadoElegido == null)
            {
                return;
            }

            try
            {
                var bonos = _bonos.ObtenerPorDocumento(_asociadoElegido.Documento);

                _poblandoGrilla = true;
                try
                {
                    _grilla.Rows.Clear();
                    foreach (var bono in bonos)
                    {
                        var indice = _grilla.Rows.Add(
                            bono.PeriodoDescripto,
                            bono.Servicio,
                            bono.Horas.ToString("N2"),
                            bono.Calcular().Neto.ToString("N2"),
                            bono.Comentario);
                        _grilla.Rows[indice].Tag = bono;
                    }
                }
                finally
                {
                    _poblandoGrilla = false;
                }
            }
            catch (Exception ex)
            {
                MostrarError("No se pudieron cargar los bonos del asociado.", ex);
            }

            ActualizarBotones();
        }

        private void ActualizarBotones()
        {
            if (_poblandoGrilla)
            {
                return;
            }

            // HU: un asociado dado de baja no recibe bonos nuevos, pero sus
            // bonos históricos se pueden consultar y corregir.
            _btnNuevo.Enabled = _asociadoElegido != null && _asociadoElegido.Activo;
            _btnEditar.Enabled = ObtenerBonoSeleccionado() != null;
        }

        private Bono ObtenerBonoSeleccionado()
        {
            var fila = _grilla.CurrentRow;
            return fila == null ? null : fila.Tag as Bono;
        }

        private void AbrirBonoNuevo()
        {
            if (_asociadoElegido == null)
            {
                return;
            }

            if (!_asociadoElegido.Activo)
            {
                MessageBox.Show(
                    "El asociado está dado de baja: no se le pueden cargar bonos nuevos.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var ficha = new FrmBonoDetalle(_asociadoElegido, null))
            {
                if (ficha.ShowDialog(this) == DialogResult.OK)
                {
                    CargarBonos();
                }
            }
        }

        private void AbrirBonoSeleccionado()
        {
            var bono = ObtenerBonoSeleccionado();
            if (bono == null || _asociadoElegido == null)
            {
                return;
            }

            using (var ficha = new FrmBonoDetalle(_asociadoElegido, bono.Id))
            {
                if (ficha.ShowDialog(this) == DialogResult.OK)
                {
                    CargarBonos();
                }
            }
        }

        private static void MostrarError(string mensaje, Exception ex)
        {
            MessageBox.Show(
                mensaje + Environment.NewLine + Environment.NewLine + ex.Message,
                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
