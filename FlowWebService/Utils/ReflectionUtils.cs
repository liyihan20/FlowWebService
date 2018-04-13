using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace FlowWebService.Utils
{
    public class ReflectionUtils
    {
        public static object GetObjPropertyVal(object obj, string propertyName)
        {
            try {
                var typ = obj.GetType();
                return typ.GetProperty(propertyName).GetValue(obj, null);
            }
            catch {                
                throw new Exception("对象中此字段不存在："+propertyName);
            }
        }
    }
}