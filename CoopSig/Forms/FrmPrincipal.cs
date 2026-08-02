using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CoopSig.Config;

namespace CoopSig.Forms
{
    /// <summary>
    /// Menú principal. El respaldo automático (Constitución VII) ya se
    /// ejecutó en Program.cs antes de que esta pantalla se abra.
    /// </summary>
    public class FrmPrincipal : Form
    {
        private Label _lblBaseActual;
        private Button _btnAsociados;

        public FrmPrincipal()
        {
            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            Text = "CoopSig — Menú principal";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(420, 320);
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Font = new Font("Segoe UI", 10F);

            var lblTitulo = new Label
            {
                Text = "CoopSig",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            _btnAsociados = new Button
            {
                Text = "&Asociados",
                Location = new Point(20, 80),
                Size = new Size(380, 50),
                Font = new Font("Segoe UI", 12F)
            };
            _btnAsociados.Click += (s, e) => AbrirAsociados();

            var btnBonos = new Button
            {
                Text = "&Bonos",
                Location = new Point(20, 140),
                Size = new Size(380, 50),
                Font = new Font("Segoe UI", 12F)
            };
            btnBonos.Click += (s, e) => AbrirBonos();

            var btnAnticipos = new Button
            {
                Text = "Anticipos (próximamente)",
                Location = new Point(20, 200),
                Size = new Size(380, 50),
                Font = new Font("Segoe UI", 12F),
                Enabled = false
            };

            // HU-6: la aplicación indica en todo momento sobre qué base se
            // está trabajando.
            _lblBaseActual = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 265),
                Size = new Size(380, 30),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.DimGray,
                Text = ObtenerTextoBaseActual()
            };

            Controls.Add(lblTitulo);
            Controls.Add(_btnAsociados);
            Controls.Add(btnBonos);
            Controls.Add(btnAnticipos);
            Controls.Add(_lblBaseActual);

            AcceptButton = _btnAsociados;
        }

        private static string ObtenerTextoBaseActual()
        {
            try
            {
                var ruta = AppSettings.RutaBaseDatos;
                return "Base de datos: " + Path.GetFileName(ruta) + "  (" + Path.GetDirectoryName(ruta) + ")";
            }
            catch (Exception ex)
            {
                return "No se pudo determinar la base de datos configurada: " + ex.Message;
            }
        }

        private void AbrirAsociados()
        {
            using (var pantalla = new FrmAsociados())
            {
                pantalla.ShowDialog(this);
            }
        }

        private void AbrirBonos()
        {
            using (var pantalla = new FrmBonos())
            {
                pantalla.ShowDialog(this);
            }
        }
    }
}
