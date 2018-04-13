using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlowWebService.Utils
{
    public class BillUtils
    {
        public object GetRuleInstance(string billType)
        {
            try {
                Type t = Type.GetType(string.Format("FlowWebService.Rules.{0}Rule", billType));
                if (t.IsClass) {
                    return Activator.CreateInstance(t);
                }
                else {
                    throw new Exception("规则转化为类失败");
                }
            }
            catch{
                throw new Exception("找不到规则:" + billType);
            }            
        }
    }
}