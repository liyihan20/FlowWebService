﻿using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 紧急出货运输申请
    /// </summary>
    public class ETRule:BaseRule
    {
        FlowDBDataContext db = new FlowDBDataContext();
        JObject o;

        /// <summary>
        /// 市场部审核人
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetMarketAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string marketName = (string)o["market_name"];
            if ("其它".Equals(marketName)) {
                return "";
            }
            var marketAuditors = db.flow_auditorRelation.Where(f => f.bill_type == "ET"
                && f.relate_name == "市场部审批" && f.relate_text == marketName).Select(f => f.relate_value).ToArray();
            return string.Join(";", marketAuditors);
        }

        /// <summary>
        /// 事业部计划审批
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetBusPlannerAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string busDep = (string)o["bus_dep"];
            var busPlannerAuditors = db.flow_auditorRelation.Where(f => f.bill_type == "ET"
                && f.relate_name == "事业部计划审批" && f.relate_text == busDep).Select(f => f.relate_value).ToArray();
            if (busPlannerAuditors.Count() == 0) return "";

            return string.Join(";", busPlannerAuditors);
        }

        public string GetRunningMinister(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string transferStyle = (string)o["transfer_style"];
            if (!"空运".Equals(transferStyle)) return "";

            var GetRunningMinisters = db.flow_auditorRelation.Where(f => f.bill_type == "ET"
                && f.relate_name == "营运总监审批").Select(f => f.relate_value).ToArray();
            if (GetRunningMinisters.Count() == 0) return "";

            return string.Join(";", GetRunningMinisters);
        }

    }
}