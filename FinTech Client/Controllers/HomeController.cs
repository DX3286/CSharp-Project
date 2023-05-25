using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FinTech.Models;
using FinTechRPC;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinTech.Controllers
{
    public class HomeController : Controller
    {
        UserModel model = new UserModel();
        public static string loggedinUser = "";

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult 投標廠商功能() { model.UserID = loggedinUser; return View(model); }
        public IActionResult 投標廠商登入() { return View(model); }
        public IActionResult Homepage() { 
            return View(model);
        }
        public IActionResult 帳戶驗證() { return View(model); }
        public IActionResult 帳戶申請() { model.UserID = loggedinUser; return View(model); }
        public IActionResult 憑證設定() {
            model.UserID = loggedinUser;
            //FintechController ff = new FintechController();
            //var temp = ff.getAllBankAcc(loggedinUser).Result;

            //var list = new List<SelectListItem>();

            //for (int i = 0; i < temp.Count; i++)
            //{
            //    list.Add(new SelectListItem { Text = temp[i], Value = temp[i] });
            //}

            //ViewBag.allacc = list;
            return View(model);
        }
        public IActionResult 帳戶設定()
        {
            model.UserID = loggedinUser;
            return View(model);
        }
        public IActionResult 廠商專案管理() {
            model.UserID = loggedinUser;
            return View(model); }
        public IActionResult 投標廠商註冊(string utype) {
            ViewBag.utype = utype; // 0 =投標 1 =招標
            return View(); 
        }
        public IActionResult 憑證產出(string receipt) {
            model.UserID = loggedinUser;
            ViewBag.myReceipt = receipt;
            return View(model); 
        }
        public IActionResult 廠商專案查詢(string caseid) { 
            model.UserID = loggedinUser;
            ViewBag.myCaseID = caseid;
            return View(model); 
        }
        public IActionResult 招標機關功能() { model.UserID = loggedinUser; return View(model); }
        public IActionResult 招標機關專案清單() { model.UserID = loggedinUser; return View(model); }
        public IActionResult 招標機關專案設定() { model.UserID = loggedinUser; return View(model); }
        public IActionResult 招標機關專案管理() { model.UserID = loggedinUser; return View(model); }
        public IActionResult 招標機關註記() { model.UserID = loggedinUser; return View(model); }
        public IActionResult 招標機關專案查詢(string caseid) {
            ViewBag.myCaseID = caseid;
            model.UserID = loggedinUser; return View(model); 
        }
        //public IActionResult () { return View(); }
        //public IActionResult () { return View(); }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public string getUser()
        {
            return loggedinUser;
            //return "user";
        }

        public ViewResult UserLogout()
        {
            loggedinUser = "";
            return View("Homepage");
        }
    }
}
