using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class JsonNetSample : MonoBehaviour
{
    public Text Output;

    void Start()
    {
        Output.text = "Start!\n\n";

        SerailizeJson();
        DeserializeJson();
        LinqToJson();
        JsonPath();

        WriteLine("\nDone!");
    }

    void WriteLine(string msg)
    {
        Output.text = Output.text + msg + "\n";
    }

    public class Product
    {
        public string Name;
        public DateTime ExpiryDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public decimal Price;
        public string[] Sizes;

        public override bool Equals(object obj)
        {
            if (obj is Product)
            {
                Product p = (Product)obj;

                return (p.Name == Name && p.ExpiryDate == ExpiryDate && p.Price == Price);
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return (Name ?? string.Empty).GetHashCode();
        }
    }

    void SerailizeJson()
    {
        WriteLine("* SerailizeJson");

        Product product = new Product();
        product.Name = "Apple";
        product.ExpiryDate = new DateTime(2008, 12, 28);
        product.Sizes = new string[] { "Small" };

        string json = JsonConvert.SerializeObject(product);
        WriteLine(json);
    }

    public class Movie
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Classification { get; set; }
        public string Studio { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public List<string> ReleaseCountries { get; set; }
    }

    void DeserializeJson()
    {
        WriteLine("* DeserializeJson");

        string json = @"{
          'Name': 'Bad Boys',
          'ReleaseDate': '1995-4-7T00:00:00',
          'Genres': [
            'Action',
            'Comedy'
          ]
        }";

        Movie m = JsonConvert.DeserializeObject<Movie>(json);

        string name = m.Name;
        WriteLine(name);
    }

    void LinqToJson()
    {
        WriteLine("* LinqToJson");

        JArray array = new JArray();
        array.Add("Manual text");
        array.Add(new DateTime(2000, 5, 23));

        JObject o = new JObject();
        o["MyArray"] = array;

        string json = o.ToString();
        WriteLine(json);
    }

    private void JsonPath()
    {
        WriteLine("* JsonPath");

        var o = JObject.Parse(@"{
            'Stores': [
            'Lambton Quay',
            'Willis Street'
            ],
            'Manufacturers': [
            {
                'Name': 'Acme Co',
                'Products': [
                {
                    'Name': 'Anvil',
                    'Price': 50
                }
                ]
            },
            {
                'Name': 'Contoso',
                'Products': [
                {
                    'Name': 'Elbow Grease',
                    'Price': 99.95
                },
                {
                    'Name': 'Headlight Fluid',
                    'Price': 4
                }
                ]
            }
            ]
        }");

        JToken acme = o.SelectToken("$.Manufacturers[?(@.Name == 'Acme Co')]");
        WriteLine(acme.ToString());

        IEnumerable<JToken> pricyProducts = o.SelectTokens("$..Products[?(@.Price >= 50)].Name");
        foreach (var item in pricyProducts)
        {
            WriteLine(item.ToString());
        }
    }
}
