using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace FinTechRPC
{
    public class Program
    {
        public static List<KeyValuePair<string, List<string>>> UserInfoCache = new List<KeyValuePair<string, List<string>>>();

        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

        public static readonly string bankA_SheetID = "1ezlxdR0189onv0Gyv-Nzri65aJBC8pr-CuZ9KEnt2AU";
        public static readonly string bankSheet1 = "Sheet1";
        
        public static readonly string fintech_SheetID = "1WMDlTC0M-lx4TjA3g97_A63VL4ljwywDIBt15IDzbeA";
        public static readonly string fintechSheet0 = "Sheet0";
        public static readonly string fintechSheet1 = "Sheet1";
        public static readonly string fintechSheet2 = "Sheet2";
        public static SheetsService Service;

        public static void Main(string[] args)
        {
            GoogleCredential credential;
            using (var stream = new FileStream("sheet_api.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
            }

            Service = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credential, ApplicationName = "RPC"
            });

            UserInfoCache = getAlluser();

            CreateHostBuilder(args).Build().Run();
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        // 建立使用者銀行帳戶Cache
        public static List<KeyValuePair<string, List<string>>> getAlluser()
        {
            //                UserID  {BankName, AccName, AccID}
            List<KeyValuePair<string, List<string>>> _temp = new List<KeyValuePair<string, List<string>>>();

            IList<IList<object>> values = (IList<IList<object>>) Services.Helper.SheetHelper("get", fintech_SheetID, fintechSheet1, "A2:D");

            if (values != null && values.Count > 0)
            {
                foreach(var row in values)
                {
                    List<string> t = new List<string> { row[1].ToString(), row[2].ToString(), row[3].ToString() };
                    _temp.Add(new KeyValuePair<string, List<string>>(row[0].ToString(), t));
                }
            }

            //Debug
            
            foreach(KeyValuePair<string, List<string>> a in _temp)
            {
                Console.WriteLine("ID:{0} Accid:{1} Bankname:{2} Accname:{3}", a.Key, a.Value[2], a.Value[0], a.Value[1]);
            }

            return _temp;
        }

        // 快速更新LIST方法
        public static void refrezh()
        {
            UserInfoCache = getAlluser();
        }
    }
}
