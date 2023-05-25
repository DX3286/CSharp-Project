using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using FinTechRPC.Services;

namespace FinTechRPC
{
    public class Fintech : FintechService.FintechServiceBase
    {

        private readonly ILogger<Fintech> _logger;
        public Fintech(ILogger<Fintech> logger)
        {
            _logger = logger;
        }

        // 產生憑證
        public override Task<ReturnCode> F_GenerateReceipt(ReceiptInfo request, ServerCallContext context)
        {
            // 產生
            Helper.SheetHelper("add", Program.fintech_SheetID, Program.fintechSheet2, "A:K", new List<object> { request.Caseid, request.User, "-1", request.Receipt, "未送繳", request.Bacc1, request.Bacc2, request.Bacc3, "-1", "-1", request.Amount });

            // 鎖定帳戶金額 Bacc1
            LockBankMoney(request.Bacc1);
            LockBankMoney(request.Bacc2);
            LockBankMoney(request.Bacc3);

            // Return
            return Task.FromResult(new ReturnCode { Code = 1 });
        }

        public void LockBankMoney(string s, int reverse = 0)
        {
            var file = Program.bankA_SheetID;
            var sheet = Program.bankSheet1;

            if (s == "0") { return; }

            string[] _accInfo = s.Split("=");
            int indexOfaccid = (int)Helper.SheetHelper("index", file, sheet, "A:A", null, _accInfo[0]);

            IList<IList<object>> remain = (IList<IList<object>>)Helper.SheetHelper("get", file, sheet, "B" + indexOfaccid);
            IList<IList<object>> lockAmount = (IList<IList<object>>)Helper.SheetHelper("get", file, sheet, "C" + indexOfaccid);

            if (reverse == 1)   // 解除
            {
                Helper.SheetHelper("update", file, sheet, "C" + indexOfaccid, new List<object> { Helper.N(lockAmount[0][0], _accInfo[1]) });
                Helper.SheetHelper("update", file, sheet, "B" + indexOfaccid, new List<object> { Helper.N(remain[0][0], _accInfo[1], 1) });
                return;
            }

            if (reverse == 2)   // 沒入
            {
                Helper.SheetHelper("update", file, sheet, "C" + indexOfaccid, new List<object> { Helper.N(lockAmount[0][0], _accInfo[1]) });
                return;
            } 

            // Normal
            Helper.SheetHelper("update", file, sheet, "B" + indexOfaccid, new List<object> { Helper.N(remain[0][0], _accInfo[1]) });
            Helper.SheetHelper("update", file, sheet, "C" + indexOfaccid, new List<object> { Helper.N(lockAmount[0][0], _accInfo[1], 1) });
        }

        // 寄送憑證
        public override Task<ReturnCode> F_SendReceipt(ReceiptSendInfo request, ServerCallContext context)
        {
            // Check if user exists
            if (Helper.CheckUser(request.Who))
            {
                // Find 'receipt' index update it
                int indexOfreceipt = (int)Helper.SheetHelper("index", Program.fintech_SheetID, Program.fintechSheet2, "D:D", null, request.Receipt);

                Helper.SheetHelper("update", Program.fintech_SheetID, Program.fintechSheet2, "C" + indexOfreceipt, new List<object> { request.Who });
                Helper.SheetHelper("update", Program.fintech_SheetID, Program.fintechSheet2, "E" + indexOfreceipt, new List<object> { "已送繳" });
                Helper.SheetHelper("update", Program.fintech_SheetID, Program.fintechSheet2, "I" + indexOfreceipt, new List<object> { DateTime.Now.ToString("yyyy/MM/dd HH:mm") });

                return Task.FromResult(new ReturnCode { Code = 1 }); // Success
            }

            return Task.FromResult(new ReturnCode { Code = -1 }); // User doesnt exists
        }

        // 註冊帳號
        public override Task<ReturnCode> fUserCreate(UserRegInfo request, ServerCallContext context)
        {
            // Check if User already exists
            if (Helper.CheckUser(request.Id))
            {
                return Task.FromResult(new ReturnCode
                {
                    Code = 0 // Exists
                });
            }

            // Create new User
            Helper.SheetHelper("add", Program.fintech_SheetID, Program.fintechSheet0, "A:E", new List<object> { request.Id, request.Pass, request.Email, request.Ucode, request.Coname });
            return Task.FromResult(new ReturnCode
            {
                Code = 1 // Success
            });
        }

        // 使用者登入
        public override Task<UserInfo> fUserLogin(UserLoginInfo request, ServerCallContext context)
        {
            if (Helper.CheckUser(request.Id, request.Pass))
            {
                // 帳密符合
                return Task.FromResult(new UserInfo
                {
                    Userid = request.Id
                });
            }
            // 帳密不符
            return Task.FromResult(new UserInfo
            {
                Userid = ""
            });
        }

        // 回傳該帳號擁有的銀行帳戶(MAX3)
        public override Task<AccountID> fAccountManagement(UserInfo request, ServerCallContext context)
        {
            List<string> temp = new List<string>();

            foreach (KeyValuePair<string, List<string>> a in Program.UserInfoCache) // Program.UserInfoCache 為使用者銀行帳戶LIST，見PROGRAM.CS
            {
                if (a.Key == request.Userid)
                {
                    temp.Add(a.Value[2]);
                }
                //Console.WriteLine("ID:{0} Accid:{1} Bankname:{2} Accname:{3}", a.Key, a.Value[2], a.Value[0], a.Value[1]);
            }

            AccountID acc = new AccountID
            {
                Str1 = (temp.Count <= 0 ? "Empty" : temp[0]),
                Str2 = (temp.Count <= 1 ? "Empty" : temp[1]),
                Str3 = (temp.Count <= 2 ? "Empty" : temp[2]), // 大於3個暫時無視，我就懶
            };

            return Task.FromResult(acc);
        }

        // 建立銀行帳戶
        public override Task<ReturnCode> F_AccCreate(BankInfo request, ServerCallContext context)
        {
            // Max 3 accounts and check repeat
            int time = 0;
            foreach (KeyValuePair<string, List<string>> a in Program.UserInfoCache)
            {
                // 不分使用者，銀行帳戶ID 不會重複
                if (request.Accid == a.Value[2])
                {
                    return Task.FromResult(new ReturnCode { Code = 0 }); // 帳戶已存在
                }

                // 有分使用者的
                if (a.Key == request.Uid)
                {
                    time++;

                    if (request.Accname == a.Value[1])
                    {
                        return Task.FromResult(new ReturnCode { Code = 1 }); // 帳戶名稱重複
                    }
                }
            }

            if (time >= 3)
            {
                return Task.FromResult(new ReturnCode { Code = 2 }); // 帳戶數量已達上限
            }

            // 沒問題，新增
            Helper.SheetHelper("add", Program.fintech_SheetID, Program.fintechSheet1, "A:D", new List<object> { request.Uid, request.Bankname, request.Accname, request.Accid });

            Program.refrezh(); // 更新使用者銀行帳戶LIST，見PROGRAM.CS

            // 測試用，同時新增於銀行端
            Helper.SheetHelper("add", Program.bankA_SheetID, Program.bankSheet1, "A:C", new List<object> { request.Accid, "100000", "0" });

            return Task.FromResult(new ReturnCode { Code = 3 }); // 帳戶新增成功
        }

        // 於憑證設定頁面調整金額時，更新單一銀行帳戶餘額
        public override Task<BankRemain> F_AccSearch(BankInfo r, ServerCallContext context)
        {
            IList<IList<object>> values = (IList<IList<object>>) Helper.SheetHelper("get", Program.bankA_SheetID, Program.bankSheet1, "A2:B");

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row[0].ToString() == r.Accid)
                    {
                        return Task.FromResult(new BankRemain
                        {
                            Money = Helper.N(row[1])
                        });
                    }
                }
            }
            return Task.FromResult(new BankRemain { Money = -1 });
        }

        // 憑證設定頁面開啟時，一次讀取所有銀行帳戶餘額
        public override Task<AccountID> F_AccSearchAll(UserInfo r, ServerCallContext context)
        {
            int ii = 0;
            string[] ss = new string[] { "0", "0", "0" };

            IList<IList<object>> values = (IList<IList<object>>) Helper.SheetHelper("get", Program.bankA_SheetID, Program.bankSheet1, "A2:B");

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    foreach (KeyValuePair<string, List<string>> a in Program.UserInfoCache)
                    {
                        if (r.Userid.ToString() == a.Key && a.Value[2] == row[0].ToString())
                        {
                            ss[ii] = row[1].ToString();
                            ii++;
                        }
                    }
                }

                AccountID b = new AccountID
                {
                    Str1 = ss[0],
                    Str2 = ss[1],
                    Str3 = ss[2]
                };

                return Task.FromResult(b);
            }

            return Task.FromResult(new AccountID
            {
                Str1 = "0",
                Str2 = "0",
                Str3 = "0"
            });
        }

        public override Task<CaseArray> F_CaseManagement(UserInfoWT request, ServerCallContext context)
        {
            List<CaseInfo> temp = new List<CaseInfo>();

            IList<IList<object>> values = (IList<IList<object>>)Helper.SheetHelper("get", Program.fintech_SheetID, Program.fintechSheet2, "A:K");

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row[1].ToString() == request.Userid && request.Utype == "投標")
                    {
                        temp.Add(new CaseInfo
                        {
                            Caseid = row[0].ToString(),
                            Who = row[2].ToString(),
                            Receipt = row[3].ToString(),
                            Stats = row[4].ToString(),
                            Startdate = row[8].ToString(),
                            Enddate = row[9].ToString(),
                            Amount = row[10].ToString()
                        });
                    }
                    if (row[2].ToString() == request.Userid && request.Utype == "招標")
                    {
                        temp.Add(new CaseInfo
                        {
                            Caseid = row[0].ToString(),
                            Who = row[1].ToString(),
                            Receipt = row[3].ToString(),
                            Stats = row[4].ToString(),
                            Startdate = row[8].ToString(),
                            Enddate = row[9].ToString(),
                            Amount = row[10].ToString()
                        });
                    }
                }
            }

            var ca = new CaseArray();

            ca.Caseinfo.Add(temp);

            //Console.WriteLine(ca.ToString());
            return Task.FromResult(ca);
        }

        public override Task<ReturnCode> F_DisableCase(DisableCaseInfo request, ServerCallContext context)
        {
            int indexOfCase = (int) Helper.SheetHelper("index", Program.fintech_SheetID, Program.fintechSheet2, "A:A", null, request.Caseid);

            IList<IList<object>> values = (IList<IList<object>>)Helper.SheetHelper("get", Program.fintech_SheetID, Program.fintechSheet2, "A:K");
            // 解除
            if (request.Reason == "解除")
            {
                if (values != null && values.Count > 0)
                {
                    foreach (var row in values)
                    {
                        if (row[0].ToString() == request.Caseid)
                        {
                            LockBankMoney(row[5].ToString(), 1);
                            LockBankMoney(row[6].ToString(), 1);
                            LockBankMoney(row[7].ToString(), 1);
                            Helper.SheetHelper("update", Program.fintech_SheetID, Program.fintechSheet2, "E" + indexOfCase, new List<object> { "已解除" });
                            Helper.SheetHelper("update", Program.fintech_SheetID, Program.fintechSheet2, "J" + indexOfCase, new List<object> { DateTime.Now.ToString("yyyy/MM/dd HH:mm") });
                            break;
                        }
                    }
                }
            }
            // 沒入
            if (request.Reason == "沒入")
            {
                if (values != null && values.Count > 0)
                {
                    foreach (var row in values)
                    {
                        if (row[0].ToString() == request.Caseid)
                        {
                            LockBankMoney(row[5].ToString(), 2);
                            LockBankMoney(row[6].ToString(), 2);
                            LockBankMoney(row[7].ToString(), 2);
                            Helper.SheetHelper("update", Program.fintech_SheetID, Program.fintechSheet2, "E" + indexOfCase, new List<object> { "已沒入" });
                            Helper.SheetHelper("update", Program.fintech_SheetID, Program.fintechSheet2, "J" + indexOfCase, new List<object> { DateTime.Now.ToString("yyyy/MM/dd HH:mm") });
                            break;
                        }
                    }
                }
            }
            return Task.FromResult(new ReturnCode { Code = 1 });
        }

        //
        public override Task<ReturnCode> F_AddCase(AddCaseInfo request, ServerCallContext context)
        {
            int indexOfCase = (int)Helper.SheetHelper("index", Program.fintech_SheetID, Program.fintechSheet2, "A:A", null, request.Caseid);

            if (indexOfCase == -1)
            {
                return Task.FromResult(new ReturnCode { Code = -1 }); //Case doesnt exists
            }

            IList<IList<object>> values = (IList<IList<object>>)Helper.SheetHelper("get", Program.fintech_SheetID, Program.fintechSheet2, "A" + indexOfCase + ":" + "K" + indexOfCase);

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row[4].ToString() == "未送繳" && row[3].ToString() == request.Receipt && row[1].ToString() == request.Who)
                    {
                        Helper.SheetHelper("update", Program.fintech_SheetID, Program.fintechSheet2, "C" + indexOfCase, new List<object> { request.Whoami });
                        Helper.SheetHelper("update", Program.fintech_SheetID, Program.fintechSheet2, "E" + indexOfCase, new List<object> { "已送繳" });
                        Helper.SheetHelper("update", Program.fintech_SheetID, Program.fintechSheet2, "I" + indexOfCase, new List<object> { DateTime.Now.ToString("yyyy/MM/dd HH:mm") });
                        return Task.FromResult(new ReturnCode { Code = 1 });
                    }
                }
                
            }

            return Task.FromResult(new ReturnCode { Code = 0 });
        }

        public override Task<UserInfo> F_SearchCase(AddCaseInfo request, ServerCallContext context)
        {
            IList<IList<object>> values = (IList<IList<object>>)Helper.SheetHelper("get", Program.fintech_SheetID, Program.fintechSheet2, "A:K");

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    if (row[0].ToString() == request.Caseid || row[1].ToString() == request.Who || row[3].ToString() == request.Receipt)
                    {
                        return Task.FromResult(new UserInfo { Userid = row[0].ToString() });
                    }
                }
            }

            return Task.FromResult(new UserInfo { Userid = "null" });
        }
    }
}
