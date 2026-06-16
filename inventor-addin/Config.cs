using System;
using System.Collections.Generic;
using System.IO;

namespace BuscadorFicheros
{
    /// <summary>
    /// Carga las carpetas de red donde buscar.
    /// Lee el fichero:  Documentos\BuscadorInventor\carpetas.txt
    /// (una ruta por línea; las líneas que empiezan por '#' se ignoran).
    /// Si no existe, lo crea con un ejemplo para que el usuario lo edite.
    /// </summary>
    public static class Config
    {
        public static string ConfigFolder =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "BuscadorInventor");

        public static string ConfigFile => Path.Combine(ConfigFolder, "carpetas.txt");

        public static List<string> CargarCarpetas()
        {
            var carpetas = new List<string>();

            if (!File.Exists(ConfigFile))
            {
                CrearPlantilla();
                return carpetas;
            }

            foreach (var linea in File.ReadAllLines(ConfigFile))
            {
                var l = linea.Trim();
                if (l.Length == 0 || l.StartsWith("#")) continue;
                carpetas.Add(l);
            }
            return carpetas;
        }

        private static void CrearPlantilla()
        {
            try
            {
                Directory.CreateDirectory(ConfigFolder);
                File.WriteAllText(ConfigFile,
                    "# Carpetas de red donde buscar ficheros de Inventor." + Environment.NewLine +
                    "# Una ruta por línea. Acepta UNC (\\\\servidor\\carpeta) o unidades (Q:\\)." + Environment.NewLine +
                    "# Quita el '#' y pon tus rutas reales:" + Environment.NewLine +
                    "# \\\\servidor\\DISEÑOS" + Environment.NewLine +
                    "# Q:\\DISEÑOS" + Environment.NewLine);
            }
            catch
            {
                // Si no se puede crear, la app avisará de que no hay carpetas.
            }
        }
    }
}
