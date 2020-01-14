using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Interface;
using FlowWebService.Models;
using Newtonsoft.Json.Linq;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 非正常时间出货申请
    /// </summary>
    public class UCRule:BaseRule
    {
        FlowDBDataContext db = new FlowDBDataContext();
        JObject o;

        /// <summary>
        /// 市场部总经理,根据不同事业部选择
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetMarketManager(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string marketName = (string)o["market_name"];
            var marketAuditors = db.flow_auditorRelation.Where(f => f.bill_type.Contains("UC") 
                && f.relate_name == "市场部总经理" && f.relate_text == marketName).Select(f => f.relate_value).ToArray();
            return string.Join(";",marketAuditors);
        }

        /// <summary>
        /// 事业部长审批，根据不同生产事业部选择
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetBusDepMinister(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string busDep = (string)o["bus_dep"];
            var busDepAuditors = db.flow_auditorRelation.Where(f => f.bill_type.Contains("UC") 
                && f.relate_name == "事业部长" && f.relate_text == busDep).Select(f => f.relate_value).ToArray();
            return string.Join(";",busDepAuditors);
        }

        /// <summary>
        /// 会计部审核，根据不同公司选择
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetAccountingManager(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string company = (string)o["company"];
            var companyAuditors = db.flow_auditorRelation.Where(f => f.bill_type.Contains("UC") 
                && f.relate_name == "会计部主管" && f.relate_text == company).Select(f => f.relate_value).ToArray();
            return string.Join(";",companyAuditors);
        }

        /// <summary>
        /// 市场管理部审批，如果不是电子的，到晓哗处理，否则跳过
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetMarketAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string company = (string)o["company"];
            var companyAuditors = db.flow_auditorRelation.Where(f => f.bill_type.Contains("UC")
                && f.relate_name == "市场管理部" && f.relate_text == company).Select(f => f.relate_value).ToArray();
            return string.Join(";", companyAuditors);
        }

    }
}