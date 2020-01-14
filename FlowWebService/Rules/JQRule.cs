using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Interface;
using FlowWebService.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace FlowWebService.Rules
{
    /// <summary>
    /// 离职/自离申请流程
    /// </summary>
    public class JQRule : BaseRule, IBeforeStartFlow, IFlowQueue,IStepName,ICancelFlowAfterFinish
    {
        
        FlowDBDataContext db = new FlowDBDataContext();
        string BILLTYPE = "JQ";
        JObject o;

        public List<flow_applyEntryQueue> GetFlowQueue(string formObj)
        {
            o = JObject.Parse(formObj);
            List<flow_applyEntryQueue> list = new List<flow_applyEntryQueue>();
            string sysNo=(string)o["sys_no"];
            string salaryType = (string)o["salary_type"];
            int step = 1;
            string AHAuditor = string.Join(",", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "AH审批" && f.relate_text == salaryType).Select(f => f.relate_value).ToArray());
            if (string.IsNullOrEmpty(AHAuditor)) throw new Exception("找不到AH审批处理人");
            
            if ("月薪".Equals(salaryType)) {
                string depChargerNum = (string)o["dep_charger_num"];
                string highestChargerNum = (string)o["highest_charger_num"];

                //1. 部门负责人
                list.Add(new flow_applyEntryQueue()
                {
                    step = step++,
                    auditors = depChargerNum,
                    step_name = "部门负责人审批",
                    sys_no = sysNo,
                    countersign = false
                });

                //2. 部门最高负责人
                list.Add(new flow_applyEntryQueue()
                {
                    step = step++,
                    auditors = highestChargerNum,
                    step_name = "部门最高负责人审批",
                    sys_no = sysNo,
                    countersign = false
                });
                //3. AH审批
                list.Add(new flow_applyEntryQueue()
                {
                    step = step++,
                    auditors = AHAuditor,
                    step_name = "AH审批",
                    sys_no = sysNo,
                    countersign = false
                });
            }
            else if ("计件".Equals(salaryType)) {
                string groupLeaderNum = (string)o["group_leader_num"];
                string chargerNum = (string)o["charger_num"];
                string produceMinisterNum = (string)o["produce_minister_num"];

                //1. 组长
                list.Add(new flow_applyEntryQueue()
                {
                    step = step++,
                    auditors = groupLeaderNum,
                    step_name = "组长审批",
                    sys_no = sysNo,
                    countersign = false                    
                });

                //2. 主管
                list.Add(new flow_applyEntryQueue()
                {
                    step = step++,
                    auditors = chargerNum,
                    step_name = "主管审批",
                    sys_no = sysNo,
                    countersign = false
                });

                //3. 生产部长
                list.Add(new flow_applyEntryQueue()
                {
                    step = step++,
                    auditors = produceMinisterNum,
                    step_name = "生产部长审批",
                    sys_no = sysNo,
                    countersign = false
                });
            }
            else {
                throw new Exception("此员工薪水类型不是月薪或计件");
            }

            

            return list;
        }

        public void Validate(string formObj, string createUser)
        {
            o = JObject.Parse(formObj);
            string cardNumber = (string)o["card_number"];
            var existsJQ = from j in db.ei_jqApply
                           join a in db.flow_apply on j.sys_no equals a.sys_no
                           where j.card_number == cardNumber
                           && (a.success == null || a.success == true)
                           select a;
            if (existsJQ.Count() > 0) {
                throw new Exception("此离职人已存在未结束或已申请成功的离职申请，不能再次申请！");
            }
        }

        public void DoBeforeFlow(string formObj)
        {
            o = JObject.Parse(formObj);
            string chargerNum = (string)o["charger_num"];
            string highestChargerNum = (string)o["highest_charger_num"];

            //如果提交流程后，没有主管或最高部门负责人的，表示是由上一节点指定下一节点的流程，不使用审核队列
            if (string.IsNullOrEmpty(chargerNum) && string.IsNullOrEmpty(highestChargerNum)) return;

            //一开始的流程，由申请人指定所有审核人，启用审核队列
            var queue = GetFlowQueue(formObj);
            db.flow_applyEntryQueue.InsertAllOnSubmit(queue);
            db.SubmitChanges();
        }

        /// <summary>
        /// 另外指定流程模板中的审核节点名称
        /// </summary>
        /// <param name="stepName"></param>
        /// <param name="formJson"></param>
        /// <returns></returns>
        public string GetSpecStepName(string stepName, string formJson)
        {
            o = JObject.Parse(formJson);
            string salaryType = (string)o["salary_type"];
            string realStepName = stepName;
            if ("月薪".Equals(salaryType)) {
                switch (stepName) {
                    case "节点1":
                        realStepName = "部门负责人";
                        break;
                    case "节点2":
                        realStepName = "部门最高负责人";
                        break;
                    case "节点3":
                        realStepName = "AH部";
                        break;
                }
            }
            else {
                switch (stepName) {
                    case "节点1":
                        realStepName = "组长";
                        break;
                    case "节点2":
                        realStepName = "主管";
                        break;
                    case "节点3":
                        realStepName = "生产部长";
                        break;
                }
            }
            return realStepName;
        }

        public string GetFirstAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string salaryType = (string)o["salary_type"];
            if ("月薪".Equals(salaryType)) {
                return (string)o["dep_charger_num"]; //部门负责人
            }
            else if ("计件".Equals(salaryType)) {
                return (string)o["group_leader_num"]; //组长
            }
            else {
                return "";
            }
        }

        public string GetSecondAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string salaryType = (string)o["salary_type"];
            if ("月薪".Equals(salaryType)) {
                return (string)o["highest_charger_num"]; //部门最高负责人
            }
            else if ("计件".Equals(salaryType)) {
                return (string)o["charger_num"]; //主管
            }
            else {
                return "";
            }
        }

        public string GetThirdAuditor(flow_apply apply, string formJson)
        {
            o = JObject.Parse(formJson);
            string salaryType = (string)o["salary_type"];
            string depName = (string)o["dep_name"];
            if ("月薪".Equals(salaryType)) {
                if (depName.Contains("惠州")) {
                    return string.Join(",", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "AH审批" && f.relate_text == "惠州月薪").Select(f => f.relate_value).ToArray()); //AH
                }
                else {
                    return string.Join(",", db.flow_auditorRelation.Where(f => f.bill_type == BILLTYPE && f.relate_name == "AH审批" && f.relate_text == "汕尾月薪").Select(f => f.relate_value).ToArray()); //AH
                }
                
            }
            else if ("计件".Equals(salaryType)) {
                return (string)o["produce_minister_num"]; //生产部长
            }
            else {
                return "";
            }
        }

        public void CancelFlow(string sysNo, string cardNumber)
        {
            var apply = db.flow_apply.Where(f => f.sys_no == sysNo).FirstOrDefault();
            if (apply == null) throw new Exception("流水单号不存在");
            if (apply.success == null) throw new Exception("此离职申请还未完结，不能作废");
            if (apply.success == false) throw new Exception("此离职申请已被NG，不能作废");

            apply.finish_date = DateTime.Now;
            apply.success = false;
            db.flow_applyEntry.InsertOnSubmit(new flow_applyEntry()
            {
                flow_apply = apply,
                auditors = cardNumber,
                final_auditor = cardNumber,
                pass = false,
                audit_time = DateTime.Now,
                opinion = "AH部作废",
                step = 100,
                step_name = "AH部"
            });
            db.SubmitChanges();
        }
    }
}