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
    /// trabaja de a un bono por vez, con los datos de la persona arriba,
    /// haberes a la izquierda y descuentos a la derecha.
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

        // El formulario se arma en dos columnas. Los totales y los botones van
        // abajo, cruzando las dos. Las alturas se calculan a partir del punto
        // más bajo que alcanzó cualquiera de las columnas, para que agregar un
        // campo no empuje los botones fuera de la ventana.
        private const int ColumnaIzquierda = 15;
        private const int EtiquetaIzquierda = 115;
        private const int CampoIzquierdo = ColumnaIzquierda + EtiquetaIzquierda + 10;

        private const int ColumnaDerecha = 380;
        private const int EtiquetaDerecha = 130;
        private const int CampoDerecho = ColumnaDerecha + EtiquetaDerecha + 10;

        private const int AnchoCampo = 170;
        private const int AnchoImporte = 130;
        private const int SaltoFila = 30;

        private int _columnaX;
        private int _anchoEtiqueta;
        private int _campoX;
        private int _filaY;

        /// <summary>
        /// Activo mientras se cargan los campos desde un bono existente. Cada
        /// asignación de texto dispara TextChanged, y sin esta guarda el
        /// recálculo corre una vez por campo con la ficha a medio llenar.
        /// </summary>
        private bool _cargandoCampos;

        private ComboBox _cmbMes;
        private TextBox _txtAnio;
        private TextBox _txtFecha;
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
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            KeyPreview = true;
            Font = new Font("Segoe UI", 9.5F);

            var yEncabezado = AgregarTituloDePersona();

            // -- Columna izquierda: período, servicio y haberes --
            IniciarColumna(ColumnaIzquierda, EtiquetaIzquierda, CampoIzquierdo, yEncabezado);

            _cmbMes = AgregarComboBox("Período");
            _txtAnio = new TextBox
            {
                Location = new Point(_cmbMes.Right + 10, _cmbMes.Top),
                Size = new Size(60, 25),
                MaxLength = 4
            };
            RegistrarAvanceConEnter(_txtAnio);
            Controls.Add(_txtAnio);

            _txtFecha = AgregarTextBox("Fecha");
            _txtFecha.Size = new Size(110, 25);
            _txtFecha.MaxLength = 10;
            Controls.Add(new Label
            {
                Text = "dd/mm/aaaa",
                Location = new Point(_txtFecha.Right + 8, _txtFecha.Top + 4),
                AutoSize = true,
                ForeColor = Color.DimGray
            });

            _cmbServicio = AgregarComboBox("Servicio *");

            AgregarSeparador("Haberes");
            _txtHoras = AgregarImporte("Horas");
            _txtValorHora = AgregarImporte("Valor hora");
            _lblTotalHoras = AgregarTotal("Horas × valor hora");
            _txtComentario = AgregarTextBox("Concepto");
            _txtBasico = AgregarImporte("Básico");

            var finIzquierda = _filaY;

            // -- Columna derecha: descuentos --
            IniciarColumna(ColumnaDerecha, EtiquetaDerecha, CampoDerecho, yEncabezado);

            AgregarSeparador("Descuentos");
            _lblLey20337 = AgregarTotal("Ley 20337 (2%)");
            _txtMutual = AgregarImporte("Seguro / Mutual");
            _txtAnticipo = AgregarImporte("Anticipo");
            _txtOtrosComentario = AgregarTextBox("Concepto de otros");
            _txtOtros = AgregarImporte("Otros");

            var finDerecha = _filaY;

            // -- Totales y botones, cruzando las dos columnas --
            IniciarColumna(ColumnaIzquierda, EtiquetaIzquierda, CampoIzquierdo,
                Math.Max(finIzquierda, finDerecha) + 10);

            AgregarSeparador("Totales");
            _lblHaberes = AgregarTotal("Total de haberes");
            _lblDescuentos = AgregarTotal("Total descuentos");
            _lblNeto = AgregarTotal("Neto a cobrar");
            _lblNeto.Font = new Font("Segoe UI", 13F, FontStyle.Bold);

            _filaY += 14;

            var btnGuardar = new Button
            {
                Text = "&Guardar",
                Location = new Point(ColumnaIzquierda, _filaY),
                Size = new Size(130, 36),
                Font = new Font("Segoe UI", 10F)
            };
            btnGuardar.Click += (s, e) => Guardar();

            var btnCancelar = new Button
            {
                Text = "&Cancelar",
                Location = new Point(ColumnaIzquierda + 140, _filaY),
                Size = new Size(130, 36),
                Font = new Font("Segoe UI", 10F)
            };
            btnCancelar.Click += (s, e) => Close();

            Controls.Add(btnGuardar);
            Controls.Add(btnCancelar);

            // La ventana se dimensiona DESPUÉS de armar todo, a partir de dónde
            // terminaron los controles. Fijar el alto a mano fue lo que dejó los
            // botones y el neto fuera de la pantalla.
            ClientSize = new Size(700, btnGuardar.Bottom + 15);

            // Enter avanza de campo, no dispara Guardar (mismo criterio que la
            // ficha de asociado). Al llegar al botón, Enter sí lo activa.
            AcceptButton = null;
            CancelButton = btnCancelar;
        }

        private void IniciarColumna(int columnaX, int anchoEtiqueta, int campoX, int y)
        {
            _columnaX = columnaX;
            _anchoEtiqueta = anchoEtiqueta;
            _campoX = campoX;
            _filaY = y;
        }

        private int AgregarTituloDePersona()
        {
            Controls.Add(new Label
            {
                Text = _asociado.NombreCompleto,
                Location = new Point(ColumnaIzquierda, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold)
            });

            Controls.Add(new Label
            {
                Text = string.Format(
                    "Documento {0}{1}",
                    _asociado.Documento,
                    _asociado.Cuit == null ? string.Empty : "   ·   CUIL " + _asociado.Cuit),
                Location = new Point(ColumnaIzquierda, 41),
                AutoSize = true,
                ForeColor = Color.DimGray
            });

            return 73;
        }

        private void AgregarSeparador(string titulo)
        {
            Controls.Add(new Label
            {
                Text = titulo,
                Location = new Point(_columnaX, _filaY),
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
                Location = new Point(_columnaX, _filaY + 4),
                Size = new Size(_anchoEtiqueta, 20)
            });
        }

        private TextBox AgregarTextBox(string etiqueta)
        {
            AgregarEtiquetaDeFila(etiqueta);
            var caja = new TextBox
            {
                Location = new Point(_campoX, _filaY),
                Size = new Size(AnchoCampo, 25)
            };
            Controls.Add(caja);
            RegistrarAvanceConEnter(caja);
            _filaY += SaltoFila;
            return caja;
        }

        /// <summary>
        /// Campo de importe. El texto se escribe alineado a la izquierda, como
        /// cualquier otro campo: alinearlo a la derecha hace que el cursor
        /// arranque pegado al borde y desconcierta al tipear.
        /// </summary>
        private TextBox AgregarImporte(string etiqueta)
        {
            AgregarEtiquetaDeFila(etiqueta);
            var caja = new TextBox
            {
                Location = new Point(_campoX, _filaY),
                Size = new Size(AnchoImporte, 25)
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
                Location = new Point(_campoX, _filaY),
                Size = new Size(AnchoCampo, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(combo);
            RegistrarAvanceConEnter(combo);
            _filaY += SaltoFila;
            return combo;
        }

        /// <summary>Renglón de total: etiqueta a la izquierda, importe a la derecha.</summary>
        private Label AgregarTotal(string etiqueta)
        {
            Controls.Add(new Label
            {
                Text = etiqueta + ":",
                Location = new Point(_columnaX, _filaY + 4),
                Size = new Size(_anchoEtiqueta + 40, 22)
            });

            var valor = new Label
            {
                Location = new Point(_campoX, _filaY),
                Size = new Size(AnchoImporte + 40, 24),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            Controls.Add(valor);
            _filaY += 28;
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
            _txtFecha.Text = DateTime.Today.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);

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
                    _txtFecha.Text = _bonoActual.Fecha.HasValue
                        ? _bonoActual.Fecha.Value.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture)
                        : string.Empty;

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
                Fecha = LeerFecha(),
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

            if (!string.IsNullOrWhiteSpace(_txtFecha.Text) && !LeerFecha().HasValue)
            {
                return "La fecha no se entiende. Escribala como dd/mm/aaaa, por ejemplo 05/03/2026.";
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
        /// Fecha tipeada a mano. Vacía es válida: no todos los bonos la tienen,
        /// y en los datos históricos contradice al período, así que no se usa
        /// para nada más que dejarla registrada.
        /// </summary>
        private DateTime? LeerFecha()
        {
            var texto = _txtFecha.Text.Trim();
            if (texto.Length == 0)
            {
                return null;
            }

            DateTime fecha;
            return DateTime.TryParse(
                texto, CultureInfo.CurrentCulture, DateTimeStyles.None, out fecha)
                ? (DateTime?)fecha.Date
                : null;
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
