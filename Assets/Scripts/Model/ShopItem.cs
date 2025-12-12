using Newtonsoft.Json;
using System.Collections.Generic;

public class ShopItem
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("price")]
    public string Price { get; set; }
}

public class ShopData
{
    [JsonProperty("borders")]
    public List<ShopItem> Borders { get; set; }

    [JsonProperty("avatars")]
    public List<ShopItem> Avatars { get; set; }
}