using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace GestionImagenWebApp.Services
{
    public class AzureService
    {
        private readonly string _conexion;
        private readonly string _contenedor;
        private static Dictionary<string, (string url, DateTimeOffset expira)> _sasCache = new();

        public AzureService(IConfiguration configuration)
        {
            _conexion   = configuration.GetConnectionString("AzureConexion");
            _contenedor = configuration.GetConnectionString("AzureContenedor");
        }

        public async Task<string> SubirArchivo(IFormFile file)
        {
            var blobServiceClient = new BlobServiceClient(_conexion);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_contenedor);

            var Nombre = file.FileName;

            var blobClient = blobContainerClient.GetBlobClient(Nombre);
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            return blobClient.Uri.ToString();
        }

        public async Task<List<string>> ObtenerLista()
        {
            var blobServiceClient = new BlobServiceClient(_conexion);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_contenedor);

            var blobs = new List<string>();

            await foreach (var blobItem in blobContainerClient.GetBlobsAsync())
            {
                blobs.Add(blobItem.Name);
            }

            return blobs;
        } 

        public async Task<string> ObtenerURL(string blobName)
        {
            if (_sasCache.TryGetValue(blobName, out var entry) && entry.expira > DateTimeOffset.UtcNow)
            {
                return entry.url;
            }

            var blobServiceClient = new BlobServiceClient(_conexion);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_contenedor);
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("No se puede generar un SAS para este blob.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _contenedor,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(2) 
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

            _sasCache[blobName] = (sasUri.ToString(), sasBuilder.ExpiresOn);

            return sasUri.ToString();
        }

        public async Task<bool> EliminarImagen(string nombreArchivo)
        {
            var blobServiceClient = new BlobServiceClient(_conexion);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_contenedor);
            var blob = blobContainerClient.GetBlobClient(nombreArchivo);

            if (await blob.ExistsAsync())
            {
                await blob.DeleteAsync();
                return true;
            }

            return false;
        }

    }
}
