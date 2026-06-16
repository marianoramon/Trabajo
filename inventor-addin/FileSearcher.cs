using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BuscadorFicheros
{
    /// <summary>Tipo de fichero a buscar.</summary>
    public enum TipoBusqueda
    {
        Todos,
        Piezas,        // .ipt
        Ensamblajes    // .iam
    }

    /// <summary>Un fichero encontrado.</summary>
    public class ResultadoBusqueda
    {
        public string Ruta { get; set; }
        public string Nombre { get; set; }
        public string Extension { get; set; }
        public long Tamano { get; set; }
        public DateTime Modificado { get; set; }

        public string TamanoLegible
        {
            get
            {
                double s = Tamano;
                string[] u = { "B", "KB", "MB", "GB" };
                int i = 0;
                while (s >= 1024 && i < u.Length - 1) { s /= 1024; i++; }
                return i == 0 ? $"{s:0} B" : $"{s:0.0} {u[i]}";
            }
        }
    }

    /// <summary>
    /// Busca piezas y ensamblajes en carpetas de red por coincidencia parcial
    /// del código en el nombre del fichero. Recorre subcarpetas de forma segura
    /// ante errores de permisos.
    /// </summary>
    public class FileSearcher
    {
        public const int MaxResultados = 500;

        private readonly List<string> _carpetas;

        public FileSearcher(IEnumerable<string> carpetas)
        {
            _carpetas = carpetas
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public bool Truncado { get; private set; }

        public List<ResultadoBusqueda> Buscar(string codigo, TipoBusqueda tipo)
        {
            Truncado = false;
            var resultados = new List<ResultadoBusqueda>();
            if (string.IsNullOrWhiteSpace(codigo)) return resultados;

            var exts = ExtensionesDe(tipo);
            var codigoLower = codigo.Trim().ToLowerInvariant();

            foreach (var carpeta in _carpetas)
            {
                if (!Directory.Exists(carpeta)) continue;
                BuscarEnCarpeta(carpeta, codigoLower, exts, resultados);
                if (Truncado) break;
            }

            return resultados
                .OrderByDescending(r => r.Modificado)
                .ToList();
        }

        private static HashSet<string> ExtensionesDe(TipoBusqueda tipo)
        {
            switch (tipo)
            {
                case TipoBusqueda.Piezas:      return new HashSet<string> { ".ipt" };
                case TipoBusqueda.Ensamblajes: return new HashSet<string> { ".iam" };
                default:                       return new HashSet<string> { ".ipt", ".iam" };
            }
        }

        private void BuscarEnCarpeta(string carpeta, string codigoLower,
            HashSet<string> exts, List<ResultadoBusqueda> resultados)
        {
            string[] ficheros;
            try { ficheros = Directory.GetFiles(carpeta); }
            catch { return; } // sin permiso: se ignora

            foreach (var f in ficheros)
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                if (!exts.Contains(ext)) continue;

                var nombre = Path.GetFileNameWithoutExtension(f);
                if (nombre.ToLowerInvariant().IndexOf(codigoLower, StringComparison.Ordinal) < 0)
                    continue;

                try
                {
                    var fi = new FileInfo(f);
                    resultados.Add(new ResultadoBusqueda
                    {
                        Ruta = f,
                        Nombre = fi.Name,
                        Extension = ext,
                        Tamano = fi.Length,
                        Modificado = fi.LastWriteTime
                    });
                }
                catch { continue; }

                if (resultados.Count >= MaxResultados)
                {
                    Truncado = true;
                    return;
                }
            }

            string[] subcarpetas;
            try { subcarpetas = Directory.GetDirectories(carpeta); }
            catch { return; }

            foreach (var sc in subcarpetas)
            {
                BuscarEnCarpeta(sc, codigoLower, exts, resultados);
                if (Truncado) return;
            }
        }
    }
}
