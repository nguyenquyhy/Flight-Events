using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FlightEvents.Data
{
    public class AzureBlobOptions
    {
        [Required]
        public string ConnectionString { get; set; }
        [Required]
        public string ContainerName { get; set; }
        public string CustomDomain { get; set; }
    }

    public class AzureBlobFlightPlanFileStorage : IFlightPlanFileStorage
    {
        private readonly BlobContainerClient containerClient;
        private readonly string customDomain;
        private readonly XmlSerializer serializer;

        public AzureBlobFlightPlanFileStorage(IOptionsMonitor<AzureBlobOptions> options)
        {
            var serviceClient = new BlobServiceClient(options.CurrentValue.ConnectionString);
            containerClient = serviceClient.GetBlobContainerClient(options.CurrentValue.ContainerName);
            customDomain = options.CurrentValue.CustomDomain;
            serializer = new XmlSerializer(typeof(FlightPlanDocumentXml));
        }

        public async Task<string> GetFlightPlanUrlAsync(string id)
        {
            await containerClient.CreateIfNotExistsAsync();
            var blobClient = containerClient.GetBlobClient(id);
            if (string.IsNullOrEmpty(customDomain))
            {
                return blobClient.Uri.ToString();
            }
            else
            {
                return blobClient.Uri.ToString().Replace(blobClient.Uri.Host, customDomain);
            }
        }

        public async Task<FlightPlanData> GetFlightPlanAsync(string id)
        {

            await containerClient.CreateIfNotExistsAsync();
            var blobClient = containerClient.GetBlobClient(id);
            var info = await blobClient.DownloadAsync();
            using (info.Value.Content)
            {
                var document = serializer.Deserialize(info.Value.Content) as FlightPlanDocumentXml;
                return document.FlightPlan.ToData();
            }
        }
    }
}
