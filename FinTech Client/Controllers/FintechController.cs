using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FinTech.Models;
using FinTechRPC;

namespace FinTech.Controllers
{
    public class FintechController : ControllerBase
    {
        private readonly GrpcChannel channel;

        public FintechController()
        {
            channel = GrpcChannel.ForAddress("https://localhost:5001");
        }

        /**public bool AddBankAcc(string bankname, string accname, string accid, string user = "user")
        {
			var client = new FintechService.FintechServiceClient(channel);
			var reply = client.F_AccCreateAsync(
			new BankInfo { Uid = user, Bankname = bankname, Accname = accname, Accid = Int32.Parse(accid) });
			return true;
		}**/

        public async Task<int> CreateUser(string userid, string password, string mail, string code, string name)
        {
            var client = new FintechService.FintechServiceClient(channel);
            var reply = await client.fUserCreateAsync(new UserRegInfo { Id = userid, Pass = password, Email = mail, Ucode = code, Coname = name });

            return reply.Code; // -1 error, 0 exists, 1 success
        }

        public async Task<string> Fintech_Login(string id, string pass)
        {
            var client = new FintechService.FintechServiceClient(channel);
            var reply = await client.fUserLoginAsync(new UserLoginInfo { Id = id, Pass = pass });
            HomeController.loggedinUser = reply.Userid;
            return reply.Userid;
        }

        public async Task<int> AddBankAcc(string userid, string bankname, string accname, string accid)
        {
            var client = new FintechService.FintechServiceClient(channel);
            var reply = await client.F_AccCreateAsync(
            new BankInfo { Uid = userid, Bankname = bankname, Accname = accname, Accid = accid });
            return reply.Code;
        }

        public async Task<string[]> AccountManagement(string id)
        {
            string[] temp = new string[3];
            var client = new FintechService.FintechServiceClient(channel);
            var reply = await client.fAccountManagementAsync(new UserInfo { Userid = id });

            temp[0] = reply.Str1;
            temp[1] = reply.Str2;
            temp[2] = reply.Str3;
            return temp;
        }

        public async Task<List<string>> getAllBankAcc(string id)
        {
            List<string> temp = new List<string>();
            var client = new FintechService.FintechServiceClient(channel);
            var reply = await client.fAccountManagementAsync(new UserInfo { Userid = id });

            if (reply.Str1 != "Empty")
            {
                temp.Add(reply.Str1);
            }
            if (reply.Str2 != "Empty")
            {
                temp.Add(reply.Str2);
            }
            if (reply.Str3 != "Empty")
            {
                temp.Add(reply.Str3);
            }

            return temp;
        }

        public async Task<string> ShowRemain(string id)
        {
            var client = new FintechService.FintechServiceClient(channel);
            var reply = await client.F_AccSearchAsync(new BankInfo { Accid = id });

            return reply.Money.ToString();
        }

        public async Task<string[]> ShowRemainAll(string id)
        {
            string[] temp = new string[3];

            var client = new FintechService.FintechServiceClient(channel);
            var reply = await client.F_AccSearchAllAsync(new UserInfo { Userid = id });

            temp[0] = reply.Str1; temp[1] = reply.Str2; temp[2] = reply.Str3;
            return temp;
        }

        public async Task<string> GenReceipt(string uid, string a1, string a2, string a3, string amount)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string generatedstr = "";
			Random rnd = new Random();
			for (int i = 0; i < 16; i++)
            {
				generatedstr += chars[rnd.Next(chars.Length)];
            }

            string _caseid = DateTime.Now.ToString("MMddHHmmss");
            string _receipt = generatedstr;

            var client = new FintechService.FintechServiceClient(channel);
            var r = await client.F_GenerateReceiptAsync(new ReceiptInfo { User = uid, Caseid = _caseid, Receipt = _receipt, Bacc1 = a1, Bacc2 = a2, Bacc3 = a3, Amount = amount });

            if (r.Code == 1)
            {
            	return _receipt;
            }
            return "Error";
        }

        public async Task<int> SendReceipt(string receipt, string who)
        {
            var client = new FintechService.FintechServiceClient(channel);
            var r = await client.F_SendReceiptAsync(new ReceiptSendInfo { Receipt = receipt, Who = who });
            return r.Code;
        }

        public async Task<List<string>> GetCaseInfo(string uid, string utype, string caseid = null)
        {
            
            var client = new FintechService.FintechServiceClient(channel);
            var r = await client.F_CaseManagementAsync(new UserInfoWT { Userid = uid, Utype = utype });

            List<string> s = new List<string>();

            if (caseid != null) //查詢
            {
                // Caseinfo {caseid, who, receipt, stats, startdate, enddate, amount}
                foreach (var c in r.Caseinfo)
                {
                    if (c.Caseid == caseid)
                    {
                        s.Add(c.Caseid);
                        s.Add(c.Who);
                        s.Add(c.Receipt);
                        s.Add(c.Stats);
                        s.Add(c.Startdate);
                        s.Add(c.Enddate);
                        s.Add(c.Amount);
                        return s;
                    }
                }
            }

            // 點進專案管理時顯示用的
            // [caseid, who, receipt, stat, amount]
            var rc = r.Caseinfo;
            for (int i = 0; i < rc.Count; i++)
            {
                if (utype == "招標" && (rc[i].Stats == "已解除" || rc[i].Stats == "已沒入"))
                {
                    continue;
                }

                s.Add(rc[i].Caseid);//0 5 10
                s.Add(rc[i].Who);
                s.Add(rc[i].Receipt);
                s.Add(rc[i].Stats);
                s.Add(rc[i].Amount);//4 9 14
            }

            return s;
        }

        public async Task<int> DisableCase(string caseid, string reason)
        {
            var client = new FintechService.FintechServiceClient(channel);
            var r = await client.F_DisableCaseAsync(new DisableCaseInfo { Caseid = caseid, Reason = reason });
            return r.Code;
        }

        public async Task<int> AddNewCase(string caseid, string receipt, string who, string whoami)
        {
            var client = new FintechService.FintechServiceClient(channel);
            var r = await client.F_AddCaseAsync(new AddCaseInfo { Caseid = caseid, Receipt = receipt, Who = who, Whoami = whoami });
            return r.Code;
        }

        public async Task<string> SearchCase(string caseid, string receipt, string who, string whoami)
        {
            if (whoami == who)
            {
                return "null";
            }

            var client = new FintechService.FintechServiceClient(channel);
            var r = await client.F_SearchCaseAsync(new AddCaseInfo { Caseid = caseid, Receipt = receipt, Who = who });
            return r.Userid;
        }
    }
}
