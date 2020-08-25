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
    /// 电子考勤补记（漏刷卡流程），CR：Card Register
    /// </summary>
    public class CRRule : BaseRule, IBeforeStartFlow, IFlowQueue
    {
        FlowDBDataContext db = new FlowDBDataContext();

        public void Validate(string formObj, string createUser)
        {

        }

        public void DoBeforeFlow(string formObj)
        {
            var o = JObject.Parse(formObj);
            string auditorQueues = (string)o["auditor_queues"];
            if (string.IsNullOrEmpty(auditorQueues)) {
                //流程开始之前，先将审核人队列准备好
                var queue = GetFlowQueue(formObj);
                db.flow_applyEntryQueue.InsertAllOnSubmit(queue);
                db.SubmitChanges();
            }
            else {
                List<flow_applyEntryQueue> list = JsonConvert.DeserializeObject<List<flow_applyEntryQueue>>(auditorQueues);
                db.flow_applyEntryQueue.InsertAllOnSubmit(list);
                db.SubmitChanges();
            }
        }

        public virtual List<flow_applyEntryQueue> GetFlowQueue(string formObj)
        {
            List<flow_applyEntryQueue> list = new List<flow_applyEntryQueue>();

            var o = JObject.Parse(formObj);
            string sysNo = (string)o["sys_no"];
            string cardNo = (string)o["applier_num"];
            string depNo = (string)o["dep_no"];
            string processName = "请假";
            string[] spNode = new string[] { "AH审批", "行政审批" };
            bool isDirectCharge = ((bool?)o["is_direct_charge"]) ?? false;

            int step = 1;

            var dep = db.ei_department.Where(d => d.FNumber == depNo).FirstOrDefault();
            if (dep == null) {
                throw new Exception("部门不存在");
            }
            if (isDirectCharge) {
                //直管
                var ceoDep = GetNearestDep(depNo, "董事办公室");
                if (ceoDep == null) {
                    ceoDep = GetNearestDep(depNo, "总经理办公室"); //没有董事办公室，看有没有总经理办公室
                }
                if (ceoDep == null) {
                    throw new Exception("[董事办公室/总经理办公室]审核人未设置");
                }
                var n0 = GetGivenDepAuditor(ceoDep, "请假");
                n0.sys_no = sysNo;
                n0.step = step++;
                list.Add(n0);
            }
            else {
                //一级节点
                var node = dep.ei_departmentAuditNode.Where(d => d.FProcessName == processName && !spNode.Contains(d.FAuditNodeName) && dep.FIsAuditNode == true).FirstOrDefault();
                if (node != null) {
                    var auditUser = node.ei_departmentAuditUser.Where(u => u.FBeginTime <= DateTime.Now && u.FEndTime >= DateTime.Now && (u.isDeleted == false || u.isDeleted == null)).ToList();
                    if (auditUser.Count() > 0) {
                        flow_applyEntryQueue queue = new flow_applyEntryQueue();
                        queue.countersign = node.FIsCounterSign;
                        queue.step_name = node.FAuditNodeName;
                        queue.auditors = string.Join(";", auditUser.Select(u => u.FAuditorNumber).ToArray());
                        queue.step = step++;
                        queue.sys_no = sysNo;
                        list.Add(queue);
                    }
                }

                //二级节点
                if (dep.FParent != null) {
                    dep = db.ei_department.Single(d => d.FNumber == dep.FParent);
                    node = dep.ei_departmentAuditNode.Where(d => d.FProcessName == processName && !spNode.Contains(d.FAuditNodeName) && dep.FIsAuditNode == true).FirstOrDefault();
                    if (node != null) {
                        var auditUser = node.ei_departmentAuditUser.Where(u => u.FBeginTime <= DateTime.Now && u.FEndTime >= DateTime.Now && (u.isDeleted == false || u.isDeleted == null)).ToList();
                        if (auditUser.Count() > 0) {
                            flow_applyEntryQueue queue = new flow_applyEntryQueue();
                            queue.countersign = node.FIsCounterSign;
                            queue.step_name = node.FAuditNodeName;
                            queue.auditors = string.Join(";", auditUser.Select(u => u.FAuditorNumber).ToArray());
                            queue.step = step++;
                            queue.sys_no = sysNo;
                            if (list.Where(l => l.auditors == queue.auditors).Count() == 0) {
                                list.Add(queue);
                            }
                        }
                    }
                }
            }
            return list;
        }
    }
}