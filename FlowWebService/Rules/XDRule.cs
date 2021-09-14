using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 委外超时特殊申请
    /// </summary>
    public class XDRule:BaseRule
    {
        JObject o;
        string BILLTYPE = "XD";

        public string LogAndStockAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            var lod = (string)o["lod_confirm_people_num"];
            var sto = (string)o["stock_confirm_people_num"];

            List<string> auditors = new List<string>();
            if (!string.IsNullOrEmpty(lod)) {
                auditors.Add(lod);
            }
            if (!string.IsNullOrEmpty(sto)) {
                auditors.Add(sto);
            }

            return string.Join(";", auditors.Distinct());
        }

        public string DepChargerAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["dep_charger_num"];
        }

    }
}