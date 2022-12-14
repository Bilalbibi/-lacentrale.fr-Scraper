using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using lacentrale.fr_Scraper.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using LicenseContext = OfficeOpenXml.LicenseContext;
namespace lacentrale.fr_Scraper
{
    public partial class DevForm : DevExpress.XtraEditors.XtraForm
    {
        private List<Car> cars = new List<Car>();
        private Config _config;
        public List<InputModel> inputModels = new List<InputModel>(); public HttpCaller HttpCaller = new HttpCaller();
        public DevForm()
        {
            InitializeComponent();
        }
        public delegate void SetProgressD(int x);
        public void SetProgress(int x)
        {
            if (InvokeRequired)
            {
                Invoke(new SetProgressD(SetProgress), x);
                return;
            }
            if ((x <= 100))
            {
                progressB.EditValue = x;
            }
        }
        public delegate void DisplayD(string s);
        public void Display(string s)
        {
            if (InvokeRequired)
            {
                Invoke(new DisplayD(Display), s);
                return;
            }
            displayT.Text = s;
        }
        private async void StartB_Click(object sender, EventArgs e)
        {

            //&reclaimVAT=true
            #region Find Vat Filters
            //var doc = await HttpCaller.GetDoc("https://www.lacentrale.fr/listing?reclaimVAT=true&customerFamilyCodes=CONCESSIONNAIRE&energies=elec&mileageMax=50000&priceMax=29000&reclaimVAT=true&yearMax=2021&yearMin=2018");
            //var json = doc.DocumentNode
            //    .SelectSingleNode("//script[contains(text(),'window.__PRELOADED_STATE_LISTING__')]").InnerText; 
            #endregion
            #region Get Url cars for each page
            //var doc = await HttpCaller.GetDoc("https://www.lacentrale.fr/listing?customerFamilyCodes=CONCESSIONNAIRE&energies=elec&makesModelsCommercialNames=BMW&mileageMax=50000&options=&priceMax=29000&yearMax=2021&yearMin=2018");
            //var carsNodes = doc.DocumentNode.SelectNodes("//div[@class='resultList mB15 hiddenOverflow listing ']//a[@class='searchCard__link ']");
            //var nbrCars = doc.DocumentNode.SelectSingleNode("//span[@class='numAnn']").InnerText.Trim();
            //var pages = 1;
            //var otherPages = doc.DocumentNode.SelectSingleNode("//div[@class='pagination__more']");
            //if (otherPages!=null)
            //{
            //    pages = int.Parse(doc.DocumentNode.SelectSingleNode("//div[@class='pagination__more']/ul/li[last()]")
            //        .InnerText.Trim());
            //}
            //else
            //{
            //    pages = int.Parse(doc.DocumentNode.SelectSingleNode("//div[@class='rch-pagination']/ul/li[@class='last']")
            //        .InnerText.Trim());
            //}
            #endregion

            #region car details
            //var doc = await HttpCaller.GetDoc("https://www.lacentrale.fr/auto-occasion-annonce-69107134687.html");
            ////doc.Save("car.html");

            //var title = doc.DocumentNode.SelectSingleNode("//div[@id='generalInformationWrapper']//h1").InnerText
            //    .Trim();
            //var array = title.Split(' ');
            //var make = array[0];
            //var model = title.Replace(make, "").Trim();
            //var date = doc.DocumentNode
            //    .SelectSingleNode("//*[contains(text(),'circulation')]/../following-sibling::span").InnerText
            //    .Trim();
            //var kms = doc.DocumentNode
            //    .SelectSingleNode("//*[contains(text(),'Kilométrage com')]/../following-sibling::span").InnerText
            //    .Trim();
            //var jsonPhoneVaribalesQuery = doc.DocumentNode
            //    .SelectSingleNode("//script[contains(text(),'window.fragment_seller_contact_tabs_state')]").InnerText
            //    .Trim().Replace("window.fragment_seller_contact_tabs_state = ", "").Replace(";", "");
            //var objt = JObject.Parse(jsonPhoneVaribalesQuery);
            //var price = string.Format(CultureInfo.InvariantCulture, "€{0:n0}", (int)objt.SelectToken("price"));
            //var id = (string)objt.SelectToken("classifiedId");
            //var cuRef = (string)objt.SelectToken("classifiedOwnerCorrelationId");
            //var eci = (string)objt.SelectToken("eventCorrelationId");
            //var url = $@"https://www.lacentrale.fr/seller-contact-tab?tab=PHONE&id={id}&cuRef={cuRef}&eci={eci}";
            //doc = await HttpCaller.GetDoc(url);
            //Console.WriteLine(doc.DocumentNode.OuterHtml);
            //var phoneText = WebUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//span[@class='phone']").InnerText.Trim());
            //var arrayP = phoneText.Split('\n');
            //var phone = arrayP[0];
            #endregion
            //return;
            if (!Daily.Checked && !ThreeDays.Checked)
            {
                MessageBox.Show(@"Please select time base scraping ""Daily"" or ""3 days"" option ");
                return;
            }
            do
            {
                if (File.Exists("filters url.txt"))
                {
                    File.Delete("filters url.txt");
                }
                inputModels = new List<InputModel>();
                cars = new List<Car>();
                for (int i = 0; i < FiltersDGV.RowCount; i++)
                {
                    var input = FiltersDGV.GetRow(i) as InputModel;
                    inputModels.Add(input);
                }
                var d2 = new DateTime();
                var days = 1;
                if (Daily.Checked)
                {
                    d2 = DateTime.Now.AddDays(1);
                }
                if (ThreeDays.Checked)
                {
                    days = 3;
                    d2 = DateTime.Now.AddDays(3);
                }
                try
                {
                    await MainWork();
                }
                catch (Exception)
                {

                    return;
                }
                var d1 = DateTime.Now;
                Display($@"work done for today next run will be {DateTime.Now.AddDays(days):dd/MM/yyyy} ");
                await Task.Delay(d2 - d1);
            } while (true);
        }
        private async Task MainWork()
        {
            foreach (var inputModel in inputModels)
            {
                await StartScraping(inputModel);
            }
            if (cars.Count > 0)
            {
                await SaveData();
            }
        }

        private async Task StartScraping(InputModel inputModel)
        {
            var companySellerType = "";
            if (inputModel.CompanySeller)
            {
                companySellerType = "CONCESSIONNAIRE%2CAGENT%2CCENTRE_MULTIMARQUES%2CLOUEUR%2CGARAGISTE%2CREPARATEUR_AGREE%2CINTERMEDIAIRE";
            }
            var model = "";
            var MakeAndModel = "";
            if (inputModel.Make != null)
            {
                MakeAndModel = inputModel.Make.Name;
                if (inputModel.Model != null)
                {
                    model = inputModel.Model.Name;
                }

                if (model != "")
                {
                    MakeAndModel = MakeAndModel + ":" + model;
                }
            }
            var fromDate = "";
            if (inputModel.FromDate != null)
            {

                fromDate = inputModel.FromDate;
            }
            var toDate = "";
            if (inputModel.ToDate != null)
            {

                toDate = inputModel.ToDate;
            }

            if (inputModel.FuelType == null)
            {
                MessageBox.Show(@"please select fuel type");
                throw new Exception();
            }

            var url = $@"https://www.lacentrale.fr/listing?customerFamilyCodes={companySellerType}&energies={inputModel.FuelType.Code}&makesModelsCommercialNames={MakeAndModel}&mileageMax={inputModel.MaxKilometers}&mileageMin={inputModel.MinKilometers}&priceMin={inputModel.MinPrice}&priceMax={inputModel.MaxPrice}&reclaimVAT={inputModel.Vat.ToString().ToLower()}&page=1&yearMax={toDate}&yearMin={fromDate}";
            File.AppendAllText("filters url.txt",
                MakeAndModel + "/" + inputModel.FromDate + "/" + inputModel.ToDate + "/" + inputModel.MinPrice + "/" +
                inputModel.MaxPrice + "/" + inputModel.MinKilometers + "/" + inputModel.MaxKilometers +
                "/" + inputModel.FuelType.Name + "/" + " company seller: " + inputModel.CompanySeller.ToString().ToLower() + "/"
               + " vat: " + inputModel.Vat.ToString().ToLower() + " ==> " + url + "\r\n");

            var doc = await HttpCaller.GetDoc(url);

            var pages = 1;
            var otherPages = doc.DocumentNode.SelectSingleNode("//div[@class='pagination__more']");
            if (otherPages != null)
            {
                pages = int.Parse(doc.DocumentNode.SelectSingleNode("//div[@class='pagination__more']/ul/li[last()]")
                    .InnerText.Trim());
            }
            else
            {
                try
                {
                    pages = int.Parse(doc.DocumentNode.SelectSingleNode("//div[@class='rch-pagination']/ul/li[@class='last']")?.InnerText.Trim() ?? "1");
                }
                catch (Exception)
                {
                    //
                }
            }
            var carNbr = int.Parse(doc.DocumentNode.SelectSingleNode("//span[@class='numAnn']")?.InnerText.Replace(" ", "").Replace(",", "") ?? "0");
            if (carNbr == 0)
            {
                Display($@"no results from filter {MakeAndModel}");
                await Task.Delay(3000);
                return;
            }
            var counter = 1;
            for (int i = 1; i <= pages; i++)
            {
                url = $@"https://www.lacentrale.fr/listing?customerFamilyCodes={companySellerType}&energies={inputModel.FuelType.Code}&makesModelsCommercialNames={MakeAndModel}&mileageMax={inputModel.MaxKilometers}&mileageMin={inputModel.MinKilometers}&priceMin={inputModel.MinPrice}&priceMax={inputModel.MaxPrice}&reclaimVAT={inputModel.Vat.ToString().ToLower()}&page={i}&yearMax={toDate}&yearMin={fromDate}";
                doc = await HttpCaller.GetDoc(url);
                var carsNodes = doc.DocumentNode.SelectNodes("//div[@class='resultList mB15 hiddenOverflow listing ']//a[@class='searchCard__link ']");
                foreach (var carNode in carsNodes)
                {
                    var urlCar = "https://www.lacentrale.fr" + carNode.GetAttributeValue("href", "");
                    var car = await ScrapeCarDetails(urlCar);
                    await Task.Delay(1000);
                    cars.Add(car);
                    Display($@"{counter} car scraped/{carNbr} from filter {MakeAndModel}");
                    SetProgress(((counter * 100) / (carNbr)));
                    counter++;
                }
            }
        }
        private async Task<Car> ScrapeCarDetails(string url)
        {
            var car = new Car();
            car.Url = url;
            var doc = await HttpCaller.GetDoc(url);
            //doc.Save("car.html");
            var title = doc.DocumentNode.SelectSingleNode("//div[@id='generalInformationWrapper']//h1").InnerText
                .Trim();
            var array = title.Split(' ');
            car.Make = array[0];
            car.Model = title.Replace(car.Make, "").Trim();
            car.Year = doc.DocumentNode
                .SelectSingleNode("//*[contains(text(),'circulation')]/../following-sibling::span").InnerText
                .Trim();
            car.Kilometre = doc.DocumentNode
                .SelectSingleNode("//*[contains(text(),'Kilométrage com')]/../following-sibling::span").InnerText
                .Trim();
            var jsonPhoneVaribalesQuery = doc.DocumentNode
                .SelectSingleNode("//script[contains(text(),'window.fragment_seller_contact_tabs_state')]").InnerText
                .Trim().Replace("window.fragment_seller_contact_tabs_state = ", "").Replace(";", "");
            var objt = JObject.Parse(jsonPhoneVaribalesQuery);
            car.Price = string.Format(CultureInfo.InvariantCulture, "€{0:n0}", (int)objt.SelectToken("price"));
            var id = (string)objt.SelectToken("classifiedId");
            var cuRef = (string)objt.SelectToken("classifiedOwnerCorrelationId");
            var eci = (string)objt.SelectToken("eventCorrelationId");
            var link = $@"https://www.lacentrale.fr/seller-contact-tab?tab=PHONE&id={id}&cuRef={cuRef}&eci={eci}";
            doc = await HttpCaller.GetDoc(link);

            var phoneText = WebUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//span[@class='phone']")?.InnerText.Trim());
            if (phoneText != null)
            {
                var arrayP = phoneText.Split('\n');
                car.Phone = arrayP[0];
            }
            else
            {
                car.Phone = "N/A";
            }
            return car;
        }

        private async void DevForm_Load(object sender, EventArgs e)
        {
            if (!Directory.Exists("outcomes"))
            {
                Directory.CreateDirectory("outcomes");
            }

            //await ScrapeConfig();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("conf"));
            MakesRepositoryItemLookUpEdit.DataSource = _config.Makes;
            DateFromRepositoryItemLookUpEdit.DataSource = _config.Dates;
            DateToRepositoryItemLookUpEdit.DataSource = _config.Dates;
            FuelTypeRepositoryItemLookUpEdit.DataSource = _config.Fuels;

            var d = JsonConvert.DeserializeObject<List<InputModel>>(File.ReadAllText("save conf"));
            GridControle.DataSource = new BindingList<InputModel>(d);
        }

        private async Task ScrapeConfig()
        {
            _config = new Config();
            // full options could be in url  https://www.lacentrale.fr/listing?customerFamilyCodes=CONCESSIONNAIRE&energies=elec&makesModelsCommercialNames=RENAULT&mileageMax=50000&mileageMin=10000&priceMax=29000&
            // =true&yearMax=2021&yearMin=2018
            #region Scrape Makes
            var doc = await HttpCaller.GetDoc("https://www.lacentrale.fr/");
            var json = doc.DocumentNode
                .SelectSingleNode("//script[contains(text(),'window.__PRELOADED_STATE_LISTING')]").InnerText.Trim()
                .Replace("window.__PRELOADED_STATE_LISTING__ =", "").Replace("vehicle.makeModelCommercialName", "CommercialName").Replace("vehicle.make", "vehicleMake");

            var objt = JObject.Parse(json);
            var makesNodes = objt.SelectToken("..vehicleMake");
            var makes = new List<Make>();
            foreach (var makesNode in makesNodes)
            {
                var make = new Make();
                make.Name = (string)makesNode.SelectToken("key");
                var models = new List<Model>();
                foreach (var model in makesNode["agg"])
                {
                    models.Add(new Model { Name = (string)model.SelectToken("key") });
                }
                make.Models = models;
                makes.Add(make);
            }
            #endregion
            #region Scrape Dates

            var dts = File.ReadAllLines("Dates.txt");
            var dates = new List<string>();
            foreach (var dt in dts)
            {
                var array = dt.Split(':');
                dates.Add(array[1]);
            }
            #endregion
            #region Add fuelTypes
            var txtFuelTypes = File.ReadAllLines("Fuel types.txt").ToList();
            var fuels = new List<FuelType>();
            foreach (var s in txtFuelTypes)
            {
                var array = s.Split(',');
                fuels.Add(new FuelType { Code = array[1], Name = array[0] });
            }
            #endregion

            _config.Fuels = fuels;
            _config.Dates = dates;
            _config.Makes = makes;
            File.WriteAllText("conf", JsonConvert.SerializeObject(_config, Formatting.Indented));
        }

        private void DevForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            inputModels = new List<InputModel>();
            for (int i = 0; i < FiltersDGV.RowCount; i++)
            {
                var input = FiltersDGV.GetRow(i) as InputModel;
                inputModels.Add(input);
            }
            File.WriteAllText("save conf", JsonConvert.SerializeObject(inputModels, Formatting.Indented));
        }

        private void FiltersDGV_CustomRowCellEdit(object sender, DevExpress.XtraGrid.Views.Grid.CustomRowCellEditEventArgs e)
        {
            if (e.Column.FieldName.Equals("Model"))
            {

                var inputModel = FiltersDGV.GetRow(e.RowHandle) as InputModel;
                if (inputModel == null) return;
                if (inputModel.Make == null)
                {
                    var r2 = new RepositoryItemLookUpEdit();
                    e.RepositoryItem = r2;
                    e.Column.ColumnEdit = r2;
                    return;
                }
                var r = new RepositoryItemLookUpEdit();
                r.DataSource = inputModel.Make.Models;
                r.Columns.Add(new LookUpColumnInfo { FieldName = "Name" });
                r.DisplayMember = "Name";
                GridControle.RepositoryItems.Add(r);
                e.RepositoryItem = r;
                e.Column.ColumnEdit = r;
            }
        }
        private async Task SaveData()
        {
            var date = DateTime.Now.ToString("dd_MM_yyyy hh_mm_ss");
            var path = $@"outcomes\lacentrale.fr{date}.xlsx";
            var excelPkg = new ExcelPackage(new FileInfo(path));

            var sheet = excelPkg.Workbook.Worksheets.Add("Cars");
            sheet.Protection.IsProtected = false;
            sheet.Protection.AllowSelectLockedCells = false;
            sheet.Row(1).Height = 20;
            sheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet.Row(1).Style.Font.Bold = true;
            sheet.Row(1).Style.Font.Size = 8;
            sheet.Cells[1, 1].Value = "Make";
            sheet.Cells[1, 2].Value = "Model";
            sheet.Cells[1, 3].Value = "Price";
            sheet.Cells[1, 4].Value = "Phone";
            sheet.Cells[1, 5].Value = "Year";
            sheet.Cells[1, 6].Value = "Kilometers";
            sheet.Cells[1, 7].Value = "Weblink";

            var range = sheet.Cells[$"A1:G{cars.Count + 1}"];
            var tab = sheet.Tables.Add(range, "");

            tab.TableStyle = TableStyles.Medium2;
            sheet.Cells.Style.Font.Size = 12;

            var row = 2;
            foreach (var car in cars)
            {

                sheet.Cells[row, 1].Value = car.Make;
                sheet.Cells[row, 2].Value = car.Model;
                sheet.Cells[row, 3].Value = car.Price;
                sheet.Cells[row, 4].Value = car.Phone;
                sheet.Cells[row, 5].Value = car.Year;
                sheet.Cells[row, 6].Value = car.Kilometre;
                sheet.Cells[row, 7].Value = car.Url;
                row++;
            }

            for (int i = 2; i <= sheet.Dimension.End.Column; i++)
                sheet.Column(i).AutoFit();

            sheet.View.FreezePanes(2, 1);
            await excelPkg.SaveAsync();

        }
    }
}