using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CoopSig.Data;
using CoopSig.Models;
using CoopSig.Utils;

namespace CoopSig.Forms
{
    /// <summary>
    /// Carga y edición de un bono. Replica el flujo del sistema anterior: se
    /// trabaja de a un bono por vez, con los datos de la persona arriba y los
    /// importes abajo.
    ///
    /// Los totales se recalculan mientras se tipea. El 2% de Ley 20337 se
    /// muestra pero no se carga: es calculado y no existe como columna.
    /// </summary>
    public class FrmBonoDetalle : Form
    {
        private readonly BonoRepository _repositorio = new BonoRepository();
        private readonly Asociado _asociado;
        private readonly int? _idBono;
        private Bono _bonoActual;

        private const int MargenIzquierdo = 15;
        private const int AnchoEtiqueta = 130;
        private const int IzquierdaCampo = MargenIzquierdo + AnchoEtiqueta + 5;
        private const int AnchoCampo = 200;
        private const int SaltoFila = 30;

        private int _filaY;

        /// <summary>
        /// Activo mientras se cargan los campos desde un bono existente. Cada
        /// asignación de texto dispara TextChanged, y sin esta guarda el
        /// recálculo corre una vez por campo con la ficha a medio llenar.
        /// </summary>
        private bool _cargandoCampos;

        private ComboBox _cmbMes;
        private TextBox _txtAnio;
        private DateTimePicker _dtpFecha;
        private ComboBox _cmbServicio;
        private TextBox _txtHoras;
        private TextBox _txtValorHora;
        private TextBox _txtComentario;
        private TextBox _txtBasico;
        private TextBox _txtMutual;
        private TextBox _txtAnticipo;
        private TextBox _txtOtrosComentario;
        private TextBox _txtOtros;

        private Label _lblTotalHoras;
        private Label _lblLey20337;
        private Label _lblHaberes;
        private Label _lblDescuentos;
        private Label _lblNeto;

        private Button _btnGuardar;

        /// <param name="asociado">Persona a la que se le carga el bono.</param>
        /// <param name="idBono">Null para un bono nuevo; con valor, abre ese bono existente.</param>
        public FrmBonoDetalle(Asociado asociado, int? idBono)
        {
            _asociado = asociado;
            _idBono = idBono;
            InicializarComponentes();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            CargarServicios();

            if (_idBono.HasValue)
            {
                CargarBonoExistente(_idBono.Value);
            }
            else
            {
                PrepararBonoNuevo();
            }

            Recalcular();
            _cmbMes.Focus();
        }

        private void InicializarComponentes()
        {
            Text = _idBono.HasValue ? "Editar bono" : "Nuevo bono";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            KeyPreview = true;
            Font = new Font("Segoe UI", 9.5F);

            _filaY = 15;

            AgregarTituloDePersona();

            _cmbMes = AgregarComboBox("Período");
            _cmbMes.Items.AddRange(Periodo.Meses().Cast<object>().ToArray());

            // El año va al lado del mes, en la misma fila.
            _txtAnio = new TextBox
            {
                Location = new Point(IzquierdaCampo + AnchoCampo + 10, _cmbMes.Top),
                Size = new Size(70, 25),
                MaxLength = 4
            };
            RegistrarAvanceConEnter(_txtAnio);
            Controls.Add(_txtAnio);

            _dtpFecha = AgregarSelectorDeFecha("Fecha");
            _cmbServicio = AgregarComboBox("Servicio *");

            AgregarSeparador("Haberes");
            _txtHoras = AgregarImporte("Horas");
            _txtValorHora = AgregarImporte("Valor hora");
            _lblTotalHoras = AgregarTotalEnFila("Horas × valor hora", _txtValorHora.Bottom + 6);
            _filaY += SaltoFila;

            _txtComentario = AgregarTextBox("Concepto");
            _txtBasico = AgregarImporte("Básico");

            AgregarSeparador("Descuentos");
            _lblLey20337 = AgregarTotalEnFila("Ley 20337 (2%)", _filaY + 4);
            _filaY += SaltoFila;
            _txtMutual = AgregarImporte("Seguro / Mutual");
            _txtAnticipo = AgregarImporte("Anticipo");
            _txtOtrosComentario = AgregarTextBox("Concepto de otros");
            _txtOtros = AgregarImporte("Otros");

            AgregarSeparador("Totales");
            _lblHaberes = AgregarTotalEnFila("Total de haberes", _filaY + 4);
            _filaY += 26;
            _lblDescuentos = AgregarTotalEnFila("Total descuentos", _filaY + 4);
            _filaY += 26;
            _lblNeto = AgregarTotalEnFila("Neto a cobrar", _filaY + 4);
            _lblNeto.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            _filaY += 40;

            _btnGuardar = new Button
            {
                Text = "&Guardar",
                Location = new Point(MargenIzquierdo, _filaY),
                Size = new Size(120, 34)
            };
            _btnGuardar.Click += (s, e) => Guardar();

            var btnCancelar = new Button
            {
                Text = "&Cancelar",
                Location = new Point(MargenIzquierdo + 130, _filaY),
                Size = new Size(120, 34)
            };
            btnCancelar.Click += (s, e) => Close();

            Controls.Add(_btnGuardar);
            Controls.Add(btnCancelar);

            // Enter avanza de campo, no dispara Guardar (mismo criterio que la
            // ficha de asociado). Al llegar al botón, Enter sí lo activa.
            AcceptButton = null;
            CancelButton = btnCancelar;
        }

        private void AgregarTituloDePersona()
        {
            Controls.Add(new Label
            {
                Text = _asociado.NombreCompleto,
                Location = new Point(MargenIzquierdo, _filaY),
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold)
            });
            _filaY += 26;

            Controls.Add(new Label
            {
                Text = string.Format(
                    "Documento {0}{1}",
                    _asociado.Documento,
                    _asociado.Cuit == null ? string.Empty : "   ·   CUIL " + _asociado.Cuit),
                Location = new Point(MargenIzquierdo, _filaY),
                AutoSize = true,
                ForeColor = Color.DimGray
            });
            _filaY += 32;
        }

        private void AgregarSeparador(string titulo)
        {
            Controls.Add(new Label
            {
                Text = titulo,
                Location = new Point(MargenIzquierdo, _filaY),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 120)
            });
            _filaY += 24;
        }

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
                Size = new Size(AnchoCampo + 80, 25)
            };
            Controls.Add(caja);
            RegistrarAvanceConEnter(caja);
            _filaY += SaltoFila;
            return caja;
        }

        /// <summary>
        /// Campo de importe: alineado a la derecha y con recálculo en vivo,
        /// para que el neto se vea cambiar mientras se carga.
        /// </summary>
        private TextBox AgregarImporte(string etiqueta)
        {
            AgregarEtiquetaDeFila(etiqueta);
            var caja = new TextBox
            {
                Location = new Point(IzquierdaCampo, _filaY),
                Size = new Size(120, 25),
                TextAlign = HorizontalAlignment.Right
            };
            caja.TextChanged += (s, e) =>
            {
                if (!_cargandoCampos)
                {
                    Recalcular();
                }
            };
            Controls.Add(caja);
            RegistrarAvanceConEnter(caja);
            _filaY += SaltoFila;
            return caja;
        }

        private ComboBox AgregarComboBox(string etiqueta)
        {
            AgregarEtiquetaDeFila(etiqueta);
            var combo = new ComboBox
            {
                Location = new Point(IzquierdaCampo, _filaY),
                Size = new Size(AnchoCampo, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(combo);
            RegistrarAvanceConEnter(combo);
            _filaY += SaltoFila;
            return combo;
        }

        private DateTimePicker AgregarSelectorDeFecha(string etiqueta)
        {
            AgregarEtiquetaDeFila(etiqueta);
            var selector = new DateTimePicker
            {
                Location = new Point(IzquierdaCampo, _filaY),
                Size = new Size(140, 25),
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = true,
                Checked = true,
                Value = DateTime.Today
            };
            Controls.Add(selector);
            RegistrarAvanceConEnter(selector);
            _filaY += SaltoFila;
            return selector;
        }

        /// <summary>Renglón de total: etiqueta a la izquierda, importe a la derecha.</summary>
        private Label AgregarTotalEnFila(string etiqueta, int y)
        {
            Controls.Add(new Label
            {
                Text = etiqueta + ":",
                Location = new Point(MargenIzquierdo, y),
                Size = new Size(AnchoEtiqueta + 60, 22),
                TextAlign = ContentAlignment.MiddleLeft
            });

            var valor = new Label
            {
                Location = new Point(IzquierdaCampo + 60, y),
                Size = new Size(160, 22),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            Controls.Add(valor);
            return valor;
        }

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
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void CargarServicios()
        {
            try
            {
                var servicios = new CatalogoRepository()
                    .ObtenerServicios()
                    .Select(s => s.Nombre)
                    .Cast<object>()
                    .ToArray();

                _cmbServicio.Items.Clear();
                _cmbServicio.Items.AddRange(servicios);
            }
            catch (Exception ex)
            {
                MostrarError("No se pudieron cargar los servicios.", ex);
            }
        }

        private void PrepararBonoNuevo()
        {
            _bonoActual = null;

            _cmbMes.SelectedItem = Periodo.NombreDeMes(DateTime.Today.Month);
            _txtAnio.Text = DateTime.Today.Year.ToString(CultureInfo.InvariantCulture);

            // El servicio se propone desde el asociado, pero se puede cambiar:
            // el bono guarda el suyo propio y no siempre coincide.
            SeleccionarOAgregar(_cmbServicio, _asociado.Servicio);
        }

        private void CargarBonoExistente(int id)
        {
            try
            {
                _bonoActual = _repositorio.ObtenerPorId(id);
                if (_bonoActual == null)
                {
                    MessageBox.Show(
                        "El bono ya no existe.", "Aviso",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.Cancel;
                    Close();
                    return;
                }

                _cargandoCampos = true;
                try
                {
                    _cmbMes.SelectedItem = _bonoActual.PeriodoMes;
                    _txtAnio.Text = _bonoActual.PeriodoAnio;

                    if (_bonoActual.Fecha.HasValue)
                    {
                        _dtpFecha.Value = _bonoActual.Fecha.Value;
                        _dtpFecha.Checked = true;
                    }
                    else
                    {
                        _dtpFecha.Checked = false;
                    }

                    SeleccionarOAgregar(_cmbServicio, _bonoActual.Servicio);

                    _txtHoras.Text = Formatear(_bonoActual.Horas);
                    _txtValorHora.Text = Formatear(_bonoActual.ValorHora);
                    _txtBasico.Text = Formatear(_bonoActual.Basico);
                    _txtMutual.Text = Formatear(_bonoActual.Mutual);
                    _txtAnticipo.Text = Formatear(_bonoActual.Anticipo);
                    _txtOtros.Text = Formatear(_bonoActual.Otros);
                    _txtComentario.Text = _bonoActual.Comentario;
                    _txtOtrosComentario.Text = _bonoActual.OtrosComentario;
                }
                finally
                {
                    _cargandoCampos = false;
                }
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo cargar el bono.", ex);
            }
        }

        /// <summary>
        /// Selecciona el valor en la lista, agregándolo si no está. Un servicio
        /// histórico que ya no figure en el catálogo tiene que poder mostrarse
        /// igual, o editar un bono viejo lo cambiaría sin querer.
        /// </summary>
        private static void SeleccionarOAgregar(ComboBox combo, string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return;
            }
            if (!combo.Items.Contains(valor))
            {
                combo.Items.Add(valor);
            }
            combo.SelectedItem = valor;
        }

        private void Recalcular()
        {
            var resultado = CalculoBono.Calcular(
                LeerImporte(_txtHoras),
                LeerImporte(_txtValorHora),
                LeerImporte(_txtBasico),
                LeerImporte(_txtMutual),
                LeerImporte(_txtAnticipo),
                LeerImporte(_txtOtros));

            _lblTotalHoras.Text = Formatear(resultado.TotalHoras);
            _lblLey20337.Text = Formatear(resultado.Ley20337);
            _lblHaberes.Text = Formatear(resultado.Haberes);
            _lblDescuentos.Text = Formatear(resultado.TotalDescuentos);
            _lblNeto.Text = Formatear(resultado.Neto);

            // Un neto negativo casi siempre es un anticipo mayor que el bono.
            // Se marca en rojo pero no se bloquea: decidir qué hacer en ese
            // caso es de la oficina, no del programa.
            _lblNeto.ForeColor = resultado.EsNegativo ? Color.Firebrick : Color.DarkGreen;
        }

        private void Guardar()
        {
            var mensajeError = Validar();
            if (mensajeError != null)
            {
                MessageBox.Show(mensajeError, "Revise los datos",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var mes = _cmbMes.SelectedItem as string;
                var anio = _txtAnio.Text.Trim();

                if (!ConfirmarSiYaHayBonoDelPeriodo(mes, anio))
                {
                    return;
                }

                var bono = ArmarBono(mes, anio);

                if (_bonoActual == null)
                {
                    _repositorio.Insertar(bono);
                }
                else
                {
                    bono.Id = _bonoActual.Id;
                    _repositorio.Actualizar(bono);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo guardar el bono.", ex);
            }
        }

        private Bono ArmarBono(string mes, string anio)
        {
            return new Bono
            {
                // El bono se copia adentro los datos de la persona: no los
                // consulta al reimprimir. Un bono de 2019 tiene que seguir
                // mostrando el servicio que tenía en 2019.
                Documento = _asociado.Documento,
                Nombre = _asociado.Nombre,
                Apellido = _asociado.Apellido,
                Cuil = _asociado.Cuil.HasValue
                    ? _asociado.Cuil.Value.ToString("00", CultureInfo.InvariantCulture)
                    : null,
                Digito = _asociado.Digito.HasValue
                    ? _asociado.Digito.Value.ToString(CultureInfo.InvariantCulture)
                    : null,
                Servicio = _cmbServicio.SelectedItem as string,
                PeriodoMes = mes,
                PeriodoAnio = anio,
                Fecha = _dtpFecha.Checked ? (DateTime?)_dtpFecha.Value.Date : null,
                Horas = LeerImporte(_txtHoras),
                ValorHora = LeerImporte(_txtValorHora),
                Basico = LeerImporte(_txtBasico),
                Mutual = LeerImporte(_txtMutual),
                Anticipo = LeerImporte(_txtAnticipo),
                Otros = LeerImporte(_txtOtros),
                Comentario = TextoONull(_txtComentario.Text),
                OtrosComentario = TextoONull(_txtOtrosComentario.Text)
            };
        }

        private string Validar()
        {
            if (!(_cmbMes.SelectedItem is string))
            {
                return "Elija el mes del período.";
            }

            var anio = _txtAnio.Text.Trim();
            int anioNumerico;
            if (!int.TryParse(anio, NumberStyles.None, CultureInfo.InvariantCulture, out anioNumerico)
                || anio.Length != 4)
            {
                return "El año del período debe tener cuatro dígitos.";
            }

            if (_cmbServicio.SelectedItem == null)
            {
                return "Elija el servicio del bono.";
            }

            if (!EsImporteValido(_txtHoras) || !EsImporteValido(_txtValorHora)
                || !EsImporteValido(_txtBasico) || !EsImporteValido(_txtMutual)
                || !EsImporteValido(_txtAnticipo) || !EsImporteValido(_txtOtros))
            {
                return "Hay un importe que no se entiende. Revise que sean números.";
            }

            if (LeerImporte(_txtHoras) == 0m && LeerImporte(_txtBasico) == 0m)
            {
                return "El bono no tiene horas ni básico: no hay nada que pagar.";
            }

            return null;
        }

        /// <summary>
        /// Avisa si esa persona ya tiene un bono cargado para ese período. La
        /// base admite más de uno y a veces corresponde, así que se pregunta en
        /// lugar de impedirlo.
        /// </summary>
        private bool ConfirmarSiYaHayBonoDelPeriodo(string mes, string anio)
        {
            var existentes = _repositorio
                .ObtenerPorDocumentoYPeriodo(_asociado.Documento, mes, anio)
                .Where(b => _bonoActual == null || b.Id != _bonoActual.Id)
                .ToList();

            if (existentes.Count == 0)
            {
                return true;
            }

            var respuesta = MessageBox.Show(
                string.Format(
                    "{0} ya tiene {1} bono(s) cargado(s) para {2} {3}.{4}{4}¿Grabar igual?",
                    _asociado.NombreCompleto, existentes.Count, mes, anio, Environment.NewLine),
                "Ya hay un bono de ese período",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            return respuesta == DialogResult.Yes;
        }

        /// <summary>
        /// Importe tipeado por la operadora. Un campo vacío o ilegible vale
        /// cero: la validación se encarga aparte de avisar lo ilegible, y acá
        /// devolver cero evita que el recálculo en vivo reviente en cada tecla.
        /// </summary>
        private static decimal LeerImporte(TextBox caja)
        {
            decimal valor;
            return decimal.TryParse(
                caja.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out valor)
                ? valor
                : 0m;
        }

        private static bool EsImporteValido(TextBox caja)
        {
            if (string.IsNullOrWhiteSpace(caja.Text))
            {
                return true;
            }

            decimal valor;
            return decimal.TryParse(
                caja.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out valor)
                && valor >= 0m;
        }

        private static string Formatear(decimal valor)
        {
            return valor.ToString("N2", CultureInfo.CurrentCulture);
        }

        private static string TextoONull(string valor)
        {
            var limpio = (valor ?? string.Empty).Trim();
            return limpio.Length == 0 ? null : limpio;
        }

        private static void MostrarError(string mensaje, Exception ex)
        {
            MessageBox.Show(
                mensaje + Environment.NewLine + Environment.NewLine + ex.Message,
                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
