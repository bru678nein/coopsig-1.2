using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CoopSig.Data;
using CoopSig.Models;
using CoopSig.Utils;

namespace CoopSig.Forms
{
    /// <summary>
    /// Alta y edición de un asociado (HU-2, HU-3). Enter avanza al campo
    /// siguiente, Escape cancela sin guardar (HU-5). El Documento es la
    /// clave de hecho: una vez creado el registro no se permite editarlo,
    /// porque las operaciones de baja/reactivación lo usan como clave.
    /// </summary>
    public class FrmAsociadoDetalle : Form
    {
        private readonly AsociadoRepository _repositorio = new AsociadoRepository();
        private readonly Validaciones _validaciones;
        private readonly long? _documentoOriginal;
        private Asociado _asociadoActual;

        private TextBox _txtApellido;
        private TextBox _txtNombre;
        private TextBox _txtDocumento;
        private ComboBox _cmbServicio;
        private ComboBox _cmbCargo;
        private TextBox _txtCuil;
        private TextBox _txtDigito;
        private DateTimePicker _dtpFechaIngreso;
        private Label _lblEstado;
        private Button _btnGuardar;
        private Button _btnBaja;
        private Button _btnCancelar;

        /// <param name="documento">
        /// Null para alta nueva. Con valor, abre en modo edición cargando ese
        /// asociado existente.
        /// </param>
        public FrmAsociadoDetalle(long? documento)
        {
            _documentoOriginal = documento;
            _validaciones = new Validaciones(_repositorio);
            InicializarComponentes();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            CargarCatalogos();

            if (_documentoOriginal.HasValue)
            {
                CargarAsociadoExistente(_documentoOriginal.Value);
            }
            else
            {
                _asociadoActual = new Asociado { FechaIngreso = DateTime.Today };
                ActualizarEstadoVisual();
            }

            _txtApellido.Focus();
        }

        private void InicializarComponentes()
        {
            Text = _documentoOriginal.HasValue ? "Editar asociado" : "Nuevo asociado";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            KeyPreview = true;
            Font = new Font("Segoe UI", 9.5F);

            Controls.Add(NuevaEtiqueta("Apellido *", 15));
            _txtApellido = NuevoTextBox(35, 370);

            Controls.Add(NuevaEtiqueta("Nombre *", 75));
            _txtNombre = NuevoTextBox(95, 370);

            Controls.Add(NuevaEtiqueta("Documento *", 135));
            _txtDocumento = NuevoTextBox(155, 370);
            _txtDocumento.MaxLength = 9;

            Controls.Add(NuevaEtiqueta("Servicio *", 195));
            _cmbServicio = NuevoComboBox(215, 370);

            Controls.Add(NuevaEtiqueta("Cargo", 255));
            _cmbCargo = NuevoComboBox(275, 370);

            Controls.Add(NuevaEtiqueta("Identificador fiscal (opcional): prefijo / dígito", 315));
            _txtCuil = new TextBox { Location = new Point(15, 335), Size = new Size(80, 25), MaxLength = 2 };
            _txtDigito = new TextBox { Location = new Point(105, 335), Size = new Size(60, 25), MaxLength = 1 };
            Controls.Add(_txtCuil);
            Controls.Add(_txtDigito);
            RegistrarAvanceConEnter(_txtCuil);
            RegistrarAvanceConEnter(_txtDigito);

            Controls.Add(NuevaEtiqueta("Fecha de ingreso", 375));
            _dtpFechaIngreso = new DateTimePicker
            {
                Location = new Point(15, 395),
                Size = new Size(200, 25),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            Controls.Add(_dtpFechaIngreso);
            RegistrarAvanceConEnter(_dtpFechaIngreso);

            _lblEstado = new Label
            {
                Location = new Point(230, 398),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            Controls.Add(_lblEstado);

            const int yBotones = 445;

            _btnGuardar = new Button
            {
                Text = "&Guardar",
                Location = new Point(15, yBotones),
                Size = new Size(110, 32)
            };
            _btnGuardar.Click += (s, e) => Guardar();

            _btnBaja = new Button
            {
                Text = "&Baja",
                Location = new Point(135, yBotones),
                Size = new Size(110, 32),
                Enabled = _documentoOriginal.HasValue
            };
            _btnBaja.Click += (s, e) => AlternarBaja();

            _btnCancelar = new Button
            {
                Text = "&Cancelar",
                Location = new Point(255, yBotones),
                Size = new Size(110, 32)
            };
            _btnCancelar.Click += (s, e) => Close();

            Controls.Add(_btnGuardar);
            Controls.Add(_btnBaja);
            Controls.Add(_btnCancelar);

            // Enter no dispara Guardar desde cualquier campo: avanza el foco
            // (HU-5). Al llegar al botón Guardar, Enter sí lo activa (comportamiento
            // estándar de un Button enfocado).
            AcceptButton = null;
            CancelButton = _btnCancelar;
        }

        private static Label NuevaEtiqueta(string texto, int y)
        {
            return new Label { Text = texto, Location = new Point(15, y), AutoSize = true };
        }

        private TextBox NuevoTextBox(int y, int ancho)
        {
            var caja = new TextBox { Location = new Point(15, y), Size = new Size(ancho, 25) };
            Controls.Add(caja);
            RegistrarAvanceConEnter(caja);
            return caja;
        }

        private ComboBox NuevoComboBox(int y, int ancho)
        {
            var combo = new ComboBox
            {
                Location = new Point(15, y),
                Size = new Size(ancho, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(combo);
            RegistrarAvanceConEnter(combo);
            return combo;
        }

        /// <summary>HU-5: Enter avanza al campo siguiente en el orden visual.</summary>
        private void RegistrarAvanceConEnter(Control control)
        {
            control.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    SelectNextControl(control, true, true, true, true);
                }
            };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                // HU-5: Escape cierra la ficha sin guardar.
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void CargarCatalogos()
        {
            try
            {
                var catalogo = new CatalogoRepository();

                var servicios = catalogo.ObtenerServicios().Select(s => s.Nombre).ToArray();
                _cmbServicio.Items.Clear();
                _cmbServicio.Items.AddRange(servicios);

                var cargos = catalogo.ObtenerCargos().Select(c => c.Nombre).ToArray();
                _cmbCargo.Items.Clear();
                _cmbCargo.Items.Add(string.Empty);
                _cmbCargo.Items.AddRange(cargos);
                _cmbCargo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MostrarError("No se pudieron cargar los catálogos de Servicio y Cargo.", ex);
            }
        }

        private void CargarAsociadoExistente(long documento)
        {
            try
            {
                _asociadoActual = _repositorio.ObtenerPorDocumento(documento);
                if (_asociadoActual == null)
                {
                    MessageBox.Show(
                        "El asociado ya no existe en el padrón.",
                        "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.Cancel;
                    Close();
                    return;
                }

                _txtApellido.Text = _asociadoActual.Apellido;
                _txtNombre.Text = _asociadoActual.Nombre;
                _txtDocumento.Text = _asociadoActual.Documento.ToString();
                _txtDocumento.ReadOnly = true;
                _txtDocumento.TabStop = false;

                if (!string.IsNullOrEmpty(_asociadoActual.Servicio) && !_cmbServicio.Items.Contains(_asociadoActual.Servicio))
                {
                    _cmbServicio.Items.Add(_asociadoActual.Servicio);
                }
                _cmbServicio.SelectedItem = _asociadoActual.Servicio;

                if (!string.IsNullOrEmpty(_asociadoActual.Cargo) && !_cmbCargo.Items.Contains(_asociadoActual.Cargo))
                {
                    _cmbCargo.Items.Add(_asociadoActual.Cargo);
                }
                _cmbCargo.SelectedItem = string.IsNullOrEmpty(_asociadoActual.Cargo) ? string.Empty : _asociadoActual.Cargo;

                _txtCuil.Text = _asociadoActual.Cuil.HasValue ? _asociadoActual.Cuil.Value.ToString("00") : string.Empty;
                _txtDigito.Text = _asociadoActual.Digito.HasValue ? _asociadoActual.Digito.Value.ToString() : string.Empty;

                if (_asociadoActual.FechaIngreso.HasValue)
                {
                    _dtpFechaIngreso.Value = _asociadoActual.FechaIngreso.Value;
                }

                ActualizarEstadoVisual();
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo cargar la ficha del asociado.", ex);
            }
        }

        private void ActualizarEstadoVisual()
        {
            var activo = _asociadoActual == null || _asociadoActual.Activo;
            _lblEstado.Text = "Estado: " + (activo
                ? "Activo"
                : "Baja (" + _asociadoActual.FechaBaja.Value.ToShortDateString() + ")");
            _lblEstado.ForeColor = activo ? Color.DarkGreen : Color.DarkRed;
            _btnBaja.Text = activo ? "&Baja" : "&Reactivar";
        }

        private void Guardar()
        {
            long documento;
            int? cuil;
            int? digito;
            var mensajeError = ValidarCampos(out documento, out cuil, out digito);
            if (mensajeError != null)
            {
                MessageBox.Show(mensajeError, "Revise los datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var esNuevo = !_documentoOriginal.HasValue;

                if (esNuevo)
                {
                    var existente = _validaciones.BuscarDuplicadoDocumento(documento);
                    if (existente != null)
                    {
                        MessageBox.Show(
                            string.Format(
                                "Ya existe un asociado con el documento {0}: {1}.",
                                documento, existente.NombreCompleto),
                            "Documento duplicado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }

                var cargoSeleccionado = _cmbCargo.SelectedItem as string;

                var asociado = new Asociado
                {
                    Documento = documento,
                    Apellido = _txtApellido.Text.Trim(),
                    Nombre = _txtNombre.Text.Trim(),
                    Servicio = _cmbServicio.SelectedItem as string,
                    Cargo = string.IsNullOrEmpty(cargoSeleccionado) ? null : cargoSeleccionado,
                    Cuil = cuil,
                    Digito = digito,
                    FechaIngreso = _dtpFechaIngreso.Value.Date,
                    FechaBaja = esNuevo ? null : _asociadoActual.FechaBaja
                };

                if (esNuevo)
                {
                    _repositorio.Insertar(asociado);
                }
                else
                {
                    _repositorio.Actualizar(asociado);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo guardar el asociado.", ex);
            }
        }

        private string ValidarCampos(out long documento, out int? cuil, out int? digito)
        {
            documento = 0;
            cuil = null;
            digito = null;

            if (!Validaciones.EsCampoObligatorioCompleto(_txtApellido.Text))
            {
                return "El apellido es obligatorio.";
            }
            if (!Validaciones.EsCampoObligatorioCompleto(_txtNombre.Text))
            {
                return "El nombre es obligatorio.";
            }
            if (!long.TryParse(_txtDocumento.Text.Trim(), out documento) || documento <= 0)
            {
                return "El documento debe ser un número válido.";
            }
            if (_cmbServicio.SelectedItem == null)
            {
                return "Debe seleccionar un servicio de la lista.";
            }

            var textoCuil = _txtCuil.Text.Trim();
            var textoDigito = _txtDigito.Text.Trim();

            if (textoCuil.Length > 0)
            {
                int valorCuil;
                if (!int.TryParse(textoCuil, out valorCuil))
                {
                    return "El prefijo del identificador fiscal debe ser numérico.";
                }
                cuil = valorCuil;
            }

            if (textoDigito.Length > 0)
            {
                int valorDigito;
                if (!int.TryParse(textoDigito, out valorDigito))
                {
                    return "El dígito verificador del identificador fiscal debe ser numérico.";
                }
                digito = valorDigito;
            }

            var resultadoCuil = Validaciones.ValidarCuil(documento, cuil, digito);
            if (!resultadoCuil.EsValido)
            {
                return resultadoCuil.Mensaje;
            }

            return null;
        }

        private void AlternarBaja()
        {
            if (!_documentoOriginal.HasValue || _asociadoActual == null)
            {
                return;
            }

            try
            {
                if (_asociadoActual.Activo)
                {
                    var confirmacion = MessageBox.Show(
                        string.Format("¿Confirma dar de baja a {0}?", _asociadoActual.NombreCompleto),
                        "Confirmar baja",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirmacion != DialogResult.Yes)
                    {
                        return;
                    }

                    _repositorio.DarDeBaja(_asociadoActual.Documento);
                    _asociadoActual.FechaBaja = DateTime.Today;
                }
                else
                {
                    _repositorio.Reactivar(_asociadoActual.Documento);
                    _asociadoActual.FechaBaja = null;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo actualizar el estado del asociado.", ex);
            }
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
