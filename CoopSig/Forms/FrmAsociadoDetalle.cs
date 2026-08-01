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

        private const int MargenIzquierdo = 15;
        private const int AnchoEtiqueta = 130;
        private const int IzquierdaCampo = MargenIzquierdo + AnchoEtiqueta + 5;
        private const int AnchoCampo = 270;
        private const int AnchoCampoCorto = 150;
        private const int SaltoFila = 32;

        /// <summary>
        /// Cursor vertical del armado de la pantalla. Cada campo se agrega bajo
        /// el anterior y lo adelanta, en lugar de repartir coordenadas fijas
        /// que hay que recalcular a mano cada vez que se suma un campo.
        /// </summary>
        private int _filaY;

        private TextBox _txtApellido;
        private TextBox _txtNombre;
        private TextBox _txtDocumento;
        private DateTimePicker _dtpFechaNacimiento;
        private ComboBox _cmbSexo;
        private ComboBox _cmbEstadoCivil;
        private TextBox _txtDireccion;
        private TextBox _txtTelefono;
        private ComboBox _cmbServicio;
        private ComboBox _cmbCargo;
        private DateTimePicker _dtpFechaIngreso;
        private TextBox _txtNotas;
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
                _dtpFechaIngreso.Checked = true;
                ActualizarEstadoVisual();
            }

            _txtApellido.Focus();
        }

        private void InicializarComponentes()
        {
            Text = _documentoOriginal.HasValue ? "Editar asociado" : "Nuevo asociado";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(450, 577);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            KeyPreview = true;
            Font = new Font("Segoe UI", 9.5F);

            _filaY = 15;

            _txtApellido = AgregarTextBox("Apellido *");
            _txtNombre = AgregarTextBox("Nombre *");

            // Un solo campo acepta las dos formas de identificar a la persona:
            // el CUIT se parte en prefijo, documento y verificador al guardar
            // (Validaciones.ParsearDocumentoOCuit). El documento sigue siendo la
            // clave, así que cargar el CUIT no genera un registro duplicado.
            _txtDocumento = AgregarTextBox("CUIT o DNI *");
            _txtDocumento.MaxLength = 13;

            _dtpFechaNacimiento = AgregarSelectorDeFecha("Fecha de nacimiento");
            _cmbSexo = AgregarComboBox("Sexo", ComboBoxStyle.DropDown);
            _cmbEstadoCivil = AgregarComboBox("Estado civil", ComboBoxStyle.DropDown);
            _txtDireccion = AgregarTextBox("Dirección");
            _txtTelefono = AgregarTextBox("Teléfono");
            _cmbServicio = AgregarComboBox("Servicio *", ComboBoxStyle.DropDownList);
            _cmbCargo = AgregarComboBox("Cargo", ComboBoxStyle.DropDownList);
            _dtpFechaIngreso = AgregarSelectorDeFecha("Fecha de ingreso");

            _lblEstado = new Label
            {
                Location = new Point(IzquierdaCampo, _filaY + 4),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            Controls.Add(_lblEstado);
            _filaY += SaltoFila;

            Controls.Add(new Label
            {
                Text = "Notas",
                Location = new Point(MargenIzquierdo, _filaY),
                AutoSize = true
            });
            _filaY += 20;

            // Sin RegistrarAvanceConEnter a propósito: en un campo de varias
            // líneas Enter tiene que insertar un salto, no saltar de control.
            _txtNotas = new TextBox
            {
                Location = new Point(MargenIzquierdo, _filaY),
                Size = new Size(AnchoEtiqueta + 5 + AnchoCampo, 90),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                AcceptsReturn = true
            };
            Controls.Add(_txtNotas);
            _filaY += 100;

            var yBotones = _filaY + 10;

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

        /// <summary>Etiqueta a la izquierda del campo, en la misma fila.</summary>
        private void AgregarEtiquetaDeFila(string texto)
        {
            Controls.Add(new Label
            {
                Text = texto,
                Location = new Point(MargenIzquierdo, _filaY + 4),
                Size = new Size(AnchoEtiqueta, 20)
            });
        }

        private TextBox AgregarTextBox(string etiqueta)
        {
            AgregarEtiquetaDeFila(etiqueta);
            var caja = new TextBox
            {
                Location = new Point(IzquierdaCampo, _filaY),
                Size = new Size(AnchoCampo, 25)
            };
            Controls.Add(caja);
            RegistrarAvanceConEnter(caja);
            _filaY += SaltoFila;
            return caja;
        }

        private ComboBox AgregarComboBox(string etiqueta, ComboBoxStyle estilo)
        {
            AgregarEtiquetaDeFila(etiqueta);
            var combo = new ComboBox
            {
                Location = new Point(IzquierdaCampo, _filaY),
                Size = new Size(AnchoCampo, 25),
                DropDownStyle = estilo
            };
            Controls.Add(combo);
            RegistrarAvanceConEnter(combo);
            _filaY += SaltoFila;
            return combo;
        }

        /// <summary>
        /// Selector de fecha opcional. ShowCheckBox permite dejarlo sin marcar,
        /// que es como se representa "no hay dato": sin eso el control siempre
        /// devuelve una fecha y terminaría inventando una que nadie cargó.
        /// </summary>
        private DateTimePicker AgregarSelectorDeFecha(string etiqueta)
        {
            AgregarEtiquetaDeFila(etiqueta);
            var selector = new DateTimePicker
            {
                Location = new Point(IzquierdaCampo, _filaY),
                Size = new Size(AnchoCampoCorto, 25),
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = true,
                Checked = false,
                Value = DateTime.Today
            };
            Controls.Add(selector);
            RegistrarAvanceConEnter(selector);
            _filaY += SaltoFila;
            return selector;
        }

        private static string TextoONull(string valor)
        {
            var limpio = (valor ?? string.Empty).Trim();
            return limpio.Length == 0 ? null : limpio;
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

                // Sexo y estado civil se dejan editables (DropDown) porque no
                // tienen tabla de catálogo: la lista sale de los datos y podría
                // venir vacía, y una lista cerrada y vacía no deja cargar nada.
                var sexos = catalogo.ObtenerSexos().ToArray();
                _cmbSexo.Items.Clear();
                _cmbSexo.Items.AddRange(sexos);
                _cmbSexo.Text = string.Empty;

                var estadosCiviles = catalogo.ObtenerEstadosCiviles().ToArray();
                _cmbEstadoCivil.Items.Clear();
                _cmbEstadoCivil.Items.AddRange(estadosCiviles);
                _cmbEstadoCivil.Text = string.Empty;
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

                // Se muestra el CUIT completo cuando está cargado; si solo hay
                // documento, se muestra el documento. El campo es de solo
                // lectura: cambiarlo sería cambiar la clave del registro.
                _txtDocumento.Text = _asociadoActual.Cuit ?? _asociadoActual.Documento.ToString();
                _txtDocumento.ReadOnly = true;
                _txtDocumento.TabStop = false;

                _cmbSexo.Text = _asociadoActual.Sexo ?? string.Empty;
                _cmbEstadoCivil.Text = _asociadoActual.EstadoCivil ?? string.Empty;
                _txtDireccion.Text = _asociadoActual.Direccion;
                _txtTelefono.Text = _asociadoActual.Telefono;
                _txtNotas.Text = _asociadoActual.Notas;

                if (_asociadoActual.FechaNacimiento.HasValue)
                {
                    _dtpFechaNacimiento.Value = _asociadoActual.FechaNacimiento.Value;
                    _dtpFechaNacimiento.Checked = true;
                }

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

                if (_asociadoActual.FechaIngreso.HasValue)
                {
                    _dtpFechaIngreso.Value = _asociadoActual.FechaIngreso.Value;
                    _dtpFechaIngreso.Checked = true;
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
                    FechaNacimiento = FechaSeleccionada(_dtpFechaNacimiento),
                    Sexo = TextoONull(_cmbSexo.Text),
                    EstadoCivil = TextoONull(_cmbEstadoCivil.Text),
                    Direccion = TextoONull(_txtDireccion.Text),
                    Telefono = TextoONull(_txtTelefono.Text),
                    Notas = TextoONull(_txtNotas.Text),
                    FechaIngreso = FechaSeleccionada(_dtpFechaIngreso),
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

            var resultadoDocumento = Validaciones.ParsearDocumentoOCuit(
                _txtDocumento.Text, out documento, out cuil, out digito);
            if (!resultadoDocumento.EsValido)
            {
                return resultadoDocumento.Mensaje;
            }

            if (_cmbServicio.SelectedItem == null)
            {
                return "Debe seleccionar un servicio de la lista.";
            }

            if (_documentoOriginal.HasValue && _asociadoActual != null)
            {
                // En edición el campo es de solo lectura, así que se conserva lo
                // que ya estaba guardado en vez de reinterpretar el texto. Y no
                // se revalida: un identificador histórico que no pase el módulo
                // 11 dejaría la ficha imposible de guardar por un motivo que no
                // tiene nada que ver con lo que el usuario vino a cambiar.
                documento = _asociadoActual.Documento;
                cuil = _asociadoActual.Cuil;
                digito = _asociadoActual.Digito;
                return null;
            }

            var resultadoCuil = Validaciones.ValidarCuil(documento, cuil, digito);
            if (!resultadoCuil.EsValido)
            {
                return resultadoCuil.Mensaje;
            }

            return null;
        }

        /// <summary>Fecha del selector, o null si está sin marcar.</summary>
        private static DateTime? FechaSeleccionada(DateTimePicker selector)
        {
            return selector.Checked ? (DateTime?)selector.Value.Date : null;
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
