using Azure.Storage.Blobs;
using GestionImagenWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestionImagenWebApp.Controllers
{
    public class ImagenController : Controller
    {
        private readonly AzureService _blobStorageService;

        public ImagenController(AzureService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SubirImagen()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Subir(IFormFile archivo)
        {
            //
            // Validamos que el archivo sea una imagen.
            //
            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(archivo.FileName).ToLower();
            if (!extensionesPermitidas.Contains(extension))
            {
                ViewBag.Message = "Solo se permiten archivos de imagen con el siguiente formato: (JPG, JPEG, PNG, GIF).";
                return View("SubirImagen");
            }

            //
            // Subimos la imagen al contenedor de Azure Storage.
            //
            var URL = await _blobStorageService.SubirArchivo(archivo);
            ViewBag.Message = "Imagen subida correctamente.";

            return View("SubirImagen");
        }

        public async Task<IActionResult> ListarImagen()
        {
            //
            // Obtenemos la lista de imágenes del contenedor de Azure Storage.
            //
            var imagenList     = await _blobStorageService.ObtenerLista();
            var imagenInfoList = new List<ImagenInfo>();

            //
            // Obtenemos la URL de cada imagen.
            //
            foreach (var imagen in imagenList)
            {
                var URL = await _blobStorageService.ObtenerURL(imagen);
                imagenInfoList.Add(new ImagenInfo { Nombre = imagen, URL = URL });
            }

            return View(imagenInfoList);
        }

        public class ImagenInfo
        {
            public string Nombre { get; set; }
            public string URL { get; set; }
        }

        public IActionResult VerImagen(string URL)
        {
            ViewBag.ImagenUrl = URL;
            return View();
        }

        [HttpDelete]
        public async Task<IActionResult> EliminarImagen(string nombre)
        {
            bool eliminado = await _blobStorageService.EliminarImagen(nombre);

            if (eliminado)
                return Ok(new { mensaje = "Imagen eliminada correctamente." });
            else
                return NotFound(new { mensaje = "Imagen no encontrada." });
        }
    }
}
