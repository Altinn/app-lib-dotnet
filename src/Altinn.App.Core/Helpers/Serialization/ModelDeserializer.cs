using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace Altinn.App.Core.Helpers.Serialization
{
    /// <summary>
    /// Represents logic to deserialize a stream of data to an instance of the given type
    /// </summary>
    public class ModelDeserializer
    {
        private readonly ILogger _logger;
        private readonly Type _modelType;

        /// <summary>
        /// Initialize a new instance of <see cref="ModelDeserializer"/> with a logger and the Type the deserializer should target.
        /// </summary>
        /// <param name="logger">A logger that can be used to write log information.</param>
        /// <param name="modelType">The Type the deserializer should target when deserializing data.</param>
        public ModelDeserializer(ILogger logger, Type modelType)
        {
            _logger = logger;
            _modelType = modelType;
        }

        /// <summary>
        /// Deserialize a stream with data of the given content type. The operation supports application/json and application/xml.
        /// </summary>
        /// <param name="stream">The data stream to deserialize.</param>
        /// <param name="contentType">The content type of the stream.</param>
        /// <returns>An instance of the initialized type if deserializing succeed.</returns>
        public async Task<ModelDeserializerResult> DeserializeAsync(Stream stream, string? contentType)
        {

            if (contentType == null)
            {
                return ModelDeserializerResult.FromError($"Unknown content type \"null\". Cannot read the data.");
            }

            if (contentType.Contains("application/json"))
            {
                return await DeserializeJsonAsync(stream);
            }

            if (contentType.Contains("application/xml"))
            {
                return await DeserializeXmlAsync(stream);
            }
            return ModelDeserializerResult.FromError($"Unknown content type {contentType}. Cannot read the data.");
        }

        private async Task<ModelDeserializerResult> DeserializeJsonAsync(Stream stream)
        {
            try
            {
                using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                string content = await reader.ReadToEndAsync();
                return ModelDeserializerResult.FromSuccess(JsonConvert.DeserializeObject(content, _modelType));
            }
            catch (JsonReaderException jsonReaderException)
            {
                return ModelDeserializerResult.FromError(jsonReaderException.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception when attempting to deserialize JSON into '{modelType}'", _modelType);
                return ModelDeserializerResult.FromError($"Unexpected exception when attempting to deserialize JSON into '{_modelType}'");
            };

        }

        private async Task<ModelDeserializerResult> DeserializeXmlAsync(Stream stream)
        {
            // In this first try block we assume that the namespace is the same in the model
            // and in the XML. This includes no namespace in both.
            using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            string? streamContent = await reader.ReadToEndAsync();
            try
            {
                using XmlTextReader xmlTextReader = new XmlTextReader(new StringReader(streamContent));
                XmlSerializer serializer = new XmlSerializer(_modelType);

                return ModelDeserializerResult.FromSuccess(serializer.Deserialize(xmlTextReader));
            }
            catch (InvalidOperationException)
            {
                try
                {
                    // In this backup try block we assume that the modelType has declared a namespace,
                    // but that the XML is without any namespace declaration.
                    string elementName = GetRootElementName(_modelType);

                    XmlAttributeOverrides attributeOverrides = new XmlAttributeOverrides();
                    XmlAttributes attributes = new XmlAttributes();
                    attributes.XmlRoot = new XmlRootAttribute(elementName);
                    attributeOverrides.Add(_modelType, attributes);

                    using XmlTextReader xmlTextReader = new XmlTextReader(new StringReader(streamContent));
                    XmlSerializer serializer = new XmlSerializer(_modelType, attributeOverrides);

                    return ModelDeserializerResult.FromSuccess(serializer.Deserialize(xmlTextReader));
                }
                catch (InvalidOperationException invalidOperationException)
                {
                    // One possible fail condition is if the XML has a namespace, but the model does not, or that the namespaces are different.
                    return ModelDeserializerResult.FromError($"{invalidOperationException.Message} {invalidOperationException?.InnerException?.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception when attempting to deserialize XML into '{modelType}'", _modelType);
                return ModelDeserializerResult.FromError($"Unexpected exception when attempting to deserialize XML into '{_modelType}'");
            }
        }

        private static string GetRootElementName(Type modelType)
        {
            Attribute[] attributes = Attribute.GetCustomAttributes(modelType);

            foreach (var attribute in attributes)
            {
                var xmlRootAttribute = attribute as XmlRootAttribute;
                if (xmlRootAttribute != null)
                {
                    return xmlRootAttribute.ElementName;
                }
            }

            return modelType.Name;
        }
    }
}

