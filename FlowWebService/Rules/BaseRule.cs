using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FlowWebService.Utils;
using FlowWebService.Models;
using FlowWebService.Interface;

namespace FlowWebService.Rules
{
    public class BaseRule
    {
        private FlowDBDataContext db = new FlowDBDataContext();

        public string GetObjStringValue(object formObj, string fieldName)
        {
            return Convert.ToString(ReflectionUtils.GetObjPropertyVal(formObj, fieldName));
        }

        public int GetObjIntValue(object formObj, string fieldName)
        {
            return Convert.ToInt32(ReflectionUtils.GetObjPropertyVal(formObj, fieldName));
        }

        public decimal GetObjDecimalValue(object formObj, string fieldName)
        {
            return Convert.ToDecimal(ReflectionUtils.GetObjPropertyVal(formObj, fieldName));
        }

        public bool GetObjBoolValue(object formObj, string fieldName)
        {
            return Convert.ToBoolean(ReflectionUtils.GetObjPropertyVal(formObj, fieldName));
        }

        public DateTime GetObjDateTimeValue(object formObj, string fieldName)
        {
            return Convert.ToDateTime(ReflectionUtils.GetObjPropertyVal(formObj, fieldName));
        }

        #region 通用的与表单没关的获取审核人规则

        //获取之前某一步骤的记录
        public List<flow_applyEntry> GetPreStep(flow_apply apply, int step)
        {
            return apply.flow_applyEntry.Where(e => e.step == step).ToList();
        }

        //获取之前审核环节的某一步骤审核人
        public string GetPreStepAuditor_1(flow_apply apply, string formJson, string param1)
        {
            int preStep;
            if (!int.TryParse(param1, out preStep)) {
                throw new Exception("参数类型必须是整型：" + param1);
            }
            var preAuditors = apply.flow_applyEntry.Where(f => f.step == preStep && f.pass == true).Select(f => f.final_auditor).ToArray();
            if (preAuditors.Count() == 0) {
                throw new Exception("此审核步骤的审核人为空：" + param1);
            }
            return string.Join(";", preAuditors);
        }
        

        //获取申请人
        public string GetApplier(flow_apply apply, string formJson)
        {
            return apply.create_user;
        }

        #endregion

        /// <summary>
        /// 获取上级部门审核人
        /// </summary>
        /// <param name="depNo">申请人部门</param>
        /// <param name="skipNum">跳过的部门审核人数量</param>
        /// <param name="processName">流程名称</param>
        /// <returns></returns>
        protected flow_applyEntryQueue GetParentDepAuditor(string depNo, string processName, int skipNum = 0,bool canBeNull=true)
        {
            ei_department dep;
            try {
                dep = db.ei_department.Single(d => d.FNumber == depNo);
            }
            catch {
                throw new Exception("部门不存在，编码：" + depNo);
            }
            string[] spNode=new string[]{"AH审批","行政审批"};
            var auditNodes = db.ei_departmentAuditNode.Where(a => a.ei_department == dep && a.FProcessName == processName && a.ei_department.FIsAuditNode == true && !spNode.Contains(a.FAuditNodeName)).ToList() ;
            ei_departmentAuditNode node = null;
            int currentNum = 0;
            while (node == null && dep.FParent != null) {
                if (auditNodes.Count() > 0) {
                    if (auditNodes.First().ei_departmentAuditUser.Where(u => u.FBeginTime <= DateTime.Now && u.FEndTime >= DateTime.Now && (u.isDeleted == false || u.isDeleted == null)).Count() > 0) {
                        currentNum++;
                        if (currentNum > skipNum) {
                            node = auditNodes.First();
                            break;
                        }
                    }
                }
                dep = db.ei_department.Single(d => d.FNumber == dep.FParent);
                auditNodes = db.ei_departmentAuditNode.Where(a => a.ei_department == dep && a.FProcessName == processName && a.ei_department.FIsAuditNode == true && !spNode.Contains(a.FAuditNodeName)).ToList();
            }
            if (node != null) {
                flow_applyEntryQueue queue = new flow_applyEntryQueue();
                queue.countersign = node.FIsCounterSign;
                queue.step_name = node.FAuditNodeName;
                queue.auditors = string.Join(";", node.ei_departmentAuditUser.Where(u => u.FBeginTime <= DateTime.Now && u.FEndTime >= DateTime.Now && (u.isDeleted == false || u.isDeleted == null)).Select(u => u.FAuditorNumber).ToArray());
                return queue;
            }
            if (!canBeNull) {
                throw new Exception("上级部门审核人没有设置,错误代码：" + depNo + "-" + skipNum);
            }
            else {
                return null;
            }
        }


        /// <summary>
        /// 获取当前部门的审核人
        /// </summary>
        /// <param name="depNo">部门编码</param>
        /// <param name="processName">流程名称</param>
        /// <returns></returns>
        protected flow_applyEntryQueue GetGivenDepAuditor(string depNo, string processName)
        {
            ei_department dep;
            try {
                dep = db.ei_department.Single(d => d.FNumber == depNo);
            }
            catch {
                throw new Exception("部门不存在，编码：" + depNo);
            }
            return GetGivenDepAuditor(dep, processName);
        }

        /// <summary>
        /// 获取当前部门的审核人
        /// </summary>
        /// <param name="dep">部门</param>
        /// <param name="processName">流程名称</param>
        /// <returns></returns>
        protected flow_applyEntryQueue GetGivenDepAuditor(ei_department dep, string processName)
        {
            var auditNodes = db.ei_departmentAuditNode.Where(a => a.ei_department == dep && a.FProcessName == processName && a.ei_department.FIsAuditNode == true).ToList();
            ei_departmentAuditNode node = null;
            if (auditNodes.Count() > 0) {
                if (auditNodes.First().ei_departmentAuditUser.Where(u => u.FBeginTime <= DateTime.Now && u.FEndTime >= DateTime.Now && (u.isDeleted == false || u.isDeleted == null)).Count() > 0) {
                    node = auditNodes.First();
                }
            }
            else {
                throw new Exception("部门（" + dep.FNumber + ":" + dep.FName + "）没有启用审批节点");
            }
            if (node != null) {
                flow_applyEntryQueue queue = new flow_applyEntryQueue();
                queue.countersign = node.FIsCounterSign;
                queue.step_name = node.FAuditNodeName;
                queue.auditors = string.Join(";", node.ei_departmentAuditUser.Where(u => u.FBeginTime <= DateTime.Now && u.FEndTime >= DateTime.Now && (u.isDeleted == false || u.isDeleted == null)).Select(u => u.FAuditorNumber).ToArray());
                return queue;
            }
            throw new Exception("部门（" + dep.FNumber + ":" + dep.FName + "）【" + auditNodes.First().FAuditNodeName + "】审核人没有设置");
        }

        /// <summary>
        /// 逐级往上找，找到最近的符合条件的部门就返回，否则返回null
        /// </summary>
        /// <param name="depNo">搜索的起点部门</param>
        /// <param name="targetDepName">目标部门名称</param>
        /// <returns></returns>
        protected ei_department GetNearestDep(string startDepNo, string targetDepName)
        {
            ei_department target = null;
            //只在同级部门或上级部门的平级部门找，不在子级部门或上级的子级部门找
            ei_department startDep = db.ei_department.Single(d => d.FNumber == startDepNo);
            while (target == null && startDep.FParent != null) {
                var resultDeps = db.ei_department.Where(d => d.FName == targetDepName && d.FNumber.StartsWith(startDep.FParent) && d.FNumber.Length == startDep.FNumber.Length).ToList();
                if (resultDeps.Count() > 0) {
                    target = resultDeps.First();
                }
                else {
                    startDep = db.ei_department.Single(d => d.FNumber == startDep.FParent);
                }
            }

            return target;
        }


        protected ei_department GetNearestParentDepByNodeName(string startDepNo, string auditNodeName,string processName)
        {
            ei_department target = null;

            ei_department startDep = db.ei_department.Single(d => d.FNumber == startDepNo);
            while (target == null && startDep.FParent != null) {
                var parentDep=db.ei_department.Single(d=>d.FNumber==startDep.FParent);
                var resultDeps = db.ei_departmentAuditNode.Where(d => d.FDepartmentId == parentDep.id && d.FAuditNodeName == auditNodeName && d.FProcessName == processName).ToList();
                if (resultDeps.Count() > 0) {
                    target = resultDeps.First().ei_department;
                }
                else {
                    startDep = db.ei_department.Single(d => d.FNumber == startDep.FParent);
                }
            }

            return target;
        }
    }
}