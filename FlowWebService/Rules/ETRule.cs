using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System.Linq;
using FlowWebService.Interface;
using System;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 紧急出货运输申请
    /// </summary>
    public class ETRule:BaseRule,IBeforeStartFlow
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

        /// <summary>
        /// 营运部审批
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetRunningMinister(flow_apply apply, string formJson)
        {
            //2020-8-20 施慧曼要求不需要经过营运部
            return "";
            //o = JObject.Parse(formJson);
            //string transferStyle = (string)o["transfer_style"];
            //if (!"空运".Equals(transferStyle)) return "";

            //var GetRunningMinisters = db.flow_auditorRelation.Where(f => f.bill_type == "ET"
            //    && f.relate_name == "营运总监审批").Select(f => f.relate_value).ToArray();
            //if (GetRunningMinisters.Count() == 0) return "";

            //return string.Join(";", GetRunningMinisters);
        }

        /// <summary>
        /// 市场总经理审批
        /// </summary>
        /// <param name="apply"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetMarketManagerAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string marketName = (string)o["market_name"];
            if ("其它".Equals(marketName)) {
                return "";
            }

            var marketManager = db.flow_auditorRelation.Where(f => f.bill_type == "ET"
                && f.relate_name == "市场部总经理审批").Select(f => f.relate_value).ToArray();
            if (marketManager.Count() == 0) return "";

            return string.Join(";", marketManager);
        }


        public void Validate(string formObj, string createUser)
        {
            DateTime fromDate = DateTime.Parse("2021-01-14");
            var app = db.flow_apply.Where(a => a.create_user == createUser && a.flow_template.bill_type == "ET" && a.success == null && a.start_date >= fromDate).FirstOrDefault();

            if (app != null) {
                throw new Exception("2021-01-14物流中心限制：存在未完成的申请流程，结束之前不能再次申请，单号【" + app.sys_no+"】，如有问题请联系物流薛子银");
            }
        }

        public void DoBeforeFlow(string formObj)
        {
            
        }
    }
}