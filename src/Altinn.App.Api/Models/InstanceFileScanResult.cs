using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Enums;

namespace Altinn.App.Api.Models
{
    /// <summary>
    /// Light weight model representing an instance and it's file scan result status.
    /// </summary>
    public class InstanceFileScanResult
    {
        private readonly List<DataElementFileScanResult> _dataElements;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceFileScanResult"/> class.
        /// </summary>
        public InstanceFileScanResult(InstanceIdentifier instanceIdentifier)
        {
            Id = instanceIdentifier;
            _dataElements = new List<DataElementFileScanResult>();
        }

        /// <summary>
        /// Instance id
        /// </summary>
        [JsonPropertyName("id")]
        public InstanceIdentifier Id { get; set; }

        /// <summary>
        /// Contains the aggregated file scan result for an instance.
        /// Infected = If any data elements has a status of Infected
        /// Clean = If all data elements has status Clean
        /// Pending = If all or some are Pending and the rest are Clean
        /// </summary>
        [JsonPropertyName("fileScanResult")]
        public FileScanResult FileScanResult { get; private set; }

        /// <summary>
        /// File scan result for individual data elements.
        /// </summary>
        [JsonPropertyName("data")]
        public IReadOnlyList<DataElementFileScanResult> DataElements => _dataElements.AsReadOnly();

        /// <summary>
        /// Adds a individual data element file scan result and updates the aggregated file scan result status
        /// </summary>        
        public void AddFileScanResult(DataElementFileScanResult dataElementFileScanResult)
        {
            if (dataElementFileScanResult.FileScanResult != FileScanResult.NotApplicable)
            {
                _dataElements.Add(dataElementFileScanResult);

                RecalculateAggregatedStatus();
            }            
        }

        private void RecalculateAggregatedStatus()
        {
            if (_dataElements.TrueForAll(dataElement => dataElement.FileScanResult == FileScanResult.Clean))
            {
                FileScanResult = FileScanResult.Clean;
            }
            else if (_dataElements.Any(dataElement => dataElement.FileScanResult == FileScanResult.Infected))
            {
                FileScanResult = FileScanResult.Infected;
            }
            else if (_dataElements.Any(dataElement => dataElement.FileScanResult == FileScanResult.Pending))
            {
                FileScanResult = FileScanResult.Pending;
            }
        }
    }
}
