using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FlightEvents.Data
{
    public class AzureBlobFlightPlanStorage : IFlightPlanStorage
    {
        private readonly BlobContainerClient containerClient;
        private readonly string customDomain;
        private readonly XmlSerializer serializer;

        public AzureBlobFlightPlanStorage(IConfiguration configuration)
        {
            var serviceClient = new BlobServiceClient(configuration["FlightPlan:AzureStorage:ConnectionString"]);
            containerClient = serviceClient.GetBlobContainerClient(configuration["FlightPlan:AzureStorage:ContainerName"]);
            customDomain = configuration["FlightPlan:AzureStorage:CustomDomain"];
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
