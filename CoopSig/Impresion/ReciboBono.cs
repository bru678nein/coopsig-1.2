using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using CoopSig.Models;
using CoopSig.Utils;

namespace CoopSig.Impresion
{
    /// <summary>
    /// Dibuja el recibo del bono, replicando el informe de Access que usa hoy
    /// la oficina.
    ///
    /// Se imprime sobre papel EN BLANCO, no sobre formulario preimpreso: el
    /// informe original dibuja también el membrete, las etiquetas y las líneas
    /// de firma, así que acá se hace lo mismo y no hay coordenadas que medir
    /// contra un papel.
    ///
    /// El neto y el 2% de Ley 20337 se recalculan al imprimir, igual que hace
    /// el informe: no están guardados en la tabla. El resto de los importes
    /// sale congelado de la fila del bono, así que un bono viejo reimpreso hoy
    /// muestra los valores de su época.
    /// </summary>
    public class ReciboBono
    {
        private const string RazonSocial = "\"Sistema de Informaciones Generales\" Ltda.";
        private const string Encabezado = "Cooperativa de Trabajo";
        private const string Domicilio = "Rioja 443, Ciudad - Mendoza";
        private const string Cuit = "C.U.I.T. 30-62630506-3";

        private readonly Bono _bono;

        private Font _fuenteChica;
        private Font _fuenteNormal;
        private Font _fuenteNegrita;
        private Font _fuenteTitulo;
        private Font _fuenteRazonSocial;

        public ReciboBono(Bono bono)
        {
            _bono = bono;
        }

        public PrintDocument CrearDocumento()
        {
            var documento = new PrintDocument
            {
                DocumentName = string.Format(
                    "Bono {0} - {1}", _bono.PeriodoDescripto, _bono.NombreCompleto)
            };

            // A4 vertical. Es el papel estándar acá y el que usa el informe
            // original; si en la oficina cargan otro, se cambia en el diálogo
            // de impresión sin tocar el programa.
            documento.DefaultPageSettings.Landscape = false;
            documento.PrintPage += Dibujar;
            return documento;
        }

        private void Dibujar(object remitente, PrintPageEventArgs e)
        {
            using (_fuenteChica = new Font("Segoe UI", 8F))
            using (_fuenteNormal = new Font("Segoe UI", 10F))
            using (_fuenteNegrita = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (_fuenteTitulo = new Font("Segoe UI", 12F))
            using (_fuenteRazonSocial = new Font("Segoe UI", 15F, FontStyle.Bold))
            {
                DibujarRecibo(e.Graphics, e.MarginBounds);
            }

            e.HasMorePages = false;
        }

        private void DibujarRecibo(Graphics lienzo, Rectangle area)
        {
            var y = area.Top;

            y = DibujarMembrete(lienzo, area, y);
            y += 18;
            y = DibujarDatosDelAsociado(lienzo, area, y);
            y += 14;

            var resultado = _bono.Calcular();

            y = DibujarConceptos(lienzo, area, y, resultado);
            y += 16;
            y = DibujarTotales(lienzo, area, y, resultado);
            y += 24;

            DibujarImporteEnLetras(lienzo, area, y, resultado);
            DibujarFirmas(lienzo, area);
        }

        private int DibujarMembrete(Graphics lienzo, Rectangle area, int y)
        {
            y = DibujarCentrado(lienzo, Encabezado, _fuenteTitulo, area, y);
            y = DibujarCentrado(lienzo, RazonSocial, _fuenteRazonSocial, area, y);
            y = DibujarCentrado(lienzo, Domicilio, _fuenteChica, area, y);
            y = DibujarCentrado(lienzo, Cuit, _fuenteChica, area, y);
            return y;
        }

        private int DibujarDatosDelAsociado(Graphics lienzo, Rectangle area, int y)
        {
            LineaHorizontal(lienzo, area, y);
            y += 8;

            DibujarEtiquetaYValor(lienzo, area.Left, y, "Apellido:", _bono.Apellido, 260);
            DibujarEtiquetaYValor(lienzo, area.Left + 300, y, "Nombre:", _bono.Nombre, 240);
            y += 22;

            DibujarEtiquetaYValor(lienzo, area.Left, y, "CUIL:", CuilCompleto(), 260);
            DibujarEtiquetaYValor(
                lienzo, area.Left + 300, y, "Período:", _bono.PeriodoDescripto, 240);
            y += 22;

            DibujarEtiquetaYValor(lienzo, area.Left, y, "Servicio:", _bono.Servicio, 260);
            DibujarEtiquetaYValor(
                lienzo, area.Left + 300, y, "Fecha:",
                _bono.Fecha.HasValue
                    ? _bono.Fecha.Value.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture)
                    : string.Empty,
                240);
            y += 24;

            LineaHorizontal(lienzo, area, y);
            return y + 8;
        }

        /// <summary>
        /// Haberes a la izquierda, descuentos a la derecha, como en el recibo
        /// original.
        /// </summary>
        private int DibujarConceptos(
            Graphics lienzo, Rectangle area, int y, ResultadoBono resultado)
        {
            var yIzquierda = y;
            var yDerecha = y;

            var columnaDerecha = area.Left + 300;

            if (_bono.Horas > 0m || _bono.ValorHora > 0m)
            {
                DibujarRenglonDeImporte(
                    lienzo, area.Left,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "{0:N2} Horas x $ {1:N2}", _bono.Horas, _bono.ValorHora),
                    resultado.TotalHoras, yIzquierda, 280);
                yIzquierda += 22;
            }

            if (_bono.Basico > 0m || !string.IsNullOrWhiteSpace(_bono.Comentario))
            {
                DibujarRenglonDeImporte(
                    lienzo, area.Left,
                    string.IsNullOrWhiteSpace(_bono.Comentario) ? "Básico" : _bono.Comentario,
                    _bono.Basico, yIzquierda, 280);
                yIzquierda += 22;
            }

            DibujarRenglonDeImporte(
                lienzo, columnaDerecha, "Ley 20337 (2%):", resultado.Ley20337, yDerecha, 240);
            yDerecha += 22;

            DibujarRenglonDeImporte(
                lienzo, columnaDerecha, "Seguro:", _bono.Mutual, yDerecha, 240);
            yDerecha += 22;

            DibujarRenglonDeImporte(
                lienzo, columnaDerecha, "Anticipo:", _bono.Anticipo, yDerecha, 240);
            yDerecha += 22;

            if (_bono.Otros > 0m || !string.IsNullOrWhiteSpace(_bono.OtrosComentario))
            {
                DibujarRenglonDeImporte(
                    lienzo, columnaDerecha,
                    string.IsNullOrWhiteSpace(_bono.OtrosComentario)
                        ? "Otros:"
                        : _bono.OtrosComentario + ":",
                    _bono.Otros, yDerecha, 240);
                yDerecha += 22;
            }

            return Math.Max(yIzquierda, yDerecha);
        }

        private int DibujarTotales(
            Graphics lienzo, Rectangle area, int y, ResultadoBono resultado)
        {
            LineaHorizontal(lienzo, area, y);
            y += 8;

            DibujarRenglonDeImporte(
                lienzo, area.Left, "Total Excedentes Repartibles:",
                resultado.Haberes, y, 340);
            y += 22;

            DibujarRenglonDeImporte(
                lienzo, area.Left, "Total Descuentos:", resultado.TotalDescuentos, y, 340);
            y += 26;

            DibujarRenglonDeImporte(
                lienzo, area.Left, "Neto a Cobrar:", resultado.Neto, y, 340, negrita: true);
            return y + 26;
        }

        private void DibujarImporteEnLetras(
            Graphics lienzo, Rectangle area, int y, ResultadoBono resultado)
        {
            var texto = "Recibí la cantidad de Pesos: " + NumeroEnLetras.Convertir(resultado.Neto);

            // Se deja envolver en varios renglones: un importe largo en letras
            // no entra en el ancho de la hoja.
            var rectangulo = new RectangleF(area.Left, y, area.Width, 60);
            lienzo.DrawString(texto, _fuenteNormal, Brushes.Black, rectangulo);
        }

        private void DibujarFirmas(Graphics lienzo, Rectangle area)
        {
            // Las firmas van ancladas al pie del área imprimible, no debajo del
            // último concepto: así quedan siempre a la misma altura sin importar
            // cuántos renglones tenga el bono.
            var y = area.Bottom - 70;
            var ancho = area.Width / 4;

            var titulos = new[] { "Presidente", "Secretario", "Tesorero", "Asociado" };
            for (var i = 0; i < titulos.Length; i++)
            {
                var izquierda = area.Left + i * ancho;
                lienzo.DrawLine(Pens.Black, izquierda + 10, y, izquierda + ancho - 10, y);

                var formato = new StringFormat { Alignment = StringAlignment.Center };
                lienzo.DrawString(
                    titulos[i], _fuenteChica, Brushes.Black,
                    new RectangleF(izquierda, y + 4, ancho, 18), formato);
            }

            lienzo.DrawString(
                "(Aclaración y N° Documento)", _fuenteChica, Brushes.Gray,
                new RectangleF(area.Left + 3 * ancho, y + 20, ancho, 18),
                new StringFormat { Alignment = StringAlignment.Center });
        }

        private string CuilCompleto()
        {
            if (string.IsNullOrWhiteSpace(_bono.Cuil) || string.IsNullOrWhiteSpace(_bono.Digito))
            {
                return _bono.Documento.ToString(CultureInfo.InvariantCulture);
            }
            return string.Format("{0} - {1} - {2}", _bono.Cuil, _bono.Documento, _bono.Digito);
        }

        private int DibujarCentrado(
            Graphics lienzo, string texto, Font fuente, Rectangle area, int y)
        {
            var formato = new StringFormat { Alignment = StringAlignment.Center };
            var alto = (int)lienzo.MeasureString(texto, fuente, area.Width).Height;
            lienzo.DrawString(
                texto, fuente, Brushes.Black,
                new RectangleF(area.Left, y, area.Width, alto), formato);
            return y + alto;
        }

        private void DibujarEtiquetaYValor(
            Graphics lienzo, int x, int y, string etiqueta, string valor, int ancho)
        {
            lienzo.DrawString(etiqueta, _fuenteChica, Brushes.Black, x, y + 2);

            var corrimiento = (int)lienzo.MeasureString(etiqueta, _fuenteChica).Width + 6;
            lienzo.DrawString(valor ?? string.Empty, _fuenteNegrita, Brushes.Black,
                new RectangleF(x + corrimiento, y, ancho - corrimiento, 20));
        }

        /// <summary>Concepto a la izquierda e importe alineado a la derecha.</summary>
        private void DibujarRenglonDeImporte(
            Graphics lienzo, int x, string concepto, decimal importe, int y, int ancho,
            bool negrita = false)
        {
            var fuente = negrita ? _fuenteNegrita : _fuenteNormal;

            lienzo.DrawString(concepto, fuente, Brushes.Black, x, y);
            lienzo.DrawString(
                string.Format(CultureInfo.CurrentCulture, "$ {0:N2}", importe),
                fuente, Brushes.Black,
                new RectangleF(x, y, ancho, 20),
                new StringFormat { Alignment = StringAlignment.Far });
        }

        private static void LineaHorizontal(Graphics lienzo, Rectangle area, int y)
        {
            lienzo.DrawLine(Pens.Black, area.Left, y, area.Right, y);
        }
    }
}
