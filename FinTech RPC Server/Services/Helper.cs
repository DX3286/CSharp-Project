using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinTechRPC.Services
{
    public class Helper
    {
        public static int N(object obj, object obj2 = null, int mode = 0)
        {
            if (obj2 == null)
            {
                return int.Parse(obj.ToString());
            }

            if (mode == 1)  // plus
            {
                return (int.Parse(obj.ToString()) + int.Parse(obj2.ToString()));
            }
            // subtract
            return (int.Parse(obj.ToString()) - int.Parse(obj2.ToString()));
        }

        public static bool CheckUser(string id, string pass = null)
        {
            IList<IList<object>> values = (IList<IList<object>>)SheetHelper("get",Program.fintech_SheetID, Program.fintechSheet0, "A2:B");

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    // 有提供密碼，登入用
                    if (pass != null)
                    {
                        if (row[0].ToString() == id && row[1].ToString() == pass)
                        {
                            return true;
                        }
                    }

                    // 沒提供密碼，查找ID用
                    if (pass == null)
                    {
                        if (row[0].ToString() == id)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // mode = "add", "get", "update", "index" , "delete"
        // 使用 "Index" 以取得 Range(EX = A:A) 中等於 Match 值的行數。
        public static object SheetHelper(string _mode, string _file, string _sheet, string _range, List<object> _list = null, string _match = null)
        {
            switch (_mode.Trim().ToLower())
            {
                //
                case "add":
                    var range1 = $"{_sheet}!" + _range;
                    var valueRange = new ValueRange();
                    valueRange.Values = new List<IList<object>> { _list };
                    var appendReq = Program.Service.Spreadsheets.Values.Append(valueRange, _file, range1);
                    appendReq.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                    appendReq.Execute();
                    return true;
                //
                case "get":
                    var range2 = $"{_sheet}!" + _range;
                    var getReq = Program.Service.Spreadsheets.Values.Get(_file, range2);
                    var reply = getReq.Execute();
                    var value = reply.Values;
                    return value;
                //
                case "index":
                    // 防呆
                    if (_range.Length > 1)
                    {
                        _range = _range.Substring(0, 1) + ":" + _range.Substring(0, 1);
                    }
                    else if (_range.Length == 1)
                    {
                        _range = _range + ":" + _range;
                    }
                    // 防呆 Console.WriteLine("Debug Range: " + _range);
                    IList<IList<object>> v = (IList<IList<object>>) SheetHelper("get", _file, _sheet, _range); // 讀取所有 Range 的值

                    if (v != null && v.Count > 0)
                    {
                        for (int i = 0; i < v.Count; i++)
                        {
                            if (v[i][0].ToString() == _match)
                            {
                                return i + 1;
                            }
                        }
                    }
                    break;
                //
                case "update":
                    var finalrange = $"{_sheet}!" + _range;
                    var valueRange2 = new ValueRange();
                    valueRange2.Values = new List<IList<object>> { _list };
                    var updateReq = Program.Service.Spreadsheets.Values.Update(valueRange2, _file, finalrange);
                    updateReq.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    updateReq.Execute();
                    break;
                //
                case "delete":
                    var deleteRange = $"{_sheet}!" + _range;
                    var deleteRequest = new ClearValuesRequest();
                    var deleteReq = Program.Service.Spreadsheets.Values.Clear(deleteRequest, _file, deleteRange);
                    deleteReq.Execute();
                    break;
            }

            return -1;
        }

    }
}

/**
            var range = $"{Program.fintechSheet2}!A:H";
            var valueRange = new ValueRange();

            var objList = new List<object> { request.Caseid, request.User, "-1", request.Receipt, "未送繳", request.Bacc1, request.Bacc2, request.Bacc3};
            valueRange.Values = new List<IList<object>> { objList };
            var appendReq = Program.Service.Spreadsheets.Values.Append(valueRange, Program.fintech_SheetID, range);
            appendReq.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            appendReq.Execute();


            var range0 = $"{Program.fintechSheet0}!A:B";
            var request0 = Program.Service.Spreadsheets.Values.Get(Program.fintech_SheetID, range0);

            var response0 = request0.Execute();
            var values0 = response0.Values;

            if (values0 != null && values0.Count > 0)
**/