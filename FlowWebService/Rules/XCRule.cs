using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Interface;

namespace FlowWebService.Rules
{
    public class XCRule : BaseRule, IBeforeStartFlow
    {
        FlowDBDataContext db = new FlowDBDataContext();
        JObject o;
        string BILLTYPE = "XC";

        //计划部负责人
        public string PlannerCharger(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["planner_auditor_num"];
        }
        //部门主管
        public string DepCharger(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["dep_charger_num"];
        }
        //部门总经理
        public string DepManager(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            var depName = (string)o["dep_name"];
            //var currentMonth = DateTime.Now.ToString("yyyy-MM");
            return db.ei_xcDepTarget.Where(x => x.dep_name == depName).OrderByDescending(x => x.year_month).Select(x => x.manager_no).FirstOrDefault();
        }
        //采购
        public string Buyer(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["buyer_auditor_num"];
        }
        //营运部审批
        public string Operation(flow_apply apply, string formJson)
        {
            //o = JObject.Parse(formJson);
            //var total = (int)o["current_month_total"];
            //var target = (int)o["current_month_target"];

            //if (total > target) {
                return "200921024"; //佐健
            //}
            //return "";
        }

        //总裁办审批
        public string CEO(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);

            if ((bool)o["need_ceo_confirm"]) {
                return "00031304"; //王雅婷
            }
            return "";
        }

        //加工部门计划审批
        public string ProcDepPlanner(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            return (string)o["process_planner_auditor_num"];
        }

        ////物料组审批
        //public string MatGroup(flow_apply apply, string formJson)
        //{
        //    o = JObject.Parse(formJson);
        //    return (string)o["mat_group_num"];
        //}

        ////物流盘点发出
        //public string StockOut(flow_apply apply, string formJson)
        //{
        //    return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "物流发出").Select(f => f.relate_value).ToArray());
        //}
        ////仓库接收成品
        //public string StockIn(flow_apply apply, string formJson)
        //{
        //    return string.Join(";", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "仓库接收").Select(f => f.relate_value).ToArray());
        //}

        public void Validate(string formObj, string createUser)
        {
            //var existedBill = from a in db.flow_apply
            //                  join f in db.flow_template on a.flow_template_id equals f.id
            //                  join e in db.flow_applyEntry on new { id = a.id, step_name = "营运部抽检" } equals new { id = (int)e.apply_id, step_name = e.step_name }
            //                  where f.bill_type == BILLTYPE && a.success == null && e.pass == true && a.create_user == createUser
            //                  select a.sys_no;

            //if (existedBill.Count() > 0) {
            //    throw new Exception("存在未领回的委外产品，请领会后再申请，单号：" + existedBill.First());
            //}
        }

        public void DoBeforeFlow(string formObj)
        {
            
        }
    }
}