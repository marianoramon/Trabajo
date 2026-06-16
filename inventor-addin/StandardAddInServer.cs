using System;
using System.Runtime.InteropServices;
using Inventor;

namespace BuscadorFicheros
{
    /// <summary>
    /// Servidor del complemento de Inventor. Inventor lo carga al arrancar
    /// (vía el manifiesto BuscadorFicheros.addin) y añade un botón a la cinta.
    /// </summary>
    [Guid("7F3D2A1C-9B4E-4C2A-8E1F-3C5D6A7B8E90")]
    [ComVisible(true)]
    public class StandardAddInServer : ApplicationAddInServer
    {
        // Debe coincidir con el ClientId/ClassId del fichero .addin.
        private const string ClientId = "{7F3D2A1C-9B4E-4C2A-8E1F-3C5D6A7B8E90}";

        private Inventor.Application _app;
        private ButtonDefinition _btnBuscar;

        public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            _app = addInSiteObject.Application;

            // Crear el botón (comando) del buscador.
            ControlDefinitions controlDefs = _app.CommandManager.ControlDefinitions;
            _btnBuscar = controlDefs.AddButtonDefinition(
                "Buscar\nficheros",
                "BuscadorFicheros:Buscar",
                CommandTypesEnum.kQueryOnlyCmdType,
                ClientId,
                "Buscar piezas y ensamblajes en la red por código",
                "Buscar ficheros en la red",
                Type.Missing,
                Type.Missing,
                ButtonDisplayEnum.kAlwaysDisplayText);

            _btnBuscar.OnExecute += BtnBuscar_OnExecute;

            try { AnadirALaCinta(); }
            catch { /* si algún entorno no admite el botón, se ignora */ }
        }

        public void Deactivate()
        {
            if (_btnBuscar != null)
            {
                _btnBuscar.OnExecute -= BtnBuscar_OnExecute;
                _btnBuscar = null;
            }
            _app = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void ExecuteCommand(int commandID) { }

        public object Automation => null;

        private void BtnBuscar_OnExecute(NameValueMap context)
        {
            using (var frm = new SearchForm(_app))
            {
                frm.ShowDialog();
            }
        }

        /// <summary>Añade el botón a la pestaña Herramientas de cada entorno.</summary>
        private void AnadirALaCinta()
        {
            UserInterfaceManager ui = _app.UserInterfaceManager;
            string[] entornos = { "ZeroDoc", "Part", "Assembly", "Drawing", "Presentation" };

            foreach (var nombre in entornos)
            {
                try
                {
                    Ribbon ribbon = ui.Ribbons[nombre];
                    RibbonTab tab = ObtenerPestanaHerramientas(ribbon);
                    if (tab == null) continue;

                    RibbonPanel panel;
                    try { panel = tab.RibbonPanels["BuscadorFicheros:Panel"]; }
                    catch { panel = tab.RibbonPanels.Add("Buscador", "BuscadorFicheros:Panel", ClientId); }

                    panel.CommandControls.AddButton(_btnBuscar, true);
                }
                catch
                {
                    // Ese entorno no admite el botón; continuamos con el resto.
                }
            }
        }

        private static RibbonTab ObtenerPestanaHerramientas(Ribbon ribbon)
        {
            // Intentar la pestaña "Herramientas" estándar.
            try { return ribbon.RibbonTabs["id_TabTools"]; }
            catch { }

            // Si no existe (p. ej. ZeroDoc), crear una pestaña propia.
            try { return ribbon.RibbonTabs.Add("Buscador", "BuscadorFicheros:Tab", "{7F3D2A1C-9B4E-4C2A-8E1F-3C5D6A7B8E90}"); }
            catch { return null; }
        }
    }
}
