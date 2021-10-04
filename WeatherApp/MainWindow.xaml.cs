using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;

namespace WeatherApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<string> cities = new List<string>();
        public List<Weather> list = new List<Weather>();
        public string API_ID = "your api key";

        public MainWindow()
        {
            InitializeComponent();

            getCities();

            Thread task = new Thread(fetchData);
            task.Start();
            
        }

        public class Weather
        {
            public string City { get; set; }
            public string Temperature { get; set; }
            public string Description { get; set; }
            public string Wind { get; set; }
        }

        private void getCities()
        {
            RestClient client = new RestClient("https://countriesnow.space/api/v0.1/countries/population/cities/filter");
            client.Timeout = -1;
            RestRequest request = new RestRequest(Method.POST);
            string body = @"{" + "\n" +
            @"	""limit"": 42," + "\n" + //idk why but countriesnow.space filter shows only 42 cities in Poland (the biggest)
            @"	""order"": ""asc""," + "\n" +
            @"	""orderBy"": ""name""," + "\n" +
            @"	""country"": ""Poland""" + "\n" +
            @"}";
            request.AddJsonBody(body);
            IRestResponse response = client.Execute(request);
            JObject json = JObject.Parse(response.Content);
  
            for (int i = 0; i < 42; i++)
            {
                string qq = string.Format("data[{0}].city", i);
                cities.Add(json.SelectToken(qq).ToString());
            }
            
        }

        private void fetchData()
        {
            for (int i = 0; i < 42; i++)
            {
                string temp = string.Format("https://api.openweathermap.org/data/2.5/weather?q={0}&APPID={1}&units=metric", cities[i].ToLower(), API_ID);
                RestClient mainClient = new RestClient(temp);
                mainClient.Timeout = -1;
                RestRequest req = new RestRequest(Method.GET);
                IRestResponse resp = mainClient.Execute(req);
                JObject jsono = JObject.Parse(resp.Content);

                if (jsono.SelectToken("cod").ToString().Equals("200"))
                    list.Add(new Weather { City = cities[i], Temperature = jsono.SelectToken("main.temp").ToString() + " °C",
                        Description = jsono.SelectToken("weather[0].description").ToString(),
                        Wind = jsono.SelectToken("wind.speed").ToString() });
            }
            Application.Current.Dispatcher.InvokeAsync(new Action(() =>
            {
                weatherList.ItemsSource = list;
            }));
            excelExport();
        }

        private void excelExport()
        {
            List<string> header = new List<string>() { "City", "Temperature", "Description", "Wind [m/s]" };
            
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            ExcelPackage pck = new ExcelPackage();

            ExcelWorksheet ws = pck.Workbook.Worksheets.Add("Weather");
            
            for (int i = 0; i < header.Count; i++)
            {
                ws.Cells[1, i + 1].Value = header[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
            }

            for (int i = 0; i < list.Count; i++)
            {
                ws.Cells[i + 2, 1].Value = list[i].City;
                ws.Cells[i + 2, 2].Value = list[i].Temperature;
                ws.Cells[i + 2, 3].Value = list[i].Description;
                ws.Cells[i + 2, 4].Value = list[i].Wind;
            }
            ws.Cells.AutoFitColumns();

            byte[] fileText = pck.GetAsByteArray();

            File.WriteAllBytes("weather.xlsx", fileText);
        }
    }
}
