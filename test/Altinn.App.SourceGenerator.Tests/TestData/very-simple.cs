using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
namespace Altinn.App.Models
{
  [XmlRoot(ElementName="Skjema")]
  public class Skjema
  {
    [XmlElement("melding", Order = 1)]
    [JsonProperty("melding")]
    [JsonPropertyName("melding")]
    public dummy melding { get; set; }

  }

  public class dummy
  {
    [XmlElement("name", Order = 1)]
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string name { get; set; }

    [XmlElement("tags", Order = 2)]
    [JsonProperty("tags")]
    [JsonPropertyName("tags")]
    public string tags { get; set; }

    [XmlElement("simple_list", Order = 3)]
    [JsonProperty("simple_list")]
    [JsonPropertyName("simple_list")]
    public values_list simple_list { get; set; }

    [XmlElement("toggle", Order = 4)]
    [JsonProperty("toggle")]
    [JsonPropertyName("toggle")]
    public bool toggle { get; set; }

  }

  public class values_list
  {
    [XmlElement("simple_keyvalues", Order = 1)]
    [JsonProperty("simple_keyvalues")]
    [JsonPropertyName("simple_keyvalues")]
    public List<simple_keyvalues> simple_keyvalues { get; set; }

  }

  public class simple_keyvalues
  {
    [XmlElement("key", Order = 1)]
    [JsonProperty("key")]
    [JsonPropertyName("key")]
    public string key { get; set; }

    [XmlElement("doubleValue", Order = 2)]
    [JsonProperty("doubleValue")]
    [JsonPropertyName("doubleValue")]
    public decimal doubleValue { get; set; }

    [Range(Int32.MinValue,Int32.MaxValue)]
    [XmlElement("intValue", Order = 3)]
    [JsonProperty("intValue")]
    [JsonPropertyName("intValue")]
    public decimal intValue { get; set; }

  }
}
