using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
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

        private readonly Dictionary<string, FlightPlanData> cachedFlightPlans = new Dictionary<string, FlightPlanData>();
        private readonly SemaphoreSlim sm = new SemaphoreSlim(1);

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
            try
            {
                await sm.WaitAsync();

                if (cachedFlightPlans.ContainsKey(id)) return cachedFlightPlans[id];

                await containerClient.CreateIfNotExistsAsync();
                var blobClient = containerClient.GetBlobClient(id);
                var info = await blobClient.DownloadAsync();
                using (info.Value.Content)
                {
                    var document = serializer.Deserialize(info.Value.Content) as FlightPlanDocumentXml;
                    var result = document.FlightPlan.ToData();
                    cachedFlightPlans.Add(id, result);
                    return result;
                }
            }
            finally
            {
                sm.Release();
            }
        }
    }
}
