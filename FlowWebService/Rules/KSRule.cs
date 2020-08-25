using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 开源节流
    /// </summary>
    public class KSRule:BaseRule
    {
        FlowDBDataContext db = new FlowDBDataContext();
        string BILLTYPE = "KS";
        JObject o;

        public string GetOperationEmps(flow_apply apply, string formJson)
        {
            return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "营运部审批").Select(f => f.relate_value).ToArray()); 
        }

        public string GetExecutor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["executor_number"];
        }

    }
}